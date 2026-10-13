using System.Collections;
using UnityEngine;
using TMPro;

public class RaceMiniGame : MonoBehaviour
{
    public static RaceMiniGame Instance;

    [Header("--- MANAGER ---")]
    public MiniGameManager manager;

    [Header("--- TEST (không cần MiniGameManager) ---")]
    public GameObject testPlayer1;
    public GameObject testPlayer2;

    [Header("--- CẤU HÌNH VÒNG CHƠI ---")]
    public float firstWaitTime = 3f;

    [Header("--- TRACK ---")]
    public Transform spawnPoint1;
    public Transform spawnPoint2;
    [Tooltip("Tổng số checkpoint trên track (tính cả vạch đích)")]
    public int totalCheckpoints = 5;
    [Tooltip("Index của checkpoint cuối cùng = vạch đích")]
    public int finishCheckpointIndex = 4;

    [Header("--- CAMERA (SPLIT-SCREEN DỌC) ---")]
    public Camera cam1;
    public Camera cam2;

    [Header("--- UI ---")]
    public GameObject miniGameCanvas;
    public TMP_Text countdownText;
    public TMP_Text messageText;
    public TMP_Text p1ProgressText;
    public TMP_Text p2ProgressText;
    public TMP_Text resultText;
    public GameObject resultPanel;

    [Header("--- ÂM THANH TRẬN ĐẤU ---")]
    [Tooltip("AudioSource riêng cho nhạc nền (loop), tự Play lúc StartMiniGame, tự Stop lúc StopMiniGame/EndGame")]
    public AudioSource musicSource;
    [Tooltip("AudioSource dùng để phát SFX 1 lần (thắng/thua)")]
    public AudioSource sfxSource;
    [Tooltip("Nhạc/hiệu ứng khi có người thắng")]
    public AudioClip winSound;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    // Internal
    private GameObject p1obj, p2obj;
    private PlayerSubmarineController sub1, sub2;
    private CheckpointTracker tracker1, tracker2;

    private bool isRunning = false;
    private bool raceActive = false;

    private void Awake() { Instance = this; }

    // ====== BẮT ĐẦU ======
    public void StartMiniGame()
    {
        if (isRunning) return;
        if (miniGameCanvas != null) miniGameCanvas.SetActive(true);

        p1obj = manager != null ? manager.currentPlayer1 : testPlayer1;
        p2obj = manager != null ? manager.currentPlayer2 : testPlayer2;

        if (p1obj == null || p2obj == null)
        {
            Debug.LogWarning("[RACE] Missing player!");
            return;
        }

        // Đưa tàu ngầm về vị trí xuất phát
        if (spawnPoint1 != null)
        {
            p1obj.transform.SetPositionAndRotation(spawnPoint1.position, spawnPoint1.rotation);
        }
        if (spawnPoint2 != null)
        {
            p2obj.transform.SetPositionAndRotation(spawnPoint2.position, spawnPoint2.rotation);
        }

        sub1 = p1obj.GetComponent<PlayerSubmarineController>();
        sub2 = p2obj.GetComponent<PlayerSubmarineController>();
        tracker1 = p1obj.GetComponent<CheckpointTracker>();
        tracker2 = p2obj.GetComponent<CheckpointTracker>();

        if (sub1 == null || sub2 == null || tracker1 == null || tracker2 == null)
        {
            Debug.LogWarning("[RACE] Missing PlayerSubmarineController/CheckpointTracker on player!");
            return;
        }

        sub1.playerId = PlayerSubmarineController.PlayerID.Player1;
        sub2.playerId = PlayerSubmarineController.PlayerID.Player2;

        SetupSplitScreen();

        // Reset UI
        if (messageText) messageText.text = "";
        if (countdownText) countdownText.text = "";
        if (resultPanel) resultPanel.SetActive(false);

        PlayMusic();

        isRunning = true;
        StartCoroutine(GameRoutine());
    }

