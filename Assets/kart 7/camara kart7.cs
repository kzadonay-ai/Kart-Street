using UnityEngine;
using UnityEngine.InputSystem;

public class camarakart7 : MonoBehaviour
{
       [Header("Objetivo")]
    public Transform target;
    public contoroldeauto car;

    [Header("Posición de la cámara")]
    public float distance = 1.14f;
    public float height = 1.64f;
    public float positionSmoothTime = 0.03f;
    public float rotationSmoothSpeed = 6f; // qué tan fluida es la rotación (más bajo = más cinematográfico tipo Forza)

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
    public float leanAmount = 6f;
    public float leanSmoothness = 4f;
    public float bobAmount = 0.02f;
    public float bobSpeed = 10f;
    public float fovBase = 75f;
    public float fovMaxBoost = 12f;
    public float fovSmoothness = 3f;

    [Header("DEBUG (solo lectura, para diagnosticar)")]
    public float debugYaw;
    public float debugPitch;
    public bool debugIsOrbiting;

    private float yaw;
    private float pitch;
    private float yawVelocity;
    private float pitchVelocity;

    private float releaseTimer;
    private Vector3 posVelocity;

    private float currentLean;
    private float bobTimer;

    private Camera cam;

    void Start() {
        cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = fovBase;
        pitch = 15f;
        yaw = 0f;
    }

    void Update() {
        if (Mouse.current == null) return;

        bool orbiting = Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed;
        debugIsOrbiting = orbiting;

        if (orbiting) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            releaseTimer = 0f;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            float targetYawInput = yaw + mouseDelta.x * mouseSensitivity;
            targetYawInput = Mathf.Clamp(targetYawInput, minHorizontalAngle, maxHorizontalAngle);

            float targetPitchInput = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, minVerticalAngle, maxVerticalAngle);

            yaw = Mathf.SmoothDampAngle(yaw, targetYawInput, ref yawVelocity, mouseSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitchInput, ref pitchVelocity, mouseSmoothTime);
        } else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            releaseTimer += Time.deltaTime;
            if (releaseTimer >= returnDelay) {
                yaw = Mathf.SmoothDampAngle(yaw, 0f, ref yawVelocity, returnSmoothTime);
                pitch = Mathf.SmoothDamp(pitch, 15f, ref pitchVelocity, returnSmoothTime);
            }
        }

        // Red de seguridad: nunca permite que se salga de rango
        yaw = Mathf.Clamp(yaw, minHorizontalAngle, maxHorizontalAngle);
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        debugYaw = yaw;
        debugPitch = pitch;
    }

    void LateUpdate() {
        if (target == null) return;

        Quaternion mouseOffset = Quaternion.Euler(pitch, yaw, 0f);
        Quaternion finalRotation = target.rotation * mouseOffset;

        Vector3 offset = finalRotation * new Vector3(0f, 0f, -distance);
        Vector3 desiredPosition = target.position + offset + Vector3.up * height;

        float speedRatio = 0f;
        if (car != null) {
            speedRatio = Mathf.Clamp01(Mathf.Abs(car.CurrentMove) / (car.speed * Time.deltaTime + 0.0001f));
            float targetLean = -car.CurrentTurn * leanAmount;
            currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSmoothness);
        }

        bobTimer += Time.deltaTime * bobSpeed * speedRatio;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmount * speedRatio;
        desiredPosition += Vector3.up * bobOffset;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref posVelocity, positionSmoothTime);

        Quaternion lookRotation = Quaternion.LookRotation(
            (target.position + Vector3.up * lookHeight) - transform.position
        );
        Quaternion targetRotation = Quaternion.Euler(lookRotation.eulerAngles.x, lookRotation.eulerAngles.y, currentLean);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);

        if (cam != null) {
            float targetFov = fovBase + fovMaxBoost * speedRatio;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * fovSmoothness);
        }
    }
}