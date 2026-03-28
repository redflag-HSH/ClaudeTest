using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    public float speed = 5f;
    public float jumpForce = 10f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool jumpQueued;
    private _2DActions actions;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        actions = new _2DActions();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        actions.Player2D.Move.performed += OnMove;
        actions.Player2D.Move.canceled  += OnMove;
        actions.Player2D.Jump.performed += OnJump;
        actions.Player2D.Enable();
    }

    void OnDisable()
    {
        actions.Player2D.Move.performed -= OnMove;
        actions.Player2D.Move.canceled  -= OnMove;
        actions.Player2D.Jump.performed -= OnJump;
        actions.Player2D.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>().x;
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (IsGrounded())
            jumpQueued = true;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        if (jumpQueued)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpQueued = false;
        }
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
    }
}
