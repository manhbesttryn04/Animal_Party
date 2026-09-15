using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerTrapState : MonoBehaviour
{
    public PlayerManager manager;
    public bool isTrapActive = false;

    public void Start()
    {
        manager = GetComponent<PlayerManager>();
    }

    public IEnumerator CheckCurrentTile()
    {
        PlayerMoveAI a = manager.playerMoveAI;
        TrapAndCoin trap = a.pointCheck[a.currentIndex].GetComponentInChildren<TrapAndCoin>();

        if (trap == null)
            yield break;

        isTrapActive = true;

        try
        {
            //==========================
            // Bomb
            //==========================
            if (trap.hasBom)
            {
                trap.BomActivated();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySpecial(AudioManager.Instance.bombVoiceClip);
                }
                if(UIManager.Instance != null)
                {
                   StartCoroutine( UIManager.Instance.ShowDebuffAndBuffPanel(UIManager.Instance.bombTrapPanel));
                }
                yield return new WaitForSeconds(1.5f);

                yield return StartCoroutine(a.BoomHitEffect(2));

                yield break;
            }

            //==========================
            // Teleport
            //==========================
            if (trap.hasTelep)
            {
                
                trap.TelepActivated();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySpecial(AudioManager.Instance.PositionSwapVoiceClip);
                }
                if (UIManager.Instance != null)
                {
                    StartCoroutine( UIManager.Instance.ShowDebuffAndBuffPanel(UIManager.Instance.positionTrapPanel));
                }
                yield return new WaitForSeconds (1f);

                yield return StartCoroutine(trap.TeleportRoutine(manager.playerType.isPlayer2));

                yield break;
            }

            //==========================
            // Coin
            //==========================
            if (trap.hasCoin)
            {
                trap.CoinActivated(manager.playerType.isPlayer2 ? 1 : 0);
                manager.playerCoin.coinEndMiniGame += 100;

                yield return new WaitForSeconds(0.5f);
            }
        }
        finally
        {
            isTrapActive = false;
        }
    }
}