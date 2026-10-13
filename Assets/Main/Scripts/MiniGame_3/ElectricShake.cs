using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ElectricLineShakeLocal : MonoBehaviour
{
    public bool isShaking = true;

    [Header("Shake")]
    public int segmentCount = 25;
    public float shakeAmount = 0.15f;
    public float shakeSpeed = 30f;

    private LineRenderer line;

    private Vector3 originalStart;
    private Vector3 originalEnd;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();

        line.useWorldSpace = false;
    }

    private void Start()
    {
        if (line.positionCount >= 2)
        {
            originalStart = line.transform.TransformPoint(line.GetPosition(0));
            originalEnd = line.transform.TransformPoint(line.GetPosition(1));
        }
    }

    private void Update()
    {
        if (!isShaking)
            return;

        DrawElectricLine();
    }

    private void DrawElectricLine()
    {
        line.positionCount = segmentCount;

        Vector3 direction =
            (originalEnd - originalStart).normalized;

        Vector3 side =
            Vector3.Cross(direction, Vector3.forward).normalized;

        if (side == Vector3.zero)
            side = Vector3.up;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);

            Vector3 point =
                Vector3.Lerp(originalStart, originalEnd, t);

            if (i != 0 && i != segmentCount - 1)
            {
                float noise =
                    Mathf.PerlinNoise(
                        i * 0.25f,
                        Time.time * shakeSpeed);

                float offset =
                    (noise - 0.5f) * 2f * shakeAmount;

                point += side * offset;
            }

            line.SetPosition(i, point);
        }
    }

    public void SetShake(bool active)
    {
        isShaking = active;

        if (!active)
        {
            line.positionCount = 2;
            line.SetPosition(0, originalStart);
            line.SetPosition(1, originalEnd);
        }
    }
}