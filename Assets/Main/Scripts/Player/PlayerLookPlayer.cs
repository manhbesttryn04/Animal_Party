using UnityEngine;

public class PlayerLookPlayer : MonoBehaviour
{
    [Header("Tham chiếu PlayerType")]
    public PlayerManager manager;
    [SerializeField] private Quaternion savedRotation;

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
        if (manager.playerType == null) return;
        SaveRotationYPlayer();
        string targetTag = manager.playerType.isPlayer2 ? "Player 1" : "Player 2";
        GameObject opponent = GameObject.FindGameObjectWithTag(targetTag);

        if (opponent == null) return;

        float myX = transform.position.x;
        float targetX = opponent.transform.position.x;

        if (Mathf.Abs(myX - targetX) > Mathf.Epsilon)
        {
            Vector3 direction = opponent.transform.position - transform.position;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // Chỉ thay đổi Y rotation
                Vector3 currentEuler = transform.eulerAngles;

                transform.rotation = Quaternion.Euler(
                    currentEuler.x,
                    targetRotation.eulerAngles.y,
                    currentEuler.z
                );
            }
        }
    }

    public void SaveRotationYPlayer()
    {
        // Lưu rotation hiện tại
        savedRotation = transform.rotation;

    }
    public void ResetToSavedRotation()
    {
        transform.rotation = savedRotation;
    }
}

