using System.Collections.Generic;
using UnityEngine;

public class SetUpPlayerCutScene2 : MonoBehaviour
{
    public int indexP1;
    public int indexP2;

    public List<GameObject> playerViewList;
    public List<GameObject> player1List;
    public List<GameObject> player2List;
    public List<GameObject> gameObjects;

    private void Start()
    {
        SetupIndexes();
        SetupCharacter();
        EndCutScene();
    }

    private void SetupIndexes()
    {
        if (SendIndexCharacter.Instance != null)
        {
            indexP1 = SendIndexCharacter.Instance.player1Index;
            indexP2 = SendIndexCharacter.Instance.player2Index;
        }
    }

    void SetupCharacter()
    {
        // P1
        for (int i = 0; i < player1List.Count; i++)
        {
            player1List[i].SetActive(i == indexP1);
        }

        // P2
        for (int i = 0; i < player2List.Count; i++)
        {
            player2List[i].SetActive(i == indexP2);
        }

        // View
        for (int i = 0; i < playerViewList.Count; i++)
        {
            bool isSelected = (i == indexP1 || i == indexP2);
            playerViewList[i].SetActive(!isSelected);
        }
    }
    public void EndCutScene()
    {
        // Ẩn tàu
        foreach (GameObject obj in gameObjects)
        {
            obj.SetActive(false);
        }

        // Ẩn P1 được chọn
        player1List[indexP1].SetActive(false);

        // Ẩn P2 được chọn
        player2List[indexP2].SetActive(false);
    }
    public void StartCutScene()
    {
        // Ẩn tàu
        foreach (GameObject obj in gameObjects)
        {
            obj.SetActive(true);
        }

        // Ẩn P1 được chọn
        player1List[indexP1].SetActive(true);

        // Ẩn P2 được chọn
        player2List[indexP2].SetActive(true);
    }
    public void StartMovePlayer()
    {
        PlayerCutSceneEndGame p1 = player1List[indexP1].GetComponent<PlayerCutSceneEndGame>();
        PlayerCutSceneEndGame p2 = player2List[indexP2].GetComponent<PlayerCutSceneEndGame>();
        p1.startCutscene = true;
        p2.startCutscene = true;
    }

}
