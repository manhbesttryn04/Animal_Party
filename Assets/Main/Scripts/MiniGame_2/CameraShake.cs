using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        originalPos = transform.localPosition;
    }

    public void Shake(float duration, float strength)
    {
        if (shakeRoutine != null)
        {
            StopShake();
        }

        originalPos = transform.localPosition;

        if (duration <= 0f || strength <= 0f)
        {
            transform.localPosition = originalPos;
            return;
        }

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, strength));
    }

    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float timer = 0f;

        while (timer < duration)
        {
            // Khi game pause thì đưa camera về vị trí cũ và chờ
            if (Time.timeScale == 0f)
            {
                transform.localPosition = originalPos;
                yield return null;
                continue;
            }

            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;

            transform.localPosition =
                originalPos + new Vector3(x, y, 0f);

            timer += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        shakeRoutine = null;
    }

    public void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        transform.localPosition = originalPos;
    }

    private void OnDisable()
    {
        StopShake();
    }
}