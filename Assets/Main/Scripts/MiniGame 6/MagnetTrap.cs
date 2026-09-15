using UnityEngine;

public class MagnetTrap : TrapBase
{
    [Header("--- MAGNET TRAP ---")]
    [Tooltip("Lực hút thằng kia về phía mình")]
    public float pullForce = 15f;

    [Tooltip("Thời gian hiệu ứng hút kéo dài")]
    public float magnetDuration = 2.5f;

    [Header("--- VFX SETTINGS ---")]
    [Tooltip("VFX vòng xoáy/nam châm dính dưới chân người đạp bẫy")]
    public GameObject magnetVFX;

    public Vector3 vfxOffset = new Vector3(0, 0.1f, 0); // Để sát mặt đất dưới chân
    public Vector3 vfxScale = new Vector3(1f, 1f, 1f);

    protected override void OnPlayerHit(BombCarrier victim)
    {
        // Gọi MiniGame6 hoặc Manager để tìm đối thủ của người vừa đạp bẫy
        BombCarrier targetToPull = MiniGame6.Instance?.GetOtherPlayer(victim);

        if (targetToPull == null)
        { 
            return;
        }

        if (targetToPull.IsEliminated())
        {
           // Debug.Log($"[TRAP DEBUG] {name}: KHÔNG hút vì đối thủ '{targetToPull.name}' đã bị loại.");
            return;
        }

        if (targetToPull.IsFrozen())
        {
            //Debug.Log($"[TRAP DEBUG] {name}: KHÔNG hút vì đối thủ '{targetToPull.name}' đang bị đóng băng.");
            return;
        }

       // Debug.Log($"[TRAP DEBUG] {name}: Hút '{targetToPull.name}' về phía '{victim.name}'.");

        // Cho đối thủ chạy Coroutine bị hút về phía người đạp bẫy (victim)
        targetToPull.ApplyPulledByPlayer(victim.transform, pullForce, magnetDuration, magnetVFX, vfxOffset, vfxScale);
    }
}