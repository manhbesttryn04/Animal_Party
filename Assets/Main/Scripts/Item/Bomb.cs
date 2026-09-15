using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Explosion Effect")]
    public GameObject explosionEffect;

    [Header("Explosion Delay")]
    public float explodeDelay = 1f;

    [Header("Bomb Object")]
    public GameObject bombVisual;

    public bool hasExploded = false;


    // =========================
    // BẬT NỔ
    // =========================

    public void TriggerBomb()
    {
        // CHỐNG NỔ NHIỀU LẦN
        if (hasExploded)
            return;

        hasExploded = true;

        StartCoroutine(
            ExplosionRoutine()
        );
    }

    IEnumerator ExplosionRoutine()
    {
        //gameObject.SetActive(true);
        // CHỜ TRƯỚC KHI NỔ
        yield return new WaitForSeconds(
            explodeDelay
        );

        //Âm thanh nổ
        AudioManager.Instance.PlaySFX(AudioManager.Instance.boomClip);
        // EFFECT
        GameObject ef = null;

        // =========================
        // HIỆU ỨNG NỔ
        // =========================

        if (explosionEffect != null)
        {
            ef = Instantiate(
                explosionEffect,
                transform.position,
                Quaternion.identity
            );
        }

        // =========================
        // TẮT MODEL BOOM
        // =========================

        if (bombVisual != null)
        {
            bombVisual.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // =========================
        // XÓA EFFECT
        // =========================

        if (ef != null)
        {
            Destroy(ef, 1.2f);
        }
    }
}