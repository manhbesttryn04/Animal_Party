using UnityEngine;
using TMPro;
using System.Collections;

public class MiniGame9Cube : MonoBehaviour
{
    [Header("Cấu hình Khối Cube Rodeo")]
    [Tooltip("Kéo thả GameObject khối Cube chính vào đây")]
    public Transform rodeoCube;

    [Header("Cấu hình Tốc Độ & Góc Nghiêng Ngẫu Nhiên")]
    [Tooltip("Tốc độ nghiêng tối thiểu")]
    public float minTiltSpeed = 30f;

    [Tooltip("Tốc độ nghiêng tối đa")]
    public float maxTiltSpeed = 60f;

    [Tooltip("Góc nghiêng tối thiểu (Độ)")]
    public float minTiltAngle = 45f;

    [Tooltip("Góc nghiêng tối đa (Độ)")]
    public float maxTiltAngle = 180f;

    [Tooltip("Thời gian tối thiểu giữa các lần đổi hướng nghiêng (giây)")]
    public float minInterval = 1.5f;

    [Tooltip("Thời gian tối đa giữa các lần đổi hướng nghiêng (giây)")]
    public float maxInterval = 5f;

    [Header("Cấu hình Rung Lắc (Phụ thuộc góc nghiêng)")]
    [Tooltip("Bật/tắt hiệu ứng rung giật khi chuyển góc")]
    public bool enableShake = true;

    [Tooltip("Độ rung tối thiểu khi góc nghiêng thấp")]
    public float minShakeIntensity = 0.5f;

    [Tooltip("Độ rung tối đa khi góc nghiêng cao")]
    public float maxShakeIntensity = 3f;

    [Tooltip("Thời gian mỗi đợt rung lắc (giây)")]
    public float shakeDuration = 0.45f;

    [Header("Cấu hình Trượt & Vật Lý")]
    [Tooltip("Lực kéo trượt Player về phía bên nghiêng thấp hơn")]
    public float slideForce = 6f;

    [Header("UI & Trạng Thái")]
    public float gameDuration = 60f;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI statusText;

    private Quaternion targetRotation;
    private float currentTiltSpeed;
    private float currentTargetAngle; // Góc nghiêng của đợt hiện tại
    private bool isShaking = false;
    private Vector3 shakeOffset = Vector3.zero;

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

        currentTiltSpeed = minTiltSpeed;
        currentTargetAngle = minTiltAngle;

        if (rodeoCube != null)
        {
            targetRotation = rodeoCube.rotation;
        }

        if (statusText != null) statusText.text = "GÓC NGHIÊNG CÀNG CAO - RUNG LẮC CÀNG MẠNH!";

        StartCoroutine(GameTimerRoutine());
        StartCoroutine(RandomTiltRoutine());
    }

    void Update()
    {
        if (!isPlaying || rodeoCube == null) return;

        // 1. Nghiêng/Xoay khối Cube tiến về Target Rotation theo tốc độ ngẫu nhiên
        Quaternion baseRotation = Quaternion.RotateTowards(rodeoCube.rotation, targetRotation, currentTiltSpeed * Time.deltaTime);

        // 2. Tự động tính toán độ rung lắc dựa vào góc nghiêng hiện tại của Target
        if (isShaking)
        {
            // Tỷ lệ nghiêng từ 0 đến 1 dựa trên currentTargetAngle / maxTiltAngle
            float tiltRatio = Mathf.Clamp01(currentTargetAngle / maxTiltAngle);

            // Góc nghiêng càng cao thì intensity càng tiệm cận maxShakeIntensity
            float calculatedShake = Mathf.Lerp(minShakeIntensity, maxShakeIntensity, tiltRatio);

            float shakeX = Random.Range(-calculatedShake, calculatedShake);
            float shakeY = Random.Range(-calculatedShake, calculatedShake);
            float shakeZ = Random.Range(-calculatedShake, calculatedShake);
            shakeOffset = new Vector3(shakeX, shakeY, shakeZ);
        }
        else
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, Time.deltaTime * 10f);
        }

        // Áp dụng góc xoay kết hợp hiệu ứng rung
        rodeoCube.rotation = baseRotation * Quaternion.Euler(shakeOffset);

        // 3. Tác động lực trượt đẩy Player
        ApplySlidePhysics();

        // 4. Kiểm tra Player rơi rớt
        CheckPlayerFall();
    }

    // Coroutine liên tục Random hướng, tốc độ, góc nghiêng và kích hoạt rung lắc
    IEnumerator RandomTiltRoutine()
    {
        while (isPlaying)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            if (!isPlaying) yield break;

            // 1. Random Tốc độ nghiêng
            currentTiltSpeed = Random.Range(minTiltSpeed, maxTiltSpeed);

            // 2. Random Góc nghiêng tối đa cho đợt này
            currentTargetAngle = Random.Range(minTiltAngle, maxTiltAngle);

            // 3. Random hướng nghiêng theo các trục X, Z, Y
            float randomAngleX = Random.Range(-currentTargetAngle, currentTargetAngle);
            float randomAngleZ = Random.Range(-currentTargetAngle, currentTargetAngle);
            float randomAngleY = Random.Range(-20f, 20f);

            targetRotation = Quaternion.Euler(randomAngleX, randomAngleY, randomAngleZ);

            // Kích hoạt rung lắc (Lực rung sẽ tự tỉ lệ với currentTargetAngle trong Update)
            if (enableShake)
            {
                StartCoroutine(TriggerShakeRoutine());
            }
        }
    }

    IEnumerator TriggerShakeRoutine()
    {
        isShaking = true;
        yield return new WaitForSeconds(shakeDuration);
        isShaking = false;
    }

    void ApplySlidePhysics()
    {
        if (rodeoCube == null) return;

        Vector3 cubeUp = rodeoCube.up;
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, cubeUp).normalized;

        float currentTilt = Vector3.Angle(cubeUp, Vector3.up);
        if (currentTilt > 2f)
        {
            float dynamicForce = slideForce * (currentTilt / maxTiltAngle);

            if (player1Obj != null && p1CC != null && p1CC.isGrounded)
            {
                p1CC.Move(slideDirection * dynamicForce * Time.deltaTime);
            }

            if (player2Obj != null && p2CC != null && p2CC.isGrounded)
            {
                p2CC.Move(slideDirection * dynamicForce * Time.deltaTime);
            }
        }
    }

    void CheckPlayerFall()
    {
        bool p1Fell = player1Obj != null && player1Obj.transform.position.y < -5f;
        bool p2Fell = player2Obj != null && player2Obj.transform.position.y < -5f;

        if (p1Fell && p2Fell)
        {
            EndGame("HÒA NHAU! CẢ HAI CÙNG RỚT DUNG NHAM!");
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