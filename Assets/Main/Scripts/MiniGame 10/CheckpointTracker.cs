using UnityEngine;

// Gắn lên xe (cùng GameObject với CarControl), đóng vai trò tương tự BombCarrier trong MiniGame6
public class CheckpointTracker : MonoBehaviour
{
    [HideInInspector] public int currentCheckpointIndex = -1; // -1 = chưa qua checkpoint nào
    [HideInInspector] public bool hasFinished = false;

    private bool gameActive = false;

    public void SetGameActive(bool active)
    {
        gameActive = active;
        if (active)
        {
            currentCheckpointIndex = -1;
            hasFinished = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!gameActive || hasFinished) return;

        Checkpoint cp = other.GetComponent<Checkpoint>();
        if (cp == null) return;

        // Chỉ tính khi đi đúng thứ tự (index kế tiếp), tránh chạy tắt qua vạch đích
        if (cp.index == currentCheckpointIndex + 1)
        {
            currentCheckpointIndex = cp.index;

            if (RaceMiniGame.Instance != null && cp.index == RaceMiniGame.Instance.finishCheckpointIndex)
            {
                hasFinished = true;
                RaceMiniGame.Instance.OnPlayerFinish(this);
            }
        }
    }

    // Dùng để hiển thị UI % quãng đường đã đi
    public float GetProgress(int totalCheckpoints)
    {
        if (totalCheckpoints <= 0) return 0f;
        return (float)(currentCheckpointIndex + 1) / totalCheckpoints;
    }
}