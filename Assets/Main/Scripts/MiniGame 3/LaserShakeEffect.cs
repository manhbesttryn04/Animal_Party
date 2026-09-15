using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserShakeEffect : MonoBehaviour
{
    [Header("--- Laser Shake Effect ---")]
    public bool isActive = false;

    public int laserSegments = 25;
    public float shakeAmount = 0.08f;
    public float shakeSpeed = 35f;

    [Header("--- Laser Raycast ---")]
    public float maxLaserDistance = 50f;
    public LayerMask obstacleLayer;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (!isActive) return;

        UpdateShakeLaser();
    }

    private void UpdateShakeLaser()
    {
        Vector3 startPoint = transform.position;
        Vector3 endPoint;

        if (Physics.Raycast(
                transform.position,
                transform.forward,
                out RaycastHit hit,
                maxLaserDistance,
                obstacleLayer))
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = transform.position + transform.forward * maxLaserDistance;
        }

        DrawShakingLaser(startPoint, endPoint);
    }

    private void DrawShakingLaser(Vector3 startPoint, Vector3 endPoint)
    {
        line.positionCount = laserSegments;

        Vector3 direction = (endPoint - startPoint).normalized;

        Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;

        if (side == Vector3.zero)
            side = Vector3.right;

        for (int i = 0; i < laserSegments; i++)
        {
            float t = (float)i / (laserSegments - 1);

            Vector3 point = Vector3.Lerp(startPoint, endPoint, t);

            // Không rung ở đầu và cuối tia
            if (i != 0 && i != laserSegments - 1)
            {
                float noise = Mathf.PerlinNoise(
                    i * 0.3f,
                    Time.time * shakeSpeed
                );

                float offset = (noise - 0.5f) * 2f * shakeAmount;

                point += side * offset;
            }

            line.SetPosition(i, point);
        }
    }

    public void SetLaserActive(bool active)
    {
        isActive = active;

        if (!active)
        {
            line.positionCount = 2;
        }
    }
}