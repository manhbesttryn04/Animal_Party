using NUnit.Framework;
using UnityEngine;

public class MapAndCharacterManager : MonoBehaviour
{
    public static MapAndCharacterManager Instance { get; private set; }
    public GameObject mainMap;
    public GameObject mainCharacters;

    private void Awake()
    {
        SetupSingleton();
    }

    private void SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // giữ lại khi đổi scene
    }
}
