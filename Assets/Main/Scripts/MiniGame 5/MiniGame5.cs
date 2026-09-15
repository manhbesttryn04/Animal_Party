using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum PadHazardType
{
    None,
    Bomb,
    Freeze
}

public class MiniGame5 : MonoBehaviour
{
    [Header("Minigame Manager")]
    public MiniGameManager manager;
    public bool isPlaying = false;

    [Header("Danh sách các ô")]
    public List<GameObject> allPadRenderers = new List<GameObject>();

    [Header("Material")]
    public Material defaultMaterial;
    public Material player1Material;
    public Material player2Material;
    public Material bombMaterial;
    public Material freezeMaterial;

    [Header("Bomb Prefab GIẢ")]
    public GameObject bombPrefab;
    public float bombSpawnHeight = 0.6f;

    [Header("Bomb Flash")]
    public Color bombFlashColor = new Color(1f, 0.5f, 0f);
    public float bombFlashSpeed = 8f;
    public float hazardResetInterval = 5f;

    [Header("Grow Item")]
    public GameObject growItemPrefab;
    public float growDuration = 5f;
    public float itemExistDuration = 5f;
    public float growMultiplier = 3f;
    public float itemSpawnHeight = 0.5f;

    [Header("UI")]
    public TextMeshProUGUI resultText;
    public float gameDuration = 57f;

    private readonly List<PaintPadData> allPads =
        new List<PaintPadData>();

    private bool isPlayer1Frozen = false;
    private bool isPlayer2Frozen = false;
    private bool isFinishing = false;

    private float gameplayTimeLeft = 0f;

    private Vector3 player1OriginalScale = Vector3.one;
    private Vector3 player2OriginalScale = Vector3.one;

    private GameObject currentSpawnedItem;
    private Coroutine itemDestroyCoroutine;

    private void Awake()
    {
        InitializeManualPads();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        foreach (PaintPadData pad in allPads)
        {
            if (pad == null)
                continue;

            pad.UpdateDetection();

            if (pad.hazardType == PadHazardType.Bomb)
            {
                pad.UpdateBombFlashing(
                    Color.white,
                    bombFlashColor,
                    bombFlashSpeed
                );
            }
        }
    }

    private void InitializeManualPads()
    {
        allPads.Clear();

        foreach (GameObject padObject in allPadRenderers)
        {
            if (padObject == null)
                continue;

            BoxCollider[] colliders =
                padObject.GetComponents<BoxCollider>();

            foreach (BoxCollider col in colliders)
            {
                if (col != null)
                    col.isTrigger = false;
            }

            MeshRenderer padRenderer =
                padObject.GetComponent<MeshRenderer>();

            if (padRenderer == null)
                continue;

            PaintPadData data = new PaintPadData(
                padObject,
                padRenderer,
                this
            );

            allPads.Add(data);
            data.ResetColor();
        }
    }

    public void StartMiniGame()
    {
        if (isPlaying || isFinishing)
            return;

        if (manager == null ||
            manager.currentPlayer1 == null ||
            manager.currentPlayer2 == null ||
            allPads.Count == 0)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnvironment(
                AudioManager.Instance.snowFallClip
            );
        }