    // Chia màn hình dọc trái/phải, mỗi camera tự gắn script follow riêng
    void SetupSplitScreen()
    {
        if (cam1 != null)
        {
            cam1.rect = new Rect(0f, 0f, 0.5f, 1f);
            SplitScreenCameraFollow follow1 = cam1.GetComponent<SplitScreenCameraFollow>();
            if (follow1 == null) follow1 = cam1.gameObject.AddComponent<SplitScreenCameraFollow>();
            follow1.target = p1obj.transform;
        }

        if (cam2 != null)
        {
            cam2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            SplitScreenCameraFollow follow2 = cam2.GetComponent<SplitScreenCameraFollow>();
            if (follow2 == null) follow2 = cam2.gameObject.AddComponent<SplitScreenCameraFollow>();
            follow2.target = p2obj.transform;
        }
        Debug.Log($"Cam1 rect: {cam1.rect}, Cam2 rect: {cam2.rect}");
    }

    // ====== ÂM THANH ======
    void PlayMusic()
    {
        if (musicSource == null) return;
        musicSource.volume = musicVolume;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // ====== DỪNG ======
    public void StopMiniGame()
    {
        isRunning = false;
        raceActive = false;
        StopAllCoroutines();

        sub1?.SetGameActive(false);
        sub2?.SetGameActive(false);

        if (messageText != null) messageText.text = "";
        if (countdownText != null) countdownText.text = "";

        if (miniGameCanvas != null) miniGameCanvas.SetActive(false);

        StopMusic();
    }

    // ====== GAME ROUTINE ======
    IEnumerator GameRoutine()
    {
        for (int i = (int)firstWaitTime; i > 0; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText) countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);
        if (countdownText) countdownText.text = "";

        StartRace();
    }

    void StartRace()
    {
        if (!isRunning) return;

        tracker1.SetGameActive(true);
        tracker2.SetGameActive(true);
        sub1.SetGameActive(true);
        sub2.SetGameActive(true);

        raceActive = true;
        if (messageText) messageText.text = "";
    }

    private void Update()
    {
        if (!raceActive || !isRunning) return;

        if (p1ProgressText) p1ProgressText.text = $"{Mathf.RoundToInt(tracker1.GetProgress(totalCheckpoints) * 100)}%";
        if (p2ProgressText) p2ProgressText.text = $"{Mathf.RoundToInt(tracker2.GetProgress(totalCheckpoints) * 100)}%";
    }

    // ====== KHI CÓ NGƯỜI VỀ ĐÍCH (gọi từ CheckpointTracker) ======
    public void OnPlayerFinish(CheckpointTracker finisher)
    {
        if (!raceActive || !isRunning) return;

        bool isPlayer1Win = finisher == tracker1;
        EndGame(isPlayer1Win);
    }

    // ====== KẾT THÚC ======
    void EndGame(bool isPlayer1Win)
    {
        raceActive = false;
        isRunning = false;

        sub1?.SetGameActive(false);
        sub2?.SetGameActive(false);

        if (resultPanel != null) resultPanel.SetActive(true);

        string winnerName = isPlayer1Win ? "PLAYER 1" : "PLAYER 2";
        string color = isPlayer1Win ? "red" : "green";

        if (resultText != null) resultText.text = $"<color={color}>{winnerName} WINS!</color>";
        if (messageText != null) messageText.text = $"{winnerName} WINS!";

        StopMusic();
        PlaySfx(winSound);

        if (manager != null)
        {
            CheckFinishReward(manager.currentPlayer1, isPlayer1Win ? 1 : 0);
            CheckFinishReward(manager.currentPlayer2, isPlayer1Win ? 0 : 1);
        }
    }

    void CheckFinishReward(GameObject playerObj, int checkWin)
    {
        if (playerObj == null) return;

        PlayerMiniGame mini = playerObj.GetComponent<PlayerMiniGame>();
        if (mini == null) return;

        mini.UpCoin(checkWin, 100);
    }
    private void Start()
    {
        StartMiniGame(); // TEST — xóa dòng này sau khi test xong
    }
}