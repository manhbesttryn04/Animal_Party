using UnityEngine;

public class CharacterWinManager : MonoBehaviour
{
    [Header("Characters")]
    public GameObject seaGullCharacter;
    public GameObject batCharacter;
    public GameObject rabbitCharacter;
    public GameObject leoPardCharacter;
    public GameObject slothCharacter;

    [Header("Unlock States")]
    public bool isSeaGullUnlocked;
    public bool isLeoPardUnlocked;
    public bool isSlothUnlocked;
    public bool isBatUnlocked;
    public bool isRabbitUnlocked;

    [Header("Settings")]
    public bool isPlayer2 = false;
    public bool isUseManager = false;

    private void Awake()
    {
        if (isUseManager && SendPlayerWinner.Instance != null)
        {
            string characterName = SendPlayerWinner.Instance.characterName;
            bool winnerIsPlayer2 = SendPlayerWinner.Instance.isPlayer2;
            SetCharacterWinner(characterName, winnerIsPlayer2);
        }

        UnlockCharacterWinner();
    }

    private void UnlockCharacterWinner()
    {
        if (isSeaGullUnlocked) seaGullCharacter.SetActive(true);
        if (isBatUnlocked) batCharacter.SetActive(true);
        if (isRabbitUnlocked) rabbitCharacter.SetActive(true);
        if (isLeoPardUnlocked) leoPardCharacter.SetActive(true);
        if (isSlothUnlocked) slothCharacter.SetActive(true);
    }

    public void SetCharacterWinner(string name, bool isPlayer2)
    {
        switch (name)
        {
            case "Seagull":
                isSeaGullUnlocked = true;
                break;
            case "Bat":
                isBatUnlocked = true;
                break;
            case "Rabbit":
                isRabbitUnlocked = true;
                break;
            case "Leopard":
                isLeoPardUnlocked = true;
                break;
            case "Sloth":
                isSlothUnlocked = true;
                break;
            default:
                Debug.LogWarning($"Unknown character name: {name}");
                break;
        }

        this.isPlayer2 = isPlayer2;
    }
}
