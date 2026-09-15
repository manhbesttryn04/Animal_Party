using UnityEngine;

public class HitBox : MonoBehaviour
{
    public float knockbackForce = 5f;
    public GameObject thisGameObject;
    private void OnTriggerEnter(Collider other)
    {


        if (other.gameObject == thisGameObject)
            return;

        PlayerManager target = other.GetComponent<PlayerManager>();

        if (target == null)
            return;


        // Hướng đẩy
        Vector3 pushDir =
            (other.transform.position - transform.position).normalized;

        pushDir.y = 0f;

        CharacterController cc =
            other.GetComponent<CharacterController>();

        if (cc != null)
        {
            StartCoroutine(
                PushCharacter(cc, pushDir * knockbackForce)
            );
        }
    }

    private System.Collections.IEnumerator PushCharacter(
        CharacterController cc,
        Vector3 force)
    {
        float duration = 0.2f;
        float timer = 0f;

        while (timer < duration)
        {
            cc.Move(force * Time.deltaTime);

            timer += Time.deltaTime;

            yield return null;
        }
    }
}
