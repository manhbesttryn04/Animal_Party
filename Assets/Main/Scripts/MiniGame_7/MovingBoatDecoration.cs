using UnityEngine;

public class MovingBoatDecoration : MonoBehaviour
{
    [Header("Cấu hình Lộ trình")]
    [Tooltip("Kéo thả GameObject vị trí xuất phát A vào đây")]
    public Transform pointA;

    [Tooltip("Kéo thả GameObject vị trí đích B vào đây")]
    public Transform pointB;

    [Header("Cấu hình Tốc độ")]
    [Tooltip("Tốc độ di chuyển của thuyền (m/s)")]
    public float moveSpeed = 2f;

    [Tooltip("Thời gian chờ (giây) tại điểm A trước khi bắt đầu lượt chạy mới")]
    public float respawnDelay = 1f;

    private float delayTimer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        // Kiểm tra xem người dùng đã kéo đủ điểm chưa
        if (pointA == null || pointB == null)
        {
           // Debug.LogError($"[MovingBoat] Vui lòng kéo đầy đủ Point A và Point B vào Script trên {gameObject.name}!");
            enabled = false; // Tắt script nếu thiếu dữ liệu để tránh lỗi hệ thống
            return;
        }

        // Đặt thuyền về vị trí xuất phát ban đầu
        ResetToPointA();
    }

    void Update()
    {
        // Nếu đang trong trạng thái chờ hồi sinh tại điểm A
        if (isWaiting)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= respawnDelay)
            {
                isWaiting = false;
                delayTimer = 0f;
            }
            return; // Tạm dừng di chuyển trong lúc đợi
        }

        // Di chuyển thuyền tịnh tiến từ vị trí hiện tại hướng về điểm B
        transform.position = Vector3.MoveTowards(transform.position, pointB.position, moveSpeed * Time.deltaTime);

        // Tính khoảng cách còn lại tới điểm B
        float distanceToDestination = Vector3.Distance(transform.position, pointB.position);

        // Khi thuyền đã đến sát điểm B (khoảng cách nhỏ hơn 0.05 mét)
        if (distanceToDestination < 0.05f)
        {
            ResetToPointA();
        }
    }

    void ResetToPointA()
    {
        // Dịch chuyển tức thời vị trí về điểm A
        transform.position = pointA.position;

        // Tự động quay mũi thuyền hướng về phía điểm B để thuyền chạy xuôi dòng
        Vector3 directionToB = (pointB.position - pointA.position).normalized;
        if (directionToB != Vector3.zero)
        {
            directionToB.y = 0; // Đảm bảo thuyền luôn thăng bằng trên mặt nước, không bị chúi mũi xuống
            transform.rotation = Quaternion.LookRotation(directionToB);
        }

        // Kích hoạt trạng thái chờ
        isWaiting = true;
        delayTimer = 0f;
    }
}