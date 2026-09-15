using UnityEngine;

public class PlayerMiniGame: MonoBehaviour
{
    public Transform checkPoint;

    public float waterHeight = -5f;

    public void Respawn()
    {
        CharacterController cc =
            GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        transform.position =
            checkPoint.position;

        if (cc != null)
            cc.enabled = true;
        PlayerCoin coin = GetComponent<PlayerCoin>();
        coin.TakeCoin(5);
    }
    public void UpCoin(int i,int c)
    {
        int aa = Mathf.Max(0, c);
        if (i == 0)
        {
            PlayerCoin coin = GetComponent<PlayerCoin>();
       
            coin.TakeCoin(aa);
        }
        else if (i == 1) {
            PlayerCoin coin = GetComponent<PlayerCoin>();
            coin.AddCoin(aa);
        }
    } 
}
