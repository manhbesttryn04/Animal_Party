using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebuffManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    [Header("Singleton")]
    public static DebuffManager _instance;
    public static DebuffManager Instance => _instance;

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Manager References")]
    public UIManager ui;
    [Header("Prefabs")]
    public GameObject cannonPrefab;

    [Header("Cannon Camera")]
    [SerializeField]
    private Vector3 bombCameraOffset =
        new Vector3(0f, 2f, -4f);

    [SerializeField]
    private Vector3 cannonTargetCameraOffset =
        new Vector3(0f, 5f, -8f);

    [SerializeField]
    private float cannonCameraFollowSpeed = 8f;

    [SerializeField]
    private float cannonTargetSettleTime = 0.5f;

    private bool cannonTeleportTookCamera;

    // =========================================================
    // CARD SELECTION
    // =========================================================

    [Header("Card Selection")]
    [SerializeField] private int leftIndex;
    [SerializeField] private int rightIndex;

    public bool leftActive;
    public bool rightActive;

    [Header("Debuff Random Colors")]
    [SerializeField] private Color magicDebuffColor = Color.blue;
    [SerializeField] private Color cannonDebuffColor = Color.red;

    private GameObject[] currentRandomDebuffCards;
    private bool lastShowDebuffRandomColorLists;

    // =========================================================
    // NAVIGATION SETTINGS
    // =========================================================

    [Header("Navigation Settings")]
    [SerializeField] private float navigationThreshold = 0.5f;
    [SerializeField] private float resetThreshold = 0.2f;

    [Header("Controller Axis")]
    [Tooltip("Axis X chỉ dành cho Joystick 1.")]
    [SerializeField]
    private string horizontalJoystick1 =
        "HorizontalJoystick1";

    [Tooltip("Axis X chỉ dành cho Joystick 2.")]
    [SerializeField]
    private string horizontalJoystick2 =
        "HorizontalJoystick2";

    private bool leftHorizontalReady = true;
    private bool rightHorizontalReady = true;

    private bool isSelectingDebuff;
    public bool isOpen = false;

    // Khi Setting đóng, phải thả cần/phím và nút xác nhận
    // trước khi DebuffManager nhận input trở lại.
    private bool waitReleaseAfterSetting;

    // =========================================================
    // UNITY FUNCTIONS
    // =========================================================

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SetupUI();
        SetupDebuffState();
    }

    private void SetupUI()
    {
        ui = UIManager.Instance;
        HideDebuffRandomColors();
    }

    private void SetupDebuffState()
    {
        if (isOpen)
        {
            Open(0);
        }
    }

    private void Update()
    {
        // Cho phép bật/tắt công tắc UIManager ngay khi đang Play.
        if (ui != null &&
            ui.showDebuffRandomColorLists !=
            lastShowDebuffRandomColorLists)
        {
            RefreshDebuffRandomColors();
        }

        /*
         * Setting đang mở:
         * khóa hoàn toàn input chọn Debuff.
         */
        if (IsSettingOpen())
        {
            waitReleaseAfterSetting = true;

            leftHorizontalReady = false;
            rightHorizontalReady = false;

            return;
        }

        /*
         * Sau khi đóng Setting:
         * phải thả cần/phím di chuyển và nút xác nhận.
         * Tránh nút dùng để đóng Setting chọn luôn lá bài.
         */
        if (waitReleaseAfterSetting)
        {
            if (IsDebuffInputReleased())
            {
                waitReleaseAfterSetting = false;

                leftHorizontalReady = true;
                rightHorizontalReady = true;
            }

            return;
        }

        if (isSelectingDebuff)
            return;

        if (leftActive)
        {
            HandleLeft();
        }

        if (rightActive)
        {
            HandleRight();
        }
    }

    // =========================================================
    // OPEN DEBUFF UI
    // =========================================================

    public static void Open(int winner)
    {
        if (Instance == null)
            return;

        Instance.OpenDebuffInternal(winner);
    }

    public void OpenDebuffInternal(int playerIndex)
    {
        var gameManager = GameManager.Instance;
        if(gameManager != null)
        {
            PlayerLookPlayer playerLook =
                playerIndex == 0 ? gameManager.player1Main.GetComponent<PlayerLookPlayer>()
                : gameManager.player2Main.GetComponent<PlayerLookPlayer>();

            if(playerLook != null)
            {
                playerLook.LookAtOpponentOnXAxis();
            }
        }

        ui = UIManager.Instance;

        if (ui == null)
        {

            return;
        }

        ui.leftCardCanvas.SetActive(false);
        ui.rightCardCanvas.SetActive(false);
        HideDebuffRandomColors();

        HideAllCards();

        StartCoroutine(ShowPanelChoose());

        // Chưa cho người chơi chọn.
        leftActive = false;
        rightActive = false;

        // Cho phép chọn lại khi mở giao diện mới.
        isSelectingDebuff = false;

        // Khóa trục cho tới khi animation mở xong
        // và người chơi thả cần analog.
        leftHorizontalReady = false;
        rightHorizontalReady = false;

        if (playerIndex == 0)
        {
            ui.leftCardCanvas.SetActive(true);

            ResetCards(
                ui.leftCardsList,
                ref leftIndex
            );

            StartCoroutine(
                WaitOpenDebuffAnimation(0)
            );
        }
        else
        {
            ui.rightCardCanvas.SetActive(true);

            ResetCards(
                ui.rightCardsList,
                ref rightIndex
            );

            StartCoroutine(
                WaitOpenDebuffAnimation(1)
            );
        }
    }

    private IEnumerator WaitOpenDebuffAnimation(int playerIndex)
    {
        Animator animator = playerIndex == 0
            ? ui.leftCardCanvas.GetComponent<Animator>()
            : ui.rightCardCanvas.GetComponent<Animator>();

        // Đợi Animator cập nhật state.
        yield return null;

        if (animator != null)
        {
            AnimatorStateInfo stateInfo =
                animator.GetCurrentAnimatorStateInfo(0);

            yield return new WaitForSeconds(
                stateInfo.length + 0.5f
            );
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        // Tránh vừa mở giao diện đã tự chuyển card
        // do người chơi vẫn đang giữ phím/cần analog.
        yield return StartCoroutine(
            WaitForDebuffAxisRelease(playerIndex)
        );

        if (playerIndex == 0)
        {
            leftHorizontalReady = true;
            leftActive = true;

            SetHighlight(
                ui.leftCardsList,
                leftIndex,
                true
            );
        }
        else
        {
            rightHorizontalReady = true;
            rightActive = true;

            SetHighlight(
                ui.rightCardsList,
                rightIndex,
                true
            );
        }
    }

    private IEnumerator WaitForDebuffAxisRelease(int playerIndex)
    {
        while (true)
        {
            float horizontal =
                GetHorizontalInput(playerIndex);

            if (Mathf.Abs(horizontal) < resetThreshold)
                break;

            yield return null;
        }
    }

    private IEnumerator ShowPanelChoose()
    {
        if (ui.panelNotiifiChooseDebuff == null)
            yield break;

        ui.panelNotiifiChooseDebuff.SetActive(true);

        yield return new WaitForSeconds(2f);

        ui.panelNotiifiChooseDebuff.SetActive(false);
    }

    // =========================================================
    // INPUT DEVICE
    // =========================================================

    private bool IsUsingController(int playerIndex)
    {
        ControllerManager controllerManager =
            ControllerManager.Instance;

        if (controllerManager == null)
            return false;

        return playerIndex == 0
            ? controllerManager.IsConsole1Connected()
            : controllerManager.IsConsole2Connected();
    }

    private int GetConsoleNumber(int playerIndex)
    {
        return playerIndex == 0 ? 1 : 2;
    }


    private bool IsSettingOpen()
    {
        return SettingManager.Instance != null &&
               SettingManager.Instance.IsSettingBlockingInput;
    }

    // =========================================================
    // HORIZONTAL INPUT
    // =========================================================

    private float GetHorizontalInput(int playerIndex)
    {
        /*
         * Có tay cầm:
         * chỉ đọc axis tay cầm giống PlayerMove.
         * Không đọc bàn phím.
         */
        if (IsUsingController(playerIndex))
        {
            ControllerManager controllerManager =
                ControllerManager.Instance;

            return controllerManager
                .GetConsoleHorizontalRaw(
                    GetConsoleNumber(playerIndex),
                    horizontalJoystick1,
                    horizontalJoystick2
                );
        }

        /*
         * Không có tay cầm:
         * mới cho phép dùng bàn phím.
         */
        if (playerIndex == 0)
        {
            // Player 1: A / D
            if (Input.GetKey(KeyCode.A))
                return -1f;

            if (Input.GetKey(KeyCode.D))
                return 1f;
        }
        else
        {
            // Player 2: Left / Right Arrow
            if (Input.GetKey(KeyCode.LeftArrow))
                return -1f;

            if (Input.GetKey(KeyCode.RightArrow))
                return 1f;
        }

        return 0f;
    }

    private bool GetConfirmDown(int playerIndex)
    {
        /*
         * Có tay cầm:
         * chỉ nhận Button 0 của đúng Console.
         */
        if (IsUsingController(playerIndex))
        {
            return ControllerManager.Instance
                .GetConsoleButtonDown(
                    GetConsoleNumber(playerIndex),
                    0
                );
        }

        /*
         * Không có tay cầm:
         * mới dùng bàn phím.
         */
        return playerIndex == 0
            ? Input.GetKeyDown(KeyCode.J)
            : Input.GetKeyDown(KeyCode.Keypad1);
    }


    private bool GetConfirmHeld(int playerIndex)
    {
        /*
         * Có tay cầm:
         * chỉ kiểm tra Button 0 của đúng Console.
         */
        if (IsUsingController(playerIndex))
        {
            return ControllerManager.Instance
                .GetConsoleButton(
                    GetConsoleNumber(playerIndex),
                    0
                );
        }

        /*
         * Không có tay cầm:
         * mới kiểm tra bàn phím.
         */
        return playerIndex == 0
            ? Input.GetKey(KeyCode.J)
            : Input.GetKey(KeyCode.Keypad1);
    }

    private bool IsDebuffInputReleased()
    {
        float player1Horizontal =
            GetHorizontalInput(0);

        float player2Horizontal =
            GetHorizontalInput(1);

        bool movementReleased =
            Mathf.Abs(player1Horizontal) < resetThreshold &&
            Mathf.Abs(player2Horizontal) < resetThreshold;

        bool confirmReleased =
            !GetConfirmHeld(0) &&
            !GetConfirmHeld(1);

        return movementReleased &&
               confirmReleased;
    }

    // =========================================================
    // LEFT INPUT - PLAYER 1
    // =========================================================

    private void HandleLeft()
    {
        if (isSelectingDebuff)
            return;

        if (ui.leftCardsList == null ||
            ui.leftCardsList.Length == 0)
        {
            return;
        }

        if (leftIndex < 0 ||
            leftIndex >= ui.leftCardsList.Length)
        {
            return;
        }

        GameObject currentObject =
            ui.leftCardsList[leftIndex];

        if (currentObject == null)
            return;

        IndexItem current =
            currentObject.GetComponent<IndexItem>();

        if (current == null)
            return;

        float horizontal =
            GetHorizontalInput(0);

        // Khi trục trở về giữa thì mới cho phép
        // di chuyển thêm một lần nữa.
        if (Mathf.Abs(horizontal) < resetThreshold)
        {
            leftHorizontalReady = true;
        }

        if (leftHorizontalReady)
        {
            if (horizontal < -navigationThreshold)
            {
                leftHorizontalReady = false;
                MoveLeft(current.left);
            }
            else if (horizontal > navigationThreshold)
            {
                leftHorizontalReady = false;
                MoveLeft(current.right);
            }
        }

        // Player 1 xác nhận.
        if (GetConfirmDown(0))
        {
            Select(
                ui.leftCardsList[leftIndex],
                0
            );
        }
    }

    private void MoveLeft(int newIndex)
    {
        if (newIndex < 0 ||
            newIndex >= ui.leftCardsList.Length ||
            newIndex == leftIndex)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance.movechooseItemClip
            );
        }

        SetHighlight(
            ui.leftCardsList,
            leftIndex,
            false
        );

        leftIndex = newIndex;

        SetHighlight(
            ui.leftCardsList,
            leftIndex,
            true
        );
    }

    // =========================================================
    // RIGHT INPUT - PLAYER 2
    // =========================================================

    private void HandleRight()
    {
        if (isSelectingDebuff)
            return;

        if (ui.rightCardsList == null ||
            ui.rightCardsList.Length == 0)
        {
            return;
        }

        if (rightIndex < 0 ||
            rightIndex >= ui.rightCardsList.Length)
        {
            return;
        }

        GameObject currentObject =
            ui.rightCardsList[rightIndex];

        if (currentObject == null)
            return;

        // Đã sửa: phải dùng rightCardsList,
        // không phải leftCardsList.
        IndexItem current =
            currentObject.GetComponent<IndexItem>();

        if (current == null)
            return;

        float horizontal =
            GetHorizontalInput(1);

        if (Mathf.Abs(horizontal) < resetThreshold)
        {
            rightHorizontalReady = true;
        }

        if (rightHorizontalReady)
        {
            if (horizontal < -navigationThreshold)
            {
                rightHorizontalReady = false;
                MoveRight(current.left);
            }
            else if (horizontal > navigationThreshold)
            {
                rightHorizontalReady = false;
                MoveRight(current.right);
            }
        }

        // Player 2 xác nhận.
        if (GetConfirmDown(1))
        {
            Select(
                ui.rightCardsList[rightIndex],
                1
            );
        }
    }

    private void MoveRight(int newIndex)
    {
        if (newIndex < 0 ||
            newIndex >= ui.rightCardsList.Length ||
            newIndex == rightIndex)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(
                AudioManager.Instance.movechooseItemClip
            );
        }

        SetHighlight(
            ui.rightCardsList,
            rightIndex,
            false
        );

        rightIndex = newIndex;

        SetHighlight(
            ui.rightCardsList,
            rightIndex,
            true
        );
    }

    // =========================================================
    // SELECT CARD
    // =========================================================

    private void Select(GameObject cardObj, int playerIndex)
    {
        if (isSelectingDebuff)
            return;

        if (cardObj == null)
            return;

        RandomCard card =
            cardObj.GetComponent<RandomCard>();

        if (card == null)
            return;

        Image image = card.image;

        if (image == null)
            return;

        if (card.itemIndex < 0 ||
            card.itemIndex >= UIManager.Instance.debuffSpriteList.Length)
        {


            return;
        }

        StartCoroutine(
            SelectCardRoutine(
                image,
                card.itemIndex,
                playerIndex
            )
        );
    }

    private IEnumerator SelectCardRoutine(
        Image image,
        int itemIndex,
        int playerIndex)
    {
        // Khóa input ngay lập tức để tránh chọn hai lần.
        isSelectingDebuff = true;

        leftActive = false;
        rightActive = false;

        // Đổi từ hình dấu sao sang hình vật phẩm đã chọn.
        if (UIManager.Instance != null)
        {
            image.sprite = UIManager.Instance.debuffSpriteList[itemIndex];
        }


        if (AudioManager.Instance != null)
        {
            /* AudioManager.Instance.PlaySFX(
                   AudioManager.Instance.buyItemClip
               ); */
        }

        // Chờ một frame để Unity kịp cập nhật Sprite lên màn hình.
        yield return null;

        // Giữ hình vật phẩm trên màn hình trước khi đóng thẻ.
        // Dùng realtime để vẫn hoạt động khi Time.timeScale = 0.
        yield return new WaitForSeconds(1f);

        HideAll();

        // itemIndex 0 = Magic Debuff.
        if (itemIndex == 0)
        {
            yield return StartCoroutine(
                ApplyMagicDebuff(playerIndex)
            );
        }
        // itemIndex 1 = Cannon Debuff.
        else if (itemIndex == 1)
        {
            yield return StartCoroutine(
                ApplyCannonDebuff(playerIndex)
            );
        }
    }

    // =========================================================
    // MAGIC DEBUFF
    // =========================================================

    private IEnumerator ApplyMagicDebuff(int playerIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySpecial(AudioManager.Instance.petrificatioDebuffVoiceClip);
        }
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowDebuffAndBuffPanel(UIManager.Instance.magicDebuffPanel));
        }
        // Nếu Player 1 chọn thì target là Player 2,
        // ngược lại.
        var gameManager = GameManager.instance;
        if(gameManager == null)
        {
            yield break;
        }

        GameObject targetPlayer = playerIndex == 0 ? gameManager.player2Main : gameManager.player1Main;
        GameObject targetPlayerWin = playerIndex == 0 ? gameManager.player1Main : gameManager.player2Main;

        if (targetPlayer == null || targetPlayerWin == null)
        {
            ReturnToShop();
            yield break;
        }

        PlayerManager player =
            targetPlayer.GetComponent<PlayerManager>();

        PlayerManager playerWin =
            targetPlayerWin.GetComponent<PlayerManager>();

        if (player == null || playerWin == null)
        {
            ReturnToShop();
            yield break;
        }

        PlayerAnimator aniPlayerWin = playerWin.playerAnimator;

        if(aniPlayerWin == null)
        {
            ReturnToShop();
            yield break;
        }

        var random = Random.Range(0, 2);
        string aniRandomString;

        if (random == 0)
        {
            aniRandomString = "Magic";
        } else aniRandomString = "Magic 0";

        aniPlayerWin.playerAnimator.SetTrigger(aniRandomString);

        yield return new WaitForSeconds(1.5f);

        if (CameraManager.Instance != null)
        {
            yield return StartCoroutine(
                CameraManager.Instance.MoveToTarget(
                    targetPlayer.transform,
                    1f
                )
            );
        }

        // Nếu người chơi có buff khiên phép
        // thì chặn Magic Debuff.
        if (player.playerBuff != null &&
            player.playerBuff.isBuffMagic)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySpecial(AudioManager.Instance.petrificationImmunityVoiceClip);
            }
            if (UIManager.Instance != null)
            {
                StartCoroutine(UIManager.Instance.ShowDebuffAndBuffPanel(UIManager.Instance.petrificationImmunityPanel));
            }
            yield return StartCoroutine(
                player.playerBuff.ShowMagicShield()
            );

            yield return new WaitForSeconds(1.5f);

            if (CameraManager.Instance != null)
            {
                yield return StartCoroutine(
                    CameraManager.Instance.FlyUp(
                        15f,
                        1.2f
                    )
                );
            }

            ReturnToShop();
            yield break;
        }

        PlayerDebuff debuff =
            targetPlayer.GetComponent<PlayerDebuff>();

        if (debuff != null)
        {
            debuff.ApplyMagicRock();

            yield return new WaitForSeconds(1.5f);
        }

        if (CameraManager.Instance != null)
        {
            yield return StartCoroutine(
                CameraManager.Instance.FlyUp(
                    15f,
                    1.2f
                )
            );
        }

        ReturnToShop();
    }

    // =========================================================
    // CANNON DEBUFF
    // =========================================================

    private IEnumerator ApplyCannonDebuff(int playerIndex)
    {
        cannonTeleportTookCamera = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySpecial(AudioManager.Instance.cannonDebuffVoiceClip);
        }
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowDebuffAndBuffPanel(UIManager.Instance.cannonDebuffPanel));
        }
        string ownerTag =
            playerIndex == 0
                ? "Player 1"
                : "Player 2";

        string targetTag =
            playerIndex == 0
                ? "Player 2"
                : "Player 1";

        GameObject owner =
            GameObject.FindGameObjectWithTag(ownerTag);

        GameObject target =
            GameObject.FindGameObjectWithTag(targetTag);

        if (owner == null ||
            target == null ||
            cannonPrefab == null)
        {
            ReturnToShop();
            yield break;
        }

        Vector3 spawnPosition =
            owner.transform.position +
            owner.transform.forward * 1.5f +
            Vector3.down * 0.3f;

        GameObject cannon = Instantiate(
            cannonPrefab,
            spawnPosition,
            cannonPrefab.transform.rotation
        );

        cannon.transform.LookAt(target.transform);

        if (CameraManager.Instance != null)
        {
            yield return StartCoroutine(
                CameraManager.Instance.MoveToTarget(
                    cannon.transform,
                    1f
                )
            );
        }

        yield return new WaitForSeconds(1.5f);
        owner.GetComponent<PlayerAnimator>().playerAnimator.SetTrigger("Salute");

        CannonDebuff cannonScript =
            cannon.GetComponent<CannonDebuff>();

        if (cannonScript == null)
        {
            Destroy(cannon);
            ReturnToShop();
            yield break;
        }

        PlayerBuff ownerBuff =
            owner.GetComponent<PlayerBuff>();

        if (ownerBuff != null &&
            ownerBuff.isBuffCanon)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySpecial(AudioManager.Instance.powerCannonVoiceClip);
            }
            if (UIManager.Instance != null)
            {
                yield return StartCoroutine(UIManager.Instance.ShowDebuffAndBuffPanel(
                      UIManager.Instance.cannonPowerPanel)
                  );
            }
        }

        yield return new WaitForSeconds(1f);

        BombDebuff bomb =
            cannonScript.Fire(target.transform);

        // Phải kiểm tra bomb trước khi dùng bomb.power.
        if (bomb != null &&
            ownerBuff != null &&
            ownerBuff.isBuffCanon)
        {
            bomb.power += 3;
        }

        Destroy(cannon, 0.5f);

        if (bomb == null)
        {
            ReturnToShop();
            yield break;
        }

        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance
                .SetMotionBlurIntensity(0.1f);
        }

        yield return StartCoroutine(
            FollowBomb(bomb.transform)
        );

        PlayerTrapState trapState =
            target.GetComponent<PlayerTrapState>();

        PlayerMoveAI moveAI =
            target.GetComponent<PlayerMoveAI>();

        // Sau khi bom chạm mục tiêu, DebuffManager tiếp tục điều khiển
        // camera và bám theo player cho tới khi đẩy lùi hoàn tất.
        yield return StartCoroutine(
            FollowCannonTarget(
                target.transform,
                moveAI
            )
        );

        // Nếu chuỗi đẩy lùi kích hoạt Teleport thì camera của
        // TeleportAllPlayer tiếp quản. DebuffManager chỉ chờ toàn bộ
        // Teleport và trap hoàn thành, bay camera lên rồi mở Shop.
        if (cannonTeleportTookCamera)
        {
            yield return new WaitUntil(
                () =>
                    !TeleportAllPlayer.IsTeleporting &&
                    (trapState == null ||
                     !trapState.isTrapActive)
            );

            if (VolumeManager.Instance != null)
            {
                VolumeManager.Instance.ResetMotionBlur();
            }

            // Teleport đã hoàn tất và đã trả quyền camera.
            // Bay lên như luồng Debuff bình thường trước khi mở Shop.
            if (CameraManager.Instance != null)
            {
                yield return StartCoroutine(
                    CameraManager.Instance.FlyUp(
                        15f,
                        1.2f
                    )
                );
            }

            ReturnToShop();
            yield break;
        }

        if (VolumeManager.Instance != null)
        {
            VolumeManager.Instance.ResetMotionBlur();
        }

        if (CameraManager.Instance != null)
        {
            yield return StartCoroutine(
                CameraManager.Instance.FlyUp(
                    15f,
                    1.2f
                )
            );
        }

        ReturnToShop();
    }

    // =========================================================
    // BOMB FOLLOW CAMERA
    // =========================================================

    private IEnumerator FollowBomb(Transform bomb)
    {
        while (bomb != null)
        {
            Camera cam = Camera.main;

            if (cam == null)
                yield break;

            Vector3 desiredPosition =
                bomb.position +
                bombCameraOffset;

            cam.transform.position =
                Vector3.Lerp(
                    cam.transform.position,
                    desiredPosition,
                    cannonCameraFollowSpeed *
                    Time.deltaTime
                );

            cam.transform.LookAt(bomb.position);

            yield return null;
        }
    }

    // =========================================================
    // PLAYER FOLLOW AFTER CANNON HIT
    // =========================================================

    private IEnumerator FollowCannonTarget(
        Transform target,
        PlayerMoveAI moveAI
    )
    {
        const float waitForKnockbackStart = 0.75f;
        const float safetyTimeout = 10f;

        float elapsed = 0f;
        float settleTimer = 0f;
        bool knockbackStarted = false;

        while (target != null &&
               elapsed < safetyTimeout)
        {
            // Teleport có camera riêng. Ngừng Debuff follow ngay để
            // hai hệ thống không cùng ghi vị trí Camera.main.
            if (TeleportAllPlayer.IsTeleporting)
            {
                cannonTeleportTookCamera = true;
                yield break;
            }

            Camera cam = Camera.main;

            if (cam == null)
                yield break;

            Vector3 desiredPosition =
                target.position +
                cannonTargetCameraOffset;

            float followAmount =
                cannonCameraFollowSpeed *
                Time.deltaTime;

            cam.transform.position =
                Vector3.Lerp(
                    cam.transform.position,
                    desiredPosition,
                    followAmount
                );

            Vector3 lookDirection =
                target.position -
                cam.transform.position;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        lookDirection
                    );

                cam.transform.rotation =
                    Quaternion.Slerp(
                        cam.transform.rotation,
                        targetRotation,
                        followAmount
                    );
            }

            // DebuffManager không kiểm tra NavMesh, ô bom, coin
            // hoặc Teleport. PlayerMoveAI tự xử lý toàn bộ chuỗi đó.
            // Camera chỉ đọc tín hiệu vòng đời của BoomHitEffect.
            bool knockbackActive =
                moveAI != null &&
                moveAI.IsBoomHitActive;

            if (knockbackActive)
            {
                knockbackStarted = true;
                settleTimer = 0f;
            }
            else if (knockbackStarted)
            {
                settleTimer += Time.deltaTime;

                if (settleTimer >=
                    cannonTargetSettleTime)
                {
                    yield break;
                }
            }
            else if (elapsed >= waitForKnockbackStart)
            {
                // Bomb không kích hoạt BoomHitEffect: không chờ
                // timeout dài, trả camera về luồng Debuff ngay.
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // =========================================================
    // END SELECT
    // =========================================================

    private void ReturnToShop()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.Open();
        }
    }

    // =========================================================
    // RESET UI / CARD
    // =========================================================

    private void ResetCards(
        GameObject[] cards,
        ref int index)
    {
        if (cards == null || cards.Length == 0)
            return;

        index = 0;

        // Hai lá đầu chứa hai debuff khác nhau.
        if (cards.Length >= 2)
        {
            bool swap =
                Random.Range(0, 2) == 0;

            RandomCard firstCard =
                cards[0].GetComponent<RandomCard>();

            RandomCard secondCard =
                cards[1].GetComponent<RandomCard>();

            if (firstCard != null)
            {
                firstCard.itemIndex =
                    swap ? 0 : 1;
            }

            if (secondCard != null)
            {
                secondCard.itemIndex =
                    swap ? 1 : 0;
            }
        }

        // Reset toàn bộ card về sprite dấu sao.
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            RandomCard randomCard =
                cards[i].GetComponent<RandomCard>();

            if (randomCard == null ||
                randomCard.image == null)
            {
                continue;
            }

            randomCard.image.sprite =
                randomCard.spriteStar;
        }

        // Random hoàn tất: xử lý List A/B tại DebuffManager.
        ShowDebuffRandomColors(cards);
    }

    // =========================================================
    // DEBUFF RANDOM COLOR DEBUG
    // =========================================================

    private void ShowDebuffRandomColors(GameObject[] cards)
    {
        currentRandomDebuffCards = cards;
        RefreshDebuffRandomColors();
    }

    private void HideDebuffRandomColors()
    {
        currentRandomDebuffCards = null;

        if (ui == null)
            return;

        SetDebuffColorListVisible(ui.listA, false);
        SetDebuffColorListVisible(ui.listB, false);

        lastShowDebuffRandomColorLists =
            ui.showDebuffRandomColorLists;
    }

    private void RefreshDebuffRandomColors()
    {
        if (ui == null)
            return;

        lastShowDebuffRandomColorLists =
            ui.showDebuffRandomColorLists;

        if (!ui.showDebuffRandomColorLists ||
            currentRandomDebuffCards == null ||
            currentRandomDebuffCards.Length < 2)
        {
            SetDebuffColorListVisible(ui.listA, false);
            SetDebuffColorListVisible(ui.listB, false);
            return;
        }

        ApplyDebuffCardColor(
            currentRandomDebuffCards[0],
            ui.listA
        );

        ApplyDebuffCardColor(
            currentRandomDebuffCards[1],
            ui.listB
        );
    }

    private void ApplyDebuffCardColor(
        GameObject cardObject,
        List<Image> images)
    {
        if (cardObject == null)
        {
            SetDebuffColorListVisible(images, false);
            return;
        }

        RandomCard card =
            cardObject.GetComponent<RandomCard>();

        if (card == null ||
            (card.itemIndex != 0 && card.itemIndex != 1))
        {
            SetDebuffColorListVisible(images, false);
            return;
        }

        // itemIndex 0 = Magic, itemIndex 1 = Cannon.
        Color targetColor = card.itemIndex == 1
            ? cannonDebuffColor
            : magicDebuffColor;

        SetDebuffColorListVisible(images, true);

        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] != null)
            {
                images[i].color = targetColor;
            }
        }
    }

    private void SetDebuffColorListVisible(
        List<Image> images,
        bool state)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Count; i++)
        {
            if (images[i] != null)
            {
                images[i].gameObject.SetActive(state);
            }
        }
    }

    private void SetHighlight(
        GameObject[] cards,
        int index,
        bool state)
    {
        if (cards == null ||
            index < 0 ||
            index >= cards.Length ||
            cards[index] == null)
        {
            return;
        }

        if (cards[index].transform.childCount <= 1)
            return;

        cards[index]
            .transform
            .GetChild(1)
            .gameObject
            .SetActive(state);
    }

    private void HideAllCards()
    {
        if (ui == null)
            return;

        if (ui.leftCardsList != null)
        {
            for (int i = 0;
                 i < ui.leftCardsList.Length;
                 i++)
            {
                SetHighlight(
                    ui.leftCardsList,
                    i,
                    false
                );
            }
        }

        if (ui.rightCardsList != null)
        {
            for (int i = 0;
                 i < ui.rightCardsList.Length;
                 i++)
            {
                SetHighlight(
                    ui.rightCardsList,
                    i,
                    false
                );
            }
        }
    }

    private void HideAll()
    {
        leftActive = false;
        rightActive = false;

        leftHorizontalReady = false;
        rightHorizontalReady = false;

        if (ui == null)
            return;

        HideAllCards();
        HideDebuffRandomColors();

        if (ui.leftCardCanvas != null)
        {
            ui.leftCardCanvas.SetActive(false);
        }

        if (ui.rightCardCanvas != null)
        {
            ui.rightCardCanvas.SetActive(false);
        }
    }
}