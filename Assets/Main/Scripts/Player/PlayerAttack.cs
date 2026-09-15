using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    public PlayerManager playerManager;

    [Header("Attack Settings")]
    public BoxCollider kickCollider;
    public float attackCooldown = 2f;
    public float aimDistance = 3f;
    public bool hasAttack;

    public bool canAttack = true;

    private void Start()
    {
        if (kickCollider != null)
        {
            kickCollider.enabled = false;
        }

        if (playerManager == null)
        {
            playerManager = GetComponent<PlayerManager>();
        }
    }

    private void Update()
    {
        if (!hasAttack)
            return;

        if (!canAttack)
            return;

        if (IsAttackPressed())
        {
            Attack();
        }
    }

    // =========================================================
    // INPUT
    // =========================================================

    private bool IsPlayer2()
    {
        return playerManager != null &&
               playerManager.playerType != null &&
               playerManager.playerType.isPlayer2;
    }

    private bool IsUsingController()
    {
        ControllerManager controller =
            ControllerManager.Instance;

        if (controller == null)
            return false;

        if (IsPlayer2())
        {
            return controller.IsConsole2Connected();
        }

        return controller.IsConsole1Connected();
    }

    private int GetConsoleNumber()
    {
        return IsPlayer2() ? 2 : 1;
    }

    private bool IsAttackPressed()
    {
        /*
         * Có tay cầm:
         * chỉ nhận Button 1 của đúng Console.
         * Không nhận J hoặc Keypad1.
         */
        if (IsUsingController())
        {
            return ControllerManager.Instance
                .GetConsoleButtonDown(
                    GetConsoleNumber(),
                    1
                );
        }

        /*
         * Không có tay cầm:
         * mới cho phép dùng bàn phím.
         */
        if (IsPlayer2())
        {
            return Input.GetKeyDown(
                KeyCode.Keypad1
            );
        }

        return Input.GetKeyDown(
            KeyCode.J
        );
    }

    // =========================================================
    // ATTACK
    // =========================================================

    private void Attack()
    {
        Transform target =
            FindTarget();

        if (target != null)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    target.position
                );

            // Đối thủ đủ gần thì tự xoay mặt về phía đối thủ.
            if (distance <= aimDistance)
            {
                Vector3 direction =
                    target.position -
                    transform.position;

                direction.y = 0f;

                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation =
                        Quaternion.LookRotation(
                            direction
                        );
                }
            }
        }

        canAttack = false;

        if (playerManager != null &&
            playerManager.playerAnimator != null &&
            playerManager.playerAnimator.playerAnimator != null)
        {
            playerManager.playerAnimator
                .playerAnimator
                .SetTrigger("Attack");
        }

        Invoke(
            nameof(ResetAttack),
            attackCooldown
        );
    }

    // =========================================================
    // TARGET
    // =========================================================

    private Transform FindTarget()
    {
        string targetTag =
            IsPlayer2()
                ? "Player 1"
                : "Player 2";

        GameObject[] players =
            GameObject.FindGameObjectsWithTag(
                targetTag
            );

        Transform nearest = null;
        float nearestDistance =
            Mathf.Infinity;

        foreach (GameObject player in players)
        {
            if (player == null ||
                player == gameObject)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    player.transform.position
                );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = player.transform;
            }
        }

        return nearest;
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    // =========================================================
    // ANIMATION EVENTS
    // =========================================================

    public void EnableKickCollider()
    {
        if (kickCollider != null)
        {
            kickCollider.enabled = true;
        }
    }

    public void DisableKickCollider()
    {
        if (kickCollider != null)
        {
            kickCollider.enabled = false;
        }
    }

    private void OnDisable()
    {
        CancelInvoke(
            nameof(ResetAttack)
        );

        canAttack = true;

        if (kickCollider != null)
        {
            kickCollider.enabled = false;
        }
    }
}