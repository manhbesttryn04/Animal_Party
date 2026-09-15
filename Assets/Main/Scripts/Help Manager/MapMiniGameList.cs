using System.Collections.Generic;
using UnityEngine;

public class MapMiniGameList : MonoBehaviour
{
    public static MapMiniGameList Instance { get; private set; }

    [Header("MiniGame Maps")]
    public List<GameObject> mapMiniGameList;

    private void Awake()
    {
        SetupSingleton();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // giữ lại khi đổi scene nếu cần
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void DisableActiveMapMiniGame()
    {
        foreach (GameObject miniGame in mapMiniGameList)
        {
            if (miniGame != null && miniGame.activeSelf)
            {
                miniGame.SetActive(false);
            }
        }
    }
}
