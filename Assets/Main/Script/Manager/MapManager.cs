using NUnit.Framework;
using UnityEngine;

public class MapAndCharacterManager : MonoBehaviour
{
    public static MapAndCharacterManager Instance { get; private set; }
    public GameObject mainMap;
    public GameObject mainCharacters;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}