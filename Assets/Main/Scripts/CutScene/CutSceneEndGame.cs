using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CutSceneEndGame : MonoBehaviour
{
    [Header("Teleport")]
    public GameObject teleport;

    [Header("Player")]
    public GameObject player;
    public Transform[] transPlayerToWalk;

    [Header("Camera Cut Scene Points")]
    public Transform[] transCameraCutSceneList;

    [Header("Settings")]
    public float teleportOpenTime = 0.5f;
    public float waitTime = 0.5f;
    public float cameraMoveTime = 1f;

    [Header("Player Move")]
    public float playerMoveSpeed = 1f;
    public float playerSlowMoveSpeed = 0.4f;

    [Header("Black Panels")]
    public GameObject blackPanel;
    public GameObject blackStopPanel;

    [Header("--- SUBTITLE NARRATOR ---")]
    public GameObject subtitlePanel;
    public TMP_Text subtitleText;
    public Outline subtitleOutline;

    [Header("--- SKIP UI ---")]
    [Tooltip("GameObject chứa chữ Press Space to Skip")]
    public GameObject skipHintObject;

    [Tooltip("TMP Text của skip hint")]
    public TMP_Text skipHintText;

    [Tooltip("UI biểu tượng nút B. Có tay cầm thì hiện, không có thì ẩn.")]
    public GameObject controllerBHintObject;

    [Header("--- SKIP HINT TEXT ---")]
    [SerializeField]
    private string keyboardSkipHint = "Press Space to Skip";

    [SerializeField]
    private string controllerSkipHint = "Press Space or B to Skip";

    [Header("--- CONTROLLER SKIP ---")]
    [Tooltip("Button 1 thường là B trên Xbox hoặc Circle trên PlayStation.")]
    [Range(0, 19)]
    [SerializeField] private int controllerSkipButtonIndex = 1;

    [Header("--- SKIP DELAY ---")]
    [Tooltip("Thời gian từ lúc cutscene bắt đầu đến khi được phép skip.")]
    [SerializeField] private float skipEnableDelay = 2f;

    [Header("--- GRADIENT PRESET ---")]
    public TMP_ColorGradient normalGradient;
    public TMP_ColorGradient finalGradient;

    [Header("--- OUTLINE COLOR ---")]
    public Color normalOutlineColor = new Color(1f, 1f, 1f, 0.2f);
    public Color finalOutlineColor = new Color(1f, 1f, 1f, 0.31f);

    [Header("--- NARRATOR VOICE ---")]
    public AudioClip[] narratorVoices;

    [Header("--- SUBTITLE TIMING & SPEED ---")]
    public float fadeOutDuration = 0.4f;
    public float betweenLineFade = 0.5f;

    [Tooltip("Sau khi narrator thực sự nói xong, chờ thêm thời gian này rồi tắt subtitle.")]
    public float subtitleHideDelay = 0.2f;

    [Tooltip("Tốc độ chạy chữ (số ký tự trên mỗi giây).")]
    [SerializeField] private float subtitleSpeed = 40f;

    [Tooltip("Độ trễ thời gian (giây) để chữ chạy nhanh hơn voice một chút.")]
    [SerializeField] private float textSpeedOffset = 0.2f;

    [Header("--- END AUDIO FADE ---")]
    public float endAudioFadeTime = 4.18f;

    private bool isSkipped;
    private bool canSkip;

    private int subtitleRequestId;
    private bool waitSkipReleaseAfterSetting;
    private bool previousHasController;
    private bool skipHintStateInitialized;

    private readonly string[] storyLines =
    {
        "A gateway to glory... it finally appears.",
        "The winner walks into legend.",
        "Behold... the island that legends are made of.",
        "Riches untold, claimed by only the worthy.",
        "This is what it was all for.",
        "The Party Animal has found its true champion!"
    };

    private Vector3 teleportOriginalScale;

    private void Awake()
    {
        SetupTeleport();
        FindPlayerWinner();
    }

    private void SetupTeleport()
    {
        if (teleport != null)
        {
            teleportOriginalScale = teleport.transform.localScale;
            teleport.transform.localScale = Vector3.zero;
        }
    }

    private void Start()
    {
        SetupSubtitleUI();
        SetupSkipUI();
        SetupSceneUI();

        PlayCutScene();
        StartCoroutine(EnableSkipAfterDelay());
        StartCoroutine(ShowSkipHint());
    }

    private void SetupSubtitleUI()
    {
        canSkip = false;

        if (subtitlePanel != null)
            subtitlePanel.SetActive(false);

        if (subtitleText != null)
        {
            subtitleText.text = "";
            subtitleText.alpha = 1f;
        }
    }

    private void SetupSkipUI()
    {
        UpdateSkipHintText(true);

        if (skipHintObject != null)
            skipHintObject.SetActive(false);
    }

    private void SetupSceneUI()
    {
        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.canvasNotifi.SetActive(false);
            ui.openSettingPanelButton.SetActive(false);
        }

        var setting = SettingManager.Instance;
        if (setting != null)
        {
            setting.ResetSetting();
            setting.canOpenSettingByEsc = false;
            setting.isOpenExitButton = false;
            setting.canOpenSettingByController = false;
        }

        var cursor = CursorManager.Instance;
        if (cursor != null)
        {
            cursor.SetSceneCursorVisible(false);
        }
    }

    private void Update()
    {
        UpdateSkipHintText();

        if (isSkipped)
            return;

        if (!canSkip)
            return;

        bool settingOpen = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;

        if (settingOpen)
        {
            waitSkipReleaseAfterSetting = true;
            return;
        }

        if (waitSkipReleaseAfterSetting)
        {
            bool skipInputHeld = Input.GetKey(KeyCode.Space) ||
                                 Input.GetKey(KeyCode.Return) ||
                                 IsControllerSkipHeld();

            if (!skipInputHeld)
            {
                waitSkipReleaseAfterSetting = false;
            }

            return;
        }

        bool keyboardSkipDown = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

        if (keyboardSkipDown || IsControllerSkipDown())
        {
            SkipCutscene();
        }
    }

    private bool IsControllerSkipDown()
    {
        ControllerManager controller = ControllerManager.Instance;
        if (controller == null)
            return false;

        bool console1Skip = controller.IsConsole1Connected() && controller.GetConsoleButtonDown(1, controllerSkipButtonIndex);
        bool console2Skip = controller.IsConsole2Connected() && controller.GetConsoleButtonDown(2, controllerSkipButtonIndex);

        return console1Skip || console2Skip;
    }

    private void UpdateSkipHintText(bool force = false)
    {
        ControllerManager controller = ControllerManager.Instance;
        bool hasController = controller != null && controller.HasAnyController();

        if (!force && skipHintStateInitialized && hasController == previousHasController)
        {
            return;
        }

        previousHasController = hasController;
        skipHintStateInitialized = true;

        if (skipHintText != null)
        {
            skipHintText.text = hasController ? controllerSkipHint : keyboardSkipHint;
        }

        if (controllerBHintObject != null)
        {
            controllerBHintObject.SetActive(hasController);
        }
    }

    private bool IsControllerSkipHeld()
    {
        ControllerManager controller = ControllerManager.Instance;
        if (controller == null)
            return false;

        bool console1SkipHeld = controller.IsConsole1Connected() && controller.GetConsoleButton(1, controllerSkipButtonIndex);
        bool console2SkipHeld = controller.IsConsole2Connected() && controller.GetConsoleButton(2, controllerSkipButtonIndex);

        return console1SkipHeld || console2SkipHeld;
    }

    private void SkipCutscene()
    {
        if (!canSkip || isSkipped)
            return;

        isSkipped = true;

        StopAllCoroutines();
        StopNarratorVoice();
        StopPlayerMovement();
        HideSubtitleImmediate();

        if (skipHintObject != null)
            skipHintObject.SetActive(false);

        StartCoroutine(SkipToEnd());
    }

    private IEnumerator SkipToEnd()
    {
        if (blackStopPanel != null)
            blackStopPanel.SetActive(true);

        AudioManager audio = AudioManager.Instance;
        if (audio != null)
        {
            audio.ZeroAllAudio();
            audio.PauseAudio();

            if (audio.specialSource != null)
                audio.specialSource.Stop();
        }

        var setting = SettingManager.Instance;
        if (setting != null)
        {
            setting.canOpenSettingByEsc = false;
            setting.canOpenSettingByController = false;
        }

        if (LoadingManager.Instance != null)
        {
            yield return StartCoroutine(LoadingManager.Instance.ShowLoading());
        }

        SceneManager.LoadScene(6);
    }

    private IEnumerator EnableSkipAfterDelay()
    {
        if (skipEnableDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(skipEnableDelay);
        }

        if (isSkipped)
            yield break;

        canSkip = true;
    }

    private IEnumerator ShowSkipHint()
    {
        if (skipEnableDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(skipEnableDelay);
        }

        if (isSkipped || !canSkip)
            yield break;

        if (skipHintObject != null)
            skipHintObject.SetActive(true);

        if (skipHintText == null)
            yield break;

        skipHintText.alpha = 0f;

        float time = 0f;
        const float duration = 0.5f;

        while (time < duration)
        {
            if (isSkipped)
                yield break;

            time += Time.unscaledDeltaTime;
            skipHintText.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }

        skipHintText.alpha = 1f;
    }

    public void PlayCutScene()
    {
        StartCoroutine(CutSceneRoutine());
    }

    private IEnumerator WaitForSecondsWhilePaused(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (isSkipped) yield break;

            bool isPaused = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;

            if (!isPaused)
            {
                elapsed += Time.deltaTime;
            }

            yield return null;
        }
    }

    private IEnumerator CutSceneRoutine()
    {
        if (isSkipped)
            yield break;

        if (player == null || teleport == null)
        {
            yield break;
        }

        AudioManager audio = AudioManager.Instance;
        if (audio != null)
        {
            audio.PlayEnvironment(audio.javaLoopClip);
        }

        StartCoroutine(ShowFirstSubtitleWithDelay(storyLines[0], 0, false, 0.2f));

        if (audio != null)
        {
            audio.PlaySFX(audio.openTeleportClip);
        }

        yield return StartCoroutine(ScaleTeleport(teleportOriginalScale, teleportOpenTime));

        yield return StartCoroutine(WaitForSecondsWhilePaused(2f));

        if (transPlayerToWalk != null && transPlayerToWalk.Length > 0 && transPlayerToWalk[0] != null)
        {
            StartCoroutine(ShowSubtitleWithVoice(storyLines[1], 1, false));

            PlayerVFX playerVFX = player.GetComponent<PlayerVFX>();
            if (playerVFX != null)
            {
                StartCoroutine(playerVFX.DissolveInNoParticleRoutine());
            }

            Coroutine playerMoveRoutine = StartCoroutine(
                MovePlayerToPoint(player, transPlayerToWalk[0].position, playerMoveSpeed)
            );

            yield return StartCoroutine(WaitForSecondsWhilePaused(3f));

            if (audio != null)
            {
                audio.PlaySFX(audio.closeTeleportClip);
            }

            yield return StartCoroutine(ScaleTeleport(Vector3.zero, 1f));

            yield return playerMoveRoutine;
        }

        SetCameraToPoint(0);
        StartCoroutine(ShowSubtitleWithVoice(storyLines[2], 2, false));

        yield return StartCoroutine(MoveCameraToPoint(1));
        yield return StartCoroutine(MoveCameraToPoint(2));
        yield return StartCoroutine(MoveCameraToPoint(3));

        SetCameraToPoint(4);
        StartCoroutine(ShowSubtitleWithVoice(storyLines[3], 3, false));

        yield return StartCoroutine(MoveCameraToPoint(5));
        yield return StartCoroutine(WaitForSecondsWhilePaused(waitTime));

        SetCameraToPoint(6);
        yield return StartCoroutine(MoveCameraToPoint(7));

        SetCameraToPoint(8);
        StartCoroutine(ShowSubtitleWithVoice(storyLines[4], 4, false));

        yield return StartCoroutine(MoveCameraToPoint(9));
        SetCameraToPoint(10);

        yield return StartCoroutine(WaitForSecondsWhilePaused(waitTime));
        yield return StartCoroutine(MoveCameraToPoint(11));

        yield return StartCoroutine(WaitForSecondsWhilePaused(waitTime));
        SetCameraToPoint(12);

        if (transPlayerToWalk != null && transPlayerToWalk.Length > 1 && transPlayerToWalk[1] != null)
        {
            Coroutine playerMoveRoutine = StartCoroutine(
                MovePlayerToPoint(player, transPlayerToWalk[1].position, playerSlowMoveSpeed)
            );

            yield return StartCoroutine(MoveCameraToPoint(13));
            SetCameraToPoint(14);
            yield return StartCoroutine(MoveCameraToPoint(15));

            yield return StartCoroutine(WaitForSecondsWhilePaused(0.2f));
            SetCameraToPoint(16);

            yield return playerMoveRoutine;

            PlayerAnimator aniPlayer = player.GetComponent<PlayerAnimator>();
            if (aniPlayer != null)
            {
                aniPlayer.playerAnimator.SetTrigger("Win");
            }

            yield return StartCoroutine(ShowSubtitleWithVoice(storyLines[5], 5, true));

            if (skipHintObject != null)
                skipHintObject.SetActive(false);

            HideSubtitleImmediate();
            StopNarratorVoice();

            if (blackStopPanel != null)
                blackStopPanel.SetActive(true);

            if (audio != null)
                audio.FadeOutAllAudio(endAudioFadeTime);

            yield return StartCoroutine(WaitForSecondsWhilePaused(endAudioFadeTime));

            var setting = SettingManager.Instance;
            if (setting != null)
            {
                setting.canOpenSettingByEsc = false;
                setting.canOpenSettingByController = false;
            }

            if (LoadingManager.Instance != null)
            {
                yield return StartCoroutine(LoadingManager.Instance.ShowLoading());
            }

            yield return StartCoroutine(WaitForSecondsWhilePaused(5f));
            SceneManager.LoadScene(6);
        }
        else
        {
            yield return StartCoroutine(MoveCameraToPoint(13));
            SetCameraToPoint(14);

            yield return StartCoroutine(ShowSubtitleWithVoice(storyLines[5], 5, true));

            yield return StartCoroutine(MoveCameraToPoint(15));
            HideSubtitleImmediate();
        }
    }

    private IEnumerator ShowFirstSubtitleWithDelay(string line, int voiceIndex, bool isFinal, float delaySeconds)
    {
        if (isSkipped)
            yield break;

        yield return StartCoroutine(WaitForSecondsWhilePaused(delaySeconds));

        if (isSkipped)
            yield break;

        yield return StartCoroutine(ShowSubtitleWithVoice(line, voiceIndex, isFinal));
    }

    private IEnumerator ShowSubtitleWithVoice(string line, int voiceIndex, bool isFinal)
    {
        if (isSkipped)
            yield break;

        int mySubtitleRequestId = ++subtitleRequestId;

        StopNarratorVoice();

        if (subtitleText != null && !string.IsNullOrEmpty(subtitleText.text))
        {
            yield return StartCoroutine(FadeOutLine());
            yield return StartCoroutine(WaitForSecondsWhilePaused(betweenLineFade));
        }

        if (isSkipped || mySubtitleRequestId != subtitleRequestId)
            yield break;

        if (subtitlePanel != null)
            subtitlePanel.SetActive(true);

        if (subtitleText != null)
        {
            subtitleText.alpha = 1f;
            subtitleText.text = "";
            subtitleText.enableVertexGradient = true;
            subtitleText.colorGradientPreset = isFinal ? finalGradient : normalGradient;
        }

        if (subtitleOutline != null)
        {
            subtitleOutline.effectColor = isFinal ? finalOutlineColor : normalOutlineColor;
        }

        PlayVoice(voiceIndex);

        AudioManager audio = AudioManager.Instance;
        float voiceDuration = 3f;
        if (audio != null && narratorVoices != null && voiceIndex >= 0 && voiceIndex < narratorVoices.Length && narratorVoices[voiceIndex] != null)
        {
            voiceDuration = narratorVoices[voiceIndex].length;
        }

        // --- HIỆU ỨNG CHẠY CHỮ (TYPEWRITER) CÓ HỖ TRỢ PAUSE ---
        if (subtitleText != null && !string.IsNullOrEmpty(line))
        {
            float targetDuration = Mathf.Max(0.1f, voiceDuration - textSpeedOffset);
            float calculatedSpeed = line.Length / targetDuration;
            float actualSpeed = Mathf.Max(subtitleSpeed, calculatedSpeed);

            float t = 0f;
            int charsToShow = 0;

            while (charsToShow < line.Length && !isSkipped && mySubtitleRequestId == subtitleRequestId)
            {
                bool isPaused = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;
                if (!isPaused)
                {
                    t += Time.deltaTime * actualSpeed;
                    charsToShow = Mathf.Clamp(Mathf.FloorToInt(t), 0, line.Length);
                    subtitleText.text = line.Substring(0, charsToShow);
                }
                yield return null;
            }
            subtitleText.text = line;
        }

        if (audio != null && audio.specialSource != null)
        {
            while (!isSkipped && mySubtitleRequestId == subtitleRequestId && audio.specialSource != null && audio.specialSource.clip != null)
            {
                bool isPaused = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;

                if (isPaused && audio.specialSource.isPlaying)
                {
                    audio.specialSource.Pause();
                }
                else if (!isPaused && !audio.specialSource.isPlaying && audio.specialSource.time > 0 && audio.specialSource.time < audio.specialSource.clip.length)
                {
                    audio.specialSource.UnPause();
                }

                if (!isPaused && !audio.specialSource.isPlaying)
                {
                    break;
                }

                yield return null;
            }
        }

        if (!isSkipped && mySubtitleRequestId == subtitleRequestId)
            yield return StartCoroutine(WaitForSecondsWhilePaused(subtitleHideDelay));

        if (!isSkipped && mySubtitleRequestId == subtitleRequestId)
            HideSubtitleImmediate();
    }

    private void PlayVoice(int index)
    {
        if (AudioManager.Instance == null || narratorVoices == null)
            return;

        if (index < 0 || index >= narratorVoices.Length || narratorVoices[index] == null)
            return;

        AudioManager.Instance.PlaySpecial(narratorVoices[index]);
    }

    private void StopNarratorVoice()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio != null && audio.specialSource != null)
            audio.specialSource.Stop();
    }

    private void HideSubtitleImmediate()
    {
        StopNarratorVoice();

        if (subtitleText != null)
        {
            subtitleText.alpha = 1f;
            subtitleText.text = "";
        }

        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
    }

    private IEnumerator FadeOutLine()
    {
        if (subtitleText == null)
            yield break;

        float duration = Mathf.Max(0.01f, fadeOutDuration);
        float startAlpha = subtitleText.alpha;
        float time = 0f;

        while (time < duration)
        {
            if (isSkipped)
                yield break;

            bool isPaused = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;
            if (!isPaused)
            {
                time += Time.deltaTime;
                subtitleText.alpha = Mathf.Lerp(startAlpha, 0f, time / duration);
            }

            yield return null;
        }

        subtitleText.alpha = 1f;
        subtitleText.text = "";

        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
    }

    private IEnumerator ScaleTeleport(Vector3 targetScale, float duration)
    {
        if (teleport == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            teleport.transform.localScale = targetScale;
            yield break;
        }

        Vector3 startScale = teleport.transform.localScale;
        float time = 0f;

        while (time < duration)
        {
            if (isSkipped)
                yield break;

            bool isPaused = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;
            if (!isPaused)
            {
                time += Time.deltaTime;
                teleport.transform.localScale = Vector3.Lerp(startScale, targetScale, time / duration);
            }

            yield return null;
        }

        teleport.transform.localScale = targetScale;
    }

    private IEnumerator MovePlayerToPoint(GameObject playerObj, Vector3 targetPos, float speed)
    {
        if (playerObj == null)
        {
            yield break;
        }

        PlayerManager playerManager = playerObj.GetComponent<PlayerManager>();
        NavMeshAgent agent = playerObj.GetComponent<NavMeshAgent>();

        targetPos.y = playerObj.transform.position.y;
        SetPlayerWalk(playerManager, 1f);

        if (agent != null)
        {
            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            agent.isStopped = false;
            agent.speed = speed;
            agent.SetDestination(targetPos);

            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                if (isSkipped)
                    yield break;

                bool isPaused = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;
                if (isPaused)
                {
                    if (!agent.isStopped) agent.isStopped = true;
                    SetPlayerWalk(playerManager, 0f);
                }
                else
                {
                    if (agent.isStopped) agent.isStopped = false;
                    SetPlayerWalk(playerManager, agent.velocity.magnitude);
                }

                yield return null;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }
        else
        {
            while (Vector3.Distance(playerObj.transform.position, targetPos) > 0.05f)
            {
                if (isSkipped)
                    yield break;

                bool isPaused = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;
                if (!isPaused)
                {
                    Vector3 direction = targetPos - playerObj.transform.position;
                    direction.y = 0f;

                    if (direction != Vector3.zero)
                    {
                        direction.Normalize();
                        playerObj.transform.position += direction * speed * Time.deltaTime;

                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, targetRotation, Time.deltaTime * 5f);
                    }
                }

                yield return null;
            }

            playerObj.transform.position = new Vector3(targetPos.x, playerObj.transform.position.y, targetPos.z);
        }

        SetPlayerWalk(playerManager, 0f);
    }

    private void SetPlayerWalk(PlayerManager playerManager, float value)
    {
        if (playerManager != null &&
            playerManager.playerAnimator != null &&
            playerManager.playerAnimator.playerAnimator != null)
        {
            playerManager.playerAnimator.playerAnimator.SetFloat("Walk", value);
        }
    }

    private void SetCameraToPoint(int index)
    {
        if (!IsValidCameraPoint(index))
        {
            return;
        }

        StartCoroutine(ShowBlackPanelRoutine());

        Camera.main.transform.position = transCameraCutSceneList[index].position;
        Camera.main.transform.rotation = transCameraCutSceneList[index].rotation;
    }

    private IEnumerator MoveCameraToPoint(int index)
    {
        if (!IsValidCameraPoint(index))
        {
            yield break;
        }

        Transform cameraTransform = Camera.main.transform;
        Transform target = transCameraCutSceneList[index];

        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;

        if (cameraMoveTime <= 0f)
        {
            cameraTransform.position = target.position;
            cameraTransform.rotation = target.rotation;
            yield break;
        }

        float time = 0f;

        while (time < cameraMoveTime)
        {
            if (isSkipped)
                yield break;

            bool isPaused = SettingManager.Instance != null && SettingManager.Instance.IsSettingBlockingInput;
            if (!isPaused)
            {
                time += Time.deltaTime;
                float percent = Mathf.Clamp01(time / cameraMoveTime);

                cameraTransform.position = Vector3.Lerp(startPosition, target.position, percent);
                cameraTransform.rotation = Quaternion.Slerp(startRotation, target.rotation, percent);
            }

            yield return null;
        }

        cameraTransform.position = target.position;
        cameraTransform.rotation = target.rotation;
    }

    private bool IsValidCameraPoint(int index)
    {
        return transCameraCutSceneList != null &&
               index >= 0 &&
               index < transCameraCutSceneList.Length &&
               transCameraCutSceneList[index] != null &&
               Camera.main != null;
    }

    private IEnumerator ShowBlackPanelRoutine()
    {
        if (blackPanel == null)
        {
            yield break;
        }

        blackPanel.SetActive(true);
        yield return StartCoroutine(WaitForSecondsWhilePaused(1.6f));
        blackPanel.SetActive(false);
    }

    private void StopPlayerMovement()
    {
        if (player == null)
            return;

        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetPlayerWalk(player.GetComponent<PlayerManager>(), 0f);
    }

    public void FindPlayerWinner()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
}