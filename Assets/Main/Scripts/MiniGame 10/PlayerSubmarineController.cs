using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerSubmarineController : MonoBehaviour
{
    public enum PlayerID { Player1, Player2 }

    [Header("--- PLAYER ---")]
    public PlayerID playerId = PlayerID.Player1;

    [Header("--- SUBMARINE SETUP ---")]
    [Tooltip("Lực đẩy tiến/lùi")]
    public float thrustForce = 25f;
    [Tooltip("Tốc độ tối đa (m/s)")]
    public float maxSpeed = 12f;
    [Tooltip("Tốc độ lùi tối đa (m/s)")]
    public float maxReverseSpeed = 6f;
    [Tooltip("Tốc độ xoay trái/phải (độ/giây)")]
    public float turnSpeed = 60f;
    [Tooltip("Lực nổi lên/lặn xuống")]
    public float verticalThrustForce = 15f;
    [Tooltip("Tốc độ nổi/lặn tối đa (m/s)")]
    public float maxVerticalSpeed = 5f;
    [Tooltip("Lực cản nước - càng cao càng mau dừng khi buông ga")]
    public float waterDrag = 1.5f;
    [Tooltip("Giới hạn độ sâu tối đa được lặn xuống (world Y)")]
    public float minDepthY = -20f;
    [Tooltip("Giới hạn độ cao tối đa được nổi lên (world Y)")]
    public float maxDepthY = 0f;

    [Header("--- ÂM THANH ---")]
    [Tooltip("AudioSource dùng để phát 1 lần (bắn ngư lôi, trúng đạn...). Kéo 1 AudioSource rảnh vào đây")]
    public AudioSource sfxSource;
    public AudioClip fireSound;
    public AudioClip hitStunSound;

    [Header("--- VA CHẠM / HÚC NHAU ---")]
    [Tooltip("Lực húc tàu kia khi đâm vào")]
    public float pushForce = 6f;


    [Header("--- ANIMATION ---")]
    [Tooltip("Animator của model tàu ngầm - có sẵn bool isWalking/isRunning từ asset")]
    public Animator animator;
    [Tooltip("Tốc độ (m/s) bắt đầu tính là 'Walk' thay vì 'Idle'")]
    public float walkSpeedThreshold = 1f;
    [Tooltip("Tốc độ (m/s) bắt đầu tính là 'Run' thay vì 'Walk'")]
    public float runSpeedThreshold = 7f;

    [Header("--- NGƯ LÔI ---")]
    [Tooltip("Điểm bắn ngư lôi bên trái, kéo object Torpedo_L trong Hierarchy vào đây")]
    public Transform torpedoPointLeft;
    [Tooltip("Điểm bắn ngư lôi bên phải, kéo object Torpedo_R trong Hierarchy vào đây")]
    public Transform torpedoPointRight;
    public GameObject torpedoPrefab;
    public float fireCooldown = 1.5f;
    private float lastFireTime = -10f;
    private bool fireFromLeft = true; // luân phiên trái/phải mỗi lần bắn cho đẹp

    [Header("--- ĐẠN (giới hạn số lượng) ---")]
    [Tooltip("Số ngư lôi tối đa mang theo. Đặt <= 0 để bắn vô hạn (không giới hạn đạn)")]
    public int maxAmmo = 5;
    [Tooltip("Thời gian hồi lại 1 viên đạn (giây). Chỉ có ý nghĩa nếu maxAmmo > 0")]
    public float ammoReloadTime = 3f;
    private int currentAmmo;
    private float ammoReloadTimer = 0f;

    [Header("--- STUN (khi trúng ngư lôi) ---")]
    [HideInInspector] public bool isStunned = false;
    private float stunTimer = 0f;
    [Tooltip("VFX hiển thị khi tàu đang bị stun (optional, kéo prefab particle/vòng xoáy vào đây)")]
    public GameObject stunVFX;
    [Tooltip("Renderer thân tàu để nhấp nháy màu khi bị stun (optional)")]
    public Renderer bodyRenderer;
    [Tooltip("Màu nhấp nháy khi bị stun")]
    public Color stunFlashColor = Color.red;
    [Tooltip("Tốc độ nhấp nháy khi bị stun (lần/giây)")]
    public float stunFlashSpeed = 8f;
    private MaterialPropertyBlock stunPropBlock;
    private static readonly int ColorPropId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropIdLegacy = Shader.PropertyToID("_Color");
    private GameObject activeStunVFXInstance;

    [Header("--- CẢM GIÁC LÁI (feel) ---")]
    [Tooltip("Độ quẹo tối thiểu ngay cả khi gần như đứng yên (0-1). Tăng lên nếu thấy lái bị 'ì' lúc chậm")]
    [Range(0f, 1f)] public float minTurnFactor = 0.35f;
    [Tooltip("Thời gian làm mượt khi bắt đầu quẹo/đổi hướng (giây). Nhỏ = phản hồi nhanh, lớn = trôi lì")]
    public float turnSmoothTime = 0.15f;
    [Tooltip("Góc tối đa nghiêng mũi tàu khi nổi/lặn (độ) - chỉ để đẹp mắt, không ảnh hưởng vật lý thật")]
    public float pitchTiltAngle = 15f;
    [Tooltip("Tốc độ nghiêng mũi tàu theo (độ/giây)")]
    public float pitchTiltSpeed = 90f;

    float turnInputSmoothRef = 0f;
    float smoothedTurnInput = 0f;
    float currentPitch = 0f;

    [HideInInspector] public float currentSpeed;
    // Giữ tương thích ngược với code cũ từng đọc carSpeed
    public float carSpeed => currentSpeed;

    // ====== INTERNAL ======
    Rigidbody subRigidbody;
    bool gameActive = false;
    float verticalVelocitySmoothRef = 0f;

    // Input đọc ở Update, áp lực ở FixedUpdate
    bool inputForward, inputBackward, inputLeft, inputRight, inputUp, inputDown, inputFire;

    void Start()
    {
        subRigidbody = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        subRigidbody.useGravity = false;      // tàu ngầm nổi trong nước, không rơi theo gravity thường
        subRigidbody.linearDamping = waterDrag;
        subRigidbody.angularDamping = waterDrag * 2f;

        stunPropBlock = new MaterialPropertyBlock();
        currentAmmo = maxAmmo;

        // Mặc định tắt điều khiển, chờ RaceMiniGame gọi SetGameActive(true)
        SetGameActive(false);
    }

    // ====== ĐƯỢC GỌI TỪ RaceMiniGame ======
    public void SetGameActive(bool active)
    {
        gameActive = active;

        if (!active)
        {
            if (subRigidbody != null && !subRigidbody.isKinematic)
            {
                subRigidbody.linearVelocity = Vector3.zero;
                subRigidbody.angularVelocity = Vector3.zero;
            }
            inputForward = inputBackward = inputLeft = inputRight = inputUp = inputDown = false;

            // BUG FIX: nếu ván trước kết thúc lúc đang bị stun, phải reset ngay,
            // không thì ván mới bắt đầu tàu vẫn bị khóa điều khiển vô lý vài giây.
            isStunned = false;
            stunTimer = 0f;
            UpdateStunVisual();

            // Reset đạn về đầy khi bắt đầu ván mới
            currentAmmo = maxAmmo;
            ammoReloadTimer = 0f;
        }
    }

    void Update()
    {
        currentSpeed = Vector3.Dot(subRigidbody.linearVelocity, transform.forward);

        UpdateAnimation();
        UpdateStunTimer();
        UpdateStunFlash();
        UpdateAmmoReload();

        if (!gameActive) return;

        if (isStunned)
        {
            // Đang bị stun: không đọc input di chuyển/bắn, tàu trôi tự do theo quán tính + waterDrag
            inputForward = inputBackward = inputLeft = inputRight = inputUp = inputDown = inputFire = false;
            return;
        }

        ReadInput();
        HandleFireInput();
    }

    void UpdateStunTimer()
    {
        if (!isStunned) return;

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
        {
            isStunned = false;
            UpdateStunVisual();
        }
    }

    // Gọi từ TorpedoProjectile khi bị bắn trúng
    public void ApplyStun(float duration)
    {
        bool wasStunned = isStunned;
        isStunned = true;
        stunTimer = Mathf.Max(stunTimer, duration); // không cộng dồn nếu bị bắn liên tiếp, chỉ lấy thời gian dài hơn

        if (!wasStunned)
        {
            UpdateStunVisual();
            PlaySfx(hitStunSound);
        }
    }

    // Phát 1 âm thanh qua sfxSource (PlayOneShot để không cắt ngang âm thanh đang phát khác)
    void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // ====== FEEDBACK KHI BỊ STUN (VFX + nhấp nháy màu) ======
    void UpdateStunVisual()
    {
        if (stunVFX != null)
        {
            if (isStunned && activeStunVFXInstance == null)
            {
                activeStunVFXInstance = Instantiate(stunVFX, transform.position, transform.rotation, transform);
            }
            else if (!isStunned && activeStunVFXInstance != null)
            {
                Destroy(activeStunVFXInstance);
                activeStunVFXInstance = null;
            }
        }

        if (bodyRenderer != null && !isStunned)
        {
            // Trả lại màu gốc (không set gì trong block = dùng màu material mặc định)
            bodyRenderer.SetPropertyBlock(null);
        }
    }

    void UpdateStunFlash()
    {
        if (!isStunned || bodyRenderer == null) return;

        // Nhấp nháy giữa màu gốc và stunFlashColor theo sin wave, không tạo material instance mới (đỡ leak memory)
        float t = (Mathf.Sin(Time.time * stunFlashSpeed) + 1f) * 0.5f;
        bodyRenderer.GetPropertyBlock(stunPropBlock);
        Color flashColor = Color.Lerp(Color.white, stunFlashColor, t);
        stunPropBlock.SetColor(ColorPropId, flashColor);
        stunPropBlock.SetColor(ColorPropIdLegacy, flashColor);
        bodyRenderer.SetPropertyBlock(stunPropBlock);
    }

    // ====== HỒI ĐẠN ======
    void UpdateAmmoReload()
    {
        if (maxAmmo <= 0) return; // bắn vô hạn, không cần hồi đạn
        if (currentAmmo >= maxAmmo) return;

        ammoReloadTimer += Time.deltaTime;
        if (ammoReloadTimer >= ammoReloadTime)
        {
            ammoReloadTimer = 0f;
            currentAmmo++;
        }
    }

    // Cho UI bên ngoài đọc số đạn còn lại / % hồi đạn viên tiếp theo
    public int GetCurrentAmmo() => currentAmmo;
    public float GetAmmoReloadProgress() => maxAmmo <= 0 ? 1f : Mathf.Clamp01(ammoReloadTimer / ammoReloadTime);

    // ====== ANIMATION ======
    void UpdateAnimation()
    {
        if (animator == null) return;

        float speedAbs = Mathf.Abs(currentSpeed);

        bool shouldRun = speedAbs >= runSpeedThreshold;
        bool shouldWalk = !shouldRun && speedAbs >= walkSpeedThreshold;

        animator.SetBool("isRunning", shouldRun);
        animator.SetBool("isWalking", shouldWalk);
        // isBurrowed không dùng cho tàu ngầm - model chỉ có Idle/Walk/Run
    }

    void FixedUpdate()
    {
        // BUG FIX: kẹp biên nổi/lặn phải chạy LUÔN LUÔN, không phụ thuộc gameActive/isStunned,
        // vì lực húc giữa 2 tàu (OnCollisionEnter) có thể đẩy tàu vượt biên bất cứ lúc nào,
        // kể cả khi đang bị stun hoặc game tạm dừng.
        ClampDepth();

        if (!gameActive) return;
        if (isStunned) return; // đang bị stun - không nhận lực điều khiển, chỉ trôi theo waterDrag
        ApplyPhysics();
    }

    // Kẹp cứng cả vị trí lẫn vận tốc theo trục Y, chạy độc lập mọi nguồn chuyển động
    // (thrust, húc nhau, trôi tự do khi stun...), không chỉ riêng lúc ApplyPhysics() chạy.
    void ClampDepth()
    {
        if (subRigidbody == null) return;

        Vector3 pos = subRigidbody.position;
        Vector3 vel = subRigidbody.linearVelocity;
        bool changed = false;

        if (pos.y > maxDepthY)
        {
            pos.y = maxDepthY;
            if (vel.y > 0f) vel.y = 0f;
            changed = true;
        }
        else if (pos.y < minDepthY)
        {
            pos.y = minDepthY;
            if (vel.y < 0f) vel.y = 0f;
            changed = true;
        }

        if (changed)
        {
            subRigidbody.position = pos;
            subRigidbody.linearVelocity = vel;
        }
    }

    // ====== ĐỌC INPUT ======
    void ReadInput()
    {
        if (playerId == PlayerID.Player1)
        {
            inputForward = Input.GetKey(KeyCode.W);
            inputBackward = Input.GetKey(KeyCode.S);
            inputLeft = Input.GetKey(KeyCode.A);
            inputRight = Input.GetKey(KeyCode.D);
            inputUp = Input.GetKey(KeyCode.Space);       // nổi lên
            inputDown = Input.GetKey(KeyCode.LeftShift); // lặn xuống
            inputFire = Input.GetKeyDown(KeyCode.J);
        }
        else // Player2
        {
            inputForward = Input.GetKey(KeyCode.UpArrow);
            inputBackward = Input.GetKey(KeyCode.DownArrow);
            inputLeft = Input.GetKey(KeyCode.LeftArrow);
            inputRight = Input.GetKey(KeyCode.RightArrow);
            inputUp = Input.GetKey(KeyCode.RightShift);      // nổi lên
            inputDown = Input.GetKey(KeyCode.RightControl);  // lặn xuống
            inputFire = Input.GetKeyDown(KeyCode.Keypad1);
        }
    }

    // ====== BẮN NGƯ LÔI ======
    void HandleFireInput()
    {
        if (!inputFire) return;
        if (Time.time - lastFireTime < fireCooldown) return;
        if (torpedoPrefab == null) return;

        // Giới hạn đạn: nếu maxAmmo > 0 thì phải còn đạn mới bắn được
        if (maxAmmo > 0 && currentAmmo <= 0) return;

        Transform firePoint = fireFromLeft ? torpedoPointLeft : torpedoPointRight;
        if (firePoint == null) firePoint = torpedoPointLeft != null ? torpedoPointLeft : torpedoPointRight;
        if (firePoint == null) return; // chưa gán điểm bắn nào cả

        fireFromLeft = !fireFromLeft; // luân phiên bên cho lần bắn sau
        lastFireTime = Time.time;

        if (maxAmmo > 0)
        {
            currentAmmo--;
        }

        GameObject torpedo = Instantiate(torpedoPrefab, firePoint.position, transform.rotation);
        TorpedoProjectile projectile = torpedo.GetComponent<TorpedoProjectile>();
        if (projectile != null)
            projectile.Launch(transform.forward, this);

        PlaySfx(fireSound);
    }

    // ====== ÁP DỤNG VẬT LÝ ======
    void ApplyPhysics()
    {
        // Tiến / lùi - đẩy theo hướng forward của tàu
        if (inputForward && currentSpeed < maxSpeed)
        {
            subRigidbody.AddForce(transform.forward * thrustForce, ForceMode.Acceleration);
        }
        if (inputBackward && currentSpeed > -maxReverseSpeed)
        {
            subRigidbody.AddForce(-transform.forward * thrustForce * 0.6f, ForceMode.Acceleration);
        }

        // Xoay trái / phải quanh trục Y - dùng MoveRotation (không phải transform.Rotate) để không
        // bị giật khi Rigidbody có Interpolation.
        // FIX CẢM GIÁC LÁI: trước đây turnFactor = Clamp01(speed/2) khiến xe gần như không quẹo
        // nổi lúc còn chậm (ì). Giờ có minTurnFactor làm sàn, luôn quẹo được 1 mức tối thiểu.
        float speedTurnFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / 2f);
        float turnFactor = Mathf.Max(speedTurnFactor, minTurnFactor);

        float rawTurnInput = 0f;
        if (inputLeft) rawTurnInput -= 1f;
        if (inputRight) rawTurnInput += 1f;

        // FIX CẢM GIÁC LÁI: làm mượt input quẹo thay vì nhảy thẳng -1/0/1 mỗi frame,
        // giúp đổi hướng không bị "khựng" đột ngột.
        smoothedTurnInput = Mathf.SmoothDamp(smoothedTurnInput, rawTurnInput, ref turnInputSmoothRef, turnSmoothTime);

        if (Mathf.Abs(smoothedTurnInput) > 0.001f)
        {
            float turnAngleThisStep = smoothedTurnInput * turnSpeed * turnFactor * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, turnAngleThisStep, 0f);
            subRigidbody.MoveRotation(subRigidbody.rotation * deltaRotation);
        }

        // Nổi lên / lặn xuống - dùng SmoothDamp để tăng/giảm lực mượt dần, không bật/tắt đột ngột
        float targetVerticalSpeed = 0f;
        if (inputUp) targetVerticalSpeed = maxVerticalSpeed;
        else if (inputDown) targetVerticalSpeed = -maxVerticalSpeed;

        Vector3 currentVel = subRigidbody.linearVelocity;
        float smoothedVerticalSpeed = Mathf.SmoothDamp(
            currentVel.y,
            targetVerticalSpeed,
            ref verticalVelocitySmoothRef,
            0.2f // thời gian làm mượt (giây)
        );

        // Không cho vượt biên ngay tại đây - làm mượt dần về 0 khi gần biên thay vì cắt cứng
        if (subRigidbody.position.y >= maxDepthY && smoothedVerticalSpeed > 0f)
            smoothedVerticalSpeed = 0f;
        if (subRigidbody.position.y <= minDepthY && smoothedVerticalSpeed < 0f)
            smoothedVerticalSpeed = 0f;

        currentVel.y = smoothedVerticalSpeed;
        subRigidbody.linearVelocity = currentVel;

        // FIX "KHÔNG PHÊ" KHI NỔI/LẶN: nghiêng mũi tàu lên/xuống theo hướng đang di chuyển
        // theo trục Y, tạo phản hồi hình ảnh rõ ràng thay vì thân tàu trôi phẳng lì vô cảm.
        // Chỉ là hiệu ứng thị giác (visual only), không ảnh hưởng vật lý thật - an toàn tuyệt đối.
        float targetPitch = 0f;
        if (smoothedVerticalSpeed > 0.1f) targetPitch = -pitchTiltAngle;      // đang nổi -> ngẩng mũi lên
        else if (smoothedVerticalSpeed < -0.1f) targetPitch = pitchTiltAngle; // đang lặn -> cúi mũi xuống

        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, pitchTiltSpeed * Time.fixedDeltaTime);

        // Áp tilt lên local rotation X, giữ nguyên yaw (Y) đã tính ở trên
        Vector3 currentEuler = subRigidbody.rotation.eulerAngles;
        Quaternion targetRot = Quaternion.Euler(currentPitch, currentEuler.y, 0f);
        subRigidbody.MoveRotation(targetRot);
    }

    // ====== HÚC NHAU GIỮA 2 TÀU ======
    private void OnCollisionEnter(Collision collision)
    {
        if (!gameActive) return;

        PlayerSubmarineController otherSub = collision.gameObject.GetComponent<PlayerSubmarineController>();
        if (otherSub == null) return;

        Vector3 pushDir = collision.transform.position - transform.position;
        pushDir.Normalize(); // giữ nguyên cả trục Y - va chạm tàu ngầm có thể đẩy lệch cả chiều sâu

        float impactRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        otherSub.GetComponent<Rigidbody>().AddForce(pushDir * pushForce * impactRatio, ForceMode.VelocityChange);
    }
}