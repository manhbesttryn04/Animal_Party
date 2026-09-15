using System.Collections;
using UnityEngine;

public class BombDebuff : MonoBehaviour
{
    public Transform target;

    [Header("Flight")]
    public float flyTime = 1.5f;
    public float arcHeight = 4f;
    public float rotateSpeed = 720f;

    [Header("Explosion")]
    public GameObject explosionPrefab;

    [Header("Power Knockback")]
    public int power = 3;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
        StartCoroutine(FlyRoutine());
    }

    // =========================
    // FLY TO TARGET
    // =========================
    private IEnumerator FlyRoutine()
    {
        float time = 0f;

        while (time < flyTime)
        {
            time += Time.deltaTime;

            float t = time / flyTime;

            Vector3 pos = Vector3.Lerp(startPos, target.position, t);
            pos.y += arcHeight * Mathf.Sin(t * Mathf.PI);

            transform.position = pos;

            transform.Rotate(
                Vector3.forward,
                rotateSpeed * Time.deltaTime,
                Space.Self
            );

            yield return null;
        }

        HitTarget();
    }

    // =========================
    // HIT TARGET
    // =========================
    private void HitTarget()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.boomClip);
        // Explosion FX
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(explosion, 2f);
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        PlayerManager player =
            target.GetComponent<PlayerManager>();

        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        // =========================
        // SHIELD CHECK
        // =========================
        if (player.playerBuff.isBuffDeffense)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySpecial(AudioManager.Instance.cannonShieldVoiceClip);
            }
            if (UIManager.Instance != null)
            {
                StartCoroutine(UIManager.Instance.ShowDebuffAndBuffPanel(UIManager.Instance.cannonShieldPanel));
            }
            player.playerAnimator.playerAnimator.SetTrigger("Defense");
            StartCoroutine(player.playerBuff.ShowDefenseShield());

            player.playerBuff.isBuffDeffense = false;
            Destroy(gameObject);
            return;
        }

        // =========================
        // KNOCKBACK SYSTEM
        // =========================
        PlayerMoveAI moveAI =
            target.GetComponent<PlayerMoveAI>();

        if (moveAI == null)
        {
            Destroy(gameObject);
            return;
        }

        moveAI.StartCoroutine(
      moveAI.BoomHitEffect(power)
  );

        Destroy(gameObject);
    }
}