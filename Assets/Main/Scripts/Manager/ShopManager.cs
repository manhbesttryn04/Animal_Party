using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ShopManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    #region Singleton

    public static ShopManager _instance;
    public static ShopManager Instance => _instance;

    #endregion

    // =========================================================
    // INSPECTOR DATA
    // =========================================================

    #region Inspector

    [Header("Manager References")]
    public UIManager ui;
    public InputChooseItem inputChooseItem;

    [Header("Item Data")]
    public Sprite[] itemSprites;

    [Header("Players")]
    public PlayerManager[] players;

    [Header("Turn Timer")]
    public int timePerTurn = 20;

    [Header("Shop Settings")]
    public bool open = true;

    [Header("Setting Input Lock")]
    [Tooltip(
        "Các Button của Shop sẽ bị vô hiệu hóa khi Setting mở.\n" +
        "Để trống danh sách: ShopManager tự tìm Button trong Shop Panel " +
        "và Canvas Random Card."
    )]
    [SerializeField]
    private List<Button> buttonsBlockedBySetting =
        new List<Button>();

    [Range(0f, 0.5f)]
    [SerializeField] private float inputReleaseThreshold = 0.2f;

    [Tooltip("Axis X chỉ dành cho Joystick 1.")]
    [SerializeField]
    private string horizontalJoystick1 =
        "HorizontalJoystick1";

    [Tooltip("Axis X chỉ dành cho Joystick 2.")]
    [SerializeField]
    private string horizontalJoystick2 =
        "HorizontalJoystick2";

    private bool[] canErrorCoin = { true, true };

    #endregion

    // =========================================================
    // PRIVATE VARIABLES
    // =========================================================

    #region Private Variables

    private Coroutine turnTimerCoroutine;
    private int currentTime;

    private bool previousSettingBlockingInput;
    private bool shopInputLockedBySetting;
    private bool inputChooseItemWasEnabled;

    private Coroutine restoreShopInputCoroutine;

    private readonly Dictionary<Button, bool>
        buttonInteractableBeforeSetting =
            new Dictionary<Button, bool>();

    #endregion

    // =========================================================
    // UNITY METHODS
    // =========================================================

    #region Unity Methods

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        ui = UIManager.Instance;

        CacheButtonsBlockedBySetting();

        if (open)
        {
            Open();
        }
    }

    private void Update()
    {
        bool settingBlockingInput =
            IsSettingBlockingShopInput();

        if (settingBlockingInput ==
            previousSettingBlockingInput)
        {
            return;
        }

        previousSettingBlockingInput =
            settingBlockingInput;

        if (settingBlockingInput)
        {
            LockShopInputForSetting();
        }
        else
        {
            BeginRestoreShopInputAfterSetting();
        }
    }

    #endregion

    // =========================================================
    // OPEN SHOP FLOW
    // =========================================================

    #region Open Shop Flow

    public void Open()
    {
        SetupAudio();
        SetupSetting();
        SetupPlayers();
        SetupUI();
        OpenShop();
    }

    private void SetupAudio()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.openShopClip);
    }

    private void SetupSetting()
    {
        var setting = SettingManager.Instance;
        if (setting != null)
        {
            setting.canOpenSettingByController = true;
        }

        if (ui != null && ui.openSettingPanelButton != null)
        {
            ui.openSettingPanelButton.SetActive(true);
        }
    }


    #endregion

    // =========================================================
    // SETUP
    // =========================================================

    #region Setup

    private void SetupPlayers()
    {
        if (players == null || players.Length < 2)
            players = new PlayerManager[2];

        GameObject p1 = GameObject.FindGameObjectWithTag("Player 1");

        if (p1 != null)
            players[0] = p1.GetComponent<PlayerManager>();

        GameObject p2 = GameObject.FindGameObjectWithTag("Player 2");

        if (p2 != null)
            players[1] = p2.GetComponent<PlayerManager>();

        GameManager.Instance.ResetBuffAllPlayer();
    }

    private void SetupUI()
    {
        UpdateCoin();

        foreach (Image img in ui.playerItemImagesList)
        {
            if (img != null)
                img.gameObject.SetActive(false);
        }

        ui.shopPanel?.SetActive(false);

        if (ui.canvasRandomCard != null)
            ui.canvasRandomCard.SetActive(false);

        if (ui.timerShopText != null)
            ui.timerShopText.text = timePerTurn.ToString();
    }

    #endregion

    // =========================================================
    // SETTING INPUT LOCK
    // =========================================================

    #region Setting Input Lock

    private bool IsSettingBlockingShopInput()
    {
        return SettingManager.Instance != null &&
               SettingManager.Instance.IsSettingBlockingInput;
    }

    private void CacheButtonsBlockedBySetting()
    {
        if (buttonsBlockedBySetting == null)
        {
            buttonsBlockedBySetting =
                new List<Button>();
        }

        // Có gắn thủ công trong Inspector thì giữ nguyên danh sách đó.
        if (buttonsBlockedBySetting.Count > 0)
        {
            RemoveDuplicateAndNullButtons();
            return;
        }

        AddButtonsFromRoot(
            ui != null ? ui.shopPanel : null
        );

        AddButtonsFromRoot(
            ui != null ? ui.canvasRandomCard : null
        );

        RemoveDuplicateAndNullButtons();
    }

    private void AddButtonsFromRoot(GameObject root)
    {
        if (root == null)
            return;

        Button[] foundButtons =
            root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < foundButtons.Length; i++)
        {
            Button button = foundButtons[i];

            if (button != null &&
                !buttonsBlockedBySetting.Contains(button))
            {
                buttonsBlockedBySetting.Add(button);
            }
        }
    }

    private void RemoveDuplicateAndNullButtons()
    {
        HashSet<Button> uniqueButtons =
            new HashSet<Button>();

        for (int i = buttonsBlockedBySetting.Count - 1;
             i >= 0;
             i--)
        {
            Button button = buttonsBlockedBySetting[i];

            if (button == null ||
                !uniqueButtons.Add(button))
            {
                buttonsBlockedBySetting.RemoveAt(i);
            }
        }
    }

    private void LockShopInputForSetting()
    {
        if (restoreShopInputCoroutine != null)
        {
            StopCoroutine(restoreShopInputCoroutine);
            restoreShopInputCoroutine = null;
        }

        if (shopInputLockedBySetting)
            return;

        shopInputLockedBySetting = true;

        /*
         * Khóa script nhận phím/tay cầm của Shop.
         * Lưu trạng thái cũ để không vô tình bật một component
         * vốn đã bị tắt trước khi mở Setting.
         */
        if (inputChooseItem != null)
        {
            inputChooseItemWasEnabled =
                inputChooseItem.enabled;

            inputChooseItem.enabled = false;
        }

        CacheButtonsBlockedBySetting();
        buttonInteractableBeforeSetting.Clear();

        for (int i = 0;
             i < buttonsBlockedBySetting.Count;
             i++)
        {
            Button button =
                buttonsBlockedBySetting[i];

            if (button == null)
                continue;

            buttonInteractableBeforeSetting[button] =
                button.interactable;

            button.interactable = false;
        }
    }

    private void BeginRestoreShopInputAfterSetting()
    {
        if (!shopInputLockedBySetting)
            return;

        if (restoreShopInputCoroutine != null)
        {
            StopCoroutine(restoreShopInputCoroutine);
        }

        restoreShopInputCoroutine =
            StartCoroutine(
                RestoreShopInputAfterRelease()
            );
    }

    private IEnumerator RestoreShopInputAfterRelease()
    {
        /*
         * Chờ người chơi thả cần/phím và nút xác nhận.
         * Tránh nút dùng để đóng Setting mua luôn item
         * hoặc di chuyển lựa chọn ngay lập tức.
         */
        while (!IsShopInputReleased())
        {
            // Setting được mở lại trước khi thả input.
            if (IsSettingBlockingShopInput())
            {
                restoreShopInputCoroutine = null;
                yield break;
            }

            yield return null;
        }

        RestoreShopInputNow();
        restoreShopInputCoroutine = null;
    }

    private void RestoreShopInputNow()
    {
        foreach (KeyValuePair<Button, bool> pair
                 in buttonInteractableBeforeSetting)
        {
            if (pair.Key != null)
            {
                pair.Key.interactable = pair.Value;
            }
        }

        buttonInteractableBeforeSetting.Clear();

        if (inputChooseItem != null)
        {
            inputChooseItem.enabled =
                inputChooseItemWasEnabled;
        }

        shopInputLockedBySetting = false;
    }

    private bool IsShopInputReleased()
    {
        bool keyboardReleased =
            !Input.GetKey(KeyCode.A) &&
            !Input.GetKey(KeyCode.D) &&
            !Input.GetKey(KeyCode.LeftArrow) &&
            !Input.GetKey(KeyCode.RightArrow) &&
            !Input.GetKey(KeyCode.J) &&
            !Input.GetKey(KeyCode.Keypad1);

        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null)
        {
            return keyboardReleased;
        }

        float player1Horizontal =
            controller.GetConsoleHorizontalRaw(
                1,
                horizontalJoystick1,
                horizontalJoystick2
            );

        float player2Horizontal =
            controller.GetConsoleHorizontalRaw(
                2,
                horizontalJoystick1,
                horizontalJoystick2
            );

        bool controllerReleased =
            Mathf.Abs(player1Horizontal) <=
                inputReleaseThreshold &&
            Mathf.Abs(player2Horizontal) <=
                inputReleaseThreshold &&
            !controller.GetConsoleButton(1, 0) &&
            !controller.GetConsoleButton(2, 0);

        return keyboardReleased &&
               controllerReleased;
    }

    #endregion

    // =========================================================
    // COIN
    // =========================================================

    #region Coin

    public void UpdateCoin()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || ui.playerCoinTextList[i] == null)
                continue;

            ui.playerCoinTextList[i].text =
                players[i].playerCoin.coinEndMiniGame.ToString();
        }
    }

    private IEnumerator FlashCoinText(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= ui.playerCoinTextList.Length)
            yield break;

        TextMeshProUGUI text = ui.playerCoinTextList[playerIndex];

        if (text == null)
            yield break;

        Color originalColor = text.color;

        for (int i = 0; i < 3; i++)
        {
            text.color = Color.red;
            yield return new WaitForSeconds(0.08f);

            text.color = originalColor;
            yield return new WaitForSeconds(0.08f);
        }

        canErrorCoin[playerIndex] = true;
    }

    #endregion

    // =========================================================
    // ITEM BUY / SHOW / RESET
    // =========================================================

    #region Item

    public bool BuyItem(int playerIndex, int itemIndex, int price)
    {
        // Chặn cả trường hợp Button gửi sự kiện đúng lúc Setting vừa mở.
        if (IsSettingBlockingShopInput() ||
            shopInputLockedBySetting)
        {
            return false;
        }

        if (playerIndex < 0 || playerIndex >= players.Length)
            return false;

        PlayerManager player = players[playerIndex];

        if (player == null)
            return false;

        if (player.playerCoin.coinEndMiniGame < price)
        {
            if (canErrorCoin[playerIndex])
            {
                canErrorCoin[playerIndex] = false;

                AudioManager.Instance.PlaySFX(AudioManager.Instance.noCoinBuyItemClip);
                StartCoroutine(FlashCoinText(playerIndex));
            }

            return false;
        }

        if (itemIndex == 1)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.openCardRamdomClip);
        }
        else
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buyItemClip);
        }

        player.playerCoin.coinEndMiniGame -= price;

        UpdateCoin();

        if (itemIndex == 1)
            return true;

        ShowPlayerItem(playerIndex, itemIndex);

        return true;
    }

    public void ShowPlayerItem(int playerIndex, int itemIndex)
    {
        if (playerIndex < 0 || playerIndex >= ui.playerItemImagesList.Length)
            return;

        if (itemIndex < 0 || itemIndex >= itemSprites.Length)
            return;

        ui.playerItemImagesList[playerIndex].sprite = itemSprites[itemIndex];

        ui.playerItemImagesList[playerIndex].gameObject.SetActive(true);

        SendBuffToPlayer(playerIndex, itemIndex);
    }

    public void ResetShop()
    {
        foreach (Image img in ui.playerItemImagesList)
        {
            img.sprite = null;
            img.gameObject.SetActive(false);
        }
    }

    #endregion

    // =========================================================
    // SHOP OPEN / CLOSE
    // =========================================================

    #region Shop

    public void OpenShop()
    {
        StopTurnTimer();

        ui.shopPanel.SetActive(true);

        ResetShop();
        UpdateCoin();

        inputChooseItem.isPlayer1Choose = false;
        inputChooseItem.isPlayer2Choose = false;

        for (int i = 0; i < ui.itemsList.Length; i++)
        {
            ui.itemsList[i].transform.GetChild(1).gameObject.SetActive(false);
            ui.itemsList[i].transform.GetChild(2).gameObject.SetActive(false);
        }

        if (ui.canvasRandomCard != null)
            ui.canvasRandomCard.SetActive(false);

        if (ui.timerShopText != null)
        {
            ui.timerShopText.text = "";
            ui.timerShopText.gameObject.SetActive(false);
        }

        CancelInvoke(nameof(StartPlayer1Turn));
        Invoke(nameof(StartPlayer1Turn), 1f);
    }

    private void StartPlayer1Turn()
    {
        StartCoroutine(Player1TurnRoutine());
    }

    private IEnumerator Player1TurnRoutine()
    {
        ShowPlayer1Turn();

        yield return new WaitForSeconds(1.3f);

        inputChooseItem.isPlayer1Choose = true;
        inputChooseItem.isPlayer2Choose = false;

        ui.itemsList[inputChooseItem.player1Index]
            .transform.GetChild(1)
            .gameObject.SetActive(true);

        StartTurnTimer(inputChooseItem.TimeOutPlayer1);
    }

    public void CloseShop()
    {
        StopTurnTimer();
        CancelInvoke(nameof(StartPlayer1Turn));

        //  SettingManager.Instance.canOpenSettingByController = true;
        //ui.openSettingPanelButton.SetActive(true    );
        //ui.notifiPlay.SetActive(true);
        StartCoroutine(AnimationCloseShop());
    }
    public IEnumerator AnimationCloseShop()
    {
        Animator anishop = ui.shopPanel.GetComponent<Animator>();
        if (anishop != null)
        {
            anishop.SetTrigger("Close");

        }
        yield return new WaitForSeconds(0.4f);
        ui.shopPanel.SetActive(false);

        GameManager.Instance.CheckWinnerOrNextRound();

    }

    #endregion

    // =========================================================
    // TURN TIMER
    // =========================================================

    #region Turn Timer

    public void StartTurnTimer(Action onTimeOut)
    {
        StopTurnTimer();

        currentTime = timePerTurn;

        if (ui.timerShopText != null)
        {
            ui.timerShopText.gameObject.SetActive(true);
            ui.timerShopText.text = currentTime.ToString();
        }

        turnTimerCoroutine = StartCoroutine(TurnTimerRoutine(onTimeOut));
    }

    public void StopTurnTimer()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        if (ui.timerShopText != null)
        {
            ui.timerShopText.text = "";
            ui.timerShopText.gameObject.SetActive(false);
        }
    }

    private IEnumerator TurnTimerRoutine(Action onTimeOut)
    {
        while (currentTime > 0)
        {
            if (ui.timerShopText != null)
                ui.timerShopText.text = currentTime.ToString();

            yield return new WaitForSeconds(1f);

            currentTime--;
        }

        if (ui.timerShopText != null)
        {
            ui.timerShopText.text = "";
            ui.timerShopText.gameObject.SetActive(false);
        }

        turnTimerCoroutine = null;

        onTimeOut?.Invoke();
    }

    #endregion

    // =========================================================
    // TURN PANEL
    // =========================================================

    #region Turn Panel

    public void ShowPlayer1Turn()
    {
        AudioManager.Instance.PlaySpecial(AudioManager.Instance.playerOneClip);
        StartCoroutine(ShowPlayerTurn(ui.panelPlayer1Turn));
    }

    public void ShowPlayer2Turn()
    {
        AudioManager.Instance.PlaySpecial(AudioManager.Instance.playerTwoClip);
        StartCoroutine(ShowPlayerTurn(ui.panelPlayer2Turn));
    }

    private IEnumerator ShowPlayerTurn(GameObject panel)
    {
        ui.panelPlayer1Turn.SetActive(false);
        ui.panelPlayer2Turn.SetActive(false);

        panel.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        panel.SetActive(false);
    }

    #endregion

    private void OnDestroy()
    {
        if (restoreShopInputCoroutine != null)
        {
            StopCoroutine(restoreShopInputCoroutine);
            restoreShopInputCoroutine = null;
        }

        /*
         * Nếu object bị hủy lúc Setting đang mở,
         * trả lại trạng thái Button/component để không lưu khóa sai.
         */
        if (shopInputLockedBySetting)
        {
            RestoreShopInputNow();
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }

    // =========================================================
    // SEND BUFF
    // =========================================================

    #region Send Buff

    public void SendBuffToPlayer(int playerIndex, int itemIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Length)
            return;

        PlayerBuff p = players[playerIndex].playerBuff;

        if (p == null)
            return;

        p.ApplyBuff(itemIndex);
    }

    #endregion
}