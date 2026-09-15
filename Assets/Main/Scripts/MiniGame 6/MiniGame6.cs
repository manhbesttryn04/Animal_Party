using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MiniGame6 : MonoBehaviour
{
    public static MiniGame6 Instance;

    [Header("--- MANAGER ---")]
    public MiniGameManager manager;

    [Header("--- TEST (không cần MiniGameManager) ---")]
    public GameObject testPlayer1;
    public GameObject testPlayer2;
    public bool autoStartOnPlay = true;

    [Header("--- CẤU HÌNH VÒNG CHƠI ---")]
    public float roundDuration = 15f;
    public float passCooldown = 0.5f;
    public float firstWaitTime = 3f;

    [Header("--- PLAYER SETTINGS ---")]
    public float playerMoveSpeed = 5f;
    [Tooltip("Tốc độ tối đa khi gần hết giờ")]
    public float maxMoveSpeed = 9f;
    [Tooltip("Bắt đầu tăng tốc khi còn bao nhiêu giây")]
    public float speedBoostTime = 5f;

    [Header("--- SCREEN SHAKE ---")]
    public Camera mainCamera;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.3f;

    [Header("--- BOMB PREFAB ---")]
    public GameObject bombPrefab;
    public float bombFlyDuration = 0.25f;

    [Header("--- TRAP MANAGER ---")]
    public TrapManager trapManager;

    [Header("--- UI ---")]
    public GameObject miniGameCanvas;
    public TMP_Text timerText;
    public TMP_Text messageText;
    public TMP_Text countdownText;
    public TMP_Text p1NameText;
    public TMP_Text p2NameText;
    public TMP_Text resultText;
    public GameObject resultPanel;

    [Header("--- VFX ---")]
    public GameObject explosionVFX;

    // Internal
    private BombCarrier carrier1;
    private BombCarrier carrier2;
    private List<BombCarrier> activePlayers = new List<BombCarrier>();

    private BombCarrier currentBombHolder;
    private GameObject bombInstance;
    private Renderer bombRenderer;
    private Coroutine flyCoroutine;
    private float blinkTimer;

    private float timeLeft;
    private bool roundActive = false;
    private bool isRunning = false;
    private bool hasPlayedTick = false;

    private void Awake() { Instance = this; }

    // ====== BẮT ĐẦU ======
    public void StartMiniGame()
    {
        if (isRunning) return;
        if (miniGameCanvas != null)
        {
            miniGameCanvas.SetActive(true);
        }

        // Lấy player
        GameObject p1obj = manager != null ? manager.currentPlayer1 : testPlayer1;
        GameObject p2obj = manager != null ? manager.currentPlayer2 : testPlayer2;

        carrier1 = p1obj?.GetComponent<BombCarrier>();
        carrier2 = p2obj?.GetComponent<BombCarrier>();

        if (carrier1 == null || carrier2 == null)
        {
            //Debug.LogWarning("[BOMB] Missing BombCarrier on player!");
            return;
        }

        // Reset toàn bộ trạng thái (bao gồm isEliminated) TRƯỚC khi kích hoạt game.
        // Quan trọng khi StartMiniGame được gọi lại nhiều lần (replay) — nếu không reset,
        // player bị loại ở trận trước sẽ vẫn còn cờ isEliminated = true.
        carrier1.ResetForNewGame();
        carrier2.ResetForNewGame();

        // Kích hoạt BombCarrier
        carrier1.SetGameActive(true);
        carrier2.SetGameActive(true);

        // KHÓA DI CHUYỂN NGAY KHI VÀO GAME
        SetPlayerMovement(false);

        activePlayers.Clear();
        activePlayers.Add(carrier1);
        activePlayers.Add(carrier2);

        // Setup movement
        SetUpPlayer(p1obj);
        SetUpPlayer(p2obj);

        // Spawn bomb
        if (bombPrefab != null)
        {
            bombInstance = Instantiate(bombPrefab);
            bombInstance.SetActive(false);
            bombRenderer = bombInstance.GetComponentInChildren<Renderer>();
        }

        // Reset UI
        if (messageText) messageText.text = "";
        if (timerText) timerText.text = "";
        if (countdownText) countdownText.text = "";
        if (resultPanel) resultPanel.SetActive(false);
        if (p1NameText) p1NameText.text = "PLAYER 1";
        if (p2NameText) p2NameText.text = "PLAYER 2";

        isRunning = true;

        StartCoroutine(GameRoutine());
    }

    // ====== DỪNG ======
    public void StopMiniGame()
    {
        isRunning = false;
        roundActive = false;

        StopAllCoroutines();

        // Khoá di chuyển & Tắt BombCarrier
        SetPlayerMovement(false);
        carrier1?.SetGameActive(false);
        carrier2?.SetGameActive(false);

        // Tắt hết bẫy
        trapManager?.DeactivateTraps();

        if (flyCoroutine != null) StopCoroutine(flyCoroutine);
        if (bombInstance != null) Destroy(bombInstance);

        activePlayers.Clear();
        if (timerText != null) timerText.text = "";
        if (messageText != null) messageText.text = "";
        if (countdownText != null) countdownText.text = "";

        if (miniGameCanvas != null)
        {
            miniGameCanvas.SetActive(false);
        }
    }

    // ====== GAME ROUTINE ======
    IEnumerator GameRoutine()
    {
        // 1. Khóa chuyển động trước khi đếm
        SetPlayerMovement(false);

        // 2. Đếm ngược 3, 2, 1
        for (int i = (int)firstWaitTime; i > 0; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText) countdownText.text = "GO!";

        // 3. HẾT ĐẾM NGƯỢC -> MỞ KHÓA DI CHUYỂN & BẬT BẪY!
        SetPlayerMovement(true);
        trapManager?.ActivateTraps();

        yield return new WaitForSeconds(0.5f);
        if (countdownText) countdownText.text = "";

        StartNewRound();
    }

    // ====== BẬT / TẮT CHUYỂN ĐỘNG CHO PLAYER ======
    private void SetPlayerMovement(bool canMove)
    {
        carrier1?.SetCanMove(canMove);
        carrier2?.SetCanMove(canMove);
    }

    void StartNewRound()
    {
        if (!isRunning) return;

        if (activePlayers.Count <= 1)
        {
            EndGame();
            return;
        }

        // Random người cầm bom
        int index = Random.Range(0, activePlayers.Count);
        AssignBomb(activePlayers[index]);

        timeLeft = roundDuration;
        blinkTimer = 0f;
        hasPlayedTick = false;
        roundActive = true;

        if (messageText) messageText.text = "";
    }

    private void Update()
    {
        if (!roundActive || !isRunning) return;

        timeLeft -= Time.deltaTime;

        if (timerText) timerText.text = Mathf.CeilToInt(timeLeft).ToString();

        UpdateBombBlink(timeLeft / roundDuration);

        // Tiếng tích tắc khi còn 5 giây
        if (timeLeft <= 5f && !hasPlayedTick)
        {
            hasPlayedTick = true;
            AudioManager.Instance?.PlaySpecial(AudioManager.Instance.bombTickingClip);
        }

        if (timeLeft <= 0f)
        {
            roundActive = false;
            StartCoroutine(ExplodeBomb());
        }

        // Tăng tốc độ player cầm bom khi gần hết giờ
        if (timeLeft <= speedBoostTime)
        {
            float t = 1f - (timeLeft / speedBoostTime); // 0 → 1
            float boostedSpeed = Mathf.Lerp(playerMoveSpeed, maxMoveSpeed, t);
            SetBombHolderSpeed(boostedSpeed);
        }
    }

    void UpdateBombBlink(float progressLeft)
    {
        if (bombRenderer == null) return;

        float blinkSpeed = Mathf.Lerp(10f, 1f, progressLeft);
        blinkTimer += Time.deltaTime * blinkSpeed;
        float alpha = (Mathf.Sin(blinkTimer * 10f) + 1f) / 2f;

        Color c = bombRenderer.material.color;
        c.a = Mathf.Lerp(0.3f, 1f, alpha);
        bombRenderer.material.color = c;
    }

    // ====== TRUYỀN BOM ======
    public void TransferBomb(BombCarrier from, BombCarrier to)
    {
        if (!roundActive || !isRunning) return;
        if (from != currentBombHolder) return;
        if (to.IsOnCooldown()) return;

        AssignBomb(to);
    }

    // ====== TRUYỀN BOM CƯỠNG ÉP ======
    public void ForceTransferBomb(BombCarrier from)
    {
        if (!roundActive || !isRunning) return;
        if (from != currentBombHolder) return;

        BombCarrier target = (from == carrier1) ? carrier2 : carrier1;
        if (target == null || target.IsEliminated()) return;

        AssignBomb(target);
    }

    void AssignBomb(BombCarrier holder)
    {
        currentBombHolder?.SetHoldingBomb(false);

        currentBombHolder = holder;
        currentBombHolder.SetHoldingBomb(true);
        currentBombHolder.StartCooldown(passCooldown);

        if (bombInstance != null && holder.bombAnchor != null)
        {
            bombInstance.SetActive(true);
            if (flyCoroutine != null) StopCoroutine(flyCoroutine);
            flyCoroutine = StartCoroutine(FlyBombTo(holder.bombAnchor));
        }

        if (roundActive && timeLeft <= speedBoostTime)
        {
            float t = 1f - (timeLeft / speedBoostTime);
            float boostedSpeed = Mathf.Lerp(playerMoveSpeed, maxMoveSpeed, t);
            SetBombHolderSpeed(boostedSpeed);
        }
        else
        {
            SetPlayerSpeed(playerMoveSpeed);
        }
    }

    IEnumerator FlyBombTo(Transform targetAnchor)
    {
        bombInstance.transform.SetParent(null);

        Vector3 startPos = bombInstance.transform.position;
        Quaternion startRot = bombInstance.transform.rotation;
        float t = 0f;

        while (t < bombFlyDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / bombFlyDuration);

            Vector3 pos = Vector3.Lerp(startPos, targetAnchor.position, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * 0.8f;

            bombInstance.transform.position = pos;
            bombInstance.transform.rotation = Quaternion.Slerp(startRot, targetAnchor.rotation, p);

            yield return null;
        }

        bombInstance.transform.SetParent(targetAnchor);
        bombInstance.transform.localPosition = Vector3.zero;
        bombInstance.transform.localRotation = Quaternion.identity;
    }

    // ====== BOM NỔ ======
    IEnumerator ExplodeBomb()
    {
        if (flyCoroutine != null) StopCoroutine(flyCoroutine);

        AudioManager.Instance?.StopMusic();

        SetPlayerSpeed(playerMoveSpeed);

        if (explosionVFX != null && currentBombHolder != null)
            Instantiate(explosionVFX, currentBombHolder.transform.position, Quaternion.identity);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.explosionBombClip);

        if (bombInstance != null) bombInstance.SetActive(false);

        if (mainCamera != null)
            StartCoroutine(ShakeCamera());

        if (currentBombHolder != null)
            StartCoroutine(LaunchPlayer(currentBombHolder.gameObject));

        string loserName = currentBombHolder == carrier1 ? "PLAYER 1" : "PLAYER 2";
        if (messageText) messageText.text = $"{loserName} has been eliminated!";

        PlayDeadAnimation(currentBombHolder.gameObject);

        EliminatePlayer(currentBombHolder);

        yield return new WaitForSeconds(2f);

        StartNewRound();
    }

    void PlayDeadAnimation(GameObject playerObj)
    {
        if (playerObj == null) return;

        PlayerAnimator anim = playerObj.GetComponent<PlayerAnimator>();
        if (anim != null && anim.playerAnimator != null)
            anim.playerAnimator.SetBool("Die", true);
    }

    void EliminatePlayer(BombCarrier carrier)
    {
        activePlayers.Remove(carrier);
        carrier.SetEliminated(true);
        // Không còn bị mất cờ isEliminated nữa vì SetGameActive() đã không reset nó.
        carrier.SetGameActive(false);
    }

    // ====== KẾT THÚC ======
    void EndGame()
    {
        roundActive = false;
        isRunning = false;

        AudioManager.Instance?.StopMusic();

        trapManager?.DeactivateTraps();

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (activePlayers.Count != 1)
        {
           // Debug.LogError("[MiniGame6] Không xác định được người thắng!");
            return;
        }

        bool isPlayer1Win = activePlayers[0] == carrier1;

        string winnerName = isPlayer1Win ? "PLAYER 1" : "PLAYER 2";
        string color = isPlayer1Win ? "red" : "yellow";

        if (resultText != null)
            resultText.text = $"<color={color}>{winnerName} WINS!</color>";

        if (messageText != null)
            messageText.text = $"{winnerName} WINS!";

        if (AudioManager.Instance != null)
        {

            if (isPlayer1Win)
            {
                AudioManager.Instance.PlaySpecial(AudioManager.Instance.playerOneWinClip);
            }
            else
            {
                AudioManager.Instance.PlaySpecial(AudioManager.Instance.playerTwoWinClip);
            }
        }

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

    [Header("--- LAUNCH ON EXPLODE ---")]
    public float launchForce = 8f;
    public float launchUpForce = 30f;
    public float launchHideTime = 2f;

    IEnumerator LaunchPlayer(GameObject playerObj)
    {
        if (playerObj == null) yield break;

        PlayerMove move = playerObj.GetComponent<PlayerMove>();
        PlayerAnimator pani = playerObj.GetComponent<PlayerAnimator>();
        if (move != null && pani != null)
        {
            move.isJumpAndMove = false;
            pani.playerAnimator.SetFloat("Run", 0f);
        }

        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        CharacterController cc = playerObj.GetComponent<CharacterController>();

        Vector3 randomDir = new Vector3(
            Random.Range(-0.3f, 0.3f),
            1f,
            Random.Range(-0.3f, 0.3f)
        ).normalized;

        Vector3 launchVelocity = randomDir * launchForce + Vector3.up * launchUpForce;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(launchVelocity, ForceMode.Impulse);
        }
        else if (cc != null)
        {
            StartCoroutine(LaunchCharacterController(playerObj, cc, launchVelocity));
            yield break;
        }

        yield return new WaitForSeconds(launchHideTime);
        playerObj.SetActive(false);
    }

    IEnumerator LaunchCharacterController(GameObject playerObj, CharacterController cc, Vector3 velocity)
    {
        float timer = 0f;
        float gravity = -18f;

        while (timer < launchHideTime)
        {
            velocity.y += gravity * Time.deltaTime;
            cc.Move(velocity * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        playerObj.SetActive(false);
    }

    // ====== SCREEN SHAKE ======
    IEnumerator ShakeCamera()
    {
        if (mainCamera == null) yield break;

        Vector3 originalPos = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            mainCamera.transform.position = new Vector3(
                originalPos.x + x,
                originalPos.y + y,
                originalPos.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalPos;
    }

    void SetPlayerSpeed(float speed)
    {
        SetSpeedForPlayer(carrier1?.gameObject, speed);
        SetSpeedForPlayer(carrier2?.gameObject, speed);
    }

    void SetBombHolderSpeed(float boostedSpeed)
    {
        if (carrier1 != null)
            SetSpeedForPlayer(carrier1.gameObject, carrier1 == currentBombHolder ? boostedSpeed : playerMoveSpeed);

        if (carrier2 != null)
            SetSpeedForPlayer(carrier2.gameObject, carrier2 == currentBombHolder ? boostedSpeed : playerMoveSpeed);
    }

    void SetSpeedForPlayer(GameObject playerObj, float speed)
    {
        if (playerObj == null) return;

        BombCarrier carrier = playerObj.GetComponent<BombCarrier>();
        PlayerMove move = playerObj.GetComponent<PlayerMove>();

        // FIX: chỉ check trạng thái của CHÍNH player này, không check chéo carrier1/carrier2.
        // Trước đây nếu bất kỳ player nào bị loại thì cả 2 đều không được set speed —
        // vô hại với game 2 người (vì EndGame() sẽ chạy ngay sau), nhưng sai về logic
        // và sẽ lộ rõ nếu mở rộng sang >2 người chơi.
        if (move != null && carrier != null && !carrier.IsFrozen() && !carrier.IsEliminated())
            move.speed = speed;
    }

    void SetUpPlayer(GameObject playerObj)
    {
        if (playerObj == null) return;

        PlayerMove move = playerObj.GetComponent<PlayerMove>();
        PlayerAnimator anim = playerObj.GetComponent<PlayerAnimator>();

        if (move != null)
        {
            move.speed = playerMoveSpeed;
            move.isJumpAndMove = true;
            move.gravity = -18f;
        }

        if (anim != null && anim.playerAnimator != null)
            anim.playerAnimator.SetBool("Die", false);
    }

    public BombCarrier GetOtherPlayer(BombCarrier currentPlayer)
    {
        if (carrier1 == currentPlayer) return carrier2;
        if (carrier2 == currentPlayer) return carrier1;
        return null;
    }
}