using System.Collections;
using UnityEngine;

public class TrapRow : MonoBehaviour
{
    public MiniGame1 mini1;

    [Header("Objects")]
    public GameObject[] skulls;
    public GameObject[] bricks;
    public Transform[] sharks;

    [Header("Shark Setting")]
    public float sharkUpHeight = 3f;
    public float sharkSpeed = 5f;

    [Header("Time Setting")]
    public float warningTime = 2f;
    public float sharkStayTime = 2f;

    // KIỂM TRA HÀNG ĐANG CHẠY HAY KHÔNG
    [HideInInspector]
    public bool isRunning = false;

    private Vector3[] sharkStartPos;
    private Vector3[] brickStartScales;
    private Animator[] sharkAnimators;
    private bool isInitialized;

    private void Awake()
    {
        InitializeRow();
    }

    private void InitializeRow()
    {
        if (isInitialized)
            return;

        mini1 = GetComponentInParent<MiniGame1>();

        if (sharks == null)
            sharks = new Transform[0];

        if (skulls == null)
            skulls = new GameObject[0];

        if (bricks == null)
            bricks = new GameObject[0];

        // Lưu vị trí ban đầu cá mập
        sharkStartPos = new Vector3[sharks.Length];

        // Mảng animator
        sharkAnimators = new Animator[sharks.Length];

        for (int i = 0; i < sharks.Length; i++)
        {
            if (sharks[i] == null)
                continue;

            sharkStartPos[i] = sharks[i].position;

            // Lấy animator
            sharkAnimators[i] =
                sharks[i].GetComponent<Animator>();

            // Ẩn cá mập lúc đầu
            sharks[i].gameObject.SetActive(false);
        }

        // Ẩn đầu lâu lúc đầu
        foreach (GameObject skull in skulls)
        {
            if (skull != null)
                skull.SetActive(false);
        }

        brickStartScales = new Vector3[bricks.Length];

        for (int i = 0; i < bricks.Length; i++)
        {
            brickStartScales[i] = bricks[i] != null
                ? bricks[i].transform.localScale
                : Vector3.one;
        }

        isInitialized = true;
    }

    public void StartRow()
    {
        InitializeRow();

        if (isRunning || !isActiveAndEnabled)
            return;

        StartCoroutine(RowRoutine());
    }

    public IEnumerator RowRoutine()
    {
        // NẾU ĐANG CHẠY THÌ KHÔNG CHẠY TIẾP
        if (isRunning)
            yield break;

        isRunning = true;


        yield return new WaitForSeconds(warningTime);


        // =========================
        // HIỆN CÁ MẬP + ATTACK
        // =========================
        for (int i = 0; i < sharks.Length; i++)
        {
            if (sharks[i] == null)
                continue;

            sharks[i].gameObject.SetActive(true);

            if (sharkAnimators[i] != null)
            {
                sharkAnimators[i].SetTrigger("Attack");
            }
        }

        // =========================
        // CÁ MẬP BAY LÊN
        // =========================
        yield return StartCoroutine(MoveSharks(true));

        // =========================
        // ẨN GẠCH
        // =========================
        foreach (GameObject brick in bricks)
        {
            if (brick == null)
                continue;

            MeshRenderer mesh =
                brick.GetComponent<MeshRenderer>();

            if (mesh != null)
                mesh.enabled = false;

            Collider col =
                brick.GetComponent<Collider>();

            if (col != null)
                col.enabled = false;
        }

        // =========================
        // CHỜ
        // =========================
        yield return new WaitForSeconds(sharkStayTime);

        // =========================
        // CÁ MẬP BAY XUỐNG
        // =========================
        yield return StartCoroutine(MoveSharks(false));

        // =========================
        // ẨN CÁ MẬP
        // =========================
        foreach (Transform shark in sharks)
        {
            if (shark != null)
                shark.gameObject.SetActive(false);
        }

        // =========================
        // HIỆN LẠI GẠCH
        // =========================
        yield return StartCoroutine(
     RestoreBricks()
 );

        // XONG
        isRunning = false;
    }
    IEnumerator RestoreBricks()
    {
        yield return new WaitForSeconds(1f);
        // Hồi từng cục
        for (int i = 0; i < bricks.Length; i++)
        {
            if (bricks[i] == null)
                continue;

            StartCoroutine(
                RestoreSingleBrick(
                    bricks[i],
                    brickStartScales[i]
                )

            );

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(
                    AudioManager.Instance.loadBrickClip
                );
            }

            // Delay giữa từng cục
            yield return new WaitForSeconds(0.18f);
        }

