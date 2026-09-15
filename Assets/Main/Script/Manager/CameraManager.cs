using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    private Camera cam;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0, 5f, -8f);

    private void Awake()
    {
        SetupSingleton();
        SetupCamera();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // giữ lại khi đổi scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupCamera()
    {
        cam = Camera.main;
    }

    // =========================
    // MOVE TO TARGET (CALL ONLY)
    // =========================
    public IEnumerator MoveToTarget(Transform target, float duration)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.moveCamera);
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        Vector3 endPos = target.position + offset;
        Quaternion endRot = Quaternion.LookRotation(target.position - endPos);

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            cam.transform.position =
                Vector3.Lerp(startPos, endPos, lerp);

            cam.transform.rotation =
                Quaternion.Slerp(startRot, endRot, lerp);

            yield return null;
        }

        cam.transform.position = endPos;
        cam.transform.rotation = endRot;
    }

    // =========================
    // FLY UP AT CURRENT POSITION
    // =========================
    public IEnumerator FlyUp(float height, float duration)
    {
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.moveCamera);
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        Vector3 endPos = startPos + Vector3.up * height;
        Quaternion endRot = Quaternion.Euler(90f, 0f, 0f);

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            cam.transform.position =
                Vector3.Lerp(startPos, endPos, lerp);

            cam.transform.rotation =
                Quaternion.Slerp(startRot, endRot, lerp);

            yield return null;
        }

        cam.transform.position = endPos;
        cam.transform.rotation = endRot;
    }

    // =========================
    // SNAP BACK (instant follow reset)
    // =========================
    public void SnapToPosition(Vector3 pos, Quaternion rot)
    {
        cam.transform.position = pos;
        cam.transform.rotation = rot;
    }
    // =========================
    // MOVE TO POINT
    // Camera bay đến Transform có sẵn
    // =========================
    public IEnumerator MoveToPoint(Transform point, float duration)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.moveCamera);

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        Vector3 endPos = point.position;
        Quaternion endRot = point.rotation;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            cam.transform.position = Vector3.Lerp(startPos, endPos, lerp);
            cam.transform.rotation = Quaternion.Slerp(startRot, endRot, lerp);

            yield return null;
        }

        cam.transform.position = endPos;
        cam.transform.rotation = endRot;
    }
    // =========================
    // TELEPORT TO POINT
    // Dịch chuyển tức thì đến Transform
    // =========================
    public void TeleportToPoint(Transform point)
    {
        cam.transform.position = point.position;
        cam.transform.rotation = point.rotation;
    }
    public IEnumerator MoveToPosition(Vector3 position, Quaternion rotation, float duration)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.moveCamera);

        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = t / duration;

            cam.transform.position = Vector3.Lerp(startPos, position, lerp);
            cam.transform.rotation = Quaternion.Slerp(startRot, rotation, lerp);

            yield return null;
        }

        cam.transform.position = position;
        cam.transform.rotation = rotation;
    }
}