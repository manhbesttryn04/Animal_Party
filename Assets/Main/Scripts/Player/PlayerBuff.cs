using System.Collections;
using UnityEngine;

public class PlayerBuff : MonoBehaviour
{
    [Header("Manager")]
    public PlayerManager manager;

    [Header("Buff")]
    public bool isBuffDeffense;
    public bool isBuffMagic;
    public bool isBuffCanon;
    public int isBuffDice;
    public int isBuffDiceNext;

    public int countCoinPower;

    [Header("Effects")]
    public GameObject shieldMagic;
    public GameObject shieldDefense;

    public void Start()
    {
        SetupPlayerBuff();
    }

    public void SetupPlayerBuff()
    {
        manager = GetComponent<PlayerManager>();
    }

    public void ResetBuff()
    {
        isBuffDeffense = false;
        isBuffMagic = false;
        isBuffCanon = false;

        // countCoinPower = 0;

        if (shieldMagic != null)
        {
            shieldMagic.SetActive(false);
        }

        if (shieldDefense != null)
        {
            shieldDefense.SetActive(false);
        }
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

    public void ShowMagicShieldEvent()
    {
        StartCoroutine(ShowMagicShield());
    }

    public IEnumerator ShowMagicShield()
    {
        if (shieldMagic == null)
            yield break;

        shieldMagic.SetActive(true);

        // Audio
        if (AudioManager.Instance != null)
        {
            if (AudioManager.Instance.buffMagicClip != null)
            {
                AudioManager.Instance.PlaySFX(
                    AudioManager.Instance.buffMagicClip
                );
            }
        }

        Transform shield = shieldMagic.transform;

        if (shield == null)
            yield break;

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

        if (shieldMagic != null)
        {
            shieldMagic.SetActive(false);
        }

        // Reset rotation
        if (manager != null)
        {
            if (manager.playerLookPlayer != null)
            {
                manager.playerLookPlayer.ResetToSavedRotation();
            }
        }
    }

    public void ShowDefenseShield()
    {
        if (shieldDefense == null)
            return;

        // Audio
        if (AudioManager.Instance != null)
        {
            if (AudioManager.Instance.buffDeffClip != null)
            {
                AudioManager.Instance.PlaySFX(
                    AudioManager.Instance.buffDeffClip
                );
            }
        }

        shieldDefense.SetActive(true);
    }

    public void HideDefenseShield()
    {
        if (shieldDefense != null)
        {
            shieldDefense.SetActive(false);
        }

        // Reset rotation
        if (manager != null)
        {
            if (manager.playerBuff != null)
            {
                manager.playerBuff.isBuffDeffense = false;
            }
            if (manager.playerLookPlayer != null)
            {
                manager.playerLookPlayer.ResetToSavedRotation();
            }
        }
        
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