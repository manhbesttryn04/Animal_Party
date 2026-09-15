using System.Collections;
using UnityEditor;
using UnityEngine;

public class StartGame : MonoBehaviour
{
    public GameObject player1;
    public GameObject player2;

    bool isFistRoundSussce = false;

    [Header("Check Timer")]
    public float checkTime = 1f;
    private float checkTimer = 0f;

    void Start()
    {
        SetupPlayers();
        StartCoroutine(FistRoundPlayer1());
    }

    private void SetupPlayers()
    {
        player1 = GameObject.FindGameObjectWithTag("Player 1");
        player2 = GameObject.FindGameObjectWithTag("Player 2");
    }


    private void Update()
    {
        if (isFistRoundSussce) return;

        checkTimer += Time.deltaTime;

        if (checkTimer >= checkTime)
        {
            checkTimer = 0f;
            CheckNextPlayer2();
        }
    }

    public void CheckNextPlayer2()
    {
        if (isFistRoundSussce) return;
        if (player1 == null || player2 == null) return;

        PlayerManager p1Manager = player1.GetComponent<PlayerManager>();

        if (p1Manager == null) return;

        if (p1Manager.playerRound.isRound1)
        {
            StartCoroutine(FistRoundPlayer2());
            isFistRoundSussce = true;
        }
    }

    public IEnumerator FistRoundPlayer1()
    {
       
        PlayerManager p1 = player1.GetComponent<PlayerManager>();

        p1.playerCamera.isFllow2 = true;

        yield return new WaitForSeconds(2f);

        p1.playerCamera.isFllow2 = false;

        yield return new WaitForSeconds(0.5f);
        AudioManager.Instance.PlaySpecial(AudioManager.Instance.playerOneClip);
        yield return StartCoroutine(p1.playerNotifi.SetNotifi());

        p1.playerInputDice.isClick = false;
    }

    public IEnumerator FistRoundPlayer2()
    {
        PlayerManager p2 = player2.GetComponent<PlayerManager>();

        yield return new WaitForSeconds(0.5f);

        p2.playerCamera.isFllow2 = true;

        yield return new WaitForSeconds(0.5f);

        p2.playerCamera.isFllow2 = false;
        AudioManager.Instance.PlaySpecial(AudioManager.Instance.playerTwoClip);
        yield return StartCoroutine(p2.playerNotifi.SetNotifi());

        p2.playerInputDice.isClick = false;
    }
}