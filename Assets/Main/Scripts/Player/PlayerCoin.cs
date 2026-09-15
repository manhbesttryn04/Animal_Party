using UnityEngine;

public class PlayerCoin : MonoBehaviour
{
    public PlayerManager manager;
    public MiniGameManager gameManager;
    public int coinMiniGame = 100;
    public int coinEndMiniGame = 0;

    public void TakeCoin(int i)
    {
        coinMiniGame = Mathf.Max(0, coinMiniGame - i);

        gameManager = FindFirstObjectByType<MiniGameManager>();

        if (!manager.playerType.isPlayer2)
        {
            UIManager.Instance.coinMiniGameTextP1.text = coinMiniGame.ToString();
        }
        else
        {
            UIManager.Instance.coinMiniGameTextP2.text = coinMiniGame.ToString();
        }
    }
    public void AddCoin(int i)
    {
        coinMiniGame +=i;
    }
    public void AddCoinToPlayerMain(int i)
    {
        coinEndMiniGame +=i;
    }

}
