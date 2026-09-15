using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGame1 : MonoBehaviour
{
    [Header("Manager")]
    public MiniGameManager manager;

    [Header("Trap Rows")]
    public TrapRow[] rows;

    [Header("Round Delay")]

    // Thời gian nghỉ ban đầu giữa các round
    public float delayBetweenRounds = 3f;

    // Delay nhỏ nhất giữa các round
    public float minDelay = 1f;

    // Mỗi round giảm bao nhiêu giây
    public float delayDecrease = 0.2f;

    [Header("Start Delay")]

    // Thời gian chờ trước khi minigame bắt đầu
    public float startDelay = 5f;

    [Header("Warning")]

    // Thời gian warning xuất hiện ban đầu
    public float warningShowTime = 0.4f;

    // Thời gian warning nhỏ nhất
    public float minWarningShowTime = 0.1f;

    // Mỗi round giảm bao nhiêu thời gian warning
    public float warningDecrease = 0.05f;

    // Khoảng nghỉ sau khi tắt warning của từng row
    public float warningHideDelay = 0.1f;

    // Thời gian chờ sau tất cả warning trước khi trap chạy
    public float delayBeforeTrap = 0.5f;

    [Header("Runtime")]

    [SerializeField]
    private bool isRunning;

    // Delay hiện tại giữa các round
    [SerializeField]
    private float currentDelay;

    // Thời gian warning hiện tại
    [SerializeField]
    private float currentWarningShowTime;

    private Coroutine gameRoutine;

    // Lưu những row đã chọn ở round trước
    private List<TrapRow> previousSelectedRows =
        new List<TrapRow>();

    // Lưu row bị bỏ ở round trước khi chọn 4/5
    private TrapRow previousExcludedRow;

    public void StartMiniGame()
    {
        StopMiniGame();

        previousSelectedRows.Clear();
        previousExcludedRow = null;

        // Reset độ khó mỗi lần bắt đầu minigame
        currentDelay = delayBetweenRounds;
        currentWarningShowTime = warningShowTime;

        isRunning = true;

        gameRoutine = StartCoroutine(
            RandomRowsRoutine()
        );
    }

    public void StopMiniGame()
    {
        isRunning = false;

        // Dừng cả vòng lặp chính và mọi coroutine phụ từng được
        // khởi chạy bởi MiniGame1.
        StopAllCoroutines();
        gameRoutine = null;

        if (rows == null)
            return;

        foreach (TrapRow row in rows)
        {
            if (row != null)
            {
                row.ResetRow();
            }
        }
    }

    private IEnumerator RandomRowsRoutine()
    {
        yield return new WaitForSeconds(
            startDelay
        );

        while (isRunning)
        {
            List<TrapRow> selectedRows =
                GetRandomRows();

            if (selectedRows.Count == 0)
            {
                break;
            }

            // =========================
            // HIỆN WARNING
            // =========================

            foreach (TrapRow row in selectedRows)
            {
                if (!isRunning)
                    yield break;

                if (row == null)
                    continue;

                row.ShowWarning();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(
                        AudioManager.Instance.warningClip
                    );
                }

                // Dùng thời gian warning hiện tại
                yield return new WaitForSeconds(
                    currentWarningShowTime
                );

                row.HideWarning();

                yield return new WaitForSeconds(
                    warningHideDelay
                );
            }

            if (!isRunning)
                yield break;

            yield return new WaitForSeconds(
                delayBeforeTrap
            );

            // =========================
            // CHẠY TRAP
            // =========================

            foreach (TrapRow row in selectedRows)
            {
                if (!isRunning)
                    yield break;

                if (row == null)
                    continue;

                // Để TrapRow tự sở hữu coroutine của nó.
                // ResetRow() khi đó chắc chắn dừng được hàng đang chạy.
                row.StartRow();

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(
                        AudioManager.Instance.sharkAttackClip
                    );
                }
            }

            // Chờ tất cả row hoàn thành
            yield return StartCoroutine(
                WaitForRowsFinished()
            );

            if (!isRunning)
                yield break;

            // Check Player 1 và Player 2
            if (manager != null)
            {
                CheckPlayerRespawn(
                    manager.currentPlayer1
                );

                CheckPlayerRespawn(
                    manager.currentPlayer2
                );
            }

            // Delay giữa các round
            yield return new WaitForSeconds(
                currentDelay
            );

            // =========================
            // TĂNG ĐỘ KHÓ
            // =========================

            // Giảm delay giữa các round
            currentDelay -= delayDecrease;

            currentDelay = Mathf.Max(
                currentDelay,
                minDelay
            );

            // Giảm thời gian hiện warning
            currentWarningShowTime -= warningDecrease;

            currentWarningShowTime = Mathf.Max(
                currentWarningShowTime,
                minWarningShowTime
            );
        }

        isRunning = false;
        gameRoutine = null;
    }

    private List<TrapRow> GetRandomRows()
    {
        List<TrapRow> availableRows =
            new List<TrapRow>();

        // Thêm row hợp lệ và loại reference bị trùng
        if (rows != null)
        {
            foreach (TrapRow row in rows)
            {
                if (row != null &&
                    !availableRows.Contains(row))
                {
                    availableRows.Add(row);
                }
            }
        }

        if (availableRows.Count == 0)
        {
            return new List<TrapRow>();
        }

        // Ban đầu chọn 3 row
        int rowCount = 3;

        // Khi delay đã chạm mức thấp nhất thì chọn 4 row
        if (currentDelay <= minDelay)
        {
            rowCount = 4;
        }

        rowCount = Mathf.Min(
            rowCount,
            availableRows.Count
        );

        /*
         * Trường hợp có 5 row và chọn 4:
         *
         * Round 1 bỏ row 5
         * Round 2 không được bỏ row 5 nữa
         *
         * Vì vậy row 5 chắc chắn xuất hiện ở round sau.
         */
        if (rowCount == availableRows.Count - 1)
        {
            return GetAllExceptOneRow(
                availableRows
            );
        }

        /*
         * Trường hợp chọn 3 trong 5:
         * Hạn chế trùng quá nhiều với round trước.
         */
        return GetRowsWithLowRepeat(
            availableRows,
            rowCount
        );
    }

    private List<TrapRow> GetAllExceptOneRow(
        List<TrapRow> availableRows
    )
    {
        List<TrapRow> possibleExcludedRows =
            new List<TrapRow>(availableRows);

        // Không cho bỏ lại row đã bị bỏ ở round trước
        if (previousExcludedRow != null &&
            possibleExcludedRows.Count > 1)
        {
            possibleExcludedRows.Remove(
                previousExcludedRow
            );
        }

        TrapRow excludedRow =
            possibleExcludedRows[
                Random.Range(
                    0,
                    possibleExcludedRows.Count
                )
            ];

        previousExcludedRow = excludedRow;

        List<TrapRow> selectedRows =
            new List<TrapRow>(availableRows);

        selectedRows.Remove(
            excludedRow
        );

        ShuffleRows(selectedRows);

        previousSelectedRows =
            new List<TrapRow>(selectedRows);

        return selectedRows;
    }

    private List<TrapRow> GetRowsWithLowRepeat(
        List<TrapRow> availableRows,
        int rowCount
    )
    {
        List<TrapRow> bestResult = null;

        int lowestSameCount =
            int.MaxValue;

        // Thử random 20 lần
        for (int attempt = 0; attempt < 20; attempt++)
        {
            List<TrapRow> shuffledRows =
                new List<TrapRow>(availableRows);

            ShuffleRows(shuffledRows);

            List<TrapRow> candidate =
                shuffledRows.GetRange(
                    0,
                    rowCount
                );

            int sameCount =
                CountSameRows(
                    candidate,
                    previousSelectedRows
                );

            // Lưu kết quả ít trùng nhất
            if (sameCount < lowestSameCount)
            {
                lowestSameCount =
                    sameCount;

                bestResult =
                    new List<TrapRow>(
                        candidate
                    );
            }

            // Chọn 3 trong 5 thì tối thiểu phải trùng 1 row
            if (previousSelectedRows.Count == 0 ||
                sameCount <= 1)
            {
                break;
            }
        }

        if (bestResult == null)
        {
            bestResult =
                new List<TrapRow>();
        }

        previousSelectedRows =
            new List<TrapRow>(
                bestResult
            );

        previousExcludedRow = null;

        return bestResult;
    }

    private int CountSameRows(
        List<TrapRow> first,
        List<TrapRow> second
    )
    {
        int sameCount = 0;

        foreach (TrapRow row in first)
        {
            if (second.Contains(row))
            {
                sameCount++;
            }
        }

        return sameCount;
    }

    private void ShuffleRows(
        List<TrapRow> list
    )
    {
        // Fisher-Yates Shuffle
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex =
                Random.Range(0, i + 1);

            TrapRow temporaryRow =
                list[i];

            list[i] =
                list[randomIndex];

            list[randomIndex] =
                temporaryRow;
        }
    }

    private IEnumerator WaitForRowsFinished()
    {
        while (isRunning)
        {
            bool allFinished = true;

            if (rows != null)
            {
                foreach (TrapRow row in rows)
                {
                    if (row != null &&
                        row.isRunning)
                    {
                        allFinished = false;
                        break;
                    }
                }
            }

            if (allFinished)
            {
                yield break;
            }

            yield return null;
        }
    }

    private void CheckPlayerRespawn(
        GameObject player
    )
    {
        if (player == null)
            return;

        if (player.transform.position.y >= 0.5f)
            return;

        PlayerVFX vfx = player.GetComponent<PlayerVFX>();
        PlayerMiniGame playerMiniGame =
            player.GetComponent<PlayerMiniGame>();
        if (vfx != null)
        {
            StartCoroutine(vfx.DissolveInNoParticleRoutine(0.5f));
        }
        if (playerMiniGame != null)
        {
            playerMiniGame.Respawn();
        }
    }

    private void OnDisable()
    {
        StopMiniGame();
    }
}