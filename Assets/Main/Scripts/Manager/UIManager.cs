using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    // Singleton để các script khác có thể gọi UIManager.Instance
    public static UIManager Instance { get; private set; }
    [Header("Player References")]
    public PlayerManager playerManager1;
    public PlayerManager playerManager2;

    [Header("Main UI")]
    public GameObject uiMain;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI coinTextP1;
    public TextMeshProUGUI coinTextP2;
    public GameObject player1ResultUI;
    public GameObject player2ResultUI;
    public Image avatarResultP1;
    public Image avatarResultP2;

    [Header("Notification UI")]
    public GameObject canvasNotifi;
    public GameObject notifiPanel;
    public TextMeshProUGUI textNotifi;
    public GameObject diceRollP1;
    public GameObject diceRollP2;
    public GameObject notifiP1;
    public GameObject notifiP2;

    [Header("Player Info UI")]
    public GameObject notifiPlay;
    public GameObject notifiplayer1;
    public GameObject notifiplayer2;
    public TextMeshProUGUI coinTextNotP1;
    public TextMeshProUGUI coinTextNotP2;
    public TextMeshProUGUI indexTextP1;
    public TextMeshProUGUI indexTextP2;
    public Image imageCurrentBuffP1;
    public Image imageCurrentBuffP2;
    public List<Sprite> buffImageList;
    public GameObject bonusCoinTextP1;
    public GameObject bonusCoinTextP2;

    [Header("Mini Game UI")]
    public GameObject canvasMiniGame;
    public TextMeshProUGUI timeMiniGameText;
    public TextMeshProUGUI coinMiniGameTextP1;
    public TextMeshProUGUI coinMiniGameTextP2;
    public Image avatarP1;
    public Image avatarP2;

    [Header("Instruction UI")]
    public GameObject canvasIntructGamePlay;
    public TextMeshProUGUI nameMiniGameText;
    public TextMeshProUGUI instructGamePlayText;
    public TextMeshProUGUI errorGamePlayText;
    public GameObject canvasInstructInput;

    [Header("Debuff UI")]
    public GameObject leftCardCanvas;
    public GameObject rightCardCanvas;
    public GameObject[] leftCardsList;
    public GameObject[] rightCardsList;
    public Sprite[] debuffSpriteList;
    public GameObject panelNotiifiChooseDebuff;
    public GameObject magicDebuffPanel;
    public GameObject cannonDebuffPanel;

    [Header("Debuff Random Color Debug")]
    [Tooltip("Bật để hiện List A/B sau khi hai card random xong.")]
    public bool showDebuffRandomColorLists;
    public List<Image> listA = new List<Image>();
    public List<Image> listB = new List<Image>();

    [Header("Buff UI")]
    public GameObject cannonPowerPanel;
    public GameObject cannonShieldPanel;
    public GameObject petrificationImmunityPanel;
    [Header("Trap UI")]
    public GameObject bombTrapPanel;
    public GameObject positionTrapPanel;

    [Header("Shop UI")]
    public GameObject shopPanel;
    public TextMeshProUGUI[] playerCoinTextList;
    public Image[] playerItemImagesList;
    public TextMeshProUGUI timerShopText;
    public GameObject canvasRandomCard;
    public GameObject panelPlayer1Turn;
    public GameObject panelPlayer2Turn;
    public GameObject[] itemsList;
    public GameObject[] itemCardRandomList;
    [Header("Minigame Input Instructions")]
    [FormerlySerializedAs("instructKeyBoard")]
    public GameObject minigameKeyboardRoot;

    [FormerlySerializedAs("instructKeyBoardP1")]
    public GameObject minigameKeyboardP1Root;

    [FormerlySerializedAs("instructKeyBoardP2")]
    public GameObject minigameKeyboardP2Root;

    [FormerlySerializedAs("instructKeyBoardInputListP1")]
    public List<GameObject> minigameKeyboardInputListP1 =
        new List<GameObject>();

    [FormerlySerializedAs("instructKeyBoardInputListP2")]
    public List<GameObject> minigameKeyboardInputListP2 =
        new List<GameObject>();

    [FormerlySerializedAs("instructConsoleClone")]
    public GameObject minigameConsoleRoot;

    [FormerlySerializedAs("instructConsoleCloneP1")]
    public GameObject minigameConsoleP1Root;

    [FormerlySerializedAs("instructConsoleCloneP2")]
    public GameObject minigameConsoleP2Root;

    [FormerlySerializedAs("instructConsoleInputListP1")]
    public List<GameObject> minigameConsoleInputListP1 =
        new List<GameObject>();

    [FormerlySerializedAs("instructConsoleInputListP2")]
    public List<GameObject> minigameConsoleInputListP2 =
        new List<GameObject>();

    [FormerlySerializedAs("instructBothConsole")]
    public GameObject minigameBothControllersRoot;

    [FormerlySerializedAs("instructBothConsoleInputListP1")]
    public List<GameObject> minigameBothControllersInputList =
        new List<GameObject>();

    // =========================================================
    // LEGACY MINIGAME FIELD ALIASES
    // Giữ InstructInputMinigame cũ tiếp tục compile và hoạt động.
    // Các property này không xuất hiện trong Inspector.
    // =========================================================

    public GameObject instructKeyBoard
    {
        get => minigameKeyboardRoot;
        set => minigameKeyboardRoot = value;
    }

    public GameObject instructKeyBoardP1
    {
        get => minigameKeyboardP1Root;
        set => minigameKeyboardP1Root = value;
    }

    public GameObject instructKeyBoardP2
    {
        get => minigameKeyboardP2Root;
        set => minigameKeyboardP2Root = value;
    }

    public List<GameObject> instructKeyBoardInputListP1
    {
        get => minigameKeyboardInputListP1;
        set => minigameKeyboardInputListP1 = value;
    }

    public List<GameObject> instructKeyBoardInputListP2
    {
        get => minigameKeyboardInputListP2;
        set => minigameKeyboardInputListP2 = value;
    }

    public GameObject instructConsoleClone
    {
        get => minigameConsoleRoot;
        set => minigameConsoleRoot = value;
    }

    public GameObject instructConsoleCloneP1
    {
        get => minigameConsoleP1Root;
        set => minigameConsoleP1Root = value;
    }

    public GameObject instructConsoleCloneP2
    {
        get => minigameConsoleP2Root;
        set => minigameConsoleP2Root = value;
    }

    public List<GameObject> instructConsoleInputListP1
    {
        get => minigameConsoleInputListP1;
        set => minigameConsoleInputListP1 = value;
    }

    public List<GameObject> instructConsoleInputListP2
    {
        get => minigameConsoleInputListP2;
        set => minigameConsoleInputListP2 = value;
    }

    public GameObject instructBothConsole
    {
        get => minigameBothControllersRoot;
        set => minigameBothControllersRoot = value;
    }

    public List<GameObject> instructBothConsoleInputListP1
    {
        get => minigameBothControllersInputList;
        set => minigameBothControllersInputList = value;
    }

    [Header("Global Device Instructions")]
    [Tooltip("UI bàn phím bổ sung ngoài phần hướng dẫn minigame của Player 1.")]
    public List<GameObject> globalKeyboardInstructionListP1 =
        new List<GameObject>();

    [Tooltip("UI bàn phím bổ sung ngoài phần hướng dẫn minigame của Player 2.")]
    public List<GameObject> globalKeyboardInstructionListP2 =
        new List<GameObject>();

    [Tooltip("UI tay cầm bổ sung ngoài phần hướng dẫn minigame của Player 1.")]
    public List<GameObject> globalConsoleInstructionListP1 =
        new List<GameObject>();

    [Tooltip("UI tay cầm bổ sung ngoài phần hướng dẫn minigame của Player 2.")]
    public List<GameObject> globalConsoleInstructionListP2 =
        new List<GameObject>();

    [Tooltip(
        "UI tay cầm dùng chung ngoài minigame như Setting hoặc Shop. " +
        "Chỉ cần P1 hoặc P2 có tay cầm thì các UI trong list này sẽ hiện."
    )]
    [FormerlySerializedAs("instructAnyControllerInputList")]
    public List<GameObject> globalAnyControllerInstructionList =
        new List<GameObject>();

    // Cache trạng thái tay cầm dùng chung cho toàn bộ UI ngoài minigame.
    // Không liên quan tới InstructInputMinigame.
    private bool previousGlobalP1ControllerConnected;
    private bool previousGlobalP2ControllerConnected;
    private bool previousIsShowKeyboard;
    private bool globalInstructionInitialized;

    [Tooltip(
        "Chu kỳ kiểm tra cha của các Global Instruction vừa được mở/đóng."
    )]
    [Min(0.05f)]
    [SerializeField]
    private float globalParentVisibilityCheckInterval = 0.25f;

    private float globalParentVisibilityCheckTimer;
    private int previousGlobalParentVisibilityHash;
    private bool globalParentVisibilityInitialized;

    [Header("Screen Transition")]
    public GameObject blackPanel;
    public GameObject flastBlackPanel;

    [Header("Settings UI")]
    public GameObject settingPanel;
    public GameObject openSettingPanelButton;
    public GameObject exitMainMenuButton;

    [Header("Settings Animation")]
    [SerializeField] private Animator settingPanelAnimator;
    [SerializeField] private string settingOpenTrigger = "Open";
    [SerializeField] private string settingCloseTrigger = "Close";

    [Min(0f)]
    [SerializeField] private float settingOpenAnimationTime = 0.5f;

    [Min(0f)]
    [SerializeField] private float settingCloseAnimationTime = 0.5f;

    private Coroutine settingPanelAnimationCoroutine;

    public bool IsSettingPanelTransitioning
    {
        get;
        private set;
    }
    [Header("Cosole UI")]
    public GameObject consoleOpenImageP1;
    public GameObject consoleCloseImageP1;
    public GameObject consoleOpenImageP2;
    public GameObject consoleCloseImageP2;
    public GameObject instructConsolePanel;
    [Header("Key Board")]
    public GameObject instructKeyBoardPanel;
    public bool isShowKeyBoard = false;

    [Header("Bonus UI")]
    public GameObject bonusPanel;

    [Header("Update Settings")]
    public float updateUITime = 0.25f;
    private float updateTimer;
    public List<GameObject> allUI;

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
        }
    }

    private void Start()
    {
        SetupPlayers();
        UpdateAllPlayMainUI();
        SetupControllerUI();
    }

    private void SetupPlayers()
    {
        playerManager1 = GameObject.FindGameObjectWithTag("Player 1")?.GetComponent<PlayerManager>();
        playerManager2 = GameObject.FindGameObjectWithTag("Player 2")?.GetComponent<PlayerManager>();
    }

    private void SetupControllerUI()
    {
        // UIManager quản lý toàn bộ UI kết nối tay cầm ngoài minigame.
        UpdateControllerConnectionUI(true, true);
    }

    private void Update()
    {
        bool globalParentVisibilityChanged =
            CheckGlobalInstructionParentVisibilityChanged();

        // Trạng thái gốc vẫn lấy từ ControllerManager.
        // UI chỉ thay đổi khi P1/P2 đổi kết nối hoặc cha Global vừa mở lại.
        UpdateControllerConnectionUI(
            globalParentVisibilityChanged
        );

        if (notifiPlay == null ||
            !notifiPlay.activeSelf)
        {
            return;
        }

        updateTimer += Time.deltaTime;

        if (updateTimer >= updateUITime)
        {
            updateTimer = 0f;
            UpdateAllPlayMainUI();
        }
    }

    // =========================================================
    // GLOBAL DEVICE INSTRUCTION
    // =========================================================

    public void RefreshGlobalDeviceInstructionUI()
    {
        UpdateControllerConnectionUI(true);
    }

    public void RefreshControllerConnectionUI()
    {
        UpdateControllerConnectionUI(true);
    }

    private void UpdateControllerConnectionUI(
        bool force = false,
        bool showInitialNotifications = false)
    {
        ControllerManager controller =
            ControllerManager.Instance;

        bool p1ControllerConnected =
            controller != null &&
            controller.IsConsole1Connected();

        bool p2ControllerConnected =
            controller != null &&
            controller.IsConsole2Connected();

        bool wasInitialized =
            globalInstructionInitialized;

        bool p1ConnectionChanged =
            wasInitialized &&
            p1ControllerConnected !=
                previousGlobalP1ControllerConnected;

        bool p2ConnectionChanged =
            wasInitialized &&
            p2ControllerConnected !=
                previousGlobalP2ControllerConnected;

        bool keyboardDisplayModeChanged =
            wasInitialized &&
            isShowKeyBoard != previousIsShowKeyboard;

        if (!force &&
            wasInitialized &&
            !p1ConnectionChanged &&
            !p2ConnectionChanged &&
            !keyboardDisplayModeChanged)
        {
            return;
        }

        previousGlobalP1ControllerConnected =
            p1ControllerConnected;

        previousGlobalP2ControllerConnected =
            p2ControllerConnected;

        previousIsShowKeyboard =
            isShowKeyBoard;

        globalInstructionInitialized = true;

        bool hasAnyController =
            p1ControllerConnected ||
            p2ControllerConnected;

        bool hasBothControllers =
            p1ControllerConnected &&
            p2ControllerConnected;

        SetGameObjectActiveIfChanged(
            instructConsolePanel,
            hasAnyController
        );

        if (isShowKeyBoard)
        {
            SetGameObjectActiveIfChanged(
                instructKeyBoardPanel,
                !hasBothControllers
            );
        }

        // Chỉ thay đổi nhóm Global, không chạm vào list của minigame.
        SetGlobalInstructionListActive(
            globalKeyboardInstructionListP1,
            !p1ControllerConnected
        );

        SetGlobalInstructionListActive(
            globalConsoleInstructionListP1,
            p1ControllerConnected
        );

        SetGlobalInstructionListActive(
            globalKeyboardInstructionListP2,
            !p2ControllerConnected
        );

        SetGlobalInstructionListActive(
            globalConsoleInstructionListP2,
            p2ControllerConnected
        );

        // Có 1/2 hoặc 2/2 tay cầm thì hiện hướng dẫn chung.
        SetGlobalInstructionListActive(
            globalAnyControllerInstructionList,
            hasAnyController
        );

        if (wasInitialized)
        {
            if (p1ConnectionChanged)
            {
                ShowControllerConnectionNotification(
                    0,
                    p1ControllerConnected
                );
            }

            if (p2ConnectionChanged)
            {
                ShowControllerConnectionNotification(
                    1,
                    p2ControllerConnected
                );
            }
        }
        else if (showInitialNotifications)
        {
            if (p1ControllerConnected)
            {
                ShowControllerConnectionNotification(0, true);
            }

            if (p2ControllerConnected)
            {
                ShowControllerConnectionNotification(1, true);
            }
        }
    }

    private void SetGameObjectActiveIfChanged(
        GameObject target,
        bool active)
    {
        if (target != null &&
            target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private void ShowControllerConnectionNotification(
        int playerIndex,
        bool connected)
    {
        GameObject notification;

        if (playerIndex == 0)
        {
            notification = connected
                ? consoleOpenImageP1
                : consoleCloseImageP1;
        }
        else
        {
            notification = connected
                ? consoleOpenImageP2
                : consoleCloseImageP2;
        }

        if (notification == null)
            return;

        if (connected)
        {
            StartCoroutine(
                ShowConsoleConect(notification)
            );
        }
        else
        {
            StartCoroutine(
                ShowConsoleFailConect(notification)
            );
        }
    }

    private void SetGlobalInstructionListActive(
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

            if (instruction == null)
                continue;

            Transform parent =
                instruction.transform.parent;

            // UI cha đang bị ẩn thì toàn bộ UI con cũng không nhìn thấy.
            // Không cần gọi SetActive cho mục này cho tới khi cha được mở lại.
            if (parent != null &&
                !parent.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (instruction.activeSelf != active)
            {
                instruction.SetActive(active);
            }
        }
    }

    private bool CheckGlobalInstructionParentVisibilityChanged()
    {
        globalParentVisibilityCheckTimer +=
            Time.unscaledDeltaTime;

        float interval = Mathf.Max(
            0.05f,
            globalParentVisibilityCheckInterval
        );

        if (globalParentVisibilityInitialized &&
            globalParentVisibilityCheckTimer < interval)
        {
            return false;
        }

        globalParentVisibilityCheckTimer = 0f;

        int currentHash =
            CalculateGlobalInstructionParentVisibilityHash();

        if (!globalParentVisibilityInitialized)
        {
            previousGlobalParentVisibilityHash =
                currentHash;

            globalParentVisibilityInitialized = true;
            return false;
        }

        if (currentHash ==
            previousGlobalParentVisibilityHash)
        {
            return false;
        }

        previousGlobalParentVisibilityHash =
            currentHash;

        return true;
    }

    private int CalculateGlobalInstructionParentVisibilityHash()
    {
        unchecked
        {
            int hash = 17;

            AddInstructionParentVisibilityToHash(
                globalKeyboardInstructionListP1,
                ref hash
            );

            AddInstructionParentVisibilityToHash(
                globalConsoleInstructionListP1,
                ref hash
            );

            AddInstructionParentVisibilityToHash(
                globalKeyboardInstructionListP2,
                ref hash
            );

            AddInstructionParentVisibilityToHash(
                globalConsoleInstructionListP2,
                ref hash
            );

            AddInstructionParentVisibilityToHash(
                globalAnyControllerInstructionList,
                ref hash
            );

            return hash;
        }
    }

    private void AddInstructionParentVisibilityToHash(
    List<GameObject> instructionList,
    ref int hash)
    {
        if (instructionList == null)
            return;

        for (int i = 0;
             i < instructionList.Count;
             i++)
        {
            GameObject instruction =
                instructionList[i];

            if (instruction == null)
                continue;

            Transform parent =
                instruction.transform.parent;

            if (parent == null)
            {
                hash = hash * 31 + 1;
                continue;
            }

            hash = hash * 31 + i;

            hash = hash * 31 +
                   (parent.gameObject.activeInHierarchy
                       ? 1
                       : 0);
        }
    }

    public void UpdateAllPlayMainUI()
    {
        if (playerManager1 != null && playerManager2 != null)
        {
            UpdateCoinPowerUI();
            UpdateCoinAllPlayer();
            UpdateIndexPlayerWalk();
            UpdateCurrentBuffPlayer();
        }
    }

    // =========================================================
    // COIN POWER UI
    // =========================================================

    /// <summary>
    /// Cập nhật số lượng Coin Power hiển thị trên UI.
    /// Coin chưa có sẽ bị che bởi panel màu đen.
    /// </summary>
    public void UpdateCoinPowerUI()
    {
        PlayerBuff p1 = playerManager1.playerBuff;
        PlayerBuff p2 = playerManager2.playerBuff;

        // Root chứa icon Coin Power Player 1
        Transform coinRoot =
            notifiplayer1.transform
            .GetChild(3)
            .GetChild(1);

        // Root chứa icon Coin Power Player 2
        Transform coinRoot2 =
            notifiplayer2.transform
            .GetChild(3)
            .GetChild(1);
        Transform highlightYellowCoinPowerP1 = notifiplayer1.transform.GetChild(3).GetChild(0);
        Transform highlightYellowCoinPowerP2 = notifiplayer2.transform.GetChild(3).GetChild(0);

        // =========================
        // PLAYER 1 COIN POWER
        // =========================

        for (int i = 0; i < coinRoot.childCount; i++)
        {
            // Panel đen che icon
            Transform blackPanel =
                coinRoot.GetChild(i).GetChild(0);

            // Nếu đã có Coin Power thì tắt panel đen
            blackPanel.gameObject.SetActive(i >= p1.countCoinPower);
            var hightlightCoinPower = coinRoot.GetChild(i).GetComponent<UIImagePowerCoinColorEffect>();
            if (i >= p1.countCoinPower)
            {
                hightlightCoinPower.enabled = false;
                highlightYellowCoinPowerP1.GetChild(i).gameObject.SetActive(false);
            }
            else
            {
                hightlightCoinPower.enabled = true;
                highlightYellowCoinPowerP1.GetChild(i).gameObject.SetActive(true);
            }

        }

        // =========================
        // PLAYER 2 COIN POWER
        // =========================

        for (int i = 0; i < coinRoot2.childCount; i++)
        {
            // Panel đen che icon
            Transform blackPanel =
                coinRoot2.GetChild(i).GetChild(0);

            // Nếu đã có Coin Power thì tắt panel đen
            blackPanel.gameObject.SetActive(i >= p2.countCoinPower);
            var hightlightCoinPower = coinRoot2.GetChild(i).GetComponent<UIImagePowerCoinColorEffect>();
            if (i >= p2.countCoinPower)
            {
                hightlightCoinPower.enabled = false;
                highlightYellowCoinPowerP2.GetChild(i).gameObject.SetActive(false);
            }
            else
            {
                hightlightCoinPower.enabled = true;
                highlightYellowCoinPowerP2.GetChild(i).gameObject.SetActive(true);
            }

        }
    }

    // =========================================================
    // COIN UI
    // =========================================================

    /// <summary>
    /// Cập nhật số coin của cả hai người chơi.
    /// </summary>
    public void UpdateCoinAllPlayer()
    {
        PlayerCoin p1 = playerManager1.playerCoin;
        PlayerCoin p2 = playerManager2.playerCoin;

        coinTextNotP1.text = p1.coinEndMiniGame.ToString();
        coinTextNotP2.text = p2.coinEndMiniGame.ToString();
    }

    // =========================================================
    // PLAYER BOARD INDEX UI
    // =========================================================

    /// <summary>
    /// Cập nhật vị trí hiện tại trên bàn cờ.
    /// </summary>
    public void UpdateIndexPlayerWalk()
    {
        PlayerMoveAI p1 = playerManager1.playerMoveAI;
        PlayerMoveAI p2 = playerManager2.playerMoveAI;

        indexTextP1.text = $"{p1.currentIndex + 1}/33";
        indexTextP2.text = $"{p2.currentIndex + 1}/33";
    }

    // =========================================================
    // CURRENT BUFF UI
    // =========================================================

    public void UpdateCurrentBuffPlayer()
    {
        PlayerBuff p1 = playerManager1.playerBuff;
        PlayerBuff p2 = playerManager2.playerBuff;

        // =========================
        // PLAYER 1 CURRENT BUFF
        // =========================

        if (p1.isBuffDeffense)
        {
            imageCurrentBuffP1.sprite = buffImageList[1];
        }
        else if (p1.isBuffMagic)
        {
            imageCurrentBuffP1.sprite = buffImageList[2];
        }
        else if (p1.isBuffCanon)
        {
            imageCurrentBuffP1.sprite = buffImageList[3];
        }
        else if (p1.isBuffDice > 0 || p1.isBuffDiceNext > 0)
        {
            imageCurrentBuffP1.sprite = buffImageList[4];
        }
        else
        {
            imageCurrentBuffP1.sprite = buffImageList[0];
        }

        // =========================
        // PLAYER 2 CURRENT BUFF
        // =========================

        if (p2.isBuffDeffense)
        {
            imageCurrentBuffP2.sprite = buffImageList[1];
        }
        else if (p2.isBuffMagic)
        {
            imageCurrentBuffP2.sprite = buffImageList[2];
        }
        else if (p2.isBuffCanon)
        {
            imageCurrentBuffP2.sprite = buffImageList[3];
        }
        else if (p2.isBuffDice > 0 || p2.isBuffDiceNext > 0)
        {
            imageCurrentBuffP2.sprite = buffImageList[4];
        }
        else
        {
            imageCurrentBuffP2.sprite = buffImageList[0];
        }
    }

    // =========================================================
    // NOTIFICATION PANEL
    // =========================================================

    /// <summary>
    /// Hiện hoặc ẩn panel thông báo.
    /// </summary>
    public void HidePlayerPlayPanel(bool i)
    {
        notifiPanel.gameObject.SetActive(i);
    }

    /// <summary>
    /// Hiển thị thông báo trong 2 giây.
    /// </summary>
    public IEnumerator ShowDebuffAndBuffPanel(GameObject ui)
    {
        if (ui != null)
        {
            ui.SetActive(true);
            yield return new WaitForSeconds(1.4f);
            ui.SetActive(false);
        }
    }

    // =========================================================
    // RESULT PANEL
    // =========================================================

    /// <summary>
    /// Hiển thị bảng kết quả cuối game.
    /// </summary>
    public void UpdateResultPanel(int coinP1, int coinP2)
    {
        if (resultPanel != null)
        {
            // Hiện panel kết quả
            resultPanel.SetActive(true);

            // Phát âm thanh mở panel kết quả
            AudioManager.Instance.PlayUI(AudioManager.Instance.openResultPanel);

            // Cập nhật coin Player 1
            if (coinTextP1 != null)
                coinTextP1.text = $"{coinP1}";

            // Cập nhật coin Player 2
            if (coinTextP2 != null)
                coinTextP2.text = $"{coinP2}";

            // =========================
            // PLAYER 1 THẮNG
            // =========================

            if (coinP1 > coinP2)
            {
                player1ResultUI.transform.GetChild(0).gameObject.SetActive(true);   // Win
                player1ResultUI.transform.GetChild(1).gameObject.SetActive(false);  // Lose

                player2ResultUI.transform.GetChild(0).gameObject.SetActive(false);
                player2ResultUI.transform.GetChild(1).gameObject.SetActive(true);
            }

            // =========================
            // PLAYER 2 THẮNG
            // =========================

            else if (coinP1 < coinP2)
            {
                player1ResultUI.transform.GetChild(0).gameObject.SetActive(false);
                player1ResultUI.transform.GetChild(1).gameObject.SetActive(true);

                player2ResultUI.transform.GetChild(0).gameObject.SetActive(true);
                player2ResultUI.transform.GetChild(1).gameObject.SetActive(false);
            }

            // =========================
            // HÒA
            // =========================

            else
            {
                player1ResultUI.transform.GetChild(0).gameObject.SetActive(true);
                player1ResultUI.transform.GetChild(1).gameObject.SetActive(false);

                player2ResultUI.transform.GetChild(0).gameObject.SetActive(true);
                player2ResultUI.transform.GetChild(1).gameObject.SetActive(false);
            }

            avatarResultP1.sprite = playerManager1.GetComponent<PlayerInfo>().avatarCharacter;
            avatarResultP2.sprite = playerManager2.GetComponent<PlayerInfo>().avatarCharacter;

        }
    }

    /// <summary>
    /// Ẩn bảng kết quả.
    /// </summary>
    public void HideResultPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    // =========================================================
    // PLAYER PLAY PANEL
    // =========================================================

    /// <summary>
    /// Hiện hoặc ẩn bảng thông tin người chơi.
    /// </summary>
    public void HideNotifiPlayPanel(bool i)
    {
        notifiPlay.gameObject.SetActive(i);
    }

    // =========================================================
    // BONUS PANEL
    // =========================================================

    /// <summary>
    /// Hiển thị panel Bonus trong 1 giây rồi tự tắt.
    /// </summary>
    public IEnumerator HideBonusPanel()
    {
        bonusPanel.SetActive(true);

        yield return new WaitForSeconds(2f);

        bonusPanel.SetActive(false);
    }

    public void ShowBonusCoin(int playerID)
    {
        // Player 1 bonus coin UI
        if (playerID == 0)
        {
            StartCoroutine(ShowUIBonusCoin(bonusCoinTextP1));
        }

        // Player 2 bonus coin UI
        else if (playerID == 1)
        {
            StartCoroutine(ShowUIBonusCoin(bonusCoinTextP2));
        }
    }

    IEnumerator ShowUIBonusCoin(GameObject canvas)
    {
        // Hiện UI bonus coin
        canvas.SetActive(true);

        yield return new WaitForSeconds(3f);

        // Ẩn UI bonus coin
        canvas.SetActive(false);
    }
    public IEnumerator BlackPanelRoutine()
    {
        // Hiện panel
        blackPanel.SetActive(true);

        yield return null;
    }
    public void HideUIMain()
    {
        uiMain.SetActive(false);
    }

    public bool ActiveSettingPanel(bool active)
    {
        if (settingPanel == null ||
            IsSettingPanelTransitioning)
        {
            return false;
        }

        if (settingPanel.activeSelf == active)
        {
            return false;
        }

        settingPanelAnimationCoroutine =
            StartCoroutine(
                AnimateSettingPanel(active)
            );

        return true;
    }

    private IEnumerator AnimateSettingPanel(bool open)
    {
        IsSettingPanelTransitioning = true;

        if (settingPanel == null)
        {
            IsSettingPanelTransitioning = false;
            settingPanelAnimationCoroutine = null;
            yield break;
        }

        if (settingPanelAnimator == null)
        {
            settingPanelAnimator =
                settingPanel.GetComponent<Animator>();
        }

        if (open)
        {
            settingPanel.SetActive(true);

            if (settingPanelAnimator != null)
            {
                settingPanelAnimator.ResetTrigger(
                    settingCloseTrigger
                );

                settingPanelAnimator.SetTrigger(
                    settingOpenTrigger
                );
            }

            // Không kiểm tra Animator.
            // Sau đúng 0.5 giây thì Pause.
            yield return new WaitForSecondsRealtime(0.5f);

            if (PauseGameManager.Instance != null)
            {
                PauseGameManager.Instance.PauseGame();
            }
        }
        else
        {
            // Resume trước khi chạy animation đóng.
            if (PauseGameManager.Instance != null)
            {
                PauseGameManager.Instance.ResumeGame();
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (settingPanelAnimator != null)
            {
                settingPanelAnimator.ResetTrigger(
                    settingOpenTrigger
                );

                settingPanelAnimator.SetTrigger(
                    settingCloseTrigger
                );
            }

            // Sau đúng 0.5 giây thì tắt panel.
            yield return new WaitForSecondsRealtime(0.7f);

            settingPanel.SetActive(false);
        }

        IsSettingPanelTransitioning = false;
        settingPanelAnimationCoroutine = null;
    }

    public void ForceHideSettingPanel()
    {
        if (settingPanelAnimationCoroutine != null)
        {
            StopCoroutine(settingPanelAnimationCoroutine);
            settingPanelAnimationCoroutine = null;
        }

        IsSettingPanelTransitioning = false;

        if (settingPanelAnimator == null &&
            settingPanel != null)
        {
            settingPanelAnimator =
                settingPanel.GetComponent<Animator>();
        }

        if (settingPanelAnimator != null)
        {
            settingPanelAnimator.ResetTrigger(
                settingOpenTrigger
            );

            settingPanelAnimator.ResetTrigger(
                settingCloseTrigger
            );
        }

        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }

    public void ActiveOpenSettingButton(bool i)
    {
        openSettingPanelButton.SetActive(i);
    }
    public void FindPlayerManager()
    {
        GameObject player1 = GameObject.FindGameObjectWithTag("Player 1");
        GameObject player2 = GameObject.FindGameObjectWithTag("Player 2");
        if (player1 != null)
        {
            playerManager1 = player1.GetComponent<PlayerManager>();
        }

        if (player2 != null)
        {
            playerManager2 = player2.GetComponent<PlayerManager>();
        }
    }
    public IEnumerator ShowConsoleConect(GameObject ui)
    {
        ui.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        ui.SetActive(false);
    }
    public IEnumerator ShowConsoleFailConect(GameObject ui)
    {
        ui.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        ui.SetActive(false);
    }
    public void HideAllUI()
    {
        for (int i = 0; i < allUI.Count; i++)
        {
            if (allUI[i] != null && allUI[i].activeSelf)
            {
                allUI[i].SetActive(false);
            }

        }
    }
}