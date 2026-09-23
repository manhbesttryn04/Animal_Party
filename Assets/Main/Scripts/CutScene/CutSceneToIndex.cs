using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class CutSceneToIndex : MonoBehaviour
{
    public static CutSceneToIndex Instance;

    [Header("Teleport")]
    public GameObject teleport;
    private ParticleSystem teleportParticle;

    [Header("Camera Points")]
    public Transform[] transformsCutScene;
    // 0 = Camera điểm đầu

    [Header("Player Walk Point")]
    public Transform transPlayerToWalk;

    [Header("Settings")]
    public float cameraMoveTime = 1f;
    public float waitTime = 0.5f;
    public float scaleTime = 0.5f;

    [Header("Player Move")]
    public float playerMoveSpeed = 0.5f;

    [Header("Camera Behind Player")]
    public float cameraBackDistance = 3f;

    private Vector3 teleportOriginalScale;

    private void Awake()
    {
        SetupSingleton();
        SetupTeleport();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void SetupTeleport()
    {
        if (teleport != null)
        {
            teleportOriginalScale = teleport.transform.localScale;
            teleportParticle = teleport.GetComponent<ParticleSystem>();

            if (teleportParticle != null)
                teleportParticle.Play();
        }
    }
    public void PlayCutScene(GameObject playerWinner)
    {
        StartCoroutine(CutSceneRoutine(playerWinner));
    }

    private IEnumerator CutSceneRoutine(GameObject playerWinner)
    {
        if (playerWinner == null || teleport == null)
            yield break;

        // 1. Bắt đầu vignette
        VolumeManager.Instance.StartVignette();

        // 2. Camera đi tới point 0 trước
        if (transformsCutScene != null &&
            transformsCutScene.Length > 0 &&
            transformsCutScene[0] != null)
        {
            yield return StartCoroutine(
                CameraManager.Instance.MoveToPoint(transformsCutScene[0], cameraMoveTime)
            );
        }

        yield return new WaitForSeconds(waitTime);

        // 3. Camera đi ra sau lưng player
        Vector3 cameraPos =
            playerWinner.transform.position -
            playerWinner.transform.forward * cameraBackDistance;

        cameraPos.y = teleport.transform.position.y;

        Quaternion cameraRot = Camera.main.transform.rotation;

        yield return StartCoroutine(
            CameraManager.Instance.MoveToPosition(cameraPos, cameraRot, cameraMoveTime)
        );

        yield return new WaitForSeconds(waitTime);

        yield return new WaitForSeconds(waitTime);

        // 5. Player đi vào cổng
        if (transPlayerToWalk != null)
        {
           StartCoroutine(
                MovePlayerToPoint(playerWinner, transPlayerToWalk.position)
            );
        }
        yield return new WaitForSeconds(4.5f);

        // 6. Player tan biến bằng VFX không particle
        AudioManager.Instance.PlaySFX(AudioManager.Instance.playerTeleport);

        PlayerVFX playerVFX = playerWinner.GetComponent<PlayerVFX>();

        if (playerVFX != null)
        {
            yield return StartCoroutine(
                playerVFX.DissolveOutNoParticleRoutine()
            );
        }
        else
        {
            SetPlayerMeshActive(playerWinner, false);
        }

        yield return new WaitForSeconds(waitTime);

        // 7. Đóng cổng khi player đã vào
        AudioManager.Instance.PlaySFX(AudioManager.Instance.closeTeleportClip);

        yield return StartCoroutine(
            ScaleTeleport(Vector3.zero, scaleTime)
        );
        SceneManager.LoadScene(5);
    }

    private IEnumerator ScaleTeleport(Vector3 targetScale, float duration)
    {
        Vector3 startScale = teleport.transform.localScale;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            teleport.transform.localScale =
                Vector3.Lerp(startScale, targetScale, t / duration);

            yield return null;
        }

        teleport.transform.localScale = targetScale;
    }

    private IEnumerator MovePlayerToPoint(GameObject player, Vector3 targetPos)
    {
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        PlayerManager playerManager = player.GetComponent<PlayerManager>();

        targetPos.y = player.transform.position.y;

        if (playerManager != null && playerManager.playerAnimator != null)
        {
            playerManager.playerAnimator.playerAnimator.SetFloat("Walk", 1f);
        }

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.speed = playerMoveSpeed;
            agent.SetDestination(targetPos);

            while (agent.pathPending ||
                   agent.remainingDistance > agent.stoppingDistance)
            {
                if (playerManager != null && playerManager.playerAnimator != null)
                {
                    playerManager.playerAnimator.playerAnimator.SetFloat(
                        "Walk",
                        agent.velocity.magnitude
                    );
                }

                yield return null;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        if (playerManager != null && playerManager.playerAnimator != null)
        {
            playerManager.playerAnimator.playerAnimator.SetFloat("Walk", 0f);
        }
    }

    private void SetPlayerMeshActive(GameObject player, bool active)
    {
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            r.enabled = active;
        }
    }
}