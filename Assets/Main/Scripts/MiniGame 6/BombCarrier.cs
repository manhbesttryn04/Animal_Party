using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerType))]
public class BombCarrier : MonoBehaviour
{
    [Header("Tham chiếu")]
    public Transform bombAnchor;

    // FIX: cooldown âm thanh bẫy dùng CHUNG cho toàn bộ game (static), không phải riêng
    // từng bẫy. Vì mỗi bẫy có cooldown riêng, khi player đi qua vùng nhiều bẫy đặt gần nhau,
    // mỗi bẫy khác nhau vẫn tự phát âm thanh của nó -> âm thanh dồn chồng liên tục không dứt.
    [Header("--- TRAP SFX THROTTLE (Global) ---")]
    [Tooltip("Khoảng thời gian tối thiểu giữa 2 lần phát âm thanh bẫy, tính chung cho MỌI bẫy/MỌI player.")]
    public static float trapSfxMinInterval = 0.35f;
    private static float lastTrapSfxTime = -999f;

    private static bool CanPlayTrapSfx()
    {
        float elapsed = Time.time - lastTrapSfxTime;

        // FIX: nếu Unity Editor bật "Enter Play Mode Options -> tắt Reload Domain",
        // biến static lastTrapSfxTime KHÔNG reset giữa các lần bấm Play, trong khi
        // Time.time LUÔN reset về 0 mỗi lần vào Play Mode mới. Hậu quả: elapsed bị âm
        // rất lớn (vd Time.time=5, lastTrapSfxTime=500 từ session cũ) -> luôn nhỏ hơn
        // trapSfxMinInterval -> CanPlayTrapSfx() trả về false VĨNH VIỄN -> không bao giờ
        // phát âm thanh bẫy nữa cho tới khi Time.time > lastTrapSfxTime (có thể rất lâu).
        // -> Coi elapsed âm là "đã hết hạn từ lâu", cho phép phát ngay.
        if (elapsed < 0f || elapsed >= trapSfxMinInterval)
        {
            lastTrapSfxTime = Time.time;
            return true;
        }

        return false;
    }

    private PlayerType playerType;
    private PlayerMove playerMove;

    private bool isGameActive = false;
    private bool isHoldingBomb = false;
    private bool isEliminated = false;
    private bool canMove = false;
    private float cooldownTimer = 0f;

    // --- FREEZE ---
    private bool isFrozen = false;
    private float defaultSpeed = 0f;
    private Coroutine freezeCoroutine;
    private GameObject activeFreezeLoopVFX; // FIX: theo dõi VFX loop đang chạy để destroy thủ công khi bị dính bẫy lại

    // --- MAGNET ---
    private Coroutine magnetCoroutine;
    private GameObject activeMagnetVFX; // FIX: theo dõi VFX magnet đang chạy để destroy thủ công khi bị dính bẫy lại
    private bool isBeingPulled = false; // FIX: cờ trạng thái để chặn phát lại âm thanh/VFX khi đang bị hút

    void Awake()
    {
        playerType = GetComponent<PlayerType>();
        playerMove = GetComponent<PlayerMove>();

        if (playerMove != null)
        {
            defaultSpeed = playerMove.speed;
        }
    }

    void Update()
    {
        if (!isGameActive) return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        UpdatePlayerMovementState();
    }

    private void UpdatePlayerMovementState()
    {
        if (playerMove == null) return;

        if (isEliminated || !canMove || isFrozen)
        {
            playerMove.isMove = false;
            playerMove.isJump = false;
        }
        else
        {
            playerMove.isMove = true;
            playerMove.isJump = true;
        }
    }

    public void SetCanMove(bool enable)
    {
        canMove = enable;
        UpdatePlayerMovementState();
    }

    public bool CanMove() => canMove;

    /// <summary>
    /// Bật/tắt trạng thái active của carrier trong game.
    /// KHÔNG reset isEliminated ở đây — dùng ResetForNewGame() khi bắt đầu 1 game/round mới
    /// để tránh việc gọi SetGameActive(false) sau khi loại player làm mất cờ isEliminated.
    /// </summary>
    public void SetGameActive(bool active)
    {
        isGameActive = active;
        isHoldingBomb = false;
        canMove = false;

        if (isFrozen)
        {
            if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
            if (activeFreezeLoopVFX != null) { Destroy(activeFreezeLoopVFX); activeFreezeLoopVFX = null; }
            SetFrozen(false);
        }

        if (magnetCoroutine != null) StopCoroutine(magnetCoroutine);
        if (activeMagnetVFX != null) { Destroy(activeMagnetVFX); activeMagnetVFX = null; }
        isBeingPulled = false;

        cooldownTimer = 0f;
    }

