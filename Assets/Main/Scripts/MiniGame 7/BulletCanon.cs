using UnityEngine;

using UnityEngine;
using System.Collections;

public class BulletCanon : MonoBehaviour
{
    [Header("Cấu hình Đạn")]
    public float speed = 15f;
    public float lifeTime = 4f;

    [Header("Phản Đạn")]
    [Range(0f, 90f)]
    public float reflectAngle = 45f;

    public float reflectCooldown = 0.15f;
    public float pushOutDistance = 0.3f;

    [Header("Hiệu ứng Nổ")]
    public GameObject explosionPrefab;
    public float explosionDestroyTime = 2f;

    [Header("Âm thanh Nổ")]
    public AudioClip explosionSound;

    [Range(0f, 1f)]
    public float volume = 1f;

    private Rigidbody rb;

    private bool canReflect = true;
    private bool hasExploded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /*
     * Hàm này chỉ dùng nếu script khác muốn truyền hướng bay.
     * Trong MiniGame7, đạn đã được gán linearVelocity khi Instantiate,
     * nên không bắt buộc phải gọi hàm này.
     */
    public void SetupDirection(Vector3 direction)
    {
        if (rb == null)
            return;

        Vector3 normalizedDirection = direction.normalized;

        rb.linearVelocity =
            normalizedDirection * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded)
            return;

        // Kiểm tra khiên trước PlayerType,
        // vì collider khiên có thể nằm bên trong Player.
        if (other.CompareTag("Defense"))
        {
            TryReflect(other.transform);
            return;
        }

        PlayerType playerType =
            other.GetComponentInParent<PlayerType>();

        if (playerType != null ||
            other.CompareTag("Player 1") ||
            other.CompareTag("Player 2"))
        {
            TriggerExplosion();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded)
            return;
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buffDeffClip);    
        GameObject hitObject =
            collision.gameObject;

        if (hitObject.CompareTag("Defense"))
        {
            TryReflect(hitObject.transform);
            return;
        }

        PlayerType playerType =
            hitObject.GetComponentInParent<PlayerType>();

        if (playerType != null ||
            hitObject.CompareTag("Player 1") ||
            hitObject.CompareTag("Player 2"))
        {
            TriggerExplosion();
        }
    }

    private void TryReflect(Transform shield)
    {
        if (!canReflect || rb == null)
            return;

        canReflect = false;

        ReflectBullet(shield);

        StartCoroutine(ReflectCooldownRoutine());
    }

    private void ReflectBullet(Transform shield)
    {
        /*
         * Chọn góc ngẫu nhiên trong vùng hình tam giác:
         *
         *               0°
         *              ↑
         *         ↖         ↗
         *      -45°         +45°
         */

        float randomAngle =
            Random.Range(
                -reflectAngle,
                reflectAngle
            );

        Vector3 direction =
            Quaternion.AngleAxis(
                randomAngle,
                Vector3.up
            ) * shield.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = -rb.linearVelocity.normalized;

        direction.Normalize();

        float currentSpeed =
            rb.linearVelocity.magnitude;

        // Phòng trường hợp Rigidbody đang đứng yên
        if (currentSpeed < 0.1f)
            currentSpeed = speed;

        rb.linearVelocity =
            direction * currentSpeed;

        // Xoay đầu viên đạn theo hướng mới
        transform.forward = direction;

        // Đẩy ra khỏi collider khiên để tránh va chạm lại ngay
        rb.position +=
            direction * pushOutDistance;
    }

    private IEnumerator ReflectCooldownRoutine()
    {
        yield return new WaitForSeconds(
            reflectCooldown
        );

        canReflect = true;
    }

    private void TriggerExplosion()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance.boomClip
            );
        }

        if (explosionPrefab != null)
        {
            GameObject explosion =
                Instantiate(
                    explosionPrefab,
                    transform.position,
                    Quaternion.identity
                );

            Destroy(
                explosion,
                explosionDestroyTime
            );
        }

        Destroy(gameObject);
    }
}