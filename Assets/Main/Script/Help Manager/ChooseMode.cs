using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChooseMode : MonoBehaviour
{
    [Header("Characters")]
    public List<GameObject> player1;
    public List<GameObject> player2;

    [Header("Choose State")]
    public List<GameObject> stateChooseP1;
    public List<GameObject> stateChooseP2;

    [Header("Character Buttons")]
    public List<Button> ListButtonChoose;

    [Header("Buttons Blocked By Setting")]
    [Tooltip(
        "Thêm tất cả Button thuộc màn hình chọn vào đây. " +
        "Không thêm Button của bảng Setting."
    )]
    [SerializeField]
    private List<Button> buttonsBlockedBySetting =
        new List<Button>();

    [Header("Direction Button Flash")]
    [Tooltip("Index 0 = Left, Index 1 = Right")]
    public List<Button> listButtonHighlightP1;

    [Tooltip("Index 0 = Left, Index 1 = Right")]
    public List<Button> listButtonHighlightP2;

    [SerializeField]
    private float buttonHighlightTime = 0.08f;

    [SerializeField]
    private float buttonPressedTime = 0.08f;

    [Header("Choose Status")]
    public bool isPlayer1Choose;
    public bool isPlayer2Choose;

    [Header("Input Settings")]
    [Range(0.1f, 1f)]
    public float inputThreshold = 0.5f;

    [Range(0f, 0.5f)]
    public float resetThreshold = 0.2f;

    [Header("Controller Horizontal Axis")]
    [Tooltip("Axis chỉ dành cho Joystick 1, không gán phím bàn phím.")]
    [SerializeField]
    private string horizontalJoystick1 =
        "HorizontalJoystick1";

    [Tooltip("Axis chỉ dành cho Joystick 2, không gán phím bàn phím.")]
    [SerializeField]
    private string horizontalJoystick2 =
        "HorizontalJoystick2";

    [Header("Start Game")]
    public GameObject buttonStart;
    public string sceneName;

    [Header("Input Instruction UI")]
    [Tooltip("Các UI hướng dẫn bàn phím dành riêng cho Player 1.")]
    [SerializeField]
    private List<GameObject> keyboardInstructP1List =
        new List<GameObject>();

    [Tooltip("Các UI hướng dẫn bàn phím dành riêng cho Player 2.")]
    [SerializeField]
    private List<GameObject> keyboardInstructP2List =
        new List<GameObject>();

    [Tooltip("Các UI hướng dẫn tay cầm dành riêng cho Player 1.")]
    [SerializeField]
    private List<GameObject> consoleInstructP1List =
        new List<GameObject>();

    [Tooltip("Các UI hướng dẫn tay cầm dành riêng cho Player 2.")]
    [SerializeField]
    private List<GameObject> consoleInstructP2List =
        new List<GameObject>();

    [Tooltip(
        "UI hướng dẫn nút Start bằng tay cầm. " +
        "Hiện khi P1 hoặc P2 có tay cầm."
    )]
    [SerializeField]
    private GameObject startControllerInstruction;

    [Tooltip(
        "GameObject cha chứa toàn bộ UI nằm giữa hai người chơi. " +
        "GameObject này sẽ ẩn khi lệnh Start được chấp nhận."
    )]
    [SerializeField]
    private GameObject middlePlayersUIRoot;

    private int indexP1;
    private int indexP2;

    // Ngăn analog giữ liên tục làm đổi nhiều lần.
    private bool canMoveP1 = true;
    private bool canMoveP2 = true;

    // Sau khi đóng Setting phải thả input.
    private bool waitReleaseAfterSetting;

    // Lưu trạng thái cũ để không bật lại những Button vốn đã bị khóa.
    private readonly List<bool> buttonInteractableBeforeSetting =
        new List<bool>();

    private bool areChooseButtonsBlockedBySetting;

    private bool isStartingGame;
    private bool waitStartButtonRelease;

    [Header("Start Button Animation")]
    [SerializeField]
    private float startButtonAnimationTime = 0.2f;

    private bool canPressStartButton;
    private Coroutine startButtonEnableRoutine;

    // Cache trạng thái để chỉ cập nhật UI khi tay cầm thay đổi.
    private bool previousP1ControllerConnected;
    private bool previousP2ControllerConnected;
    private bool inputInstructionInitialized;

    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        UpdatePlayer1();
        UpdatePlayer2();
        CheckStartButton();
        UpdateInputInstructionUI(true);

        CursorManager cursor =
            CursorManager.Instance;

        if (cursor != null)
        {
            cursor.UpdateCursorByControllerState();
            cursor.SetSceneCursorVisible(true);
        }

        var audio = AudioManager.Instance;
        if (audio != null)
        {
          //  audio.SetupMainGameAudio();
            audio.PlayMusic(
                audio.musicChooseSceneClip
            );

            audio.PlayEnvironment(
                audio.theSeaClip
            );
        }

        var ui = UIManager.Instance;
        if (ui != null &&
            ui.openSettingPanelButton != null)
        {
            ui.openSettingPanelButton
                .SetActive(true);
            ui.isShowKeyBoard = true;
        }

        var setting = SettingManager.Instance;
        if (setting != null)
        {
            setting.isOpenExitButton = true;
            setting.canOpenSettingByController = true;
        }
    }

    private void Update()
    {
        // Luôn cập nhật trước khi return vì Setting đang mở.
        // Nhờ vậy rút/cắm tay cầm trong Setting vẫn đổi hướng dẫn ngay.
        UpdateInputInstructionUI();

        bool settingOpen =
            SettingManager.Instance != null &&
            SettingManager.Instance.IsSettingBlockingInput;

        UpdateChooseButtonsSettingLock(settingOpen);
        UpdateStartButtonBySetting(settingOpen);

        // Setting đang mở thì khóa ChooseMode.
        if (settingOpen)
        {
            waitReleaseAfterSetting = true;

            canMoveP1 = false;
            canMoveP2 = false;

            return;
        }

        /*
         * Sau khi đóng Setting phải:
         * - Thả analog
         * - Thả phím di chuyển
         * - Thả nút xác nhận
         */
        if (waitReleaseAfterSetting)
        {
            if (IsChooseInputReleased())
            {
                waitReleaseAfterSetting = false;

                canMoveP1 = true;
                canMoveP2 = true;
            }

            return;
        }

        MoveChoosePlayer1();
        MoveChoosePlayer2();
        CheckStartInput();
    }

    // =========================================================
    // SETTING BUTTON LOCK
    // =========================================================

    private void UpdateChooseButtonsSettingLock(bool settingOpen)
    {
        // Chỉ chạy một lần khi trạng thái Setting thay đổi.
        if (areChooseButtonsBlockedBySetting == settingOpen)
        {
            return;
        }

        areChooseButtonsBlockedBySetting = settingOpen;

        if (settingOpen)
        {
            SaveAndDisableChooseButtons();
        }
        else
        {
            RestoreChooseButtons();
        }
    }

    private void SaveAndDisableChooseButtons()
    {
        if (buttonsBlockedBySetting == null)
        {
            return;
        }

        for (int i = 0; i < buttonsBlockedBySetting.Count; i++)
        {
            Button button = buttonsBlockedBySetting[i];

            if (button == null)
            {
                continue;
            }

            // Mở Setting: khóa Button và hiện trạng thái Disabled.
            button.interactable = false;
            SetButtonDisabledVisual(button);
        }
    }

    private void RestoreChooseButtons()
    {
        if (buttonsBlockedBySetting == null)
        {
            return;
        }

        for (int i = 0; i < buttonsBlockedBySetting.Count; i++)
        {
            Button button = buttonsBlockedBySetting[i];

            if (button == null)
            {
                continue;
            }

            bool keepDisabled =
                IsChosenPlayerButton(button);

            if (keepDisabled)
            {
                // Người chơi đã chọn xong:
                // sau khi đóng Setting vẫn phải khóa và giữ Disabled.
                button.interactable = false;
                SetButtonDisabledVisual(button);
            }
            else
            {
                // Nút chưa chọn xong:
                // mở lại và trở về Normal.
                button.interactable = true;
                ResetButtonToNormal(button);
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private bool IsChosenPlayerButton(Button button)
    {
        if (button == null)
        {
            return false;
        }

        bool belongsToPlayer1 =
            (listButtonHighlightP1 != null &&
             listButtonHighlightP1.Contains(button)) ||
            (ListButtonChoose != null &&
             ListButtonChoose.Count > 0 &&
             ListButtonChoose[0] == button);

        bool belongsToPlayer2 =
            (listButtonHighlightP2 != null &&
             listButtonHighlightP2.Contains(button)) ||
            (ListButtonChoose != null &&
             ListButtonChoose.Count > 1 &&
             ListButtonChoose[1] == button);

        return (belongsToPlayer1 && isPlayer1Choose) ||
               (belongsToPlayer2 && isPlayer2Choose);
    }

    private void DisablePlayerHighlightButtons(
        List<Button> buttonList)
    {
        if (buttonList == null)
        {
            return;
        }

        for (int i = 0; i < buttonList.Count; i++)
        {
            Button button = buttonList[i];

            if (button == null)
            {
                continue;
            }

            button.interactable = false;
            SetButtonDisabledVisual(button);
        }
    }

    private void DisableChooseButton(int playerIndex)
    {
        if (ListButtonChoose == null ||
            playerIndex < 0 ||
            playerIndex >= ListButtonChoose.Count)
        {
            return;
        }

        Button button = ListButtonChoose[playerIndex];

        if (button == null)
        {
            return;
        }

        button.interactable = false;
        SetButtonDisabledVisual(button);
    }

    private void SetButtonDisabledVisual(Button button)
    {
        Image image = GetButtonImage(button);

        if (image == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        SpriteState sprites = button.spriteState;

        // Dùng đồng thời Disabled Color và Disabled Sprite.
        image.color = colors.disabledColor;

        if (sprites.disabledSprite != null)
        {
            image.overrideSprite = sprites.disabledSprite;
        }

        image.SetAllDirty();
    }

    private void ResetButtonToNormal(Button button)
    {
        if (button == null)
            return;

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem != null)
        {
            PointerEventData pointerData =
                new PointerEventData(eventSystem)
                {
                    button = PointerEventData.InputButton.Left
                };

            BaseEventData baseEventData =
                new BaseEventData(eventSystem);

            button.OnPointerUp(pointerData);
            button.OnPointerExit(pointerData);
            button.OnDeselect(baseEventData);

            if (eventSystem.currentSelectedGameObject ==
                button.gameObject)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        Image image = GetButtonImage(button);

        if (image == null)
            return;

        ColorBlock colors = button.colors;

        // Trả sprite về Source Image ban đầu.
        image.overrideSprite = null;

        // Image Color vẫn giữ trắng.
        image.color = Color.white;

        // Chỉ đổi màu hiển thị của Button sang Normal Color vàng.
        image.CrossFadeColor(
            colors.normalColor * colors.colorMultiplier,
            0f,
            true,
            true
        );

        image.SetAllDirty();
    }

    private Image GetButtonImage(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Image image = button.targetGraphic as Image;

        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        return image;
    }

    // =========================================================
    // CONTROLLER STATE
    // =========================================================

    private bool IsPlayer1UsingController()
    {
        return ControllerManager.Instance != null &&
               ControllerManager.Instance
                   .IsConsole1Connected();
    }

    private bool IsPlayer2UsingController()
    {
        return ControllerManager.Instance != null &&
               ControllerManager.Instance
                   .IsConsole2Connected();
    }

    // =========================================================
    // INPUT INSTRUCTION UI
    // =========================================================

    private void UpdateInputInstructionUI(bool force = false)
    {
        bool p1ControllerConnected =
            IsPlayer1UsingController();

        bool p2ControllerConnected =
            IsPlayer2UsingController();

        if (!force &&
            inputInstructionInitialized &&
            p1ControllerConnected ==
                previousP1ControllerConnected &&
            p2ControllerConnected ==
                previousP2ControllerConnected)
        {
            return;
        }

        previousP1ControllerConnected =
            p1ControllerConnected;

        previousP2ControllerConnected =
            p2ControllerConnected;

        inputInstructionInitialized = true;

        // Mỗi Player đổi hướng dẫn độc lập.
        SetInstructionListActive(
            keyboardInstructP1List,
            !p1ControllerConnected
        );

        SetInstructionListActive(
            consoleInstructP1List,
            p1ControllerConnected
        );

        SetInstructionListActive(
            keyboardInstructP2List,
            !p2ControllerConnected
        );

        SetInstructionListActive(
            consoleInstructP2List,
            p2ControllerConnected
        );

        // Chỉ cần một trong hai Player còn tay cầm là hiện hướng dẫn Start.
        // Khi START đã được chấp nhận thì phải giữ ẩn trong suốt lúc loading,
        // kể cả khi người chơi cắm/rút tay cầm.
        if (startControllerInstruction != null)
        {
            startControllerInstruction.SetActive(
                !isStartingGame &&
                (p1ControllerConnected ||
                 p2ControllerConnected)
            );
        }
    }

    private void HideStartControllerInstruction()
    {
        if (startControllerInstruction != null)
        {
            startControllerInstruction.SetActive(false);
        }
    }

    private void SetInstructionListActive(
        List<GameObject> instructionList,
        bool active)
    {
        if (instructionList == null)
            return;

        for (int i = 0;
             i < instructionList.Count;
             i++)
        {
            GameObject instruction =
                instructionList[i];

            if (instruction != null &&
                instruction.activeSelf != active)
            {
                instruction.SetActive(active);
            }
        }
    }

    // =========================================================
    // PLAYER 1 INPUT
    // =========================================================

    private float GetPlayer1MoveInput()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        /*
         * Có Console 1:
         * chỉ nhận Horizontal của Console 1.
         * Không đọc A/D.
         */
        if (IsPlayer1UsingController())
        {
            return controller.GetConsoleHorizontalRaw(
                1,
                horizontalJoystick1,
                horizontalJoystick2
            );
        }

        /*
         * Không có Console 1:
         * mới cho Player 1 dùng A/D.
         */
        if (Input.GetKey(KeyCode.A))
            return -1f;

        if (Input.GetKey(KeyCode.D))
            return 1f;

        return 0f;
    }

    private bool IsPlayer1ConfirmPressed()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        // Có tay cầm thì không nhận J.
        if (IsPlayer1UsingController())
        {
            return controller.GetConsoleButtonDown(
                1,
                0
            );
        }

        return Input.GetKeyDown(KeyCode.J);
    }

    private bool IsPlayer1ConfirmHeld()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        // Có tay cầm thì không kiểm tra J.
        if (IsPlayer1UsingController())
        {
            return controller.GetConsoleButton(
                1,
                0
            );
        }

        return Input.GetKey(KeyCode.J);
    }

    // =========================================================
    // PLAYER 2 INPUT
    // =========================================================

    private float GetPlayer2MoveInput()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        /*
         * Có Console 2:
         * chỉ nhận Horizontal của Console 2.
         * Không đọc phím mũi tên.
         */
        if (IsPlayer2UsingController())
        {
            return controller.GetConsoleHorizontalRaw(
                2,
                horizontalJoystick1,
                horizontalJoystick2
            );
        }

        /*
         * Không có Console 2:
         * mới cho Player 2 dùng mũi tên.
         */
        if (Input.GetKey(KeyCode.LeftArrow))
            return -1f;

        if (Input.GetKey(KeyCode.RightArrow))
            return 1f;

        return 0f;
    }

    private bool IsPlayer2ConfirmPressed()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        // Có tay cầm thì không nhận Keypad1.
        if (IsPlayer2UsingController())
        {
            return controller.GetConsoleButtonDown(
                2,
                0
            );
        }

        return Input.GetKeyDown(KeyCode.Keypad1);
    }

    private bool IsPlayer2ConfirmHeld()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        // Có tay cầm thì không kiểm tra Keypad1.
        if (IsPlayer2UsingController())
        {
            return controller.GetConsoleButton(
                2,
                0
            );
        }

        return Input.GetKey(KeyCode.Keypad1);
    }

    // =========================================================
    // RELEASE INPUT AFTER SETTING
    // =========================================================

    private bool IsChooseInputReleased()
    {
        float moveP1 =
            GetPlayer1MoveInput();

        float moveP2 =
            GetPlayer2MoveInput();

        bool movementReleased =
            Mathf.Abs(moveP1) <= resetThreshold &&
            Mathf.Abs(moveP2) <= resetThreshold;

        bool confirmReleased =
            !IsPlayer1ConfirmHeld() &&
            !IsPlayer2ConfirmHeld();

        return movementReleased &&
               confirmReleased;
    }

    // =========================================================
    // CHOOSE INPUT
    // =========================================================

    public void MoveChoosePlayer1()
    {
        if (isPlayer1Choose)
            return;

        float horizontal =
            GetPlayer1MoveInput();

        // Trả analog hoặc phím về giữa.
        if (Mathf.Abs(horizontal) <= resetThreshold)
        {
            canMoveP1 = true;
        }

        if (canMoveP1)
        {
            if (horizontal <= -inputThreshold)
            {
                PrevPlayer1();
                canMoveP1 = false;
            }
            else if (horizontal >= inputThreshold)
            {
                NextPlayer1();
                canMoveP1 = false;
            }
        }

        if (IsPlayer1ConfirmPressed())
        {
            ChoosePlayer1();
        }
    }

    public void MoveChoosePlayer2()
    {
        if (isPlayer2Choose)
            return;

        float horizontal =
            GetPlayer2MoveInput();

        // Trả analog hoặc phím về giữa.
        if (Mathf.Abs(horizontal) <= resetThreshold)
        {
            canMoveP2 = true;
        }

        if (canMoveP2)
        {
            if (horizontal <= -inputThreshold)
            {
                PrevPlayer2();
                canMoveP2 = false;
            }
            else if (horizontal >= inputThreshold)
            {
                NextPlayer2();
                canMoveP2 = false;
            }
        }

        if (IsPlayer2ConfirmPressed())
        {
            ChoosePlayer2();
        }
    }

    // =========================================================
    // PLAYER 1
    // =========================================================

    public void PrevPlayer1()
    {
        if (isPlayer1Choose ||
            player1 == null ||
            player1.Count == 0)
        {
            return;
        }

        PlayMoveSound();

        indexP1--;

        if (indexP1 < 0)
        {
            indexP1 = player1.Count - 1;
        }

        FlashDirectionButton(
            listButtonHighlightP1,
            0
        );

        UpdatePlayer1();
    }

    public void NextPlayer1()
    {
        if (isPlayer1Choose ||
            player1 == null ||
            player1.Count == 0)
        {
            return;
        }

        PlayMoveSound();

        indexP1++;

        if (indexP1 >= player1.Count)
        {
            indexP1 = 0;
        }

        FlashDirectionButton(
            listButtonHighlightP1,
            1
        );

        UpdatePlayer1();
    }

    public void ChoosePlayer1()
    {
        if (isPlayer1Choose ||
            player1 == null ||
            player1.Count == 0)
        {
            return;
        }

        // Không cho P1 chọn trùng nhân vật mà P2 đã chọn.
        // Nếu cả hai bấm cùng một frame, P1 được xử lý trước
        // vì MoveChoosePlayer1() chạy trước MoveChoosePlayer2().
        if (isPlayer2Choose && indexP1 == indexP2)
        {
            PlayCannotChooseSound();
            FlashDirectionButton(ListButtonChoose, 0);
            return;
        }

        PlaySalute(player1[indexP1]);
        PlayChooseSound();

        isPlayer1Choose = true;

        // P1 chọn xong bằng bàn phím hoặc tay cầm:
        // khóa toàn bộ Button Highlight của P1.
        StartCoroutine(DisPlayerChoose(0, listButtonHighlightP1));

        if (stateChooseP1 != null &&
            stateChooseP1.Count > 1)
        {
            if (stateChooseP1[0] != null)
                stateChooseP1[0].SetActive(false);

            if (stateChooseP1[1] != null)
                stateChooseP1[1].SetActive(true);
        }

        if (SendIndexCharacter.Instance != null)
        {
            SendIndexCharacter.Instance.player1Index =
                indexP1;
        }

        CheckStartButton();
    }
