using UnityEngine;
using TMPro;
using System.Collections;

public class MiniGame9 : MonoBehaviour
{
    [Header("Cấu hình 3 Trụ Xoay")]
    public Transform mainRoller;
    public Transform leftRoller;
    public Transform rightRoller;

    [Header("Cấu hình Tốc Độ & Đảo Chiều Ngẫu Nhiên")]
    [Tooltip("Tốc độ quay tối thiểu")]
    public float minRotateSpeed = 20f;

    [Tooltip("Tốc độ quay tối đa")]
    public float maxRotateSpeed = 65f;

    [Tooltip("Thời gian tối thiểu để đổi tốc độ/chiều quay (giây)")]
    public float minChangeInterval = 4f;

    [Tooltip("Thời gian tối đa để đổi tốc độ/chiều quay (giây)")]
    public float maxChangeInterval = 8f;

    [Tooltip("Độ mượt khi chuyển đổi tốc độ")]
    public float speedLerpSmoothness = 2f;

    [Header("Cấu hình Lực Ma Sát Đẩy Player")]
    public float surfaceSlipForce = 4f;

    [Header("UI & Trạng Thái")]
    public float gameDuration = 60f;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    // Tốc độ hiện tại (Có dấu: Dương = Quay tiến, Âm = Quay lùi)
    private float currentMainSpeed;
    private float currentLeftSpeed;
    private float currentRightSpeed;

    // Tốc độ mục tiêu
    private float targetMainSpeed;
    private float targetLeftSpeed;
    private float targetRightSpeed;

    private float timeLeft;
    private bool isPlaying = false;

    private GameObject player1Obj;
    private GameObject player2Obj;
    private CharacterController p1CC;
    private CharacterController p2CC;

    void Start()
    {
        FindPlayers();
        StartMinigame();
    }

    void FindPlayers()
    {
        player1Obj = GameObject.Find("Player Play 1");
        player2Obj = GameObject.Find("Player Play 2");

        if (player1Obj != null) p1CC = player1Obj.GetComponent<CharacterController>();
        if (player2Obj != null) p2CC = player2Obj.GetComponent<CharacterController>();
    }

    public void StartMinigame()
    {
        timeLeft = gameDuration;
        isPlaying = true;

        // Khởi tạo tốc độ mục tiêu ban đầu
        GenerateNewTargetSpeeds();
        currentMainSpeed = targetMainSpeed;
        currentLeftSpeed = targetLeftSpeed;
        currentRightSpeed = targetRightSpeed;

        if (statusText != null) statusText.text = "CHÚ Ý! TRỤ ĐỔI HƯỚNG & TỐC ĐỘ XOAY!";

        StartCoroutine(GameTimerRoutine());
        StartCoroutine(RandomSpeedRoutine());
    }

    void Update()
    {
        if (!isPlaying) return;

        // Biến đổi tốc độ mượt mà từ hiện tại sang mục tiêu (hỗ trợ đổi chiều không bị khựng)
        currentMainSpeed = Mathf.Lerp(currentMainSpeed, targetMainSpeed, Time.deltaTime * speedLerpSmoothness);
        currentLeftSpeed = Mathf.Lerp(currentLeftSpeed, targetLeftSpeed, Time.deltaTime * speedLerpSmoothness);
        currentRightSpeed = Mathf.Lerp(currentRightSpeed, targetRightSpeed, Time.deltaTime * speedLerpSmoothness);

        // FIX CHUẨN: Xoay theo Vector3.up nhưng chỉ dùng 1 dấu chuẩn 
        // Lực ngược chiều được tính toán bằng cách lật hướng trong TargetSpeed
        if (mainRoller != null)
        {
            mainRoller.Rotate(Vector3.up * currentMainSpeed * Time.deltaTime, Space.Self);
        }

        if (leftRoller != null)
        {
            leftRoller.Rotate(Vector3.up * currentLeftSpeed * Time.deltaTime, Space.Self);
        }

        if (rightRoller != null)
        {
            rightRoller.Rotate(Vector3.up * currentRightSpeed * Time.deltaTime, Space.Self);
        }

        ApplySurfaceDrag();
        CheckPlayerFall();
    }

