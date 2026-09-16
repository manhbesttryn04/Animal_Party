using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip(
        "Tên scene chứa màn chọn nhân vật, " +
        "phải đúng tên trong Build Settings"
    )]
    public string gameSceneName = "GameScene";

    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button settingButton;
    public Button exitButton;

    [Header("Menu Intro Lock")]
    [Tooltip(
        "Thời gian khóa input tối thiểu của Main Menu. " +
        "Ngay cả khi Animator báo xong sớm, nút vẫn bị khóa đủ thời gian này."
    )]
    [SerializeField] private float menuInputDelay = 1f;

    [Header("Menu Intro Animation Lock")]
    [Tooltip(
        "Animator đang chạy animation xuất hiện của cụm Start / Setting / Exit. " +
        "Nếu gắn Animator, nút chỉ được bấm sau khi animation hiện tại chạy xong."
    )]
    [SerializeField] private Animator menuIntroAnimator;

    [Tooltip("Layer Animator chứa animation intro của menu.")]
    [Min(0)]
    [SerializeField] private int menuIntroAnimatorLayer = 0;

    [Tooltip(
        "Tên chính xác của state Intro trong Animator. " +
        "Có thể dùng tên ngắn như MenuIntro hoặc đường dẫn Base Layer.MenuIntro. " +
        "Để trống thì script tự tìm state không Loop đang chạy."
    )]
    [SerializeField] private string menuIntroStateName = "";

    [Tooltip(
        "Thời gian tối đa chờ tìm state Intro. " +
        "Nếu nhập sai tên state, script chỉ mở khóa sau timeout và báo Warning."
    )]
    [Min(0.1f)]
    [SerializeField] private float menuIntroStateTimeout = 10f;

    [Header("Menu Intro Raycast Lock")]
    [Tooltip(
        "CanvasGroup trên GameObject cha chứa Start / Setting / Exit. " +
        "Nếu để trống và ba nút có cùng cha, script sẽ tự lấy hoặc tự thêm."
    )]
    [SerializeField] private CanvasGroup menuButtonsCanvasGroup;

    [Header("Setting Button Selected Visual")]
    [Tooltip(
        "Khi click nút Setting bằng chuột, giữ trạng thái Selected trong thời gian này. " +
        "Sau đó bắt buộc bỏ Selected để trở về Highlighted hoặc Normal."
    )]
    [Min(0f)]
    [SerializeField] private float settingButtonSelectedTime = 1f;

    [Tooltip(
        "Player phải ngừng click ít nhất thời gian này thì lần click sau mới được phép " +
        "hiện Selected lại. Spam liên tục sẽ không làm Selected bị dính."
    )]
    [Min(0f)]
    [SerializeField] private float settingButtonVisualRearmDelay = 1f;

    [Header("Controller Axis")]
    [Tooltip("Axis dọc của Joystick 1 trong Legacy Input Manager.")]
    [SerializeField] private string verticalP1AxisName = "VerticalP1";

    [Tooltip("Axis dọc của Joystick 2 trong Legacy Input Manager.")]
    [SerializeField] private string verticalP2AxisName = "VerticalP2";

    [Range(0.1f, 1f)]
    [SerializeField] private float inputThreshold = 0.5f;

    [Range(0f, 0.5f)]
    [SerializeField] private float resetThreshold = 0.2f;

    [Header("Focus")]
    [SerializeField] private bool focusStartButtonOnOpen = true;
    private Button[] menuButtons;
    private int currentButtonIndex;

    private bool canMoveVertical = true;
    private bool isLoading;
    private bool canUseMainMenu;

    // Lưu trạng thái interactable ban đầu để sau intro trả lại đúng như cũ.
    private bool startButtonInteractableOnOpen;
    private bool settingButtonInteractableOnOpen;
    private bool exitButtonInteractableOnOpen;

    private bool menuCanvasGroupInteractableOnOpen = true;
    private bool menuCanvasGroupBlocksRaycastsOnOpen = true;

    // Dùng để phát hiện Settings vừa đóng.
    private bool wasSettingOpen;

    // Trạng thái tay cầm ở lần kiểm tra trước.
    private bool previousConsole1Connected;
    private bool previousConsole2Connected;

    // Lưu joystick slot trước đó để phát hiện Unity đổi slot
    // dù trạng thái Connected vẫn là true.
    private int previousConsole1JoystickIndex;
    private int previousConsole2JoystickIndex;

    // Tay cầm hiện đang điều khiển menu:
    // 0 = không có
    // 1 = Console 1
    // 2 = Console 2
    private int activeMenuController;

    // Visual Selected của nút Setting bằng chuột.
    private Coroutine settingButtonSelectedCoroutine;
    private float lastSettingButtonPressTime = -999f;

    // Sau khi Selected 1 giây kết thúc, nếu player vẫn spam click
    // và Unity tự select lại Button thì LateUpdate sẽ xóa ngay.
    private bool forceClearSettingButtonSelection;

    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        SetupButtons();

        // Khóa Start / Settings / Exit ngay từ frame đầu.
        // Chỉ mở khóa khi animation intro thật sự chạy xong.
        LockMenuButtonsForIntro();

        SetupCursor();
        SetupAudio();
        SetupSetting();
        SetupVolume();
        SetupUI();

        InitializeControllerState();
     
        StartCoroutine(UnlockMenuAfterIntro());
    }

    private void Update()
    {
        if (isLoading)
            return;

        UpdateControllerState();

        // Trong thời gian animation mở menu:
        // không cho tay cầm di chuyển hoặc Submit.
        if (!canUseMainMenu)
            return;

        if (activeMenuController == 0)
            return;

        // Khi Settings đang mở, khóa toàn bộ điều khiển Main Menu phía sau.
        // Nút B/Circle được SettingManager tự xử lý.
        if (IsSettingOpen())
        {
            wasSettingOpen = true;
            return;
        }

        // Settings vừa đóng:
        // trả focus về nút Settings và chờ analog thả về giữa
        // trước khi cho Main Menu di chuyển tiếp.
        if (wasSettingOpen)
        {
            wasSettingOpen = false;
            canMoveVertical = false;

            StartCoroutine(
                FocusButtonDelay(settingButton)
            );

            return;
        }

        HandleVerticalInput();
        HandleSubmitInput();
    }

    private void LateUpdate()
    {
        // Logic này chỉ dành cho chuột.
        // Khi có tay cầm, Selected là focus điều khiển menu nên không được clear.
        if (activeMenuController != 0)
            return;

        if (!forceClearSettingButtonSelection)
            return;

        // Nếu player đã ngừng spam đủ lâu thì cho phép
        // lần click sau hiển thị Selected lại.
        if (Time.unscaledTime - lastSettingButtonPressTime >=
            Mathf.Max(0f, settingButtonVisualRearmDelay))
        {
            forceClearSettingButtonSelection = false;
            return;
        }

        if (settingButton == null ||
            EventSystem.current == null)
        {
            return;
        }

        // Nếu Unity tự Select lại nút do player vẫn spam click,
        // xóa ngay trong LateUpdate.
        if (EventSystem.current.currentSelectedGameObject ==
            settingButton.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void SetupButtons()
    {
        menuButtons = new Button[]
        {
            startButton,
            settingButton,
            exitButton
        };

        currentButtonIndex = 0;

        SetupMenuButtonsCanvasGroup();
    }

    private void SetupMenuButtonsCanvasGroup()
    {
        if (menuButtonsCanvasGroup != null)
            return;

        if (startButton == null ||
            settingButton == null ||
            exitButton == null)
        {
            return;
        }

        Transform commonParent =
            startButton.transform.parent;

        if (commonParent == null ||
            !settingButton.transform.IsChildOf(commonParent) ||
            !exitButton.transform.IsChildOf(commonParent))
        {
            return;
        }

        menuButtonsCanvasGroup =
            commonParent.GetComponent<CanvasGroup>();

        if (menuButtonsCanvasGroup == null)
        {
            menuButtonsCanvasGroup =
                commonParent.gameObject
                    .AddComponent<CanvasGroup>();
        }
    }

    private void LockMenuButtonsForIntro()
    {
        canUseMainMenu = false;

        if (menuButtonsCanvasGroup != null)
        {
            menuCanvasGroupInteractableOnOpen =
                menuButtonsCanvasGroup.interactable;

            menuCanvasGroupBlocksRaycastsOnOpen =
                menuButtonsCanvasGroup.blocksRaycasts;

            menuButtonsCanvasGroup.interactable = false;
            menuButtonsCanvasGroup.blocksRaycasts = false;
        }

        if (startButton != null)
        {
            startButtonInteractableOnOpen =
                startButton.interactable;

            startButton.interactable = false;
        }

        if (settingButton != null)
        {
            settingButtonInteractableOnOpen =
                settingButton.interactable;

            settingButton.interactable = false;
        }

        if (exitButton != null)
        {
            exitButtonInteractableOnOpen =
                exitButton.interactable;

            exitButton.interactable = false;
        }

        // Không giữ focus cũ trong lúc 3 nút đang animation.
        ClearControllerFocus();
    }

    private IEnumerator UnlockMenuAfterIntro()
    {
        // =====================================================
        // Có Animator: chờ đúng state Intro chạy xong.
        // Đồng thời luôn khóa ít nhất menuInputDelay giây.
        // =====================================================

        float lockStartTime = Time.unscaledTime;

        if (menuIntroAnimator != null &&
            menuIntroAnimator.gameObject.activeInHierarchy &&
            menuIntroAnimator.enabled)
        {
            yield return StartCoroutine(
                WaitForMenuIntroAnimation()
            );
        }
        float elapsedLockTime =
            Time.unscaledTime - lockStartTime;

        float remainingMinimumLockTime =
            Mathf.Max(0f, menuInputDelay - elapsedLockTime);

        // Dù Animator bị cấu hình sai, không bao giờ mở khóa
        // trước thời gian tối thiểu menuInputDelay.
        if (remainingMinimumLockTime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                remainingMinimumLockTime
            );
        }

        if (isLoading)
            yield break;

        if (startButton != null)
        {
            startButton.interactable =
                startButtonInteractableOnOpen;
        }

        if (settingButton != null)
        {
            settingButton.interactable =
                settingButtonInteractableOnOpen;
        }

        if (exitButton != null)
        {
            exitButton.interactable =
                exitButtonInteractableOnOpen;
        }

        if (menuButtonsCanvasGroup != null)
        {
            menuButtonsCanvasGroup.interactable =
                menuCanvasGroupInteractableOnOpen;

            menuButtonsCanvasGroup.blocksRaycasts =
                menuCanvasGroupBlocksRaycastsOnOpen;
        }

        // Chỉ đến đây mới cho phép Main Menu nhận input.
        canUseMainMenu = true;

        // Nếu analog đang bị giữ từ lúc animation chạy,
        // bắt buộc thả về giữa trước khi được di chuyển menu.
        canMoveVertical = false;

        // Chỉ focus Start sau khi animation đã chạy xong.
        if (focusStartButtonOnOpen &&
            ControllerManager.Instance != null &&
            ControllerManager.Instance.HasAnyController())
        {
            StartCoroutine(
                FocusButtonDelay(startButton)
            );
        }
    }

    private IEnumerator WaitForMenuIntroAnimation()
    {
        // Cho Animator ít nhất 1 frame để bắt đầu transition/state Intro.
        yield return null;

        if (menuIntroAnimator == null)
            yield break;

        int layer = menuIntroAnimatorLayer;

        if (layer < 0 ||
            layer >= menuIntroAnimator.layerCount)
        {
            layer = 0;
        }

        string configuredStateName =
            menuIntroStateName == null
                ? ""
                : menuIntroStateName.Trim();

        int configuredShortNameHash =
            string.IsNullOrEmpty(configuredStateName)
                ? 0
                : Animator.StringToHash(configuredStateName);

        int introStateFullPathHash = 0;
        bool introStarted = false;
        float waitStartTime = Time.unscaledTime;

        while (menuIntroAnimator != null &&
               menuIntroAnimator.enabled &&
               menuIntroAnimator.gameObject.activeInHierarchy)
        {
            AnimatorStateInfo currentState =
                menuIntroAnimator.GetCurrentAnimatorStateInfo(layer);

            bool isTransitioning =
                menuIntroAnimator.IsInTransition(layer);

            AnimatorStateInfo nextState = default;

            if (isTransitioning)
            {
                nextState =
                    menuIntroAnimator
                        .GetNextAnimatorStateInfo(layer);
            }

            if (introStateFullPathHash == 0)
            {
                bool currentIsIntro =
                    IsMenuIntroState(
                        currentState,
                        configuredStateName,
                        configuredShortNameHash
                    );

                bool nextIsIntro =
                    isTransitioning &&
                    IsMenuIntroState(
                        nextState,
                        configuredStateName,
                        configuredShortNameHash
                    );

                if (currentIsIntro)
                {
                    introStateFullPathHash =
                        currentState.fullPathHash;

                    introStarted = true;
                }
                else if (nextIsIntro)
                {
                    introStateFullPathHash =
                        nextState.fullPathHash;
                }
            }

            if (introStateFullPathHash != 0)
            {
                bool currentIsTarget =
                    currentState.fullPathHash ==
                    introStateFullPathHash;

                bool nextIsTarget =
                    isTransitioning &&
                    nextState.fullPathHash ==
                    introStateFullPathHash;

                if (currentIsTarget)
                {
                    introStarted = true;

                    // State Intro đã chạy đủ 100% và không còn transition.
                    if (currentState.normalizedTime >= 1f &&
                        !isTransitioning)
                    {
                        yield break;
                    }
                }
                else if (introStarted &&
                         !nextIsTarget &&
                         !isTransitioning)
                {
                    // Intro đã chuyển hoàn toàn sang state khác.
                    yield break;
                }
            }

            if (Time.unscaledTime - waitStartTime >=
                Mathf.Max(0.1f, menuIntroStateTimeout))
            {
                yield break;
            }

            yield return null;
        }
    }

    private bool IsMenuIntroState(
        AnimatorStateInfo state,
        string configuredStateName,
        int configuredShortNameHash)
    {
        if (!string.IsNullOrEmpty(configuredStateName))
        {
            return state.shortNameHash ==
                       configuredShortNameHash ||
                   state.IsName(configuredStateName);
        }

        // Tự động: state Intro thường là state one-shot không Loop.
        return !state.loop;
    }

    public void SetupUI()
    {
        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.isShowKeyBoard = false;
            if(ui.instructKeyBoardPanel.gameObject.activeSelf == true)
            {
                ui.instructKeyBoardPanel.gameObject.SetActive(false);
            }
        }
    }
    private void SetupCursor()
    {
        CursorManager cursor = CursorManager.Instance;

        if (cursor == null)
            return;

        cursor.SetSceneCursorVisible(true);
    }

    private void SetupAudio()
    {
        AudioManager audio = AudioManager.Instance;

        if (audio == null)
            return;
        audio.SetupMainGameAudio();
        audio.PlayMusic(audio.musicMainMenuClip);
      //  audio.PlaySFXNoOneShot(audio.theNightClip);
        audio.PlayEnvironment(audio.theSeaClip);
       // audio.PlayEnvironment(audio.theSeaAndShipClip);
        
    }

    private void SetupSetting()
    {
        SettingManager setting = SettingManager.Instance;

        if (setting != null)
        {
            setting.enableSettingMusic = true;
            setting.isOpenExitButton = false;
            setting.canOpenSettingByController = false;
            setting.RegisterMainMenu(
                this,
                startButton,
                settingButton,
                exitButton
            );
        }
    }

    private void SetupVolume()
    {
        VolumeManager volume = VolumeManager.Instance;

        if (volume != null)
        {
            volume.ResetVignette();
            volume.ResetDepthBlur();
            volume.ResetBloom();
        }
    }

    // =========================================================
    // CONTROLLER STATE
    // =========================================================

    private void InitializeControllerState()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null)
        {
            previousConsole1Connected = false;
            previousConsole2Connected = false;

            previousConsole1JoystickIndex = 0;
            previousConsole2JoystickIndex = 0;

            activeMenuController = 0;
            return;
        }

        previousConsole1Connected =
            controller.IsConsole1Connected();

        previousConsole2Connected =
            controller.IsConsole2Connected();

        previousConsole1JoystickIndex =
            controller.GetConsole1JoystickIndex();

        previousConsole2JoystickIndex =
            controller.GetConsole2JoystickIndex();

        SelectActiveMenuController();
    }

    private void UpdateControllerState()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null)
        {
            SetNoControllerState();
            return;
        }

        bool console1Connected =
            controller.IsConsole1Connected();

        bool console2Connected =
            controller.IsConsole2Connected();

        int console1JoystickIndex =
            controller.GetConsole1JoystickIndex();

        int console2JoystickIndex =
            controller.GetConsole2JoystickIndex();

        bool connectionChanged =
            console1Connected != previousConsole1Connected ||
            console2Connected != previousConsole2Connected;

        bool joystickIndexChanged =
            console1JoystickIndex != previousConsole1JoystickIndex ||
            console2JoystickIndex != previousConsole2JoystickIndex;

        if (!connectionChanged &&
            !joystickIndexChanged)
        {
            return;
        }

        previousConsole1Connected =
            console1Connected;

        previousConsole2Connected =
            console2Connected;

        previousConsole1JoystickIndex =
            console1JoystickIndex;

        previousConsole2JoystickIndex =
            console2JoystickIndex;

        SelectActiveMenuController();

        // Sau khi cắm/rút hoặc đổi joystick slot,
        // yêu cầu thả analog về giữa trước khi di chuyển tiếp.
        canMoveVertical = false;

        CursorManager cursor =
            CursorManager.Instance;

        if (cursor != null)
        {
            cursor.UpdateCursorByControllerState();
        }

        bool hasAnyController =
            console1Connected ||
            console2Connected;

        if (!hasAnyController)
        {
            ClearControllerFocus();
            return;
        }

        // Không focus trong lúc intro chưa chạy xong.
        if (!canUseMainMenu)
            return;

        // Khi vừa cắm tay cầm hoặc Unity đổi joystick slot,
        // focus lại nút Start.
        StartCoroutine(
            FocusButtonDelay(startButton)
        );
    }

    private void SelectActiveMenuController()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null)
        {
            activeMenuController = 0;
            return;
        }

        // Luôn ưu tiên Console 1.
        if (controller.IsConsole1Connected())
        {
            activeMenuController = 1;



            return;
        }

        // Nếu Console 1 bị rút nhưng Console 2 vẫn còn,
        // Console 2 được phép điều khiển menu.
        if (controller.IsConsole2Connected())
        {
            activeMenuController = 2;



            return;
        }

        activeMenuController = 0;


    }

    private void SetNoControllerState()
    {
        if (activeMenuController == 0 &&
            !previousConsole1Connected &&
            !previousConsole2Connected)
        {
            return;
        }

        activeMenuController = 0;

        previousConsole1Connected = false;
        previousConsole2Connected = false;

        previousConsole1JoystickIndex = 0;
        previousConsole2JoystickIndex = 0;

        canMoveVertical = true;

        ClearControllerFocus();

        CursorManager cursor =
            CursorManager.Instance;

        if (cursor != null)
        {
            cursor.UpdateCursorByControllerState();
        }
    }

    // =========================================================
    // GET ACTIVE CONTROLLER INPUT
    // =========================================================

    private float GetActiveVerticalInput()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null ||
            activeMenuController == 0)
        {
            return 0f;
        }

        // ControllerManager lấy joystick index thật của Console
        // rồi chọn VerticalP1 hoặc VerticalP2 tương ứng.
        return controller.GetConsoleAxisRaw(
            activeMenuController,
            verticalP1AxisName,
            verticalP2AxisName
        );
    }

    private bool GetActiveSubmitDown()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null ||
            activeMenuController == 0)
        {
            return false;
        }

        // Button 0:
        // Xbox A / PlayStation Cross.
        return controller.GetConsoleButtonDown(
            activeMenuController,
            0
        );
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    private bool IsSettingOpen()
    {
        SettingManager setting =
            SettingManager.Instance;

        if (setting == null)
            return false;

        return setting.isSettingOpen ||
               setting.isEscSettingOpen;
    }

    // =========================================================
    // SETTING BUTTON SELECTED VISUAL
    // =========================================================

    private void RegisterSettingButtonPressVisual()
    {
        // Chỉ áp dụng cho chuột.
        // Khi dùng tay cầm, Selected là focus nên không đụng vào.
        if (activeMenuController != 0)
            return;

        if (settingButton == null ||
            EventSystem.current == null ||
            !settingButton.gameObject.activeInHierarchy)
        {
            return;
        }

        float now = Time.unscaledTime;

        float rearmDelay = Mathf.Max(
            0f,
            settingButtonVisualRearmDelay
        );

        bool enoughTimeSinceLastPress =
            now - lastSettingButtonPressTime >= rearmDelay;

        // Luôn ghi nhận lần click mới nhất.
        // Spam liên tục sẽ liên tục đẩy mốc này lên.
        lastSettingButtonPressTime = now;

        // Nếu Selected 1 giây đang chạy thì không restart timer.
        if (settingButtonSelectedCoroutine != null)
        {
            return;
        }

        // Nếu chưa ngừng spam đủ lâu:
        // không cho Selected xuất hiện lại.
        if (!enoughTimeSinceLastPress)
        {
            forceClearSettingButtonSelection = true;

            if (EventSystem.current.currentSelectedGameObject ==
                settingButton.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            return;
        }

        forceClearSettingButtonSelection = false;

        settingButtonSelectedCoroutine =
            StartCoroutine(
                SettingButtonSelectedVisualRoutine()
            );
    }

    private IEnumerator SettingButtonSelectedVisualRoutine()
    {
        if (settingButton == null ||
            EventSystem.current == null)
        {
            settingButtonSelectedCoroutine = null;
            yield break;
        }

        // Ép Setting Button sang trạng thái Selected.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(
            settingButton.gameObject
        );

        // Dùng realtime vì lúc Setting mở game có thể Pause.
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, settingButtonSelectedTime)
        );

        // Sau đúng thời gian trên bắt buộc bỏ Selected.
        // Unity tự quyết định trạng thái tiếp theo:
        // - Chuột còn hover -> Highlighted
        // - Chuột đã rời -> Normal
        if (EventSystem.current != null &&
            settingButton != null &&
            EventSystem.current.currentSelectedGameObject ==
                settingButton.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        settingButtonSelectedCoroutine = null;

        // Nếu player vẫn spam thì LateUpdate tiếp tục xóa Selected
        // cho tới khi player ngừng click đủ rearm delay.
        forceClearSettingButtonSelection = true;
    }

    // =========================================================
    // VERTICAL INPUT
    // =========================================================

    private void HandleVerticalInput()
    {
        if (menuButtons == null ||
            menuButtons.Length == 0)
        {
            return;
        }

        float vertical =
            GetActiveVerticalInput();

        if (Mathf.Abs(vertical) <= resetThreshold)
        {
            canMoveVertical = true;
            return;
        }

        if (!canMoveVertical)
            return;

        if (vertical > inputThreshold)
        {
            MoveToPreviousButton();
            canMoveVertical = false;
        }
        else if (vertical < -inputThreshold)
        {
            MoveToNextButton();
            canMoveVertical = false;
        }
    }

    private void MoveToPreviousButton()
    {
        if (menuButtons == null ||
            menuButtons.Length == 0)
        {
            return;
        }

        int startIndex = currentButtonIndex;

        do
        {
            currentButtonIndex--;

            if (currentButtonIndex < 0)
            {
                currentButtonIndex =
                    menuButtons.Length - 1;
            }

            Button button =
                menuButtons[currentButtonIndex];

            if (CanSelectButton(button))
            {
                FocusButton(button);
                PlayMoveSound();
                return;
            }
        }
        while (currentButtonIndex != startIndex);
    }

    private void MoveToNextButton()
    {
        if (menuButtons == null ||
            menuButtons.Length == 0)
        {
            return;
        }

        int startIndex = currentButtonIndex;

        do
        {
            currentButtonIndex++;

            if (currentButtonIndex >=
                menuButtons.Length)
            {
                currentButtonIndex = 0;
            }

            Button button =
                menuButtons[currentButtonIndex];

            if (CanSelectButton(button))
            {
                FocusButton(button);
                PlayMoveSound();
                return;
            }
        }
        while (currentButtonIndex != startIndex);
    }

    private bool CanSelectButton(
        Button button)
    {
        return button != null &&
               button.gameObject.activeInHierarchy &&
               button.interactable;
    }

    // =========================================================
    // SUBMIT BUTTON
    // =========================================================

    private void HandleSubmitInput()
    {
        if (!GetActiveSubmitDown())
            return;

        if (EventSystem.current == null)
            return;

        GameObject selectedObject =
            EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
        {
            FocusButton(startButton);
            return;
        }

        Button selectedButton =
            selectedObject.GetComponent<Button>();

        if (selectedButton == null || !selectedButton.interactable)
            return;

        // Nháy pressed 0.2s rồi mới Invoke cho tất cả Start / Setting / Exit
        StartCoroutine(PressAndInvoke(selectedButton));
    }

    private IEnumerator PressAndInvoke(Button button)
    {
        if (button == null) yield break;

        // Hiệu ứng pressed
        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            ColorBlock colors = button.colors;
            SpriteState sprites = button.spriteState;

            image.color = colors.pressedColor;
            if (sprites.pressedSprite != null)
                image.overrideSprite = sprites.pressedSprite;
        }

        // Giữ pressed 0.2 giây
        yield return new WaitForSeconds(0.1f);

        // Trả về normal
        if (image != null)
        {
            image.overrideSprite = null;
            image.color = button.colors.normalColor;
        }

        // Thực thi hành động gốc
        button.onClick.Invoke();
    }


    // =========================================================
    // FLASH MENU BUTTON (Controller Pressed Visual)
    // =========================================================
    private IEnumerator FlashMenuButton(Button button)
    {
        if (button == null) yield break;

        Image image = button.targetGraphic as Image;
        if (image == null) yield break;

        ColorBlock colors = button.colors;
        SpriteState sprites = button.spriteState;

        // Hiệu ứng Pressed
        image.color = colors.pressedColor;
        if (sprites.pressedSprite != null)
            image.overrideSprite = sprites.pressedSprite;

        yield return new WaitForSeconds(0.1f);

        // Trả về Normal
        image.overrideSprite = null;
        image.color = colors.normalColor;
    }


    // =========================================================
    // FOCUS
    // =========================================================

    private IEnumerator FocusButtonDelay(
        Button targetButton)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (activeMenuController == 0)
            yield break;

        if (!canUseMainMenu)
            yield break;

        FocusButton(targetButton);
    }

    private void FocusButton(
        Button targetButton)
    {
        if (EventSystem.current == null)
        {


            return;
        }

        if (!CanSelectButton(targetButton))
            return;

        EventSystem.current
            .SetSelectedGameObject(null);

        EventSystem.current
            .SetSelectedGameObject(
                targetButton.gameObject
            );

        int index =
            System.Array.IndexOf(
                menuButtons,
                targetButton
            );

        if (index >= 0)
        {
            currentButtonIndex = index;
        }
    }

    private void FocusCurrentButton()
    {
        if (activeMenuController == 0)
            return;

        if (!canUseMainMenu)
            return;

        if (menuButtons == null ||
            menuButtons.Length == 0)
        {
            return;
        }

        if (currentButtonIndex < 0 ||
            currentButtonIndex >=
            menuButtons.Length)
        {
            currentButtonIndex = 0;
        }

        Button currentButton =
            menuButtons[currentButtonIndex];

        if (CanSelectButton(currentButton))
        {
            FocusButton(currentButton);
        }
        else
        {
            FocusButton(startButton);
        }
    }

    private void ClearControllerFocus()
    {
        if (EventSystem.current == null)
            return;

        EventSystem.current
            .SetSelectedGameObject(null);
    }

    public void FocusStartButton()
    {
        FocusButton(startButton);
    }

    public void FocusSettingButton()
    {
        FocusButton(settingButton);
    }

    public void FocusExitButton()
    {
        FocusButton(exitButton);
    }

    /// <summary>
    /// Được SettingManager gọi sau khi B/Circle đóng Setting.
    /// Lúc này cụm menu đã được hiện lại, nên trả focus về nút Setting.
    /// </summary>
    public void RestoreAfterControllerSettingClosed()
    {
        if (isLoading)
            return;

        // Ba nút vừa được bật lại, cập nhật tay cầm đang điều khiển
        // trước khi tạo focus.
        SelectActiveMenuController();

        wasSettingOpen = false;
        canMoveVertical = false;

        if (activeMenuController == 0)
            return;

        StartCoroutine(
            FocusButtonDelay(settingButton)
        );
    }

    private void PlayMoveSound()
    {
        AudioManager audio =
            AudioManager.Instance;

        if (audio != null)
        {
            audio.PlayUI(
                audio.movechooseItemClip
            );
        }
    }

    // =========================================================
    // START GAME
    // =========================================================

    public void OnStartClicked()
    {
        
        // Animation intro chưa xong -> tuyệt đối không nhận.
        if (!canUseMainMenu || isLoading)
            return;

        // Reset Pause ngay khi nhấn Start.
        if (PauseGameManager.Instance != null)
        {
            PauseGameManager.Instance.ForceResume();
        }
        else
        {
            Time.timeScale = 1f;
        }

        isLoading = true;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        AudioManager audio = AudioManager.Instance;

        if (audio != null)
        {
            audio.PlayUI(audio.clickButton);
          
        }

        var maincharacterandmap = MapAndCharacterManager.Instance;
        if (maincharacterandmap != null)
        {
            var mainmap = maincharacterandmap.mainMap;
            if (mainmap != null)
            {
                mainmap.SetActive(false);
            }

            var maincharacter = maincharacterandmap.mainCharacters;
            if (maincharacter != null)
            {
                maincharacter.SetActive(false);
            }

        }
        StartCoroutine(StartLoadScene());
    }

    private IEnumerator StartLoadScene()
    {
      //  yield return new WaitForSeconds(0.2f);
        AudioManager audio =
            AudioManager.Instance;

        if (audio != null)
        {
            audio.PauseAudio();
        }

        CursorManager cursor =
            CursorManager.Instance;

        if (cursor != null)
        {
            cursor.HideGameCursor();
        }

        SettingManager setting = SettingManager.Instance;
        if (setting != null)
        {
            setting.ResetSetting();
        }

        LoadingManager loading =
            LoadingManager.Instance;

        if (loading != null)
        {
           
           
            yield return StartCoroutine(
                loading.ShowLoading()
            );
        }

        SceneManager.LoadScene(
            gameSceneName
        );
    }

    // =========================================================
    // SETTINGS BUTTONS
    // =========================================================

    public void OnSettingsClicked()
    {
        // Animation intro chưa xong -> tuyệt đối không nhận.
        if (!canUseMainMenu || isLoading)
            return;

        // LOGIC 2:
        // Selected chỉ giữ 1 giây.
        // Spam liên tục không làm Selected bị dính.
        RegisterSettingButtonPressVisual();

        SettingManager setting =
            SettingManager.Instance;

        if (setting == null)
            return;

        // Không phát click ở MainMenuController.
        // SettingManager tự chống spam Open/Close bằng cooldown
        // và chỉ phát tiếng khi lệnh thực sự hợp lệ.
        setting.ToggleSetting();
    }

    public void OnCloseSettingsClicked()
    {
        if (!canUseMainMenu || isLoading)
            return;

        // Khi đóng cũng áp dụng visual Selected 1 giây
        // cho nút Setting của Main Menu.
        RegisterSettingButtonPressVisual();

        SettingManager setting =
            SettingManager.Instance;

        if (setting == null)
            return;

        // Đóng bình thường phải đi qua ToggleSetting để:
        // - dùng cooldown 1 giây
        // - không spam âm thanh
        // - không thể vừa đóng xong đã mở lại ngay
        setting.ToggleSetting();

        // Tránh analog đang giữ làm menu nhảy ngay sau khi đóng.
        canMoveVertical = false;

        if (activeMenuController != 0)
        {
            StartCoroutine(
                FocusButtonDelay(settingButton)
            );
        }
    }
    // =========================================================
    // FLASH MENU BUTTON (Controller Pressed Visual)
    // =========================================================
   
    // =========================================================
    // EXIT GAME
    // =========================================================

    public void OnExitClicked()
    {
        // Animation intro chưa xong -> tuyệt đối không nhận.
        if (!canUseMainMenu || isLoading)
            return;

        AudioManager audio =
            AudioManager.Instance;

        if (audio != null)
        {
            audio.PlayUI(
                audio.clickButton
            );
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        SettingManager setting = SettingManager.Instance;

        if (setting != null)
        {
            setting.UnregisterMainMenu(this);
        }
    }
}