    /// <summary>
    /// Gọi khi bắt đầu 1 trận mới (trước SetGameActive(true)) để reset toàn bộ trạng thái,
    /// bao gồm cả isEliminated.
    /// </summary>
    public void ResetForNewGame()
    {
        isEliminated = false;
        isHoldingBomb = false;
        canMove = false;
        cooldownTimer = 0f;
    }

    public bool IsGameActive() => isGameActive;

    void OnTriggerEnter(Collider other)
    {
        if (!isGameActive) return;
        if (isEliminated || !isHoldingBomb) return;
        if (isFrozen) return;
        if (IsOnCooldown()) return;

        if (!other.CompareTag("Player 1") && !other.CompareTag("Player 2")) return;

        BombCarrier otherCarrier = other.GetComponent<BombCarrier>();
        if (otherCarrier == null || otherCarrier == this) return;
        if (!otherCarrier.IsGameActive() || otherCarrier.IsEliminated()) return;

        MiniGame6.Instance?.TransferBomb(this, otherCarrier);
    }

    public void SetHoldingBomb(bool holding)
    {
        isHoldingBomb = holding;
    }

    public bool IsHoldingBomb() => isHoldingBomb;

    public void StartCooldown(float duration)
    {
        cooldownTimer = duration;
    }

    public bool IsOnCooldown() => cooldownTimer > 0f;

    public void SetEliminated(bool eliminated)
    {
        isEliminated = eliminated;
        UpdatePlayerMovementState();

        if (eliminated && isFrozen)
        {
            if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
            if (activeFreezeLoopVFX != null) { Destroy(activeFreezeLoopVFX); activeFreezeLoopVFX = null; }
            SetFrozen(false);
        }
    }

    public bool IsEliminated() => isEliminated;

    // ====== FREEZE TRAP SYSTEM ======
    public void ApplyFreeze(float duration, GameObject hitVfxPrefab, GameObject loopVfxPrefab, Vector3 vfxOffset, Vector3 vfxScale)
    {
        if (isEliminated) return;

        // FIX: chặn tại nguồn — nếu đang đóng băng rồi thì không phát lại âm thanh/VFX/coroutine.
        if (isFrozen) return;

        // FIX QUAN TRỌNG: set isFrozen = true NGAY LẬP TỨC ở đây (đồng bộ), TRƯỚC khi phát âm
        // thanh và TRƯỚC khi StartCoroutine. Trước đây isFrozen chỉ được set bên trong
        // FreezeRoutine (SetFrozen(true) là dòng đầu coroutine) — nếu có 2 bẫy Freeze đặt
        // chồng/gần nhau, cả 2 có thể trigger OnTriggerEnter trong CÙNG 1 frame (Unity xử lý
        // tuần tự nhưng không có gì ngăn code chạy tới đây trước khi frame trước set xong cờ),
        // khiến bẫy thứ 2 phát SFX trước khi guard "if (isFrozen) return;" kịp chặn.
        isFrozen = true;

        // Phát âm thanh bẫy băng qua AudioManager.PlaySFX().
        if (CanPlayTrapSfx() && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.freezeTrapClip != null ? AudioManager.Instance.freezeTrapClip : AudioManager.Instance.iceMagicClip;
            AudioManager.Instance.PlaySFX(clip);
        }

        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }

        // FIX: StopCoroutine chỉ ngắt coroutine ngay tại điểm yield, KHÔNG chạy phần code
        // dọn dẹp (Destroy loopVFX) nằm sau yield return trong FreezeRoutine cũ.
        // Nếu không destroy thủ công ở đây, VFX loop cũ sẽ bị bỏ quên và chồng lên VFX mới.
        if (activeFreezeLoopVFX != null)
        {
            Destroy(activeFreezeLoopVFX);
            activeFreezeLoopVFX = null;
        }

        if (vfxScale == Vector3.zero) vfxScale = Vector3.one;

