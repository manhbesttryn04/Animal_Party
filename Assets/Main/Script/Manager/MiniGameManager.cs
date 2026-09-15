using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #region INSPECTOR / STATE

    // =========================================================
    // INDEX
    // =========================================================

    [Header("Index MiniGame")]
    public int indexMiniGame = 1;

    // =========================================================
    // DATA LIST
    // =========================================================

    [Header("MiniGame Data")]
    public MiniGameList miniGameList;
    public IntrusTextList intrusTextList;
    public VideoInstructList videoInstructList;
    public MapMiniGameList mapMiniGameList;
    public TimeMinigame timeMinigame;
    public InstructInputMinigame inputMinigame;
    // =========================================================
    // CAMERA
    // =========================================================

    [Header("Camera")]
    public CameraCutList miniGameCamera;

    [Header("Video MiniGame")]
    public VideoPlayer videoIntrucs;

    // =========================================================
    // SPAWN
    // =========================================================

    [Header("Spawn")]
    public TransSpawPlayerList spawnPoint;

    // =========================================================
    // CURRENT PLAYERS
    // =========================================================

    [Header("Current Players")]
    public GameObject currentPlayer1;
    public GameObject currentPlayer2;

    // =========================================================
    // TIMER
    // =========================================================

    [Header("Countdown Time")]
    public float countDownTime = 99f;
    public float timer;

    // =========================================================
    // STATE
    // =========================================================

    [Header("Game State")]
    public bool isPlaying = false;

    #endregion


    #region PUBLIC ENTRY

    // =========================================================
    // START MINIGAME
    // =========================================================

    public void StartMiniGame()
    {
        if (isPlaying)
            return;

        StartCoroutine(MiniGameRoutine());
    }

    #endregion


    #region MAIN MINIGAME FLOW

    // =========================================================
    // MAIN ROUTINE
    // =========================================================

    private IEnumerator MiniGameRoutine()
    {
        // =====================================================
        // CACHE SINGLETON
        // =====================================================

        var ui = UIManager.Instance;
        var setting = SettingManager.Instance;
        var cursor = CursorManager.Instance;
        var audio = AudioManager.Instance;
        var loading = LoadingManager.Instance;
        var character = CharacterManager.Instance;
        var gameManager = GameManager.Instance;
        var volume = VolumeManager.Instance;
        var maincharacterandmap = MapAndCharacterManager.Instance;

        // =====================================================
        // CHECK SINGLETON
        // =====================================================

        if (ui == null)
        {
            yield break;
        }

        if (setting == null)
        {
            yield break;
        }

        if (audio == null)
        {
            yield break;
        }

        if (loading == null)
        {
            yield break;
        }

        if (character == null)
        {
            yield break;
        }

        if (gameManager == null)
        {
            yield break;
        }

        // =====================================================
        // CHECK INDEX TRƯỚC
        // =====================================================

        if (indexMiniGame <= 0)
        {
            yield break;
        }

        int miniGameIndex = indexMiniGame - 1;

        // =====================================================
        // CHECK DATA NULL
        // =====================================================

        if (mapMiniGameList == null)
        {
            yield break;
        }

        if (miniGameCamera == null)
        {
            yield break;
        }

        if (spawnPoint == null)
        {
            yield break;
        }

        if (miniGameList == null)
        {
            yield break;
        }

        // =====================================================
        // CHECK INDEX LIST
        // =====================================================

        if (miniGameIndex >= mapMiniGameList.mapMiniGameList.Count)
        {
            yield break;
        }

        if (miniGameIndex >= miniGameCamera.cameraList.Count)
        {
            yield break;
        }

        if (miniGameIndex >= spawnPoint.transSpawPlayerList.Count)
        {
            yield break;
        }

        // =====================================================
        // SETUP BAN ĐẦU
        // =====================================================

        SetUpStartLightAndTime();

        isPlaying = true;

        // Tắt map chính
       
        if(maincharacterandmap != null)
        {
            var mainmap = maincharacterandmap.mainMap;
            if(mainmap != null)
            {
                mainmap.SetActive(false);
            }
        }

        // Bật map minigame
        mapMiniGameList.mapMiniGameList[miniGameIndex].SetActive(true);

        // Tắt nhạc map chính
        audio.PauseAudio();

        // Ẩn bảng thông báo play
        ui.HideNotifiPlayPanel(false);

        // Không cho mở setting bằng controller
        setting.canOpenSettingByController = false;

        // Cursor
        if (cursor != null)
        {
            cursor.HideGameCursor();
        }

        // Tắt nút mở setting
        ui.ActiveOpenSettingButton(false);

        // =====================================================
        // LOADING
        // =====================================================

        yield return StartCoroutine(
            loading.ShowLoading()
        );

        loading.HideLoading();

        setting.ResetSetting();

        // =====================================================
        // BLACK PANEL
        // =====================================================

        ui.flastBlackPanel.SetActive(false);
        ui.flastBlackPanel.SetActive(true);

        // =====================================================
        // MUSIC MINIGAME
        // =====================================================

        SetupMusicMiniGame();

        // =====================================================
        // ENABLE CAMERA
        // =====================================================

        miniGameCamera
            .cameraList[miniGameIndex]
            .gameObject
            .SetActive(true);

        // =====================================================
        // ENABLE TIMER UI
        // =====================================================

        ui.timeMiniGameText.gameObject.SetActive(true);

        // =====================================================
        // SPAWN PLAYER
        // =====================================================

        Transform spawn =
            spawnPoint.transSpawPlayerList[miniGameIndex];

        // Player 1
        currentPlayer1 = Instantiate(
            character.playerPlaylist[character.indexPlayer1],
            spawn.position,
            Quaternion.identity
        );

        // Player 2
        currentPlayer2 = Instantiate(
            character.playerPlaylist[character.indexPlayer2],
            spawn.position + Vector3.right * 2f,
            Quaternion.identity
        );

        // =====================================================
        // GET COMPONENT PLAYER
        // =====================================================

        PlayerInfo avatar1 =
            currentPlayer1.GetComponent<PlayerInfo>();

        PlayerInfo avatar2 =
            currentPlayer2.GetComponent<PlayerInfo>();

        PlayerCoin coin1 =
            currentPlayer1.GetComponent<PlayerCoin>();

        PlayerCoin coin2 =
            currentPlayer2.GetComponent<PlayerCoin>();

        PlayerMiniGame p1 =
            currentPlayer1.GetComponent<PlayerMiniGame>();

        PlayerMiniGame p2 =
            currentPlayer2.GetComponent<PlayerMiniGame>();

        PlayerType player2Type =
            currentPlayer2.GetComponent<PlayerType>();

        PlayerMove move1 =
            currentPlayer1.GetComponent<PlayerMove>();

        PlayerMove move2 =
            currentPlayer2.GetComponent<PlayerMove>();

        PlayerVFX vfx1 =
            currentPlayer1.GetComponent<PlayerVFX>();

        PlayerVFX vfx2 =
            currentPlayer2.GetComponent<PlayerVFX>();

        // =====================================================
        // CHECK COMPONENT
        // =====================================================

        if (vfx1 == null || vfx2 == null)
        {
            yield break;
        }

        if (avatar1 == null || avatar2 == null)
        {
            yield break;
        }

        if (coin1 == null || coin2 == null)
        {
            yield break;
        }

        if (p1 == null || p2 == null)
        {
            yield break;
        }

        if (player2Type == null)
        {
            yield break;
        }

        // =====================================================
        // LOCK PLAYER MOVE
        // =====================================================

        yield return new WaitForSeconds(0.5f);

        if (move1 != null)
        {
            move1.isJumpAndMove = false;
        }

        if (move2 != null)
        {
            move2.isJumpAndMove = false;
        }

        // =====================================================
        // PLAYER DISSOLVE
        // =====================================================

        if (vfx2 != null)
        {
            StartCoroutine(
                vfx2.DissolveInRoutine1()
            );
        }

        if (vfx1 != null)
        {
            StartCoroutine(
                vfx1.DissolveInRoutine1()
            );
        }

        // =====================================================
        // SETUP PLAYER
        // =====================================================

        player2Type.isPlayer2 = true;

        p1.checkPoint = spawn;
        p2.checkPoint = spawn;

        // =====================================================
        // UPDATE UI BAN ĐẦU
        // =====================================================

        ui.avatarP1.sprite =
            avatar1.avatarCharacter;

        ui.avatarP2.sprite =
            avatar2.avatarCharacter;

        ui.coinMiniGameTextP1.text =
            coin1.coinMiniGame.ToString();

        ui.coinMiniGameTextP2.text =
            coin2.coinMiniGame.ToString();

        // =====================================================
        // PLAY CUTSCENE
        // =====================================================

        if (miniGameCamera.MiniGameCameraList[miniGameIndex] != null)
        {
            yield return StartCoroutine(
                miniGameCamera
                    .MiniGameCameraList[miniGameIndex]
                    .PlayCutscene()
            );
        }

        if(maincharacterandmap != null)
        {
            var maincharacter = maincharacterandmap.mainCharacters;
            if(maincharacter != null)
            {
                maincharacter.SetActive(false);
            }
        }

        // =====================================================
        // SHOW INSTRUCTION
        // =====================================================

        ui.canvasIntructGamePlay.SetActive(true);

        ui.instructGamePlayText.text =
            intrusTextList.instructTextList[miniGameIndex];

        ui.nameMiniGameText.text =
            intrusTextList.nameMiniGameList[miniGameIndex];

        ui.errorGamePlayText.text =
            intrusTextList.errorTextList[miniGameIndex];

        // =====================================================
        // VIDEO INSTRUCTION
        // =====================================================

        if (videoInstructList.videoInstructList[miniGameIndex] != null)
        {
            videoIntrucs.clip =
                videoInstructList.videoInstructList[miniGameIndex];
        }

        // =====================================================
        // WAIT INSTRUCTION
        // =====================================================

        yield return new WaitForSeconds(5f);

        videoIntrucs.Stop();
        videoIntrucs.time = 0;

        if (videoIntrucs.targetTexture != null)
        {
            videoIntrucs.targetTexture.Release();
        }

        videoIntrucs.clip = null;

        // Tắt bảng hướng dẫn
        ui.canvasIntructGamePlay.SetActive(false);

        // =====================================================
        // INPUT INSTRUCTION
        // =====================================================

        ui.canvasInstructInput.SetActive(true);

        if (inputMinigame != null)
        {
            inputMinigame.ShowInputMinigame(
                indexMiniGame
            );
        }

        yield return new WaitForSeconds(5f);

        if (inputMinigame != null)
        {
            inputMinigame.HideAllInput();
        }

        ui.canvasInstructInput.SetActive(false);

        // =====================================================
        // END PREPARATION
        // =====================================================

        ui.flastBlackPanel.SetActive(false);

        ui.ActiveOpenSettingButton(true);

        if (cursor != null)
        {
            cursor.ShowGameCursor();
        }

        setting.canOpenSettingByController = true;

        // =====================================================
        // UNLOCK PLAYER MOVE
        // =====================================================

        if (move1 != null)
        {
            move1.isJumpAndMove = true;
        }

        if (move2 != null)
        {
            move2.isJumpAndMove = true;
        }

        // =====================================================
        // SHOW GAME UI
        // =====================================================

        ui.canvasMiniGame.SetActive(true);

        // =====================================================
        // START MINIGAME LOGIC
        // =====================================================

        StartMiniGameByIndex();

        // =====================================================
        // TIMER LOOP
        // =====================================================

        timer = countDownTime;

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            int seconds =
                Mathf.CeilToInt(timer);

            int minutes =
                seconds / 60;

            int remainSeconds =
                seconds % 60;

            ui.timeMiniGameText.text =
                minutes.ToString("00") +
                ":" +
                remainSeconds.ToString("00");

            ui.coinMiniGameTextP1.text =
                coin1.coinMiniGame.ToString();

            ui.coinMiniGameTextP2.text =
                coin2.coinMiniGame.ToString();

            yield return null;
        }

        // =====================================================
        // MINIGAME 4 TIMEOUT
        // =====================================================

        if (indexMiniGame == 4 &&
            miniGameList != null &&
            miniGameList.miniGame4 != null)
        {
            ui.timeMiniGameText.text = "00:00";

            miniGameList.miniGame4
                .BeginTimeoutSequence();

            float maxWaitTime = 20f;

            while (
                miniGameList.miniGame4.IsFinishingSequence &&
                maxWaitTime > 0f)
            {
                ui.timeMiniGameText.text = "00:00";

                maxWaitTime -= Time.deltaTime;

                yield return null;
            }
        }

        // =====================================================
        // MINIGAME 7 TIMEOUT
        // =====================================================

        if (indexMiniGame == 7 &&
            miniGameList != null &&
            miniGameList.miniGame7 != null)
        {
            float maxWaitTime = 10f;

            while (
                miniGameList.miniGame7.IsFinishingSequence &&
                maxWaitTime > 0f)
            {
                ui.timeMiniGameText.text = "00:00";

                maxWaitTime -= Time.deltaTime;

                yield return null;
            }
        }

        // =====================================================
        // TIME OUT
        // =====================================================

        ui.timeMiniGameText.text = "00:00";

        // =====================================================
        // STOP MINIGAME
        // =====================================================

        audio.StopMusic();
        audio.ZeroAllAudio();

        ExitStopMiniGame();

        ui.canvasMiniGame.SetActive(false);

        // =====================================================
        // SHOW RESULT
        // =====================================================

        audio.ZeroAllAudio();

        setting.canOpenSettingByController = false;
       
        ui.ActiveOpenSettingButton(false);


        if (cursor != null)
        {
            cursor.HideGameCursor();
        }

        setting.ResetSetting();

        ui.UpdateResultPanel(
            coin1.coinMiniGame,
            coin2.coinMiniGame
        );

        // =====================================================
        // WAIT RESULT
        // =====================================================

        yield return new WaitForSeconds(4f);

        // =====================================================
        // HIDE RESULT
        // =====================================================

        ui.HideResultPanel();

        // =====================================================
        // LOADING BACK TO MAIN MAP
        // =====================================================

        yield return StartCoroutine(
            loading.ShowLoading()
        );

        audio.SetupMainGameAudio();

        if (maincharacterandmap != null)
        {
            var maincharacter = maincharacterandmap.mainCharacters;
            if (maincharacter != null)
            {
                maincharacter.SetActive(true);
            }
        }

        // Bật lại map chính
        if (maincharacterandmap != null)
        {
            var mainmap = maincharacterandmap.mainMap;
            if (mainmap != null)
            {
                mainmap.SetActive(true);
            }
        }

        // Reset light
        ResetLight();

        // Tăng index
        SetIndex();

        loading.HideLoading();

        // =====================================================
        // ENABLE SETTING / CURSOR
        // =====================================================

        ui.ActiveOpenSettingButton(true);

        if (cursor != null)
        {
            cursor.ShowGameCursor();
        }

        setting.canOpenSettingByController = true;

        // =====================================================
        // DISABLE CAMERA
        // =====================================================

        miniGameCamera
            .cameraList[miniGameIndex]
            .gameObject
            .SetActive(false);

        // =====================================================
        // HIDE TIMER
        // =====================================================

        ui.timeMiniGameText.gameObject.SetActive(false);

        // =====================================================
        // CHECK WINNER
        // =====================================================

        gameManager.CheckPlayerWinRound(
            coin1.coinMiniGame,
            coin2.coinMiniGame
        );

        // =====================================================
        // RESET DEBUFF
        // =====================================================

        gameManager.ResetMagicDebuffAllPlayer();

        gameManager.PlayerTeleportToMain();

        // =====================================================
        // CONVERT BUFF DICE
        // =====================================================

        gameManager.ConvertBuffDiceAllPlayer();

        // =====================================================
        // DESTROY PLAYER
        // =====================================================

        Destroy(currentPlayer1);
        Destroy(currentPlayer2);

        currentPlayer1 = null;
        currentPlayer2 = null;

        // =====================================================
        // DISABLE MAP MINIGAME
        // =====================================================

        mapMiniGameList
            .mapMiniGameList[miniGameIndex]
            .SetActive(false);

        // =====================================================
        // NEXT ROUND AUDIO
        // =====================================================

        audio.PlaySFX(audio.nextRound);

        // =====================================================
        // RESET STATE
        // =====================================================

        isPlaying = false;

        // Hiện lại bảng play
        ui.HideNotifiPlayPanel(true);

        // Mở lại nhạc main map
        audio.PlayMusic(audio.musicMainClip);
    }

    #endregion


    #region MINIGAME START / STOP DISPATCH

    // =========================================================
    // START MINIGAME LOGIC BY INDEX
    // =========================================================

    public void StartMiniGameByIndex()
    {
        if (indexMiniGame == 1)
        {
            miniGameList.miniGame1.StartMiniGame();
        }
        else if (indexMiniGame == 2)
        {
            miniGameList.miniGame2.StartMiniGame();
        }
        else if (indexMiniGame == 3)
        {
            miniGameList.miniGame3.StartMiniGame();
        }
        else if (indexMiniGame == 4)
        {
            miniGameList.miniGame4.StartMiniGame();
        }
        else if (indexMiniGame == 5)
        {
            miniGameList.miniGame5.StartMiniGame();
        }
        else if (indexMiniGame == 6)
        {
            miniGameList.miniGame6.StartMiniGame();
        }
        else if (indexMiniGame == 7)
        {
            miniGameList.miniGame7.StartMiniGame();
        }
        else if (indexMiniGame == 8)
        {
            // Chưa có minigame 8
        }
        else if (indexMiniGame == 9)
        {
            // Chưa có minigame 9
        }
        else if (indexMiniGame == 10)
        {
            // Chưa có minigame 10
        }
    }

    // =========================================================
    // STOP MINIGAME LOGIC BY INDEX
    // =========================================================

    public void ExitStopMiniGame()
    {
        if (indexMiniGame == 1)
        {
            miniGameList.miniGame1.StopMiniGame();
        }
        else if (indexMiniGame == 2)
        {
            miniGameList.miniGame2.StopMiniGame();
        }
        else if (indexMiniGame == 3)
        {
            miniGameList.miniGame3.StopMiniGame();
        }
        else if (indexMiniGame == 4)
        {
            miniGameList.miniGame4.StopMiniGame();
        }
        else if (indexMiniGame == 5)
        {
            miniGameList.miniGame5.StopMiniGame();
        }
        else if (indexMiniGame == 6)
        {
            miniGameList.miniGame6.StopMiniGame();
        }
        else if (indexMiniGame == 7)
        {
            miniGameList.miniGame7.StopMiniGame();
        }
        else if (indexMiniGame == 8)
        {
            // Chưa có minigame 8
        }
        else if (indexMiniGame == 9)
        {
            // Chưa có minigame 9
        }
        else if (indexMiniGame == 10)
        {
            // Chưa có minigame 10
        }
    }

    #endregion


    #region AUDIO / TIME / LIGHT

    // =========================================================
    // OPEN MUSIC MINIGAME
    // =========================================================

    public void SetupMusicMiniGame()
    {
        AudioManager.Instance
            .SetupMusicMiniGame(indexMiniGame);
    }

    // =========================================================
    // SETUP TIME BY MINIGAME
    // =========================================================

    public void SetUpStartLightAndTime()
    {
        switch (indexMiniGame)
        {
            case 1:
                countDownTime =
                    timeMinigame.timeMinigame1;
                break;

            case 2:
                countDownTime =
                    timeMinigame.timeMinigame2;

                VolumeManager.Instance
                    .SetBloomIntensity(0.5f);
                break;

            case 3:
                countDownTime =
                    timeMinigame.timeMinigame3;
                break;

            case 4:
                countDownTime =
                    timeMinigame.timeMinigame4;
                break;

            case 5:
                countDownTime =
                    timeMinigame.timeMinigame5;

                VolumeManager.Instance
                    .SetBloomIntensity(1f);
                break;

            case 6:
                countDownTime =
                    timeMinigame.timeMinigame6;
                break;

            case 7:
                countDownTime =
                    timeMinigame.timeMinigame7;
                break;

            case 8:
                break;

            case 9:
                break;

            case 10:
                break;

            default:
                break;
        }
    }

    // =========================================================
    // RESET LIGHT
    // =========================================================

    public void ResetLight()
    {
        VolumeManager.Instance
            .SetBloomIntensity(4);
    }

    // =========================================================
    // SET INDEX
    // =========================================================

    public void SetIndex()
    {
        indexMiniGame++;

        if (indexMiniGame > 7)
        {
            indexMiniGame = 1;
        }
    }
    public void DestroyAllPlayerMiniGame()
    {
        if(currentPlayer1 != null && currentPlayer2 != null)
        {
            Destroy(currentPlayer1);
            Destroy(currentPlayer2);
        }
    }

    #endregion
}