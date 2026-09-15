using UnityEngine;

public class CheckWinPlayer : MonoBehaviour
{
    public static CheckWinPlayer Instance { get; private set; }

    private void Awake()
    {
        SetupSingleton();
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

    public bool CheckWinnerByIndex(int index)
    {
        return index >= 32;
    }

    public bool CheckWinnerByCoinPower(int coin)
    {
        return coin >= 4;
    }
}
