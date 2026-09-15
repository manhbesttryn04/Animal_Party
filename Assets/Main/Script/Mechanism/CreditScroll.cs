using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditScroll : MonoBehaviour
{
    [Header("Credit Text")]
    public RectTransform creditText;

    [Header("Credit Position")]
    public Vector2 startPosition;
    public Vector2 endPosition;

    [Header("Intro Panel")]
    public CanvasGroup introPanel;
    public float introFadeTime = 4f;

    [Header("Credit Time")]
    public float creditTime = 60f;

    [Header("Black Outro Panel")]
    [Tooltip("CanvasGroup chứa một Image màu đen phủ toàn màn hình")]
    public CanvasGroup outroPanel;

    [Tooltip("Thời gian màn hình chuyển dần sang đen")]
    public float outroFadeTime = 4f;

    [Header("Fade All Audio")]
    [Tooltip("Thời gian tăng dần âm lượng nhạc khi Credit bắt đầu")]
    public float startAudioFadeTime = 5f;

    [Tooltip("Thời gian giảm toàn bộ âm thanh về 0")]
    public float endAudioFadeTime = 4f;

    [Header("THE END")]
    public TextMeshProUGUI theEndText;
    public float theEndFadeTime = 1.5f;

    [Header("Return To Menu")]
    [Tooltip("Thời gian chờ sau khi chữ THE END hiện hoàn toàn")]
    public float waitAfterTheEnd = 7f;

    [Header("Skip")]
    public KeyCode skipKey = KeyCode.Space;

    [Tooltip("Button 1 thường là B trên Xbox hoặc Circle trên PlayStation.")]
    [Range(0, 19)]
    [SerializeField] private int controllerSkipButtonIndex = 1;

    [Tooltip("Thời gian từ lúc Credit bắt đầu đến khi được phép skip.")]
    [SerializeField] private float skipEnableDelay = 2f;

    [Header("Load Scene")]
    public string loadSceneName = "MainMenu";

    private float creditTimer;

    private bool canScroll;
    private bool isEnding;
    private bool canSkip;

    private Coroutine introCoroutine;
    private Coroutine endCoroutine;

    private void Start()
    {
        creditTimer = 0f;
        canScroll = false;
        isEnding = false;
        canSkip = false;

        var volume = VolumeManager.Instance;
        if (volume != null)
        {
            volume.ResetVignette();
        }
        var audio = AudioManager.Instance;
        if (audio != null)
        {
            audio.ZeroAllAudio();
            audio.PauseAudio();
            audio.SetupMainGameAudio();
        }
        var cursor = CursorManager.Instance;
        if (cursor != null)
        {
            cursor.SetSceneCursorVisible(false);
            cursor.SetSettingCursorActive(false);
        }
        var setting = SettingManager.Instance;
        if(setting != null)
        {
            setting.ResetSetting();
        }
        SetupCredit();
        SetupIntroPanel();
        SetupOutroPanel();
        SetupTheEndText();

        PlayCreditMusic();

        introCoroutine = StartCoroutine(IntroRoutine());
        StartCoroutine(EnableSkipAfterDelay());
    }

    private IEnumerator EnableSkipAfterDelay()
    {
        // Dùng realtime để không bị ảnh hưởng bởi Time.timeScale.
        if (skipEnableDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(skipEnableDelay);
        }

        if (!isEnding)
        {
            canSkip = true;
        }
    }

    private void Update()
    {
        if (canSkip)
        {
            bool keyboardSkipDown =
                Input.GetKeyDown(skipKey);

            if (keyboardSkipDown ||
                IsControllerSkipDown())
            {
                SkipCredit();
            }
        }

        if (!canScroll || isEnding)
            return;

        creditTimer += Time.deltaTime;

        float percent;

        if (creditTime <= 0f)
        {
            percent = 1f;
        }
        else
        {
            percent = Mathf.Clamp01(
                creditTimer / creditTime
            );
        }

        if (creditText != null)
        {
            creditText.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                percent
            );
        }

        if (percent >= 1f)
        {
            StartEndSequence();
        }
    }

    //==================================================
    // SETUP
    //==================================================

    private void SetupCredit()
    {
        if (creditText != null)
        {
            creditText.anchoredPosition = startPosition;
        }
    }

    private void SetupIntroPanel()
    {
        if (introPanel == null)
            return;

        introPanel.gameObject.SetActive(true);

        // Bắt đầu bằng màu đen
        introPanel.alpha = 1f;

        introPanel.blocksRaycasts = true;
        introPanel.interactable = false;
    }

    private void SetupOutroPanel()
    {
        if (outroPanel == null)
            return;

        outroPanel.gameObject.SetActive(false);
        outroPanel.alpha = 0f;
        outroPanel.interactable = false;
        outroPanel.blocksRaycasts = false;
    }

    private void SetupTheEndText()
    {
        if (theEndText == null)
            return;

        Color color = theEndText.color;
        color.a = 0f;
        theEndText.color = color;
    }

    //==================================================
    // MUSIC
    //==================================================

    private void PlayCreditMusic()
    {
        AudioManager audio = AudioManager.Instance;

        if (audio == null)
        { 
            return;
        }

        audio.FadeInMusic(
            audio.creditMusicClip,
            startAudioFadeTime
        );
        audio.PlayEnvironment(audio.theSeaClip);
    }

    //==================================================
    // INTRO
    //==================================================

    private IEnumerator IntroRoutine()
    {
        if (introPanel == null)
        {
            canScroll = true;
            introCoroutine = null;

            yield break;
        }

        if (introFadeTime <= 0f)
        {
            introPanel.alpha = 0f;
            introPanel.gameObject.SetActive(false);

            canScroll = true;
            introCoroutine = null;

            yield break;
        }

        float timer = 0f;

        while (timer < introFadeTime)
        {
            if (isEnding)
                yield break;

            timer += Time.deltaTime;

            float percent = Mathf.Clamp01(
                timer / introFadeTime
            );

            introPanel.alpha = Mathf.Lerp(
                1f,
                0f,
                percent
            );

            yield return null;
        }

        introPanel.alpha = 0f;
        introPanel.blocksRaycasts = false;
        introPanel.gameObject.SetActive(false);

        canScroll = true;
        introCoroutine = null;
    }

    //==================================================
    // CONTROLLER SKIP
    //==================================================

    private bool IsControllerSkipDown()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null)
            return false;

        bool console1Skip =
            controller.IsConsole1Connected() &&
            controller.GetConsoleButtonDown(
                1,
                controllerSkipButtonIndex
            );

        bool console2Skip =
            controller.IsConsole2Connected() &&
            controller.GetConsoleButtonDown(
                2,
                controllerSkipButtonIndex
            );

        return console1Skip || console2Skip;
    }

    //==================================================
    // SKIP
    //==================================================

    public void SkipCredit()
    {
        // Khóa cả trường hợp SkipCredit() được gọi từ Button/UI khác.
        if (!canSkip || isEnding)
            return;

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        if (introPanel != null)
        {
            introPanel.alpha = 0f;
            introPanel.gameObject.SetActive(false);
        }

        if (creditText != null)
        {
            creditText.anchoredPosition = endPosition;
        }

        creditTimer = creditTime;

        StartEndSequence();
    }

    //==================================================
    // END SEQUENCE
    //==================================================

    private void StartEndSequence()
    {
        if (isEnding)
            return;

        isEnding = true;
        canScroll = false;

        if (endCoroutine != null)
        {
            StopCoroutine(endCoroutine);
        }

        endCoroutine = StartCoroutine(EndRoutine());
    }

    private IEnumerator EndRoutine()
    {
        // Bật lớp màn hình đen nhưng alpha ban đầu bằng 0
        if (outroPanel != null)
        {
            outroPanel.gameObject.SetActive(true);
            outroPanel.alpha = 0f;
            outroPanel.blocksRaycasts = true;
        }

        // Fade toàn bộ âm thanh cùng lúc với màn hình đen
        AudioManager audio = AudioManager.Instance;

        if (audio != null)
        {
            audio.FadeOutAllAudio(endAudioFadeTime);
        }

        // Màn hình chuyển dần sang đen
        yield return StartCoroutine(FadeToBlack());

        // Chỉ sau khi màn hình đen hoàn toàn mới hiện THE END
        yield return StartCoroutine(FadeTheEnd());

        // Chờ 7 giây sau khi THE END hiện xong
        if (waitAfterTheEnd > 0f)
        {
            yield return new WaitForSeconds(waitAfterTheEnd);
        }

        if (!string.IsNullOrEmpty(loadSceneName))
        {
            SceneManager.LoadScene(loadSceneName);
        }

        endCoroutine = null;
    }

    //==================================================
    // FADE BLACK
    //==================================================

    private IEnumerator FadeToBlack()
    {
        if (outroPanel == null)
            yield break;

        if (outroFadeTime <= 0f)
        {
            outroPanel.alpha = 1f;
            yield break;
        }

        float timer = 0f;

        while (timer < outroFadeTime)
        {
            timer += Time.deltaTime;

            float percent = Mathf.Clamp01(
                timer / outroFadeTime
            );

            outroPanel.alpha = Mathf.Lerp(
                0f,
                1f,
                percent
            );

            yield return null;
        }

        outroPanel.alpha = 1f;
        var maincharacterandmap = MapAndCharacterManager.Instance;
        if(maincharacterandmap != null)
        {
            var mainmap = maincharacterandmap.mainMap;
            if(mainmap != null)
            {
                mainmap.SetActive(false);
            }

            var maincharacter = maincharacterandmap.mainCharacters;
            if(maincharacter != null)
            {
                maincharacter.SetActive(false);
            }
          
        }
    }

    //==================================================
    // FADE THE END
    //==================================================

    private IEnumerator FadeTheEnd()
    {
        if (theEndText == null)
            yield break;

        Color color = theEndText.color;
        color.a = 0f;
        theEndText.color = color;

        if (theEndFadeTime <= 0f)
        {
            color.a = 1f;
            theEndText.color = color;

            yield break;
        }

        float timer = 0f;

        while (timer < theEndFadeTime)
        {
            timer += Time.deltaTime;

            float percent = Mathf.Clamp01(
                timer / theEndFadeTime
            );

            color.a = Mathf.Lerp(
                0f,
                1f,
                percent
            );

            theEndText.color = color;

            yield return null;
        }

        color.a = 1f;
        theEndText.color = color;
    }
}