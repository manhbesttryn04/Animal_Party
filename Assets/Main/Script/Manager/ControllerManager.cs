using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

public class ControllerManager : MonoBehaviour
{
    public enum ControllerChangeType
    {
        None,
        ConnectedNewController,
        Disconnected,
        ReconnectedSameController,
        ReplacedWithDifferentController
    }

    public static ControllerManager Instance
    {
        get;
        private set;
    }

    // =========================================================
    // CONTROLLER STATE
    // =========================================================

    [Header("Controller Slots")]
    [SerializeField] private bool console1Connected;
    [SerializeField] private bool console2Connected;

    [Header("Input System Device ID")]
    [Tooltip("Device ID của Gamepad đang được gán cho Controller 1.")]
    [SerializeField] private int console1JoystickIndex;

    [Tooltip("Device ID của Gamepad đang được gán cho Controller 2.")]
    [SerializeField] private int console2JoystickIndex;

    [Header("Controller Names")]
    [SerializeField] private string console1Name = "";
    [SerializeField] private string console2Name = "";

    [Header("Last Change")]
    [SerializeField]
    private ControllerChangeType console1LastChange;

    [SerializeField]
    private ControllerChangeType console2LastChange;

    [Header("Update")]
    [Tooltip("Kiểm tra dự phòng. Việc cắm/rút chính được nhận bằng InputSystem.onDeviceChange.")]
    [Min(0.05f)]
    [SerializeField] private float checkInterval = 0.25f;

    private Gamepad console1Gamepad;
    private Gamepad console2Gamepad;

    // Giữ dấu vết thiết bị cũ để ưu tiên trả tay cầm về đúng Console
    // sau khi rút rồi cắm lại.
    private int rememberedConsole1DeviceId;
    private int rememberedConsole2DeviceId;
    private string rememberedConsole1Fingerprint = "";
    private string rememberedConsole2Fingerprint = "";

    private float checkTimer;
    private bool controllerStateDirty = true;
    private bool hasInitialized;

    private Coroutine notificationCoroutine;
    private bool allowControllerSounds;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        SetupSingleton();
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

    private IEnumerator Start()
    {
        SetupInitialControllerState();
        yield return SetupAudioDelay();
    }

    private void SetupInitialControllerState()
    {
        InitializeControllerState();
        hasInitialized = true;
    }

    private IEnumerator SetupAudioDelay()
    {
        float waitTimer = 0f;
        const float maxWaitTime = 2f;

        while (AudioManager.Instance == null && waitTimer < maxWaitTime)
        {
            waitTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        allowControllerSounds = true;
        PlayInitialConnectedSound();
    }


    private void OnEnable()
    {
        InputSystem.onDeviceChange +=
            HandleInputDeviceChange;

        controllerStateDirty = true;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -=
            HandleInputDeviceChange;
    }

    private void Update()
    {
        checkTimer += Time.unscaledDeltaTime;

        bool shouldSafetyCheck =
            checkTimer >= checkInterval;

        if (!controllerStateDirty &&
            !shouldSafetyCheck)
        {
            return;
        }

        checkTimer = 0f;
        controllerStateDirty = false;

        if (hasInitialized)
        {
            RefreshControllerState();
        }
    }

    private void HandleInputDeviceChange(
        InputDevice device,
        InputDeviceChange change)
    {
        if (!(device is Gamepad))
            return;

        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
            case InputDeviceChange.Reconnected:
            case InputDeviceChange.Enabled:
            case InputDeviceChange.Disabled:
            case InputDeviceChange.ConfigurationChanged:
                controllerStateDirty = true;
                break;
        }
    }

    // =========================================================
    // INITIALIZE
    // =========================================================

