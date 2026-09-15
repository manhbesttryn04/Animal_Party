using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDiceRoll : MonoBehaviour
{
    public PlayerManager manager;

    // Thứ tự các phần tử phải tương ứng với mặt 1 → 6
    public List<GameObject> dices = new List<GameObject>();

    public GameObject diceRandom;

    public int currentDiceNumber;

    private Coroutine hideDiceCoroutine;

    // 0 = không ép số, lần tung tiếp theo vẫn random bình thường.
    // 1 -> 6 = kết quả được tool tay cầm chọn cho lần tung kế tiếp.
    private int forcedNextDiceNumber;

    private void Start()
    {
        manager = GetComponent<PlayerManager>();
    }

    public void SetDiceRandom(int i)
    {
        if (diceRandom == null)
            return;

        diceRandom.SetActive(i != 0);
    }

    public void RandomDice()
    {
        /*
         * Xác suất:
         * Mặt 1: 1%
         * Mặt 2: 5%
         * Mặt 3: 10%
         * Mặt 4: 28%
         * Mặt 5: 28%
         * Mặt 6: 28%
         *
         * Tổng: 100%
         */
        // int[] diceWeights = { 1, 1, 10, 29, 29, 30 };
        int[] diceWeights = { 17, 17, 17, 17, 16, 16 };
     
        if (forcedNextDiceNumber >= 1 &&
            forcedNextDiceNumber <= 6)
        {
            currentDiceNumber =
                forcedNextDiceNumber;

            // Chỉ ép đúng một lần tung rồi trở lại random.
            forcedNextDiceNumber = 0;
        }
        else
        {
            currentDiceNumber =
                GetWeightedDiceNumber(diceWeights);
        }

        // Tắt toàn bộ mặt xúc xắc
        HideAllDices();

        // List bắt đầu từ vị trí 0 nên phải trừ 1
        int diceIndex = currentDiceNumber - 1;

        if (diceIndex >= 0 && diceIndex < dices.Count)
        {
            dices[diceIndex].SetActive(true);
        }
        else
        {


        }

        //Debug.Log("Dice Number: " + currentDiceNumber);

        // Ngăn nhiều Coroutine chạy cùng lúc
        if (hideDiceCoroutine != null)
        {
            StopCoroutine(hideDiceCoroutine);
        }

        hideDiceCoroutine = StartCoroutine(HideDiceAfterTime());
    }

    public void ForceNextDiceNumber(int diceNumber)
    {
        if (diceNumber < 1 || diceNumber > 6)
            return;

        forcedNextDiceNumber = diceNumber;
    }

    private int GetWeightedDiceNumber(int[] weights)
    {
        int totalWeight = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            totalWeight += weights[i];
        }

        // Random từ 0 đến 99
        int randomValue = Random.Range(0, totalWeight);

        int accumulatedWeight = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            accumulatedWeight += weights[i];

            if (randomValue < accumulatedWeight)
            {
                // i = 0 tương ứng mặt 1
                return i + 1;
            }
        }

        return 6;
    }

    private IEnumerator HideDiceAfterTime()
    {
        // Hiện mặt xúc xắc trong 3 giây
        yield return new WaitForSeconds(1f);

        if (manager != null && manager.playerCamera != null)
        {
            manager.playerCamera.isFollow = false;
            manager.playerCamera.isFllow2 = true;
        }

        HideAllDices();

        yield return new WaitForSeconds(1f);

        if (manager != null && manager.playerMoveAI != null)
        {
            manager.playerMoveAI.isMoving = true;

            // Di chuyển đúng số bước từ 1 đến 6
            StartCoroutine(
                manager.playerMoveAI.AIToPoint(currentDiceNumber - 1)
            );
        }

        hideDiceCoroutine = null;
    }

    private void HideAllDices()
    {
        for (int i = 0; i < dices.Count; i++)
        {
            if (dices[i] != null)
            {
                dices[i].SetActive(false);
            }
        }
    }

    public void PlayerAudioDice()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFX(
            AudioManager.Instance.diceRollClip
        );
    }
}