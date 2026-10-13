using System.Collections.Generic;
using UnityEngine;

public class FixBugMiniGame3 : MonoBehaviour
{
    public static FixBugMiniGame3 Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MiniGameManager miniGameManager;

    [Header("Recover Settings")]
    [SerializeField] private float recoverDelay = 1.5f;

    [Header("Current Players")]
    [SerializeField] private PlayerMove player1;
    [SerializeField] private PlayerMove player2;

    [Header("Recover Flags")]
    [SerializeField] private bool player1NeedRecover;
    [SerializeField] private bool player2NeedRecover;

    [Header("Current Timers")]
    [SerializeField] private float player1RecoverTimer;
    [SerializeField] private float player2RecoverTimer;

    [Header("State")]
    [SerializeField] private bool isRunning;

    // =========================
    // PLAYER 1 DATA
    // =========================

    private Renderer[] player1Renderers;

    private readonly List<Material> player1Materials =
        new List<Material>();

    private readonly List<Color> player1OriginalColors =
        new List<Color>();

    private float player1OriginalHeight;
    private float player1OriginalCenterY;

    // =========================
    // PLAYER 2 DATA
    // =========================

    private Renderer[] player2Renderers;

    private readonly List<Material> player2Materials =
        new List<Material>();

    private readonly List<Color> player2OriginalColors =
        new List<Color>();

