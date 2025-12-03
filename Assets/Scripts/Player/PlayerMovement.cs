using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    /// <summary>
    /// Hvor hurtigt spilleren bevæger sig vandret i verden.
    /// </summary>
    [SerializeField] private float movementSpeed = 5f;

    [SerializeField] private float keyboardForwardThreshold = 0.1f; 

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

    /// <summary>
    /// Reference til spillerens Rigidbody, bruges til fysisk bevægelse og rotation.
    /// </summary>
    private Rigidbody _PlayerRigidbody;

    /// <summary>
    /// Reference til spillerens Animator, styrer gå-/idle-animationer.
    /// </summary>
    private Animator _animation;

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

    /// <summary>
    /// Beregnet yaw-ændring (drejehastighed) for den næste fysik-opdatering.
    /// </summary>
    private float _pendingYaw;

    /// <summary>
    /// Intern værdi brugt af SmoothDampAngle til at glatte rotationen.
    /// </summary>
    private float _yawSmoothVelocity;

    /// <summary>
    /// Finder komponentreferencer og sætter Rigidbody interpolation.
    /// </summary>
    private void Awake()
    {
        _PlayerRigidbody = GetComponent<Rigidbody>();
        _animation = GetComponent<Animator>();

        _PlayerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// <summary>
    /// Læser input (inkl. editor fallback), beregner turning og opdaterer animation.
    /// </summary>
    private void Update()
    {
        ReadInput_EditorFallback();
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

    /// <summary>
    /// Sætter bevægelsesinput udefra vha. et 3D-vektor (x/z bruges som 2D-input).
    /// </summary>
    /// <param name="input3D">Bevægelsesvektor i verdensrum (x/z-planet).</param>
    public void SetMovementInput(Vector3 input3D)
    {
        SetMoveInput(new Vector2(input3D.x, input3D.z));
    }

    /// <summary>
    /// Input System callback for Move (direkte Vector2).
    /// </summary>
    public void OnMove(Vector2 movementVector) => SetMoveInput(movementVector);

    /// <summary>
    /// Input System callback for Move via InputValue wrapper.
    /// </summary>
    public void OnMove(InputValue movementVector) => SetMoveInput(movementVector.Get<Vector2>());

    /// <summary>
    /// Input System callback for Move via CallbackContext (events/phase-baseret).
    /// </summary>
    public void OnMove(InputAction.CallbackContext callbackContext) => SetMoveInput(ReadFromContext(callbackContext));

    /// <summary>
    /// Input System callback for Look/Turn (direkte Vector2).
    /// </summary>
    public void OnLook(Vector2 movementVector) => SetTurnInput(movementVector);

    /// <summary>
    /// Input System callback for Look/Turn via InputValue wrapper.
    /// </summary>
    public void OnLook(InputValue movementVector) => SetTurnInput(movementVector.Get<Vector2>());

    /// <summary>
    /// Input System callback for Look/Turn via CallbackContext (events/phase-baseret).
    /// </summary>
    public void OnLook(InputAction.CallbackContext callbackContext) => SetTurnInput(ReadFromContext(callbackContext));

    /// <summary>
    /// Læser et Vector2-input fra en InputAction-context, men kun når actionen er startet eller udført.
    /// </summary>
    /// <param name="callbackContext">Context-information fra InputAction-callbacken.</param>
    private static Vector2 ReadFromContext(InputAction.CallbackContext callbackContext)
    {
        return (callbackContext.phase == InputActionPhase.Performed ||
                callbackContext.phase == InputActionPhase.Started)
            ? callbackContext.ReadValue<Vector2>()
            : Vector2.zero;
    }

    /// <summary>
    /// Opdaterer bevægelsesinput og markerer, at det kom fra Input System denne frame.
    /// </summary>
    /// <param name="movementVector">Den modtagne bevægelsesvektor (x = strafe, y = frem/tilbage).</param>
    private void SetMoveInput(Vector2 movementVector)
    {
        _moveInput = movementVector;
        _moveFromInputSystem = true;
    }

    /// <summary>
    /// Opdaterer drejeinput og markerer, at det kom fra Input System denne frame.
    /// </summary>
    /// <param name="turnVector">Den modtagne drejevektor (x = venstre/højre).</param>
    private void SetTurnInput(Vector2 turnVector)
    {
        _turnInput = turnVector;
        _turnFromInputSystem = true;
    }

    /// <summary>
    /// Fallback til keyboard (WASD/piletaster) i editor, hvis Input System ikke gav input.
    /// Anvender samtidig deadzones på alle input-aksler.
    /// </summary>
    private void ReadInput_EditorFallback()
    {
#if UNITY_EDITOR
        float keyboardForwardInput = Input.GetAxisRaw("Vertical");
        float keyboardTurnInput = Input.GetAxisRaw("Horizontal");

        // Movement overskrives (Hvis joystick ikke giver nogen bevægelse i den frame)
        if (!_moveFromInputSystem)
        {
            _moveInput.y = keyboardForwardInput;

            if (Mathf.Abs(keyboardForwardInput) > keyboardForwardThreshold)
                _moveInput.x = 0f;
        }

        // Drejer overskrives(Kun hvis joystick ikke giver nogen inputs)
        if (!_turnFromInputSystem)
        {
            _turnInput.x = keyboardTurnInput;
        }
#endif
        // tilføj deadzones
        if (Mathf.Abs(_moveInput.x) < inputDeadzone) _moveInput.x = 0f;
        if (Mathf.Abs(_moveInput.y) < inputDeadzone) _moveInput.y = 0f;

        if (Mathf.Abs(_turnInput.x) < inputDeadzone) _turnInput.x = 0f;
    }

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
        float currentYaw = _PlayerRigidbody.rotation.eulerAngles.y;
        float targetYaw = currentYaw + _pendingYaw * Time.fixedDeltaTime;

        float smoothedYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref _yawSmoothVelocity,
            rotationSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        _PlayerRigidbody.MoveRotation(Quaternion.Euler(0, smoothedYaw, 0));
    }

    /// <summary>
    /// Beregner vandret bevægelsesvektor og sætter Rigidbodyens hastighed.
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 forwardDirection = transform.forward;
        Vector3 rightDirection = transform.right;

        forwardDirection.y = 0f;
        rightDirection.y = 0f;
        forwardDirection.Normalize();
        rightDirection.Normalize();

        Vector3 planarMovement = (forwardDirection * _moveInput.y) + (rightDirection * _moveInput.x);
        Vector3 velocity = planarMovement.normalized * (planarMovement.magnitude * movementSpeed);

        // Bevar lodret hastighed (tyngdekraft osv.)
        velocity.y = _PlayerRigidbody.linearVelocity.y;
        _PlayerRigidbody.linearVelocity = velocity;
    }

    /// <summary>
    /// Bestemmer om spilleren går eller drejer og sætter Animator-booleanen "IsWalking".
    /// </summary>
    private void UpdateAnimation()
    {
        Vector3 localVelocity =
            transform.InverseTransformDirection(_PlayerRigidbody.linearVelocity);

        Vector2 planarVelocity = new Vector2(localVelocity.x, localVelocity.z);

        bool isMoving = planarVelocity.sqrMagnitude > walkThreshold;
        bool isTurning = Mathf.Abs(_turnInput.x) > inputDeadzone;

        _animation.SetBool("IsWalking", isMoving || isTurning);
    }

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
