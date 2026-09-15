using UnityEngine;

public class AnswerPad : MonoBehaviour
{
    public MathManager mathManager;
    public int padIndex; // 0=A, 1=B, 2=C, 3=D

    // Sử dụng OnTriggerStay để liên tục cập nhật ô lựa chọn khi player đang đứng trong ô
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem vật thể chạm vào ô có phải là người chơi hay không
        if (other.gameObject.name.Contains("Player Play"))
        {
            MathManager mathManager = FindFirstObjectByType<MathManager>();
            if (mathManager != null)
            {
                // Truyền trực tiếp GameObject (other.gameObject) và chỉ số ô (padIndex)
                mathManager.OnPlayerStepOnPad(other.gameObject, this.padIndex);
            }
        }
    }
}