    private float player2OriginalHeight;
    private float player2OriginalCenterY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!isRunning)
            return;

        UpdatePlayer1Recover();
        UpdatePlayer2Recover();
    }

    // =====================================================
    // SETUP
    // =====================================================

    /// <summary>
    /// Gọi khi MiniGame 3 bắt đầu.
    /// Phải gọi sau khi MiniGameManager đã lấy được hai player.
    /// </summary>
    public void SetupPlayers()
    {
        StopFixBug();
        ClearData();

        if (miniGameManager == null)
        {
          

            return;
        }

        player1 = GetPlayerMove(
            miniGameManager.currentPlayer1
        );

        player2 = GetPlayerMove(
            miniGameManager.currentPlayer2
        );

        SaveOriginalPlayerData(
            player1,
            out player1Renderers,
            player1Materials,
            player1OriginalColors
        );

        SaveOriginalPlayerData(
            player2,
            out player2Renderers,
            player2Materials,
            player2OriginalColors
        );

        SaveControllerData(
            player1,
            out player1OriginalHeight,
            out player1OriginalCenterY
        );

        SaveControllerData(
            player2,
            out player2OriginalHeight,
            out player2OriginalCenterY
        );

        isRunning = true;

       
    }

    private PlayerMove GetPlayerMove(GameObject playerObject)
    {
        if (playerObject == null)
            return null;

        PlayerMove move =
            playerObject.GetComponent<PlayerMove>();

        if (move != null)
            return move;

        return playerObject
            .GetComponentInChildren<PlayerMove>(true);
    }

    private void SaveControllerData(
        PlayerMove move,
        out float originalHeight,
        out float originalCenterY)
    {
        originalHeight = 0f;
        originalCenterY = 0f;

        if (move == null || move.controller == null)
        {
            if (move != null)
            {
              
            }

            return;
        }

        originalHeight = move.controller.height;
        originalCenterY = move.controller.center.y;

       
    }

    private void SaveOriginalPlayerData(
        PlayerMove move,
        out Renderer[] renderers,
        List<Material> materials,
        List<Color> originalColors)
    {
        renderers = null;

        materials.Clear();
        originalColors.Clear();

        if (move == null)
            return;

        if (move.transform.childCount == 0)
        {
           

            return;
        }

        // Model của player nằm ở Child 0
        Transform playerModel =
            move.transform.GetChild(0);

        renderers =
            playerModel.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer currentRenderer in renderers)
        {
            if (currentRenderer == null)
                continue;

            Material[] rendererMaterials =
                currentRenderer.materials;

            foreach (Material material in rendererMaterials)
            {
                if (material == null)
                    continue;

                materials.Add(material);

                if (material.HasProperty("_BaseColor"))
                {
                    originalColors.Add(
                        material.GetColor("_BaseColor")
                    );
                }
                else if (material.HasProperty("_Color"))
                {
                    originalColors.Add(
                        material.GetColor("_Color")
                    );
                }
                else
                {
                    originalColors.Add(Color.white);
                }
            }
        }
    }

    // =====================================================
    // UPDATE RECOVER
    // =====================================================

    private void UpdatePlayer1Recover()
    {
        if (!player1NeedRecover)
        {
            player1RecoverTimer = 0f;
            return;
        }

        if (player1 == null)
        {
            player1NeedRecover = false;
            player1RecoverTimer = 0f;
            return;
        }

        player1RecoverTimer += Time.deltaTime;

        if (player1RecoverTimer < recoverDelay)
            return;

        ForceRecoverPlayer(player1);
    }

    private void UpdatePlayer2Recover()
    {
        if (!player2NeedRecover)
        {
            player2RecoverTimer = 0f;
            return;
        }

        if (player2 == null)
        {
            player2NeedRecover = false;
            player2RecoverTimer = 0f;
            return;
        }

        player2RecoverTimer += Time.deltaTime;

        if (player2RecoverTimer < recoverDelay)
            return;

        ForceRecoverPlayer(player2);
    }

    // =====================================================
    // SET RECOVER FLAG
    // =====================================================

    /// <summary>
    /// Gọi một lần khi player vừa dính điện hoặc lửa.
    /// Script không lưu và không thay đổi vị trí player.
    /// </summary>
    public void SetNeedRecover(PlayerMove move)
    {
        if (!isRunning || move == null)
            return;

        if (move == player1)
        {
            player1NeedRecover = true;
            player1RecoverTimer = 0f;

            
            
        }
        else if (move == player2)
        {
            player2NeedRecover = true;
            player2RecoverTimer = 0f;

            
        }
    }

    /// <summary>
    /// Dùng khi code đang có GameObject player.
    /// </summary>
    public void SetNeedRecover(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayerMove move =
            playerObject.GetComponent<PlayerMove>();

        if (move == null)
        {
            move = playerObject
                .GetComponentInParent<PlayerMove>();
        }

        if (move == null)
        {
            move = playerObject
                .GetComponentInChildren<PlayerMove>(true);
        }

        SetNeedRecover(move);
    }

    // =====================================================
    // FORCE RECOVER
    // =====================================================

    public void ForceRecoverPlayer(PlayerMove move)
    {
        if (move == null)
            return;

        move.isMove = true;
        move.isJump = true;

        if (move == player1)
        {
            RestorePlayer(
                move,
                player1Renderers,
                player1Materials,
                player1OriginalColors,
                player1OriginalHeight,
                player1OriginalCenterY
            );

            player1NeedRecover = false;
            player1RecoverTimer = 0f;
        }
        else if (move == player2)
        {
            RestorePlayer(
                move,
                player2Renderers,
                player2Materials,
                player2OriginalColors,
                player2OriginalHeight,
                player2OriginalCenterY
            );

            player2NeedRecover = false;
            player2RecoverTimer = 0f;
        }
        else
        {
            return;
        }

        RemoveEffectObjects(move);
        ResetAnimator(move);

        
    }

    private void RestorePlayer(
        PlayerMove move,
        Renderer[] renderers,
        List<Material> materials,
        List<Color> originalColors,
        float originalHeight,
        float originalCenterY)
    {
        if (move == null)
            return;

        // Không thay đổi transform position của player.

        if (move.controller != null)
        {
            move.controller.height = originalHeight;

            Vector3 center = move.controller.center;
            center.y = originalCenterY;
            move.controller.center = center;
        }

        if (renderers != null)
        {
            foreach (Renderer currentRenderer in renderers)
            {
                if (currentRenderer == null)
                    continue;

                currentRenderer.enabled = true;
            }
        }

        int materialCount = Mathf.Min(
            materials.Count,
            originalColors.Count
        );

        for (int i = 0; i < materialCount; i++)
        {
            Material material = materials[i];

            if (material == null)
                continue;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    originalColors[i]
                );
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor(
                    "_Color",
                    originalColors[i]
                );
            }
        }
    }

    private void ResetAnimator(PlayerMove move)
    {
        if (move == null)
            return;

        if (move.manager == null)
            return;

        if (move.manager.playerAnimator == null)
            return;

        Animator animator =
            move.manager.playerAnimator.playerAnimator;

        if (animator == null)
            return;

        animator.SetFloat("Walk", 0f);
        animator.Rebind();
        animator.Update(0f);
    }

    // =====================================================
    // REMOVE EFFECT
    // =====================================================

    private void RemoveEffectObjects(PlayerMove move)
    {
        if (move == null)
            return;

        Transform[] allChildren =
            move.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child == null)
                continue;

            if (child == move.transform)
                continue;

            if (child.CompareTag("Fire"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    // =====================================================
    // STOP
    // =====================================================

    /// <summary>
    /// Gọi khi MiniGame 3 kết thúc hoặc bị dừng.
    /// </summary>
    public void StopAndClearPlayers()
    {
        isRunning = false;

        if (player1 != null)
        {
            ForceRecoverPlayer(player1);
        }

        if (player2 != null)
        {
            ForceRecoverPlayer(player2);
        }

        ClearData();

      
    }

    private void StopFixBug()
    {
        isRunning = false;

        player1NeedRecover = false;
        player2NeedRecover = false;

        player1RecoverTimer = 0f;
        player2RecoverTimer = 0f;
    }

    private void ClearData()
    {
        player1NeedRecover = false;
        player2NeedRecover = false;

        player1RecoverTimer = 0f;
        player2RecoverTimer = 0f;

        player1OriginalHeight = 0f;
        player1OriginalCenterY = 0f;

        player2OriginalHeight = 0f;
        player2OriginalCenterY = 0f;

        player1 = null;
        player2 = null;

        player1Renderers = null;
        player2Renderers = null;

        player1Materials.Clear();
        player1OriginalColors.Clear();

        player2Materials.Clear();
        player2OriginalColors.Clear();
    }

    private void OnDestroy()
    {
        StopFixBug();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}