    IEnumerator RandomSpeedRoutine()
    {
        while (isPlaying)
        {
            float waitTime = Random.Range(minChangeInterval, maxChangeInterval);
            yield return new WaitForSeconds(waitTime);

            if (!isPlaying) yield break;

            // Random lại tốc độ và chiều quay mới
            GenerateNewTargetSpeeds();
        }
    }

    void GenerateNewTargetSpeeds()
    {
        // Random giá trị tốc độ
        float mainSpd = Random.Range(minRotateSpeed, maxRotateSpeed);
        float leftSpd = Random.Range(minRotateSpeed, maxRotateSpeed);
        float rightSpd = Random.Range(minRotateSpeed, maxRotateSpeed);

        // Random chiều quay (50% cơ hội quay tiến, 50% quay lùi)
        float mainDir = Random.value > 0.5f ? 1f : -1f;

        // Khối MainRoller quay theo chiều mainDir
        targetMainSpeed = mainSpd * mainDir;

        // Do Left/Right có Z = -90 (được đặt ngược góc 180 độ so với Main Z = 90),
        // nên ta giữ nguyên dấu mainDir để 2 khối này TỰ ĐỘNG XOAY NGƯỢC CHIỀU với MainRoller!
        targetLeftSpeed = leftSpd * mainDir;
        targetRightSpeed = rightSpd * mainDir;
    }

    void ApplySurfaceDrag()
    {
        if (player1Obj != null && p1CC != null && p1CC.isGrounded)
        {
            ApplyDragToPlayer(player1Obj, p1CC);
        }

        if (player2Obj != null && p2CC != null && p2CC.isGrounded)
        {
            ApplyDragToPlayer(player2Obj, p2CC);
        }
    }

    void ApplyDragToPlayer(GameObject player, CharacterController cc)
    {
        Transform currentRoller = GetNearestRoller(player.transform.position);
        if (currentRoller == null) return;

        float speed = 0f;
        if (currentRoller == mainRoller) speed = currentMainSpeed;
        else if (currentRoller == leftRoller) speed = currentLeftSpeed;
        else if (currentRoller == rightRoller) speed = currentRightSpeed;

        // Tính hướng trượt thực tế dựa theo tốc độ và chiều quay của trụ đó
        Vector3 dragDirection = -currentRoller.forward * Mathf.Sign(speed);
        float dynamicSlip = surfaceSlipForce * (Mathf.Abs(speed) / minRotateSpeed);

        cc.Move(dragDirection * dynamicSlip * Time.deltaTime);
    }

    Transform GetNearestRoller(Vector3 playerPos)
    {
        Transform nearest = mainRoller;
        float minDistance = float.MaxValue;

        Transform[] rollers = { mainRoller, leftRoller, rightRoller };
        foreach (Transform r in rollers)
        {
            if (r == null) continue;
            float dist = Vector3.Distance(playerPos, r.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = r;
            }
        }
        return nearest;
    }

    void CheckPlayerFall()
    {
        bool p1Fell = player1Obj != null && player1Obj.transform.position.y < -6f;
        bool p2Fell = player2Obj != null && player2Obj.transform.position.y < -6f;

        if (p1Fell && p2Fell)
        {
            EndGame("HÒA NHAU! CẢ HAI ĐỀU RỚT!");
        }
        else if (p1Fell)
        {
            EndGame("PLAYER 2 CHIẾN THẮNG!");
        }
        else if (p2Fell)
        {
            EndGame("PLAYER 1 CHIẾN THẮNG!");
        }
    }

    IEnumerator GameTimerRoutine()
    {
        while (timeLeft > 0 && isPlaying)
        {
            if (timerText != null) timerText.text = Mathf.CeilToInt(timeLeft).ToString() + "s";
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
        }

        if (timeLeft <= 0 && isPlaying)
        {
            EndGame("HẾT GIỜ! CẢ HAI CÙNG SỐNG SÓT!");
        }
    }

    public void EndGame(string message)
    {
        isPlaying = false;
        StopAllCoroutines();
        if (statusText != null) statusText.text = message;
    }
}