using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class CutScenePowerCoin : MonoBehaviour
{
    public static CutScenePowerCoin Instance;

    [Header("Teleport")]
    public GameObject teleport;
    private ParticleSystem teleportParticle;

    [Header("Camera Points")]
    public Transform[] transformsCutScene;
    // 0 = Camera đầu

    [Header("Player Walk Point")]
    public Transform transPlayerToWalk;

    [Header("Settings")]
    public float cameraMoveTime = 1f;
    public float waitTime = 0.5f;
    public float scaleTime = 0.5f;

    [Header("Player Move")]
    public float playerMoveSpeed = 0.5f;
    public float teleportForwardDistance = 1f;

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
            DontDestroyOnLoad(gameObject); // giữ lại khi đổi scene
        }
        else
        {
            Destroy(gameObject);
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

    public void PlayCutScene(GameObject player)
    {
        StartCoroutine(CutSceneRoutine(player));
    }

    private IEnumerator CutSceneRoutine(GameObject player)
    {
        if (player == null || teleport == null)
            yield break;
        VolumeManager.Instance.StartVignette();
        // 1. Camera tới điểm đầu
        if (transformsCutScene.Length > 0 && transformsCutScene[0] != null)
        {
            yield return StartCoroutine(
                CameraManager.Instance.MoveToPoint(transformsCutScene[0], cameraMoveTime)
            );
        }

        yield return new WaitForSeconds(waitTime);

        AudioManager.Instance.PlaySFX(AudioManager.Instance.closeTeleportClip);
        // 2. Thu nhỏ cổng
        yield return StartCoroutine(ScaleTeleport(Vector3.zero, scaleTime));
      

        yield return new WaitForSeconds(waitTime);

        // 3. Đặt cổng trước mặt player
        Vector3 pos =
            player.transform.position +
            player.transform.forward * teleportForwardDistance;

        teleport.transform.position = new Vector3(
            pos.x,
            teleport.transform.position.y,
            pos.z
        );

        teleport.transform.rotation =
            Quaternion.LookRotation(player.transform.forward);

        yield return new WaitForSeconds(0.2f);

        // 4. Camera di chuyển ra sau player
        // XZ theo sau lưng player, Y lấy theo teleport
        // Rotation giữ nguyên, không LookAt
        Vector3 cameraPos =
            player.transform.position -
            player.transform.forward * cameraBackDistance;

        cameraPos.y = teleport.transform.position.y;

        Quaternion cameraRot = Camera.main.transform.rotation;

        yield return StartCoroutine(
            CameraManager.Instance.MoveToPosition(cameraPos, cameraRot, cameraMoveTime)
        );

        yield return new WaitForSeconds(waitTime);

        // 5. Mở cổng
        AudioManager.Instance.PlaySFX(AudioManager.Instance.openTeleportClip);
        yield return StartCoroutine(
            ScaleTeleport(teleportOriginalScale, scaleTime)
        );
       
        yield return new WaitForSeconds(waitTime);

        // 6. Player dùng NavMeshAgent đi qua cổng
        if (transPlayerToWalk != null)
        {
            StartCoroutine(
                MovePlayerToPoint(player, transPlayerToWalk.position)
            );
        }
         yield return new WaitForSeconds(4.5f);

        // 7. Player biến mất  từ từ
        PlayerVFX playerVFX = player.GetComponent<PlayerVFX>();

        if (playerVFX != null)
        {
            yield return StartCoroutine(playerVFX.DissolveOutNoParticleRoutine());
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerTeleport);
        }
        else
        {
            SetPlayerMeshActive(player, false);
        }

        yield return new WaitForSeconds(waitTime);
        // 8. Đóng cổng
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

        // Bỏ Y của điểm đến, giữ Y player
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
        else
        {
            while (Vector3.Distance(player.transform.position, targetPos) > 0.05f)
            {
                Vector3 dir = targetPos - player.transform.position;
                dir.y = 0f;
                dir.Normalize();

                player.transform.position +=
                    dir * playerMoveSpeed * Time.deltaTime;

                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);

                    player.transform.rotation = Quaternion.Slerp(
                        player.transform.rotation,
                        targetRot,
                        Time.deltaTime * 3f
                    );
                }

                yield return null;
            }

            player.transform.position = new Vector3(
                targetPos.x,
                player.transform.position.y,
                targetPos.z
            );
        }

        if (playerManager != null && playerManager.playerAnimator != null)
        {
            playerManager.playerAnimator.playerAnimator.SetFloat("Walk", 0f);
        }
    }

    private void SetPlayerMeshActive(GameObject player, bool active)
    {
        Renderer[] renderers =
            player.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            r.enabled = active;
        }
    }
}