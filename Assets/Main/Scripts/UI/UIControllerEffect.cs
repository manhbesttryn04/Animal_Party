using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIControllerEffect : MonoBehaviour
{
    public enum EffectState
    {
        Open,
        Close
    }

    [Header("Effect State")]
    [SerializeField] private EffectState effectState = EffectState.Open;

    [Header("Effect Switches")]
    [Tooltip("Bật hiệu ứng mở khi GameObject được Enable.")]
    [SerializeField] private bool enableOpenEffect = true;

    [Tooltip("Bật hiệu ứng đóng khi GameObject được Enable.")]
    [SerializeField] private bool enableCloseEffect = false;

    [Header("UI References")]
    [SerializeField] private Image targetImage;

    [Tooltip("Sprite màu xám.")]
    [SerializeField] private Sprite graySprite;

    [Tooltip("Sprite màu vàng.")]
    [SerializeField] private Sprite yellowSprite;

    [Header("Open Effect")]
    [Min(0.01f)]
    [SerializeField] private float openScaleTime = 0.25f;

    [Min(0f)]
    [SerializeField] private float openGrayHoldTime = 0.1f;

    [Min(0f)]
    [SerializeField] private float openYellowHoldTime = 0.35f;

    [Header("Close Effect")]
    [Min(0f)]
    [SerializeField] private float closeYellowHoldTime = 0.2f;

    [Min(0f)]
    [SerializeField] private float closeGrayHoldTime = 0.15f;

    [Min(0.01f)]
    [SerializeField] private float closeScaleTime = 0.25f;

    [Header("Scale")]
    [Tooltip("Kích thước bắt đầu của hiệu ứng Open.")]
    [SerializeField] private Vector3 hiddenScale = Vector3.zero;

    [Tooltip("Có tự lưu kích thước ban đầu của UI không?")]
    [SerializeField] private bool useInitialScale = true;

    [Tooltip("Kích thước cuối nếu không dùng kích thước ban đầu.")]
    [SerializeField] private Vector3 normalScale = Vector3.one;

    [Header("End")]
    [Tooltip("Tắt GameObject sau khi hiệu ứng hoàn thành.")]
    [SerializeField] private bool disableAfterEffect = true;

    private Vector3 originalScale;
    private Coroutine effectCoroutine;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        originalScale = transform.localScale;

        if (useInitialScale)
        {
            normalScale = originalScale;
        }
    }

    private void OnEnable()
    {
        if (targetImage == null)
        {
   
            return;
        }

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(PlaySelectedEffect());
    }

    private void OnDisable()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }
    }

    private IEnumerator PlaySelectedEffect()
    {
        SetImageVisible(true);

        if (effectState == EffectState.Open && enableOpenEffect)
        {
            yield return OpenRoutine();
        }
        else if (effectState == EffectState.Close && enableCloseEffect)
        {
            yield return CloseRoutine();
        }
        else if (enableOpenEffect && !enableCloseEffect)
        {
            yield return OpenRoutine();
        }
        else if (enableCloseEffect && !enableOpenEffect)
        {
            yield return CloseRoutine();
        }
        else
        {
            effectCoroutine = null;
            yield break;
        }

        effectCoroutine = null;

        if (disableAfterEffect)
        {
            gameObject.SetActive(false);
        }
        else
        {
            SetImageVisible(false);
        }
    }

    // =========================================================
    // OPEN
    // Xám + scale 0 → scale gốc → vàng → biến mất
    // =========================================================

    private IEnumerator OpenRoutine()
    {
        SetSprite(graySprite);

        transform.localScale = hiddenScale;

        if (openGrayHoldTime > 0f)
        {
            yield return WaitRealtime(openGrayHoldTime);
        }

        yield return ScaleRoutine(
            hiddenScale,
            normalScale,
            openScaleTime
        );

        transform.localScale = normalScale;

        SetSprite(yellowSprite);

        if (openYellowHoldTime > 0f)
        {
            yield return WaitRealtime(openYellowHoldTime);
        }

        SetImageVisible(false);
    }

    // =========================================================
    // CLOSE
    // Vàng → xám → thu nhỏ → biến mất
    // =========================================================

    private IEnumerator CloseRoutine()
    {
        transform.localScale = normalScale;

        SetSprite(yellowSprite);

        if (closeYellowHoldTime > 0f)
        {
            yield return WaitRealtime(closeYellowHoldTime);
        }

        SetSprite(graySprite);

        if (closeGrayHoldTime > 0f)
        {
            yield return WaitRealtime(closeGrayHoldTime);
        }

        yield return ScaleRoutine(
            normalScale,
            hiddenScale,
            closeScaleTime
        );

        transform.localScale = hiddenScale;

        SetImageVisible(false);
    }

    // =========================================================
    // SCALE
    // =========================================================

    private IEnumerator ScaleRoutine(
        Vector3 startScale,
        Vector3 endScale,
        float duration)
    {
        if (duration <= 0f)
        {
            transform.localScale = endScale;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                timer / duration
            );

            // SmoothStep giúp hiệu ứng mượt hơn.
            float smoothProgress =
                progress * progress * (3f - 2f * progress);

            transform.localScale = Vector3.LerpUnclamped(
                startScale,
                endScale,
                smoothProgress
            );

            yield return null;
        }

        transform.localScale = endScale;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void SetSprite(Sprite newSprite)
    {
        if (targetImage == null || newSprite == null)
            return;

        targetImage.sprite = newSprite;
    }

    private void SetImageVisible(bool isVisible)
    {
        if (targetImage == null)
            return;

        Color imageColor = targetImage.color;
        imageColor.a = isVisible ? 1f : 0f;
        targetImage.color = imageColor;
    }

    private IEnumerator WaitRealtime(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // =========================================================
    // PUBLIC
    // =========================================================

    public void SetOpenEffect()
    {
        effectState = EffectState.Open;

        enableOpenEffect = true;
        enableCloseEffect = false;
    }

    public void SetCloseEffect()
    {
        effectState = EffectState.Close;

        enableOpenEffect = false;
        enableCloseEffect = true;
    }

    public void PlayOpen()
    {
        SetOpenEffect();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        RestartEffect();
    }

    public void PlayClose()
    {
        SetCloseEffect();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        RestartEffect();
    }

    private void RestartEffect()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(
            PlaySelectedEffect()
        );
    }
}