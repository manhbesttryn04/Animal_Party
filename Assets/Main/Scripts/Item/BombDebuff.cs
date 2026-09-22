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

    private bool hasTriggeredDuck = false;

    private void Start()
    {
        startPos = transform.position;

        if (target != null)
        {
            StartCoroutine(FlyRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        CheckDistanceToPlayer();
    }

    // =========================
    // FLY TO TARGET
    // =========================

    private IEnumerator FlyRoutine()
    {
        float time = 0f;

        while (time < flyTime)
        {
            if (target == null)
            {
                yield break;
            }

            time += Time.deltaTime;

            float t = time / flyTime;

            Vector3 pos =
                Vector3.Lerp(
                    startPos,
                    target.position,
                    t
                );

            pos.y +=
                arcHeight *
                Mathf.Sin(t * Mathf.PI);

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
        // =========================
        // SOUND
        // =========================

        if (AudioManager.Instance != null)
        {
            if (AudioManager.Instance.boomClip != null)
            {
                AudioManager.Instance.PlaySFX(
                    AudioManager.Instance.boomClip
                );
            }
        }

        // =========================
        // EXPLOSION FX
        // =========================

        if (explosionPrefab != null)
        {
            GameObject explosion =
                Instantiate(
                    explosionPrefab,
                    transform.position,
                    Quaternion.identity
                );

            if (explosion != null)
            {
                Destroy(explosion, 2f);
            }
        }

        // =========================
        // TARGET CHECK
        // =========================

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

        if (player.playerBuff != null &&
            player.playerBuff.isBuffDeffense)
        {
            // Shield voice
            if (AudioManager.Instance != null)
            {
                if (AudioManager.Instance.cannonShieldVoiceClip != null)
                {
                    AudioManager.Instance.PlaySpecial(
                        AudioManager.Instance.cannonShieldVoiceClip
                    );
                }
            }

            // Shield UI
            if (UIManager.Instance != null)
            {
                if (UIManager.Instance.cannonShieldPanel != null)
                {
                    StartCoroutine(
                        UIManager.Instance.ShowDebuffAndBuffPanel(
                            UIManager.Instance.cannonShieldPanel
                        )
                    );
                }
            }

            // =========================
            // DEFENSE ANIMATION
            // =========================

            if (player.playerAnimator != null)
            {
                if (player.playerAnimator.playerAnimator != null)
                {
                    player.playerAnimator.playerAnimator.SetTrigger(
                        "Defense Cannon Debuff"
                    );
                }
            }

            // =========================
            // REMOVE SHIELD BUFF
            // =========================

            //player.playerBuff.isBuffDeffense = false;

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

    // =========================
    // CHECK DISTANCE TO PLAYER
    // =========================

    private void CheckDistanceToPlayer()
    {
        if (target == null ||
            hasTriggeredDuck)
        {
            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                target.position
            );

        if (distance <= 5f)
        {
            PlayerManager player =
                target.GetComponent<PlayerManager>();

            if (player != null)
            {
                // =========================
                // DUCK
                // =========================

                if (player.playerBuff != null &&
                    !player.playerBuff.isBuffDeffense)
                {
                    if (player.playerAnimator != null)
                    {
                        if (player.playerAnimator.playerAnimator != null)
                        {
                            player.playerAnimator.playerAnimator.SetTrigger(
                                "Duck"
                            );
                        }
                    }
                }

                // Khóa không cho gọi Duck lần 2
                hasTriggeredDuck = true;
            }
        }
    }
}