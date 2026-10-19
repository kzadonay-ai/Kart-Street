using UnityEngine;
using UnityEngine.InputSystem;

public class camaracarro3 : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;
    public carro3 car;

    [Header("Posición de la cámara")]
    public float distance = 4f;
    public float height = 1.64f;
    public float yawFollowSpeed = 10f;   // qué tan rápido la cámara se coloca detrás del coche al girar (más bajo = más cinematográfico)

    [Header("Hacia dónde mira")]
    public float lookHeight = 1f;

    [Header("Control con el mouse (solo mientras mantienes click)")]
    public float mouseSensitivity = 2f;
    public float mouseSmoothTime = 0.05f;
    public float minVerticalAngle = -10f;
    public float maxVerticalAngle = 60f;
    public float minHorizontalAngle = -110f;
    public float maxHorizontalAngle = 110f;
    public float returnDelay = 0.3f;
    public float returnSmoothTime = 0.6f;

    [Header("Dinamismo / sensación 3D")]
    public float leanAmount = 4f;        // grados MÁXIMOS de inclinación al girar
    public float leanSmoothness = 5f;
    public float bobAmount = 0.02f;
    public float bobSpeed = 10f;
    public float fovBase = 75f;
    public float fovMaxBoost = 12f;
    public float fovSmoothness = 3f;

    [Header("DEBUG (solo lectura)")]
    public float debugYaw;
    public float debugPitch;
    public bool debugIsOrbiting;

    private float yaw;
    private float pitch;
    private float yawVelocity;
    private float pitchVelocity;
    private float releaseTimer;

    private float smoothedYaw;
    private float lastTargetYaw;
    private float yawRate;
    private float currentLean;
    private float bobTimer;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = fovBase;
        pitch = 15f;
        yaw = 0f;

        if (target != null)
        {
            smoothedYaw = target.eulerAngles.y;
            lastTargetYaw = smoothedYaw;
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        bool orbiting = Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed;
        debugIsOrbiting = orbiting;

        if (orbiting)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            releaseTimer = 0f;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float targetYawInput = Mathf.Clamp(yaw + mouseDelta.x * mouseSensitivity, minHorizontalAngle, maxHorizontalAngle);
            float targetPitchInput = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, minVerticalAngle, maxVerticalAngle);

            yaw = Mathf.SmoothDampAngle(yaw, targetYawInput, ref yawVelocity, mouseSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitchInput, ref pitchVelocity, mouseSmoothTime);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            releaseTimer += Time.deltaTime;
            if (releaseTimer >= returnDelay)
            {
                yaw = Mathf.SmoothDampAngle(yaw, 0f, ref yawVelocity, returnSmoothTime);
                pitch = Mathf.SmoothDamp(pitch, 15f, ref pitchVelocity, returnSmoothTime);
            }
        }

        yaw = Mathf.Clamp(yaw, minHorizontalAngle, maxHorizontalAngle);
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        debugYaw = yaw;
        debugPitch = pitch;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        // --- Solo el giro (yaw) del coche, suavizado. Ignora cabeceo/balanceo de rampas ---
        float targetYaw = target.eulerAngles.y;

        float delta = Mathf.DeltaAngle(lastTargetYaw, targetYaw);
        lastTargetYaw = targetYaw;
        float rate = delta / dt; // grados por segundo reales
        yawRate = Mathf.Lerp(yawRate, rate, 1f - Mathf.Exp(-leanSmoothness * dt));

        smoothedYaw = Mathf.LerpAngle(smoothedYaw, targetYaw, 1f - Mathf.Exp(-yawFollowSpeed * dt));

        // --- Posición: pegada al coche, sin retraso (distancia constante) ---
        Quaternion baseRot = Quaternion.Euler(0f, smoothedYaw, 0f);
        Quaternion finalRotation = baseRot * Quaternion.Euler(pitch, yaw, 0f);

        float speedRatio = 0f;
        float maxTurn = 120f;
        if (car != null)
        {
            speedRatio = Mathf.Clamp01(Mathf.Abs(car.CurrentMove) / (car.speed * Time.deltaTime + 0.0001f));
            maxTurn = Mathf.Max(car.turnSpeed, 1f);
        }

        bobTimer += dt * bobSpeed * speedRatio;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmount * speedRatio;

        Vector3 desiredPosition = target.position
                                  + finalRotation * new Vector3(0f, 0f, -distance)
                                  + Vector3.up * (height + bobOffset);

        transform.position = desiredPosition;

        // --- Rotación: mira directo al coche, así nunca se desliza en pantalla ---
        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        Quaternion look = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);

        // Inclinación suave y con tope, según qué tan rápido gira de verdad
        float targetLean = -Mathf.Clamp(yawRate / maxTurn, -1f, 1f) * leanAmount * speedRatio;
        currentLean = Mathf.Lerp(currentLean, targetLean, 1f - Mathf.Exp(-leanSmoothness * dt));

        transform.rotation = look * Quaternion.Euler(0f, 0f, currentLean);

        if (cam != null)
        {
            float targetFov = fovBase + fovMaxBoost * speedRatio;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-fovSmoothness * dt));
        }
    }
}