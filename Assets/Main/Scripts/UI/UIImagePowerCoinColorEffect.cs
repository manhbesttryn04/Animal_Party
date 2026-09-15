using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImagePowerCoinColorEffect : MonoBehaviour
{
    [Header("Color")]
    [SerializeField]
    private Color normalColor =
        new Color32(255, 255, 255, 255); // #FFFFFF

    [SerializeField]
    private Color effectColor =
        new Color32(255, 219, 219, 255); // #FFDBDB

    [Header("Animation")]
    [Tooltip("Thời gian hoàn thành một vòng: trắng → hồng → trắng")]
    [SerializeField] private float cycleDuration = 1.2f;

    [SerializeField]
    private AnimationCurve animationCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Scale Pulse")]
    [SerializeField] private bool useScaleEffect = true;

    [Tooltip("Mức phóng to tối đa")]
    [SerializeField] private float scaleMultiplier = 1.06f;

    private Image image;
    private RectTransform rectTransform;
    private Coroutine effectCoroutine;

    private Vector3 originalScale;

    private void Awake()
    {
        // Lấy Image và RectTransform của chính GameObject này
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        originalScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        originalScale = rectTransform.localScale;

        image.color = normalColor;
        rectTransform.localScale = originalScale;

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(PlayEffect());
    }

    private void OnDisable()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        if (image != null)
        {
            image.color = normalColor;
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
    }

    private IEnumerator PlayEffect()
    {
        float time = 0f;

        while (true)
        {
            time += Time.unscaledDeltaTime;

            // Giá trị chạy liên tục từ 0 → 1 → 0
            float wave = Mathf.Sin(
                time / Mathf.Max(0.01f, cycleDuration)
                * Mathf.PI * 2f
                - Mathf.PI / 2f
            );

            float progress = (wave + 1f) * 0.5f;
            progress = animationCurve.Evaluate(progress);

            // Chuyển màu
            image.color = Color.Lerp(
                normalColor,
                effectColor,
                progress
            );

            // Phóng to và thu nhỏ nhẹ
            if (useScaleEffect)
            {
                float scale = Mathf.Lerp(
                    1f,
                    scaleMultiplier,
                    progress
                );

                rectTransform.localScale = originalScale * scale;
            }

            yield return null;
        }
    }
}