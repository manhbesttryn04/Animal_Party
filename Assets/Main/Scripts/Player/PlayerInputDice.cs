using UnityEngine;
using UnityEngine.Serialization;

public class PlayerInputDice : MonoBehaviour
{
    [Header("References")]
    public PlayerManager manager;

    [Header("Dice State")]
    public bool isClick = true;

    [Header("Dice Number Tool")]
    [Tooltip(
        "Bật tool chọn nhanh kết quả xúc xắc. " +
        "Tay cầm: X/Y/B. P1 bàn phím: hàng số 4/5/6. " +
        "P2 bàn phím: Numpad 4/5/6."
    )]
    [FormerlySerializedAs("enableControllerDiceTool")]
    [SerializeField]
    private bool enableDiceNumberTool = true;

    private PlayerDiceRoll playerDiceRoll;

    // Sau khi đóng Setting phải thả nút rồi mới nhận input lại.
    private bool waitReleaseAfterSetting;

    private void Start()
    {
        manager = GetComponent<PlayerManager>();
        playerDiceRoll = GetComponent<PlayerDiceRoll>();
    }

    private void Update()
    {
        HandleDiceInput();
    }

    // =========================================================
    // PLAYER
    // =========================================================

    private bool IsPlayer2()
    {
        return manager != null &&
               manager.playerType != null &&
               manager.playerType.isPlayer2;
    }

    // =========================================================
    // CONTROLLER
    // =========================================================

    private bool IsUsingController()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null)
            return false;

        return IsPlayer2()
            ? controller.IsConsole2Connected()
            : controller.IsConsole1Connected();
    }

    private int GetConsoleNumber()
    {
        return IsPlayer2() ? 2 : 1;
    }

    // =========================================================
    // DICE INPUT
    // =========================================================

    private bool IsDicePressed()
    {
        /*
         * Có tay cầm:
         * chỉ nhận Button 0 của đúng Console.
         */
        if (IsUsingController())
        {
            return ControllerManager.Instance
                .GetConsoleButtonDown(
                    GetConsoleNumber(),
                    0
                );
        }

        /*
         * Không có tay cầm:
         * mới nhận bàn phím.
         */
        if (IsPlayer2())
        {
            return Input.GetKeyDown(
                KeyCode.Keypad1
            );
        }

        return Input.GetKeyDown(
            KeyCode.J
        );
    }

    private bool IsDiceHeld()
    {
        /*
         * Dùng để kiểm tra người chơi đã thả nút
         * sau khi đóng Setting chưa.
         */
        if (IsUsingController())
        {
            ControllerManager controller =
                ControllerManager.Instance;

            int consoleNumber =
                GetConsoleNumber();

            bool normalDiceButtonHeld =
                controller.GetConsoleButton(
                    consoleNumber,
                    0
                );

            if (!enableDiceNumberTool)
                return normalDiceButtonHeld;

            // Sau khi đóng Setting bằng B, hoặc đang giữ X/Y,
            // phải thả nút trước khi tool được nhận input.
            return normalDiceButtonHeld ||
                   controller.GetConsoleButton(
                       consoleNumber,
                       1
                   ) ||
                   controller.GetConsoleButton(
                       consoleNumber,
                       2
                   ) ||
                   controller.GetConsoleButton(
                       consoleNumber,
                       3
                   );
        }

        if (IsPlayer2())
        {
            bool normalDiceKeyHeld =
                Input.GetKey(KeyCode.Keypad1);

            if (!enableDiceNumberTool)
                return normalDiceKeyHeld;

            return normalDiceKeyHeld ||
                   Input.GetKey(KeyCode.Keypad4) ||
                   Input.GetKey(KeyCode.Keypad5) ||
                   Input.GetKey(KeyCode.Keypad6);
        }

        bool player1NormalDiceKeyHeld =
            Input.GetKey(KeyCode.J);

        if (!enableDiceNumberTool)
            return player1NormalDiceKeyHeld;

        return player1NormalDiceKeyHeld ||
               Input.GetKey(KeyCode.Alpha4) ||
               Input.GetKey(KeyCode.Alpha5) ||
               Input.GetKey(KeyCode.Alpha6);
    }

    private int GetForcedDiceNumber()
    {
        if (!enableDiceNumberTool)
            return 0;

        /*
         * Có tay cầm:
         * chỉ đọc đúng Console của người chơi.
         * Tuyệt đối không đọc phím 4/5/6 của người chơi đó.
         */
        if (IsUsingController())
        {
            ControllerManager controller =
                ControllerManager.Instance;

            int consoleNumber =
                GetConsoleNumber();

            // Xbox X / PlayStation Square.
            if (controller.GetConsoleButtonDown(
                    consoleNumber,
                    2))
            {
                return 4;
            }

            // Xbox Y / PlayStation Triangle.
            if (controller.GetConsoleButtonDown(
                    consoleNumber,
                    3))
            {
                return 5;
            }

            // Xbox B / PlayStation Circle.
            if (controller.GetConsoleButtonDown(
                    consoleNumber,
                    1))
            {
                return 6;
            }

            return 0;
        }

        /*
         * Không có tay cầm:
         * Player 1 dùng hàng số phía trên bàn phím.
         * Player 2 dùng cụm phím Numpad.
         */
        if (IsPlayer2())
        {
            if (Input.GetKeyDown(KeyCode.Keypad4))
                return 4;

            if (Input.GetKeyDown(KeyCode.Keypad5))
                return 5;

            if (Input.GetKeyDown(KeyCode.Keypad6))
                return 6;

            return 0;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
            return 4;

        if (Input.GetKeyDown(KeyCode.Alpha5))
            return 5;

        if (Input.GetKeyDown(KeyCode.Alpha6))
            return 6;

        return 0;
    }

    // =========================================================
    // SETTING
    // =========================================================

    private bool IsSettingOpen()
    {
        return SettingManager.Instance != null &&
               SettingManager.Instance
                   .IsSettingBlockingInput;
    }

    // =========================================================
    // HANDLE INPUT
    // =========================================================

    private void HandleDiceInput()
    {
        /*
         * Setting đang mở:
         * khóa hoàn toàn input xúc xắc.
         */
        if (IsSettingOpen())
        {
            waitReleaseAfterSetting = true;
            return;
        }

        /*
         * Sau khi đóng Setting:
         * phải thả nút tung thường và các nút chọn số của tool.
         *
         * Điều này ngăn nút dùng để đóng Setting
         * kích hoạt luôn xúc xắc.
         */
        if (waitReleaseAfterSetting)
        {
            if (!IsDiceHeld())
            {
                waitReleaseAfterSetting = false;
            }

            return;
        }

        ExitsInput();
    }

    // =========================================================
    // DICE
    // =========================================================

    public void ExitsInput()
    {
        if (isClick)
            return;

        int forcedDiceNumber =
            GetForcedDiceNumber();

        bool dicePressed =
            forcedDiceNumber != 0 ||
            IsDicePressed();

        if (!dicePressed)
            return;

        if (manager == null ||
            manager.playerAnimator == null ||
            manager.playerAnimator.playerAnimator == null)
        {
            return;
        }

        if (forcedDiceNumber != 0)
        {
            if (playerDiceRoll == null)
                return;

            playerDiceRoll.ForceNextDiceNumber(
                forcedDiceNumber
            );
        }

        manager.playerAnimator
            .playerAnimator
            .SetTrigger("Dice");

        isClick = true;
    }
}