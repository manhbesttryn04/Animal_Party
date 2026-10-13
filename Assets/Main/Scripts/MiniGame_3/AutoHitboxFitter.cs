using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AutoHitboxFitter : MonoBehaviour
{
    [Header("--- Kéo cục Particle Lửa vào đây ---")]
    public ParticleSystem fireParticle;

    [Header("--- Độ phình to của Hitbox (Bề ngang) ---")]
    public float thicknessMultiplier = 1.5f;

    // Lệnh này tạo ra một nút bấm đặc biệt ngay trong Editor của Unity
    [ContextMenu("🔥 Tự Động Fit Hitbox 🔥")]
    public void AutoFit()
    {
        if (fireParticle == null)
        {
           // Debug.LogError("Chưa kéo object Lửa vào ô Fire Particle kìa!");
            return;
        }

        BoxCollider col = GetComponent<BoxCollider>();
        var main = fireParticle.main;

        // Tính chiều dài tia lửa: Vận tốc (Start Speed) * Thời gian sống (Start Lifetime)
        float fireLength = main.startSpeed.constant * main.startLifetime.constant;
        
        // Tính độ dày của hitbox dựa trên kích thước hạt (Start Size)
        float fireThickness = main.startSize.constant * thicknessMultiplier;

        // Tự động điền số cho BoxCollider
        col.size = new Vector3(fireThickness, fireThickness, fireLength);
        
        // Đẩy tâm Z ra giữa để đuôi hộp neo đúng vào họng súng
        col.center = new Vector3(0, 0, fireLength / 2f); 
        
        // Tự động tích luôn ô Is Trigger cho khỏi quên
        col.isTrigger = true;

       // Debug.Log($"<color=green><b>[Đã Căn Xong!]</b></color> Chiều dài Hitbox: {fireLength}");
    }
}