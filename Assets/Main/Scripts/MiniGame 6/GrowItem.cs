using UnityEngine;

public class GrowItem : MonoBehaviour
{
    private MiniGame5 manager;

    // Bán kính quét xem Player có đến gần để nhặt không
    private float pickupRadius = 0.6f;

    // Hàm này giúp GameManager truyền tham chiếu quản lý vào vật phẩm khi sinh ra
    public void Setup(MiniGame5 gameManager)
    {
        manager = gameManager;
    }

    void Update()
    {
        // Hiệu ứng xoay xoay cho vật phẩm nhìn đẹp mắt và sinh động hơn
        transform.Rotate(Vector3.up * 90f * Time.deltaTime);

        if (manager == null || !manager.isPlaying) return;

        // Quét xung quanh vật phẩm xem có Player nào chạm vào không
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (Collider col in hitColliders)
        {
            PlayerType pType = col.GetComponent<PlayerType>();
            if (pType != null)
            {
                // Báo cho GameManager biết Player này đã ăn vật phẩm
                manager.OnGrowItemPickedUp(pType.isPlayer2, col.gameObject);

                // Tự hủy cục vật phẩm này sau khi bị ăn
                Destroy(gameObject);
                break;
            }
        }
    }

    // Vẽ vòng tròn giả lập trong Cửa sổ Scene để bạn dễ căn chỉnh bán kính nhặt
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}