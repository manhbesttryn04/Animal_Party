using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGame4 : MonoBehaviour
{
    [Header("Manager")]
    public MiniGameManager manager;

    [Header("Pirate")]
    public Animator animator;

    [Header("Settings")]
    public float firstWaitTime = 3f;
    public float watchTime = 5f;
    public float rotateSpeed = 5f;
    public float fastAimSpeed = 25f;

    [Header("Player Speed")]
    public float startMoveSpeed = 1f;
    public float detectedMoveSpeed = 0.3f;

    [Header("Detect")]
    public float moveTolerance = 0.05f;
    public float freezeDelayAfterDetected = 0.18f;

    [Header("Shoot Fake Bullet")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 80f;
    public float bulletHitDistance = 0.15f;
    public float bulletTargetHeight = 1.0f;

    [Header("Finish Line")]
    public float finishLineZ;

    [Header("VFX")]
    public GameObject muzzleFlash;
    public float blinkTime = 1f;
    public float blinkSpeed = 0.12f;

    private bool isRunning;
    private bool isWatching;
    private bool isAttacking;
    private bool hasLaughThisWatch;

    public bool IsFinishingSequence { get; private set; }

    private GameObject currentAttackTarget;
    private bool isWaitingAttackEvent;
    private bool hasSetEndTimer;
    private bool hasGivenReward;

    private Quaternion backRotation;
    private Quaternion lookRotation;

    private Queue<GameObject> attackQueue = new Queue<GameObject>();

    private HashSet<GameObject> detectedPlayers = new HashSet<GameObject>();
    private HashSet<GameObject> deadPlayers = new HashSet<GameObject>();
    private List<GameObject> finishedPlayers = new List<GameObject>();

    // Chỉ dùng để dọn những viên đạn còn tồn tại khi minigame dừng.
    // Không thay đổi cách bay hoặc cách gây chết của đạn.
    private HashSet<GameObject> activeBullets = new HashSet<GameObject>();

    private Dictionary<GameObject, Vector3> redStartPositions =
        new Dictionary<GameObject, Vector3>();

    private Quaternion startRotation;

    private void Start()
    {
        startRotation = transform.rotation;
        backRotation = Quaternion.Euler(0f, 0f, 0f);
        lookRotation = backRotation * Quaternion.Euler(0f, 180f, 0f);
    }

    public void StartMiniGame()
    {
        if (isRunning || IsFinishingSequence) return;

        StopAllCoroutines();

        // Dọn phòng trường hợp một viên đạn cũ còn sót từ lần chơi trước.
        ClearActiveBullets();

        isWatching = false;
        isAttacking = false;
        hasLaughThisWatch = false;
        isWaitingAttackEvent = false;
        currentAttackTarget = null;

        hasSetEndTimer = false;
        hasGivenReward = false;
        IsFinishingSequence = false;

        attackQueue.Clear();
        detectedPlayers.Clear();
        deadPlayers.Clear();
        finishedPlayers.Clear();
        redStartPositions.Clear();

        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);

        if (animator != null)
            animator.ResetTrigger("Attack");

        SetUpAllPlayer();
        isRunning = true;
        StartCoroutine(PirateRoutine());
    }

    public void StopMiniGame()
    {
        isRunning = false;
        isWatching = false;
        isAttacking = false;
        hasLaughThisWatch = false;
        isWaitingAttackEvent = false;
        IsFinishingSequence = false;
        currentAttackTarget = null;

        StopAllCoroutines();
        ClearActiveBullets();

        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);

        if (animator != null)
            animator.ResetTrigger("Attack");

        AudioManager.Instance.StopEnvironment();
        AudioManager.Instance.StopSpecial();


        // Tính thưởng trước khi Clear
        if (!hasGivenReward && manager != null)
        {
            hasGivenReward = true;
            CheckFinishReward(manager.currentPlayer1);
            CheckFinishReward(manager.currentPlayer2);
        }

        attackQueue.Clear();
        detectedPlayers.Clear();
        deadPlayers.Clear();
        finishedPlayers.Clear();
        redStartPositions.Clear();

        hasSetEndTimer = false;

        transform.rotation = startRotation;

        // Khôi phục Layer Overrides (xoá layer của người kia khỏi danh sách Exclude)
        if (manager.currentPlayer1 != null && manager.currentPlayer2 != null)
        {
            CharacterController col1 = manager.currentPlayer1.GetComponent<CharacterController>();
            CharacterController col2 = manager.currentPlayer2.GetComponent<CharacterController>();

            if (col1 != null && col2 != null)
            {
                col1.excludeLayers &= ~(1 << manager.currentPlayer2.layer);
                col2.excludeLayers &= ~(1 << manager.currentPlayer1.layer);
            }
        }
    }

    IEnumerator PirateRoutine()
    {

        transform.rotation = backRotation;

        yield return new WaitForSeconds(firstWaitTime);

        while (isRunning)
        {
            attackQueue.Clear();
            detectedPlayers.Clear();
            redStartPositions.Clear();
            hasLaughThisWatch = false;
            AudioManager.Instance.PlaySFX(AudioManager.Instance.scanPiratesClip);

            yield return StartCoroutine(RotateTo(lookRotation, rotateSpeed));

            isWatching = true;

            SaveRedStartPosition(manager.currentPlayer1);
            SaveRedStartPosition(manager.currentPlayer2);

            float timer = 0f;

            while (timer < watchTime)
            {
                CheckFinish(manager.currentPlayer1);
                CheckFinish(manager.currentPlayer2);

                // Nếu cả hai đã chết thì không cần tiếp tục thời gian quan sát.
                if (AreBothPlayersDead())
                    break;

                CheckPlayer(manager.currentPlayer1);
                CheckPlayer(manager.currentPlayer2);

                if (!isAttacking && attackQueue.Count > 0)
                    StartCoroutine(ProcessAttackQueue());

                timer += Time.deltaTime;
                yield return null;
            }

            isWatching = false;

            while (isAttacking ||
                   attackQueue.Count > 0 ||
                   activeBullets.Count > 0)
            {
                if (!isAttacking && attackQueue.Count > 0)
                    StartCoroutine(ProcessAttackQueue());

                yield return null;
            }
            AudioManager.Instance.StopSpecial();

            // Giữ nguyên hướng cướp biển sau phát bắn cuối.
            // Không quay lưng và không bắt đầu nhịp xanh khi cả hai đã chết.
            if (AreBothPlayersDead())
            {
                isRunning = false;
                yield break;
            }

            yield return StartCoroutine(RotateTo(backRotation, rotateSpeed));

            int randomGreenTime = Random.Range(2, 5);

            PlayRandomPirateVoice(randomGreenTime);

            yield return new WaitForSeconds(randomGreenTime);
        }
    }

    void CheckPlayer(GameObject playerObj)
    {
        if (playerObj == null) return;
        if (!isWatching) return;
        if (finishedPlayers.Contains(playerObj)) return;
        if (deadPlayers.Contains(playerObj)) return;
        if (detectedPlayers.Contains(playerObj)) return;

        PlayerMove move = playerObj.GetComponent<PlayerMove>();
        if (move == null) return;

        bool isMovingByInput = move.IsMoving;
        bool isMovingByPosition = HasMovedFromRedStart(playerObj);

        if (isMovingByInput || isMovingByPosition)
        {
            detectedPlayers.Add(playerObj);

            move.speed = detectedMoveSpeed;

            attackQueue.Enqueue(playerObj);

            if (!isAttacking)
                StartCoroutine(ProcessAttackQueue());
        }
    }

    IEnumerator ProcessAttackQueue()
    {
        isAttacking = true;

        if (!hasLaughThisWatch)
        {
            hasLaughThisWatch = true;

            AudioManager.Instance.StopEnvironment();
            AudioManager.Instance.PlaySpecialOneShot(AudioManager.Instance.laughPiratesClip);

        }

        while (attackQueue.Count > 0)
        {
            GameObject target = attackQueue.Dequeue();

            if (target != null &&
                !deadPlayers.Contains(target) &&
                !finishedPlayers.Contains(target))
            {
                yield return StartCoroutine(AttackPlayerPirate(target));
            }
        }

        isAttacking = false;
    }

    IEnumerator AttackPlayerPirate(GameObject target)
    {
        if (target == null) yield break;

        PlayerMove move = target.GetComponent<PlayerMove>();
        PlayerAnimator playerAnimator = target.GetComponent<PlayerAnimator>();

        if (move != null)
            move.speed = detectedMoveSpeed;

        yield return new WaitForSeconds(freezeDelayAfterDetected);

        if (move != null)
            move.isJumpAndMove = false;

        if (playerAnimator != null && playerAnimator.playerAnimator != null)
            playerAnimator.playerAnimator.SetFloat("Walk", 0f);

        Vector3 dir = target.transform.position - transform.position;
        dir.y = 0f;

        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);

            while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    fastAimSpeed * Time.deltaTime
                );

                yield return null;
            }

            transform.rotation = targetRot;
        }

        currentAttackTarget = target;
        isWaitingAttackEvent = true;

        if (animator != null)
            animator.SetTrigger("Attack");
        else
            PiraterAttack();

        float attackEventWait = 2f;

        while (isWaitingAttackEvent && attackEventWait > 0f)
        {
            attackEventWait -= Time.deltaTime;
            yield return null;
        }

        // Dự phòng nếu animation không có event PiraterAttack.
        if (isWaitingAttackEvent)
            PiraterAttack();

        currentAttackTarget = null;
    }

    // GỌI HÀM NÀY TRONG ANIMATION EVENT ATTACK
    public void PiraterAttack()
    {
        // Animation Event cũ không được phép bắn sau khi minigame đã dừng.
        if (!isRunning && !IsFinishingSequence)
            return;

        StartCoroutine(ShowMuzzleFlash());

        if (currentAttackTarget != null)
            ShootFakeBullet(currentAttackTarget);

        isWaitingAttackEvent = false;
    }

    void ShootFakeBullet(GameObject target)
    {
        if (target == null)
            return;

        if (bulletPrefab == null || firePoint == null)
        {
            KillFakePlayer(target);
            return;
        }

        Vector3 targetPos =
            target.transform.position + Vector3.up * bulletTargetHeight;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        if (bullet != null)
            activeBullets.Add(bullet);

        StartCoroutine(FakeBulletFly(bullet, target, targetPos));
    }

    IEnumerator FakeBulletFly(GameObject bullet, GameObject target, Vector3 targetPos)
    {
        if (bullet == null)
        {
            KillFakePlayer(target);
            yield break;
        }

        while (Vector3.Distance(bullet.transform.position, targetPos) > bulletHitDistance)
        {
            Vector3 dir = targetPos - bullet.transform.position;

            if (dir != Vector3.zero)
                bullet.transform.rotation = Quaternion.LookRotation(dir);

            bullet.transform.position = Vector3.MoveTowards(
                bullet.transform.position,
                targetPos,
                bulletSpeed * Time.deltaTime
            );

            yield return null;
        }

        activeBullets.Remove(bullet);
        Destroy(bullet);
        KillFakePlayer(target);
    }

    public void BeginTimeoutSequence()
    {
        if (IsFinishingSequence)
            return;

        // Ghi nhận người vừa qua đích đúng thời điểm đồng hồ về 00:00.
        if (manager != null)
        {
            CheckFinish(manager.currentPlayer1);
            CheckFinish(manager.currentPlayer2);
        }

        isRunning = false;
        isWatching = false;
        isAttacking = false;
        isWaitingAttackEvent = false;
        currentAttackTarget = null;

        // Dừng vòng quay bình thường và các đòn tấn công đang chạy.
        StopAllCoroutines();
        ClearActiveBullets();

        attackQueue.Clear();
        detectedPlayers.Clear();
        redStartPositions.Clear();

        StartCoroutine(TimeoutShootSequence());
    }

    IEnumerator TimeoutShootSequence()
    {
        IsFinishingSequence = true;

        // Khóa ngay những người chưa về đích để họ không chạy thêm sau 00:00.
        LockUnfinishedPlayer(manager != null ? manager.currentPlayer1 : null);
        LockUnfinishedPlayer(manager != null ? manager.currentPlayer2 : null);

        AudioManager.Instance.StopSpecial();
        AudioManager.Instance.StopEnvironment();

        GameObject player1 = manager != null ? manager.currentPlayer1 : null;
        GameObject player2 = manager != null ? manager.currentPlayer2 : null;

        if (MustBeShotAtTimeout(player1))
            yield return StartCoroutine(ShootTimeoutPlayer(player1));

        if (MustBeShotAtTimeout(player2))
            yield return StartCoroutine(ShootTimeoutPlayer(player2));

        // Một khoảng ngắn để animation chết hiện rõ trước khi bảng kết quả mở.
        yield return new WaitForSeconds(0.5f);

        IsFinishingSequence = false;
    }

    bool MustBeShotAtTimeout(GameObject player)
    {
        if (player == null)
            return false;

        return !finishedPlayers.Contains(player) &&
               !deadPlayers.Contains(player);
    }

    void LockUnfinishedPlayer(GameObject player)
    {
        if (!MustBeShotAtTimeout(player))
            return;

        PlayerMove move = player.GetComponent<PlayerMove>();
        if (move != null)
        {
            move.isJumpAndMove = false;
            move.isWalk = false;
        }

        PlayerAnimator playerAnimator = player.GetComponent<PlayerAnimator>();
        if (playerAnimator != null && playerAnimator.playerAnimator != null)
            playerAnimator.playerAnimator.SetFloat("Walk", 0f);
    }

    IEnumerator ShootTimeoutPlayer(GameObject player)
    {
        if (!MustBeShotAtTimeout(player))
            yield break;

        yield return StartCoroutine(AttackPlayerPirate(player));

        // AttackPlayerPirate kết thúc khi animation event bắn đạn được gọi.
        // Chờ đạn thật sự chạm player và KillFakePlayer hoàn tất.
        float maxWaitTime = 5f;

        while (MustBeShotAtTimeout(player) && maxWaitTime > 0f)
        {
            maxWaitTime -= Time.deltaTime;
            yield return null;
        }

        // Dự phòng nếu prefab đạn/animation event gặp lỗi.
        if (MustBeShotAtTimeout(player))
            KillFakePlayer(player);
    }

    void KillFakePlayer(GameObject target)
    {
        if (target == null) return;
        if (finishedPlayers.Contains(target)) return;
        if (deadPlayers.Contains(target)) return;

        deadPlayers.Add(target);

        // Khóa di chuyển vĩnh viễn trong minigame này
        PlayerMove move = target.GetComponent<PlayerMove>();
        if (move != null)
        {
            move.isJumpAndMove = false;
            move.isWalk = false;
        }

        // Chỉ hiện animation chết, không hồi sinh và không dùng VFX
        PlayerAnimator playerAnimator = target.GetComponent<PlayerAnimator>();
        if (playerAnimator != null && playerAnimator.playerAnimator != null)
        {
            playerAnimator.playerAnimator.SetFloat("Walk", 0f);
            playerAnimator.playerAnimator.SetBool("Die", true);
        }

        CheckEndCondition();
    }

    bool IsPlayerDone(GameObject player)
    {
        if (player == null) return false;

        return deadPlayers.Contains(player) ||
               finishedPlayers.Contains(player);
    }

    bool AreBothPlayersDead()
    {
        if (manager == null ||
            manager.currentPlayer1 == null ||
            manager.currentPlayer2 == null)
        {
            return false;
        }

        return deadPlayers.Contains(manager.currentPlayer1) &&
               deadPlayers.Contains(manager.currentPlayer2);
    }

    void CheckEndCondition()
    {
        if (hasSetEndTimer || manager == null) return;

        bool player1Done = IsPlayerDone(manager.currentPlayer1);
        bool player2Done = IsPlayerDone(manager.currentPlayer2);

        // Cả hai đã chết, đã về đích, hoặc một chết và một về đích
        if (player1Done && player2Done)
        {
            hasSetEndTimer = true;
            manager.timer = 2f;
        }
    }

    void SaveRedStartPosition(GameObject playerObj)
    {
        if (playerObj == null) return;
        if (finishedPlayers.Contains(playerObj)) return;
        if (deadPlayers.Contains(playerObj)) return;

        redStartPositions[playerObj] = playerObj.transform.position;
    }

    bool HasMovedFromRedStart(GameObject playerObj)
    {
        if (!redStartPositions.ContainsKey(playerObj))
            return false;

        Vector3 startPos = redStartPositions[playerObj];
        Vector3 currentPos = playerObj.transform.position;

        startPos.y = 0f;
        currentPos.y = 0f;

        return Vector3.Distance(startPos, currentPos) > moveTolerance;
    }

    void CheckFinish(GameObject player)
    {
        if (player == null) return;
        if (finishedPlayers.Contains(player)) return;
        if (detectedPlayers.Contains(player)) return;
        if (deadPlayers.Contains(player)) return;

        // Nếu player chạy theo hướng Z giảm
        if (player.transform.position.z >= finishLineZ)
        {
            finishedPlayers.Add(player);

            PlayerMove move = player.GetComponent<PlayerMove>();
            if (move != null)
            {
                move.isJumpAndMove = false;
            }

            PlayerAnimator playerAnimator = player.GetComponent<PlayerAnimator>();
            if (playerAnimator != null && playerAnimator.playerAnimator != null)
            {
                playerAnimator.playerAnimator.SetFloat("Walk", 0f);
                playerAnimator.playerAnimator.SetBool("Die", false);
            }

            // Quay player về phía sau
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            CheckEndCondition();
        }
    }


    void CheckFinishReward(GameObject player)
    {
        if (player == null) return;

        PlayerMiniGame mini = player.GetComponent<PlayerMiniGame>();
        if (mini == null) return;

        if (finishedPlayers.Contains(player))
        {
            int finishRank = finishedPlayers.IndexOf(player);
            if (finishRank == 0)
            {
                mini.UpCoin(1, 200); // 1st place gets more
            }
            else
            {
                mini.UpCoin(1, 100); // 2nd place gets normal amount
            }
        }
        else
        {
            mini.UpCoin(0, 100);
        }
    }

    IEnumerator RotateTo(Quaternion targetRotation, float speed)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                speed * Time.deltaTime
            );

            yield return null;
        }

        transform.rotation = targetRotation;
    }

    void PlayRandomPirateVoice(int random)
    {
        // 2 -> index 0
        // 3 -> index 1
        // 4 -> index 2
        int clipIndex = random - 2;

        AudioClip[] clipList = AudioManager.Instance.piratesSingClipList;

        if (clipList == null || clipList.Length == 0)
            return;

        if (clipIndex < 0 || clipIndex >= clipList.Length)
            return;

        AudioManager.Instance.PlaySpecialOneShot(clipList[clipIndex]);
        // Debug.Log(clipIndex);

    }

    IEnumerator ShowMuzzleFlash()
    {
        if (muzzleFlash != null)
            muzzleFlash.SetActive(true);

        AudioManager.Instance.PlaySFX(AudioManager.Instance.gunShotPiratesClip);

        yield return new WaitForSeconds(0.15f);

        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }

    void ClearActiveBullets()
    {
        foreach (GameObject bullet in activeBullets)
        {
            if (bullet != null)
                Destroy(bullet);
        }

        activeBullets.Clear();
    }

    public void SetUpAllPlayer()
    {
        SetUpPlayer(manager.currentPlayer1);
        SetUpPlayer(manager.currentPlayer2);

        // Sử dụng Layer Overrides để bỏ qua va chạm với layer của người kia
        if (manager.currentPlayer1 != null && manager.currentPlayer2 != null)
        {
            CharacterController col1 = manager.currentPlayer1.GetComponent<CharacterController>();
            CharacterController col2 = manager.currentPlayer2.GetComponent<CharacterController>();

            if (col1 != null && col2 != null)
            {
                col1.excludeLayers |= (1 << manager.currentPlayer2.layer);
                col2.excludeLayers |= (1 << manager.currentPlayer1.layer);
            }
        }
    }

    void SetUpPlayer(GameObject playerObj)
    {
        if (playerObj == null) return;

        PlayerMove move = playerObj.GetComponent<PlayerMove>();
        PlayerAnimator playerAnimator = playerObj.GetComponent<PlayerAnimator>();

        if (move != null)
        {
            move.speed = startMoveSpeed;
            move.isJumpAndMove = true;
            move.isWalk = true;
        }

        if (playerAnimator != null && playerAnimator.playerAnimator != null)
            playerAnimator.playerAnimator.SetBool("Die", false);
    }
}