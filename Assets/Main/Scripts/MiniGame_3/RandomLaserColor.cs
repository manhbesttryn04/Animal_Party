using UnityEngine;

public class RandomLaserColor : MonoBehaviour
{
    [Header("Bảng Màu Tùy Chọn")]
    [Tooltip("Thêm các màu bạn muốn Laser đổi vào đây")]
    public Color[] possibleColors;

    [Header("Cấu hình Tốc độ đổi màu (Liên kết thời gian)")]
    [Tooltip("Cứ sau bao nhiêu giây thì laser sẽ tự động đổi màu một lần khi đang chơi")]
    public float changeColorInterval = 1.5f; 

    [Header("Độ Phát Sáng (Emission)")]
    public float emissionIntensity = 3.5f;

    private float colorTimer = 0f;
    private Color lastColor;

    void Start()
    {
        // Đổi màu phát đầu tiên ngay khi vào trận
        ChangeToRandomColor();
    }

    void Update()
    {
        // LIÊN KẾT VỚI THỜI GIAN:
        // Đếm xuôi thời gian thực của game. Game chạy đến đâu, timer tăng đến đó.
        colorTimer += Time.deltaTime;

        // Cứ mỗi khi thời gian tích lũy vượt quá số giây quy định thì đổi màu
        if (colorTimer >= changeColorInterval)
        {
            ChangeToRandomColor();
            colorTimer = 0f; // Reset cái đồng hồ nhỏ này về 0 để đếm lại vòng mới
        }
    }

    // Hàm xử lý đổi màu ngẫu nhiên
    void ChangeToRandomColor()
    {
        if (possibleColors.Length == 0) return;

        // Lắc xí ngầu chọn màu
        int randomIndex = Random.Range(0, possibleColors.Length);
        Color pickedColor = possibleColors[randomIndex];

        // Mẹo chống sượng: Nếu chọn trùng y hệt màu vừa đổi ở vòng trước, 
        // bắt nó nhảy sang màu kế tiếp trong danh sách luôn cho mới mẻ.
        if (possibleColors.Length > 1 && pickedColor == lastColor)
        {
            randomIndex = (randomIndex + 1) % possibleColors.Length;
            pickedColor = possibleColors[randomIndex];
        }

        // Ghi nhớ màu này lại để vòng sau check
        lastColor = pickedColor;

        // Quét sạch các bộ phận hiển thị hình ảnh của Laser Hub để nhuộm màu mới
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in allRenderers)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            
            // Đổi màu gốc
            block.SetColor("_BaseColor", pickedColor); 
            // Đổi màu phát sáng HDR
            block.SetColor("_EmissionColor", pickedColor * emissionIntensity); 

            r.SetPropertyBlock(block);
        }
    }
}