using UnityEngine;

/// <summary>
/// Ngư lôi bắn ra từ Torpedo_L/Torpedo_R. Bay THẲNG TUYỆT ĐỐI sau khi bắn (không cong,
/// không đuổi theo) - nhưng lúc bắn ra, nếu có tàu đối phương nằm trong góc ngắm gần đúng,
/// code tự tính "điểm đón đầu" (dựa theo vận tốc hiện tại của đối phương lúc đó) để nhắm
/// hộ hướng bắn, giống pháo thủ tàu chiến ngắm đón đầu mục tiêu di chuyển.
/// Nếu đối phương đổi hướng SAU khi ngư lôi đã bắn ra, ngư lôi KHÔNG tự chỉnh lại - vẫn bay
/// theo đường thẳng đã tính từ đầu, nên vẫn có thể bắn hụt nếu đoán sai, không phải auto-trúng.
/// </summary>
public class TorpedoProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 4f;
    public float stunDuration = 2f;
    public float hitRadius = 0.6f;

    [Header("--- LEAD PREDICTION (ngắm đón đầu lúc bắn) ---")]
    [Tooltip("Góc hẹp phía trước lúc bắn để tính mục tiêu được ngắm đón đầu (độ, tính 1 bên)")]
    public float predictionConeAngle = 25f;
    [Tooltip("Khoảng cách tối đa để tính mục tiêu được ngắm đón đầu")]
    public float predictionMaxRange = 30f;

    [Tooltip("Bù góc xoay cho mesh (Capsule mặc định nằm dọc trục Y, cần xoay X=90 để nằm ngang theo hướng bay). Thử đổi giá trị nếu vẫn sai hướng.")]
    public Vector3 meshRotationOffsetEuler = new Vector3(90f, 0f, 0f);

    [Header("--- VFX KHI TRÚNG ---")]
    [Tooltip("Prefab hiệu ứng nổ/va chạm, spawn tại đúng vị trí trúng đạn")]
    public GameObject hitVFXPrefab;
    [Tooltip("Thời gian tự hủy VFX sau khi spawn (giây) - phòng trường hợp prefab VFX không tự hủy sẵn")]
    public float hitVFXLifeTime = 2f;
    [Tooltip("Âm thanh nổ khi ngư lôi trúng tàu hoặc trúng tường/địa hình")]
    public AudioClip explosionSound;
    [Range(0f, 1f)] public float explosionVolume = 1f;

    [Header("--- VA CHẠM MÔI TRƯỜNG (bug fix) ---")]
    [Tooltip("Layer của tường/đá/địa hình. Ngư lôi trúng layer này sẽ nổ luôn thay vì bay xuyên qua")]
    public LayerMask environmentMask;

    private Vector3 direction;
    private Vector3 previousPosition;
    private PlayerSubmarineController owner;

    public void Launch(Vector3 dir, PlayerSubmarineController shooter)
    {
        Vector3 fallbackDirection = dir.normalized;
        owner = shooter;

        PlayerSubmarineController predictedTarget = FindPredictionTarget(shooter, fallbackDirection);

        if (predictedTarget != null)
        {
            Vector3 leadDirection = CalculateLeadDirection(predictedTarget);
            direction = leadDirection != Vector3.zero ? leadDirection : fallbackDirection;
        }
        else
        {
            direction = fallbackDirection;
        }

        previousPosition = transform.position;
        UpdateVisualRotation();
        Destroy(gameObject, lifeTime);
    }

    // Chỉ tính đón đầu nếu có tàu đối phương nằm trong góc hẹp phía trước + trong tầm lúc bắn
    PlayerSubmarineController FindPredictionTarget(PlayerSubmarineController shooter, Vector3 fireDirection)
    {
        PlayerSubmarineController[] allSubs = FindObjectsOfType<PlayerSubmarineController>();
        PlayerSubmarineController best = null;
        float bestAngle = float.MaxValue;

        foreach (var sub in allSubs)
        {
            if (sub == shooter) continue;

            Vector3 toTarget = sub.transform.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist > predictionMaxRange) continue;

            float angle = Vector3.Angle(fireDirection, toTarget);
            if (angle > predictionConeAngle) continue;

            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = sub;
            }
        }

        return best;
    }

    // Giải phương trình bậc 2 tìm thời gian chặn (intercept time), dựa theo vận tốc hiện tại
    // của mục tiêu tại THỜI ĐIỂM BẮN - không cập nhật lại sau đó.
    Vector3 CalculateLeadDirection(PlayerSubmarineController target)
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        Vector3 targetVelocity = targetRb != null ? targetRb.linearVelocity : Vector3.zero;
        Vector3 toTarget = target.transform.position - transform.position;

        float a = targetVelocity.sqrMagnitude - speed * speed;
        float b = 2f * Vector3.Dot(targetVelocity, toTarget);
        float c = toTarget.sqrMagnitude;

        float interceptTime = 0f;
        bool solved = false;

        if (Mathf.Abs(a) > 0.0001f)
        {
            float discriminant = b * b - 4f * a * c;
            if (discriminant >= 0f)
            {
                float sqrtDisc = Mathf.Sqrt(discriminant);
                float t1 = (-b + sqrtDisc) / (2f * a);
                float t2 = (-b - sqrtDisc) / (2f * a);

                // Lấy nghiệm dương nhỏ nhất hợp lệ
                if (t1 > 0f && t2 > 0f) interceptTime = Mathf.Min(t1, t2);
                else if (t1 > 0f) interceptTime = t1;
                else if (t2 > 0f) interceptTime = t2;
                else solved = false;

                solved = interceptTime > 0f;
            }
        }
        else if (Mathf.Abs(b) > 0.0001f)
        {
            // Trường hợp a ~ 0 (tốc độ mục tiêu ~ tốc độ đạn) - giải tuyến tính
            interceptTime = -c / b;
            solved = interceptTime > 0f;
        }

        if (!solved)
            return Vector3.zero; // không giải được -> dùng hướng bắn gốc (fallback)

        Vector3 interceptPoint = target.transform.position + targetVelocity * interceptTime;
        return (interceptPoint - transform.position).normalized;
    }

    void Update()
    {
        Vector3 nextPosition = transform.position + direction * speed * Time.deltaTime;

        // BUG FIX: check môi trường bằng Raycast giữa vị trí cũ và mới, tránh trường hợp
        // di chuyển nhanh "nhảy" qua tường mỏng trong 1 frame (tunneling).
        if (CheckEnvironmentHit(previousPosition, nextPosition))
        {
            previousPosition = nextPosition;
            return; // đã bị destroy trong CheckEnvironmentHit
        }

        previousPosition = transform.position;
        transform.position = nextPosition;
        CheckHit();
    }

    void UpdateVisualRotation()
    {
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(meshRotationOffsetEuler);
    }

    // Trúng tàu đối phương -> stun. Trúng môi trường (tường/đá) xử lý riêng ở CheckEnvironmentHit.
    void CheckHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);
        foreach (var hit in hits)
        {
            PlayerSubmarineController target = hit.GetComponentInParent<PlayerSubmarineController>();
            if (target == null || target == owner) continue;

            target.ApplyStun(stunDuration);
            SpawnHitVFX(transform.position);
            Destroy(gameObject);
            return;
        }
    }

    // BUG FIX: ngư lôi trước đây bay xuyên tường/địa hình vì CheckHit chỉ check tàu, không check môi trường.
    bool CheckEnvironmentHit(Vector3 from, Vector3 to)
    {
        if (environmentMask.value == 0) return false; // chưa gán layer nào -> bỏ qua check (giữ hành vi cũ)

        Vector3 delta = to - from;
        float dist = delta.magnitude;
        if (dist <= 0.0001f) return false;

        if (Physics.SphereCast(from, hitRadius, delta.normalized, out RaycastHit hitInfo, dist, environmentMask))
        {
            SpawnHitVFX(hitInfo.point);
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    void SpawnHitVFX(Vector3 atPosition)
    {
        if (explosionSound != null)
        {
            // Dùng PlayClipAtPoint vì object ngư lôi sắp bị Destroy ngay sau đó,
            // không thể phát qua AudioSource gắn trên chính nó.
            AudioSource.PlayClipAtPoint(explosionSound, atPosition, explosionVolume);
        }

        if (hitVFXPrefab == null) return;

        GameObject vfx = Instantiate(hitVFXPrefab, atPosition, Quaternion.identity);
        Destroy(vfx, hitVFXLifeTime);
    }
}