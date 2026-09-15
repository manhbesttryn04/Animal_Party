using System.Collections;
using UnityEngine;

public class ColorPad : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private BoxCollider boxCollider;
    private Rigidbody rb;

    [HideInInspector] public Color currentColor;
    [HideInInspector] public bool isSafe = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private Coroutine resetCoroutine;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        boxCollider = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();

        startPosition = transform.position;
        startRotation = transform.rotation;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = true;
        }
    }

    public void SetPadColor(Color color)
    {
        currentColor = color;

        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
        }
    }

    public void CheckSurvival()
    {
        if (!isSafe)
        {
            Fall();
        }
    }

    private void Fall()
    {
        if (rb == null)
            return;

        rb.isKinematic = false;

        Vector3 force =
            Vector3.down * 5f +
            new Vector3(
                Random.Range(-2f, 2f),
                0,
                Random.Range(-2f, 2f)
            );

        rb.AddForce(force, ForceMode.Impulse);

        rb.AddTorque(
            Random.insideUnitSphere * 20f,
            ForceMode.Impulse
        );
    }

    public void ResetPad()
    {
        isSafe = false;

        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }

        resetCoroutine =
            StartCoroutine(
                ResetAnimation()
            );
    }

    IEnumerator ResetAnimation()
    {
        if (rb != null)
        {
            rb.isKinematic = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Vector3 currentPosition =
            transform.position;

        Quaternion currentRotation =
            transform.rotation;

        float duration = 1f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    timer / duration
                );

            transform.position =
                Vector3.Lerp(
                    currentPosition,
                    startPosition,
                    t
                );

            transform.rotation =
                Quaternion.Slerp(
                    currentRotation,
                    startRotation,
                    t
                );

            yield return null;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;

        SetPadColor(Color.gray3);

        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }

        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
    }
}