        isPlaying = true;
        isFinishing = false;
        isPlayer1Frozen = false;
        isPlayer2Frozen = false;
        gameplayTimeLeft = Mathf.Max(0f, gameDuration);

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = "";
        }

        ResetAllPadsToDefault();
        SetUpPlayer();

        StartCoroutine(PaintGameRoutine());
        StartCoroutine(SpawnHazardsRoutine());
        StartCoroutine(SpawnGrowItemPrefabRoutine());
    }

    public void StopMiniGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopEnvironment();
        }

        isPlaying = false;
        isFinishing = false;
        gameplayTimeLeft = 0f;

        StopAllCoroutines();
        itemDestroyCoroutine = null;

        ResetAllPadsToDefault();

        if (manager != null)
        {
            SetHighlightPlayer(manager.currentPlayer1, false);
            SetHighlightPlayer(manager.currentPlayer2, false);
        }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
        }
    }

    private IEnumerator PaintGameRoutine()
    {
        while (gameplayTimeLeft > 0f && isPlaying)
        {
            gameplayTimeLeft -= Time.deltaTime;
            yield return null;
        }

        if (!isPlaying)
            yield break;

        gameplayTimeLeft = 0f;
        isPlaying = false;
        isFinishing = true;

        ClearCurrentSpawnedItem();
        CalculateFinalScore();

        StartCoroutine(ResetPadsAfterResult());
    }

    private IEnumerator ResetPadsAfterResult()
    {
        yield return new WaitForSeconds(3.5f);
        ResetAllPadsToDefault();
    }

    private void ResetAllPadsToDefault()
    {
        foreach (PaintPadData pad in allPads)
        {
            if (pad != null)
                pad.ResetColor();
        }

        ClearCurrentSpawnedItem();

        isPlayer1Frozen = false;
        isPlayer2Frozen = false;
    }

    public void SetUpPlayer()
    {
        if (manager == null)
            return;

        SetHighlightPlayer(manager.currentPlayer1, true);
        SetHighlightPlayer(manager.currentPlayer2, true);
    }

    private void SetHighlightPlayer(
        GameObject playerObject,
        bool isActive
    )
    {
        if (playerObject == null)
            return;

        foreach (Transform child in
                 playerObject.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Hight Light Player")
                continue;

            child.gameObject.SetActive(isActive);
            break;
        }
    }

    private IEnumerator SpawnHazardsRoutine()
    {
        while (isPlaying)
        {
            ClearCurrentHazards();

            int bombCount = gameplayTimeLeft > 30f
                ? Random.Range(2, 11)
                : Random.Range(11, 21);

            int freezeCount = Random.Range(1, 6);

            List<PaintPadData> availablePads =
                new List<PaintPadData>();

            foreach (PaintPadData pad in allPads)
            {
                if (pad != null &&
                    pad.hazardType == PadHazardType.None)
                {
                    availablePads.Add(pad);
                }
            }

            ShufflePads(availablePads);

            int totalHazards = Mathf.Min(
                bombCount + freezeCount,
                availablePads.Count
            );

            int currentIndex = 0;

            for (int i = 0; i < bombCount; i++)
            {
                if (currentIndex >= totalHazards)
                    break;

                PaintPadData pad = availablePads[currentIndex];
                pad.hazardType = PadHazardType.Bomb;
                pad.ApplyHazardVisual();
                currentIndex++;
            }

            for (int i = 0; i < freezeCount; i++)
            {
                if (currentIndex >= totalHazards)
                    break;

                PaintPadData pad = availablePads[currentIndex];
                pad.hazardType = PadHazardType.Freeze;
                pad.ApplyHazardVisual();
                currentIndex++;
            }

            yield return new WaitForSeconds(
                Mathf.Max(0.1f, hazardResetInterval)
            );
        }
    }

    private void ClearCurrentHazards()
    {
        foreach (PaintPadData pad in allPads)
        {
            if (pad == null)
                continue;

            if (pad.hazardType != PadHazardType.Bomb &&
                pad.hazardType != PadHazardType.Freeze)
            {
                continue;
            }

            pad.hazardType = PadHazardType.None;
            pad.RestoreVisualAfterHazard();
            pad.RemoveSpawnedBomb();
        }
    }

    private static void ShufflePads(List<PaintPadData> pads)
    {
        for (int i = 0; i < pads.Count; i++)
        {
            int randomIndex = Random.Range(i, pads.Count);

            PaintPadData temp = pads[i];
            pads[i] = pads[randomIndex];
            pads[randomIndex] = temp;
        }
    }

    private IEnumerator SpawnGrowItemPrefabRoutine()
    {
        yield return new WaitForSeconds(5f);

        while (isPlaying)
        {
            if (growItemPrefab != null && allPads.Count > 0)
            {
                ClearCurrentSpawnedItem();

                PaintPadData randomPad =
                    allPads[Random.Range(0, allPads.Count)];

                if (randomPad != null &&
                    randomPad.padObject != null)
                {
                    Vector3 spawnPosition =
                        randomPad.padObject.transform.position +
                        Vector3.up * itemSpawnHeight;

                    currentSpawnedItem = Instantiate(
                        growItemPrefab,
                        spawnPosition,
                        Quaternion.identity
                    );

                    GrowItem itemScript =
                        currentSpawnedItem.GetComponent<GrowItem>();

                    if (itemScript != null)
                        itemScript.Setup(this);

                    itemDestroyCoroutine = StartCoroutine(
                        DestroyItemAfterDelay(
                            currentSpawnedItem,
                            itemExistDuration
                        )
                    );
                }
            }

            yield return new WaitForSeconds(11f);
        }
    }

    private IEnumerator DestroyItemAfterDelay(
        GameObject item,
        float delay
    )
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        if (item != null && item == currentSpawnedItem)
        {
            Destroy(item);
            currentSpawnedItem = null;
        }

        itemDestroyCoroutine = null;
    }

    public void OnPadTriggered(
        PaintPadData padData,
        GameObject playerObject
    )
    {
        if (!isPlaying || padData == null || playerObject == null)
            return;

        PlayerType playerType =
            playerObject.GetComponent<PlayerType>();

        if (playerType == null)
            return;

        bool isPlayer2 = playerType.isPlayer2;

        if (!isPlayer2 && isPlayer1Frozen)
            return;

        if (isPlayer2 && isPlayer2Frozen)
            return;

        if (padData.hazardType == PadHazardType.Bomb)
        {
            TriggerBombExplosion(padData);
            return;
        }

        if (padData.hazardType == PadHazardType.Freeze)
        {
            TriggerFreezeStatus(isPlayer2, playerObject);
            padData.ResetColor();
            return;
        }

        if (isPlayer2)
        {
            padData.SetOwner("Player 2", player2Material);
        }
        else
        {
            padData.SetOwner("Player 1", player1Material);
        }
    }

    private void TriggerBombExplosion(PaintPadData explodedPad)
    {
        GameObject bombObject = explodedPad.spawnedBomb;

        explodedPad.spawnedBomb = null;
        explodedPad.hazardType = PadHazardType.None;

        if (bombObject != null)
        {
            Bomb bomb = bombObject.GetComponent<Bomb>();

            if (bomb != null)
            {
                bomb.TriggerBomb();

                Destroy(
                    bombObject,
                    Mathf.Max(0f, bomb.explodeDelay) + 1.5f
                );
            }
            else
            {
                Destroy(bombObject);
            }
        }

        foreach (PaintPadData pad in allPads)
        {
            if (pad == null ||
                pad.padObject == null ||
                explodedPad.padObject == null)
            {
                continue;
            }

            float distance = Vector3.Distance(
                explodedPad.padObject.transform.position,
                pad.padObject.transform.position
            );

            if (distance <= 2.5f)
                pad.ResetColor();
        }
    }

    private void TriggerFreezeStatus(
        bool isPlayer2,
        GameObject playerObject
    )
    {
        if (!isPlayer2)
        {
            if (!isPlayer1Frozen)
            {
                StartCoroutine(
                    FreezePlayerRoutine(1, playerObject)
                );
            }
        }
        else if (!isPlayer2Frozen)
        {
            StartCoroutine(
                FreezePlayerRoutine(2, playerObject)
            );
        }
    }

    private IEnumerator FreezePlayerRoutine(
        int playerNumber,
        GameObject playerObject
    )
    {
        if (playerObject == null)
            yield break;

        if (playerNumber == 1)
            isPlayer1Frozen = true;
        else
            isPlayer2Frozen = true;

        Transform iceBlock = null;

        foreach (Transform child in
                 playerObject.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "IceBlock")
                continue;

            iceBlock = child;
            break;
        }

        PlayerMove movement =
            playerObject.GetComponent<PlayerMove>();

        PlayerAnimator playerAnimator =
            playerObject.GetComponent<PlayerAnimator>();

        if (iceBlock != null)
            iceBlock.gameObject.SetActive(true);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance.iceMagicClip
            );
        }

        if (movement != null)
            movement.isJumpAndMove = false;

        if (playerAnimator != null &&
            playerAnimator.playerAnimator != null)
        {
            playerAnimator.playerAnimator.speed = 0f;
        }

        yield return new WaitForSeconds(2f);

        if (movement != null)
            movement.isJumpAndMove = true;

        if (playerAnimator != null &&
            playerAnimator.playerAnimator != null)
        {
            playerAnimator.playerAnimator.speed = 1f;
        }

        if (iceBlock != null)
            iceBlock.gameObject.SetActive(false);

        if (playerNumber == 1)
            isPlayer1Frozen = false;
        else
            isPlayer2Frozen = false;
    }

    public void OnGrowItemPickedUp(
        bool isPlayer2,
        GameObject playerObject
    )
    {
        if (!isPlaying || playerObject == null)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance.buffBigClip
            );
        }

        if (itemDestroyCoroutine != null)
        {
            StopCoroutine(itemDestroyCoroutine);
            itemDestroyCoroutine = null;
        }

        GameObject pickedItem = currentSpawnedItem;
        currentSpawnedItem = null;

        if (pickedItem != null)
            Destroy(pickedItem);

        StartCoroutine(
            GrowPlayerRoutine(isPlayer2, playerObject)
        );
    }

    private IEnumerator GrowPlayerRoutine(
        bool isPlayer2,
        GameObject playerObject
    )
    {
        if (playerObject == null)
            yield break;

        CharacterController controller =
            playerObject.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        if (!isPlayer2)
            player1OriginalScale = playerObject.transform.localScale;
        else
            player2OriginalScale = playerObject.transform.localScale;

        float liftOffset = 1f;

        Collider playerCollider =
            playerObject.GetComponent<Collider>();

        if (playerCollider != null)
            liftOffset = playerCollider.bounds.size.y;

        Vector3 originalScale = isPlayer2
            ? player2OriginalScale
            : player1OriginalScale;

        Vector3 targetScale = originalScale * growMultiplier;

        playerObject.transform.localScale = targetScale;
        playerObject.transform.position +=
            Vector3.up * liftOffset *
            (growMultiplier - 1f) * 0.5f;

        if (controller != null)
            controller.enabled = true;

        float originalSpeed = 5f;

        PlayerMove movement =
            playerObject.GetComponent<PlayerMove>();

        if (movement != null)
        {
            originalSpeed = movement.speed;
            movement.speed = originalSpeed * 0.5f;
        }

        yield return new WaitForSeconds(
            Mathf.Max(0f, growDuration)
        );

        if (playerObject == null)
            yield break;

        if (controller != null)
            controller.enabled = false;

        playerObject.transform.localScale = originalScale;

        if (controller != null)
            controller.enabled = true;

        if (movement != null)
            movement.speed = originalSpeed;
    }

    private void ClearCurrentSpawnedItem()
    {
        if (itemDestroyCoroutine != null)
        {
            StopCoroutine(itemDestroyCoroutine);
            itemDestroyCoroutine = null;
        }

        if (currentSpawnedItem == null)
            return;

        Destroy(currentSpawnedItem);
        currentSpawnedItem = null;
    }

    private void CalculateFinalScore()
    {
        int player1Count = 0;
        int player2Count = 0;

        foreach (PaintPadData pad in allPads)
        {
            if (pad == null)
                continue;

            if (pad.hazardType == PadHazardType.Bomb ||
                pad.hazardType == PadHazardType.Freeze)
            {
                pad.hazardType = PadHazardType.None;
                pad.RestoreVisualAfterHazard();
                pad.RemoveSpawnedBomb();
            }

            if (pad.ownerTag == "Player 1")
                player1Count++;
            else if (pad.ownerTag == "Player 2")
                player2Count++;
        }

        if (resultText != null)
        {
            if (player1Count > player2Count)
            {
                resultText.text =
                    $"P1 win! ({player1Count} vs {player2Count})";
                resultText.color = Color.yellow;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySpecial(
                        AudioManager.Instance.playerOneWinClip
                    );
                }
            }
            else if (player2Count > player1Count)
            {
                resultText.text =
                    $"P2 win! ({player2Count} vs {player1Count})";
                resultText.color = Color.red;

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySpecial(
                        AudioManager.Instance.playerTwoWinClip
                    );
                }
            }
            else
            {
                resultText.text =
                    $"Draw! ({player1Count} vs {player2Count})";
                resultText.color = Color.orange;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySpecial(
                        AudioManager.Instance.bothPlayerDrawClip
                    );
                }
            }
        }

        ExitResultAllPlayer(player1Count, player2Count);
    }

    public void ExitResultAllPlayer(
        int countPadPlayer1,
        int countPadPlayer2
    )
    {
        if (manager == null ||
            manager.currentPlayer1 == null ||
            manager.currentPlayer2 == null)
        {
            return;
        }

        PlayerMiniGame player1 =
            manager.currentPlayer1.GetComponent<PlayerMiniGame>();

        PlayerMiniGame player2 =
            manager.currentPlayer2.GetComponent<PlayerMiniGame>();

        if (player1 == null || player2 == null)
            return;

        if (countPadPlayer1 > countPadPlayer2)
            player1.UpCoin(1, 100);
        else if (countPadPlayer2 > countPadPlayer1)
            player2.UpCoin(1, 100);
    }
}

