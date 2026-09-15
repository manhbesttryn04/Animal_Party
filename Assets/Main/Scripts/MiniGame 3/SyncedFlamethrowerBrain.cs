using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SyncedFlamethrowerBrain : MonoBehaviour
{
    [Header("--- Trạm Điều Khiển Bẫy ---")]
    public WallFlamethrowerCore leftCore;
    public Transform leftTrap;
    public WallFlamethrowerCore rightCore;
    public Transform rightTrap;

    [Header("--- Nhịp Độ Chung ---")]
    public float idleTimeMin = 2f;
    public float idleTimeMax = 4f;

    [Header("--- Cảnh Báo (Telegraph) ---")]
    public float telegraphDuration = 1f;
    public float shakeIntensity = 5f;

    [Header("--- Cơ chế 1: Sweep ---")]
    public float sweepDuration = 4f;
    public float sweepAngle = 45f;
    public float rpm = 30f;
    public bool mirrorSweep = true;

    [Header("--- Cơ chế 2: Aim Player ---")]
    public float detectionRadius = 50f;
    public float trackingSpeed = 5f;
    public float aimDuration = 1.5f;
    public float trackingFireDuration = 3.5f;
    [Range(0f, 1f)] public float fireTrackingSpeedMultiplier = 0.35f;
    public float retargetInterval = 0.25f;
    public float equalDistanceTolerance = 0.15f;
    public bool splitTargetsWhenPossible = true;
    public LayerMask playerLayer;

    private Quaternion leftInitialRot;
    private Quaternion rightInitialRot;
    private Coroutine brainCoroutine;
    private bool trapRunning;
    private void Awake()
    {
        if (leftTrap != null) leftInitialRot = leftTrap.localRotation;
        if (rightTrap != null) rightInitialRot = rightTrap.localRotation;
    }

    private void OnEnable()
    {
        SetTrapRunning(false);
    }

    private void OnDisable()
    {
        SetTrapRunning(false);
    }
    public void SetTrapRunning(bool state)
    {
        if (state)
        {
            if (trapRunning)
                return;

            trapRunning = true;

            SetFireState(false);
            ResetRotations();

            // Dọn sạch mọi coroutine còn sót từ lần chạy trước.
            StopAllCoroutines();

            brainCoroutine = StartCoroutine(TrapRoutine());
        }
        else
        {
            trapRunning = false;

            // TrapRoutine từng chạy các routine con bằng StartCoroutine.
            // Chỉ StopCoroutine(brainCoroutine) có thể để routine con tiếp tục
            // và bật lửa/âm thanh trở lại sau khi minigame đã kết thúc.
            StopAllCoroutines();
            brainCoroutine = null;

            // Luôn ép tắt Particle, Collider và âm thanh lửa.
            SetFireState(false);
            ResetRotations();
        }
    }

    private void SetFireState(bool state)
    {
        if (leftCore != null) leftCore.SyncFlamethrowerState(state);
        if (rightCore != null) rightCore.SyncFlamethrowerState(state);
    }

    private void ResetRotations()
    {
        if (leftTrap != null) leftTrap.localRotation = leftInitialRot;
        if (rightTrap != null) rightTrap.localRotation = rightInitialRot;
    }

    private IEnumerator TrapRoutine()
    {
        while (trapRunning)
        {
            yield return new WaitForSeconds(Random.Range(idleTimeMin, idleTimeMax));

            if (!trapRunning)
                yield break;

            // Yield trực tiếp IEnumerator để routine con thuộc cùng chuỗi.
            yield return TelegraphRoutine();

            if (!trapRunning)
                yield break;

            // Tỷ lệ cố định: 9 lần Aim Player, 1 lần Sweep.
            bool useTrackingMode = Random.Range(0, 10) < 9;

            if (useTrackingMode)
                yield return TrackingModeRoutine();
            else
                yield return SweepModeRoutine();

            SetFireState(false);
            ResetRotations();
        }
    }

    private IEnumerator TelegraphRoutine()
    {
        float elapsed = 0f;
        while (trapRunning && elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float shake = Mathf.Sin(Time.time * 50f) * shakeIntensity;

            if (leftTrap != null) leftTrap.localRotation = leftInitialRot * Quaternion.Euler(0, shake, 0);
            if (rightTrap != null) rightTrap.localRotation = rightInitialRot * Quaternion.Euler(0, shake, 0);

            yield return null;
        }
        ResetRotations();
    }

    private IEnumerator SweepModeRoutine()
    {
        if (!trapRunning)
            yield break;

        SetFireState(true);
        float elapsed = 0f;
        float angularSpeed = (rpm * Mathf.PI * 2f) / 60f;

        while (trapRunning && elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            float baseAngle = Mathf.Sin(elapsed * angularSpeed) * sweepAngle;

            if (leftTrap != null)
                leftTrap.localRotation = leftInitialRot * Quaternion.Euler(0, baseAngle, 0);

            if (rightTrap != null)
            {
                float rightAngle = mirrorSweep ? -baseAngle : baseAngle;
                rightTrap.localRotation = rightInitialRot * Quaternion.Euler(0, rightAngle, 0);
            }
            yield return null;
        }
    }

    private IEnumerator TrackingModeRoutine()
    {
        if (!trapRunning)
            yield break;

        AcquireTrackingTargets(out Transform leftTarget, out Transform rightTarget);

        float elapsedAim = 0f;
        float retargetTimer = 0f;

        // Giai đoạn cảnh báo/aim: bám mục tiêu với tốc độ đầy đủ.
        while (trapRunning && elapsedAim < aimDuration)
        {
            elapsedAim += Time.deltaTime;
            retargetTimer -= Time.deltaTime;

            if (retargetTimer <= 0f &&
                (!IsTargetValid(leftTarget, leftTrap) ||
                 !IsTargetValid(rightTarget, rightTrap)))
            {
                AcquireTrackingTargets(out leftTarget, out rightTarget);
                retargetTimer = Mathf.Max(0.05f, retargetInterval);
            }

            RotateTrapTowards(leftTrap, leftTarget, trackingSpeed);
            RotateTrapTowards(rightTrap, rightTarget, trackingSpeed);
            yield return null;
        }

        if (!trapRunning)
            yield break;

        SetFireState(true);

        float elapsedFire = 0f;
        retargetTimer = 0f;
        float fireTrackingSpeed = trackingSpeed * fireTrackingSpeedMultiplier;

        // Khi đang phun vẫn bám mục tiêu chậm hơn để nguy hiểm nhưng còn né được.
        while (trapRunning && elapsedFire < trackingFireDuration)
        {
            elapsedFire += Time.deltaTime;
            retargetTimer -= Time.deltaTime;

            if (retargetTimer <= 0f &&
                (!IsTargetValid(leftTarget, leftTrap) ||
                 !IsTargetValid(rightTarget, rightTrap)))
            {
                AcquireTrackingTargets(out leftTarget, out rightTarget);
                retargetTimer = Mathf.Max(0.05f, retargetInterval);
            }

            RotateTrapTowards(leftTrap, leftTarget, fireTrackingSpeed);
            RotateTrapTowards(rightTrap, rightTarget, fireTrackingSpeed);
            yield return null;
        }
    }

    private void AcquireTrackingTargets(
        out Transform leftTarget,
        out Transform rightTarget)
    {
        leftTarget = null;
        rightTarget = null;

        bool chooseLeftFirst = Random.value < 0.5f;

        if (chooseLeftFirst)
        {
            if (leftTrap != null)
                leftTarget = FindNearestPlayer(leftTrap.position, null);

            if (rightTrap != null)
            {
                Transform excluded = splitTargetsWhenPossible ? leftTarget : null;
                rightTarget = FindNearestPlayer(rightTrap.position, excluded);

                if (rightTarget == null)
                    rightTarget = FindNearestPlayer(rightTrap.position, null);
            }
        }
        else
        {
            if (rightTrap != null)
                rightTarget = FindNearestPlayer(rightTrap.position, null);

            if (leftTrap != null)
            {
                Transform excluded = splitTargetsWhenPossible ? rightTarget : null;
                leftTarget = FindNearestPlayer(leftTrap.position, excluded);

                if (leftTarget == null)
                    leftTarget = FindNearestPlayer(leftTrap.position, null);
            }
        }
    }

    private Transform FindNearestPlayer(Vector3 origin, Transform excludedTarget)
    {
        Collider[] hits = Physics.OverlapSphere(origin, detectionRadius, playerLayer);
        Transform nearest = null;
        float minDist = Mathf.Infinity;
        HashSet<Transform> checkedPlayers = new HashSet<Transform>();

        foreach (var hit in hits)
        {
            if (hit == null)
                continue;

            PlayerMove playerMove = hit.GetComponentInParent<PlayerMove>();
            Transform candidate = playerMove != null
                ? playerMove.transform
                : hit.transform;

            if (candidate == null || candidate == excludedTarget)
                continue;

            // Một player có thể có nhiều collider, chỉ tính một lần.
            if (!checkedPlayers.Add(candidate))
                continue;

            float dist = Vector3.Distance(origin, candidate.position);

            if (dist < minDist - equalDistanceTolerance)
            {
                minDist = dist;
                nearest = candidate;
            }
            else if (Mathf.Abs(dist - minDist) <= equalDistanceTolerance &&
                     Random.value < 0.5f)
            {
                // Hai player gần bằng nhau: chọn ngẫu nhiên để công bằng.
                nearest = candidate;
            }
        }

        return nearest;
    }

    private bool IsTargetValid(Transform target, Transform trap)
    {
        if (target == null || trap == null)
            return false;

        float maxDistanceSqr = detectionRadius * detectionRadius;
        return (target.position - trap.position).sqrMagnitude <= maxDistanceSqr;
    }

    private void RotateTrapTowards(
        Transform trap,
        Transform target,
        float speed)
    {
        if (trap == null || target == null || speed <= 0f)
            return;

        Vector3 direction = target.position - trap.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        trap.rotation = Quaternion.Slerp(
            trap.rotation,
            targetRotation,
            Time.deltaTime * speed
        );
    }
}