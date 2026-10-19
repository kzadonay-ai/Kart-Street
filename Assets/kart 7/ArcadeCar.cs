using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArcadeCar : MonoBehaviour
{
    public WheelInput input;

    [Header("Motor")]
    public float acceleration = 45f;
    public float brakeForce = 50f;            // fuerza del freno
    public float reverseAcceleration = 20f;   // fuerza del retroceso
    public float reverseEngageSpeed = 2f;     // el retroceso solo actúa por debajo de esta velocidad
    public float maxSpeed = 40f;
    public float maxReverseSpeed = 12f;

    [Header("Giro")]
    public float maxSteer = 200f;             // grados por segundo
    public float minSpeedToSteer = 1f;
    public float highSpeedSteerFactor = 0.75f;
    public float grip = 6f;

    [Header("Estabilidad")]
    public float centerOfMassDrop = 0.25f;
    public float angularDamping = 2f;

    [Header("Alineación al piso")]
    public float groundAlignSpeed = 10f;
    public float airAlignSpeed = 2f;
    public float maxGroundAngle = 50f;

    [Header("Debug")]
    public bool showDebug = true;

    Rigidbody rb;
    BoxCollider col;
    float speed;
    float lastYaw;
    float measuredYawRate;
    bool grounded;
    static readonly RaycastHit[] hits = new RaycastHit[8];

    void Awake()
    {
        foreach (var old in GetComponentsInParent<contoroldeauto>())
        {
            if (old.enabled)
            {
                old.enabled = false;
                Debug.LogWarning($"ArcadeCar: desactivé 'contoroldeauto' en '{old.gameObject.name}'.");
            }
        }

        rb = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
        ApplyBodySettings();
    }

    void Start()
    {
        ApplyBodySettings();

        float yaw = transform.eulerAngles.y;
        rb.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = rb.rotation;
        lastYaw = yaw;
    }

    void ApplyBodySettings()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.angularDamping = angularDamping;
        rb.maxAngularVelocity = 20f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.automaticCenterOfMass = false;
        Vector3 center = col != null ? col.center : Vector3.zero;
        rb.centerOfMass = center + Vector3.down * centerOfMassDrop;

        if (col != null)
        {
            var mat = new PhysicsMaterial("CarroSinFriccion");
            mat.dynamicFriction = 0f;
            mat.staticFriction = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            mat.bounciness = 0f;
            col.material = mat;
        }
    }

    Vector3 GetGroundNormal()
    {
        grounded = false;
        if (col == null) return Vector3.up;

        Vector3 origin = col.bounds.center;
        float length = col.bounds.extents.y + 1.2f;
        int n = Physics.RaycastNonAlloc(origin, Vector3.down, hits, length, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        Vector3 normal = Vector3.up;
        for (int i = 0; i < n; i++)
        {
            var h = hits[i];
            if (h.collider == col || h.collider.transform.IsChildOf(transform)) continue;
            if (Vector3.Angle(h.normal, Vector3.up) > maxGroundAngle) continue;
            if (h.distance < best)
            {
                best = h.distance;
                normal = h.normal;
                grounded = true;
            }
        }
        return normal;
    }

    void FixedUpdate()
    {
        if (input == null) return;

        Vector3 vel = rb.linearVelocity;
        speed = Vector3.Dot(vel, transform.forward);

        // ACELERADOR: avanzar
        if (input.Throttle > 0f && speed < maxSpeed)
            rb.AddForce(transform.forward * input.Throttle * acceleration, ForceMode.Acceleration);

        // FRENO: solo frena. Se opone al movimiento (adelante o atrás) y se detiene en 0
        if (input.Brake > 0f)
        {
            float k = Mathf.Clamp01(Mathf.Abs(speed) / 1.5f);   // suave cerca de 0, sin temblor
            rb.AddForce(-transform.forward * Mathf.Sign(speed) * input.Brake * brakeForce * k,
                        ForceMode.Acceleration);
        }

        // RETROCESO (embrague): solo actúa si el carro está casi parado o ya va hacia atrás
        if (input.Reverse > 0f && speed < reverseEngageSpeed && speed > -maxReverseSpeed)
            rb.AddForce(-transform.forward * input.Reverse * reverseAcceleration, ForceMode.Acceleration);

        // Tope de velocidad en reversa
        if (speed < -maxReverseSpeed)
            rb.AddForce(transform.forward * reverseAcceleration, ForceMode.Acceleration);

        // Agarre lateral
        Vector3 lateral = Vector3.Project(vel, transform.right);
        rb.AddForce(-lateral * grip, ForceMode.Acceleration);

        // Giro
        float steerFactor = Mathf.Clamp(speed / minSpeedToSteer, -1f, 1f);
        float speed01 = Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed);
        float reduction = Mathf.Lerp(1f, highSpeedSteerFactor, speed01);
        float turnDegPerSec = input.Steer * maxSteer * steerFactor * reduction;

        Vector3 av = rb.angularVelocity;
        av.y = turnDegPerSec * Mathf.Deg2Rad;
        rb.angularVelocity = av;

        // Alineación al piso
        Vector3 normal = GetGroundNormal();
        float alignSpeed = grounded ? groundAlignSpeed : airAlignSpeed;
        Quaternion target = Quaternion.FromToRotation(transform.up, normal) * rb.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, alignSpeed * Time.fixedDeltaTime));

        // Medición del giro real (diagnóstico)
        float yaw = transform.eulerAngles.y;
        measuredYawRate = Mathf.DeltaAngle(lastYaw, yaw) / Time.fixedDeltaTime;
        lastYaw = yaw;
    }

    void OnGUI()
    {
        if (!showDebug || input == null) return;
        GUI.skin.label.fontSize = 36;
        GUI.Label(new Rect(10, 150, 1800, 50),
            $"Steer {input.Steer:F2}  Acel {input.Throttle:F2}  Freno {input.Brake:F2}  Retro {input.Reverse:F2}  Vel {speed:F1}");
        GUI.Label(new Rect(10, 200, 1800, 50),
            $"Giro real {measuredYawRate:F0} °/s   Inclin X {transform.eulerAngles.x:F0}  Z {transform.eulerAngles.z:F0}   Piso {(grounded ? "sí" : "no")}");
    }
}