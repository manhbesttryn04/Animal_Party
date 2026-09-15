using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PummelLaserHub : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float depth = 5f;
    public float startDelay = 3f;

    [Header("--- Tốc Độ Quay ---")]
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    public float acceleration = 180f; 
    
    [Header("--- Ép Xung (Progression) ---")]
    public bool useProgression = true;
    public float speedIncreasePerSecond = 1.5f;
    public float absoluteMaxSpeed = 300f;

    [Header("--- Timing: Chiêu Cũ (Dừng & Quay) ---")]
    public float minSpinTime = 2f;
    public float maxSpinTime = 4f;
    public float minPauseTime = 0.5f;
    public float maxPauseTime = 1.5f;

    [Header("--- Timing: Chiêu Mới (Quay Điên Cuồng) ---")]
    public float minContinuousTime = 5f;
    public float maxContinuousTime = 8f;

    [Header("--- Fake Out (Chiêu Lừa) ---")]
    [Range(0f, 1f)] public float fakeOutChance = 0.4f;
    public float fakeOutPauseTime = 0.6f; 
    public float minFakeOutSpinTime = 2.5f;
    public float maxFakeOutSpinTime = 4f;

    public float knockbackForce = 15f;
    public List<AutoFitLaser> laserBeams = new List<AutoFitLaser>();

    private bool isReady = false;
    private float currentVelocity = 0f; 
    private float targetVelocity = 0f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float originalMaxSpeed;

    private void Awake()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalMaxSpeed = maxSpeed;
    }

    private void Start()
    {
        ResetHub();
    }

    public void ResetHub()
    {
        StopAllCoroutines();

        isReady = false;
        currentVelocity = 0f;
        targetVelocity = 0f;
        maxSpeed = originalMaxSpeed;

        transform.position = originalPosition - new Vector3(0, depth, 0);
        transform.rotation = originalRotation;

        foreach (var laser in laserBeams)
        {
            if (laser != null) laser.SetLaserActive(false);
        }

        StartCoroutine(RiseRoutine());
    }

    private IEnumerator RiseRoutine()
    {
        Vector3 targetPos = originalPosition;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        yield return new WaitForSeconds(startDelay);

        isReady = true;

        foreach (var laser in laserBeams)
        {
            if (laser != null) laser.SetLaserActive(true);
        }
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXNoOneShot(AudioManager.Instance.laserMoveClip);
            
        StartCoroutine(VIPPatternRoutine());
    }

    private void Update()
    {
        if (!isReady) return;

        if (useProgression)
        {
            maxSpeed += speedIncreasePerSecond * Time.deltaTime;
            maxSpeed = Mathf.Min(maxSpeed, absoluteMaxSpeed);
        }

        currentVelocity = Mathf.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        transform.Rotate(Vector3.up * currentVelocity * Time.deltaTime);
    }

    private IEnumerator VIPPatternRoutine()
    {
        while (isReady)
        {
            // Tung đồng xu: 0 là dùng chiêu Cũ, 1 là dùng chiêu Mới
            int patternMode = Random.Range(0, 2);

            if (patternMode == 0)
            {
                // ==========================================
                // CƠ CHẾ CŨ: BÌNH THƯỜNG (CÓ DỪNG & CÓ LỪA)
                // ==========================================
                float dir = Random.value > 0.5f ? 1f : -1f;
                targetVelocity = Random.Range(minSpeed, maxSpeed * 0.7f) * dir;
                
                yield return new WaitForSeconds(Random.Range(minSpinTime, maxSpinTime));

                targetVelocity = 0f; // Phanh lại cho nghỉ
                yield return new WaitForSeconds(Random.Range(minPauseTime, maxPauseTime));

                if (Random.value <= fakeOutChance)
                {
                    float fakeDir = Random.value > 0.5f ? 1f : -1f;
                    targetVelocity = 50f * fakeDir; // Nhích nhẹ lừa đảo
                    
                    yield return new WaitForSeconds(fakeOutPauseTime); 

                    targetVelocity = maxSpeed * -fakeDir; // Khởi động gắt ngược chiều
                    yield return new WaitForSeconds(Random.Range(minFakeOutSpinTime, maxFakeOutSpinTime));
                    
                    targetVelocity = 0f;
                    yield return new WaitForSeconds(Random.Range(minPauseTime, maxPauseTime));
                }
            }
            else
            {
                // ==========================================
                // CƠ CHẾ MỚI: TỪ VIDEO (QUAY NHANH LIÊN TỤC)
                // ==========================================
                float dir = Random.value > 0.5f ? 1f : -1f;
                targetVelocity = maxSpeed * dir; // Ép tốc độ tối đa ngay lập tức
                
                yield return new WaitForSeconds(Random.Range(minContinuousTime, maxContinuousTime));

                // Đảo chiều cực gắt, không cho thời gian nghỉ ngơi
                targetVelocity = maxSpeed * -dir; 
                yield return new WaitForSeconds(Random.Range(minContinuousTime, maxContinuousTime));

                // Phanh lại trước khi đổi sang chiêu khác
                targetVelocity = 0f;
                yield return new WaitForSeconds(Random.Range(minPauseTime, maxPauseTime));
            }
        }
    }

    public void EndMinigameAndSink()
    {
        if (!isReady) return;

        isReady = false;
        StopAllCoroutines();
        StartCoroutine(SinkRoutine());
    }

    private IEnumerator SinkRoutine()
    {
        foreach (var laser in laserBeams)
        {
            if (laser != null) laser.SetLaserActive(false);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopSFXNoOneShot();
            
        float duration = 1.5f;
        Vector3 startPos = transform.position;
        Vector3 endPos = originalPosition - new Vector3(0, depth, 0);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        gameObject.SetActive(false);
    }
}