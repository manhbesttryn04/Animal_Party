using System.Collections;
using UnityEngine;

public class PlayerBuff : MonoBehaviour
{
    [Header("Buff")]
    public bool isBuffDeffense;
    public bool isBuffMagic;
    public bool isBuffCanon;
    public int isBuffDice;
    public int isBuffDiceNext;

    public int countCoinPower;

    [Header("Effects")]
    public GameObject shieldMagic;
    public GameObject shieldCanon;
    public GameObject shieldDice;
    public GameObject shieldDefense;

    public void ResetBuff()
    {
        isBuffDeffense = false;
        isBuffMagic = false;
        isBuffCanon = false;
       // countCoinPower = 0;

        if (shieldMagic != null)
            shieldMagic.SetActive(false);

        if (shieldCanon != null)
            shieldCanon.SetActive(false);

        if (shieldDice != null)
            shieldDice.SetActive(false);

        if (shieldDefense != null)
            shieldDefense.SetActive(false);
    }

    public void ApplyBuff(int itemIndex)
    {
        switch (itemIndex)
        {
            case 0:
                isBuffMagic = true;
                break;

            case 2:
                isBuffCanon = true;
                break;

            case 3:
                isBuffDeffense = true;
                break;

            case 4:
                countCoinPower += 1;
                break;

            case 5:
                isBuffDiceNext = 1;
                break;
        }
    }

    public IEnumerator ShowMagicShield()
    {
        if (shieldMagic == null)
            yield break;

        shieldMagic.SetActive(true);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buffMagicClip);

        Transform shield = shieldMagic.transform;

        // Bắt đầu từ scale 0
        shield.localScale = Vector3.zero;

        float growTime = 0.3f;
        float shrinkTime = 0.3f;

        // Scale từ 0 -> 1.7
        float t = 0f;
        while (t < growTime)
        {
            t += Time.deltaTime;

            float scale =
                Mathf.Lerp(
                    0f,
                    1.7f,
                    t / growTime
                );

            shield.localScale =
                Vector3.one * scale;

            yield return null;
        }

        shield.localScale = Vector3.one * 1.7f;

        // Giữ 2 giây
        yield return new WaitForSeconds(2f);

        // Scale từ 1.7 -> 0
        t = 0f;

        while (t < shrinkTime)
        {
            t += Time.deltaTime;

            float scale =
                Mathf.Lerp(
                    1.7f,
                    0f,
                    t / shrinkTime
                );

            shield.localScale =
                Vector3.one * scale;

            yield return null;
        }

        shield.localScale = Vector3.zero;

        shieldMagic.SetActive(false);
    }

    public IEnumerator ShowCanonShield()
    {
        if (shieldCanon == null)
            yield break;

        shieldCanon.SetActive(true);

        yield return new WaitForSeconds(2f);

        shieldCanon.SetActive(false);
    }

    public IEnumerator ShowDiceShield()
    {
        if (shieldDice == null)
            yield break;
        
        shieldDice.SetActive(true);
     

        yield return new WaitForSeconds(2f);

        shieldDice.SetActive(false);
    }

    public IEnumerator ShowDefenseShield()
    {
        if (shieldDefense == null)
            yield break;
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buffDeffClip);
        shieldDefense.SetActive(true);

        yield return new WaitForSeconds(2f);

        shieldDefense.SetActive(false);
    }
    public void ConvertBuffDice()
    {
        if (isBuffDiceNext > 0)
        {
            isBuffDice = 1;
            isBuffDiceNext = 0;
        }
    }
}