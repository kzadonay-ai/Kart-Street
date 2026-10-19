using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class contoroldeauto : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 40f;
    public float turnSpeed = 120f;
    public float acceleration = 60f;
    public float deceleration = 40f;
    public float brakeDeceleration = 90f;

    [Header("Freno de mano (Espacio)")]
    public float handbrakeDeceleration = 150f;

    [Header("Derrape")]
    public float driftSpeedThreshold = 0.6f;
    public float driftGripLow = 2f;
    public float driftGripHigh = 12f;
    public float driftSteerBoost = 1.25f;

    [Header("Rebote al chocar")]
    [Range(0f, 1f)] public float bounceFactor = 0.4f;
    [Range(0f, 1f)] public float wallSlideKeep = 0.8f;
    public float minBounceSpeed = 5f;
    public float bounceDuration = 0.3f;
    public float bounceGrip = 1.5f;
    public float bounceSpinKick = 60f;
    public float bounceCooldown = 0.2f;

    [Header("Anti-salto")]
    public float maxDepenetration = 2f;
    public float wallContactTime = 0.15f;

    [Header("Suelo, rampas y aire")]
    public float groundCheckDistance = 0.3f;   // qué tan lejos del suelo se considera "en el suelo"
    public float gravityMultiplier = 2.5f;     // gravedad extra en el aire (más alto = cae más rápido)
    public float groundAlignSpeed = 12f;       // qué tan rápido se acomoda a la pendiente
    public float airAlignSpeed = 3f;           // qué tan rápido sigue la trayectoria en el aire
    public float maxAirPitch = 35f;            // inclinación máxima de la nariz en el aire
    [Range(0f, 1f)] public float airSteerFactor = 0.3f; // control de giro en el aire

    [Header("Ruedas")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform backLeftWheel;
    public Transform backRightWheel;
    public float wheelSpinSpeed = 300f;
    public float maxSteerAngle = 30f;
    public float steerSmoothness = 8f;

    private Rigidbody rb;
    private Collider col;
    private float currentSpeed;
    private float move;
    private float turn;
    private float spinAngle;
    private float currentSteerAngle;
    private bool isDrifting;
    private bool isGrounded;

    private float vertical;
    private float horizontal;
    private bool handbrake;

    // Orientación
    private float yaw;
    private Quaternion tiltRot = Quaternion.identity;

    // Estado del rebote
    private Vector3 preCollisionVelocity;
    private float bounceTimer;
    private float cooldownTimer;
    private float bounceYaw;
    private float wallContactTimer;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[8];

    public float CurrentMove => move;
    public float CurrentTurn => turn;
    public bool IsDrifting => isDrifting;
    public bool IsBouncing => bounceTimer > 0f;
    public bool IsGrounded => isGrounded;

    public event Action<float> OnImpact;

    private Vector3 RbVelocity
    {
#if UNITY_6000_0_OR_NEWER
        get => rb.linearVelocity;
        set => rb.linearVelocity = value;
#else
        get => rb.velocity;
        set => rb.velocity = value;
#endif
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponentInChildren<Collider>();

        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // la rotación la controla el script
        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
        rb.maxDepenetrationVelocity = maxDepenetration;

        yaw = rb.rotation.eulerAngles.y;
    }

    void Update()
    {
        vertical = 0f;
        horizontal = 0f;
        handbrake = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal = 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal = -1f;
            handbrake = Keyboard.current.spaceKey.isPressed;
        }

        move = currentSpeed * Time.deltaTime;

        float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / speed);
        RotateWheels(speedRatio * Mathf.Sign(currentSpeed), horizontal);
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (bounceTimer > 0f) bounceTimer -= dt;
        if (cooldownTimer > 0f) cooldownTimer -= dt;
        if (wallContactTimer > 0f) wallContactTimer -= dt;

        bool bouncing = bounceTimer > 0f;

        // --- Detección de suelo ---
        isGrounded = CheckGround(out Vector3 groundNormal);

        float v = bouncing ? 0f : vertical;
        float h = bouncing ? horizontal * 0.3f : horizontal;

        // --- Aceleración / frenado ---
        float targetSpeed = v * speed;
        float accel;

        if (handbrake)
        {
            targetSpeed = 0f;
            accel = handbrakeDeceleration;
        }
        else if (Mathf.Abs(v) > 0.01f)
        {
            bool sameDirection = Mathf.Sign(v) == Mathf.Sign(currentSpeed) || Mathf.Abs(currentSpeed) < 0.1f;
            accel = sameDirection ? acceleration : brakeDeceleration;
        }
        else
        {
            accel = bouncing ? deceleration * 0.5f : deceleration;
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * dt);

        bool isMoving = Mathf.Abs(currentSpeed) > 0.05f;
        float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / speed);

        float turnInput = isMoving ? h : 0f;
        if (currentSpeed < 0f) turnInput = -turnInput;

        // --- Derrape ---
        isDrifting = isGrounded && isMoving && Mathf.Abs(h) > 0.5f && (speedRatio > driftSpeedThreshold || handbrake);

        float turnRate = turnSpeed * (isDrifting ? driftSteerBoost : 1f);
        float steerFactor = isGrounded ? 1f : airSteerFactor;
        turn = turnInput * turnRate * steerFactor * dt;

        turn += bounceYaw * dt;
        bounceYaw = Mathf.MoveTowards(bounceYaw, 0f, bounceSpinKick * 4f * dt);

        yaw += turn;
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 flatForward = yawRot * Vector3.forward;

        Vector3 vel = RbVelocity;
        Vector3 horizontalVel;

        if (isGrounded)
        {
            // --- EN EL SUELO: la velocidad sigue la superficie (plano o rampa) ---
            Vector3 fwdOnGround = Vector3.ProjectOnPlane(flatForward, groundNormal).normalized;
            Vector3 desiredVelocity = fwdOnGround * currentSpeed;

            float grip = bouncing ? bounceGrip : (isDrifting ? driftGripLow : driftGripHigh);

            Vector3 planeVel = Vector3.ProjectOnPlane(vel, groundNormal);
            planeVel = Vector3.Lerp(planeVel, desiredVelocity, dt * grip);

            RbVelocity = planeVel;
            horizontalVel = new Vector3(planeVel.x, 0f, planeVel.z);
        }
        else
        {
            // --- EN EL AIRE: conserva su impulso, gravedad extra, sin conducir ---
            horizontalVel = new Vector3(vel.x, 0f, vel.z);
            currentSpeed = Vector3.Dot(horizontalVel, flatForward); // así al aterrizar no hay tirones

            float vy = wallContactTimer > 0f ? Mathf.Min(vel.y, 0f) : vel.y;
            RbVelocity = new Vector3(horizontalVel.x, vy, horizontalVel.z);

            rb.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        }

        // --- Orientación: se acomoda a la pendiente o sigue la trayectoria en el aire ---
        Quaternion targetTilt;
        float alignSpeed;

        if (isGrounded)
        {
            targetTilt = Quaternion.FromToRotation(Vector3.up, groundNormal);
            alignSpeed = groundAlignSpeed;
        }
        else
        {
            float hs = horizontalVel.magnitude;
            float pitch = 0f;
            if (hs > 2f)
                pitch = Mathf.Clamp(-Mathf.Atan2(RbVelocity.y, hs) * Mathf.Rad2Deg, -maxAirPitch, maxAirPitch);

            targetTilt = Quaternion.AngleAxis(pitch, yawRot * Vector3.right);
            alignSpeed = airAlignSpeed;
        }

        tiltRot = Quaternion.Slerp(tiltRot, targetTilt, Mathf.Clamp01(dt * alignSpeed));
        rb.MoveRotation(tiltRot * yawRot);
        rb.angularVelocity = Vector3.zero;

        preCollisionVelocity = horizontalVel;
    }

    // Raycast hacia abajo desde el centro del collider; ignora el propio coche
    bool CheckGround(out Vector3 normal)
    {
        normal = Vector3.up;
        if (col == null) return false;

        Bounds b = col.bounds;
        float dist = b.extents.y + groundCheckDistance;

        int n = Physics.RaycastNonAlloc(b.center, Vector3.down, hitBuffer, dist, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < n; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (hit.collider.attachedRigidbody == rb) continue; // es el propio coche
            if (hit.normal.y < 0.5f) continue;                  // muy inclinado = pared, no suelo

            if (hit.distance < best)
            {
                best = hit.distance;
                normal = hit.normal;
                found = true;
            }
        }

        return found;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!TryGetWallNormal(collision, out Vector3 normal)) return;

        wallContactTimer = wallContactTime;
        Vector3 cv = RbVelocity;
        RbVelocity = new Vector3(cv.x, Mathf.Min(cv.y, 0f), cv.z);

        if (cooldownTimer > 0f) return;

        Vector3 vv = preCollisionVelocity;
        float impactSpeed = -Vector3.Dot(vv, normal);
        if (impactSpeed < minBounceSpeed) return;

        Vector3 vn = Vector3.Dot(vv, normal) * normal;
        Vector3 vt = vv - vn;

        Vector3 bounced = vt * wallSlideKeep + (-vn) * bounceFactor;

        RbVelocity = new Vector3(bounced.x, Mathf.Min(RbVelocity.y, 0f), bounced.z);

        currentSpeed = Vector3.Dot(bounced, transform.forward);

        float side = Vector3.Dot(transform.right, normal);
        bounceYaw = side * bounceSpinKick * Mathf.Clamp01(impactSpeed / speed);

        bounceTimer = bounceDuration;
        cooldownTimer = bounceCooldown;

        OnImpact?.Invoke(Mathf.Clamp01(impactSpeed / speed));
    }

    void OnCollisionStay(Collision collision)
    {
        if (!TryGetWallNormal(collision, out _)) return;

        wallContactTimer = wallContactTime;

        if (bounceTimer > 0f) return;
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeDeceleration * 0.5f * Time.fixedDeltaTime);
    }

    bool TryGetWallNormal(Collision collision, out Vector3 normal)
    {
        normal = Vector3.zero;
        bool found = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;
            if (Mathf.Abs(n.y) < 0.5f)
            {
                normal += new Vector3(n.x, 0f, n.z);
                found = true;
            }
        }

        if (found) normal.Normalize();
        return found;
    }

    void RotateWheels(float speedSigned, float horizontalInput)
    {
        spinAngle += speedSigned * wheelSpinSpeed * Time.deltaTime;

        float targetSteerAngle = horizontalInput * maxSteerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.deltaTime * steerSmoothness);

        if (backLeftWheel) backLeftWheel.localRotation = Quaternion.Euler(spinAngle, 0f, 0f);
        if (backRightWheel) backRightWheel.localRotation = Quaternion.Euler(spinAngle, 0f, 0f);
        if (frontLeftWheel) frontLeftWheel.localRotation = Quaternion.Euler(spinAngle, currentSteerAngle, 0f);
        if (frontRightWheel) frontRightWheel.localRotation = Quaternion.Euler(spinAngle, currentSteerAngle, 0f);
    }
}