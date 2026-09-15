using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour
{
  
    [Header("Delay")]
    public float hideDelay = 0.1f;


    public void TriggerCoin(int p)
    {
        StartCoroutine(CollectRoutine(p));
    }
    IEnumerator CollectRoutine(int p)
    {
        //gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        AudioManager.Instance.PlaySFX(AudioManager.Instance.coinClip);
        UIManager.Instance.ShowBonusCoin(p);

        // Ẩn model đồng xu
        GetComponent<MeshRenderer>().enabled = false;
        //GetComponent<Collider>().enabled = false;

        // Chờ
        yield return new WaitForSeconds(hideDelay);

        // Xóa object
        Destroy(gameObject);
    }
}