        // Coroutine hồi viên gạch cuối cần 0.3 giây nhưng vòng lặp
        // mới chờ 0.18 giây. Chờ phần còn lại trước khi kết thúc row.
        yield return new WaitForSeconds(0.12f);
    }
    IEnumerator RestoreSingleBrick(
        GameObject brick,
        Vector3 targetScale
    )
    {
        float time = 0;
        float duration = 0.3f;

        // BẬT COLLIDER
        Collider col =
            brick.GetComponent<Collider>();
        MeshRenderer mes = brick.GetComponent<MeshRenderer>();
        if (mes != null) { mes.enabled = true; }

        if (col != null)
            col.enabled = true;

        // SCALE TỪ 0
        brick.transform.localScale =
            Vector3.zero;

        while (time < 1)
        {
            time += Time.deltaTime / duration;

            float smoothTime =
                Mathf.SmoothStep(0, 1, time);

            brick.transform.localScale =
                Vector3.Lerp(
                    Vector3.zero,
                    targetScale,
                    smoothTime
                );

            yield return null;
        }

        // FIX SCALE
        brick.transform.localScale =
            targetScale;
    }

    IEnumerator MoveSharks(bool moveUp)
    {
        float time = 0;

        Vector3[] targetPos =
            new Vector3[sharks.Length];

        for (int i = 0; i < sharks.Length; i++)
        {
            if (sharks[i] == null)
                continue;

            if (moveUp)
            {
                targetPos[i] =
                    sharkStartPos[i] +
                    Vector3.up * sharkUpHeight;
            }
            else
            {
                targetPos[i] =
                    sharkStartPos[i];
            }
        }

        while (time < 1)
        {
            time += Time.deltaTime * sharkSpeed;

            for (int i = 0; i < sharks.Length; i++)
            {
                if (sharks[i] == null)
                    continue;

                sharks[i].position =
                    Vector3.Lerp(
                        sharks[i].position,
                        targetPos[i],
                        time
                    );
            }

            yield return null;
        }

        for (int i = 0; i < sharks.Length; i++)
        {
            if (sharks[i] != null)
                sharks[i].position = targetPos[i];
        }
    }
    public void ShowWarning()
    {
        foreach (GameObject skull in skulls)
        {
            if (skull != null)
                skull.SetActive(true);
        }
    }

    public void HideWarning()
    {
        foreach (GameObject skull in skulls)
        {
            if (skull != null)
                skull.SetActive(false);
        }
    }
    public void ResetRow()
    {
        InitializeRow();

        // Dừng toàn bộ coroutine đang chạy trên TrapRow
        StopAllCoroutines();

        isRunning = false;

        // Ẩn warning
        HideWarning();

        // Reset cá mập
        for (int i = 0; i < sharks.Length; i++)
        {
            if (sharks[i] == null)
                continue;

            sharks[i].position = sharkStartPos[i];
            sharks[i].gameObject.SetActive(false);
        }

        // Phục hồi toàn bộ gạch
        for (int i = 0; i < bricks.Length; i++)
        {
            GameObject brick = bricks[i];

            if (brick == null)
                continue;

            brick.transform.localScale = brickStartScales[i];

            MeshRenderer mesh =
                brick.GetComponent<MeshRenderer>();

            if (mesh != null)
                mesh.enabled = true;

            Collider col =
                brick.GetComponent<Collider>();

            if (col != null)
                col.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (isInitialized)
        {
            ResetRow();
        }
    }
}