using UnityEngine;

public class girodeelice : MonoBehaviour
{
    [Header("Giro")]
    [Tooltip("Eje LOCAL de la hélice alrededor del cual gira.")]
    public Vector3 spinAxis = Vector3.forward;
    [Tooltip("Grados por segundo cuando el kart está parado.")]
    public float idleSpeed = 300f;
    [Tooltip("Grados por segundo extra cuando el kart va a velocidad máxima.")]
    public float extraSpeedAtMax = 1500f;
    public bool invert = false;

    [Header("Opcional: ligar al kart")]
    [Tooltip("Arrastra aquí el kart (el objeto con controlkart7).")]
    public controlkart7 kart;

    private Quaternion initRot;
    private float angle;

    void Start()
    {
        initRot = transform.localRotation;
    }

    void Update()
    {
        float speed = idleSpeed;

        if (kart != null && Time.deltaTime > 0f)
        {
            float kartSpeed = Mathf.Abs(kart.CurrentMove / Time.deltaTime);
            float ratio = Mathf.Clamp01(kartSpeed / kart.speed);
            speed += extraSpeedAtMax * ratio;
        }

        angle += speed * (invert ? -1f : 1f) * Time.deltaTime;
        angle = Mathf.Repeat(angle, 360f);

        transform.localRotation = initRot * Quaternion.AngleAxis(angle, spinAxis.normalized);
    }
}