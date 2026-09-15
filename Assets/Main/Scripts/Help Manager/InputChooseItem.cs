using System.Collections;
using UnityEngine;

public class InputChooseItem : MonoBehaviour
{
    #region References

    public ShopManager shopManager;
    public UIManager ui;

    #endregion

    #region Player State

    public int player1Index = 0;
    public int player2Index = 0;

    public bool isPlayer1Choose = false;
    public bool isPlayer2Choose = false;

    #endregion

    #region Navigation Settings

    [Header("Navigation Settings")]
    [SerializeField] private float navigationThreshold = 0.5f;
    [SerializeField] private float resetThreshold = 0.2f;

    [Header("Controller Axis")]
    [Tooltip("Axis X chỉ dành cho Joystick 1.")]
    [SerializeField] private string horizontalJoystick1 = "HorizontalJoystick1";

    [Tooltip("Axis X chỉ dành cho Joystick 2.")]
    [SerializeField] private string horizontalJoystick2 = "HorizontalJoystick2";

    [Tooltip("Axis Y chỉ dành cho Joystick 1.")]
    [SerializeField] private string verticalJoystick1 = "VerticalJoystick1";

    [Tooltip("Axis Y chỉ dành cho Joystick 2.")]
    [SerializeField] private string verticalJoystick2 = "VerticalJoystick2";

    private bool player1HorizontalReady = true;
    private bool player1VerticalReady = true;

    private bool player2HorizontalReady = true;
    private bool player2VerticalReady = true;

    #endregion

    #region Random Card State

    private bool isChoosingRandomCard = false;
    private int randomCardIndex = 0;

    // 0 = Player 1
    // 1 = Player 2
    private int randomCardPlayer = -1;

    private Coroutine randomCardCoroutine;
    private bool isChoosingCardRoutine = false;

