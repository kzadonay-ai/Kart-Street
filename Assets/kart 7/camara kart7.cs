using UnityEngine;
using UnityEngine.InputSystem;

public class camarakart7 : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;          // chasis
    public Rigidbody carBody;         // Rigidbody de chasis
    public WheelInput wheelInput;     // Wheel Input de chasis
    public float maxSpeedRef = 40f;
    public bool useColliderCenter = true;      // mira al centro real del carro
    public Vector3 focusOffset = Vector3.zero; // solo si useColliderCenter está apagado

    [Header("Posición de la cámara")]
    public float distance = 5f;
    public float height = 2f;
    public float positionSmoothTime = 0.12f;
    public float yawFollowSpeed = 4f;
    public float rotationSmoothSpeed = 8f;

    [Header("Hacia dónde mira")]
    public float lookHeight = 0.5f;

    [Header("Control con el mouse (mientras mantienes click)")]
    public float mouseSensitivity = 2f;
    public float mouseSmoothTime = 0.05f;
    public float minVerticalAngle = -10f;
    public float maxVerticalAngle = 60f;
    public float minHorizontalAngle = -110f;
    public float maxHorizontalAngle = 110f;
    public float returnDelay = 0.3f;
    public float returnSmoothTime = 0.6f;

    [Header("Dinamismo")]
    public float leanAmount = 3f;
    public float leanSmoothness = 4f;
    public float bobAmount = 0.02f;
    public float bobSpeed = 10f;
    public float fovBase = 75f;
    public float fovMaxBoost = 12f;
    public float fovSmoothness = 3f;

    [Header("DEBUG")]
    public float debugYaw;
    public float debugPitch;
    public bool debugIsOrbiting;

    float yaw, pitch, yawVelocity, pitchVelocity;
    float releaseTimer;
    Vector3 posVelocity;
    float currentLean, bobTimer;
    float followYaw;
    Camera cam;
    BoxCollider col;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = fovBase;
        pitch = 15f;
        yaw = 0f;
        if (target != null)
        {
            followYaw = target.eulerAngles.y;
            col = target.GetComponent<BoxCollider>();
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

            Vector2 d = Mouse.current.delta.ReadValue();
            float targetYaw = Mathf.Clamp(yaw + d.x * mouseSensitivity, minHorizontalAngle, maxHorizontalAngle);
            float targetPitch = Mathf.Clamp(pitch - d.y * mouseSensitivity, minVerticalAngle, maxVerticalAngle);

            yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVelocity, mouseSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, mouseSmoothTime);
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

        // Punto al que mira: el centro real del carro
        Vector3 localFocus = (useColliderCenter && col != null) ? col.center : focusOffset;
        Vector3 focus = target.TransformPoint(localFocus);

        // Solo seguimos el giro en Y, suavizado
        followYaw = Mathf.LerpAngle(followYaw, target.eulerAngles.y, yawFollowSpeed * Time.deltaTime);
        Quaternion baseRot = Quaternion.Euler(0f, followYaw, 0f);
        Quaternion finalRotation = baseRot * Quaternion.Euler(pitch, yaw, 0f);

        Vector3 offset = finalRotation * new Vector3(0f, 0f, -distance);
        Vector3 desiredPosition = focus + offset + Vector3.up * height;

        float speedRatio = 0f;
        if (carBody != null)
            speedRatio = Mathf.Clamp01(carBody.linearVelocity.magnitude / maxSpeedRef);

        float steer = wheelInput != null ? wheelInput.Steer : 0f;
        currentLean = Mathf.Lerp(currentLean, -steer * leanAmount, Time.deltaTime * leanSmoothness);

        bobTimer += Time.deltaTime * bobSpeed * speedRatio;
        desiredPosition += Vector3.up * (Mathf.Sin(bobTimer) * bobAmount * speedRatio);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref posVelocity, positionSmoothTime);

        Vector3 lookDir = (focus + Vector3.up * lookHeight) - transform.position;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(lookDir, Vector3.up);
            Quaternion targetRotation = Quaternion.Euler(look.eulerAngles.x, look.eulerAngles.y, currentLean);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
        }

        if (cam != null)
        {
            float targetFov = fovBase + fovMaxBoost * speedRatio;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * fovSmoothness);
        }
    }
}