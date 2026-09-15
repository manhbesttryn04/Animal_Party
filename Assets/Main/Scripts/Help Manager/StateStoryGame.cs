using UnityEngine;

public class StateStoryGame : MonoBehaviour
{
    public bool isFistRound = false;
    public bool isNextRound = false;
    public bool hasPlayer1Win = false;
    public bool hasPlayer2Win = false;
    public bool hasWinByCoin = false;
    public bool hasWinByIndex = false;
    public void SetOnePlayerWin(int playerNumber)
    {
        if (playerNumber == 0)
        {
            hasPlayer1Win = true;
            hasPlayer2Win = false;
        }
        else if (playerNumber == 1)
        {
            hasPlayer2Win = true;
            hasPlayer1Win = false;
        }
    }
}