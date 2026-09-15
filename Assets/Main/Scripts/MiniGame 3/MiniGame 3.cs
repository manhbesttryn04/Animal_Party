using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGame3 : MonoBehaviour
{
    [System.Serializable]
    public class Phase1_LaserHub
    {
        [Tooltip("Giây thứ mấy thì bắt đầu màn Trụ Quay? (Mặc định là 0)")]
        public float startTime = 0f;
    }

    [System.Serializable]
    public class Phase2_Flamethrower
    {
        [Tooltip("Giây thứ mấy thì Trụ Quay chìm xuống và Bắt đầu khạc lửa?")]
        public float startTime = 30f;
    }

    [System.Serializable]
    public class Phase3_LaserSpam
    {
        [Tooltip("Giây thứ mấy thì Tắt Lửa, bắt đầu Lazer Bay?")]
        public float startTime = 60f;

        [Header("Độ Khó (Difficulty)")]
        public float laserSpeed = 6f;
        [Tooltip("Số lượng Lazer bắn ra trong 1 đợt (Wave)")]
        public int lasersPerWave = 4;
        [Tooltip("Thời gian nghỉ giữa 2 đợt bắn (Wave Delay)")]
        public float waveDelay = 3.5f;
        [Tooltip("Độ trễ nhịp bắn liên thanh giữa các tia trong cùng 1 đợt")]
        public float delayBetweenLasers = 1.2f;
    }

    [System.Serializable]
    public class Phase4_LaserHard
    {
        [Tooltip("Giây thứ mấy thì Lazer Bay tăng tốc Ép Xung?")]
        public float startTime = 90f;

        [Header("Độ Khó (Difficulty)")]
        public float laserSpeed = 8.5f;
        public int lasersPerWave = 5;
        public float waveDelay = 3f;
        public float delayBetweenLasers = 0.8f;
    }

    public enum SpawnMode
    {
        Random, Alternating, DoubleAlternating, Burst, BothAtSameTime, AutoMixed
    }

    // ==========================================
    // CÁC BIẾN CHÍNH TRÊN INSPECTOR
    // ==========================================
    [Header("MiniGame Manager")]
    public MiniGameManager manager;
    [Header("Fix Bug")]
    [SerializeField] private FixBugMiniGame3 fixBugMiniGame3;

    [Header("--- TỔNG THỜI GIAN GAME ---")]
    [Tooltip("Tổng thời gian sinh tồn (Giây)")]
    public float totalGameTime = 120f;

    [Header("--- THIẾT LẬP KỊCH BẢN (TIMELINE) ---")]
    public Phase1_LaserHub phase1_Hub;
    public Phase2_Flamethrower phase2_Fire;
    public Phase3_LaserSpam phase3_Spam;
    public Phase4_LaserHard phase4_Hard;

    [Header("--- THÀNH PHẦN KẾT NỐI (REFERENCES) ---")]
    public PummelLaserHub centralHub;
    public GameObject spamLaserPrefab;
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("--- BẪY LỬA ---")]
    public GameObject[] flamethrowerTraps;
    // Kéo Master_Flamethrower vào đây
    public SyncedFlamethrowerBrain flamethrowerBrain;

    [Header("Team Integration")]
    public bool isRunning = false;

    // Các biến chạy ngầm quản lý state
    private float timeBetweenWaves;
    private int lasersPerWave;
    private float delayBetweenLasers;
    private float currentLaserSpeed;
    private SpawnMode currentMode;

    private float survivalTime;
    private float waveTimer;
    private int spawnCounter;
    private int lastSpawnIndex;
    private int currentPhase;
    private bool isGameOver;
    private bool isSpawningWave;

    private void Start()
    {
        ResetMiniGameState();

        if (centralHub != null)
            centralHub.gameObject.SetActive(false);

        ToggleFlamethrowers(false);
    }

    public void StartMiniGame()
    {
        if (fixBugMiniGame3 != null)
            fixBugMiniGame3.SetupPlayers();
        SetUpAllPlayer();
        StopAllCoroutines();
        ClearAllLasers();
        ResetMiniGameState();

        isRunning = true;
        isGameOver = false;

        if (centralHub != null)
        {
            centralHub.gameObject.SetActive(true);
            centralHub.ResetHub();
        }

        UpdateDifficultyPhase();
    }

    public void StopMiniGame()
    {
        // Không return sớm theo isRunning/isGameOver.
        // Manager có thể đã đổi state trước khi gọi hàm này, nhưng bẫy vẫn
        // luôn cần được cleanup cưỡng bức.
        ShutdownMiniGame(true);
    }

    private void OnDisable()
    {
        // Lớp an toàn khi manager tắt thẳng GameObject/minigame map.
        StopAllCoroutines();
        ToggleFlamethrowers(false);
    }

    private void Update()
    {
        if (!isRunning || isGameOver) return;

        survivalTime += Time.deltaTime;

        if (survivalTime >= totalGameTime)
        {
            CompleteMiniGame();
            return;
        }

        UpdateDifficultyPhase();

        if (lasersPerWave > 0)
        {
            waveTimer -= Time.deltaTime;

            if (waveTimer <= 0f && !isSpawningWave)
            {
                StartCoroutine(SpawnSpamWave());
                waveTimer = timeBetweenWaves;
            }
        }
    }

    private void ResetMiniGameState()
    {
        survivalTime = 0f;
        waveTimer = 3f;

        currentPhase = 0;
        spawnCounter = 0;
        lastSpawnIndex = 0;
        isSpawningWave = false;

        timeBetweenWaves = 5f;
        lasersPerWave = 0;
        delayBetweenLasers = 1.2f;
        currentLaserSpeed = 4f;
        currentMode = SpawnMode.Alternating;

        isRunning = false;
        isGameOver = false;

        ToggleFlamethrowers(false);
    }

    private void CompleteMiniGame()
    {
        ShutdownMiniGame(true);
    }

    private void ShutdownMiniGame(bool sinkCentralHub)
    {
        isRunning = false;
        isGameOver = true;

        if (fixBugMiniGame3 != null)
            fixBugMiniGame3.StopAndClearPlayers();

        StopAllCoroutines();
        ClearAllLasers();
        ToggleFlamethrowers(false);

        if (sinkCentralHub && centralHub != null && centralHub.gameObject.activeInHierarchy)
            centralHub.EndMinigameAndSink();
    }

    // --- HỆ THỐNG NÃO CHỈNH NHỊP ĐỘ (ĐÃ LINK VỚI INSPECTOR) ---
    private void UpdateDifficultyPhase()
    {
        // Phase 1 (Bắt đầu từ thời điểm startTime của Phase 1)
        if (survivalTime >= phase1_Hub.startTime && survivalTime < phase2_Fire.startTime && currentPhase != 1)
        {
            currentPhase = 1;
            lasersPerWave = 0;
            ToggleFlamethrowers(false);
        }
        // Phase 2
        else if (survivalTime >= phase2_Fire.startTime && survivalTime < phase3_Spam.startTime && currentPhase != 2)
        {
            currentPhase = 2;

            if (centralHub != null && centralHub.gameObject.activeInHierarchy)
                centralHub.EndMinigameAndSink();

            ToggleFlamethrowers(true);
            lasersPerWave = 0;
        }
        // Phase 3 
        else if (survivalTime >= phase3_Spam.startTime && survivalTime < phase4_Hard.startTime && currentPhase != 3)
        {
            currentPhase = 3;
            ToggleFlamethrowers(false);

            currentMode = SpawnMode.Alternating;

            // Gán thông số từ Inspector
            currentLaserSpeed = phase3_Spam.laserSpeed;
            lasersPerWave = phase3_Spam.lasersPerWave;
            timeBetweenWaves = phase3_Spam.waveDelay;
            delayBetweenLasers = phase3_Spam.delayBetweenLasers;

            waveTimer = timeBetweenWaves;
        }
        // Phase 4
        else if (survivalTime >= phase4_Hard.startTime && currentPhase != 4)
        {
            currentPhase = 4;
            ToggleFlamethrowers(false);

            currentMode = SpawnMode.AutoMixed;

            // Gán thông số từ Inspector
            currentLaserSpeed = phase4_Hard.laserSpeed;
            lasersPerWave = phase4_Hard.lasersPerWave;
            timeBetweenWaves = phase4_Hard.waveDelay;
            delayBetweenLasers = phase4_Hard.delayBetweenLasers;

            waveTimer = timeBetweenWaves;
        }
    }

    private void ToggleFlamethrowers(bool state)
    {
        /*
         * Không SetActive(false) FireTrap_Left và FireTrap_Right.
         * Nếu tắt GameObject, coroutine hiệu ứng thiêu đốt
         * trên script của bẫy có thể bị dừng giữa chừng.
         */

        if (flamethrowerTraps != null)
        {
            foreach (GameObject trap in flamethrowerTraps)
            {
                if (trap == null)
                    continue;

                // Hai bẫy phải luôn active
                if (!trap.activeSelf)
                    trap.SetActive(true);
            }
        }

        // Chỉ bật/tắt bộ não điều khiển bẫy
        if (flamethrowerBrain != null)
        {
            flamethrowerBrain.SetTrapRunning(state);
        }
        else
        {
            Debug.LogWarning(
                "[MiniGame3] Chưa gán Master_Flamethrower vào flamethrowerBrain!"
            );
        }

        // Khi tắt, ép từng core reset trực tiếp để vẫn an toàn nếu reference
        // flamethrowerBrain bị thiếu hoặc cấu hình sai trong Inspector.
        if (!state && flamethrowerTraps != null)
        {
            foreach (GameObject trap in flamethrowerTraps)
            {
                if (trap == null)
                    continue;

                WallFlamethrowerCore[] cores =
                    trap.GetComponentsInChildren<WallFlamethrowerCore>(true);

                foreach (WallFlamethrowerCore core in cores)
                {
                    if (core != null)
                        core.ForceStopImmediately();
                }
            }
        }
    }

    private IEnumerator SpawnSpamWave()
    {
        isSpawningWave = true;

        if (spawnPoints.Count == 0 || spamLaserPrefab == null || lasersPerWave <= 0)
        {
            isSpawningWave = false;
            yield break;
        }

        SpawnMode waveMode = currentMode == SpawnMode.AutoMixed ? (SpawnMode)Random.Range(0, 5) : currentMode;
        spawnCounter = 0;

        for (int i = 0; i < lasersPerWave; i++)
        {
            if (isGameOver)
            {
                isSpawningWave = false;
                yield break;
            }

            if (waveMode == SpawnMode.BothAtSameTime)
            {
                foreach (Transform sp in spawnPoints) SpawnSingleLaser(sp);
            }
            else
            {
                Transform selectedPoint = GetSpawnPoint(waveMode);
                if (selectedPoint != null) SpawnSingleLaser(selectedPoint);
            }

            float actualDelay = waveMode == SpawnMode.Burst ? delayBetweenLasers * 0.5f : delayBetweenLasers;
            yield return new WaitForSeconds(actualDelay);
        }

        if (waveMode == SpawnMode.Burst)
            lastSpawnIndex = (lastSpawnIndex + 1) % spawnPoints.Count;

        isSpawningWave = false;
    }

    private Transform GetSpawnPoint(SpawnMode mode)
    {
        switch (mode)
        {
            case SpawnMode.Random: return spawnPoints[Random.Range(0, spawnPoints.Count)];
            case SpawnMode.Alternating:
            case SpawnMode.Burst:
                Transform pt = spawnPoints[lastSpawnIndex];
                if (mode == SpawnMode.Alternating) lastSpawnIndex = (lastSpawnIndex + 1) % spawnPoints.Count;
                return pt;
            case SpawnMode.DoubleAlternating:
                Transform dPt = spawnPoints[lastSpawnIndex];
                spawnCounter++;
                if (spawnCounter >= 2)
                {
                    lastSpawnIndex = (lastSpawnIndex + 1) % spawnPoints.Count;
                    spawnCounter = 0;
                }
                return dPt;
            default: return spawnPoints[0];
        }
    }

    private void SpawnSingleLaser(Transform sp)
    {
        Quaternion finalRotation = sp.rotation;

        if (Camera.main != null)
        {
            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            if (sp.name.Contains("Left")) finalRotation = Quaternion.LookRotation(camRight);
            else if (sp.name.Contains("Right")) finalRotation = Quaternion.LookRotation(-camRight);
        }

        GameObject newLaser = Instantiate(spamLaserPrefab, sp.position, finalRotation);

        if (newLaser.TryGetComponent<LaserSpamObject>(out var laserScript))
            laserScript.speed = currentLaserSpeed;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.laserMoveClip);
    }

    private void ClearAllLasers()
    {
        LaserSpamObject[] remainingLasers = FindObjectsByType<LaserSpamObject>(FindObjectsSortMode.None);
        foreach (LaserSpamObject laser in remainingLasers) Destroy(laser.gameObject);
    }

    public void SetUpAllPlayer()
    {
        if (manager == null || manager.currentPlayer1 == null || manager.currentPlayer2 == null)
        {
            //Debug.LogWarning("[MiniGame3] SetUpAllPlayer: manager or player references are missing.");
            return;
        }

        PlayerManager p1 = manager.currentPlayer1.GetComponent<PlayerManager>();
        PlayerManager p2 = manager.currentPlayer2.GetComponent<PlayerManager>();

        if (p1 == null || p2 == null)
        {
            //Debug.LogWarning("[MiniGame3] SetUpAllPlayer: PlayerManager component missing on a player.");
            return;
        }

        p1.playerMove.hasLie = true;
        p2.playerMove.hasLie = true;

        p1.playerAttack.hasAttack = false;
        p2.playerAttack.hasAttack = false;
    }
}