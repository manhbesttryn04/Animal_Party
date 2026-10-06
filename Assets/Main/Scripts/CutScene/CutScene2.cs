using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CutScene2 : MonoBehaviour
{
    [Header("References")]
    public ShipPatrol ship;
    public SetUpPlayerCutScene2 set;
    public Camera cam;
    public GameObject teleport;

    [Header("CutScene Points")]
    public List<Transform> transVideos;

    [Header("Settings")]
    public float moveSpeed = 3f;
    public float rotateSpeed = 5f;

    [Header("Ship Stop Position")]
    public Vector3 shipStopPosition = new Vector3(-42.9f, 0.2f, 16f);
    public float shipStopDistance = 0.5f;

    [Header("Panels")]
    public GameObject blackPanel;
    public GameObject blackClosePanel;
    public GameObject nameMapPanel;

    [Header("--- SUBTITLE ---")]
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

    [Tooltip("Button index dùng để skip bằng tay cầm.\nLegacy Input thường dùng Button 1 cho Xbox B / PlayStation Circle.")]
    [Range(0, 19)]
    [SerializeField] private int controllerSkipButtonIndex = 1;

    [Header("--- SKIP DELAY ---")]
    [Tooltip("Số giây từ lúc cutscene bắt đầu trước khi cho phép skip.")]
    [Min(0f)]
    [SerializeField] private float skipEnableDelay = 2f;

    [Header("--- GRADIENT PRESET ---")]
    [Tooltip("NormalGradient: trái #5C2E00, phải #1A0A00")]
    public TMP_ColorGradient normalGradient;

    [Tooltip("FinalGradient: 4 góc đều #7A0000")]
    public TMP_ColorGradient finalGradient;

    [Header("--- OUTLINE COLOR ---")]
    public Color normalOutlineColor = new Color(1f, 1f, 1f, 0.2f);
    public Color finalOutlineColor = new Color(1f, 1f, 1f, 0.31f);

    [Header("--- NARRATOR VOICE ---")]
    [Tooltip("Kéo file voice vào đây theo đúng thứ tự câu")]
    public AudioClip[] narratorVoices;

    [Header("--- TIMING & SPEED ---")]
    public float fadeOutDuration = 0.4f;
    public float betweenLineFade = 0.5f;

    [Tooltip("Sau khi narrator thực sự nói xong, chờ thêm thời gian này rồi tắt subtitle.")]
    public float subtitleHideDelay = 0.2f;

    [Tooltip("Tốc độ chạy chữ (số ký tự trên mỗi giây).")]
    [SerializeField] private float subtitleSpeed = 40f;

    [Tooltip("Độ trễ thời gian (giây) để chữ chạy nhanh hơn voice một chút.")]
    [SerializeField] private float textSpeedOffset = 0.2f;

    [Header("--- END AUDIO FADE ---")]
    public float endAudioFadeTime = 4.5f;

    private bool isSkipped;
    private bool canSkip;

    private int subtitleRequestId;
    private bool waitSkipReleaseAfterSetting;
    private bool previousHasController;
    private bool skipHintStateInitialized;

    private readonly string[] storyLines =
    {
        "Welcome to Party Land — where the fun never stops!",
        "The wildest party on the island is about to begin!",
        "Roll the dice — your fate is in the hands of luck!",
        "Survive the craziest mini-games",
        "33 steps or a bag full of Pirate Coins — first one wins!",
        "No cheating, no crying — just pure chaotic fun!",
        "Two players. One island. Who will claim victory?"
    };

    private void Start()
    {
        SetupAudio();
        SetupSetting();
        SetupUI();

        canSkip = false;
        UpdateSkipHintText(true);

        if (subtitlePanel != null)
            subtitlePanel.SetActive(false);

        if (subtitleText != null)
        {
            subtitleText.text = "";
            subtitleText.alpha = 1f;
        }

        if (skipHintObject != null)
            skipHintObject.SetActive(false);

        StartCoroutine(CutScene());
        StartCoroutine(CheckShipStop());
        StartCoroutine(EnableSkipAfterDelay());
        StartCoroutine(ShowSkipHint());
    }

    private void SetupAudio()
    {
        var audio = AudioManager.Instance;
        if (audio != null)
        {
            audio.SetupMainGameAudio();
            audio.PlayMusic(audio.musicCutScene2Clip);
        }
    }
    public void SetupGraphic()
    {
        var graphic = GraphicManager.Instance;
        if (graphic != null)
        {
            graphic.ResetVignette();
        }
    }

    private void SetupSetting()
    {
        var setting = SettingManager.Instance;
        if (setting != null)
        {
            setting.enableSettingMusic = false;
            setting.ResetSetting();
            setting.canOpenSettingByEsc = false;
            setting.canOpenSettingByController = false;
        }
    }

    private void SetupUI()
    {
        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.isShowKeyBoard = false;
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
        bool controllerSkipDown = IsControllerSkipDown();

        if (keyboardSkipDown || controllerSkipDown)
        {
            SkipCutscene();
        }
    }

    private IEnumerator EnableSkipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(skipEnableDelay);

        if (isSkipped)
            yield break;

        canSkip = true;
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

    private bool IsControllerSkipDown()
    {
        ControllerManager controller = ControllerManager.Instance;
        if (controller == null) return false;

        bool console1Skip = controller.IsConsole1Connected() && controller.GetConsoleButtonDown(1, controllerSkipButtonIndex);
        bool console2Skip = controller.IsConsole2Connected() && controller.GetConsoleButtonDown(2, controllerSkipButtonIndex);

        return console1Skip || console2Skip;
    }

    private bool IsControllerSkipHeld()
    {
        ControllerManager controller = ControllerManager.Instance;
        if (controller == null) return false;

        bool console1SkipHeld = controller.IsConsole1Connected() && controller.GetConsoleButton(1, controllerSkipButtonIndex);
        bool console2SkipHeld = controller.IsConsole2Connected() && controller.GetConsoleButton(2, controllerSkipButtonIndex);

        return console1SkipHeld || console2SkipHeld;
    }

    private void SkipCutscene()
    {
        if (isSkipped || !canSkip)
            return;

        isSkipped = true;

        StopAllCoroutines();
        StopNarratorVoice();

        var setting = SettingManager.Instance;
        if (setting != null)
        {
            setting.ResetEscSetting();
            setting.canOpenSettingByEsc = false;
            setting.canOpenSettingByController = false;
        }

        AudioManager audio = AudioManager.Instance;
        if (audio != null)
            audio.PauseAudio();

        HideSubtitleImmediate();

        if (skipHintObject != null)
            skipHintObject.SetActive(false);

        StartCoroutine(SkipToEnd());
    }

    private IEnumerator SkipToEnd()
    {
        if (blackClosePanel != null)
            blackClosePanel.SetActive(true);

        AudioManager audio = AudioManager.Instance;
        if (audio != null)
        {
            audio.ZeroAllAudio();
            audio.PauseAudio();

            if (audio.specialSource != null)
                audio.specialSource.Stop();
        }

        var maincharacterandmap = MapAndCharacterManager.Instance;
        if (maincharacterandmap != null)
        {
            var map = maincharacterandmap.mainMap;
            if (map != null) map.SetActive(false);

            var characters = maincharacterandmap.mainCharacters;
            if (characters != null) characters.SetActive(false);
        }

        SetupGraphic();

        if (LoadingManager.Instance != null)
        {
            yield return StartCoroutine(LoadingManager.Instance.ShowLoading());
        }

        SceneManager.LoadScene("Main Scene");
    }

    private IEnumerator ShowSkipHint()
    {
        yield return new WaitForSecondsRealtime(skipEnableDelay);

        if (isSkipped)
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

            time += Time.deltaTime;
            skipHintText.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }

        skipHintText.alpha = 1f;
    }

    private IEnumerator CutScene()
    {
        if (!CheckReferences())
            yield break;

        yield return new WaitForSeconds(0.1f);

        StartCoroutine(ShowFirstSubtitleWithDelay(storyLines[0], 0, false, 0.2f));

        yield return StartCoroutine(MoveToTransform(transVideos[0]));

        TeleportToTransform(transVideos[1]);

        if (nameMapPanel != null)
            nameMapPanel.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(ShowSubtitleWithVoice(storyLines[1], 1, false));

        TeleportToTransform(transVideos[2]);

        StartCoroutine(ShowSubtitleWithVoice(storyLines[2], 2, false));

        yield return StartCoroutine(MoveToTransform(transVideos[3]));

        TeleportToTransform(transVideos[4]);

        StartCoroutine(ShowSubtitleWithVoice(storyLines[3], 3, false));

        yield return StartCoroutine(MoveToTransform(transVideos[5]));

        TeleportToTransform(transVideos[6]);

        if (ship != null)
            ship.gameObject.SetActive(false);

        if (set != null)
            set.StartCutScene();

        var audio = AudioManager.Instance;
        if (audio != null)
        {
            audio.PlaySFX(audio.seaGullClip);
        }

        StartCoroutine(ShowSubtitleWithVoice(storyLines[4], 4, false));

        yield return StartCoroutine(MoveToTransform(transVideos[7]));

        if (teleport != null)
            teleport.SetActive(true);

        if (audio != null)
        {
            audio.PlaySFX(audio.llamaVoiceClip);
        }

        TeleportToTransform(transVideos[8]);

        StartCoroutine(ShowSubtitleWithVoice(storyLines[5], 5, false));

        if (nameMapPanel != null)
            nameMapPanel.SetActive(false);

        yield return new WaitForSeconds(3f);

        moveSpeed = 100f;

        if (skipHintObject != null)
            skipHintObject.SetActive(false);

        yield return new WaitForSeconds(2f);

        StartCoroutine(ShowSubtitleWithVoice(storyLines[6], 6, true));

        yield return StartCoroutine(MoveToTransform(transVideos[9]));

        yield return new WaitForSeconds(5f);

        HideSubtitleImmediate();
        StopNarratorVoice();

        if (set != null)
            set.StartMovePlayer();

        if (blackClosePanel != null)
            blackClosePanel.SetActive(true);

        TeleportToTransform(transVideos[10]);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.FadeOutAllAudio(endAudioFadeTime);
        }

        yield return new WaitForSeconds(endAudioFadeTime);

        var setting = SettingManager.Instance;
        if (setting != null)
        {
            setting.canOpenSettingByEsc = false;
            setting.canOpenSettingByController = false;
        }

        var maincharacterandmap = MapAndCharacterManager.Instance;
        if (maincharacterandmap != null)
        {
            var mainmap = maincharacterandmap.mainMap;
            if (mainmap != null) mainmap.SetActive(false);

            var maincharacter = maincharacterandmap.mainCharacters;
            if (maincharacter != null) maincharacter.SetActive(false);
        }

        SetupGraphic();

        if (LoadingManager.Instance != null)
        {
            yield return StartCoroutine(LoadingManager.Instance.ShowLoading());
        }

        SceneManager.LoadScene("Main Scene");
    }

    private IEnumerator ShowFirstSubtitleWithDelay(string line, int voiceIndex, bool isFinal, float delaySeconds)
    {
        if (isSkipped)
            yield break;

        yield return new WaitForSeconds(delaySeconds);

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
            yield return new WaitForSeconds(betweenLineFade);
        }

        if (isSkipped)
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

        // --- HIỆU ỨNG CHẠY CHỮ (TYPEWRITER) ---
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
            yield return new WaitForSeconds(subtitleHideDelay);

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
            subtitlePanel.SetActive(false);
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

            time += Time.deltaTime;
            subtitleText.alpha = Mathf.Lerp(startAlpha, 0f, time / duration);
            yield return null;
        }

        subtitleText.alpha = 1f;
        subtitleText.text = "";

        if (subtitlePanel != null)
            subtitlePanel.SetActive(false);
    }

    private IEnumerator CheckShipStop()
    {
        if (ship == null)
            yield break;

        while (Vector3.Distance(ship.transform.position, shipStopPosition) > shipStopDistance)
        {
            if (isSkipped)
                yield break;

            yield return null;
        }

        ship.isStop = true;
    }

    private IEnumerator MoveToTransform(Transform target)
    {
        if (cam == null || target == null)
            yield break;

        while (Vector3.Distance(cam.transform.position, target.position) > 0.05f ||
               Quaternion.Angle(cam.transform.rotation, target.rotation) > 0.5f)
        {
            if (isSkipped)
                yield break;

            cam.transform.position = Vector3.MoveTowards(cam.transform.position, target.position, moveSpeed * Time.deltaTime);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, target.rotation, rotateSpeed * Time.deltaTime);

            yield return null;
        }

        cam.transform.position = target.position;
        cam.transform.rotation = target.rotation;
    }

    private void TeleportToTransform(Transform target)
    {
        if (cam == null || target == null)
            return;

        StartCoroutine(ShowBlackPanel(2f));

        cam.transform.position = target.position;
        cam.transform.rotation = target.rotation;
    }

    private IEnumerator ShowBlackPanel(float time = 1f)
    {
        if (blackPanel == null)
            yield break;

        blackPanel.SetActive(true);
        yield return new WaitForSeconds(time);
        blackPanel.SetActive(false);
    }

    private bool CheckReferences()
    {
        if (cam == null || transVideos == null || transVideos.Count < 11)
            return false;

        for (int i = 0; i < 11; i++)
        {
            if (transVideos[i] == null)
                return false;
        }

        return true;
    }
}