using UnityEngine;
using System.Collections;
using TMPro;

public class TypingDotsAnimation : MonoBehaviour
{
    public float dotInterval = 0.3f;

    TextMeshProUGUI tmp;

    void Awake()
    {
        tmp = GetComponentInChildren<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        StartCoroutine(AnimateDots());
    }

    IEnumerator AnimateDots()
    {
        string[] dotStates = { ".", "..", "..." };
        int i = 0;

        while (true)
        {
            if (tmp != null)
                tmp.text = dotStates[i % dotStates.Length];
            i++;
            yield return new WaitForSeconds(dotInterval);
        }
    }

    // Trả về thời gian cần chờ để kết thúc đúng lúc "..." (đủ 1 chu kỳ trọn vẹn)
    public float GetFullCycleDuration(int cycles = 1)
    {
        return dotInterval * 3 * cycles;
    }
}