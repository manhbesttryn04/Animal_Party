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

        string targetTag = manager.playerType.isPlayer2 ? "Player 1" : "Player 2";
        GameObject opponent = GameObject.FindGameObjectWithTag(targetTag);

        if (opponent == null) return;

        RotateTowards(opponent);
    }

    private void RotateTowards(GameObject opponent)
    {
        SaveRotationYPlayer();

        Vector3 direction = opponent.transform.position - transform.position;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation; // xoay ngay lập tức
        }
    }

    public void SaveRotationYPlayer()
    {
        savedRotation = transform.rotation;
    }

    public void ResetToSavedRotation()
    {
        transform.rotation = savedRotation;
    }
}
