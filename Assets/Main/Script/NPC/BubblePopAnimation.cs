using UnityEngine;
using System.Collections;

public class BubblePopAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public float popInDuration = 0.15f;
    public float popOutDuration = 0.15f;
    public float displayDuration = 2f;
    public bool autoDestroy = true;

    private Vector3 targetScale;
    private bool isStarted = false;

    public void Init(Vector3 finalScale)
    {
        SetupAnimation(finalScale);
    }

    private void Start()
    {
        if (!isStarted)
        {
            SetupAnimation(transform.localScale);
        }
    }

    private void SetupAnimation(Vector3 finalScale)
    {
        targetScale = finalScale;
        transform.localScale = Vector3.zero;

        if (!isStarted)
        {
            isStarted = true;
            StartCoroutine(PlayAnimation());
        }
    }

    private IEnumerator PlayAnimation()
    {
        yield return StartCoroutine(ScaleTo(Vector3.zero, targetScale, popInDuration));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(ScaleTo(targetScale, Vector3.zero, popOutDuration));

        if (autoDestroy)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ScaleTo(Vector3 from, Vector3 to, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;
            float eased = EaseOutBack(progress);
            transform.localScale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }
        transform.localScale = to;
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
    }
}
