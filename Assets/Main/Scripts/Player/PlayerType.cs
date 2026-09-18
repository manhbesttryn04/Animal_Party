using UnityEngine;

public class PlayerType : MonoBehaviour
{
    [Header("Type Player")]
    public bool isPlayer2 = false;

    private void Awake()
    {
        SetupPlayerType();
    }

    private void SetupPlayerType()
    {
        gameObject.tag = isPlayer2 ? "Player 2" : "Player 1";
    }

    // Nếu bạn muốn thay đổi trong runtime:
    public void SetPlayerType(bool player2)
    {
        isPlayer2 = player2;
        SetupPlayerType();
    }

}