public class PaintPadData
{
    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    public GameObject padObject;
    public MeshRenderer renderer;
    public string ownerTag = "";
    public PadHazardType hazardType = PadHazardType.None;
    public GameObject spawnedBomb;

    private readonly MiniGame5 manager;
    private readonly MaterialPropertyBlock propertyBlock =
        new MaterialPropertyBlock();

    public PaintPadData(
        GameObject pad,
        MeshRenderer padRenderer,
        MiniGame5 gameManager
    )
    {
        padObject = pad;
        renderer = padRenderer;
        manager = gameManager;
    }

    public void UpdateDetection()
    {
        if (padObject == null || manager == null)
            return;

        Vector3 centerPosition =
            padObject.transform.position +
            new Vector3(0f, 0.6f, 0f);

        Vector3 checkSize =
            new Vector3(1.1f, 0.5f, 1.1f);

        Collider[] hitColliders = Physics.OverlapBox(
            centerPosition,
            checkSize,
            padObject.transform.rotation
        );

        foreach (Collider col in hitColliders)
        {
            if (col == null)
                continue;

            GameObject playerObject = col.gameObject;

            if (playerObject.GetComponent<PlayerType>() == null)
                continue;

            Collider playerCollider =
                playerObject.GetComponent<Collider>();

            if (playerCollider == null)
                continue;

            Vector3 padCenter = padObject.transform.position;
            Vector3 closestPoint =
                playerCollider.ClosestPoint(padCenter);

            float distanceX =
                Mathf.Abs(closestPoint.x - padCenter.x);

            float distanceZ =
                Mathf.Abs(closestPoint.z - padCenter.z);

            float targetRadius = 0.42f;

            if (playerObject.transform.localScale.x > 1.5f)
            {
                targetRadius *=
                    playerObject.transform.localScale.x;
            }

            if (distanceX >= targetRadius ||
                distanceZ >= targetRadius)
            {
                continue;
            }

            manager.OnPadTriggered(this, playerObject);
        }
    }

