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

    // husk om vi har fået move-input fra Input System denne frame
    private bool _hasMoveActionInput;

    // rotation smoothing state
    private float _pendingYawRate;
    private float _yawSmoothVelocity;

    // precomputed for performance + læsbarhed
    private float _inputDeadzoneSqr;

    // public helpers (bruges kun hvis du får brug for det udefra)
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

    /// <summary>
    /// Gammel API: tager Vector3 (x = strafe, z = frem) og laver det om til 2D-input.
    /// </summary>
    public void SetMovementInput(Vector3 input)
    {
        _rawMoveInput = new Vector2(input.x, input.z);
    }

    private void Awake()
    {
        _rb = _rb ?? GetComponent<Rigidbody>();
        _anim = _anim ?? GetComponent<Animator>();

        _inputDeadzoneSqr = inputDeadzone * inputDeadzone;

        if (_rb != null)
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        ReadInputsFallback();
        GatherTurnInput();
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        ApplyTurn();
        ApplyMovement();
    }

    // -------------------------
    // Input handlers (PlayerInput kan ramme forskellige af dem)
    // -------------------------
    public void OnMove(Vector2 value) => SetMoveInput(value);
    public void OnLook(Vector2 value) => SetTurnInput(value);

    public void OnMove(InputValue value) => SetMoveInput(value.Get<Vector2>());
    public void OnLook(InputValue value) => SetTurnInput(value.Get<Vector2>());

    public void OnMove(InputAction.CallbackContext ctx) => SetMoveInput(ReadVector2FromContext(ctx));
    public void OnLook(InputAction.CallbackContext ctx) => SetTurnInput(ReadVector2FromContext(ctx));

    private static Vector2 ReadVector2FromContext(InputAction.CallbackContext ctx)
    {
        return (ctx.phase == InputActionPhase.Performed || ctx.phase == InputActionPhase.Started)
            ? ctx.ReadValue<Vector2>()
            : Vector2.zero;
    }

    private void SetMoveInput(Vector2 v) => _rawMoveInput = v;
    private void SetTurnInput(Vector2 v) => _rawTurnInput = v;

    // -------------------------
    // Movement / turning logic
    // -------------------------
    private void ReadInputsFallback()
    {
        // Brug joystick-input hvis der ER noget, ellers WASD/piletaster til test på PC
        if (_rawMoveInput.sqrMagnitude <= _inputDeadzoneSqr)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            _rawMoveInput = new Vector2(h, v);

            if (Mathf.Abs(_rawMoveInput.y) < inputDeadzone) _rawMoveInput.y = 0f;
            if (Mathf.Abs(_rawMoveInput.x) < inputDeadzone) _rawMoveInput.x = 0f;
        }
        else
        {
            // deadzone på joystick-aksen
            if (Mathf.Abs(_rawMoveInput.x) < inputDeadzone) _rawMoveInput.x = 0f;
            if (Mathf.Abs(_rawMoveInput.y) < inputDeadzone) _rawMoveInput.y = 0f;
        }

        // deadzone på dreje-joystick
        if (Mathf.Abs(_rawTurnInput.x) < inputDeadzone) _rawTurnInput.x = 0f;
        if (Mathf.Abs(_rawTurnInput.y) < inputDeadzone) _rawTurnInput.y = 0f;
    }

    private void GatherTurnInput()
    {
        // vandret akse på turn-stick styrer yaw
        float lookX = _rawTurnInput.x;
        _pendingYawRate = Mathf.Abs(lookX) > inputDeadzone ? lookX * rotationSpeed : 0f;
    }

    private void ApplyTurn()
    {
        if (_rb == null) return;

        float currentYaw = _rb.rotation.eulerAngles.y;
        float targetYaw = currentYaw + _pendingYawRate * Time.fixedDeltaTime;

        float smoothYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref _yawSmoothVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        _rb.MoveRotation(Quaternion.Euler(0f, smoothYaw, 0f));
    }

    public void ApplyMovement()
    {
        if (_rb == null) return;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 planar = (right * _rawMoveInput.x) + (forward * _rawMoveInput.y);
        Vector3 velocity = planar.normalized * (planar.magnitude * movementSpeed);

        // behold lodret hastighed (fx tyngdekraft)
        velocity.y = _rb.linearVelocity.y;
        _rb.linearVelocity = velocity;
    }

    private void UpdateAnimationState()
    {
        if (_anim == null || _rb == null) return;

        // se om pandaen flytter sig hen over gulvet
        Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
        Vector2 planarVel = new Vector2(localVelocity.x, localVelocity.z);

        bool isMoving = planarVel.sqrMagnitude > walkThreshold;
        bool isTurning = Mathf.Abs(_rawTurnInput.x) > inputDeadzone;

        bool isWalking = isMoving || isTurning;

        _anim.SetBool("IsWalking", isWalking);
    }
}
