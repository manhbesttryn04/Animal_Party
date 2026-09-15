using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Follow Points")]
    public Transform transFollow;
    public Transform transFollow2;
    public Transform transFollow3;
    public Transform transFollow4;

    [Header("References")]
    public GameObject player;
    public Camera cameraMain;

    [Header("Settings")]
    public float smoothTime = 0.3f;

    private Vector3 velocity;

    public bool isFollow = false;
    public bool isFllow2 = false;
    public bool isFllow3 = false;
    private void Start()
    {
        SetupCamera();
    }

    private void SetupCamera()
    {
        cameraMain = Camera.main;
        player = gameObject;
    }

    private void LateUpdate()
    {
        HandleCameraFollow();
    }

    private void HandleCameraFollow()
    {
        Transform target = null;

        if (isFollow) target = transFollow;
        else if (isFllow2) target = transFollow2;
        else if (isFllow3) target = transFollow3;

        if (target == null) return;

        // Di chuyển camera
        cameraMain.transform.position = Vector3.SmoothDamp(
            cameraMain.transform.position,
            target.position,
            ref velocity,
            smoothTime
        );

        // Xoay camera
        if (isFollow)
        {
            var moveAI = player.GetComponent<PlayerMoveAI>();
            Transform targetPoint = moveAI.pointCheck[moveAI.currentIndex].transform;

            Vector3 lookPos = targetPoint.position;
            lookPos.y += 4f;

            Quaternion targetRot = Quaternion.LookRotation(lookPos - cameraMain.transform.position);
            cameraMain.transform.rotation = Quaternion.Slerp(
                cameraMain.transform.rotation,
                targetRot,
                100f * Time.deltaTime
            );
        }
        else
        {
            cameraMain.transform.LookAt(player.transform);
        }
    }

    public void SetCamera1()
    {
        isFollow = true;
        isFllow2 = false;
        isFllow3 = false;
    }

    public void SetCamera2()
    {
        isFollow = false;
        isFllow2 = true;
        isFllow3 = false;
    }

    public void SetCamera3()
    {
        isFollow = false;
        isFllow2 = false;
        isFllow3 = true;
    }

    public void StopFollow()
    {
        isFollow = false;
        isFllow2 = false;
        isFllow3 = false;
    }
}