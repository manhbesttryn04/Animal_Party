using UnityEngine;

// Gắn lên từng camera (cam1, cam2), RaceMiniGame sẽ tự gán target lúc StartMiniGame
public class SplitScreenCameraFollow : MonoBehaviour
{
    [Header("--- TARGET ---")]
    public Transform target;

    [Header("--- OFFSET & ĐỘ MƯỢT ---")]
    [Tooltip("Vị trí camera so với xe, tính theo local space của xe (x=ngang, y=cao, z=lùi ra sau)")]
    public Vector3 offset = new Vector3(0f, 4f, -6f);
    public float positionSmoothTime = 0.15f;
    public float rotationSmoothSpeed = 8f;

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        // SmoothDamp mượt hơn Lerp vì có tính tới vận tốc hiện tại, không bị "đuổi theo giật cục"
        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, positionSmoothTime);

        Vector3 lookPoint = target.position + Vector3.up * 1.5f;
        Quaternion desiredRot = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSmoothSpeed * Time.deltaTime);
    }
}