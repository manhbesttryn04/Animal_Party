using System.Collections;
using UnityEngine;

public class PauseGameManager : MonoBehaviour
{
    public static PauseGameManager Instance { get; private set; }

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Pause State")]
    [SerializeField] private bool isPaused;

    [Header("Pause Permission")]
    [Tooltip(
        "Bật: PauseGame() sẽ tạm dừng game.\n" +
        "Tắt: Setting vẫn mở nhưng game không bị tạm dừng."
    )]
    [SerializeField] public bool enablePauseGame = true;

    [Header("Resume Time")]
    [Tooltip("Thời gian tăng Time.timeScale từ 0 lên 1.")]
    [Min(0f)]
    [SerializeField] private float resumeDuration = 0.5f;

    private Coroutine resumeRoutine;

    public bool IsPaused => isPaused;
    public bool IsPauseEnabled => enablePauseGame;

    private void Awake()
    {
        SetupSingleton();
        SetupPausePanel();
    }

    private void SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void SetupPausePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        /*
         * Trường hợp tắt Enable Pause Game trong Inspector
         * khi game đang Pause thì tự động Resume.
         */
        if (!enablePauseGame && isPaused)
        {
            ForceResume();
        }
    }

    //==================================================
    // CHO PHÉP HOẶC KHÔNG CHO PHÉP PAUSE
    //==================================================

    public void SetPauseEnabled(bool enabled)
    {
        enablePauseGame = enabled;

        if (!enablePauseGame && isPaused)
        {
            ForceResume();
        }
    }

    public void EnablePause()
    {
        SetPauseEnabled(true);
    }

    public void DisablePause()
    {
        SetPauseEnabled(false);
    }

    //==================================================
    // CÔNG TẮC PAUSE
    //==================================================

    public void TogglePause()
    {
        if (!enablePauseGame)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    //==================================================
    // PAUSE GAME
    //==================================================

    public void PauseGame()
    {
        /*
         * Khi Enable Pause Game bị tắt,
         * Setting vẫn có thể mở nhưng game không Pause.
         */
        if (!enablePauseGame || isPaused)
        {
            return;
        }

        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }

        isPaused = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.StartDepthBlur();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseGameplayAudio();
        }

        Time.timeScale = 0f;
    }

    //==================================================
    // RESUME GAME
    //==================================================

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }

        if (resumeDuration <= 0f)
        {
            ForceResume();
            return;
        }

        resumeRoutine = StartCoroutine(
            ResumeGameRoutine()
        );
    }

    private IEnumerator ResumeGameRoutine()
    {
        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.ResetDepthBlur();
        }

        float startTimeScale = Time.timeScale;
        float elapsedTime = 0f;

        while (elapsedTime < resumeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / resumeDuration
            );

            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            Time.timeScale = Mathf.Lerp(
                startTimeScale,
                1f,
                progress
            );

            yield return null;
        }

        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeGameplayAudio();
        }

        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        resumeRoutine = null;
    }

    //==================================================
    // ÉP RESUME
    // DÙNG TRƯỚC KHI CHUYỂN SCENE
    //==================================================

    public void ForceResume()
    {
        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }

        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeGameplayAudio();
        }

        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.ResetDepthBlur();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeGameplayAudio();
            }

            Instance = null;
        }
    }
}