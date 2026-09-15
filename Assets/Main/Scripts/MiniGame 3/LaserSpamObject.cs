using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserSpamObject : MonoBehaviour
{
    [Header("Cấu hình tia")]
    public float laserLength = 50f; 
    public float laserWidth = 0.4f;  
    public float speed = 8f;         // Tốc độ di chuyển (khi spam nên để nhanh một chút)
    public float maxDistance = 40f;  // Đi hết bao nhiêu mét thì tự hủy

    private LineRenderer line;
    private Vector3 startPosition;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false; 

        // Vẽ tia laser nằm ngang
        line.startWidth = laserWidth;
        line.endWidth = laserWidth;
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(-laserLength / 2f, 0, 0));
        line.SetPosition(1, new Vector3(laserLength / 2f, 0, 0));

        startPosition = transform.position;
    }

    void Update()
    {
        // Di chuyển tịnh tiến về phía trước theo trục Z của vật thể
        transform.position += transform.forward * speed * Time.deltaTime;

        // Nếu vượt quá khoảng cách tối đa thì tự động hủy hoàn toàn để tránh rác bộ nhớ
        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }

        // Dò va chạm tàng hình bằng ống SphereCast
        Vector3 startPoint = transform.TransformPoint(new Vector3(-laserLength / 2f, 0, 0));
        float radius = laserWidth / 2f;
        RaycastHit[] hits = Physics.SphereCastAll(startPoint, radius, transform.right, laserLength);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Player"))
            {
              //  Debug.Log("Spam Laser đã chém trúng: " + hit.collider.name);
            }
        }
    }
}