    private void InitializeControllerState()
    {
        List<Gamepad> currentGamepads =
            GetConnectedGamepads();

        if (currentGamepads.Count >= 1)
        {
            AssignConsole1(
                currentGamepads[0],
                ControllerChangeType.None,
                false
            );
        }

        if (currentGamepads.Count >= 2)
        {
            AssignConsole2(
                currentGamepads[1],
                ControllerChangeType.None,
                false
            );
        }

        controllerStateDirty = false;

        UpdateCursor();
        PrintControllerState();
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshControllerState()
    {
        List<Gamepad> currentGamepads =
            GetConnectedGamepads();

        bool changed =
            UpdateControllerSlots(currentGamepads);

        if (!changed)
            return;

        UpdateCursor();
        PrintControllerState();
    }

    // =========================================================
    // CONTROLLER SLOT LOGIC
    // =========================================================

    private bool UpdateControllerSlots(
        List<Gamepad> currentGamepads)
    {
        console1LastChange =
            ControllerChangeType.None;

        console2LastChange =
            ControllerChangeType.None;

        bool changed = false;

        List<Gamepad> unassigned =
            new List<Gamepad>(currentGamepads);

        // Giữ thiết bị hiện tại của Console 1 nếu nó vẫn còn.
        int console1CurrentIndex =
            FindCurrentGamepad(
                unassigned,
                console1Gamepad
            );

        if (console1CurrentIndex >= 0)
        {
            Gamepad gamepad =
                unassigned[console1CurrentIndex];

            AssignConsole1(
                gamepad,
                ControllerChangeType.None,
                false
            );

            unassigned.RemoveAt(
                console1CurrentIndex
            );
        }
        else if (console1Connected ||
                 console1Gamepad != null)
        {
            DisconnectConsole1();
            changed = true;
        }

        // Giữ thiết bị hiện tại của Console 2 nếu nó vẫn còn.
        int console2CurrentIndex =
            FindCurrentGamepad(
                unassigned,
                console2Gamepad
            );

        if (console2CurrentIndex >= 0)
        {
            Gamepad gamepad =
                unassigned[console2CurrentIndex];

            AssignConsole2(
                gamepad,
                ControllerChangeType.None,
                false
            );

            unassigned.RemoveAt(
                console2CurrentIndex
            );
        }
        else if (console2Connected ||
                 console2Gamepad != null)
        {
            DisconnectConsole2();
            changed = true;
        }

        // Ưu tiên trả thiết bị cũ về Console 1.
        if (!console1Connected)
        {
            int rememberedIndex =
                FindRememberedGamepad(
                    unassigned,
                    rememberedConsole1DeviceId,
                    rememberedConsole1Fingerprint
                );

            if (rememberedIndex >= 0)
            {
                Gamepad gamepad =
                    unassigned[rememberedIndex];

                unassigned.RemoveAt(
                    rememberedIndex
                );

                AssignConsole1(
                    gamepad,
                    ControllerChangeType
                        .ReconnectedSameController,
                    true
                );

                changed = true;
            }
        }

        // Ưu tiên trả thiết bị cũ về Console 2.
        if (!console2Connected)
        {
            int rememberedIndex =
                FindRememberedGamepad(
                    unassigned,
                    rememberedConsole2DeviceId,
                    rememberedConsole2Fingerprint
                );

            if (rememberedIndex >= 0)
            {
                Gamepad gamepad =
                    unassigned[rememberedIndex];

                unassigned.RemoveAt(
                    rememberedIndex
                );

                AssignConsole2(
                    gamepad,
                    ControllerChangeType
                        .ReconnectedSameController,
                    true
                );

                changed = true;
            }
        }

        // Console 1 còn trống thì lấy tay cầm chưa được gán đầu tiên.
        if (!console1Connected &&
            unassigned.Count > 0)
        {
            Gamepad gamepad = unassigned[0];
            unassigned.RemoveAt(0);

            ControllerChangeType changeType =
                HasRememberedConsole1()
                    ? ControllerChangeType
                        .ReplacedWithDifferentController
                    : ControllerChangeType
                        .ConnectedNewController;

            AssignConsole1(
                gamepad,
                changeType,
                true
            );

            changed = true;
        }

        // Console 2 còn trống thì lấy tay cầm chưa được gán đầu tiên.
        if (!console2Connected &&
            unassigned.Count > 0)
        {
            Gamepad gamepad = unassigned[0];
            unassigned.RemoveAt(0);

            ControllerChangeType changeType =
                HasRememberedConsole2()
                    ? ControllerChangeType
                        .ReplacedWithDifferentController
                    : ControllerChangeType
                        .ConnectedNewController;

            AssignConsole2(
                gamepad,
                changeType,
                true
            );

            changed = true;
        }

        // Tay cầm thứ ba trở lên vẫn bị bỏ qua.
        return changed;
    }

    // =========================================================
    // ASSIGN
    // =========================================================

    private void AssignConsole1(
        Gamepad gamepad,
        ControllerChangeType changeType,
        bool notify)
    {
        if (gamepad == null)
            return;

        console1Gamepad = gamepad;
        console1Connected = true;
        console1Name = GetGamepadDisplayName(gamepad);
        console1JoystickIndex = gamepad.deviceId;

        rememberedConsole1DeviceId =
            gamepad.deviceId;

        rememberedConsole1Fingerprint =
            GetGamepadFingerprint(gamepad);

        console1LastChange = changeType;

        if (notify)
        {
            PlayConnectSound();
        }
    }

    private void AssignConsole2(
        Gamepad gamepad,
        ControllerChangeType changeType,
        bool notify)
    {
        if (gamepad == null)
            return;

        console2Gamepad = gamepad;
        console2Connected = true;
        console2Name = GetGamepadDisplayName(gamepad);
        console2JoystickIndex = gamepad.deviceId;

        rememberedConsole2DeviceId =
            gamepad.deviceId;

        rememberedConsole2Fingerprint =
            GetGamepadFingerprint(gamepad);

        console2LastChange = changeType;

        if (notify)
        {
            PlayConnectSound();
        }
    }

    // =========================================================
    // DISCONNECT
    // =========================================================

    private void DisconnectConsole1()
    {
        if (!console1Connected &&
            console1Gamepad == null)
        {
            return;
        }

        RememberConsole1Gamepad();

        console1Gamepad = null;
        console1Connected = false;
        console1JoystickIndex = 0;
        console1Name = "";

        console1LastChange =
            ControllerChangeType.Disconnected;

        PlayDisconnectSound();
    }

    private void DisconnectConsole2()
    {
        if (!console2Connected &&
            console2Gamepad == null)
        {
            return;
        }

        RememberConsole2Gamepad();

        console2Gamepad = null;
        console2Connected = false;
        console2JoystickIndex = 0;
        console2Name = "";

        console2LastChange =
            ControllerChangeType.Disconnected;

        PlayDisconnectSound();
    }

    private void RememberConsole1Gamepad()
    {
        if (console1Gamepad == null)
            return;

        rememberedConsole1DeviceId =
            console1Gamepad.deviceId;

        rememberedConsole1Fingerprint =
            GetGamepadFingerprint(
                console1Gamepad
            );
    }

    private void RememberConsole2Gamepad()
    {
        if (console2Gamepad == null)
            return;

        rememberedConsole2DeviceId =
            console2Gamepad.deviceId;

        rememberedConsole2Fingerprint =
            GetGamepadFingerprint(
                console2Gamepad
            );
    }

    private bool HasRememberedConsole1()
    {
        return rememberedConsole1DeviceId != 0 ||
               !string.IsNullOrWhiteSpace(
                   rememberedConsole1Fingerprint
               );
    }

    private bool HasRememberedConsole2()
    {
        return rememberedConsole2DeviceId != 0 ||
               !string.IsNullOrWhiteSpace(
                   rememberedConsole2Fingerprint
               );
    }

    // =========================================================
    // GAMEPAD LIST / MATCHING
    // =========================================================

    private List<Gamepad> GetConnectedGamepads()
    {
        List<Gamepad> gamepads =
            new List<Gamepad>();

        for (int i = 0;
             i < Gamepad.all.Count;
             i++)
        {
            Gamepad gamepad = Gamepad.all[i];

            if (gamepad == null ||
                !gamepad.enabled)
            {
                continue;
            }

            gamepads.Add(gamepad);
        }

        return gamepads;
    }

    private bool IsGamepadConnected(
        Gamepad target)
    {
        if (target == null)
            return false;

        for (int i = 0;
             i < Gamepad.all.Count;
             i++)
        {
            Gamepad current = Gamepad.all[i];

            if (current == null ||
                !current.enabled)
            {
                continue;
            }

            if (ReferenceEquals(current, target) ||
                current.deviceId == target.deviceId)
            {
                return true;
            }
        }

        return false;
    }

    private int FindCurrentGamepad(
        List<Gamepad> gamepads,
        Gamepad currentGamepad)
    {
        if (gamepads == null ||
            currentGamepad == null)
        {
            return -1;
        }

        for (int i = 0;
             i < gamepads.Count;
             i++)
        {
            Gamepad candidate = gamepads[i];

            if (candidate == null)
                continue;

            if (ReferenceEquals(
                    candidate,
                    currentGamepad) ||
                candidate.deviceId ==
                    currentGamepad.deviceId)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindRememberedGamepad(
        List<Gamepad> gamepads,
        int rememberedDeviceId,
        string rememberedFingerprint)
    {
        if (gamepads == null ||
            gamepads.Count == 0)
        {
            return -1;
        }

        // Input System thường giữ nguyên deviceId khi Reconnected.
        if (rememberedDeviceId != 0)
        {
            for (int i = 0;
                 i < gamepads.Count;
                 i++)
            {
                if (gamepads[i] != null &&
                    gamepads[i].deviceId ==
                        rememberedDeviceId)
                {
                    return i;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(
            rememberedFingerprint))
        {
            return -1;
        }

        // Nếu deviceId thay đổi, chỉ ghép bằng fingerprint khi
        // trong danh sách chưa gán có đúng một thiết bị phù hợp.
        int foundIndex = -1;
        int matchCount = 0;

        for (int i = 0;
             i < gamepads.Count;
             i++)
        {
            Gamepad gamepad = gamepads[i];

            if (gamepad == null)
                continue;

            string fingerprint =
                GetGamepadFingerprint(gamepad);

            if (!string.Equals(
                fingerprint,
                rememberedFingerprint,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foundIndex = i;
            matchCount++;
        }

        return matchCount == 1
            ? foundIndex
            : -1;
    }

    private string GetGamepadDisplayName(
        Gamepad gamepad)
    {
        if (gamepad == null)
            return "";

        if (!string.IsNullOrWhiteSpace(
            gamepad.displayName))
        {
            return gamepad.displayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(
            gamepad.description.product))
        {
            return gamepad.description
                .product.Trim();
        }

        return gamepad.name;
    }

    private string GetGamepadFingerprint(
        Gamepad gamepad)
    {
        if (gamepad == null)
            return "";

        InputDeviceDescription description =
            gamepad.description;

        return string.Join(
            "|",
            description.interfaceName ?? "",
            description.manufacturer ?? "",
            description.product ?? "",
            description.serial ?? "",
            gamepad.layout ?? ""
        ).Trim().ToLowerInvariant();
    }

    // =========================================================
    // AXIS INPUT
    // =========================================================

    // Giữ nguyên chữ ký hàm cũ để MainMenu và SettingManager
    // không cần sửa. Tên Axis cũ chỉ dùng để xác định trục X/Y.
    public float GetConsoleAxisRaw(
        int consoleNumber,
        string joystick1Axis,
        string joystick2Axis)
    {
        string selectedAxis =
            consoleNumber == 2
                ? joystick2Axis
                : joystick1Axis;

        if (string.IsNullOrWhiteSpace(
            selectedAxis))
        {
            selectedAxis =
                joystick1Axis ??
                joystick2Axis ??
                "";
        }

        bool isVertical =
            selectedAxis.IndexOf(
                "Vertical",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;

        return isVertical
            ? GetConsoleVerticalRaw(consoleNumber)
            : GetConsoleHorizontalRaw(consoleNumber);
    }

    // Giữ overload cũ để toàn bộ script hiện tại tiếp tục compile.
    public float GetConsoleHorizontalRaw(
        int consoleNumber,
        string horizontalJoystick1,
        string horizontalJoystick2)
    {
        return GetConsoleHorizontalRaw(
            consoleNumber
        );
    }

    public float GetConsoleVerticalRaw(
        int consoleNumber,
        string verticalJoystick1,
        string verticalJoystick2)
    {
        return GetConsoleVerticalRaw(
            consoleNumber
        );
    }

    // Overload mới, dùng khi dọn code Legacy về sau.
    public float GetConsoleHorizontalRaw(
        int consoleNumber)
    {
        return GetConsoleMoveInput(
            consoleNumber
        ).x;
    }

    public float GetConsoleVerticalRaw(
        int consoleNumber)
    {
        return GetConsoleMoveInput(
            consoleNumber
        ).y;
    }

    private Vector2 GetConsoleMoveInput(
        int consoleNumber)
    {
        Gamepad gamepad =
            GetConsoleGamepad(consoleNumber);

        if (gamepad == null)
            return Vector2.zero;

        Vector2 stick =
            gamepad.leftStick.ReadValue();

        Vector2 dpad =
            gamepad.dpad.ReadValue();

        float horizontal =
            Mathf.Abs(dpad.x) >
            Mathf.Abs(stick.x)
                ? dpad.x
                : stick.x;

        float vertical =
            Mathf.Abs(dpad.y) >
            Mathf.Abs(stick.y)
                ? dpad.y
                : stick.y;

        return new Vector2(
            horizontal,
            vertical
        );
    }

    // =========================================================
    // BUTTON INPUT
    // =========================================================

    public bool GetConsoleButtonDown(
        int consoleNumber,
        int buttonIndex)
    {
        ButtonControl button =
            GetGamepadButton(
                GetConsoleGamepad(consoleNumber),
                buttonIndex
            );

        return button != null &&
               button.wasPressedThisFrame;
    }

    public bool GetConsoleButton(
        int consoleNumber,
        int buttonIndex)
    {
        ButtonControl button =
            GetGamepadButton(
                GetConsoleGamepad(consoleNumber),
                buttonIndex
            );

        return button != null &&
               button.isPressed;
    }

    public bool GetConsoleButtonUp(
        int consoleNumber,
        int buttonIndex)
    {
        ButtonControl button =
            GetGamepadButton(
                GetConsoleGamepad(consoleNumber),
                buttonIndex
            );

        return button != null &&
               button.wasReleasedThisFrame;
    }

    private ButtonControl GetGamepadButton(
        Gamepad gamepad,
        int buttonIndex)
    {
        if (gamepad == null)
            return null;

        switch (buttonIndex)
        {
            // Giữ quy ước Button cũ của project.
            case 0:
                return gamepad.buttonSouth;      // A / Cross

            case 1:
                return gamepad.buttonEast;       // B / Circle

            case 2:
                return gamepad.buttonWest;       // X / Square

            case 3:
                return gamepad.buttonNorth;      // Y / Triangle

            case 4:
                return gamepad.leftShoulder;

            case 5:
                return gamepad.rightShoulder;

            case 6:
                return gamepad.selectButton;

            case 7:
                return gamepad.startButton;

            case 8:
                return gamepad.leftStickButton;

            case 9:
                return gamepad.rightStickButton;

            case 10:
                return gamepad.leftTrigger;

            case 11:
                return gamepad.rightTrigger;

            case 12:
                return gamepad.dpad.up;

            case 13:
                return gamepad.dpad.down;

            case 14:
                return gamepad.dpad.left;

            case 15:
                return gamepad.dpad.right;

            default:
                return null;
        }
    }

    private Gamepad GetConsoleGamepad(
        int consoleNumber)
    {
        Gamepad gamepad = null;

        if (consoleNumber == 1)
        {
            gamepad = console1Gamepad;
        }
        else if (consoleNumber == 2)
        {
            gamepad = console2Gamepad;
        }

        return IsGamepadConnected(gamepad)
            ? gamepad
            : null;
    }

    // =========================================================
    // UI
    // =========================================================

    // Giữ hàm public cũ để các script khác không bị lỗi compile.
    // Toàn bộ logic UI thật sự đã được chuyển sang UIManager.
    public void RefreshInputInstructionUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance
                .RefreshControllerConnectionUI();
        }
    }

    private void UpdateCursor()
    {
        if (CursorManager.Instance == null)
            return;

        CursorManager.Instance
            .UpdateCursorByControllerState();
    }

    // =========================================================
    // INITIAL CONNECTION SOUND
    // =========================================================

    private void PlayInitialConnectedSound()
    {
        if (HasAnyController())
        {
            PlayConnectSound();
        }
    }

    // =========================================================
    // SOUND
    // =========================================================

    private void PlayConnectSound()
    {
        if (!allowControllerSounds ||
            AudioManager.Instance == null ||
            AudioManager.Instance
                .consoleControllerConect == null)
        {
            return;
        }

        AudioManager.Instance.PlayUI(
            AudioManager.Instance
                .consoleControllerConect
        );
    }

    private void PlayDisconnectSound()
    {
        if (!allowControllerSounds ||
            AudioManager.Instance == null ||
            AudioManager.Instance
                .consoleControllerDisConect == null)
        {
            return;
        }

        AudioManager.Instance.PlayUI(
            AudioManager.Instance
                .consoleControllerDisConect
        );
    }

    // =========================================================
    // NOTIFICATION
    // =========================================================

    private void ShowControllerNotification()
    {
        if (notificationCoroutine != null)
        {
            StopCoroutine(
                notificationCoroutine
            );
        }

        notificationCoroutine =
            StartCoroutine(
                NotificationRoutine()
            );
    }

    private IEnumerator NotificationRoutine()
    {
        yield return new WaitForSecondsRealtime(
            0.5f
        );

        notificationCoroutine = null;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void PrintControllerState()
    {

    }

    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool IsConsole1Connected()
    {
        return console1Connected &&
               IsGamepadConnected(
                   console1Gamepad
               );
    }

    public bool IsConsole2Connected()
    {
        return console2Connected &&
               IsGamepadConnected(
                   console2Gamepad
               );
    }

    public bool IsConsoleConnected(
        int consoleNumber)
    {
        if (consoleNumber == 1)
            return IsConsole1Connected();

        if (consoleNumber == 2)
            return IsConsole2Connected();

        return false;
    }

    public bool HasAnyController()
    {
        return IsConsole1Connected() ||
               IsConsole2Connected();
    }

    public int GetControllerCount()
    {
        int count = 0;

        if (IsConsole1Connected())
            count++;

        if (IsConsole2Connected())
            count++;

        return count;
    }

    public string GetConsole1Name()
    {
        return IsConsole1Connected()
            ? console1Name
            : "";
    }

    public string GetConsole2Name()
    {
        return IsConsole2Connected()
            ? console2Name
            : "";
    }

    // Tên hàm được giữ nguyên để code cũ không lỗi.
    // Giá trị trả về bây giờ là Input System deviceId,
    // không phải Legacy Joystick 1/2/3...
    public int GetConsole1JoystickIndex()
    {
        if (!IsConsole1Connected())
            return 0;

        return console1JoystickIndex;
    }

    public int GetConsole2JoystickIndex()
    {
        if (!IsConsole2Connected())
            return 0;

        return console2JoystickIndex;
    }

    public int GetConsoleJoystickIndex(
        int consoleNumber)
    {
        if (consoleNumber == 1)
        {
            return GetConsole1JoystickIndex();
        }

        if (consoleNumber == 2)
        {
            return GetConsole2JoystickIndex();
        }

        return 0;
    }

    public ControllerChangeType
        GetConsole1LastChange()
    {
        return console1LastChange;
    }

    public ControllerChangeType
        GetConsole2LastChange()
    {
        return console2LastChange;
    }

    public bool DidConsole1ControllerChange()
    {
        return console1LastChange ==
               ControllerChangeType
                   .ReplacedWithDifferentController;
    }

    public bool DidConsole2ControllerChange()
    {
        return console2LastChange ==
               ControllerChangeType
                   .ReplacedWithDifferentController;
    }
}