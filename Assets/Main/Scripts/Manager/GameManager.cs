using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static GameManager instance;
    public static GameManager Instance => instance;

    // =========================================================
    // INSPECTOR
    // =========================================================
    #region Inspector

    [Header("Winner")]
    public string playerWinRound;

    [Header("Main Players")]
    public GameObject player1Main;
    public GameObject player2Main;

    [Header("Game Loop")]
   // public bool canRunGameLoopUpdate = true;
    public bool canStartNextRound = false;
    public bool canCheckPlayer2 = true;
    public bool canCheckMiniGame = true;
    public bool canRandomIndexMiniGame = true;

    #endregion

    // =========================================================
    // PRIVATE VARIABLES
    // =========================================================
    #region Private Variables

    private MiniGameManager miniGameManager;
    private StateStoryGame stateGame;
    private float timer;

    // =========================================================
    // RANDOM MINIGAME KHÔNG LẶP
    // =========================================================

    // Danh sách những minigame chưa chơi
    private List<int> remainingMiniGames = new List<int>();

    // Minigame vừa chơi gần nhất
    // -1 = chưa chơi minigame nào
    private int lastMiniGame = -1;

    #endregion

    // =========================================================
    // UNITY FUNCTIONS
    // =========================================================

    private void Awake()
    {
        SetupSingleTon();
    }
    public void SetupSingleTon()
    {
        // Tạo Singleton
        if (instance == null)
        {
            instance = this;

            // Không bị hủy khi chuyển scene
            DontDestroyOnLoad(gameObject);

            // Tìm MiniGameManager trong scene
            miniGameManager = FindAnyObjectByType<MiniGameManager>();

            // Lấy StateStoryGame trên cùng GameObject
            stateGame = GetComponent<StateStoryGame>();
        }
        else
        {
            // Nếu đã tồn tại GameManager thì xóa bản mới
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupPlayers();
    }

    private void SetupPlayers()
    {
        player1Main = GameObject.FindGameObjectWithTag("Player 1");
        player2Main = GameObject.FindGameObjectWithTag("Player 2");
    }


    private void Update()
    {
        // Minigame, debuff hoặc shop đang xử lý thì tạm dừng kiểm tra lượt chơi.
        

        timer += Time.deltaTime;

        if (timer < 1f)
            return;

        timer = 0f;

        // Chờ Player 1 đi xong để tới lượt Player 2
        if (canCheckPlayer2)
            WaitPlayer1RollDice();

        // Nếu cả 2 player đã xong lượt thì bắt đầu minigame tiếp theo
        if (canCheckMiniGame)
            StartNextRound();
    }

    // =========================================================
    // ROUND RESULT
    // =========================================================

    // Kiểm tra ai thắng vòng dựa trên coin kiếm được trong minigame
    public void CheckPlayerWinRound(int playerCoin1, int playerCoin2)
    {
        if (playerCoin1 > playerCoin2)
        {
            playerWinRound = "Player 1";
        }
        else if (playerCoin2 > playerCoin1)
        {
            playerWinRound = "Player 2";
        }
        else
        {
            playerWinRound = "Draw";
        }

        // Bắt đầu cộng coin và xử lý kết quả sau vòng
        StartCoroutine(AddCoinToPlayer(playerCoin1, playerCoin2));
    }

    IEnumerator AddCoinToPlayer(int playerCoin1, int playerCoin2)
    {
        // Chờ một chút trước khi cộng coin
        yield return new WaitForSeconds(2f);

        PlayerCoin player1 = player1Main.GetComponent<PlayerCoin>();
        PlayerCoin player2 = player2Main.GetComponent<PlayerCoin>();

        // Cộng coin từ minigame vào coin chính của từng player
        player1.AddCoinToPlayerMain(playerCoin1);
        player2.AddCoinToPlayerMain(playerCoin2);

        yield return new WaitForSeconds(2f);

        // =========================
        // PLAYER 1 THẮNG
        // =========================
        if (playerWinRound == "Player 1")
        {
            yield return StartCoroutine(
                CameraManager.Instance.MoveToTarget(
                    player1Main.transform,
                    1f
                )
            );

            // Mở debuff cho Player 1 chọn
            DebuffManager.Instance.OpenDebuffInternal(0);
        }

        // =========================
        // PLAYER 2 THẮNG
        // =========================
        else if (playerWinRound == "Player 2")
        {
            yield return StartCoroutine(
                CameraManager.Instance.MoveToTarget(
                    player2Main.transform,
                    1f
                )
            );

            // Mở debuff cho Player 2 chọn
            DebuffManager.Instance.OpenDebuffInternal(1);
        }

        // =========================
        // HÒA
        // =========================
        else
        {
            yield return StartCoroutine(
                CameraManager.Instance.FlyUp(
                    15f,
                    1f
                )
            );

            // Nếu hòa thì mở shop
            ShopManager.Instance.Open();
        }
    }

    // =========================================================
    // MINI GAME CONTROL
    // =========================================================

    // Bắt đầu minigame ngẫu nhiên
    public void JoinRandomMiniGame()
    {
        if (canRandomIndexMiniGame)
        {
            // Random minigame chưa chơi
            miniGameManager.indexMiniGame = GetRandomMiniGameNoRepeat();


        }

        // Chạy minigame
        miniGameManager.StartMiniGame();
    }

    // =========================================================
    // RANDOM MINIGAME KHÔNG LẶP
    // =========================================================

    private int GetRandomMiniGameNoRepeat()
    {
        // Nếu đã chơi hết toàn bộ minigame
        // thì tạo lại danh sách 1 -> 7
        if (remainingMiniGames.Count == 0)
        {
            ResetMiniGameRandomList();
        }

        int randomIndex;

        // Random một minigame trong danh sách chưa chơi
        do
        {
            randomIndex = Random.Range(
                0,
                remainingMiniGames.Count
            );

        }
        // Khi vừa reset danh sách,
        // tránh minigame cuối của chu kỳ trước
        // xuất hiện lại ngay lập tức
        while (
            remainingMiniGames.Count > 1 &&
            remainingMiniGames[randomIndex] == lastMiniGame
        );

        // Lấy index minigame
        int selectedMiniGame =
            remainingMiniGames[randomIndex];

        // Xóa khỏi danh sách
        // => minigame này sẽ không thể xuất hiện lại
        // cho tới khi chơi hết tất cả
        remainingMiniGames.RemoveAt(randomIndex);

        // Lưu lại minigame vừa chơi
        lastMiniGame = selectedMiniGame;



        return selectedMiniGame;
    }

    // Tạo lại danh sách minigame
    private void ResetMiniGameRandomList()
    {
        remainingMiniGames.Clear();

        // MiniGame index từ 1 -> 7
        for (int i = 1; i <= 7; i++)
        {
            remainingMiniGames.Add(i);
        }


    }

    // Có thể gọi hàm này nếu muốn reset toàn bộ
    // lịch sử random khi bắt đầu một game mới
    public void ResetMiniGameRandomSystem()
    {
        remainingMiniGames.Clear();
        lastMiniGame = -1;


    }

    // =========================================================
    // RESET BUFF / DEBUFF
    // =========================================================

    // Reset debuff phép của cả 2 player
    public void ResetMagicDebuffAllPlayer()
    {
        PlayerManager p1 = player1Main.GetComponent<PlayerManager>();
        PlayerManager p2 = player2Main.GetComponent<PlayerManager>();

        if (p1 != null && p1.playerBuff != null)
        {
            p1.playerDebuff.ResetDebuff();
        }

        if (p2 != null && p2.playerBuff != null)
        {
            p2.playerDebuff.ResetDebuff();
        }
    }

    // Reset buff của cả 2 player
    public void ResetBuffAllPlayer()
    {
        PlayerManager p1 = player1Main.GetComponent<PlayerManager>();
        PlayerManager p2 = player2Main.GetComponent<PlayerManager>();

        if (p1 != null && p1.playerBuff != null)
        {
            p1.playerBuff.ResetBuff();
        }

        if (p2 != null && p2.playerBuff != null)
        {
            p2.playerBuff.ResetBuff();
        }
    }

    public void PlayerTeleportToMain()
    {
        PlayerVFX v1 = player1Main.GetComponent<PlayerVFX>();
        PlayerVFX v2 = player2Main.GetComponent<PlayerVFX>();

        if (v1 && v2 != null)
        {
            StartCoroutine(v1.DissolveInRoutine1());
            StartCoroutine(v2.DissolveInRoutine1());
        }
    }

    public void PlayerTeleportToMiniGame()
    {
        PlayerVFX v1 = player1Main.GetComponent<PlayerVFX>();
        PlayerVFX v2 = player2Main.GetComponent<PlayerVFX>();

        if (v1 && v2 != null)
        {
            StartCoroutine(v1.DissolveOutRoutine1());
            StartCoroutine(v2.DissolveOutRoutine1());
        }
    }

    // Chuyển buff xúc xắc của cả 2 player
    public void ConvertBuffDiceAllPlayer()
    {
        PlayerManager p1 = player1Main.GetComponent<PlayerManager>();
        PlayerManager p2 = player2Main.GetComponent<PlayerManager>();

        if (p1 != null && p1.playerBuff != null)
        {
            p1.playerBuff.ConvertBuffDice();
        }

        if (p2 != null && p2.playerBuff != null)
        {
            p2.playerBuff.ConvertBuffDice();
        }
    }

    // =========================================================
    // GAME LOOP / NEXT ROUND
    // =========================================================

    // Reset trạng thái vòng chơi để chuẩn bị lượt mới
    public void ResetGameLoop()
    {
        canStartNextRound = false;
        stateGame.isNextRound = false;
        canCheckPlayer2 = true;
        canCheckMiniGame = true;

        PlayerManager p1 = player1Main.GetComponent<PlayerManager>();
        PlayerManager p2 = player2Main.GetComponent<PlayerManager>();

        p1.playerRound.ResetNextRound();
        p2.playerRound.ResetNextRound();

        // ConvertBuffDiceAllPlayer();
    }

    // Thoát màn hình next round và bắt đầu lượt Player 1 đổ xúc xắc
    public void ExitNextRound()
    {
        ResetGameLoop();

        StartCoroutine(Player1Dice());
    }

    // =========================================================
    // PLAYER 1 DICE TURN
    // =========================================================

    public IEnumerator Player1Dice()
    {
        PlayerManager p1 = player1Main.GetComponent<PlayerManager>();

        // Nếu Player 1 không bị debuff cấm roll dice
        if (!p1.playerDebuff.isNoRollDice)
        {
            yield return new WaitForSeconds(0.5f);

            // Camera follow Player 1
            p1.playerCamera.isFllow2 = true;

            yield return new WaitForSeconds(2f);

            // Tắt follow sau khi camera đã tới
            p1.playerCamera.isFllow2 = false;

            AudioManager.Instance.PlaySpecial(
                AudioManager.Instance.playerOneClip
            );

            // Hiện thông báo roll dice
            yield return StartCoroutine(
                player1Main
                    .GetComponent<PlayerManager>()
                    .playerNotifi
                    .SetNotifi()
            );

            // Cho phép Player 1 bấm xúc xắc
            p1.GetComponent<PlayerManager>()
                .playerInputDice.isClick = false;
        }
        else
        {
            yield return new WaitForSeconds(0.5f);

            // Camera vẫn bay tới Player 1 để báo lượt
            p1.playerCamera.isFllow2 = true;

            AudioManager.Instance.PlaySpecial(
                AudioManager.Instance.skipDiceClip
            );

            yield return new WaitForSeconds(2f);

            p1.playerCamera.isFllow2 = false;

            yield return new WaitForSeconds(1f);

            // Bị cấm roll dice nên bỏ lượt
            // và đánh dấu đã xong lượt
            p1.playerRound.nextRound = true;
        }
    }

    // =========================================================
    // PLAYER 2 DICE TURN
    // =========================================================

    public IEnumerator Player2Dice()
    {
        PlayerManager p2 = player2Main.GetComponent<PlayerManager>();

        // Nếu Player 2 không bị debuff cấm roll dice
        if (!p2.playerDebuff.isNoRollDice)
        {
            yield return new WaitForSeconds(0.5f);

            // Camera follow Player 2
            p2.playerCamera.isFllow2 = true;

            yield return new WaitForSeconds(2f);

            // Tắt follow sau khi camera đã tới
            p2.playerCamera.isFllow2 = false;

            AudioManager.Instance.PlayUI(
                AudioManager.Instance.playerTwoClip
            );

            // Hiện thông báo roll dice
            yield return StartCoroutine(
                player2Main
                    .GetComponent<PlayerManager>()
                    .playerNotifi
                    .SetNotifi()
            );

            // Cho phép Player 2 bấm xúc xắc
            p2.GetComponent<PlayerManager>()
                .playerInputDice.isClick = false;
        }
        else
        {
            yield return new WaitForSeconds(0.5f);

            // Camera vẫn bay tới Player 2 để báo lượt
            p2.playerCamera.isFllow2 = true;

            AudioManager.Instance.PlaySFX(
                AudioManager.Instance.skipDiceClip
            );

            yield return new WaitForSeconds(2f);

            p2.playerCamera.isFllow2 = false;

            yield return new WaitForSeconds(1f);

            // Bị cấm roll dice nên bỏ lượt
            // và đánh dấu đã xong lượt
            p2.playerRound.nextRound = true;
        }
    }

    // =========================================================
    // TURN FLOW CHECK
    // =========================================================

    // Chờ Player 1 đi xong, sau đó chuyển sang Player 2
    public void WaitPlayer1RollDice()
    {
        PlayerManager p1 = player1Main.GetComponent<PlayerManager>();
        PlayerManager p2 = player2Main.GetComponent<PlayerManager>();

        if (
            p1.playerRound.nextRound &&
            !p2.playerRound.nextRound &&
            !stateGame.isNextRound
        )
        {
            canCheckPlayer2 = false;

            StartCoroutine(Player2Dice());

            // Khóa để không gọi Player2Dice liên tục trong Update
            stateGame.isNextRound = true;
        }
    }

    // Nếu cả 2 player đều xong lượt
    // thì bắt đầu minigame tiếp theo
    public void StartNextRound()
    {
        PlayerManager p1 = player1Main.GetComponent<PlayerManager>();
        PlayerManager p2 = player2Main.GetComponent<PlayerManager>();

        if (
            p1.playerRound.nextRound &&
            p2.playerRound.nextRound &&
            !canStartNextRound
        )
        {
            // Cả hai người chơi đã roll xong:
            // khóa Update trong suốt minigame, debuff và shop.
            //canRunGameLoopUpdate = false;
            canCheckMiniGame = false;

            PlayerTeleportToMiniGame();

            JoinRandomMiniGame();

            // Khóa để tránh gọi minigame nhiều lần trong Update
            canStartNextRound = true;
        }
    }

    public bool CheckWinnerByPowerCoinP1()
    {
        PlayerManager p =
            player1Main.GetComponent<PlayerManager>();

        int coinCount =
            p.playerBuff.countCoinPower;

        bool i =
            CheckWinPlayer.Instance
                .CheckWinnerByCoinPower(coinCount);

        if (i)
        {
            stateGame.SetOnePlayerWin(0);

            AudioManager.Instance.PlaySFX(
                AudioManager.Instance.fourPowerCoinClip
            );

            stateGame.hasWinByCoin = true;

            SendPlayerWinner.Instance
                .SetInforPlayerWinner(player1Main);

            return true;
        }

        return false;
    }

    public bool CheckWinnerByPowerCoinP2()
    {
        PlayerManager p =
            player2Main.GetComponent<PlayerManager>();

        int coinCount =
            p.playerBuff.countCoinPower;

        bool i =
            CheckWinPlayer.Instance
                .CheckWinnerByCoinPower(coinCount);

        if (i)
        {
            AudioManager.Instance.PlaySpecialOneShot(
                AudioManager.Instance.fourPowerCoinClip
            );

            stateGame.SetOnePlayerWin(1);

            stateGame.hasWinByCoin = true;

            SendPlayerWinner.Instance
                .SetInforPlayerWinner(player2Main);

            return true;
        }

        return false;
    }

    public void CheckWinnerOrNextRound()
    {
        if (
            stateGame.hasPlayer1Win ||
            stateGame.hasPlayer2Win
        )
        {
            ResetMagicDebuffAllPlayer();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideUIMain();
                UIManager.Instance.ActiveOpenSettingButton(false);
            }

            if (SettingManager.Instance != null)
            {
                SettingManager.Instance.enableSettingMusic = false;
                SettingManager.Instance.canOpenSettingByEsc = true;
                // SettingManager.Instance.isOpenExitButton = false;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();

                AudioManager.Instance.PlayMusic(
                    AudioManager.Instance.winnerMiniGameClip1
                );
            }
            if (CursorManager.Instance != null)
            {

                CursorManager.Instance.SetSceneCursorVisible(false);
                CursorManager.Instance.SetSettingCursorActive(false);
            }

            // PointCheck.Instance.HideAllTraps();

            StartCutSceneWinner();
        }
        else
        {
            var cursor = CursorManager.Instance;

            if (cursor != null)
            {
                cursor.ShowGameCursor();
            }

            ExitNextRound();
        }
    }

    public bool CheckWinnerByIndex(
        bool isPlayer2,
        int index
    )
    {
        bool i =
            CheckWinPlayer.Instance
                .CheckWinnerByIndex(index);

        if (i)
        {
            if (!isPlayer2)
            {
                stateGame.SetOnePlayerWin(0);

                SendPlayerWinner.Instance
                    .SetInforPlayerWinner(player1Main);
            }
            else
            {
                stateGame.SetOnePlayerWin(1);

                SendPlayerWinner.Instance
                    .SetInforPlayerWinner(player2Main);
            }

            stateGame.hasWinByIndex = true;

            // Khóa flow game lại
            canCheckPlayer2 = false;
            canCheckMiniGame = false;
            canStartNextRound = true;
            stateGame.isNextRound = true;

            AudioManager.Instance.PlaySpecialOneShot(
                AudioManager.Instance.threethirtyIndexClip
            );

            CheckWinnerOrNextRound();

            return true;
        }

        return false;
    }

    public void StartCutSceneWinner()
    {
        if (stateGame.hasWinByCoin)
        {
            if (stateGame.hasPlayer1Win)
            {
                CutScenePowerCoin.Instance
                    .PlayCutScene(player1Main,player2Main);
            }
            else if (stateGame.hasPlayer2Win)
            {
                CutScenePowerCoin.Instance
                    .PlayCutScene(player2Main,player1Main);
            }
        }
        else if (stateGame.hasWinByIndex)
        {
            if (stateGame.hasPlayer1Win)
            {
                CutSceneToIndex.Instance
                    .PlayCutScene(player1Main);
            }
            else if (stateGame.hasPlayer2Win)
            {
                CutSceneToIndex.Instance
                    .PlayCutScene(player2Main);
            }
        }
    }
}