using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Cursor References")]
    [SerializeField] private RectTransform cursorRect;
    [SerializeField] private Image cursorImage;

    [Header("Cursor Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Cursor Settings")]
    [SerializeField] private Vector2 cursorOffset = Vector2.zero;
    [SerializeField] private bool keepBetweenScenes = true;
    [SerializeField] private bool hideSystemCursor = true;

    [Header("Cursor State")]
    [Tooltip("Scene hiện tại có cho phép hiện chuột hay không. Main Menu = true, Cutscene = false.")]
    [SerializeField] private bool sceneAllowsCursor = true;

    [Tooltip("Setting đang mở và cần chuột khi không còn tay cầm.")]
    [SerializeField] private bool settingForcesCursor;

    [SerializeField] private bool isGameCursorVisible;

    private Coroutine restoreCursorCoroutine;

    private void Awake()
    {
        SetupSingleton();
        SetupCursorReferences();
        SetupCursorDefaults();
    }

    private void SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (keepBetweenScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void SetupCursorReferences()
    {
        FindReferences();
    }

    private void SetupCursorDefaults()
    {
        Cursor.lockState = CursorLockMode.None;

        if (cursorImage != null && normalSprite != null)
        {
            cursorImage.sprite = normalSprite;
        }
    }


    private void Start()
    {
        ApplyCursorState();
    }

    private void Update()
    {
        if (!isGameCursorVisible)
            return;

        SyncCursorPosition();
        UpdatePressedSprite();
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (cursorRect == null)
        {
            cursorRect = GetComponentInChildren<RectTransform>(true);
        }

        if (cursorImage == null)
        {
            cursorImage = GetComponentInChildren<Image>(true);
        }
    }

    // =========================================================
    // FINAL CURSOR LOGIC
    // =========================================================

    private void ApplyCursorState()
    {
        bool hasController =
            ControllerManager.Instance != null &&
            ControllerManager.Instance.HasAnyController();

        /*
         * Chuột chỉ hiện khi:
         * 1. Không có tay cầm kết nối.
         * 2. Scene cho phép chuột HOẶC Setting đang cần chuột.
         *
         * Ví dụ Cutscene:
         * sceneAllowsCursor = false.
         * Rút tay cầm nhưng Setting chưa mở => chuột vẫn ẩn.
         * Setting đang mở và rút hết tay cầm => chuột hiện.
         */
        bool shouldShowCursor =
            !hasController &&
            (sceneAllowsCursor || settingForcesCursor);

        if (shouldShowCursor)
        {
            ShowGameCursorInternal();
        }
        else
        {
            HideGameCursorInternal();
        }
    }

    /// <summary>
    /// ControllerManager gọi hàm này khi trạng thái tay cầm thay đổi.
    /// </summary>
    public void UpdateCursorByControllerState()
    {
        ApplyCursorState();
    }

    // =========================================================
    // SCENE CURSOR STATE
    // =========================================================

    /// <summary>
    /// Đặt trạng thái chuột mặc định của scene.
    /// Main Menu / màn chọn nhân vật thường dùng true.
    /// Cutscene / gameplay không cần chuột thường dùng false.
    /// </summary>
    public void SetSceneCursorVisible(bool visible)
    {
        sceneAllowsCursor = visible;
        ApplyCursorState();
    }

    public bool DoesSceneAllowCursor()
    {
        return sceneAllowsCursor;
    }

    // =========================================================
    // SETTING CURSOR STATE
    // =========================================================

    /// <summary>
    /// Gọi true khi Setting mở.
    /// Gọi false khi Setting đóng.
    /// Nếu còn tay cầm thì chuột vẫn ẩn.
    /// Nếu rút hết tay cầm khi Setting đang mở thì chuột tự hiện.
    /// </summary>
    public void SetSettingCursorActive(bool active)
    {
        settingForcesCursor = active;
        ApplyCursorState();
    }

    public bool IsSettingCursorActive()
    {
        return settingForcesCursor;
    }

    // =========================================================
    // CURSOR UPDATE
    // =========================================================

    private void SyncCursorPosition()
    {
        if (cursorRect == null)
            return;

        cursorRect.position =
            (Vector2)Input.mousePosition +
            cursorOffset;
    }

    private void UpdatePressedSprite()
    {
        if (cursorImage == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (pressedSprite != null)
            {
                cursorImage.sprite = pressedSprite;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (normalSprite != null)
            {
                cursorImage.sprite = normalSprite;
            }
        }
    }

    // =========================================================
    // APPLICATION FOCUS
    // =========================================================

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ShowSystemCursor();
            return;
        }

        if (restoreCursorCoroutine != null)
        {
            StopCoroutine(restoreCursorCoroutine);
        }

        restoreCursorCoroutine =
            StartCoroutine(RestoreGameCursorRoutine());
    }

    private IEnumerator RestoreGameCursorRoutine()
    {
        if (cursorImage != null)
        {
            cursorImage.enabled = false;
        }

        yield return null;

        Cursor.lockState = CursorLockMode.None;

        SyncCursorPosition();

        yield return null;

        ApplyCursorState();

        restoreCursorCoroutine = null;
    }

    // =========================================================
    // PUBLIC SHOW / HIDE
    // =========================================================

    /// <summary>
    /// Scene yêu cầu ẩn chuột.
    /// Không dùng hàm này để đóng Setting; đóng Setting hãy dùng
    /// SetSettingCursorActive(false).
    /// </summary>
    public void HideGameCursor()
    {
        sceneAllowsCursor = false;
        ApplyCursorState();
    }

    /// <summary>
    /// Scene cho phép hiện chuột.
    /// Nếu đang có tay cầm thì chuột vẫn được giữ ẩn.
    /// </summary>
    public void ShowGameCursor()
    {
        sceneAllowsCursor = true;
        ApplyCursorState();
    }

    private void HideGameCursorInternal()
    {
        isGameCursorVisible = false;

        if (cursorImage != null)
        {
            cursorImage.enabled = false;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void ShowGameCursorInternal()
    {
        isGameCursorVisible = true;

        Cursor.lockState = CursorLockMode.None;

        // Nếu dùng cursor UI riêng thì ẩn cursor Windows.
        Cursor.visible = !hideSystemCursor;

        if (cursorImage != null)
        {
            cursorImage.enabled = true;
        }

        SyncCursorPosition();
    }

    /// <summary>
    /// Chỉ dùng khi game mất focus hoặc cần chuột Windows thật.
    /// Hàm này không thay đổi trạng thái scene/setting đã lưu.
    /// </summary>
    public void ShowSystemCursor()
    {
        isGameCursorVisible = false;

        if (cursorImage != null)
        {
            cursorImage.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =========================================================
    // CURSOR SETTINGS
    // =========================================================

    public void SetCursorSprite(Sprite newSprite)
    {
        if (cursorImage == null || newSprite == null)
            return;

        cursorImage.sprite = newSprite;
    }

    public void ResetCursorSprite()
    {
        if (cursorImage == null || normalSprite == null)
            return;

        cursorImage.sprite = normalSprite;
    }

    public void SetCursorOffset(Vector2 newOffset)
    {
        cursorOffset = newOffset;
    }

    public void SetCursorSize(Vector2 newSize)
    {
        if (cursorRect == null)
            return;

        cursorRect.sizeDelta = newSize;
    }

    public bool IsGameCursorVisible()
    {
        return isGameCursorVisible;
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnApplicationQuit()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}