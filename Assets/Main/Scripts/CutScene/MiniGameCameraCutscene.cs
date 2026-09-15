using System.Collections;
using UnityEngine;

public class MiniGameCameraCutscene : MonoBehaviour
{
    public Camera cam;

    [Header("Cutscene Points")]
    public Transform[] cameraPoints;

    [Header("Default Gameplay Point")]
    public Transform defaultPoint;

    [Header("Move Setting")]
    public float moveSpeed = 3f;
    public float rotateSpeed = 240f;

    [Header("Wait Time")]
    public float waitAtPoint = 2f;

    public IEnumerator PlayCutscene()
    {
        if (cam == null) yield break;
        if (defaultPoint == null) yield break;

        for (int i = 0; i < cameraPoints.Length; i++)
        {
            if (cameraPoints[i] == null) continue;

            yield return StartCoroutine(
                MoveCamera(cameraPoints[i])
            );

            yield return new WaitForSeconds(waitAtPoint);
        }

        yield return StartCoroutine(
            MoveCamera(defaultPoint)
        );
    }

    IEnumerator MoveCamera(Transform target)
    {
        if (target == null) yield break;

        while (
            Vector3.Distance(cam.transform.position, target.position) > 0.05f ||
            Quaternion.Angle(cam.transform.rotation, target.rotation) > 0.5f
        )
        {
            cam.transform.position =
                Vector3.MoveTowards(
                    cam.transform.position,
                    target.position,
                    moveSpeed * Time.deltaTime
                );

            cam.transform.rotation =
                Quaternion.RotateTowards(
                    cam.transform.rotation,
                    target.rotation,
                    rotateSpeed * Time.deltaTime
                );

            yield return null;
        }

        cam.transform.position = target.position;
        cam.transform.rotation = target.rotation;
    }
}