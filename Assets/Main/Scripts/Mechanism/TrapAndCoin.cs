using System.Collections;
using UnityEngine;

public class TrapAndCoin : MonoBehaviour
{
    public GameObject bom;
    public GameObject coin;
    public ParticleSystem teleport;

    public bool hasBom;
    public bool hasCoin;
    public bool hasTelep;

    private void Start()
    {
       SetUpItems();
    }

    public void SetUpItems()
    {
        
            bom.SetActive(false);
        
            coin.SetActive(false);
      
            teleport.gameObject.SetActive(false);
        
    }

    public void BomActivated()
    {
        bom.SetActive(true);
        Bomb b = bom.GetComponent<Bomb>();

        if (b != null)
        {
            b.TriggerBomb();
            hasBom = false;
        }
    }

    public void CoinActivated(int i)
    {
        coin.SetActive(true);
        Coin c = coin.GetComponent<Coin>();

        if (c != null)
        {
            c.TriggerCoin(i);
            hasCoin = false;
        }
    }

    public void TelepActivated()
    {
        if (!hasTelep || teleport == null)
            return;
         if (teleport == null) return;

            teleport.gameObject.SetActive(true);
            teleport.loop = true;
            teleport.Play();

        hasTelep = false;

     
    }

    public IEnumerator TeleportRoutine(bool isPlayer2)
    {
        TeleportAllPlayer teleportManager = teleport.GetComponent<TeleportAllPlayer>();

        if (teleportManager != null)
        {
            yield return StartCoroutine(teleportManager.TeleportPlayers(isPlayer2));
        }

        teleport.loop = false;

        yield return new WaitForSeconds(1f);

        teleport.gameObject.SetActive(false);
    }
}