using UnityEngine;
using System.Collections.Generic;
public class WallFlamethrowerCore : MonoBehaviour
{
    [Header("--- Kích Hoạt Hệ Thống ---")]
    [Tooltip("Bật/Tắt lửa trực tiếp để test nhanh trong Editor")]
    public bool isFiring = true;

    [Header("--- Cấu Hình Thành Phần (References) ---")]
    [Tooltip("Kéo Particle System hiệu ứng lửa vào đây")]
    public ParticleSystem fireParticles;

    [Tooltip("Kéo Box Collider (Vùng gây sát thương) vào đây")]
    public BoxCollider fireCollider;

    [Header("--- Cấu Hình Sát Thương & Vật Lý ---")]
    [Tooltip("Lực hất văng người chơi ra khỏi luồng lửa")]
    public float knockbackForce = 12f;

    [Tooltip("Thời gian giãn cách giữa các lần đốt máu (giây)")]
    public float hitCooldown = 0.5f;

    // Bộ nhớ đệm lưu lại thời gian một Collider bị đốt gần nhất
    private readonly Dictionary<Collider, float> _lastHitTimes = new Dictionary<Collider, float>();
    private bool _lastState = false;

    void Start()
    {
        // Đồng bộ trạng thái ban đầu của bẫy
        SyncFlamethrowerState(isFiring);
    }

    void Update()
    {
        // Hỗ trợ cập nhật trạng thái thời gian thực khi bạn tick chọn nút trên Inspector lúc đang chạy game
        if (isFiring != _lastState)
        {
            SyncFlamethrowerState(isFiring);
        }
    }

    private void OnDisable()
    {
        ForceStopImmediately();
    }

    public void ForceStopImmediately()
    {
        SyncFlamethrowerState(false);
    }

    public void SyncFlamethrowerState(bool state)
    {
        isFiring = state;
        _lastState = state;
        var audio = AudioManager.Instance;
        if (fireCollider != null)
        {
            fireCollider.enabled = state;
        }

        if (fireParticles != null)
        {
            if (state)
            {
                fireParticles.Play();
            }
            else
            {
                // Xóa toàn bộ hạt đang sống để lửa biến mất ngay khi hết giờ.
                fireParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }

        // Âm thanh phải được xử lý kể cả khi quên gán Particle System.
        if (audio != null)
        {
            if (state)
                audio.PlaySFXNoOneShot(audio.openFireClip);
            else
                audio.StopSFXNoOneShot();
        }

        // Nếu tắt lửa thì dọn dẹp bộ nhớ đệm để sẵn sàng cho lần xịt kế tiếp
        if (!state)
        {
            _lastHitTimes.Clear();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isFiring) return;

        if (!CanHitTarget(other)) return;

        // CHỈ TẬP TRUNG XỬ LÝ ĐẨY VĂNG VẬT LÝ (KNOCKBACK)
        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb != null)
        {
            // Triệt tiêu một phần quán tính cũ
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.2f, 0f, rb.linearVelocity.z * 0.2f);

            // Đẩy nhân vật bay theo hướng họng súng
            Vector3 pushDir = transform.forward;
            pushDir.y = 0.7f;

            rb.AddForce(pushDir * knockbackForce, ForceMode.Impulse);

            // Ghi nhận thời gian va chạm
            _lastHitTimes[other] = Time.time;
        }
    }

    private bool CanHitTarget(Collider target)
    {
        if (_lastHitTimes.TryGetValue(target, out float lastTime))
        {
            return (Time.time - lastTime) >= hitCooldown;
        }
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 1. Tự động kiểm tra lỗi lật ngược Scale (Negative Scale) gây hỏng ma trận vật lý
        if (transform.localScale.x < 0 || transform.localScale.y < 0 || transform.localScale.z < 0)
        {

        }
        // 2. Tự động nhắc nhở nếu quên chưa tích chọn thuộc tính Trigger trên Collider của lửa
        if (fireCollider != null && !fireCollider.isTrigger)
        {
           
            fireCollider.isTrigger = true;
        }
    }
#endif
}