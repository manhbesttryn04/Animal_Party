using UnityEngine;

public class SendIndexCharacter : MonoBehaviour
{
    public static SendIndexCharacter Instance { get; private set; }

    [Header("Player Indexes")]
    public int player1Index;
    public int player2Index;

    private void Awake()
    {
        SetupSingleton();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // giữ lại khi đổi scene
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