    public void UpdateBombFlashing(
        Color firstColor,
        Color secondColor,
        float speed
    )
    {
        if (renderer == null)
            return;

        float lerpFactor = Mathf.PingPong(
            Time.time * Mathf.Max(0f, speed),
            1f
        );

        SetColorOverride(
            UnityEngine.Color.Lerp(
                firstColor,
                secondColor,
                lerpFactor
            )
        );
    }

    public void SetOwner(string tag, Material playerMaterial)
    {
        if (hazardType != PadHazardType.None || ownerTag == tag)
            return;

        ownerTag = tag;
        SetMaterial(playerMaterial);
    }

    public void ApplyHazardVisual()
    {
        if (hazardType == PadHazardType.Bomb)
        {
            SetMaterial(manager.bombMaterial);
            SpawnFakeBomb();
        }
        else if (hazardType == PadHazardType.Freeze)
        {
            SetMaterial(manager.freezeMaterial);
            RemoveSpawnedBomb();
        }
    }

    private void SpawnFakeBomb()
    {
        if (manager == null ||
            manager.bombPrefab == null ||
            padObject == null ||
            spawnedBomb != null)
        {
            return;
        }

        Vector3 spawnPosition =
            padObject.transform.position +
            Vector3.up * manager.bombSpawnHeight;

        spawnedBomb = GameObject.Instantiate(
            manager.bombPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Collider[] bombColliders =
            spawnedBomb.GetComponentsInChildren<Collider>();

        foreach (Collider col in bombColliders)
        {
            if (col != null)
                col.enabled = false;
        }
    }

    public void RestoreVisualAfterHazard()
    {
        if (manager == null)
            return;

        if (ownerTag == "Player 1")
            SetMaterial(manager.player1Material);
        else if (ownerTag == "Player 2")
            SetMaterial(manager.player2Material);
        else
            SetMaterial(manager.defaultMaterial);
    }

    public void ResetColor()
    {
        ownerTag = "";
        hazardType = PadHazardType.None;

        RemoveSpawnedBomb();

        if (manager != null)
            SetMaterial(manager.defaultMaterial);
    }

    public void RemoveSpawnedBomb()
    {
        if (spawnedBomb == null)
            return;

        GameObject.Destroy(spawnedBomb);
        spawnedBomb = null;
    }

    private void SetMaterial(Material material)
    {
        if (renderer == null || material == null)
            return;

        renderer.sharedMaterial = material;
        ClearColorOverride();
    }

    private void SetColorOverride(Color color)
    {
        if (renderer == null)
            return;

        propertyBlock.Clear();
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);
        renderer.SetPropertyBlock(propertyBlock);
    }

    private void ClearColorOverride()
    {
        if (renderer == null)
            return;

        propertyBlock.Clear();
        renderer.SetPropertyBlock(propertyBlock);
    }
}