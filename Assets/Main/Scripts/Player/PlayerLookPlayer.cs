using System.Collections;
using UnityEngine;

public class PlayerLookPlayer : MonoBehaviour
{
    public enum TurnType
    {
        None,
        Left,
        Right,
        Back
    }

    private TurnType lastTurnType = TurnType.None;

    [Header("Tham chiếu PlayerManager")]
    public PlayerManager manager;

    [Header("Saved Rotation")]
    [SerializeField] private Quaternion savedRotation;

    [Header("Smooth Look")]
    [SerializeField] private float smoothLookTime = 0.25f;

    private void Start()
    {
        SetUpPlayerLookPlayer();
    }

    private void SetUpPlayerLookPlayer()
    {
        manager = GetComponent<PlayerManager>();
    }

    public void LookAtOpponentOnXAxis()
    {
        if (manager == null || manager.playerType == null)
            return;

        if (GameManager.Instance == null)
            return;

        PlayerMoveAI myMoveAI = GetComponent<PlayerMoveAI>();

        if (myMoveAI == null)
            return;

        GameObject opponent;

        if (manager.playerType.isPlayer2)
        {
            opponent = GameManager.Instance.player1Main;
        }
        else
        {
            opponent = GameManager.Instance.player2Main;
        }

        if (opponent == null)
            return;

        PlayerMoveAI opponentMoveAI =
            opponent.GetComponent<PlayerMoveAI>();

        if (opponentMoveAI == null)
            return;

        PlayerAnimator animator =
            GetComponent<PlayerAnimator>();

        if (animator == null)
            return;

        SaveRotationYPlayer();

        // ==========================================
        // PLAYER ĐỨNG TRƯỚC
        // ==========================================

        if (myMoveAI.currentIndex > opponentMoveAI.currentIndex)
        {
            lastTurnType = TurnType.Back;

            animator.playerAnimator.SetTrigger("Back Turn");

            StartCoroutine(
                LookAtOpponentAfterDelay(1.5f)
            );
        }

        // ==========================================
        // PLAYER ĐỨNG SAU
        // ==========================================

        else if (myMoveAI.currentIndex < opponentMoveAI.currentIndex)
        {
            return;
        }

        // ==========================================
        // CÙNG INDEX
        // ==========================================

        else
        {
            if (manager.playerType.isPlayer2)
            {
                lastTurnType = TurnType.Left;

                animator.playerAnimator.SetTrigger("Left Turn");
                StartCoroutine(
                LookAtOpponentAfterDelay(0.5f)
            );
            }
            else
            {
                lastTurnType = TurnType.Right;

                animator.playerAnimator.SetTrigger("Right Turn");
                StartCoroutine(
                LookAtOpponentAfterDelay(0.5f)
            );
            }
        }
    }

    // ==========================================
    // SAVE ROTATION
    // ==========================================

    public void SaveRotationYPlayer()
    {
        savedRotation = transform.rotation;
    }

    // ==========================================
    // LOOK AT OPPONENT AFTER DELAY
    // ==========================================

    private IEnumerator LookAtOpponentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        LookAtOpponent();
    }

    // ==========================================
    // RESET TURN
    // ==========================================

    public void ResetToSavedRotation()
    {
        PlayerAnimator animator =
            GetComponent<PlayerAnimator>();

        if (animator == null)
            return;

        // LEFT → RIGHT
        if (lastTurnType == TurnType.Left)
        {
            animator.playerAnimator.SetTrigger("Right Turn");

            StartCoroutine(
                RotateToSavedRotationAfterDelay(0.7f)
            );
        }

        // RIGHT → LEFT
        else if (lastTurnType == TurnType.Right)
        {
            animator.playerAnimator.SetTrigger("Left Turn");

            StartCoroutine(
                RotateToSavedRotationAfterDelay(0.7f)
            );
        }

        // BACK → BACK
        else if (lastTurnType == TurnType.Back)
        {
            animator.playerAnimator.SetTrigger("Back Turn");

            StartCoroutine(
                RotateToSavedRotationAfterDelay(1.7f)
            );
        }
    }

    // ==========================================
    // BACK RESET
    // ==========================================

    private IEnumerator RotateToSavedRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        StartCoroutine(
            SmoothRotateToTarget(savedRotation)
        );
    }

    // ==========================================
    // LOOK AT OPPONENT
    // ==========================================

    public void LookAtOpponent()
    {
        if (manager == null || manager.playerType == null)
            return;

        if (GameManager.Instance == null)
            return;

        GameObject opponent;

        if (manager.playerType.isPlayer2)
        {
            opponent = GameManager.Instance.player1Main;
        }
        else
        {
            opponent = GameManager.Instance.player2Main;
        }

        if (opponent == null)
            return;

        Vector3 direction =
            opponent.transform.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        StartCoroutine(
            SmoothRotateToTarget(targetRotation)
        );
    }

    // ==========================================
    // SMOOTH ROTATION
    // ==========================================

    private IEnumerator SmoothRotateToTarget(
        Quaternion targetRotation)
    {
        Quaternion startRotation =
            transform.rotation;

        float time = 0f;

        while (time < smoothLookTime)
        {
            time += Time.deltaTime;

            float t =
                time / smoothLookTime;

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );

            yield return null;
        }

        transform.rotation = targetRotation;
    }
}
