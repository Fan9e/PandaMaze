using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float walkThreshold = 0.0001f;
    [SerializeField] private float inputDeadzone = 0.05f;

    [Header("Turning")]
    [SerializeField, Tooltip("Degrees per second when using the turn-stick")]
    private float rotationSpeed = 120f;
    [SerializeField, Tooltip("Time (s) to smooth yaw changes")]
    private float rotationSmoothTime = 0.08f;

    // runtime
    private Rigidbody _rb;
    private Animator _anim;

    // raw inputs
    private Vector2 _rawMoveInput;
    private Vector2 _rawTurnInput;

    // rotation smoothing state
    private float _pendingYawRate;
    private float _yawSmoothVelocity;

    // public helpers
    public Rigidbody RigidbodyComponent
    {
        get => _rb;
        set => _rb = value;
    }

    public float MovementSpeed
    {
        get => movementSpeed;
        set => movementSpeed = value;
    }

    // Backwards-compatible API used by older callers/tests
    // Accepts Vector3 (x = strafe, z = forward) like previous versions.
    public void SetMovementInput(Vector3 input)
    {
        _rawMoveInput = new Vector2(input.x, input.z);
    }

    private void Awake()
    {
        _rb = _rb ?? GetComponent<Rigidbody>();
        _anim = _anim ?? GetComponent<Animator>();

        // reduce jitter between FixedUpdate physics and Update rendering
        if (_rb != null)
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        ReadInputsFallback();
        UpdateAnimationState();
        GatherTurnInput();
    }

    private void FixedUpdate()
    {
        ApplyTurn();
        ApplyMovement();
    }

    // -------------------------
    // Input handlers (public) — PlayerInput Invoke Unity Events or UI can call these
    // -------------------------
    public void OnMove(Vector2 value) => SetMoveInput(value);
    public void OnLook(Vector2 value) => SetTurnInput(value);

    // Send Messages (PlayerInput behavior = Send Messages)
    public void OnMove(InputValue value) => SetMoveInput(value.Get<Vector2>());
    public void OnLook(InputValue value) => SetTurnInput(value.Get<Vector2>());

    // CallbackContext (if wiring actions to be sent)
    public void OnMove(InputAction.CallbackContext ctx) => SetMoveInput(ReadVector2FromContext(ctx));
    public void OnLook(InputAction.CallbackContext ctx) => SetTurnInput(ReadVector2FromContext(ctx));

    private static Vector2 ReadVector2FromContext(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed || ctx.phase == InputActionPhase.Started)
            return ctx.ReadValue<Vector2>();
        return Vector2.zero;
    }

    private void SetMoveInput(Vector2 v) => _rawMoveInput = v;
    private void SetTurnInput(Vector2 v) => _rawTurnInput = v;

    // -------------------------
    // Movement / turning logic
    // -------------------------
    private void ReadInputsFallback()
    {
        // If joystick provided values, use them (with deadzone). Otherwise fallback to old Input axes for editor testing.
        if (_rawMoveInput.sqrMagnitude <= inputDeadzone * inputDeadzone)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            _rawMoveInput = new Vector2(h, v);
            if (Mathf.Abs(_rawMoveInput.y) < inputDeadzone) _rawMoveInput.y = 0f;
        }
        else
        {
            // apply deadzone to each axis
            if (Mathf.Abs(_rawMoveInput.x) < inputDeadzone) _rawMoveInput.x = 0f;
            if (Mathf.Abs(_rawMoveInput.y) < inputDeadzone) _rawMoveInput.y = 0f;
        }

        if (Mathf.Abs(_rawTurnInput.x) < inputDeadzone) _rawTurnInput.x = 0f;
        if (Mathf.Abs(_rawTurnInput.y) < inputDeadzone) _rawTurnInput.y = 0f;
    }

    private void GatherTurnInput()
    {
        // Horizontal of the turn-stick controls yaw rate; vertical ignored (no camera pitch here)
        float lookX = _rawTurnInput.x;
        _pendingYawRate = Mathf.Abs(lookX) > inputDeadzone ? lookX * rotationSpeed : 0f;
    }

    private void ApplyTurn()
    {
        if (_rb == null) return;

        float currentYaw = _rb.rotation.eulerAngles.y;
        float targetYaw = currentYaw + _pendingYawRate * Time.fixedDeltaTime;

        // smooth transition to target yaw
        float smoothYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref _yawSmoothVelocity, rotationSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        _rb.MoveRotation(Quaternion.Euler(0f, smoothYaw, 0f));
    }

    // Kept public to match test expectations
    public void ApplyMovement()
    {
        if (_rb == null) return;

        // movement relative to player forward/right
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 planar = (right * _rawMoveInput.x) + (forward * _rawMoveInput.y);

        Vector3 velocity = planar.normalized * (planar.magnitude * movementSpeed);
        // preserve existing vertical velocity (gravity/jump)
        velocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = velocity;
    }

    private void UpdateAnimationState()
    {
        if (_anim == null || _rb == null) return;

        // Use the player's local horizontal velocity to decide walking state.
        // This ensures animations reflect actual movement (works with keyboard, joystick, or physics).
        Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
        Vector2 planarVel = new Vector2(localVelocity.x, localVelocity.z);

        // Compare squared magnitude (keeps behavior similar to previous implementation).
        bool isWalking = planarVel.sqrMagnitude > walkThreshold;

        _anim.SetBool("IsWalking", isWalking);
    }
}