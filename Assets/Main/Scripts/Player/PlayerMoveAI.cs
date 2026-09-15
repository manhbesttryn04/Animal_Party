using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class PlayerMoveAI : MonoBehaviour
{
    #region Variables

    [Header("Manager")]
    public PlayerManager manager;
    public PlayerTrapState playerTrapState;

    [Header("NavMesh")]
    public NavMeshAgent navMeshAgent;

    [Header("Board")]
    public List<GameObject> pointCheck = new List<GameObject>();
    public int currentIndex = 0;

    [Header("State")]
    public bool isMoving = false;

    // Tín hiệu riêng dành cho camera Cannon Debuff.
    public int activeBoomHitCount = 0;
    public bool IsBoomHitActive => activeBoomHitCount > 0;

    #endregion

    #region Unity Events

    private void Start()
    {
        SetupPlayerAI();
        FindPoint();
    }

    private void SetupPlayerAI()
    {
        playerTrapState = GetComponent<PlayerTrapState>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        navMeshAgent.avoidancePriority = 50;
        navMeshAgent.updateRotation = false;

        PointCheck boardPoint = FindAnyObjectByType<PointCheck>();
        if (boardPoint != null)
        {
            pointCheck = boardPoint.point.ToList(); // sửa lại đúng tên list trong PointCheck
        }
    }


    #endregion

    #region Move Main

    public void StartMove(int value)
    {
        if (isMoving)
            return;

        if (CheckFinishIndex())
            return;

        StartCoroutine(AIToPoint(value));
    }

    public IEnumerator AIToPoint(int value)
    {
        isMoving = true;

        navMeshAgent.speed = 4f;
        navMeshAgent.acceleration = 8f;

        int finishIndex = pointCheck.Count - 1;

        int stepCanMove = Mathf.Min(
            value,
            finishIndex - currentIndex
        );

        for (int i = 0; i <= stepCanMove; i++)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(
                    AudioManager.Instance.walkPlayerClip
                );
            }

            currentIndex++;

            GameObject target = pointCheck[currentIndex];

            Vector3 finalPosition =
                target.transform.position +
                GetPlayerOffset();

            yield return StartCoroutine(
                JumpTo(finalPosition)
            );

            if (CheckFinishIndex())
            {
                StopAllCameraFollow();
                isMoving = false;
                yield break;
            }
        }

        yield return new WaitForSeconds(0.5f);

        LookNextPoint();

        if (manager.playerBuff.isBuffDice > 0)
        {
            manager.playerBuff.isBuffDice--;

            yield return StartCoroutine(MoveBonus());
            yield break;
        }

        // Chỉ kiểm tra ô sau khi đã đi đủ số bước.
        if (playerTrapState != null)
        {
            yield return StartCoroutine(
                playerTrapState.CheckCurrentTile()
            );
        }

        if (CheckFinishIndex())
        {
            StopAllCameraFollow();
            isMoving = false;
            yield break;
        }

        EndTurn();
    }

    #endregion

    #region Bonus Move

    public IEnumerator MoveBonus()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySpecial(
                AudioManager.Instance.bonusDiceVoiceClip
            );
        }

        if (UIManager.Instance != null)
        {
            StartCoroutine(
                UIManager.Instance.HideBonusPanel()
            );
        }

        yield return new WaitForSeconds(1.7f);

        int finishIndex = pointCheck.Count - 1;

        int bonusStep = Mathf.Min(
            2,
            finishIndex - currentIndex
        );

        for (int i = 0; i < bonusStep; i++)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(
                    AudioManager.Instance.walkPlayerClip
                );
            }

            currentIndex++;

            GameObject target = pointCheck[currentIndex];

            Vector3 finalPosition =
                target.transform.position +
                GetPlayerOffset();

            yield return StartCoroutine(
                JumpTo(finalPosition)
            );

            if (CheckFinishIndex())
            {
                StopAllCameraFollow();
                isMoving = false;
                yield break;
            }

            yield return new WaitForSeconds(0.3f);
        }

        LookNextPoint();

        if (playerTrapState != null)
        {
            yield return StartCoroutine(
                playerTrapState.CheckCurrentTile()
            );
        }

        if (CheckFinishIndex())
        {
            StopAllCameraFollow();
            isMoving = false;
            yield break;
        }

        EndTurn();
    }

    #endregion

    #region Win Check

    public bool CheckFinishIndex()
    {
        if (pointCheck == null ||
            pointCheck.Count == 0)
        {
            return false;
        }

        int finishIndex = pointCheck.Count - 1;

        if (currentIndex < finishIndex)
            return false;

        currentIndex = finishIndex;

        bool isPlayer2 =
            manager.playerType.isPlayer2;

        bool hasWin =
            GameManager.Instance != null &&
            GameManager.Instance.CheckWinnerByIndex(
                isPlayer2,
                currentIndex
            );

        if (hasWin)
        {
            isMoving = false;

            if (navMeshAgent != null &&
                navMeshAgent.enabled)
            {
                navMeshAgent.ResetPath();
            }
        }

        return hasWin;
    }

    #endregion

    #region End Turn

    private void StopAllCameraFollow()
    {
        if (manager == null ||
            manager.playerCamera == null)
        {
            return;
        }

        manager.playerCamera.isFllow2 = false;
        manager.playerCamera.isFllow3 = false;
    }

    public void EndTurn()
    {
        manager.playerCamera.isFllow2 = false;
        manager.playerCamera.isFllow3 = true;

        StartCoroutine(EndTurnRoutine());
    }

    private IEnumerator EndTurnRoutine()
    {
        yield return new WaitForSeconds(1f);

        manager.playerCamera.isFllow3 = false;

        isMoving = false;

        if (!manager.playerRound.isRound1)
        {
            manager.playerRound.isRound1 = true;
        }

        if (!manager.playerRound.nextRound)
        {
            manager.playerRound.nextRound = true;
        }
    }

    #endregion

    #region Effects

    public IEnumerator BoomHitEffect(int power)
    {
        // Mỗi lần bom kích hoạt sẽ tăng bộ đếm.
        // Nếu gặp tiếp một quả bom khác thì camera vẫn tiếp tục bám.
        activeBoomHitCount++;
        isMoving = true;

        try
        {
            if (manager.playerAnimator != null)
            {
                manager.playerAnimator
                    .playerAnimator
                    .SetTrigger("HitBomb");
            }

            currentIndex = Mathf.Max(
                0,
                currentIndex - power
            );

            GameObject targetPoint =
                pointCheck[currentIndex];

            Vector3 finalPosition =
                targetPoint.transform.position +
                GetPlayerOffset();

            navMeshAgent.speed = 25f;
            navMeshAgent.acceleration = 999f;

            navMeshAgent.SetDestination(finalPosition);
            if (playerTrapState != null)
            {
                yield return StartCoroutine(
                    playerTrapState.CheckCurrentTile()
                );
            }
          

            // Chờ NavMesh tính đường.
            while (navMeshAgent.enabled &&
                   navMeshAgent.pathPending)
            {
                yield return null;
            }

            // Đợi người chơi thật sự đến ô đích.
            float moveTimeout = 5f;

            while (navMeshAgent.enabled &&
                   navMeshAgent.hasPath &&
                   navMeshAgent.remainingDistance >
                   navMeshAgent.stoppingDistance &&
                   moveTimeout > 0f)
            {
                moveTimeout -= Time.deltaTime;
                yield return null;
            }

            navMeshAgent.speed = 4f;
            navMeshAgent.acceleration = 8f;

            if (navMeshAgent.enabled)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.Warp(transform.position);
            }

            // Chỉ kiểm tra bomb, coin hoặc teleport
            // sau khi đã đáp đến ô đích.
           
        }
        finally
        {
            activeBoomHitCount = Mathf.Max(
                0,
                activeBoomHitCount = 0
            );

            // Chỉ kết thúc khi toàn bộ chuỗi bom đã chạy xong.
            isMoving = activeBoomHitCount > 0;
        }
    }

    public IEnumerator TeleportEffect(int targetIndex)
    {
        isMoving = true;

        currentIndex = Mathf.Clamp(
            targetIndex,
            0,
            pointCheck.Count - 1
        );

        GameObject targetPoint =
            pointCheck[currentIndex];

        Vector3 finalPosition =
            targetPoint.transform.position +
            GetPlayerOffset();

        navMeshAgent.enabled = false;

        transform.position = finalPosition;

        navMeshAgent.enabled = true;
        navMeshAgent.Warp(finalPosition);

        if (CheckFinishIndex())
        {
            isMoving = false;
            yield break;
        }

        isMoving = false;
        yield return null;
    }

    private IEnumerator JumpTo(Vector3 targetPosition)
    {
        navMeshAgent.enabled = false;

        Vector3 startPosition = transform.position;

        manager.playerAnimator
            .playerAnimator
            .SetTrigger("Jump");

        while (!manager.playerAnimator
                   .playerAnimator
                   .GetCurrentAnimatorStateInfo(0)
                   .IsName("Jump"))
        {
            yield return null;
        }

        float duration = 0.4f;
        float height = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float percent = Mathf.Clamp01(
                elapsed / duration
            );

            Vector3 position = Vector3.Lerp(
                startPosition,
                targetPosition,
                percent
            );

            position.y +=
                Mathf.Sin(percent * Mathf.PI) *
                height;

            transform.position = position;

            yield return null;
        }

        transform.position = targetPosition;

        while (manager.playerAnimator
                   .playerAnimator
                   .GetCurrentAnimatorStateInfo(0)
                   .IsName("Jump"))
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        navMeshAgent.enabled = true;
        navMeshAgent.Warp(targetPosition);
    }

    #endregion

    #region Utility

    public void FindPoint()
    {
        if (pointCheck == null)
        {
            pointCheck = new List<GameObject>();
        }

        while (pointCheck.Count < 33)
        {
            pointCheck.Add(null);
        }

        for (int i = 0; i < 33; i++)
        {
            GameObject point =
                GameObject.Find($"Point {i + 1}");

            if (point != null)
            {
                pointCheck[i] = point;
            }
        }
    }

    private Vector3 GetPlayerOffset()
    {
        return manager.playerType.isPlayer2
            ? new Vector3(0f, 0f, -0.3f)
            : new Vector3(0f, 0f, 0.3f);
    }

    private void LookNextPoint()
    {
        if (currentIndex + 1 >= pointCheck.Count)
            return;

        GameObject nextPoint =
            pointCheck[currentIndex + 1];

        if (nextPoint != null)
        {
            transform.LookAt(
                nextPoint.transform.position
            );
        }
    }

    #endregion
}