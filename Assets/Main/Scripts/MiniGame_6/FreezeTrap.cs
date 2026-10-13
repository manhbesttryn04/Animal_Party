using UnityEngine;

public class FreezeTrap : TrapBase
{
    [Header("--- FREEZE TRAP ---")]
    public float freezeDuration = 2f;

    [Header("--- VFX SETTINGS ---")]
    [Tooltip("VFX nổ 1 phát rồi thôi (Instant/Hit)")]
    public GameObject freezeHitVFX;

    [Tooltip("VFX duy trì bám theo player trong suốt thời gian bị đóng băng (Loop)")]
    public GameObject freezeLoopVFX;

    public Vector3 vfxOffset = new Vector3(0, 1f, 0);
    public Vector3 vfxScale = new Vector3(1f, 1f, 1f);

    protected override void OnPlayerHit(BombCarrier carrier)
    {
        if (carrier.IsFrozen())
        {
           // Debug.Log($"[TRAP DEBUG] {name}: KHÔNG áp dụng Freeze vì '{carrier.name}' đã đang bị đóng băng.");
            return;
        }

       // Debug.Log($"[TRAP DEBUG] {name}: Áp dụng Freeze lên '{carrier.name}', duration = {freezeDuration}s.");

        // Truyền cả 2 VFX sang cho BombCarrier xử lý
        carrier.ApplyFreeze(freezeDuration, freezeHitVFX, freezeLoopVFX, vfxOffset, vfxScale);
    }
}