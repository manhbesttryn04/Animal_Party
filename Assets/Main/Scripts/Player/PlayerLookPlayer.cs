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

    [Header("Manager")]
    public PlayerManager manager;

    [Header("Saved Rotation")]
    [SerializeField] private Quaternion savedRotation;

    [Header("Smooth Look")]
    [SerializeField] private float smoothLookTime = 0.25f;


    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        SetUpPlayerLookPlayer();
    }


    // =====================================================
    // SETUP
    // =====================================================

    private void SetUpPlayerLookPlayer()
    {
        manager = GetComponent<PlayerManager>();
    }


    // =====================================================
    // LOOK AT OPPONENT ON X AXIS
    // =====================================================

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

        // =================================================
        // PLAYER ĐỨNG TRƯỚC
        // =================================================

        if (myMoveAI.currentIndex > opponentMoveAI.currentIndex)
        {
            lastTurnType = TurnType.Back;

            animator.playerAnimator.SetTrigger("Back Turn");

            StartCoroutine(
                LookAtOpponentAfterDelay(1.4f)
            );
        }

        // =================================================
        // PLAYER ĐỨNG SAU
        // =================================================

        else if (myMoveAI.currentIndex < opponentMoveAI.currentIndex)
        {
            return;
        }

        // =================================================
        // CÙNG INDEX
        // =================================================

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



    // =====================================================
    // LOOK AT ATTACKER
    // =====================================================

    public void LookAtAttacker()
    {
        if (manager == null || manager.playerType == null)
            return;

        if (GameManager.Instance == null)
            return;

        PlayerMoveAI myMoveAI = GetComponent<PlayerMoveAI>();

        if (myMoveAI == null)
            return;

        GameObject attacker;

        // Tìm kẻ tấn công
        if (manager.playerType.isPlayer2)
        {
            attacker = GameManager.Instance.player1Main;
        }
        else
        {
            attacker = GameManager.Instance.player2Main;
        }

        if (attacker == null)
            return;

        PlayerMoveAI attackerMoveAI =
            attacker.GetComponent<PlayerMoveAI>();

        if (attackerMoveAI == null)
            return;

        PlayerAnimator animator =
            GetComponent<PlayerAnimator>();

        if (animator == null)
            return;


        // =================================================
        // MÌNH ĐỨNG SAU KẺ TẤN CÔNG
        // =================================================

        if (myMoveAI.currentIndex < attackerMoveAI.currentIndex)
        {
            return;
        }


        // =================================================
        // CÙNG INDEX
        // =================================================

        if (myMoveAI.currentIndex == attackerMoveAI.currentIndex)
        {
            SaveRotationYPlayer();


            // =============================================
            // DEFENSE BUFF
            //
            // P1 -> LEFT
            // P2 -> RIGHT
            //
            // Quay cùng hướng với kẻ tấn công
            // =============================================

            if (manager.playerBuff != null &&
                manager.playerBuff.isBuffDeffense)
            {
                if (manager.playerType.isPlayer2)
                {
                    lastTurnType = TurnType.Right;

                    // animator.playerAnimator.SetTrigger("Right Turn");
                }
                else
                {
                    lastTurnType = TurnType.Left;

                    // animator.playerAnimator.SetTrigger("Left Turn");
                }

                StartCoroutine(
                    LookAtDefenseDirectionAfterDelay(
                        0f,
                        attacker
                    )
                );

                return;
            }
            else if(manager.playerBuff != null && manager.playerBuff.isBuffMagic)
            {
                if (manager.playerType.isPlayer2)
                {
                    lastTurnType = TurnType.Left;

                    // animator.playerAnimator.SetTrigger("Right Turn");
                }
                else
                {
                    lastTurnType = TurnType.Right;

                    // animator.playerAnimator.SetTrigger("Left Turn");
                }
                StartCoroutine(LookAtOpponentAfterDelay(0f));
                return;
            }
        }

        SaveRotationYPlayer();

        lastTurnType = TurnType.Back;

        StartCoroutine(LookAtOpponentAfterDelay(0f));
    }
        // =====================================================
        // RESET TURN
        // =====================================================

    public void ResetToSavedRotation()
    {
        PlayerAnimator animator =
            manager.playerAnimator;

        if (animator == null)
            return;


        // LEFT -> RIGHT

        if (lastTurnType == TurnType.Left)
        {
            animator.playerAnimator.SetTrigger("Right Turn");

            StartCoroutine(
                RotateToSavedRotationAfterDelay(0.7f)
            );
        }


        // RIGHT -> LEFT

        else if (lastTurnType == TurnType.Right)
        {
            animator.playerAnimator.SetTrigger("Left Turn");

            StartCoroutine(
                RotateToSavedRotationAfterDelay(0.7f)
            );
        }


        // BACK -> BACK

        else if (lastTurnType == TurnType.Back)
        {
            animator.playerAnimator.SetTrigger("Back Turn");

            StartCoroutine(
                RotateToSavedRotationAfterDelay(1.4f)
            );
        }
    }


    // =====================================================
    // SAVE ROTATION
    // =====================================================

    public void SaveRotationYPlayer()
    {
        savedRotation = transform.rotation;
    }


    // =====================================================
    // COROUTINE
    // =====================================================

    private IEnumerator LookAtOpponentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        LookAtOpponent();
    }


    private IEnumerator LookAtDefenseDirectionAfterDelay(
     float delay,
     GameObject attacker)
    {
        yield return new WaitForSeconds(delay);

        if (attacker == null)
            yield break;

        // Player quay cùng hướng với kẻ tấn công
        Quaternion targetRotation =
            attacker.transform.rotation;

        yield return StartCoroutine(
            SmoothRotateToTarget(targetRotation)
        );
    }


    private IEnumerator RotateToSavedRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        yield return StartCoroutine(
            SmoothRotateToTarget(savedRotation)
        );
    }


    // =====================================================
    // LOOK TARGET
    // =====================================================

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


    // =====================================================
    // SMOOTH ROTATION
    // =====================================================

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
