using System.Collections;
using UnityEngine;

public class PlayerDefense : MonoBehaviour
{
    [Header("References")]
    public PlayerManager playerManager;
    public PlayerMove playerMove;
    public GameObject shield;

    [Header("Defense Settings")]
    public bool hasDefense = true;

    [Tooltip("Thời gian khiên được bật")]
    public float defenseDuration = 0.3f;

    [Tooltip("Thời gian chờ sau khi khiên tắt")]
    public float defenseCooldown = 2f;

    [Header("Defense State")]
    public bool isDefending;
    public bool canDefense = true;

    private Coroutine defenseCoroutine;

    private void Start()
    {
        SetupDefense();
    }

    private void SetupDefense()
    {
        if (playerManager == null)
            playerManager = GetComponent<PlayerManager>();

        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (shield != null)
            shield.SetActive(false);
    }

    private void Update()
    {
        if (!hasDefense || !canDefense || isDefending)
            return;

        if (playerManager == null || playerManager.playerType == null || playerMove == null)
            return;

        if (!IsDefensePressed())
            return;

        if (!playerMove.isGround)
            return;

        StartDefense();
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
        ControllerManager controllerManager =
            ControllerManager.Instance;

        if (controllerManager == null)
            return false;

        if (IsPlayer2())
        {
            return controllerManager.IsConsole2Connected();
        }

        return controllerManager.IsConsole1Connected();
    }

    private int GetConsoleNumber()
    {
        return IsPlayer2() ? 2 : 1;
    }

    private bool IsDefensePressed()
    {
        /*
         * Có tay cầm:
         * chỉ nhận Button 1 của đúng Console.
         * Không nhận bàn phím.
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
         * mới nhận bàn phím.
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
    // DEFENSE
    // =========================================================

    private void StartDefense()
    {
        if (!canDefense || isDefending)
            return;

        if (playerMove == null ||
            !playerMove.isGround)
        {
            return;
        }

        if (defenseCoroutine != null)
        {
            StopCoroutine(defenseCoroutine);
        }

        defenseCoroutine =
            StartCoroutine(DefenseRoutine());
    }

    private IEnumerator DefenseRoutine()
    {
        isDefending = true;
        canDefense = false;

        // Khóa di chuyển và nhảy.
        if (playerMove != null)
        {
            playerMove.isJumpAndMove = false;
        }

        // Chạy animation Defense.
        if (playerManager != null &&
            playerManager.playerAnimator != null &&
            playerManager.playerAnimator.playerAnimator != null)
        {
            Animator animator =
                playerManager.playerAnimator.playerAnimator;

            animator.SetFloat("Walk", 0f);
            animator.SetFloat("Run", 0f);
            animator.SetTrigger("Defense");
        }

        // Bật khiên.
        if (shield != null)
        {
            shield.SetActive(true);
        }

        yield return new WaitForSeconds(
            defenseDuration
        );

        // Tắt khiên.
        if (shield != null)
        {
            shield.SetActive(false);
        }

        isDefending = false;

        // Mở lại di chuyển và nhảy.
        if (playerMove != null)
        {
            playerMove.isJumpAndMove = true;
        }

        yield return new WaitForSeconds(
            defenseCooldown
        );

        canDefense = true;
        defenseCoroutine = null;
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetDefenseState()
    {
        isDefending = false;
        canDefense = true;

        if (playerMove != null)
        {
            playerMove.isJumpAndMove = true;
        }

        if (shield != null)
        {
            shield.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (defenseCoroutine != null)
        {
            StopCoroutine(defenseCoroutine);
            defenseCoroutine = null;
        }

        ResetDefenseState();
    }
}