public IEnumerator DisPlayerChoose(int index, List<Button> listButtonChoose)
    {
        FlashDirectionButton(ListButtonChoose, index);
        yield return new WaitForSeconds(0.2f);
        DisablePlayerHighlightButtons(listButtonChoose);
        DisableChooseButton(index);
    }
    // =========================================================
    // PLAYER 2
    // =========================================================

    public void PrevPlayer2()
    {
        if (isPlayer2Choose ||
            player2 == null ||
            player2.Count == 0)
        {
            return;
        }

        PlayMoveSound();

        indexP2--;

        if (indexP2 < 0)
        {
            indexP2 = player2.Count - 1;
        }

        FlashDirectionButton(
            listButtonHighlightP2,
            0
        );

        UpdatePlayer2();
    }

    public void NextPlayer2()
    {
        if (isPlayer2Choose ||
            player2 == null ||
            player2.Count == 0)
        {
            return;
        }

        PlayMoveSound();

        indexP2++;

        if (indexP2 >= player2.Count)
        {
            indexP2 = 0;
        }

        FlashDirectionButton(
            listButtonHighlightP2,
            1
        );

        UpdatePlayer2();
    }

    public void ChoosePlayer2()
    {
        if (isPlayer2Choose ||
            player2 == null ||
            player2.Count == 0)
        {
            return;
        }

        // Không cho P2 chọn trùng nhân vật mà P1 đã chọn.
        // Trường hợp cả hai bấm cùng một frame và cùng index:
        // P1 chọn thành công, P2 sẽ bị từ chối tại đây.
        if (isPlayer1Choose && indexP2 == indexP1)
        {
            PlayCannotChooseSound();
            FlashDirectionButton(ListButtonChoose, 1);
            return;
        }

        PlaySalute(player2[indexP2]);
        PlayChooseSound();

        isPlayer2Choose = true;

        // P2 chọn xong bằng bàn phím hoặc tay cầm:
        StartCoroutine(DisPlayerChoose(1, listButtonHighlightP2));

        if (stateChooseP2 != null &&
            stateChooseP2.Count > 1)
        {
            if (stateChooseP2[0] != null)
                stateChooseP2[0].SetActive(false);

            if (stateChooseP2[1] != null)
                stateChooseP2[1].SetActive(true);
        }

        if (SendIndexCharacter.Instance != null)
        {
            SendIndexCharacter.Instance.player2Index =
                indexP2;
        }

        CheckStartButton();
    }

    // =========================================================
    // MOUSE BUTTONS
    // =========================================================

    public void SelectCharacterP1(int index)
    {
        if (isPlayer1Choose)
            return;

        if (player1 == null ||
            index < 0 ||
            index >= player1.Count)
        {
            return;
        }

        indexP1 = index;
        UpdatePlayer1();
    }

    public void SelectCharacterP2(int index)
    {
        if (isPlayer2Choose)
            return;

        if (player2 == null ||
            index < 0 ||
            index >= player2.Count)
        {
            return;
        }

        indexP2 = index;
        UpdatePlayer2();
    }

    // =========================================================
    // UPDATE CHARACTER
    // =========================================================

    private void UpdatePlayer1()
    {
        if (player1 == null)
            return;

        for (int i = 0; i < player1.Count; i++)
        {
            if (player1[i] != null)
            {
                player1[i].SetActive(
                    i == indexP1
                );
            }
        }
    }

    private void UpdatePlayer2()
    {
        if (player2 == null)
            return;

        for (int i = 0; i < player2.Count; i++)
        {
            if (player2[i] != null)
            {
                player2[i].SetActive(
                    i == indexP2
                );
            }
        }
    }

    // =========================================================
    // START BUTTON
    // =========================================================

    private void UpdateStartButtonBySetting(bool settingOpen)
    {
        if (buttonStart == null)
        {
            return;
        }

        Button btn = buttonStart.GetComponent<Button>();

        // Đã bấm START:
        // luôn hiện, khóa tương tác và giữ Disabled Color.
        if (isStartingGame)
        {
            StopStartButtonEnableRoutine();
            canPressStartButton = false;

            buttonStart.SetActive(true);

            if (btn != null)
            {
                btn.interactable = false;
                SetButtonDisabledVisual(btn);
            }

            return;
        }

        // Setting đang mở:
        // ẩn START và hủy thời gian chờ hiện tại.
        if (settingOpen)
        {
            HideStartButton();
            return;
        }

        bool bothSelected =
            isPlayer1Choose &&
            isPlayer2Choose;

        if (!bothSelected)
        {
            HideStartButton();
            return;
        }

        // START vừa xuất hiện -> chạy animation trước.
        if (!buttonStart.activeSelf)
        {
            ShowStartButtonWithDelay();
            return;
        }

        // Không ép visual về Normal mỗi frame.
        // Chỉ cập nhật khả năng tương tác.
        if (btn != null)
        {
            btn.interactable = canPressStartButton;
        }
    }

    private void CheckStartButton()
    {
        bool bothSelected =
            isPlayer1Choose &&
            isPlayer2Choose;

        if (bothSelected)
        {
            ShowStartButtonWithDelay();

            /*
             * Không cho lần nhấn chọn nhân vật cuối
             * kích hoạt luôn nút Start.
             */
            waitStartButtonRelease = true;
        }
        else
        {
            HideStartButton();
        }
    }

    private void ShowStartButtonWithDelay()
    {
        if (buttonStart == null)
            return;

        // Nếu START đã hiện và đã sẵn sàng thì không chạy lại delay.
        if (buttonStart.activeSelf && canPressStartButton)
            return;

        buttonStart.SetActive(true);
        canPressStartButton = false;

        Button btn = buttonStart.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = false;
        }

        StopStartButtonEnableRoutine();
        startButtonEnableRoutine =
            StartCoroutine(EnableStartButtonAfterAnimation());
    }

    private IEnumerator EnableStartButtonAfterAnimation()
    {
        // Realtime để vẫn đúng 0.2 giây kể cả Time.timeScale thay đổi.
        yield return new WaitForSecondsRealtime(
            startButtonAnimationTime
        );

        startButtonEnableRoutine = null;

        bool settingOpen =
            SettingManager.Instance != null &&
            SettingManager.Instance.IsSettingBlockingInput;

        bool canEnable =
            !settingOpen &&
            !isStartingGame &&
            isPlayer1Choose &&
            isPlayer2Choose &&
            buttonStart != null &&
            buttonStart.activeSelf;

        if (!canEnable)
        {
            canPressStartButton = false;
            yield break;
        }

        canPressStartButton = true;

        Button btn = buttonStart.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
        }
    }

    private void HideStartButton()
    {
        StopStartButtonEnableRoutine();
        canPressStartButton = false;

        if (buttonStart != null)
        {
            Button btn = buttonStart.GetComponent<Button>();

            if (btn != null)
            {
                btn.interactable = false;
            }

            buttonStart.SetActive(false);
        }
    }

    private void StopStartButtonEnableRoutine()
    {
        if (startButtonEnableRoutine != null)
        {
            StopCoroutine(startButtonEnableRoutine);
            startButtonEnableRoutine = null;
        }
    }

    private void CheckStartInput()
    {
        if (!isPlayer1Choose || !isPlayer2Choose)
            return;

        if (isStartingGame)
            return;

        // Nếu nút Start đang chạy animation xuất hiện (0.2s) thì chưa nhận input
        if (!canPressStartButton)
            return;

        /*
         * Xử lý trường hợp người chơi bấm giữ phím từ lúc chọn nhân vật.
         * Yêu cầu nhả toàn bộ phím chọn ra trước khi nhận lệnh Start.
         */
        if (waitStartButtonRelease)
        {
            if (!IsPlayer1ConfirmHeld() && !IsPlayer2ConfirmHeld() && !Input.GetKey(KeyCode.A))
            {
                waitStartButtonRelease = false;
            }
            return;
        }

        // Kiểm tra tín hiệu bấm Start từ P1 hoặc P2
        bool p1Pressed = IsStartPressedP1();
        bool p2Pressed = IsStartPressedP2();

        if (p1Pressed || p2Pressed)
        {
            TriggerStartButton();
        }
    }

    private bool IsStartPressedP1()
    {
        ControllerManager controller = ControllerManager.Instance;

        // P1 có tay cầm -> Đọc Button 0 của Tay cầm 1
        if (IsPlayer1UsingController())
        {
            return controller.GetConsoleButtonDown(1, 0);
        }

        // P1 dùng bàn phím -> Bấm A hoặc J
        return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.J);
    }

    private bool IsStartPressedP2()
    {
        ControllerManager controller = ControllerManager.Instance;

        // P2 có tay cầm -> Đọc Button 0 của Tay cầm 2
        if (IsPlayer2UsingController())
        {
            return controller.GetConsoleButtonDown(2, 0);
        }

        // P2 dùng bàn phím -> Bấm Keypad1 (hoặc bạn có thể bổ sung phím P2 tùy chọn)
        return Input.GetKeyDown(KeyCode.Keypad1);
    }

    private void TriggerStartButton()
    {
        if (buttonStart == null) return;

        Button btn = buttonStart.GetComponent<Button>();
        if (btn != null && btn.interactable)
        {
            StartCoroutine(SimulateStartButtonPressed(btn));
        }
    }

    private IEnumerator SimulateStartButtonPressed(Button btn)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left
        };

        // 1. Kích hoạt hiệu ứng visual Pressed (đổi màu/sprite pressed)
        btn.OnPointerDown(pointerData);

        // 2. Chờ thời gian buttonPressedTime (0.08s) đã khai báo trong Inspector
        yield return new WaitForSecondsRealtime(0.1f);

        // 3. Trả nút về trạng thái Normal
        btn.OnPointerUp(pointerData);

        // 4. Kích hoạt sự kiện onClick của Button
        btn.onClick.Invoke();
    }

    // =========================================================
    // LOAD SCENE
    // =========================================================

    public void LoadScene(int buildIndex)
    {
        // Không cho bất kỳ nguồn input nào kích START
        // trước khi animation 0.2 giây chạy xong.
        if (!canPressStartButton)
            return;

        if (!isPlayer1Choose ||
            !isPlayer2Choose)
        {
            return;
        }

        if (isStartingGame)
            return;

        isStartingGame = true;

        // START hợp lệ và đã bắt đầu tải màn:
        // ẩn ngay hướng dẫn nút Start của tay cầm.
        HideStartControllerInstruction();

        // Ẩn toàn bộ cụm UI nằm giữa Player 1 và Player 2.
        if (middlePlayersUIRoot != null)
        {
            middlePlayersUIRoot.SetActive(false);
        }

        if (UIManager.Instance != null &&
            UIManager.Instance.exitMainMenuButton != null)
        {
            UIManager.Instance
                .exitMainMenuButton
                .SetActive(false);
        }

        if (SettingManager.Instance != null)
        {
            SettingManager.Instance.isOpenExitButton =
                false;
        }

        StartCoroutine(
            LoadSceneDelay(buildIndex)
        );
    }

    private IEnumerator LoadSceneDelay(
        int buildIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance
                    .startGameButtonClickClip
            );
        }

        if (buttonStart != null)
        {
            Button btn =
                buttonStart.GetComponent<Button>();

            TextMeshProUGUI text =
                buttonStart.GetComponentInChildren<
                    TextMeshProUGUI
                >();

            if (btn != null)
            {
                btn.interactable = false;
                SetButtonDisabledVisual(btn);
            }

            if (text != null)
            {
                text.fontSize = 30;

                float timer = 0f;

                while (timer < 7f)
                {
                    text.text = "LOADING";
                    yield return new WaitForSeconds(0.5f);

                    text.text = "LOADING.";
                    yield return new WaitForSeconds(0.5f);

                    text.text = "LOADING..";
                    yield return new WaitForSeconds(0.5f);

                    text.text = "LOADING...";
                    yield return new WaitForSeconds(0.5f);

                    timer += 2f;
                }
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseAudio();
        }

        CursorManager cursor =
            CursorManager.Instance;

        if (cursor != null)
        {
            cursor.HideGameCursor();
        }

        if (SettingManager.Instance != null &&
            UIManager.Instance != null)
        {
            SettingManager.Instance.ResetSetting();
            SettingManager.Instance.canOpenSettingByController = false;

            if (UIManager.Instance
                    .openSettingPanelButton != null)
            {
                UIManager.Instance
                    .openSettingPanelButton
                    .SetActive(false);
            }
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

        if (LoadingManager.Instance != null)
        {     
            yield return StartCoroutine(
                LoadingManager.Instance
                    .ShowLoading()
            );
        }

        SceneManager.LoadScene("CutScene 1");
    }

    // =========================================================
    // EFFECTS
    // =========================================================

    private void FlashDirectionButton(
        List<Button> buttonList,
        int buttonIndex)
    {
        if (buttonList == null ||
            buttonIndex < 0 ||
            buttonIndex >= buttonList.Count)
        {
            return;
        }

        Button button = buttonList[buttonIndex];

        if (button == null ||
            !button.IsInteractable())
        {
            return;
        }

        StartCoroutine(
            FlashButtonRoutine(button)
        );
    }

    private IEnumerator FlashButtonRoutine(
        Button button)
    {
        if (button == null)
            yield break;

        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
            yield break;

        BaseEventData selectData =
            new BaseEventData(eventSystem);

        PointerEventData pointerData =
            new PointerEventData(eventSystem)
            {
                button =
                    PointerEventData.InputButton.Left
            };

        // Highlight một lần.
        button.OnSelect(selectData);

        yield return new WaitForSecondsRealtime(
            buttonHighlightTime
        );

        if (button == null)
            yield break;

        // Pressed một lần, chỉ chạy hiệu ứng hình ảnh.
        // Không gọi OnPointerClick nên không kích hoạt onClick lần nữa.
        button.OnPointerDown(pointerData);

        yield return new WaitForSecondsRealtime(
            buttonPressedTime
        );

        if (button == null)
            yield break;

        button.OnPointerUp(pointerData);

        yield return null;

        if (button != null)
        {
            button.OnDeselect(selectData);
        }
    }

    private void PlaySalute(
        GameObject character)
    {
        if (character == null)
            return;

        Animator animator =
            character.GetComponent<Animator>();

        if (animator != null)
        {
            animator.SetTrigger("Salute");
        }
    }

    private void PlayMoveSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance
                    .movechooseItemClip
            );
        }
    }

    private void PlayChooseSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance
                    .doneChooseClickClip
            );
        }
    }

    private void PlayCannotChooseSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance
                    .noCoinBuyItemClip
            );
        }
    }
}