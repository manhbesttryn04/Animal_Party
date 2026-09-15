using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;

    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Setting Music")]
    public bool enableSettingMusic = true;

    [Range(0f, 1f)]
    [SerializeField] private float settingMusicVolume = 0.15f;

    [Header("UI")]
    public GameObject settingPanel;

    [Header("Graphics")]
    public TMP_Dropdown qualityGraphicDropDown;

    [Header("Resolution")]
    public TMP_Dropdown resolutionDropdown;

    [Header("Current Setting Values")]
    [Range(0f, 1f)]
    public float masterValue = 1f;

    [Range(0f, 1f)]
    public float musicValue = 1f;

    [Range(0f, 1f)]
    public float sfxValue = 1f;

    [Header("Main Menu")]
    public Button backToMainMenuButton;

    // Chỉ ẩn/hiện trực tiếp ba nút. Không tắt GameObject cha
    // để Animator Intro trên cha không bị chạy lại khi bật menu.
    private MainMenuController registeredMainMenuController;
    private Button registeredStartButton;
    private Button registeredSettingButton;
    private Button registeredExitButton;
    private bool mainMenuButtonsHiddenForControllerSetting;

    [Header("Buttons")]
    public Button openSettingButton;
    public int countClick;

    [Header("Guide")]
    public Button openGuideButton;
    public int guideClick;

    public int indexScene;

    private readonly List<Vector2Int> availableResolutions =
        new List<Vector2Int>();

    private readonly Vector2Int[] resolutionPresets =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(1920, 1200),
        new Vector2Int(2560, 1440),
        new Vector2Int(2560, 1600),
        new Vector2Int(3840, 2160)
    };

    [Header("Display Mode")]
    public TMP_Dropdown displayModeDropdown;

    [Header("Windowed Size")]
    [SerializeField] private int windowedWidth = 1280;
    [SerializeField] private int windowedHeight = 720;

    public bool isOpenAudioClick;

    [Header("Open Setting")]
    public bool canOpenSettingByEsc = true;

    public bool isSettingOpen;
    public bool isEscSettingOpen;
    public bool canOpenSettingByController = true;
    public bool isOpenExitButton;

    [Header("Controller Setting Navigation")]
    [Tooltip("Không thêm Background của nút Exit vào danh sách này.")]
    [SerializeField]
    private List<Image> settingItemBackgrounds = new List<Image>();

    [SerializeField] private Color normalItemColor = Color.white;
    [SerializeField] private Color focusedItemColor = Color.cyan;

    [Tooltip("Màu của lựa chọn đang lia trong Dropdown, chưa xác nhận.")]
    [SerializeField]
    private Color dropdownPreviewColor =
        new Color(0.2f, 0.2f, 0.2f, 0.9f);

    [Range(0.01f, 0.5f)]
    [SerializeField] private float sliderControllerStep = 0.05f;

    [Range(0.1f, 1f)]
    [SerializeField] private float controllerInputThreshold = 0.5f;

    [Range(0f, 0.5f)]
    [SerializeField] private float controllerResetThreshold = 0.2f;

    [SerializeField] private string verticalP1Axis = "VerticalP1";
    [SerializeField] private string horizontalP1Axis = "HorizontalP1";
    [SerializeField] private string verticalP2Axis = "VerticalP2";
    [SerializeField] private string horizontalP2Axis = "HorizontalP2";

    private Selectable[] settingItems;
    private int currentSettingIndex;

    private bool canMoveSettingVertical = true;
    private bool canMoveSettingHorizontal = true;

    private bool isControllerDropdownOpen;
    private TMP_Dropdown currentControllerDropdown;
    private int controllerDropdownPreviewValue;
    private int controllerDropdownOriginalValue;

    [Header("Controller Open Setting")]
    [Tooltip("Button 7 thường là nút Menu/Start/3 gạch.")]
    [SerializeField] private int controllerSettingButtonIndex = 7;

    // 0 = không có console
    // 1 = Console 1
    // 2 = Console 2
    private int previousSettingConsole;

    private bool waitControllerSettingButtonRelease;
    private Coroutine closeSettingCoroutine;
    private Coroutine displayApplyCoroutine;

    [Header("Setting Input Lock")]
    [Tooltip("Sau khi mở/đóng Setting phải chờ thời gian này mới được nhận lệnh tiếp theo.")]
    [Min(0f)]
    [SerializeField] private float settingInputCooldown = 1f;

    // Dùng unscaledTime vì khi Setting mở game có thể đang Time.timeScale = 0.
    private float nextSettingInputTime;

    [Header("Pause Game")]
    [SerializeField] private bool pauseGameWhenSettingOpen = true;
    public bool IsSettingBlockingInput
    {
        get
        {
            return isSettingOpen || isEscSettingOpen;
        }
    }

    private void Awake()
    {
        SetupSingleton();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SetupDefaultValue();
        SetupResolutionDropdown();
        SetupDisplayModeDropdown();
        SetupControllerSettingItems();
        AddListeners();
        ApplySettings();
        ResetSetting();
        SetupBackToMainMenuButton();
        isOpenAudioClick = true;
    }

    private void SetupBackToMainMenuButton()
    {
        if (backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.AddListener(OnClickBackToMainMenu);
        }
    }

    private void Update()
    {
        HandleControllerConnectionState();

        int activeConsole = GetActiveSettingConsole();

        // Không nhận thêm lệnh trong lúc animation mở/đóng.
        if (IsSettingPanelTransitioning())
        {
            return;
        }

        // Chỉ cho tay cầm mở Setting ở scene được phép.
        if (activeConsole != 0 &&
            canOpenSettingByController)
        {
            HandleControllerSettingButton();
        }

        // ESC là bàn phím, vẫn hoạt động dù có tay cầm đang cắm.
        if (canOpenSettingByEsc &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingByEsc();
        }


        if (!IsAnySettingPanelOpen())
            return;

        // Không có tay cầm thì sử dụng chuột.
        if (activeConsole == 0)
            return;

        HandleControllerSettingNavigation();
    }

    // ==================================================
    // DEFAULT VALUES
    // ==================================================

    private void SetupDefaultValue()
    {
        masterValue = 1f;
        musicValue = 1f;
        sfxValue = 1f;

        if (masterSlider != null)
        {
            masterSlider.minValue = 0f;
            masterSlider.maxValue = 1f;
            masterSlider.value = masterValue;
        }

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = musicValue;
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = sfxValue;
        }

        if (qualityGraphicDropDown != null &&
            VolumeManager.Instance != null)
        {
            qualityGraphicDropDown.SetValueWithoutNotify(
                VolumeManager.Instance.GetCurrentQuality()
            );

            qualityGraphicDropDown.RefreshShownValue();
        }
    }

    // ==================================================
    // RESOLUTION
    // ==================================================

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        resolutionDropdown.ClearOptions();
        availableResolutions.Clear();

        Resolution[] screenResolutions = Screen.resolutions;

        foreach (Vector2Int preset in resolutionPresets)
        {
            if (IsResolutionSupported(
                    preset.x,
                    preset.y,
                    screenResolutions))
            {
                availableResolutions.Add(preset);
            }
        }

        Vector2Int fullHD = new Vector2Int(1920, 1080);

        if (!availableResolutions.Contains(fullHD))
        {
            availableResolutions.Insert(0, fullHD);
        }

        List<string> resolutionOptions = new List<string>();

        foreach (Vector2Int resolution in availableResolutions)
        {
            resolutionOptions.Add(
                resolution.x + " × " + resolution.y
            );
        }

        resolutionDropdown.AddOptions(resolutionOptions);

        int selectedIndex = FindBestResolutionIndex();

        resolutionDropdown.SetValueWithoutNotify(selectedIndex);
        resolutionDropdown.RefreshShownValue();
    }

    private bool IsResolutionSupported(
        int width,
        int height,
        Resolution[] screenResolutions)
    {
        foreach (Resolution resolution in screenResolutions)
        {
            if (resolution.width == width &&
                resolution.height == height)
            {
                return true;
            }
        }

        return false;
    }

    private int FindBestResolutionIndex()
    {
        if (availableResolutions.Count == 0)
        {
            return 0;
        }

        int monitorWidth = Display.main.systemWidth;
        int monitorHeight = Display.main.systemHeight;

        for (int i = 0;
             i < availableResolutions.Count;
             i++)
        {
            Vector2Int resolution = availableResolutions[i];

            if (resolution.x == monitorWidth &&
                resolution.y == monitorHeight)
            {
                return i;
            }
        }

        for (int i = availableResolutions.Count - 1;
             i >= 0;
             i--)
        {
            Vector2Int resolution = availableResolutions[i];

            if (resolution.x <= monitorWidth &&
                resolution.y <= monitorHeight)
            {
                return i;
            }
        }

        return FindResolutionIndex(1920, 1080);
    }

    private int FindResolutionIndex(int width, int height)
    {
        for (int i = 0;
             i < availableResolutions.Count;
             i++)
        {
            Vector2Int resolution = availableResolutions[i];

            if (resolution.x == width &&
                resolution.y == height)
            {
                return i;
            }
        }

        return 0;
    }

    // ==================================================
    // LISTENERS
    // ==================================================

    private void AddListeners()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(
                SetMasterVolume
            );
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(
                SetMusicVolume
            );
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(
                SetSFXVolume
            );
        }

        if (qualityGraphicDropDown != null)
        {
            qualityGraphicDropDown.onValueChanged.AddListener(
                SetGraphicQuality
            );
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(
                SetResolution
            );
        }

        if (displayModeDropdown != null)
        {
            displayModeDropdown.onValueChanged.AddListener(
                SetDisplayMode
            );
        }

        if (openSettingButton != null)
        {
            openSettingButton.onClick.AddListener(
                ToggleSetting
            );
        }

    }

    private void RemoveListeners()
    {
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(
                SetMasterVolume
            );
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(
                SetMusicVolume
            );
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(
                SetSFXVolume
            );
        }

        if (qualityGraphicDropDown != null)
        {
            qualityGraphicDropDown.onValueChanged.RemoveListener(
                SetGraphicQuality
            );
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(
                SetResolution
            );
        }

        if (displayModeDropdown != null)
        {
            displayModeDropdown.onValueChanged.RemoveListener(
                SetDisplayMode
            );
        }

        if (openSettingButton != null)
        {
            openSettingButton.onClick.RemoveListener(
                ToggleSetting
            );
        }



        if (backToMainMenuButton != null)
        {
            backToMainMenuButton.onClick.RemoveListener(
                OnClickBackToMainMenu
            );
        }
    }

    // ==================================================
    // DISPLAY MODE
    // ==================================================

    private void SetupDisplayModeDropdown()
    {
        if (displayModeDropdown == null)
        {
            return;
        }

        displayModeDropdown.ClearOptions();

        displayModeDropdown.AddOptions(
            new List<string>
            {
                "Fullscreen",
                "Borderless",
                "Windowed"
            }
        );

        int selectedIndex;

        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                selectedIndex = 0;
                break;

            case FullScreenMode.FullScreenWindow:
            case FullScreenMode.MaximizedWindow:
                selectedIndex = 1;
                break;

            default:
                selectedIndex = 2;
                break;
        }

        displayModeDropdown.SetValueWithoutNotify(selectedIndex);
        displayModeDropdown.RefreshShownValue();
        UpdateResolutionDropdownInteractable(selectedIndex);
    }

    public void SetDisplayMode(int index)
    {
        index = Mathf.Clamp(index, 0, 2);

        if (isOpenAudioClick &&
            AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(
                AudioManager.Instance.clickButton
            );
        }

        UpdateResolutionDropdownInteractable(index);
        ApplyDisplaySettings(index);
    }

    private void UpdateResolutionDropdownInteractable(
        int displayModeIndex)
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        // Borderless luôn bằng độ phân giải desktop.
        resolutionDropdown.interactable =
            displayModeIndex != 1;
    }

    public void SetResolution(int index)
    {
        if (availableResolutions.Count == 0)
        {
            return;
        }

        index = Mathf.Clamp(
            index,
            0,
            availableResolutions.Count - 1
        );

        if (resolutionDropdown != null &&
            resolutionDropdown.value != index)
        {
            resolutionDropdown.SetValueWithoutNotify(index);
            resolutionDropdown.RefreshShownValue();
        }

        int displayModeIndex = GetSelectedDisplayModeIndex();

        // Borderless không cho dùng độ phân giải riêng.
        if (displayModeIndex == 1)
        {
            ApplyDisplaySettings(1);
            return;
        }

        if (isOpenAudioClick &&
            AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(
                AudioManager.Instance.clickButton
            );
        }

        ApplyDisplaySettings(displayModeIndex);
    }

    private int GetSelectedDisplayModeIndex()
    {
        if (displayModeDropdown != null)
        {
            return Mathf.Clamp(
                displayModeDropdown.value,
                0,
                2
            );
        }

        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return 0;

            case FullScreenMode.FullScreenWindow:
            case FullScreenMode.MaximizedWindow:
                return 1;

            default:
                return 2;
        }
    }

    private Vector2Int GetSelectedResolution()
    {
        if (availableResolutions.Count == 0)
        {
            return new Vector2Int(
                Screen.width,
                Screen.height
            );
        }

        int index = 0;

        if (resolutionDropdown != null)
        {
            index = Mathf.Clamp(
                resolutionDropdown.value,
                0,
                availableResolutions.Count - 1
            );
        }

        return availableResolutions[index];
    }

    private void ApplyDisplaySettings(int displayModeIndex)
    {
        if (displayApplyCoroutine != null)
        {
            StopCoroutine(displayApplyCoroutine);
        }

        displayApplyCoroutine = StartCoroutine(
            ApplyDisplaySettingsRoutine(displayModeIndex)
        );
    }

    private IEnumerator ApplyDisplaySettingsRoutine(int displayModeIndex)
    {
        displayModeIndex = Mathf.Clamp(displayModeIndex, 0, 2);

        Vector2Int selectedResolution = GetSelectedResolution();

        // =====================================================
        // BORDERLESS
        // Borderless luôn sử dụng độ phân giải hiện tại của Desktop.
        // =====================================================
        if (displayModeIndex == 1)
        {
            Screen.SetResolution(
                Display.main.systemWidth,
                Display.main.systemHeight,
                FullScreenMode.FullScreenWindow
            );

            yield return null;
            yield return new WaitForEndOfFrame();

            Debug.Log(
                $"Borderless: {Screen.width} x {Screen.height}"
            );

            displayApplyCoroutine = null;
            yield break;
        }

        // =====================================================
        // WINDOWED
        // =====================================================
        if (displayModeIndex == 2)
        {
            windowedWidth = selectedResolution.x;
            windowedHeight = selectedResolution.y;

            Screen.SetResolution(
                windowedWidth,
                windowedHeight,
                FullScreenMode.Windowed
            );

            yield return null;
            yield return new WaitForEndOfFrame();
            displayApplyCoroutine = null;
            yield break;
        }

        // =====================================================
        // EXCLUSIVE FULLSCREEN
        //
        // Đổi resolution trước, sau đó chuyển sang Exclusive.
        // Cách này giúp Unity thoát khỏi trạng thái Borderless/
        // Windowed trước khi yêu cầu display mode mới.
        // =====================================================

        // Bước 1: Thoát fullscreen hiện tại.
        Screen.SetResolution(
            selectedResolution.x,
            selectedResolution.y,
            FullScreenMode.Windowed
        );

        yield return null;
        yield return new WaitForEndOfFrame();

        // Bước 2: Yêu cầu đúng độ phân giải ở Exclusive Fullscreen.
        Screen.SetResolution(
            selectedResolution.x,
            selectedResolution.y,
            FullScreenMode.ExclusiveFullScreen
        );

        yield return null;
        yield return new WaitForEndOfFrame();

        // Chờ thêm 1 frame để Windows/Unity hoàn tất việc đổi mode.
        yield return null;

        Debug.Log(
            $"Requested Resolution: " +
            $"{selectedResolution.x} x {selectedResolution.y} | " +
            $"Actual Game Resolution: {Screen.width} x {Screen.height} | " +
            $"Desktop: {Display.main.systemWidth} x " +
            $"{Display.main.systemHeight} | " +
            $"Mode: {Screen.fullScreenMode}"
        );

        displayApplyCoroutine = null;
    }

    // ==================================================
    // AUDIO
    // ==================================================

    public void SetMasterVolume(float value)
    {
        masterValue = Mathf.Clamp01(value);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyMasterSetting(
                masterValue
            );
        }
    }

    public void SetMusicVolume(float value)
    {
        musicValue = Mathf.Clamp01(value);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyMusicSetting(
                musicValue
            );
        }
    }

    public void SetSFXVolume(float value)
    {
        sfxValue = Mathf.Clamp01(value);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplySFXSetting(
                sfxValue
            );
        }
    }

    // ==================================================
    // GRAPHICS
    // ==================================================

    public void SetGraphicQuality(int value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(
                AudioManager.Instance.clickButton
            );
        }

        value = Mathf.Clamp(value, 0, 2);

        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.SetGraphicsQuality(
                value
            );
        }
    }

    private void ApplySettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyMasterSetting(
                masterValue
            );

            AudioManager.Instance.ApplyMusicSetting(
                musicValue
            );

            AudioManager.Instance.ApplySFXSetting(
                sfxValue
            );
        }

        if (VolumeManager.Instance != null &&
            qualityGraphicDropDown != null)
        {
            VolumeManager.Instance.SetGraphicsQuality(
                qualityGraphicDropDown.value
            );
        }

        // Chỉ gọi một lần. Hàm này tự áp dụng cả mode và resolution.
        ApplyDisplaySettings(
            GetSelectedDisplayModeIndex()
        );
    }

    // ==================================================
    // CONTROLLER SETTING
    // ==================================================

    private void SetupControllerSettingItems()
    {
        settingItems = new Selectable[]
        {
            musicSlider,
            sfxSlider,
            masterSlider,
            qualityGraphicDropDown,
            resolutionDropdown,
            displayModeDropdown,
            backToMainMenuButton
        };

        currentSettingIndex = 0;
        UpdateSettingControllerFocus();
    }

    private bool IsAnySettingPanelOpen()
    {
        return isSettingOpen || isEscSettingOpen;
    }

    private bool HasControllerForSetting()
    {
        return GetActiveSettingConsole() != 0;
    }

    private int GetActiveSettingConsole()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null)
        {
            return 0;
        }

        // Console 1 luôn được ưu tiên.
        if (controller.IsConsole1Connected())
        {
            return 1;
        }

        // Console 2 chỉ điều khiển khi Console 1 mất kết nối.
        if (controller.IsConsole2Connected())
        {
            return 2;
        }

        return 0;
    }

    private void HandleControllerConnectionState()
    {
        int activeConsole = GetActiveSettingConsole();

        bool hasController = activeConsole != 0;
        bool hadController = previousSettingConsole != 0;

        // Vừa cắm tay cầm.
        if (!hadController && hasController)
        {
            if (IsAnySettingPanelOpen())
            {
                // Settings vẫn đang mở và vừa có tay cầm trở lại:
                // ẩn lại cụm Main Menu rồi chuyển focus vào Settings.
                HideMainMenuButtonsForControllerSetting();
                FocusFirstSettingItem();
            }
        }
        // Vừa rút hết tay cầm.
        else if (hadController && !hasController)
        {
            ResetSettingControllerFocus();
            ClearSelectedUI();

            waitControllerSettingButtonRelease = false;

            // Không còn tay cầm nào trong lúc Settings vẫn mở:
            // hiện lại cụm Main Menu để người chơi có thể dùng chuột.
            ShowMainMenuButtonsAfterAllControllersDisconnected();

            /*
             * Không gọi ShowGameCursor() ở đây.
             *
             * ControllerManager sẽ báo trạng thái tay cầm
             * cho CursorManager.
             *
             * CursorManager tự hiện chuột nếu:
             * - Setting đang mở.
             * - Không còn tay cầm.
             */
        }
        // Chuyển quyền từ Console 1 sang Console 2
        // hoặc từ Console 2 về Console 1.
        else if (hasController &&
                 activeConsole != previousSettingConsole)
        {
            waitControllerSettingButtonRelease = false;

            if (IsAnySettingPanelOpen())
            {
                FocusFirstSettingItem();
            }
        }

        previousSettingConsole = activeConsole;
    }

    private float GetSettingVerticalInput()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        int activeConsole = GetActiveSettingConsole();

        if (controller == null ||
            activeConsole == 0)
        {
            return 0f;
        }

        return controller.GetConsoleAxisRaw(
            activeConsole,
            verticalP1Axis,
            verticalP2Axis
        );
    }

    private float GetSettingHorizontalInput()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        int activeConsole = GetActiveSettingConsole();

        if (controller == null ||
            activeConsole == 0)
        {
            return 0f;
        }

        return controller.GetConsoleAxisRaw(
            activeConsole,
            horizontalP1Axis,
            horizontalP2Axis
        );
    }

    private bool GetSettingSubmitDown()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        int activeConsole = GetActiveSettingConsole();

        if (controller == null ||
            activeConsole == 0)
        {
            return false;
        }

        return controller.GetConsoleButtonDown(
            activeConsole,
            0
        );
    }

    private bool GetSettingCancelDown()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        int activeConsole = GetActiveSettingConsole();

        if (controller == null ||
            activeConsole == 0)
        {
            return false;
        }

        // Button 1 thường là B trên Xbox hoặc Circle trên PlayStation.
        return controller.GetConsoleButtonDown(
            activeConsole,
            1
        );
    }

    private bool GetControllerSettingButtonDown()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        int activeConsole = GetActiveSettingConsole();

        if (controller == null ||
            activeConsole == 0)
        {
            return false;
        }

        return controller.GetConsoleButtonDown(
            activeConsole,
            controllerSettingButtonIndex
        );
    }

    private bool GetControllerSettingButtonHeld()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        int activeConsole = GetActiveSettingConsole();

        if (controller == null ||
            activeConsole == 0)
        {
            return false;
        }

        return controller.GetConsoleButton(
            activeConsole,
            controllerSettingButtonIndex
        );
    }

    private void HandleControllerSettingButton()
    {
        if (GetActiveSettingConsole() == 0)
        {
            waitControllerSettingButtonRelease = false;
            return;
        }

        if (waitControllerSettingButtonRelease)
        {
            if (!GetControllerSettingButtonHeld())
            {
                waitControllerSettingButtonRelease = false;
            }

            return;
        }

        if (!GetControllerSettingButtonDown())
        {
            return;
        }

        waitControllerSettingButtonRelease = true;

        // Nút Menu/Start chỉ dùng để mở Setting.
        // Khi Setting đang mở, nhấn lại nút này sẽ không đóng.
        if (!IsAnySettingPanelOpen())
        {
            // 1. Nhá hiệu ứng Pressed trên UI Button (nếu đã gán tham chiếu openSettingButton)
            if (openSettingButton != null)
            {
                StartCoroutine(PulseButtonEffect(openSettingButton));
            }

            // 2. Mở Setting Menu
            OpenControllerSetting();
        }
    }

    /// <summary>
    /// Coroutine kích hoạt trạng thái Pressed UI của Button trong 0.1s
    /// </summary>
    private IEnumerator PulseButtonEffect(Button targetButton)
    {
        if (targetButton == null) yield break;

        PointerEventData pointerData = new PointerEventData(EventSystem.current);

        // Kích hoạt hiệu ứng đè nút UI Down
        ExecuteEvents.Execute(targetButton.gameObject, pointerData, ExecuteEvents.pointerDownHandler);

        // Giữ trạng thái Pressed 0.1s (dùng Realtime phòng trường hợp Pause game)
        yield return new WaitForSecondsRealtime(0.1f);

        // Kích hoạt nhả nút UI Up
        ExecuteEvents.Execute(targetButton.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
    }

    private void HandleControllerSettingNavigation()
    {
        if (settingItems == null ||
            settingItems.Length == 0)
        {
            return;
        }

        // B / Circle: đóng Dropdown trước, sau đó mới đóng toàn bộ Setting.
        if (GetSettingCancelDown())
        {
            if (isControllerDropdownOpen)
            {
                CancelControllerDropdown();
            }
            else
            {
                // Nhá lại hiệu ứng của chính nút Open Setting
                if (openSettingButton != null)
                {
                    StartCoroutine(PulseButtonEffect(openSettingButton));
                }

                CloseControllerSetting();
            }

            return;
        }

        float vertical = GetSettingVerticalInput();
        float horizontal = GetSettingHorizontalInput();

        if (isControllerDropdownOpen)
        {
            HandleOpenedDropdown(vertical);

            if (GetSettingSubmitDown())
            {
                ConfirmControllerDropdown();
            }

            return;
        }

        HandleSettingVertical(vertical);
        HandleSettingHorizontal(horizontal);

        if (GetSettingSubmitDown())
        {
            HandleSettingSubmit();
        }
    }

    private void HandleSettingVertical(float vertical)
    {
        if (Mathf.Abs(vertical) <= controllerResetThreshold)
        {
            canMoveSettingVertical = true;
            return;
        }

        if (!canMoveSettingVertical)
        {
            return;
        }

        if (vertical > controllerInputThreshold)
        {
            MoveToPreviousSettingItem();
            canMoveSettingVertical = false;
        }
        else if (vertical < -controllerInputThreshold)
        {
            MoveToNextSettingItem();
            canMoveSettingVertical = false;
        }
    }

    private void MoveToPreviousSettingItem()
    {
        int startIndex = currentSettingIndex;

        do
        {
            currentSettingIndex--;

            if (currentSettingIndex < 0)
            {
                currentSettingIndex =
                    settingItems.Length - 1;
            }

            if (CanUseSettingItem(
                    settingItems[currentSettingIndex]))
            {
                UpdateSettingControllerFocus();
                PlayControllerMoveSound();
                return;
            }

        } while (currentSettingIndex != startIndex);
    }

    private void MoveToNextSettingItem()
    {
        int startIndex = currentSettingIndex;

        do
        {
            currentSettingIndex++;

            if (currentSettingIndex >= settingItems.Length)
            {
                currentSettingIndex = 0;
            }

            if (CanUseSettingItem(
                    settingItems[currentSettingIndex]))
            {
                UpdateSettingControllerFocus();
                PlayControllerMoveSound();
                return;
            }

        } while (currentSettingIndex != startIndex);
    }

    private bool CanUseSettingItem(Selectable item)
    {
        return item != null &&
               item.gameObject.activeInHierarchy &&
               item.interactable;
    }

    private void HandleSettingHorizontal(float horizontal)
    {
        if (currentSettingIndex < 0 ||
            currentSettingIndex > 2)
        {
            canMoveSettingHorizontal = true;
            return;
        }

        if (Mathf.Abs(horizontal) <=
            controllerResetThreshold)
        {
            canMoveSettingHorizontal = true;
            return;
        }

        if (!canMoveSettingHorizontal)
        {
            return;
        }

        Slider selectedSlider =
            settingItems[currentSettingIndex] as Slider;

        if (selectedSlider == null)
        {
            return;
        }

        float direction =
            horizontal > 0f ? 1f : -1f;

        float newValue =
            selectedSlider.value +
            direction * sliderControllerStep;

        newValue = Mathf.Clamp(
            newValue,
            selectedSlider.minValue,
            selectedSlider.maxValue
        );

        selectedSlider.value = newValue;

        canMoveSettingHorizontal = false;

        PlayControllerMoveSound();
    }

    private void HandleSettingSubmit()
    {
        // Ba mục đầu là Slider.
        if (currentSettingIndex <= 2)
        {
            return;
        }

        Selectable selectedItem =
            settingItems[currentSettingIndex];

        Button button = selectedItem as Button;

        if (button != null)
        {
            button.onClick.Invoke();
            return;
        }

        TMP_Dropdown dropdown =
            selectedItem as TMP_Dropdown;

        if (dropdown == null)
        {
            return;
        }

        OpenControllerDropdown(dropdown);
    }

    private void OpenControllerDropdown(
        TMP_Dropdown dropdown)
    {
        if (dropdown == null ||
            dropdown.options == null ||
            dropdown.options.Count == 0)
        {
            return;
        }

        currentControllerDropdown = dropdown;
        isControllerDropdownOpen = true;

        controllerDropdownOriginalValue = dropdown.value;

        controllerDropdownPreviewValue =
            dropdown.value;

        canMoveSettingVertical = false;
        canMoveSettingHorizontal = true;

        dropdown.Show();

        UpdateDropdownPreviewHighlight();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                dropdown.gameObject
            );
        }

        PlayControllerClickSound();
    }

    private void HandleOpenedDropdown(float vertical)
    {
        if (currentControllerDropdown == null)
        {
            CloseControllerDropdownState();
            return;
        }

        if (Mathf.Abs(vertical) <=
            controllerResetThreshold)
        {
            canMoveSettingVertical = true;
            return;
        }

        if (!canMoveSettingVertical)
        {
            return;
        }

        int optionCount =
            currentControllerDropdown.options.Count;

        if (optionCount <= 0)
        {
            return;
        }

        int newValue =
            controllerDropdownPreviewValue;

        if (vertical > controllerInputThreshold)
        {
            newValue--;

            if (newValue < 0)
            {
                newValue = optionCount - 1;
            }
        }
        else if (vertical <
                 -controllerInputThreshold)
        {
            newValue++;

            if (newValue >= optionCount)
            {
                newValue = 0;
            }
        }
        else
        {
            return;
        }

        controllerDropdownPreviewValue = newValue;

        currentControllerDropdown.SetValueWithoutNotify(
            controllerDropdownPreviewValue
        );

        currentControllerDropdown.RefreshShownValue();

        UpdateDropdownPreviewHighlight();

        canMoveSettingVertical = false;

        PlayControllerMoveSound();
    }

    private void UpdateDropdownPreviewHighlight()
    {
        if (currentControllerDropdown == null)
        {
            return;
        }

        Toggle[] allToggles =
            currentControllerDropdown.transform.root
                .GetComponentsInChildren<Toggle>(true);

        List<Toggle> optionToggles =
            new List<Toggle>();

        foreach (Toggle toggle in allToggles)
        {
            if (toggle == null ||
                !toggle.gameObject.activeInHierarchy)
            {
                continue;
            }

            Transform current = toggle.transform;
            bool belongsToDropdownList = false;

            while (current != null)
            {
                if (current.name == "Dropdown List")
                {
                    belongsToDropdownList = true;
                    break;
                }

                current = current.parent;
            }

            if (belongsToDropdownList)
            {
                optionToggles.Add(toggle);
            }
        }

        for (int i = 0;
             i < optionToggles.Count;
             i++)
        {
            Toggle toggle = optionToggles[i];
            ColorBlock colors = toggle.colors;

            bool isPreview =
                i == controllerDropdownPreviewValue;

            Color normalColor =
                isPreview
                    ? dropdownPreviewColor
                    : Color.white;

            colors.normalColor = normalColor;
            colors.selectedColor = normalColor;
            colors.highlightedColor = normalColor;
            colors.pressedColor = normalColor;

            toggle.colors = colors;

            if (toggle.targetGraphic != null)
            {
                toggle.targetGraphic.color =
                    normalColor;
            }
        }
    }

    private void ConfirmControllerDropdown()
    {
        if (currentControllerDropdown == null)
        {
            CloseControllerDropdownState();
            return;
        }

        TMP_Dropdown dropdown =
            currentControllerDropdown;

        dropdown.SetValueWithoutNotify(
            controllerDropdownPreviewValue
        );

        dropdown.RefreshShownValue();

        dropdown.onValueChanged.Invoke(
            controllerDropdownPreviewValue
        );

        dropdown.Hide();

        isControllerDropdownOpen = false;
        currentControllerDropdown = null;
        controllerDropdownPreviewValue = 0;
        controllerDropdownOriginalValue = 0;

        canMoveSettingVertical = false;
        canMoveSettingHorizontal = true;

        UpdateSettingControllerFocus();
        PlayControllerClickSound();
    }

    private void CancelControllerDropdown()
    {
        if (currentControllerDropdown == null)
        {
            CloseControllerDropdownState();
            return;
        }

        // Trả lại giá trị ban đầu trước khi người chơi mở Dropdown.
        currentControllerDropdown.SetValueWithoutNotify(
            controllerDropdownOriginalValue
        );

        currentControllerDropdown.RefreshShownValue();
        currentControllerDropdown.Hide();

        isControllerDropdownOpen = false;
        currentControllerDropdown = null;
        controllerDropdownPreviewValue = 0;
        controllerDropdownOriginalValue = 0;

        canMoveSettingVertical = false;
        canMoveSettingHorizontal = true;

        UpdateSettingControllerFocus();
        PlayControllerClickSound();
    }

    private void CloseControllerDropdownState()
    {
        isControllerDropdownOpen = false;
        currentControllerDropdown = null;
        controllerDropdownPreviewValue = 0;
        controllerDropdownOriginalValue = 0;

        canMoveSettingVertical = false;
        canMoveSettingHorizontal = true;
    }

    private void FocusFirstSettingItem()
    {
        if (!HasControllerForSetting())
        {
            return;
        }

        if (settingItems == null ||
            settingItems.Length == 0)
        {
            SetupControllerSettingItems();
        }

        currentSettingIndex = 0;

        isControllerDropdownOpen = false;
        currentControllerDropdown = null;

        canMoveSettingVertical = false;
        canMoveSettingHorizontal = true;

        UpdateSettingControllerFocus();
    }

    private void UpdateSettingControllerFocus()
    {
        if (settingItems == null ||
            settingItems.Length == 0)
        {
            return;
        }

        currentSettingIndex = Mathf.Clamp(
            currentSettingIndex,
            0,
            settingItems.Length - 1
        );

        for (int i = 0;
             i < settingItemBackgrounds.Count;
             i++)
        {
            Image background =
                settingItemBackgrounds[i];

            if (background == null)
            {
                continue;
            }

            background.color =
                i == currentSettingIndex
                    ? focusedItemColor
                    : normalItemColor;
        }

        Selectable selectedItem =
            settingItems[currentSettingIndex];

        if (!CanUseSettingItem(selectedItem))
        {
            return;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                null
            );

            EventSystem.current.SetSelectedGameObject(
                selectedItem.gameObject
            );
        }
    }

    private void ResetSettingControllerFocus()
    {
        if (currentControllerDropdown != null)
        {
            currentControllerDropdown.Hide();
        }

        isControllerDropdownOpen = false;
        currentControllerDropdown = null;

        canMoveSettingVertical = true;
        canMoveSettingHorizontal = true;

        for (int i = 0;
             i < settingItemBackgrounds.Count;
             i++)
        {
            if (settingItemBackgrounds[i] != null)
            {
                settingItemBackgrounds[i].color =
                    normalItemColor;
            }
        }
    }

    private void PlayControllerMoveSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(
                AudioManager.Instance.movechooseItemClip
            );
        }
    }

    private void PlayControllerClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(
                AudioManager.Instance.clickButton
            );
        }
    }

    // ==================================================
    // SETTING PANEL
    // ==================================================

    /// <summary>
    /// Kiểm tra xem Setting có được phép nhận thêm lệnh mở/đóng hay không.
    /// Có 2 lớp khóa:
    /// 1. UI đang chạy animation Open / Close.
    /// 2. Chưa đủ thời gian cooldown sau lần nhấn hợp lệ trước đó.
    /// </summary>
    private bool CanUseSettingInput()
    {
        if (IsSettingPanelTransitioning())
        {
            return false;
        }

        if (Time.unscaledTime < nextSettingInputTime)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Khóa lệnh mở/đóng Setting trong settingInputCooldown giây.
    /// Dùng unscaledTime nên vẫn hoạt động khi game Pause.
    /// </summary>
    private void LockSettingInput()
    {
        nextSettingInputTime =
            Time.unscaledTime + Mathf.Max(0f, settingInputCooldown);
    }

    private void OpenControllerSetting()
    {
        if (!CanUseSettingInput() ||
            IsAnySettingPanelOpen())
        {
            return;
        }

        // Chỉ khóa và phát âm thanh khi lệnh mở thực sự hợp lệ.
        LockSettingInput();
        PlaySettingClickSound();

        isSettingOpen = true;
        isEscSettingOpen = false;
        countClick = 1;
        ApplySettingMusicState(true);

        SetSettingPanelActive(true);
        SetExitButtonActive(isOpenExitButton);

        // Báo Setting đang mở cho CursorManager.
        SetCursorSettingState(true);

        HideMainMenuButtonsForControllerSetting();

        FocusFirstSettingItem();
    }

    private void CloseControllerSetting()
    {
        if (!CanUseSettingInput() ||
            !IsAnySettingPanelOpen() ||
            closeSettingCoroutine != null)
        {
            return;
        }

        // Sau khi mở Setting, phải chờ đủ cooldown mới được đóng.
        LockSettingInput();
        PlaySettingClickSound();
        CloseSettingPanel();
    }

    public void ToggleSetting()
    {
        // Main Menu gọi hàm này.
        // Nếu player spam nút khi chưa đủ 1 giây hoặc animation đang chạy,
        // lệnh bị bỏ qua và KHÔNG phát tiếng click.
        if (!CanUseSettingInput())
        {
            return;
        }

        bool isOpen = IsAnySettingPanelOpen();

        // Bảo vệ thêm trường hợp coroutine đóng đang chạy.
        if (isOpen && closeSettingCoroutine != null)
        {
            return;
        }

        LockSettingInput();
        PlaySettingClickSound();

        if (isOpen)
        {
            CloseSettingPanel();
        }
        else
        {
            OpenSettingPanel();
        }
    }

    public void ToggleSettingByEsc()
    {
        if (!CanUseSettingInput())
        {
            return;
        }

        bool isOpen = IsAnySettingPanelOpen();

        if (isOpen && closeSettingCoroutine != null)
        {
            return;
        }

        LockSettingInput();
        PlaySettingClickSound();

        if (isOpen)
        {
            CloseSettingPanel();
        }
        else
        {
            OpenEscSetting();
        }
    }

    private void OpenSettingPanel()
    {
        if (IsSettingPanelTransitioning() ||
            IsAnySettingPanelOpen())
        {
            return;
        }

        isSettingOpen = true;
        isEscSettingOpen = false;
        countClick = 1;

        SetSettingPanelActive(true);
        SetExitButtonActive(isOpenExitButton);
        SetCursorSettingState(true);
        ApplySettingMusicState(true);
        HideMainMenuButtonsForControllerSetting();

        if (HasControllerForSetting())
        {
            FocusFirstSettingItem();
        }
        else
        {
            ClearSelectedUI();
        }
    }

    private void CloseSettingPanel()
    {
        if (IsSettingPanelTransitioning() ||
            !IsAnySettingPanelOpen() ||
            closeSettingCoroutine != null)
        {
            return;
        }

        closeSettingCoroutine =
            StartCoroutine(
                CloseSettingPanelRoutine()
            );
    }

    private IEnumerator CloseSettingPanelRoutine()
    {
        ResetSettingControllerFocus();
        SetExitButtonActive(false);
        ClearSelectedUI();

        SetSettingPanelActive(false);

        // Trong lúc animation Close chạy,
        // Settings vẫn được xem là đang mở.
        while (IsSettingPanelTransitioning())
        {
            yield return null;
        }

        isSettingOpen = false;
        isEscSettingOpen = false;
        countClick = 0;
        ApplySettingMusicState(false);

        waitControllerSettingButtonRelease = false;

        SetCursorSettingState(false);

        RestoreMainMenuButtonsAfterControllerSetting();

        closeSettingCoroutine = null;
    }

    private void OpenEscSetting()
    {
        if (IsSettingPanelTransitioning() ||
            IsAnySettingPanelOpen())
        {
            return;
        }

        isEscSettingOpen = true;
        isSettingOpen = false;
        countClick = 0;

        SetSettingPanelActive(true);
        SetExitButtonActive(isOpenExitButton);
        SetCursorSettingState(true);
        ApplySettingMusicState(true);

        HideMainMenuButtonsForControllerSetting();

        if (GetActiveSettingConsole() != 0)
        {
            FocusFirstSettingItem();
        }
        else
        {
            ClearSelectedUI();
        }
    }

    public void ResetEscSetting()
    {
        // Nếu đây là nút đóng Setting trên UI thì cũng phải tuân theo
        // khóa 1 giây để không thể mở rồi đóng ngay trước lúc game Pause.
        if (!CanUseSettingInput() ||
            !IsAnySettingPanelOpen() ||
            closeSettingCoroutine != null)
        {
            return;
        }

        LockSettingInput();
        CloseSettingPanel();
    }

    public void ResetSetting()
    {
        if (closeSettingCoroutine != null)
        {
            StopCoroutine(closeSettingCoroutine);
            closeSettingCoroutine = null;
        }

        isSettingOpen = false;
        isEscSettingOpen = false;
        countClick = 0;
        ApplySettingMusicState(false);

        // ResetSetting là force reset khi đổi scene / khởi tạo,
        // vì vậy xóa luôn thời gian khóa.
        nextSettingInputTime = 0f;

        waitControllerSettingButtonRelease = false;

        ResetSettingControllerFocus();
        SetExitButtonActive(false);
        ClearSelectedUI();
        SetCursorSettingState(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ForceHideSettingPanel();
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        RestoreMainMenuButtonsAfterControllerSetting();
    }

    private bool IsSettingPanelTransitioning()
    {
        return UIManager.Instance != null &&
               UIManager.Instance.IsSettingPanelTransitioning;
    }

    private void SetCursorSettingState(bool open)
    {
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetSettingCursorActive(open);
        }
    }

    private void SetSettingPanelActive(bool active)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ActiveSettingPanel(active);
        }
        else if (settingPanel != null)
        {
            settingPanel.SetActive(active);
        }
    }

    private void SetExitButtonActive(bool active)
    {
        if (UIManager.Instance != null &&
            UIManager.Instance.exitMainMenuButton != null)
        {
            UIManager.Instance.exitMainMenuButton.SetActive(
                active
            );
        }
    }

    private void PlaySettingClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(
                AudioManager.Instance.clickButton
            );
        }
    }

    private void ClearSelectedUI()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                null
            );
        }
    }

    // ==================================================
    // MAIN MENU CONTROLLER SETTING FLOW
    // ==================================================

    /// <summary>
    /// Main Menu đăng ký trực tiếp ba nút Start/Setting/Exit.
    /// GameObject cha có Animator luôn được giữ active.
    /// </summary>
    public void RegisterMainMenu(
        MainMenuController mainMenuController,
        Button startButton,
        Button settingButton,
        Button exitButton)
    {
        registeredMainMenuController = mainMenuController;
        registeredStartButton = startButton;
        registeredSettingButton = settingButton;
        registeredExitButton = exitButton;
        mainMenuButtonsHiddenForControllerSetting = false;

        if (registeredStartButton == null ||
            registeredSettingButton == null ||
            registeredExitButton == null)
        {
           // Debug.LogWarning(
               // "MainMenuController chưa gắn đủ Start, Setting hoặc Exit Button."
           // );
        }
    }

    public void UnregisterMainMenu(
        MainMenuController mainMenuController)
    {
        if (registeredMainMenuController != mainMenuController)
            return;

        registeredMainMenuController = null;
        registeredStartButton = null;
        registeredSettingButton = null;
        registeredExitButton = null;
        mainMenuButtonsHiddenForControllerSetting = false;
    }

    private void HideMainMenuButtonsForControllerSetting()
    {
        if (!HasControllerForSetting() ||
            registeredMainMenuController == null ||
            !HasAnyRegisteredMainMenuButton() ||
            mainMenuButtonsHiddenForControllerSetting)
        {
            return;
        }

        mainMenuButtonsHiddenForControllerSetting = true;
        ClearSelectedUI();
        SetRegisteredMainMenuButtonsActive(false);
    }

    private void RestoreMainMenuButtonsAfterControllerSetting()
    {
        if (!mainMenuButtonsHiddenForControllerSetting)
            return;

        MainMenuController mainMenuController =
            registeredMainMenuController;

        ShowMainMenuButtonsAfterAllControllersDisconnected();

        if (mainMenuController != null)
        {
            mainMenuController
                .RestoreAfterControllerSettingClosed();
        }
    }

    private void ShowMainMenuButtonsAfterAllControllersDisconnected()
    {
        if (!mainMenuButtonsHiddenForControllerSetting)
            return;

        mainMenuButtonsHiddenForControllerSetting = false;
        SetRegisteredMainMenuButtonsActive(true);
    }

    private bool HasAnyRegisteredMainMenuButton()
    {
        return registeredStartButton != null ||
               registeredSettingButton != null ||
               registeredExitButton != null;
    }

    private void SetRegisteredMainMenuButtonsActive(bool active)
    {
        if (registeredStartButton != null)
        {
            registeredStartButton.gameObject.SetActive(active);
        }

        if (registeredSettingButton != null)
        {
            registeredSettingButton.gameObject.SetActive(active);
        }

        if (registeredExitButton != null)
        {
            registeredExitButton.gameObject.SetActive(active);
        }
    }

    // ==================================================
    // BACK TO MAIN MENU
    // ==================================================

    public void OnClickBackToMainMenu()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI(
                AudioManager.Instance.clickButton
            );
        }

        if(MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.ExitStopMiniGame();
            MiniGameManager.Instance.DestroyAllPlayerMiniGame();
        }

        if(MapAndCharacterManager.Instance != null)
        {
            var mainmap = MapAndCharacterManager.Instance.mainMap;
            if( mainmap != null && mainmap.gameObject.activeSelf == true)
            {
                mainmap.SetActive(false);
            }

            var maincharacter = MapAndCharacterManager.Instance.mainCharacters;
            if(maincharacter != null && maincharacter.gameObject.activeSelf == true)
            {
                maincharacter.SetActive(false);
            } 
        }

        if(MapMiniGameList.Instance != null)
        {
            MapMiniGameList.Instance.DisableActiveMapMiniGame();
        }

        StartCoroutine(BackToMainMenuRoutine());
    }

    private IEnumerator BackToMainMenuRoutine()
    {
        /*
         * Resume game trước để animation Loading và các coroutine
         * sử dụng Time.deltaTime có thể tiếp tục chạy.
         */
        if (PauseGameManager.Instance != null)
        {
            PauseGameManager.Instance.ForceResume();
        }
        else
        {
            Time.timeScale = 1f;
        }

        /*
         * Đóng trạng thái Setting trước khi chuyển scene.
         * Không gọi HideGameCursor trực tiếp.
         */
        ResetSetting();

        AudioManager audio = AudioManager.Instance;

        if (audio != null)
        {
            audio.ZeroAllAudio();
            audio.PauseAudio();
        }

        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.openSettingPanelButton != null)
            {
                UIManager.Instance.openSettingPanelButton.SetActive(
                    false
                );
            }

            if (UIManager.Instance.canvasNotifi != null)
            {
                UIManager.Instance.canvasNotifi.SetActive(
                    false
                );
                UIManager.Instance.canvasMiniGame.SetActive(
                    false
                );
                UIManager.Instance.isShowKeyBoard = false;
                UIManager.Instance.HideAllUI();
            }
        }

        if (LoadingManager.Instance != null)
        {
            yield return LoadingManager.Instance.ShowLoading();
        }

        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.SetGraphicsQuality(
                VolumeManager.Instance.currentQuality
            );
        }

        SceneManager.LoadScene(indexScene);
    }
    private void ApplySettingMusicState(bool settingOpen)
    {
        if (AudioManager.Instance == null)
            return;

        if (settingOpen && enableSettingMusic)
        {
            AudioManager.Instance.SetSettingMusicMode(
                true,
                settingMusicVolume
            );
        }
        else
        {
            AudioManager.Instance.SetSettingMusicMode(false);
        }
    }

    // ==================================================
    // GUIDE
    // ==================================================




    private void OnDestroy()
    {
        RemoveListeners();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}