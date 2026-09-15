using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class VolumeManager : MonoBehaviour
{
    public static VolumeManager Instance;

    [Header("Volume")]
    public Volume volume;

    [Header("Default Bloom")]
    public float defaultBloomIntensity = 4f;

    [Header("Default Motion Blur")]
    public float defaultMotionBlurIntensity = 0.5f;

    [Header("Vignette")]
    public float vignetteTargetIntensity = 0.25f;
    public float vignetteMoveTime = 0.5f;
    [Header("Depth Of Field")]
    [SerializeField] private float defaultFocusDistance = 2.2f;
    [SerializeField] private float pauseFocusDistance = 0f;
    [SerializeField] private float depthMoveTime = 0.5f;

    private Coroutine depthRoutine;
    [Header("Current Graphics Quality")]
    [SerializeField]
    public int currentQuality = 2;

    private Bloom bloom;
    private MotionBlur motionBlur;
    private Vignette vignette;
    private Tonemapping tonemapping;
    private ColorAdjustments colorAdjustments;
    private DepthOfField depthOfField;

    private Coroutine vignetteRoutine;
    private Coroutine updateCameraRoutine;

    //==================================================
    // UNITY
    //==================================================

    private void Awake()
    {
        SetupSingleton();
        SetupVolumeProfile();
        SetupVolumeComponents();
    }


    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void SetupVolumeProfile()
    {
        if (volume == null) return;

        if (volume.sharedProfile != null)
        {
            // Tạo bản copy runtime để không chỉnh trực tiếp asset gốc
            volume.profile = Instantiate(volume.sharedProfile);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    //==================================================
    // SETUP VOLUME
    //==================================================

    private void SetupVolumeComponents()
    {
        if (volume == null || volume.profile == null)
        {
            return;
        }

        //==================================================
        // BLOOM
        //==================================================

        if (volume.profile.TryGet(out bloom))
        {
            bloom.intensity.overrideState = true;
            bloom.intensity.value =
                defaultBloomIntensity;

            bloom.active = true;
        }

        //==================================================
        // MOTION BLUR
        //==================================================

        if (volume.profile.TryGet(out motionBlur))
        {
            motionBlur.intensity.overrideState = true;
            motionBlur.intensity.value =
                defaultMotionBlurIntensity;

            motionBlur.active =
                defaultMotionBlurIntensity > 0f;
        }

        //==================================================
        // VIGNETTE
        //==================================================

        if (volume.profile.TryGet(out vignette))
        {
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0f;
            vignette.active = false;
        }

        //==================================================
        // TONEMAPPING
        //==================================================

        if (volume.profile.TryGet(out tonemapping))
        {
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value =
                TonemappingMode.ACES;

            tonemapping.active = true;
        }

        //==================================================
        // COLOR ADJUSTMENTS
        //==================================================

        if (volume.profile.TryGet(
                out colorAdjustments
            ))
        {
            colorAdjustments.active = true;
        }

        //==================================================
        // DEPTH OF FIELD
        //==================================================

        if (volume.profile.TryGet(out depthOfField))
        {
            depthOfField.active = true;

            depthOfField.focusDistance.overrideState = true;

            // Focus Distance bình thường luôn là 2.2.
            depthOfField.focusDistance.value =
                defaultFocusDistance;
        }
    }

    //==================================================
    // SCENE LOADED
    //==================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode loadMode
    )
    {
        if (updateCameraRoutine != null)
        {
            StopCoroutine(updateCameraRoutine);
        }

        updateCameraRoutine =
            StartCoroutine(UpdateCameraNextFrame());
    }

    private IEnumerator UpdateCameraNextFrame()
    {
        /*
         * Chờ camera của scene mới được tạo và Awake xong.
         */
        yield return null;

        UpdateAllCamera();

        updateCameraRoutine = null;
    }

    //==================================================
    // GRAPHICS QUALITY
    // 0 = Low
    // 1 = Medium
    // 2 = High
    //==================================================

    public void SetGraphicsQuality(int quality)
    {
       

        currentQuality = Mathf.Clamp(
            quality,
            0,
            2
        );

        switch (currentQuality)
        {
            //==================================================
            // LOW
            // Tắt Post Processing trên tất cả Camera
            //==================================================

            case 0:
                {
                    SetAllCameraPostProcessing(false);

                    break;
                }

            //==================================================
            // MEDIUM
            // Post Processing ON
            // Neutral
            // Motion Blur OFF
            //==================================================

            case 1:
                {
                    SetAllCameraPostProcessing(true);

                    SetTonemapping(
                        TonemappingMode.Neutral
                    );

                    SetBloomActive(true);
                    SetMotionBlurActive(true);
                    SetColorAdjustmentsActive(true);
                    SetDepthOfFieldActive(true);

                    break;
                }

            //==================================================
            // HIGH
            // Post Processing ON
            // ACES
            // Motion Blur ON
            //==================================================

            case 2:
                {
                    SetAllCameraPostProcessing(true);

                    SetTonemapping(
                        TonemappingMode.ACES
                    );

                    SetBloomActive(true);
                    SetMotionBlurActive(true);
                    SetColorAdjustmentsActive(true);
                    SetDepthOfFieldActive(true);

                 


                    break;
                }
        }
    }

    public int GetCurrentQuality()
    {
        return currentQuality;
    }

    //==================================================
    // UPDATE CAMERA
    //==================================================

    public void UpdateAllCamera()
    {
        /*
         * Low = tắt Post Processing.
         * Medium và High = bật Post Processing.
         */
        bool enablePostProcessing =
            currentQuality != 0;

        SetAllCameraPostProcessing(
            enablePostProcessing
        );

       
    }

    private void SetAllCameraPostProcessing(
        bool state
    )
    {
        Camera[] cameras =
            FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Camera cameraItem in cameras)
        {
            if (cameraItem == null)
            {
                continue;
            }

            UniversalAdditionalCameraData cameraData =
                cameraItem
                    .GetUniversalAdditionalCameraData();

            if (cameraData != null)
            {
                cameraData.renderPostProcessing =
                    state;
            }
        }
    }

    //==================================================
    // TONEMAPPING
    //==================================================

    private void SetTonemapping(
        TonemappingMode mode
    )
    {
        if (tonemapping == null)
        {
            return;
        }

        tonemapping.active = true;
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = mode;
    }

    //==================================================
    // BLOOM
    //==================================================

    private void SetBloomActive(bool state)
    {
        if (bloom == null)
        {
            return;
        }

        bloom.active = state;

        
    }
    public void ResetBloom()
    {
        if (bloom == null)
        {
            return;
        }
        bloom.intensity.overrideState = true;
        bloom.intensity.value =
            defaultBloomIntensity;
    }

    public void SetBloomIntensity(float intensity)
    {
        if (bloom == null)
        {
            return;
        }

        bloom.active = intensity > 0f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = intensity;
    }

    public float GetBloomIntensity()
    {
        if (bloom != null)
        {
            return bloom.intensity.value;
        }

        return 0f;
    }

    //==================================================
    // MOTION BLUR
    //==================================================

    private void SetMotionBlurActive(bool state)
    {
        if (motionBlur == null)
        {
            return;
        }

        motionBlur.active = state;

        if (state)
        {
            motionBlur.intensity.overrideState = true;
            motionBlur.intensity.value =
                defaultMotionBlurIntensity;
        }
    }

    public void SetMotionBlurIntensity(
        float intensity
    )
    {
        if (motionBlur == null)
        {
            return;
        }

        motionBlur.active = intensity > 0f;
        motionBlur.intensity.overrideState = true;
        motionBlur.intensity.value = intensity;
    }

    public void ResetMotionBlur()
    {
        if (motionBlur == null)
        {
            return;
        }

        motionBlur.active =
            defaultMotionBlurIntensity > 0f;

        motionBlur.intensity.overrideState = true;
        motionBlur.intensity.value =
            defaultMotionBlurIntensity;
    }

    //==================================================
    // COLOR ADJUSTMENTS
    //==================================================

    private void SetColorAdjustmentsActive(
        bool state
    )
    {
        if (colorAdjustments == null)
        {
            return;
        }

        colorAdjustments.active = state;
    }

    public void SetColorActive(bool state)
    {
        SetColorAdjustmentsActive(state);
    }

    //==================================================
    // DEPTH OF FIELD
    //==================================================

    //==================================================
    // DEPTH OF FIELD
    //==================================================

    private void SetDepthOfFieldActive(bool state)
    {
        if (depthOfField == null)
        {
            return;
        }

        depthOfField.active = state;
    }

    public void SetDepthActive(bool state)
    {
        SetDepthOfFieldActive(state);
    }

    public void StartDepthBlur()
    {
        if (depthOfField == null)
        {
            return;
        }

        if (depthRoutine != null)
        {
            StopCoroutine(depthRoutine);
        }

        depthRoutine = StartCoroutine(
            MoveDepthFocusDistance(
                depthOfField.focusDistance.value,
                pauseFocusDistance,
                depthMoveTime
            )
        );
    }

    public void ResetDepthBlur()
    {
        if (depthOfField == null)
        {
            return;
        }

        if (depthRoutine != null)
        {
            StopCoroutine(depthRoutine);
        }

        depthRoutine = StartCoroutine(
            MoveDepthFocusDistance(
                depthOfField.focusDistance.value,
                defaultFocusDistance,
                depthMoveTime
            )
        );
    }

    private IEnumerator MoveDepthFocusDistance(
        float start,
        float end,
        float duration
    )
    {
        if (depthOfField == null)
        {
            yield break;
        }

        depthOfField.active = true;
        depthOfField.focusDistance.overrideState = true;

        if (duration <= 0f)
        {
            depthOfField.focusDistance.value = end;
            depthRoutine = null;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            // Không bị ảnh hưởng bởi Time.timeScale = 0
            time += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(time / duration);

            // SmoothStep giúp chuyển mờ mềm hơn
            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            depthOfField.focusDistance.value =
                Mathf.Lerp(
                    start,
                    end,
                    progress
                );

            yield return null;
        }

        depthOfField.focusDistance.value = end;
        depthRoutine = null;
    }

    //==================================================
    // VIGNETTE
    //==================================================

    public void StartVignette()
    {
        if (vignette == null)
        {
            return;
        }

        if (vignetteRoutine != null)
        {
            StopCoroutine(vignetteRoutine);
        }

        vignetteRoutine = StartCoroutine(
            MoveVignette(
                vignette.intensity.value,
                vignetteTargetIntensity,
                vignetteMoveTime
            )
        );
    }

    public void ResetVignette()
    {
        if (vignette == null)
        {
            return;
        }

        if (vignetteRoutine != null)
        {
            StopCoroutine(vignetteRoutine);
        }

        vignetteRoutine = StartCoroutine(
            MoveVignette(
                vignette.intensity.value,
                0f,
                vignetteMoveTime
            )
        );
    }

    public IEnumerator MoveVignette(
        float start,
        float end,
        float duration
    )
    {
        if (vignette == null)
        {
            yield break;
        }

        vignette.active = true;
        vignette.intensity.overrideState = true;

        if (duration <= 0f)
        {
            vignette.intensity.value = end;
            vignette.active = end > 0f;
            vignetteRoutine = null;

            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(time / duration);

            vignette.intensity.value =
                Mathf.Lerp(
                    start,
                    end,
                    progress
                );

            yield return null;
        }

        vignette.intensity.value = end;

        if (end <= 0f)
        {
            vignette.active = false;
        }

        vignetteRoutine = null;
    }

}