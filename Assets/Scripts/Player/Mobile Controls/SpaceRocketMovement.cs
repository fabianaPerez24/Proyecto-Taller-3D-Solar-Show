using System.Collections;
using UnityEngine;

public class SpaceRocketMovement : BaseVehicle
{
    private Turbo turbo;
    private IInputProvider inputProvider;

    [Header("Configs de aceleración")]
    [SerializeField] private float accelerationForce = 5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float brakeForce = 5f;

    [Header("Configs de volante")]
    [SerializeField] private float steeringSpeed = 2f;
    [SerializeField] private float driftThreshold = 0.5f;

    [Header("Configs de turbo")]
    [SerializeField] private float driftBoostForce = 10f;
    [SerializeField] private float requiredBoostTime = 2f;
    [SerializeField] private float boostSpeedIncrease = 5f;

    [Header("Estados")]
    [SerializeField] private bool isTurning = false;
    [SerializeField] private bool boostActivated = false;
    [SerializeField] private float boostPressedTime = 0f;
    [SerializeField] private bool isBoostOn;

    [Header("Audio")]
    [SerializeField] private AudioClip acceleratorClip;
    [SerializeField] private AudioSource audioSource;

    [Header("Estados de derrape")]
    [SerializeField] private bool rightDrifting = false;
    [SerializeField] private bool leftDrifting = false;
    [SerializeField] private bool isDrifting = false;

    protected override void Start()
    {
        base.Start();
        turbo = GetComponent<Turbo>();

        inputProvider = FindFirstObjectByType<MobileInputManager>();
    }

    private void OnEnable()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.OnTurboTriggered += ExecuteTurboBoost;
            MobileInputManager.Instance.OnDriftStarted += HandleDriftStart;
            MobileInputManager.Instance.OnDriftReleased += HandleDriftRelease;
        }
    }

    private void OnDisable()
    {
        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.OnTurboTriggered -= ExecuteTurboBoost;
            MobileInputManager.Instance.OnDriftStarted -= HandleDriftStart;
            MobileInputManager.Instance.OnDriftReleased -= HandleDriftRelease;
        }
    }

    private void Update()
    {
        if (Mathf.Abs(currentSpeed) < 0.5f && inputProvider.VerticalInput == 0f)
        {
            currentSpeed = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!onStun)
        {
            ManageAcceleration();
            ManageSteering();
            ManageDriftCharging();
            ApplyVelocity();
            ManageContinuousTurbo();
        }
    }

    public void OffStun()
    {
        onStun = false;
    }

    protected override void ManageAcceleration()
    {
        if (inputProvider.VerticalInput > 0f)
        {
            if (!turbo.TurboActive)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = acceleratorClip;
                    audioSource.Play();
                }
            }
            currentSpeed += accelerationForce * Time.deltaTime;
        }

        else if (inputProvider.VerticalInput < 0f)
        {
            currentSpeed -= brakeForce * Time.deltaTime;
        }

        else
        {
            if (audioSource.isPlaying && audioSource.clip == acceleratorClip)
            {
                audioSource.Stop();
            }

            if (currentSpeed > 0f)
            {
                currentSpeed -= brakeForce * Time.deltaTime * 0.75f;
            }

            else if (currentSpeed < 0f)
            {
                currentSpeed += brakeForce * Time.deltaTime * 0.75f;
            }
        }

        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);
        audioSource.volume = Mathf.Abs(currentSpeed) / maxSpeed;
    }

    protected override void ManageSteering()
    {
        float horizontalInput = inputProvider.HorizontalInput;
        float steeringModifier = isTurning ? 1.5f : 1f;
        float steeringAngle = horizontalInput * steeringSpeed * steeringModifier;

        if (isTurning && !turbo.TurboActive)
        {
            if (isDrifting)
            {
                if (leftDrifting)
                {
                    if (horizontalInput > 0f) steeringAngle *= 0.55f;

                    else if (horizontalInput < 0f && currentSpeed > 0f) steeringAngle *= 1.55f;
                }

                else if (rightDrifting)
                {
                    if (horizontalInput < 0f) steeringAngle *= 0.55f;

                    else if (horizontalInput > 0f && currentSpeed > 0f) steeringAngle *= 1.55f;
                }
            }

            else
            {
                steeringAngle *= 1.1f;
            }
        }

        transform.Rotate(Vector3.up, steeringAngle);

        if (Mathf.Abs(horizontalInput) > driftThreshold && currentSpeed > 0f)
        {
            isTurning = true;
        }

        else
        {
            isTurning = false;
        }
    }

    private void ManageDriftCharging()
    {
        if (isTurning)
        {
            boostPressedTime += Time.deltaTime;
            if (boostPressedTime >= requiredBoostTime)
            {
                turbo.Charging = true;
                boostActivated = true;
            }
        }

        else
        {
            turbo.Charging = false;
            if (boostActivated && !isBoostOn)
            {
                maxSpeed = 75f;
                currentSpeed += driftBoostForce * Time.deltaTime;
                boostPressedTime = 0f;
                Invoke(nameof(ResetVelocity), 0.5f);
            }

            else
            {
                if (boostPressedTime > 0f) boostPressedTime = 0f;
            }
        }
    }

    private void ManageContinuousTurbo()
    {
        if (turbo.TurboActive && !isTurning && !isBoostOn)
        {
            maxSpeed = 150f;
            currentSpeed += driftBoostForce * Time.deltaTime;
            boostPressedTime = 0f;
            Invoke(nameof(ResetVelocity), 0.5f);
        }
    }

    protected override void ApplyVelocity()
    {
        Vector3 velocityDirection = transform.forward * currentSpeed;
        rb.linearVelocity = new Vector3(velocityDirection.x, rb.linearVelocity.y, velocityDirection.z);

        if (!boostActivated)
        {
            ResetVelocity();
        }
    }

    public void ResetVelocity()
    {
        boostActivated = false;
        isBoostOn = false;

        if (gameObject.activeSelf)
        {
            StartCoroutine(ReduceMaxSpeed());
        }
    }

    private IEnumerator ReduceMaxSpeed()
    {
        while (maxSpeed > 50f)
        {
            if (maxSpeed <= 120f)
            {
                maxSpeed -= boostSpeedIncrease * Time.deltaTime * 0.1f;
            }

            else
            {
                maxSpeed -= boostSpeedIncrease * Time.deltaTime * 0.5f;
            }

            if (maxSpeed <= 50f) maxSpeed = 50f;

            yield return null;
        }
    }

    private void ExecuteTurboBoost()
    {
        maxSpeed = 130f;
        currentSpeed += driftBoostForce * Time.deltaTime;
        boostPressedTime = 0f;
        isBoostOn = true;
    }

    private void HandleDriftStart(bool isRight)
    {
        isDrifting = true;
        if (isRight)
        {
            rightDrifting = true;
            leftDrifting = false;
        }

        else
        {
            leftDrifting = true;
            rightDrifting = false;
        }
    }

    private void HandleDriftRelease(bool isRight)
    {
        if (isRight) rightDrifting = false;
        else leftDrifting = false;

        if (!rightDrifting && !leftDrifting)
        {
            isDrifting = false;
        }
    }
}