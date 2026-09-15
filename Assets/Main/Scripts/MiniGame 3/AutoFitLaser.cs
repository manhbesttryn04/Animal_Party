using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AutoFitLaser : MonoBehaviour
{
    public LineRenderer line;

    [Header("--- Cài đặt Laser ---")]
    public float laserWidth = 0.5f;
    public float maxLaserDistance = 50f;
    public float fadeSpeed = 10f;

    [Header("--- Layer ---")]
    public LayerMask obstacleLayer;
    public LayerMask targetLayer;

    [Header("--- Player Hit ---")]
    public int coinPenalty = 5;
    public float hitCooldown = 0.5f;
    public float stunTime = 0.2f;

    [Header("--- Player Electric Effect ---")]
    public float playerShakeAmount = 0.08f;
    public float flashDuration = 0.4f;
    public float flashInterval = 0.05f;

    [Header("--- VFX chạm tường ---")]
    public ParticleSystem wallImpact;
    public float impactOffset = 0.05f;

    public float currentWidthMultiplier = 0f;
    public float targetWidthMultiplier = 0f;

    private readonly Dictionary<PlayerMove, float> lastHitTimes = new();
    private readonly HashSet<PlayerMove> electricPlayers = new HashSet<PlayerMove>();

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = 0f;
        line.endWidth = 0f;

        HideWallImpact();
    }

    private void Update()
    {
        currentWidthMultiplier = Mathf.MoveTowards(
            currentWidthMultiplier,
            targetWidthMultiplier,
            fadeSpeed * Time.deltaTime
        );

        bool laserVisible = currentWidthMultiplier > 0.1f;
        float calculatedWidth = laserWidth * currentWidthMultiplier;

        line.startWidth = calculatedWidth;
        line.endWidth = calculatedWidth;

        Vector3 startPoint = transform.position;
        Vector3 direction = transform.forward;
        Vector3 endPoint = startPoint + direction * maxLaserDistance;

        line.SetPosition(0, startPoint);
        line.SetPosition(1, endPoint);

        if (Physics.Raycast(
            startPoint,
            direction,
            out RaycastHit wallHit,
            maxLaserDistance,
            obstacleLayer,
            QueryTriggerInteraction.Ignore))
        {
            if (laserVisible)
                ShowWallImpact(wallHit);
            else
                HideWallImpact();
        }
        else
        {
            HideWallImpact();
        }

        if (laserVisible)
        {
            CheckHitPlayer(calculatedWidth, maxLaserDistance);
        }
    }

    private void ShowWallImpact(RaycastHit wallHit)
    {
        if (wallImpact == null) return;

        wallImpact.transform.position =
            wallHit.point + wallHit.normal * impactOffset;

        wallImpact.transform.rotation =
            Quaternion.LookRotation(wallHit.normal);

        if (!wallImpact.isPlaying)
            wallImpact.Play();
    }

    private void HideWallImpact()
    {
        if (wallImpact == null) return;

        if (wallImpact.isPlaying)
            wallImpact.Stop();
    }

    private void CheckHitPlayer(float calculatedWidth, float laserLength)
    {
        float radius = calculatedWidth / 2f;

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            radius,
            transform.forward,
            laserLength,
            targetLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            PlayerMove move =
                hit.collider.GetComponentInParent<PlayerMove>();

            if (move == null) continue;
            if (!CanHit(move)) continue;

            HitPlayer(move);
            lastHitTimes[move] = Time.time;
        }
    }

    private bool CanHit(PlayerMove move)
    {
        if (lastHitTimes.TryGetValue(move, out float lastTime))
            return Time.time - lastTime >= hitCooldown;

        return true;
    }

    private void HitPlayer(PlayerMove move)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.laserHitClip);

        PlayerMiniGame mini = move.GetComponent<PlayerMiniGame>();

        if (mini != null)
            mini.UpCoin(0, coinPenalty);
        if (FixBugMiniGame3.Instance != null)
        {
            FixBugMiniGame3.Instance.SetNeedRecover(move);
        }

        StartCoroutine(ElectricStun(move));
        if (!electricPlayers.Contains(move))
        {
            StartCoroutine(PlayerElectricEffect(move));
        }
    }

    private IEnumerator ElectricStun(PlayerMove move)
    {
     
        move.isMove = false;
        move.isJump = false;

        if (move.manager != null &&
            move.manager.playerAnimator != null)
        {
            move.manager.playerAnimator.playerAnimator.SetTrigger("Lie");
        }

        yield return new WaitForSeconds(stunTime);

        move.isMove = true;
        move.isJump = true;
       
    }

    private IEnumerator PlayerElectricEffect(PlayerMove move)
    {
        electricPlayers.Add(move);
        Transform playerTransform = move.transform;
        Vector3 originalLocalPos = playerTransform.localPosition;

        Renderer[] renderers =
            move.GetComponentsInChildren<Renderer>();

        List<Material> materials = new List<Material>();
        List<Color> originalColors = new List<Color>();

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                materials.Add(mat);

                if (mat.HasProperty("_BaseColor"))
                    originalColors.Add(mat.GetColor("_BaseColor"));
                else if (mat.HasProperty("_Color"))
                    originalColors.Add(mat.GetColor("_Color"));
                else
                    originalColors.Add(Color.white);
            }
        }

        float timer = 0f;
        bool white = false;

        while (timer < flashDuration)
        {
            Vector3 shakeOffset = new Vector3(
                Random.Range(-playerShakeAmount, playerShakeAmount),
                Random.Range(-playerShakeAmount, playerShakeAmount),
                Random.Range(-playerShakeAmount, playerShakeAmount)
            );

            playerTransform.localPosition =
                originalLocalPos + shakeOffset;

            Color flashColor = white ? Color.white : Color.black;

            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i].HasProperty("_BaseColor"))
                    materials[i].SetColor("_BaseColor", flashColor);
                else if (materials[i].HasProperty("_Color"))
                    materials[i].SetColor("_Color", flashColor);
            }

            white = !white;

            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        playerTransform.localPosition = originalLocalPos;

        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i].HasProperty("_BaseColor"))
                materials[i].SetColor("_BaseColor", originalColors[i]);
            else if (materials[i].HasProperty("_Color"))
                materials[i].SetColor("_Color", originalColors[i]);
        }
        electricPlayers.Remove(move);

       
    }

    public void SetLaserActive(bool isActive)
    {
        targetWidthMultiplier = isActive ? 1f : 0f;

        if (!isActive)
            HideWallImpact();
    }
}