    #endregion

    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        ui = UIManager.Instance;
        InitHighlight();
    }

    private void Update()
    {
        if (isChoosingRandomCard)
        {
            HandleRandomCardInput();
            return;
        }

        HandlePlayerInput();
    }

    #region Initialize

    private void InitHighlight()
    {
        for (int i = 0; i < ui.itemsList.Length; i++)
        {
            ui.itemsList[i]
                .transform.GetChild(1)
                .gameObject.SetActive(false);

            ui.itemsList[i]
                .transform.GetChild(2)
                .gameObject.SetActive(false);
        }

        ClearRandomCardHighlight();

        isPlayer1Choose = false;
        isPlayer2Choose = false;

        isChoosingRandomCard = false;
        isChoosingCardRoutine = false;

        ResetPlayer1Navigation();
        ResetPlayer2Navigation();
    }

    private void ClearRandomCardHighlight()
    {
        for (int i = 0; i < ui.itemCardRandomList.Length; i++)
        {
            ui.itemCardRandomList[i]
                .transform.GetChild(1)
                .gameObject.SetActive(false);
        }
    }

    #endregion

    #region Player Input

    private void HandlePlayerInput()
    {
        if (isPlayer1Choose)
        {
            HandlePlayer1Input();

            if (Player1ConfirmDown())
            {
                BuyPlayer1Item();
            }
            else if (Player1CancelDown())
            {
                SkipPlayer1();
            }
        }

        if (isPlayer2Choose)
        {
            HandlePlayer2Input();

            if (Player2ConfirmDown())
            {
                BuyPlayer2Item();
            }
            else if (Player2CancelDown())
            {
                SkipPlayer2();
            }
        }
    }

    private bool IsPlayerUsingController(int playerIndex)
    {
        ControllerManager controllerManager =
            ControllerManager.Instance;

        if (controllerManager == null)
            return false;

        return playerIndex == 0
            ? controllerManager.IsConsole1Connected()
            : controllerManager.IsConsole2Connected();
    }

    private int GetConsoleNumber(int playerIndex)
    {
        return playerIndex == 0 ? 1 : 2;
    }

    private Vector2 GetPlayerMoveInput(int playerIndex)
    {
        /*
         * Có tay cầm:
         * chỉ đọc axis của đúng Console.
         * Không nhận bàn phím của player đó.
         */
        if (IsPlayerUsingController(playerIndex))
        {
            ControllerManager controllerManager =
                ControllerManager.Instance;

            int consoleNumber =
                GetConsoleNumber(playerIndex);

            float horizontal =
                controllerManager.GetConsoleHorizontalRaw(
                    consoleNumber,
                    horizontalJoystick1,
                    horizontalJoystick2
                );

            float vertical =
                controllerManager.GetConsoleVerticalRaw(
                    consoleNumber,
                    verticalJoystick1,
                    verticalJoystick2
                );

            return new Vector2(horizontal, vertical);
        }

        /*
         * Không có tay cầm:
         * Player 1 dùng WASD.
         * Player 2 dùng Arrow Keys.
         */
        float keyboardHorizontal = 0f;
        float keyboardVertical = 0f;

        if (playerIndex == 0)
        {
            if (Input.GetKey(KeyCode.A))
                keyboardHorizontal = -1f;
            else if (Input.GetKey(KeyCode.D))
                keyboardHorizontal = 1f;

            if (Input.GetKey(KeyCode.S))
                keyboardVertical = -1f;
            else if (Input.GetKey(KeyCode.W))
                keyboardVertical = 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow))
                keyboardHorizontal = -1f;
            else if (Input.GetKey(KeyCode.RightArrow))
                keyboardHorizontal = 1f;

            if (Input.GetKey(KeyCode.DownArrow))
                keyboardVertical = -1f;
            else if (Input.GetKey(KeyCode.UpArrow))
                keyboardVertical = 1f;
        }

        return new Vector2(
            keyboardHorizontal,
            keyboardVertical
        );
    }

    private bool GetPlayerButtonDown(
        int playerIndex,
        int controllerButton,
        KeyCode keyboardKey)
    {
        if (IsPlayerUsingController(playerIndex))
        {
            return ControllerManager.Instance
                .GetConsoleButtonDown(
                    GetConsoleNumber(playerIndex),
                    controllerButton
                );
        }

        return Input.GetKeyDown(keyboardKey);
    }

    private bool Player1ConfirmDown()
    {
        return GetPlayerButtonDown(
            0,
            0,
            KeyCode.J
        );
    }

    private bool Player1CancelDown()
    {
        return GetPlayerButtonDown(
            0,
            1,
            KeyCode.K
        );
    }

    private bool Player2ConfirmDown()
    {
        return GetPlayerButtonDown(
            1,
            0,
            KeyCode.Keypad1
        );
    }

    private bool Player2CancelDown()
    {
        return GetPlayerButtonDown(
            1,
            1,
            KeyCode.Keypad2
        );
    }

    #endregion

    #region Timeout

    public void TimeOutPlayer1()
    {
        if (!isPlayer1Choose)
            return;

        SkipPlayer1();
    }

    public void TimeOutPlayer2()
    {
        if (!isPlayer2Choose)
            return;

        SkipPlayer2();
    }

    public void TimeOutRandomCard()
    {
        if (!isChoosingRandomCard)
            return;

        StartChooseRandomCard();
    }

    #endregion

    #region Player 1 Navigation

    private void HandlePlayer1Input()
    {
        IndexItem current =
            ui.itemsList[player1Index].GetComponent<IndexItem>();

        if (current == null)
            return;

        Vector2 input =
            GetPlayerMoveInput(0);

        float horizontal = input.x;
        float vertical = input.y;

        UpdatePlayer1AxisReset(horizontal, vertical);

        // Chọn trục có giá trị lớn hơn để tránh đi chéo hai lần.
        if (Mathf.Abs(vertical) >= Mathf.Abs(horizontal))
        {
            if (vertical > navigationThreshold &&
                player1VerticalReady)
            {
                player1VerticalReady = false;
                ChangePlayer1(current.top);
                return;
            }

            if (vertical < -navigationThreshold &&
                player1VerticalReady)
            {
                player1VerticalReady = false;
                ChangePlayer1(current.bottom);
                return;
            }
        }

        if (horizontal < -navigationThreshold &&
            player1HorizontalReady)
        {
            player1HorizontalReady = false;
            ChangePlayer1(current.left);
            return;
        }

        if (horizontal > navigationThreshold &&
            player1HorizontalReady)
        {
            player1HorizontalReady = false;
            ChangePlayer1(current.right);
        }
    }

    private void UpdatePlayer1AxisReset(
        float horizontal,
        float vertical)
    {
        if (Mathf.Abs(horizontal) < resetThreshold)
        {
            player1HorizontalReady = true;
        }

        if (Mathf.Abs(vertical) < resetThreshold)
        {
            player1VerticalReady = true;
        }
    }

    private void ResetPlayer1Navigation()
    {
        player1HorizontalReady = true;
        player1VerticalReady = true;
    }

    private void ChangePlayer1(int newIndex)
    {
        if (newIndex < 0 ||
            newIndex >= ui.itemsList.Length ||
            newIndex == player1Index)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(
            AudioManager.Instance.movechooseItemClip
        );

        ui.itemsList[player1Index]
            .transform.GetChild(1)
            .gameObject.SetActive(false);

        player1Index = newIndex;

        ui.itemsList[player1Index]
            .transform.GetChild(1)
            .gameObject.SetActive(true);
    }

    #endregion

    #region Player 1 Buy

    private void BuyPlayer1Item()
    {
        PriceItem item =
            ui.itemsList[player1Index].GetComponent<PriceItem>();

        if (item == null)
            return;

        // Random Card
        if (player1Index == 1)
        {
            if (!shopManager.BuyItem(
                    0,
                    player1Index,
                    item.price))
            {
                return;
            }

            shopManager.StopTurnTimer();

            isPlayer1Choose = false;

            ui.itemsList[player1Index]
                .transform.GetChild(1)
                .gameObject.SetActive(false);

            OpenRandomCard(0);
            return;
        }

        if (shopManager.BuyItem(
                0,
                player1Index,
                item.price))
        {
            shopManager.StopTurnTimer();

            isPlayer1Choose = false;

            ui.itemsList[player1Index]
                .transform.GetChild(1)
                .gameObject.SetActive(false);

            bool win1 =
                GameManager.Instance.CheckWinnerByPowerCoinP1();

            if (win1)
            {
                StartCoroutine(CloseShop());
                return;
            }

            StartCoroutine(ShowPlayer2TurnDelay());
        }
    }

    private void SkipPlayer1()
    {
        AudioManager.Instance.PlaySFX(
            AudioManager.Instance.skipBuyClip
        );

        shopManager.StopTurnTimer();

        isPlayer1Choose = false;

        ui.itemsList[player1Index]
            .transform.GetChild(1)
            .gameObject.SetActive(false);

        StartCoroutine(ShowPlayer2TurnDelay());
    }

    #endregion

    #region Player 2 Navigation

    private void HandlePlayer2Input()
    {
        IndexItem current =
            ui.itemsList[player2Index].GetComponent<IndexItem>();

        if (current == null)
            return;

        Vector2 input =
            GetPlayerMoveInput(1);

        float horizontal = input.x;
        float vertical = input.y;

        UpdatePlayer2AxisReset(horizontal, vertical);

        if (Mathf.Abs(vertical) >= Mathf.Abs(horizontal))
        {
            if (vertical > navigationThreshold &&
                player2VerticalReady)
            {
                player2VerticalReady = false;
                ChangePlayer2(current.top);
                return;
            }

            if (vertical < -navigationThreshold &&
                player2VerticalReady)
            {
                player2VerticalReady = false;
                ChangePlayer2(current.bottom);
                return;
            }
        }

        if (horizontal < -navigationThreshold &&
            player2HorizontalReady)
        {
            player2HorizontalReady = false;
            ChangePlayer2(current.left);
            return;
        }

        if (horizontal > navigationThreshold &&
            player2HorizontalReady)
        {
            player2HorizontalReady = false;
            ChangePlayer2(current.right);
        }
    }

    private void UpdatePlayer2AxisReset(
        float horizontal,
        float vertical)
    {
        if (Mathf.Abs(horizontal) < resetThreshold)
        {
            player2HorizontalReady = true;
        }

        if (Mathf.Abs(vertical) < resetThreshold)
        {
            player2VerticalReady = true;
        }
    }

    private void ResetPlayer2Navigation()
    {
        player2HorizontalReady = true;
        player2VerticalReady = true;
    }

    private void ChangePlayer2(int newIndex)
    {
        if (newIndex < 0 ||
            newIndex >= ui.itemsList.Length ||
            newIndex == player2Index)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(
            AudioManager.Instance.movechooseItemClip
        );

        ui.itemsList[player2Index]
            .transform.GetChild(2)
            .gameObject.SetActive(false);

        player2Index = newIndex;

        ui.itemsList[player2Index]
            .transform.GetChild(2)
            .gameObject.SetActive(true);
    }

    #endregion

    #region Player 2 Buy

    private void BuyPlayer2Item()
    {
        PriceItem item =
            ui.itemsList[player2Index].GetComponent<PriceItem>();

        if (item == null)
            return;

        // Random Card
        if (player2Index == 1)
        {
            if (!shopManager.BuyItem(
                    1,
                    player2Index,
                    item.price))
            {
                return;
            }

            shopManager.StopTurnTimer();

            isPlayer2Choose = false;

            ui.itemsList[player2Index]
                .transform.GetChild(2)
                .gameObject.SetActive(false);

            OpenRandomCard(1);
            return;
        }

        if (shopManager.BuyItem(
                1,
                player2Index,
                item.price))
        {
            shopManager.StopTurnTimer();

            isPlayer2Choose = false;

            ui.itemsList[player2Index]
                .transform.GetChild(2)
                .gameObject.SetActive(false);

            bool win2 =
                GameManager.Instance.CheckWinnerByPowerCoinP2();

            StartCoroutine(CloseShop());
        }
    }

    private void SkipPlayer2()
    {
        AudioManager.Instance.PlaySFX(
            AudioManager.Instance.skipBuyClip
        );

        shopManager.StopTurnTimer();

        isPlayer2Choose = false;

        ui.itemsList[player2Index]
            .transform.GetChild(2)
            .gameObject.SetActive(false);

        CheckAllPlayersFinished();
    }

    #endregion

    #region Turn Delay

    private IEnumerator ShowPlayer2TurnDelay()
    {
        isPlayer1Choose = false;
        isPlayer2Choose = false;
        isChoosingRandomCard = false;

        ui.itemsList[player1Index]
            .transform.GetChild(1)
            .gameObject.SetActive(false);

        ui.itemsList[player2Index]
            .transform.GetChild(2)
            .gameObject.SetActive(false);
        yield return new WaitForSeconds(0.8f);
        shopManager.ShowPlayer2Turn();

        yield return new WaitForSeconds(1.3f);

        // Tránh P2 vừa vào lượt đã nhận input đang bị giữ.
        yield return StartCoroutine(WaitForPlayer2AxisRelease());

        ResetPlayer2Navigation();

        isPlayer2Choose = true;

        ui.itemsList[player2Index]
            .transform.GetChild(2)
            .gameObject.SetActive(true);

        shopManager.StartTurnTimer(TimeOutPlayer2);
    }

    private IEnumerator WaitForPlayer2AxisRelease()
    {
        while (true)
        {
            Vector2 input =
                GetPlayerMoveInput(1);

            float horizontal = input.x;
            float vertical = input.y;

            bool horizontalReleased =
                Mathf.Abs(horizontal) < resetThreshold;

            bool verticalReleased =
                Mathf.Abs(vertical) < resetThreshold;

            if (horizontalReleased && verticalReleased)
            {
                break;
            }

            yield return null;
        }
    }

    #endregion

    #region Random Card

    private void OpenRandomCard(int playerIndex)
    {
        var setting = SettingManager.Instance;

        ui.canvasRandomCard.SetActive(true);

        randomCardPlayer = playerIndex;
        randomCardIndex = 0;

        isChoosingRandomCard = false;
        isPlayer1Choose = false;
        isPlayer2Choose = false;
        isChoosingCardRoutine = false;

        ResetPlayer1Navigation();
        ResetPlayer2Navigation();

        RandomizeCards();
        ClearRandomCardHighlight();

        if (randomCardCoroutine != null)
        {
            StopCoroutine(randomCardCoroutine);
        }

        randomCardCoroutine =
            StartCoroutine(WaitOpenAnimation());
    }

    private IEnumerator WaitOpenAnimation()
    {
        Animator animator =
            ui.canvasRandomCard.GetComponent<Animator>();

        yield return null;

        if (animator != null)
        {
            yield return new WaitForSeconds(
                animator
                    .GetCurrentAnimatorStateInfo(0)
                    .length + 0.1f
            );
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        // Chờ người chơi thả cần trước khi cho chọn.
        yield return StartCoroutine(
            WaitForRandomCardAxisRelease()
        );

        if (randomCardPlayer == 0)
        {
            ResetPlayer1Navigation();
        }
        else
        {
            ResetPlayer2Navigation();
        }

        isChoosingRandomCard = true;

        ui.itemCardRandomList[randomCardIndex]
            .transform.GetChild(1)
            .gameObject.SetActive(true);

        shopManager.StartTurnTimer(TimeOutRandomCard);

        randomCardCoroutine = null;
    }

    private IEnumerator WaitForRandomCardAxisRelease()
    {
        while (true)
        {
            Vector2 input =
                GetPlayerMoveInput(randomCardPlayer);

            float horizontal = input.x;

            if (Mathf.Abs(horizontal) < resetThreshold)
            {
                break;
            }

            yield return null;
        }
    }

    private void RandomizeCards()
    {
        int[] randomItems = { 0, 2, 3, 5 };

        for (int i = 0; i < randomItems.Length; i++)
        {
            int randomIndex =
                Random.Range(i, randomItems.Length);

            (randomItems[i], randomItems[randomIndex]) =
                (randomItems[randomIndex], randomItems[i]);
        }

        int cardCount = Mathf.Min(
            ui.itemCardRandomList.Length,
            randomItems.Length
        );

        for (int i = 0; i < cardCount; i++)
        {
            RandomCard card =
                ui.itemCardRandomList[i]
                    .GetComponent<RandomCard>();

            if (card == null)
                continue;

            card.itemIndex = randomItems[i];
            card.image.sprite = card.spriteStar;
        }
    }

    private void HandleRandomCardInput()
    {
        IndexItem current =
            ui.itemCardRandomList[randomCardIndex]
                .GetComponent<IndexItem>();

        if (current == null)
            return;

        if (randomCardPlayer == 0)
        {
            HandlePlayer1RandomCardNavigation(current);

            if (Player1ConfirmDown())
            {
                StartChooseRandomCard();
            }
        }
        else if (randomCardPlayer == 1)
        {
            HandlePlayer2RandomCardNavigation(current);

            if (Player2ConfirmDown())
            {
                StartChooseRandomCard();
            }
        }
    }

    private void HandlePlayer1RandomCardNavigation(
        IndexItem current)
    {
        float horizontal =
            GetPlayerMoveInput(0).x;

        if (Mathf.Abs(horizontal) < resetThreshold)
        {
            player1HorizontalReady = true;
            return;
        }

        if (!player1HorizontalReady)
            return;

        if (horizontal < -navigationThreshold)
        {
            player1HorizontalReady = false;
            ChangeRandomCard(current.left);
        }
        else if (horizontal > navigationThreshold)
        {
            player1HorizontalReady = false;
            ChangeRandomCard(current.right);
        }
    }

    private void HandlePlayer2RandomCardNavigation(
        IndexItem current)
    {
        float horizontal =
            GetPlayerMoveInput(1).x;

        if (Mathf.Abs(horizontal) < resetThreshold)
        {
            player2HorizontalReady = true;
            return;
        }

        if (!player2HorizontalReady)
            return;

        if (horizontal < -navigationThreshold)
        {
            player2HorizontalReady = false;
            ChangeRandomCard(current.left);
        }
        else if (horizontal > navigationThreshold)
        {
            player2HorizontalReady = false;
            ChangeRandomCard(current.right);
        }
    }

    private void ChangeRandomCard(int newIndex)
    {
        if (newIndex < 0 ||
            newIndex >= ui.itemCardRandomList.Length ||
            newIndex == randomCardIndex)
        {
            return;
        }

        AudioManager.Instance.PlaySFX(
            AudioManager.Instance.movechooseItemClip
        );

        ui.itemCardRandomList[randomCardIndex]
            .transform.GetChild(1)
            .gameObject.SetActive(false);

        randomCardIndex = newIndex;

        ui.itemCardRandomList[randomCardIndex]
            .transform.GetChild(1)
            .gameObject.SetActive(true);
    }

    private void StartChooseRandomCard()
    {
        if (isChoosingCardRoutine)
            return;

        StartCoroutine(ChooseRandomCard());
    }

    private IEnumerator ChooseRandomCard()
    {
        isChoosingCardRoutine = true;

        shopManager.StopTurnTimer();

        isChoosingRandomCard = false;

        RandomCard card =
            ui.itemCardRandomList[randomCardIndex]
                .GetComponent<RandomCard>();

        if (card == null)
        {
            isChoosingCardRoutine = false;
            yield break;
        }

        ui.itemCardRandomList[randomCardIndex]
            .transform.GetChild(1)
            .gameObject.SetActive(true);

        card.image.sprite =
            shopManager.itemSprites[card.itemIndex];

        AudioManager.Instance.PlaySFX(
            AudioManager.Instance.buyItemClip
        );

        shopManager.ShowPlayerItem(
            randomCardPlayer,
            card.itemIndex
        );

        yield return new WaitForSeconds(2f);

        ClearRandomCardHighlight();

        ui.canvasRandomCard.SetActive(false);

        isChoosingCardRoutine = false;

        if (randomCardPlayer == 0)
        {
            StartCoroutine(ShowPlayer2TurnDelay());
        }
        else
        {
            isPlayer2Choose = false;

            ui.itemsList[player2Index]
                .transform.GetChild(2)
                .gameObject.SetActive(false);

            CheckAllPlayersFinished();
        }
    }

    #endregion

    #region Close Shop

    private void CheckAllPlayersFinished()
    {
        if (!isPlayer1Choose &&
            !isPlayer2Choose &&
            !isChoosingRandomCard)
        {
            StartCoroutine(CloseShop());
        }
    }

    public IEnumerator CloseShop()
    {
        shopManager.StopTurnTimer();

        yield return new WaitForSeconds(1f);

        ShopManager.Instance.CloseShop();
    }

    #endregion
}