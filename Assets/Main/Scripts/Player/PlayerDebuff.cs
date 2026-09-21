using System.Collections;
using UnityEngine;

public class PlayerDebuff : MonoBehaviour
{
    [Header("Manager")]
    public PlayerManager manager;

    [Header("Debuff State")]
    public bool isNoRollDice;

    [Header("Player Renderer")]
    public Renderer playerRenderer;

    [Header("Ice Shader Material")]
    public Material iceMaterial;

    [Header("Ice Time")]
    public float iceTime = 1f;

    private Material iceMatInstance;
    private Material[] originalMaterials;
    private Coroutine currentRoutine;

    private void Awake()
    {
        SetupDebuff();
    }

    private void SetupDebuff()
    {
        manager = GetComponent<PlayerManager>();

        if (playerRenderer != null)
            originalMaterials = playerRenderer.materials;
    }

    public void ApplyMagicRock()
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ApplyIceRoutine());
    }

    private IEnumerator ApplyIceRoutine()
    {
        isNoRollDice = true;

        AddIceMaterial();
        var random = Random.Range(0, 2);
        string aniRanDomString;
        if (random == 0)
        {
            aniRanDomString = "Terrified";

            manager.playerAnimator.playerAnimator.SetTrigger(aniRanDomString);

            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            aniRanDomString = "Terrified 0";

            manager.playerAnimator.playerAnimator.SetTrigger(aniRanDomString);

            yield return new WaitForSeconds(1f);
        }

        manager.playerAnimator.playerAnimator.speed = 0f;
        AudioManager.Instance.PlaySFX(AudioManager.Instance.bebuffRockMagicClip);

        yield return StartCoroutine(SetIceProgress(-2f, 2f, iceTime));

        currentRoutine = null;
    }

    public void ResetDebuff()
    {
        isNoRollDice = false;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        RemoveIceMaterial();

        manager.playerAnimator.playerAnimator.speed = 1f;
        currentRoutine = null;
    }

    private void AddIceMaterial()
    {
        if (playerRenderer == null || iceMaterial == null)
            return;

        if (iceMatInstance != null)
            return;

        iceMatInstance = new Material(iceMaterial);
        iceMatInstance.SetFloat("_Progress", -1f);

        Material[] current = playerRenderer.materials;
        Material[] newMats = new Material[current.Length + 1];

        for (int i = 0; i < current.Length; i++)
            newMats[i] = current[i];

        newMats[newMats.Length - 1] = iceMatInstance;
        playerRenderer.materials = newMats;
    }

    private IEnumerator SetIceProgress(float start, float end, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float value = Mathf.Lerp(start, end, time / duration);

            if (iceMatInstance != null)
                iceMatInstance.SetFloat("_Progress", value);

            yield return null;
        }

        if (iceMatInstance != null)
            iceMatInstance.SetFloat("_Progress", end);
    }

    private void RemoveIceMaterial()
    {
        if (playerRenderer == null || originalMaterials == null)
            return;

        playerRenderer.materials = originalMaterials;

        if (iceMatInstance != null)
        {
            Destroy(iceMatInstance);
            iceMatInstance = null;
        }
    }
}
