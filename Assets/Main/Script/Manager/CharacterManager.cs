using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [Header("Singleton")]
    public static CharacterManager Instance;

    [Header("Selected Character Index")]
    public int indexPlayer1;
    public int indexPlayer2;

    [Header("Player Character Lists")]
    public List<GameObject> player1List;
    public List<GameObject> player2List;
    public List<GameObject> playerPlaylist;

    private void Awake()
    {
        SetupSingleton();
        SetupIndexFromSend();
        SetUpPlayer();
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

    private void SetupIndexFromSend()
    {
        var send = SendIndexCharacter.Instance;
        if (send != null)
        {
            indexPlayer1 = send.player1Index;
            indexPlayer2 = send.player2Index;
        }
    }


    public void SetUpPlayer()
    {
        player1List[indexPlayer1].SetActive(true);
        player2List[indexPlayer2].SetActive(true);
    }
}