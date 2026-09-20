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

    [Header("Tham chiếu PlayerType")]
    public PlayerManager manager;

    [Header("Saved Transform")]
    [SerializeField] private Quaternion savedRotation;
    [SerializeField] private Vector3 savedPosition;

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

    // =========================
    // LOOK AT OPPONENT ON X AXIS
    // =========================
    public void LookAtOpponentOnXAxis()
    {
        if (manager == null || manager.playerType == null)
            return;

        string targetTag = manager.playerType.isPlayer2
            ? "Player 1"
            : "Player 2";

        GameObject opponent = GameObject.FindGameObjectWithTag(targetTag);

        if (opponent == null)
            return;

        Vector3 myPos = transform.position;
        Vector3 opponentPos = opponent.transform.position;

        PlayerAnimator animator = GetComponent<PlayerAnimator>();

        if (animator == null)
            return;
        SaveRotationYPlayer();
        SavePosition();

        // =========================
        // X CỦA BẢN THÂN LỚN HƠN
        // =========================
        if (myPos.x > opponentPos.x)
        {
            lastTurnType = TurnType.Back;

            animator.playerAnimator.SetTrigger("Back Turn");

            StartCoroutine(LookAtOpponentAfterDelay(1.2f));
        }

        // =========================
        // X CỦA BẢN THÂN NHỎ HƠN
        // =========================
        else if (myPos.x < opponentPos.x)
        {
            return;
        }

        // =========================
        // X BẰNG NHAU
        // =========================
        else
        {
            if (myPos.z > opponentPos.z)
            {
                lastTurnType = TurnType.Right;

                animator.playerAnimator.SetTrigger("Right Turn");
            }
            else if (myPos.z < opponentPos.z)
            {
                lastTurnType = TurnType.Left;

                animator.playerAnimator.SetTrigger("Left Turn");
            }
        }
    }

    // =========================
    // DELAY LOOK AT OPPONENT
    // =========================
    private IEnumerator LookAtOpponentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        LookAtOpponent();
    }

    // =========================
    // SAVE ROTATION
    // =========================
    public void SaveRotationYPlayer()
    {
        savedRotation = transform.rotation;
    }

    // =========================
    // SAVE POSITION
    // =========================
    public void SavePosition()
    {
        savedPosition = transform.position;
    }

    // =========================
    // RESET POSITION
    // =========================
    public void ResetPosition()
    {
        transform.position = savedPosition;
    }

    // =========================
    // RESET POSITION AFTER DELAY
    // =========================
    public void ResetPositionAfterDelay(float delay)
    {
        StartCoroutine(ResetPositionDelay(delay));
    }

    private IEnumerator ResetPositionDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        ResetPosition();
    }

    // =========================
    // RESET ROTATION
    // =========================
    public void ResetToSavedRotation()
    {
        PlayerAnimator animator = GetComponent<PlayerAnimator>();

        if (animator == null)
            return;

        // =========================
        // LEFT -> RIGHT
        // =========================
        if (lastTurnType == TurnType.Left)
        {
            animator.playerAnimator.SetTrigger("Right Turn");
            ResetPositionAfterDelay(0.4f);
        }

        // =========================
        // RIGHT -> LEFT
        // =========================
        else if (lastTurnType == TurnType.Right)
        {
            animator.playerAnimator.SetTrigger("Left Turn");
            ResetPositionAfterDelay(0.4f);
        }

        // =========================
        // BACK -> BACK
        // =========================
        else if (lastTurnType == TurnType.Back)
        {
            animator.playerAnimator.SetTrigger("Back Turn");

            StartCoroutine(RotateToSavedRotationAfterDelay(1.2f));
        }
    }

    // =========================
    // DELAY -> SAVED ROTATION
    // =========================
    private IEnumerator RotateToSavedRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetPositionAfterDelay(0f);
        StartCoroutine(SmoothRotateToTarget(savedRotation));
    }

    // =========================
    // LOOK AT OPPONENT SMOOTH
    // =========================
    public void LookAtOpponent()
    {
        if (manager == null || manager.playerType == null)
            return;

        string targetTag = manager.playerType.isPlayer2
            ? "Player 1"
            : "Player 2";

        GameObject opponent = GameObject.FindGameObjectWithTag(targetTag);

        if (opponent == null)
            return;

        Vector3 direction =
            opponent.transform.position - transform.position;

        // Chỉ xoay ngang
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        StartCoroutine(
            SmoothRotateToTarget(targetRotation)
        );
    }

    // =========================
    // SMOOTH ROTATION
    // =========================
    private IEnumerator SmoothRotateToTarget(
        Quaternion targetRotation)
    {
        Quaternion startRotation = transform.rotation;

        float time = 0f;

        while (time < smoothLookTime)
        {
            time += Time.deltaTime;

            float t = time / smoothLookTime;

            // SmoothStep giúp chuyển động mượt hơn
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        // Đảm bảo cuối cùng đúng rotation
        transform.rotation = targetRotation;
    }
}