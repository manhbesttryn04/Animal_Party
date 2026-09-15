using System.Collections;
using UnityEngine;

public class CannonDebuff : MonoBehaviour
{
    public Transform firePoint;
    public GameObject bombPrefab;

    [Header("VFX")]
    public GameObject vfxCannonAttack;
    public bool test;

    public BombDebuff Fire(Transform target)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.cannonClip);

        StartCoroutine(PlayCannonVFX());

        GameObject bomb = Instantiate(
            bombPrefab,
            firePoint.position,
            Quaternion.identity
        );

        BombDebuff bombScript = bomb.GetComponent<BombDebuff>();
        bombScript.target = target;

        AudioManager.Instance.PlaySFX(AudioManager.Instance.fallingBom);

        return bombScript;
    }

    private IEnumerator PlayCannonVFX()
    {
        if (vfxCannonAttack == null)
            yield break;

        vfxCannonAttack.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        vfxCannonAttack.SetActive(false);
    }
}