using UnityEngine;

// Gắn lên từng cột mốc/cổng trên track, đặt index tăng dần theo thứ tự đi qua.
// Checkpoint có index trùng với "finishCheckpointIndex" trong RaceMiniGame = vạch đích.
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Thứ tự checkpoint trên track, bắt đầu từ 0")]
    public int index;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}