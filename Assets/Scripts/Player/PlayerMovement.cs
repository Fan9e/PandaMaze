using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    // ==============================
    // Inspector fields
    // ==============================

    [Header("Movement")]
    /// <summary>
    /// Hvor hurtigt spilleren bevæger sig vandret i verden.
    /// </summary>
    [SerializeField] private float movementSpeed = 5f;

    /// <summary>
    /// Mindste kvadrerede hastighed, før spilleren betragtes som gående (til animation).
    /// </summary>
    [SerializeField] private float walkThreshold = 0.0001f;

    /// <summary>
    /// Mindste input-styrke før joystick/keyboard input tælles som gyldigt.
    /// </summary>
    [SerializeField] private float inputDeadzone = 0.05f;

    [Header("Turning")]
    /// <summary>
    /// Grader per sekund spilleren kan dreje (joystick eller keyboard).
    /// </summary>
    [SerializeField, Tooltip("Degrees per second when turning (joystick or keyboard).")]
    private float rotationSpeed = 120f;

    /// <summary>
    /// Hvor glat rotationen skal interpoleres (jo lavere, jo mere responsiv).
    /// </summary>
    [SerializeField, Tooltip("Smoothness when turning.")]
    private float rotationSmoothTime = 0.08f;

    // ==============================
    // Components
    // ==============================

    /// <summary>
    /// Reference til spillerens Rigidbody, bruges til fysisk bevægelse og rotation.
    /// </summary>
    private Rigidbody _rb;

    /// <summary>
    /// Reference til spillerens Animator, styrer gå-/idle-animationer.
    /// </summary>
    private Animator _anim;

    // ==============================
    // Input state
    // ==============================

    /// <summary>
    /// Bevægelsesinput (x = strafe, y = frem/tilbage) fra joystick/keyboard.
    /// </summary>
    private Vector2 _moveInput; // x = strafe, y = forward/back

    /// <summary>
    /// Drejeinput (x = venstre/højre) fra joystick/keyboard.
    /// </summary>
    private Vector2 _turnInput; // x = turn left/right

    /// <summary>
    /// Angiver om bevægelsesinput denne frame kom fra Input System.
    /// </summary>
    private bool _moveFromInputSystem;

    /// <summary>
    /// Angiver om drejeinput denne frame kom fra Input System.
    /// </summary>
    private bool _turnFromInputSystem;

    // ==============================
    // Rotation smoothing
    // ==============================

    /// <summary>
    /// Beregnet yaw-ændring (drejehastighed) for den næste fysik-opdatering.
    /// </summary>
    private float _pendingYaw;

    /// <summary>
    /// Intern værdi brugt af SmoothDampAngle til at glatte rotationen.
    /// </summary>
    private float _yawSmoothVelocity;

    // ==============================
    // Unity lifecycle
    // ==============================

    /// <summary>
    /// Finder komponentreferencer og sætter Rigidbody interpolation.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<Animator>();

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// <summary>
    /// Læser input (inkl. editor fallback), beregner turning og opdaterer animation.
    /// </summary>
    private void Update()
    {
        ReadInput_EditorFallback(); // joystick + optional keyboard
        ComputeTurnInput();
        UpdateAnimation();

        ResetInputFlagsIfReleased();
    }

    /// <summary>
    /// Anvender rotation og bevægelse på Rigidbody i faste fysik-steps.
    /// </summary>
    private void FixedUpdate()
    {
        ApplyRotation();
        ApplyMovement();
    }

    // ===========================================
    // Public API (tests / legacy support)
    // ===========================================

    /// <summary>
    /// Sætter bevægelsesinput udefra vha. et 3D-vektor (x/z bruges som 2D-input).
    /// </summary>
    /// <param name="input3D">Bevægelsesvektor i verdensrum (x/z-planet).</param>
    public void SetMovementInput(Vector3 input3D)
    {
        SetMoveInput(new Vector2(input3D.x, input3D.z));
    }

    // ===========================================
    // Input handlers (Input System)
    // ===========================================

    /// <summary>
    /// Input System callback for Move (direkte Vector2).
    /// </summary>
    public void OnMove(Vector2 v) => SetMoveInput(v);

    /// <summary>
    /// Input System callback for Move via InputValue wrapper.
    /// </summary>
    public void OnMove(InputValue v) => SetMoveInput(v.Get<Vector2>());

    /// <summary>
    /// Input System callback for Move via CallbackContext (events/phase-baseret).
    /// </summary>
    public void OnMove(InputAction.CallbackContext ctx) => SetMoveInput(ReadFromContext(ctx));

    /// <summary>
    /// Input System callback for Look/Turn (direkte Vector2).
    /// </summary>
    public void OnLook(Vector2 v) => SetTurnInput(v);

    /// <summary>
    /// Input System callback for Look/Turn via InputValue wrapper.
    /// </summary>
    public void OnLook(InputValue v) => SetTurnInput(v.Get<Vector2>());

    /// <summary>
    /// Input System callback for Look/Turn via CallbackContext (events/phase-baseret).
    /// </summary>
    public void OnLook(InputAction.CallbackContext ctx) => SetTurnInput(ReadFromContext(ctx));

    /// <summary>
    /// Læser et Vector2-input fra en InputAction-context, kun når den er aktiv/utført.
    /// </summary>
    private static Vector2 ReadFromContext(InputAction.CallbackContext ctx)
    {
        return (ctx.phase == InputActionPhase.Performed || ctx.phase == InputActionPhase.Started)
            ? ctx.ReadValue<Vector2>()
            : Vector2.zero;
    }

    /// <summary>
    /// Opdaterer bevægelsesinput og markerer at det kom fra Input System denne frame.
    /// </summary>
    private void SetMoveInput(Vector2 v)
    {
        _moveInput = v;
        _moveFromInputSystem = true;
    }

    /// <summary>
    /// Opdaterer drejeinput og markerer at det kom fra Input System denne frame.
    /// </summary>
    private void SetTurnInput(Vector2 v)
    {
        _turnInput = v;
        _turnFromInputSystem = true;
    }

    // ===========================================
    // Editor keyboard fallback
    // ===========================================

    /// <summary>
    /// Fallback til keyboard (WASD/piletaster) i editor, hvis Input System ikke gav input.
    /// Anvender samtidig deadzones på alle input-aksler.
    /// </summary>
    private void ReadInput_EditorFallback()
    {
#if UNITY_EDITOR
        float kbForward = Input.GetAxisRaw("Vertical");   // W/S, Up/Down
        float kbTurn = Input.GetAxisRaw("Horizontal");    // A/D, Left/Right

        // Movement override (only if joystick gave no movement this frame)
        if (!_moveFromInputSystem)
        {
            _moveInput.y = kbForward;

            if (Mathf.Abs(kbForward) > 0.01f)
                _moveInput.x = 0f; // disable strafe for keyboard
        }

        // Turning override (only if joystick gave no turn input)
        if (!_turnFromInputSystem)
        {
            _turnInput.x = kbTurn;
        }
#endif
        // Apply deadzones (editor + device)
        if (Mathf.Abs(_moveInput.x) < inputDeadzone) _moveInput.x = 0f;
        if (Mathf.Abs(_moveInput.y) < inputDeadzone) _moveInput.y = 0f;

        if (Mathf.Abs(_turnInput.x) < inputDeadzone) _turnInput.x = 0f;
    }

    // ===========================================
    // Turning and movement
    // ===========================================

    /// <summary>
    /// Beregner den ønskede yaw-hastighed (drejehastighed) ud fra input og deadzone.
    /// </summary>
    private void ComputeTurnInput()
    {
        _pendingYaw = Mathf.Abs(_turnInput.x) > inputDeadzone
            ? _turnInput.x * rotationSpeed
            : 0f;
    }

    /// <summary>
    /// Anvender glattet rotation på Rigidbody baseret på pending yaw.
    /// </summary>
    private void ApplyRotation()
    {
        float currentYaw = _rb.rotation.eulerAngles.y;
        float targetYaw = currentYaw + _pendingYaw * Time.fixedDeltaTime;

        float smoothedYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref _yawSmoothVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        _rb.MoveRotation(Quaternion.Euler(0, smoothedYaw, 0));
    }

    /// <summary>
    /// Beregner vandret bevægelsesvektor og sætter Rigidbodyens hastighed.
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 planar = (forward * _moveInput.y) + (right * _moveInput.x);
        Vector3 vel = planar.normalized * (planar.magnitude * movementSpeed);

        vel.y = _rb.linearVelocity.y; // keep gravity
        _rb.linearVelocity = vel;
    }

    // ===========================================
    // Animation
    // ===========================================

    /// <summary>
    /// Bestemmer om spilleren går eller drejer og sætter Animator-booleanen IsWalking.
    /// </summary>
    private void UpdateAnimation()
    {
        Vector3 localVel = transform.InverseTransformDirection(_rb.linearVelocity);
        Vector2 planar = new Vector2(localVel.x, localVel.z);

        bool isMoving = planar.sqrMagnitude > walkThreshold;
        bool isTurning = Mathf.Abs(_turnInput.x) > inputDeadzone;

        _anim.SetBool("IsWalking", isMoving || isTurning);
    }

    // ===========================================
    // Reset Input Flags
    // ===========================================

    /// <summary>
    /// Nulstiller flag for om input kom fra Input System, når input går tilbage til nul.
    /// </summary>
    private void ResetInputFlagsIfReleased()
    {
        if (_moveInput == Vector2.zero)
            _moveFromInputSystem = false;

        if (_turnInput == Vector2.zero)
            _turnFromInputSystem = false;
    }
}
