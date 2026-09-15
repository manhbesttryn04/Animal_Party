using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Float Setting")]
    public float floatHeight = 0.3f;
    public float floatSpeed = 2f;

    [Header("Rotate Setting")]
    public float rotateSpeed = 20f;

    Vector3 startPos;

    void Start()
    {
        // Lưu vị trí ban đầu
        startPos = transform.position;
    }

    void Update()
    {
        // =========================
        // LƯ LỬNG
        // =========================

        float newY =
            startPos.y +
            Mathf.Sin(
                Time.time * floatSpeed
            ) * floatHeight;

        transform.position =
            new Vector3(
                startPos.x,
                newY,
                startPos.z
            );

        // =========================
        // XOAY NHẸ
        // =========================

        transform.Rotate(
            Vector3.up *
            rotateSpeed *
            Time.deltaTime
        );
    }
}