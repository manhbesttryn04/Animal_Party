using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    public PlayerManager manager;
    public bool isPlayer1 = true;

    [Header("Move Settings")]
    public float speed = 5f;
    public float turnSpeed = 10f;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Move State")]
    public bool isGround;
    public bool isJumpAndMove;
    public bool isMove;
    public bool isJump = true;
    public bool isWalk = false;

    public bool IsMoving { get; private set; }

    [Header("Controller Axis")]
    [Tooltip("Axis X chỉ dành cho Joystick 1.")]
    [SerializeField]
    private string horizontalJoystick1 =
        "HorizontalJoystick1";

    [Tooltip("Axis X chỉ dành cho Joystick 2.")]
    [SerializeField]
    private string horizontalJoystick2 =
        "HorizontalJoystick2";

    [Tooltip("Axis Y chỉ dành cho Joystick 1.")]
    [SerializeField]
    private string verticalJoystick1 =
        "VerticalJoystick1";

    [Tooltip("Axis Y chỉ dành cho Joystick 2.")]
    [SerializeField]
    private string verticalJoystick2 =
        "VerticalJoystick2";

    [Header("Lie Settings")]
    public bool hasLie = false;
    private bool canLie = true;

    public float lieHeight = 0.5f;
    public Vector3 lieCenter =
        new Vector3(0f, 0.25f, 0f);

    private float normalHeight;
    private Vector3 normalCenter;

    [Header("References")]
    public CharacterController controller;

    private Vector3 velocity;

    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        SetupPlayerMove();
    }

    private void SetupPlayerMove()
    {
        controller = GetComponent<CharacterController>();
        manager = GetComponent<PlayerManager>();

        normalHeight = controller.height;
        normalCenter = controller.center;
    }

    private void Update()
    {
        CheckLieInput();

        if (isJumpAndMove)
        {
            if (isMove) Move();
            else StopMoveAnimation();

            if (isJump) JumpInput();
        }
        else
        {
            StopMoveAnimation();
        }

        ApplyGravity();
    }
    // =========================================================
    // INPUT DEVICE
    // =========================================================

    private bool IsPlayer2()
    {
        return manager != null &&
               manager.playerType != null &&
               manager.playerType.isPlayer2;
    }

    private bool IsUsingController()
    {
        ControllerManager controllerManager =
            ControllerManager.Instance;

        if (controllerManager == null)
            return false;

        if (IsPlayer2())
        {
            return controllerManager
                .IsConsole2Connected();
        }

        return controllerManager
            .IsConsole1Connected();
    }

    private int GetConsoleNumber()
    {
        return IsPlayer2() ? 2 : 1;
    }

    // =========================================================
    // MOVE INPUT
    // =========================================================

    private Vector2 GetMoveInput()
    {
        /*
         * Có tay cầm:
         * chỉ đọc axis tay cầm.
         * Không đọc bàn phím.
         */
        if (IsUsingController())
        {
            ControllerManager controllerManager =
                ControllerManager.Instance;

            int consoleNumber =
                GetConsoleNumber();

            float horizontal =
                controllerManager
                    .GetConsoleHorizontalRaw(
                        consoleNumber,
                        horizontalJoystick1,
                        horizontalJoystick2
                    );

            float vertical =
                controllerManager
                    .GetConsoleVerticalRaw(
                        consoleNumber,
                        verticalJoystick1,
                        verticalJoystick2
                    );

            return new Vector2(
                horizontal,
                vertical
            );
        }

        /*
         * Không có tay cầm:
         * mới cho phép dùng KeyCode bàn phím.
         */
        float keyboardHorizontal = 0f;
        float keyboardVertical = 0f;

        if (!IsPlayer2())
        {
            // Player 1: WASD
            if (Input.GetKey(KeyCode.A))
                keyboardHorizontal = -1f;
            else if (Input.GetKey(KeyCode.D))
                keyboardHorizontal = 1f;

            if (Input.GetKey(KeyCode.S))
                keyboardVertical = -1f;
            else if (Input.GetKey(KeyCode.W))
                keyboardVertical = 1f;
        }
        else
        {
            // Player 2: Arrow Keys
            if (Input.GetKey(KeyCode.LeftArrow))
                keyboardHorizontal = -1f;
            else if (Input.GetKey(KeyCode.RightArrow))
                keyboardHorizontal = 1f;

            if (Input.GetKey(KeyCode.DownArrow))
                keyboardVertical = -1f;
            else if (Input.GetKey(KeyCode.UpArrow))
                keyboardVertical = 1f;
        }

        return new Vector2(
            keyboardHorizontal,
            keyboardVertical
        );
    }

    private void Move()
    {
        Vector2 input =
            GetMoveInput();

        Vector3 move =
            new Vector3(
                input.x,
                0f,
                input.y
            );

        // Tránh đi chéo nhanh hơn.
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        IsMoving =
            move.sqrMagnitude > 0.01f;

        if (IsMoving)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(move);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );

            controller.Move(
                move *
                speed *
                Time.deltaTime
            );
        }

        UpdateMoveAnimation(
            move.magnitude
        );
    }

    // =========================================================
    // JUMP INPUT
    // =========================================================

    private bool IsJumpPressed()
    {
        /*
         * Có tay cầm:
         * chỉ Button 0 của đúng Console.
         */
        if (IsUsingController())
        {
            return ControllerManager.Instance
                .GetConsoleButtonDown(
                    GetConsoleNumber(),
                    0
                );
        }

        /*
         * Không có tay cầm:
         * mới dùng phím bàn phím.
         */
        if (!IsPlayer2())
        {
            return Input.GetKeyDown(
                KeyCode.Space
            );
        }

        return Input.GetKeyDown(
            KeyCode.Keypad0
        );
    }

    private void JumpInput()
    {
        bool jumpPressed =
            IsJumpPressed();

        if (isGround &&
            jumpPressed)
        {
            if (manager != null &&
                manager.playerAnimator != null &&
                manager.playerAnimator
                    .playerAnimator != null)
            {
                manager.playerAnimator
                    .playerAnimator
                    .SetTrigger("Jump");
            }

            velocity.y =
                Mathf.Sqrt(
                    jumpHeight *
                    -2f *
                    gravity
                );

            isGround = false;
        }
    }

    // =========================================================
    // LIE INPUT
    // =========================================================

    private bool IsLiePressed()
    {
        /*
         * Có tay cầm:
         * chỉ Button 2 của đúng Console.
         */
        if (IsUsingController())
        {
            return ControllerManager.Instance
                .GetConsoleButtonDown(
                    GetConsoleNumber(),
                    2
                );
        }

        /*
         * Không có tay cầm:
         * mới dùng bàn phím.
         */
        if (!IsPlayer2())
        {
            return Input.GetKeyDown(
                KeyCode.L
            );
        }

        return Input.GetKeyDown(
            KeyCode.Keypad2
        );
    }

    private void CheckLieInput()
    {
        if (!hasLie)
            return;

        if (!canLie)
            return;

        if (!isGround ||
            velocity.y > 0.1f)
        {
            return;
        }

        if (IsLiePressed())
        {
            canLie = false;

            if (manager != null &&
                manager.playerAnimator != null &&
                manager.playerAnimator
                    .playerAnimator != null)
            {
                manager.playerAnimator
                    .playerAnimator
                    .SetTrigger("Lie");
            }
        }
    }

    // =========================================================
    // GRAVITY
    // =========================================================

    private void ApplyGravity()
    {
        if (isGround &&
            velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y +=
            gravity *
            Time.deltaTime;

        controller.Move(
            velocity *
            Time.deltaTime
        );
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void UpdateMoveAnimation(
        float moveAmount)
    {
        if (manager == null ||
            manager.playerAnimator == null ||
            manager.playerAnimator
                .playerAnimator == null)
        {
            return;
        }

        if (!isWalk)
        {
            manager.playerAnimator
                .playerAnimator
                .SetFloat(
                    "Run",
                    moveAmount
                );

            manager.playerAnimator
                .playerAnimator
                .SetFloat(
                    "Walk",
                    0f
                );
        }
        else
        {
            manager.playerAnimator
                .playerAnimator
                .SetFloat(
                    "Walk",
                    moveAmount
                );

            manager.playerAnimator
                .playerAnimator
                .SetFloat(
                    "Run",
                    0f
                );
        }
    }

    private void StopMoveAnimation()
    {
        IsMoving = false;
        UpdateMoveAnimation(0f);
    }

    // =========================================================
    // LIE ANIMATION EVENTS
    // =========================================================

    public void StartLie()
    {
        if (!isGround)
            return;

        isMove = false;
        isJump = false;

        if (manager != null &&
            manager.playerAttack != null)
        {
            manager.playerAttack.canAttack =
                false;
        }

        controller.height =
            lieHeight;

        controller.center =
            lieCenter;

        StopMoveAnimation();
    }

    public void StopLie()
    {
        isMove = true;
        isJump = true;

        if (manager != null &&
            manager.playerAttack != null)
        {
            manager.playerAttack.canAttack =
                true;
        }

        controller.height =
            normalHeight;

        controller.center =
            normalCenter;

        canLie = true;
    }

    // =========================================================
    // GROUND
    // =========================================================

    private void OnControllerColliderHit(
        ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Ground"))
        {
            isGround = true;
        }
    }
}