        freezeCoroutine = StartCoroutine(FreezeRoutine(duration, hitVfxPrefab, loopVfxPrefab, vfxOffset, vfxScale));
    }

    private IEnumerator FreezeRoutine(float duration, GameObject hitVfxPrefab, GameObject loopVfxPrefab, Vector3 vfxOffset, Vector3 vfxScale)
    {
        // isFrozen đã được set = true đồng bộ trong ApplyFreeze() rồi.
        // Ở đây chỉ cần áp dụng side-effect (speed = 0, update movement state).
        ApplyFrozenSpeedEffect(true);

        if (hitVfxPrefab != null)
        {
            GameObject hitVFX = Instantiate(hitVfxPrefab, transform.position + vfxOffset, Quaternion.identity, transform);
            hitVFX.transform.localScale = vfxScale;
            Destroy(hitVFX, 2f);
        }

        GameObject loopVFX = null;
        if (loopVfxPrefab != null)
        {
            loopVFX = Instantiate(loopVfxPrefab, transform.position + vfxOffset, Quaternion.identity, transform);
            loopVFX.transform.localScale = vfxScale;
        }

        // FIX: lưu tham chiếu ra field để ApplyFreeze() có thể destroy thủ công
        // nếu coroutine này bị Stop giữa đường (không kịp chạy tới đoạn Destroy dưới đây).
        activeFreezeLoopVFX = loopVFX;

        yield return new WaitForSeconds(duration);

        if (loopVFX != null)
        {
            Destroy(loopVFX);
        }

        activeFreezeLoopVFX = null;
        SetFrozen(false);
        freezeCoroutine = null;
    }

    public void SetFrozen(bool frozen)
    {
        if (isEliminated) return;

        isFrozen = frozen;
        ApplyFrozenSpeedEffect(frozen);
    }

    /// <summary>
    /// Chỉ áp dụng hiệu ứng phụ (speed = 0 / speed = default + update movement state),
    /// KHÔNG đụng vào cờ isFrozen — vì ApplyFreeze() đã tự set isFrozen đồng bộ trước đó
    /// để chặn kịp thời các lần trigger chồng trong cùng frame.
    /// </summary>
    private void ApplyFrozenSpeedEffect(bool frozen)
    {
        if (playerMove == null) return;

        if (frozen)
        {
            playerMove.speed = 0f;
        }
        else
        {
            playerMove.speed = defaultSpeed;
        }

        UpdatePlayerMovementState();
    }

    public bool IsFrozen() => isFrozen;

    // ====== MAGNET TRAP SYSTEM ======
    public void ApplyPulledByPlayer(Transform pullerTransform, float force, float duration, GameObject vfxPrefab, Vector3 vfxOffset, Vector3 vfxScale)
    {
        if (isEliminated) return;

        // FIX: chặn tại nguồn — nếu đang bị hút rồi thì không phát lại âm thanh/VFX/coroutine.
        // Trước đây MagnetTrap không hề check trạng thái này, nên nếu bị 2 bẫy nam châm
        // khác nhau hút gần nhau về thời gian, âm thanh + VFX sẽ bị chồng lên nhau.
        if (isBeingPulled) return;

        // Phát âm thanh bẫy nam châm qua AudioManager.PlaySFX().
        if (CanPlayTrapSfx() && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.magnetTrapClip != null ? AudioManager.Instance.magnetTrapClip : AudioManager.Instance.laserMoveClip;
            AudioManager.Instance.PlaySFX(clip);
        }

        if (magnetCoroutine != null)
        {
            StopCoroutine(magnetCoroutine);
        }

        // FIX: tương tự Freeze — destroy thủ công VFX magnet cũ vì StopCoroutine
        // không chạy phần dọn dẹp nằm sau while loop trong PulledRoutine cũ.
        if (activeMagnetVFX != null)
        {
            Destroy(activeMagnetVFX);
            activeMagnetVFX = null;
        }

        if (vfxScale == Vector3.zero) vfxScale = Vector3.one;

        isBeingPulled = true;
        magnetCoroutine = StartCoroutine(PulledRoutine(pullerTransform, force, duration, vfxPrefab, vfxOffset, vfxScale));
    }

    private IEnumerator PulledRoutine(Transform pullerTransform, float force, float duration, GameObject vfxPrefab, Vector3 vfxOffset, Vector3 vfxScale)
    {
        GameObject spawnedVFX = null;
        if (vfxPrefab != null && pullerTransform != null)
        {
            spawnedVFX = Instantiate(vfxPrefab, pullerTransform.position + vfxOffset, Quaternion.identity, pullerTransform);
            spawnedVFX.transform.localScale = vfxScale;
        }

        // FIX: lưu tham chiếu ra field để ApplyPulledByPlayer() có thể destroy thủ công
        // nếu coroutine này bị Stop giữa đường.
        activeMagnetVFX = spawnedVFX;

        CharacterController cc = GetComponent<CharacterController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        float timer = 0f;

        while (timer < duration)
        {
            if (isEliminated || pullerTransform == null) break;

            Vector3 directionToPuller = (pullerTransform.position - transform.position);
            directionToPuller.y = 0;

            if (directionToPuller.magnitude > 1.0f)
            {
                Vector3 pullVelocity = directionToPuller.normalized * force;

                if (cc != null && cc.enabled)
                {
                    cc.Move(pullVelocity * Time.deltaTime);
                }
                else if (rb != null && !rb.isKinematic)
                {
                    rb.AddForce(pullVelocity, ForceMode.Acceleration);
                }
                else
                {
                    transform.position += pullVelocity * Time.deltaTime;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (spawnedVFX != null)
        {
            Destroy(spawnedVFX);
        }

        activeMagnetVFX = null;
        isBeingPulled = false;
        magnetCoroutine = null;
    }

}