using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGame2 : MonoBehaviour
{
    [Header("Manager")]
    public MiniGameManager manager;
    [Header("Camera Shake")]
    public CameraShake cameraShake;
    public float shakeDuration = 1.2f;
    public float shakeStrength = 0.25f;
    [Header("Danh sách ô màu")]
    public List<ColorPad> allPads = new List<ColorPad>();

    [Header("UI Giao diện")]
    public GameObject canvasMiniGame;
    public Image targetColorImage;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;
    public int[] hhh;

    [Header("Cấu hình Nhấp Nháy Cảnh Báo (Blink Warning)")]
    [Tooltip("Thời gian nhấp nháy báo động trước khi ô sập xuống")]
    public float warningDuration = 1.2f;
    [Tooltip("Tốc độ chớp tắt của hiệu ứng cảnh báo")]
    public float blinkInterval = 0.1f;

    [Header("Start Delay")]
    public float startDelay = 3f;

    bool isRunning = false;

    private List<Color> easyColors = new List<Color>();
    private List<Color> mediumColors = new List<Color>();
    private List<Color> hardColors = new List<Color>();

    private List<Color> activeColorPool = new List<Color>();

    private Color targetColor;
    private int currentRound = 1;
    private Color defaultTargetColor = Color.white;

    private void Awake()
    {
        if (targetColorImage != null)
            defaultTargetColor = targetColorImage.color;

        InitializeColorPools();
    }

    public void StartMiniGame()
    {
        if (isRunning)
            return;

        if (manager == null ||
            manager.currentPlayer1 == null ||
            manager.currentPlayer2 == null ||
            !HasValidPad())
        {
            return;
        }

        StopAllCoroutines();

        if (cameraShake != null)
            cameraShake.StopShake();

        SetUpAllPlayer();
        AudioManager.Instance.PlayEnvironment(AudioManager.Instance.javaLoopClip);
        if (canvasMiniGame != null)
        {
            canvasMiniGame.SetActive(true);
        }

        StartCoroutine(ColorGameLoop());
    }

    public void StopMiniGame()
    {
        isRunning = false;
        AudioManager.Instance.StopEnvironment();
        StopAllCoroutines();

        if (cameraShake != null)
            cameraShake.StopShake();

        if (canvasMiniGame != null)
        {
            canvasMiniGame.SetActive(false);
        }

        if (allPads != null)
        {
            foreach (ColorPad pad in allPads)
            {
                if (pad != null)
                    pad.ResetPad();
            }
        }

        if (timerText != null)
            timerText.text = "";

        if (roundText != null)
            roundText.text = "";

        if (targetColorImage != null)
            targetColorImage.color = defaultTargetColor;

        currentRound = 1;
    }

    void InitializeColorPools()
    {
        easyColors.Clear();
        mediumColors.Clear();
        hardColors.Clear();

        easyColors.Add(Color.red);
        easyColors.Add(Color.blue);
        easyColors.Add(Color.yellow);
        easyColors.Add(Color.green);

        mediumColors.AddRange(easyColors);
        mediumColors.Add(new Color(1f, 0.5f, 0f));     // Cam
        mediumColors.Add(new Color(0.5f, 0f, 0.5f));   // Tím
        mediumColors.Add(new Color(0f, 0.5f, 0f));     // Xanh lá đậm

        hardColors.AddRange(mediumColors);
        hardColors.Add(new Color(0.75f, 1f, 0f));      // Lime
        hardColors.Add(new Color(0f, 1f, 0.5f));       // Mint
        hardColors.Add(new Color(1f, 0.3f, 0.5f));     // Flamingo
        hardColors.Add(new Color(0.5f, 0.25f, 0f));    // Nâu
    }

    IEnumerator ColorGameLoop()
    {
        isRunning = true;
        currentRound = 1;

        yield return new WaitForSeconds(startDelay);

        while (isRunning)
        {
            if (roundText != null)
            {
                roundText.text = "Round:" + currentRound;
            }

            int safePadsCount = 8;
            float maxTimeForChoice = 5f;

            if (currentRound >= 1 && currentRound <= 2)
            {
                activeColorPool = easyColors;
                safePadsCount = Random.Range(8, 12);
                maxTimeForChoice = 5f;
            }
            else if (currentRound >= 3 && currentRound <= 4)
            {
                activeColorPool = mediumColors;
                safePadsCount = Random.Range(4, 7);
                maxTimeForChoice = 4f;
            }
            else
            {
                activeColorPool = hardColors;
                safePadsCount = Random.Range(1, 3);
                maxTimeForChoice = 3f;
            }

            targetColor = activeColorPool[Random.Range(0, activeColorPool.Count)];

            if (targetColorImage != null)
            {
                targetColorImage.color = targetColor;
            }

            List<ColorPad> shuffledPads = new List<ColorPad>();

            foreach (ColorPad pad in allPads)
            {
                if (pad != null)
                    shuffledPads.Add(pad);
            }

            if (shuffledPads.Count == 0)
            {
                isRunning = false;
                yield break;
            }

            for (int i = 0; i < shuffledPads.Count; i++)
            {
                ColorPad temp = shuffledPads[i];
                int randomIndex = Random.Range(i, shuffledPads.Count);
                shuffledPads[i] = shuffledPads[randomIndex];
                shuffledPads[randomIndex] = temp;
            }

            for (int i = 0; i < shuffledPads.Count; i++)
            {
                if (i < safePadsCount)
                {
                    shuffledPads[i].SetPadColor(targetColor);
                    shuffledPads[i].isSafe = true;
                }
                else
                {
                    Color randomColor;
                    do
                    {
                        randomColor = activeColorPool[Random.Range(0, activeColorPool.Count)];
                    }
                    while (randomColor == targetColor);

                    shuffledPads[i].SetPadColor(randomColor);
                    shuffledPads[i].isSafe = false;
                }
            }

            float timeLeft = maxTimeForChoice;

            while (timeLeft > 0 && isRunning)
            {
                if (timerText != null)
                {
                    timerText.text = Mathf.CeilToInt(timeLeft).ToString();
                }

                yield return new WaitForSeconds(1f);
                timeLeft -= 1f;
            }

            if (!isRunning)
                yield break;

            if (timerText != null)
            {
                timerText.text = "??";
            }

            // ---- ĐÃ BỔ SUNG: XÁC ĐỊNH CÁC Ô SẼ SẬP (BAO GỒM CẢ Ô CÓ >= 2 PLAYER) ----
            foreach (ColorPad pad in allPads)
            {
                if (pad != null && pad.isSafe)
                {
                    int playerCountOnThisPad = CountPlayersOnPad(pad.gameObject);

                    if (playerCountOnThisPad >= 2)
                    {
                        pad.isSafe = false; // Đánh dấu là không an toàn nữa
                    }
                }
            }

            // ---- BỔ SUNG: BẬT HIỆU ỨNG NHẤP NHÁY CẢNH BÁO TRƯỚC KHI RƠI ----
            yield return StartCoroutine(BlinkUnsafePadsRoutine());

            // Âm thanh sập & Camera Shake
            AudioManager.Instance.PlaySFX(AudioManager.Instance.brickFallClip);
            if (cameraShake != null)
            {
                cameraShake.Shake(shakeDuration, shakeStrength);
            }

            yield return new WaitForSeconds(0.2f);

            // Thực hiện cho sập các ô đã được đánh dấu unsafe
            foreach (ColorPad pad in allPads)
            {
                if (pad != null)
                    pad.CheckSurvival();
            }

            // Chờ người chơi rơi
            yield return new WaitForSeconds(2f);

            // Hồi lại các ô
            foreach (ColorPad pad in allPads)
            {
                if (pad != null)
                    pad.ResetPad();
            }

            // Chờ animation hồi sàn hoàn tất
            yield return new WaitForSeconds(1f);

            // Respawn Player 1
            if (
                manager.currentPlayer1 != null &&
                manager.currentPlayer1.transform.position.y <= -10f
            )
            {
                PlayerVFX vfx = manager.currentPlayer1.GetComponent<PlayerVFX>();
                PlayerMiniGame player1 = manager.currentPlayer1.GetComponent<PlayerMiniGame>();
                if (vfx != null)
                {
                    StartCoroutine(vfx.DissolveInNoParticleRoutine(0.5f));
                }
                if (player1 != null)
                {
                    player1.Respawn();
                }
            }

            // Respawn Player 2
            if (
                manager.currentPlayer2 != null &&
                manager.currentPlayer2.transform.position.y <= -10f
            )
            {
                PlayerMiniGame player2 = manager.currentPlayer2.GetComponent<PlayerMiniGame>();
                PlayerVFX vfx = manager.currentPlayer2.GetComponent<PlayerVFX>();
                if (vfx != null)
                {
                    StartCoroutine(vfx.DissolveInNoParticleRoutine(0.5f));
                }
                if (player2 != null)
                {
                    player2.Respawn();
                }
            }

            // Chờ người chơi ổn định trên sàn
            yield return new WaitForSeconds(1f);

            // Sang vòng tiếp theo
            currentRound++;
        }
    }

    // ---- MỚI: COROUTINE XỬ LÝ NHẤP NHÁY CÁC Ô KHÔNG AN TOÀN ----
    IEnumerator BlinkUnsafePadsRoutine()
    {
        float elapsed = 0f;
        bool toggle = false;
        float safeBlinkInterval = Mathf.Max(0.02f, blinkInterval);

        // Lưu lại màu gốc của các ô không an toàn để chớp tắt linh hoạt
        Dictionary<ColorPad, Color> originalColors = new Dictionary<ColorPad, Color>();
        foreach (ColorPad pad in allPads)
        {
            if (pad != null && !pad.isSafe)
            {
                Renderer r = pad.GetComponent<Renderer>();
                if (r != null)
                {
                    originalColors[pad] = r.material.color;
                }
            }
        }

        while (elapsed < warningDuration)
        {
            toggle = !toggle;

            foreach (var kvp in originalColors)
            {
                ColorPad pad = kvp.Key;
                Color originalColor = kvp.Value;

                if (pad != null)
                {
                    // Chớp tắt giữa màu gốc và màu Cảnh báo (Màu Đỏ nhạt / Tối màu)
                    Color warningColor = toggle ? Color.red : (originalColor * 0.3f);
                    pad.SetPadColor(warningColor);
                }
            }

            yield return new WaitForSeconds(safeBlinkInterval);
            elapsed += safeBlinkInterval;
        }

        // Trả lại màu gốc một khoảnh khắc trước khi chính thức sập
        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.SetPadColor(kvp.Value);
            }
        }
    }

    private int CountPlayersOnPad(GameObject padObj)
    {
        if (padObj == null)
            return 0;

        Vector3 center = padObj.transform.position + Vector3.up * 1f;
        Vector3 halfExtents = new Vector3(0.49f, 1f, 0.49f);

        Collider[] hitColliders = Physics.OverlapBox(
            center,
            halfExtents,
            padObj.transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        HashSet<PlayerManager> players = new HashSet<PlayerManager>();

        foreach (Collider col in hitColliders)
        {
            if (col == null)
                continue;

            PlayerManager player = col.GetComponentInParent<PlayerManager>();

            if (player != null)
            {
                players.Add(player);
            }
        }

        return players.Count;
    }

    public void SetUpAllPlayer()
    {
        if (manager == null)
            return;

        SetUpPlayerAttack(manager.currentPlayer1);
        SetUpPlayerAttack(manager.currentPlayer2);
    }

    private void SetUpPlayerAttack(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerManager player = playerObject.GetComponent<PlayerManager>();

        if (player == null || player.playerAttack == null)
            return;

        player.playerAttack.hasAttack = true;
    }

    private bool HasValidPad()
    {
        if (allPads == null)
            return false;

        foreach (ColorPad pad in allPads)
        {
            if (pad != null)
                return true;
        }

        return false;
    }
}