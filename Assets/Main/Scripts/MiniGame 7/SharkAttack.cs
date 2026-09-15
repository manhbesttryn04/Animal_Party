using System;
using System.Collections;
using UnityEngine;

public class SharkAttack : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Move To Player")]
    public float moveSpeed = 12f;
    public float rotateSpeed = 8f;
    public float attackDistance = 2f;

    [Header("Attack")]
    public string attackTrigger = "Attack";

    // Nếu quên đặt Animation Event, sau thời gian này
    // hệ thống vẫn tự xác nhận đã cắn
    public float biteEventTimeout = 3f;

    [Header("Swim Around")]
    public float circleRadius = 4f;
    public float circleSpeed = 45f;
    public float circleMoveSpeed = 6f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private bool isAttacking;
    private bool isSwimmingAround;
    private bool biteEventReceived;

    private Vector3 circleCenter;

    private Coroutine sharkRoutine;
    private Action onBiteCallback;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    // MiniGame7 gọi hàm này khi player rơi xuống nước
    public void AttackPlayer(
        Transform player,
        Action onBite
    )
    {
        if (player == null || isAttacking)
            return;

        StopSharkRoutine();

        onBiteCallback = onBite;
        biteEventReceived = false;
        isAttacking = true;
        isSwimmingAround = false;

        sharkRoutine = StartCoroutine(
            AttackRoutine(player)
        );
    }

    private IEnumerator AttackRoutine(Transform target)
    {
        // Bơi tới player
        while (target != null)
        {
            Vector3 direction =
                target.position - transform.position;

            // Chỉ xoay ngang, không chúi đầu lên xuống
            direction.y = 0f;

            float distance = direction.magnitude;

            if (distance <= attackDistance)
                break;

            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();

                Quaternion targetRotation =
                    Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );

                transform.position +=
                    transform.forward *
                    moveSpeed *
                    Time.deltaTime;
            }

            yield return null;
        }

        // Lưu vị trí để sau khi cắn sẽ bơi vòng quanh
        if (target != null)
        {
            circleCenter = target.position;
            circleCenter.y = transform.position.y;
        }
        else
        {
            circleCenter = transform.position;
        }

        // Quay chính xác về phía player trước khi cắn
        if (target != null)
        {
            Vector3 attackDirection =
                target.position - transform.position;

            attackDirection.y = 0f;

            if (attackDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        attackDirection.normalized
                    );
            }
        }

        // Chạy animation cắn
        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }

        // Phát âm thanh bằng AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance.sharkAttackClip
            );
        }

        // Chờ Animation Event tại đúng khung hình cắn
        float timer = 0f;

        while (!biteEventReceived &&
               timer < biteEventTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // Trường hợp quên đặt Animation Event
        if (!biteEventReceived)
        {
            ConfirmBite();
        }

        isAttacking = false;

        // Sau khi cắn, bơi vòng quanh thong thả
        yield return SwimAroundRoutine(circleCenter);
    }

    // Gọi hàm này bằng Animation Event
    // tại đúng khung hình miệng cá mập cắn player
    public void OnBiteAnimationEvent()
    {
        ConfirmBite();
    }

    private void ConfirmBite()
    {
        if (biteEventReceived)
            return;

        biteEventReceived = true;

        onBiteCallback?.Invoke();
        onBiteCallback = null;
    }

    private IEnumerator SwimAroundRoutine(Vector3 center)
    {
        isSwimmingAround = true;

        Vector3 offset =
            transform.position - center;

        offset.y = 0f;

        float angle;

        if (offset.sqrMagnitude > 0.01f)
        {
            angle =
                Mathf.Atan2(offset.z, offset.x) *
                Mathf.Rad2Deg;
        }
        else
        {
            angle = 0f;
        }

        while (isSwimmingAround)
        {
            angle += circleSpeed * Time.deltaTime;

            float radians =
                angle * Mathf.Deg2Rad;

            Vector3 targetPosition =
                center +
                new Vector3(
                    Mathf.Cos(radians) * circleRadius,
                    0f,
                    Mathf.Sin(radians) * circleRadius
                );

            targetPosition.y = transform.position.y;

            Vector3 direction =
                targetPosition - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        direction.normalized
                    );

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        rotateSpeed * Time.deltaTime
                    );

                transform.position =
                    Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        circleMoveSpeed * Time.deltaTime
                    );
            }

            yield return null;
        }
    }

    private void StopSharkRoutine()
    {
        if (sharkRoutine != null)
        {
            StopCoroutine(sharkRoutine);
            sharkRoutine = null;
        }
    }

    public void ResetShark()
    {
        StopAllCoroutines();

        sharkRoutine = null;
        onBiteCallback = null;

        isAttacking = false;
        isSwimmingAround = false;
        biteEventReceived = false;

        transform.position = startPosition;
        transform.rotation = startRotation;

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);
            animator.Rebind();
            animator.Update(0f);
        }
    }
}