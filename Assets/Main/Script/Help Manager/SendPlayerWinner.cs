using UnityEngine;

public class SendPlayerWinner : MonoBehaviour
{
    public static SendPlayerWinner Instance;

    [Header("Winner Info")]
    public string characterName;
    public bool isPlayer2;

    private void Awake()
    {
        SetupSingleton();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// Lưu thông tin người chiến thắng.
    /// </summary>
    public void SetInforPlayerWinner(GameObject player)
    {
        if (player == null) return;

        PlayerInfo playerInfo = player.GetComponent<PlayerInfo>();
        if (playerInfo != null)
        {
            characterName = playerInfo.characterName;
        }

        PlayerType playerType = player.GetComponent<PlayerType>();
        if (playerType != null)
        {
            isPlayer2 = playerType.isPlayer2;
        }
    }
}