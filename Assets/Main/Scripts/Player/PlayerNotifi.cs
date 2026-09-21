using System.Collections;
using UnityEngine;

public class PlayerNotifi : MonoBehaviour
{
    [Header("Manager")]
    public PlayerManager manager;
    public GameObject NotifiPlayer;
    public GameObject DicePlayer;

    public void Start()
    {
        SetupNotifi();
    }

    private void SetupNotifi()
    {
        manager = GetComponent<PlayerManager>();
        var ui = UIManager.Instance;

        if (!manager.playerType.isPlayer2)
        {
            NotifiPlayer = ui.notifiP1;
            DicePlayer = ui.diceRollP1;
        }
        else
        {
            NotifiPlayer = ui.notifiP2;
            DicePlayer = ui.diceRollP2;
        }

        NotifiPlayer.SetActive(false);
        DicePlayer.SetActive(false);
    }
    public IEnumerator SetNotifi()
    {
        NotifiPlayer.SetActive(true);
        yield return new WaitForSeconds(1.3f);
        DicePlayer.SetActive(true);
        yield return new WaitForSeconds(0.45f);
        NotifiPlayer.SetActive(false);
    }
    public void SetDice()
    {
        DicePlayer.SetActive(false);
    }

}
