using UnityEngine;
using UnityEngine.InputSystem;

public class WheelInput : MonoBehaviour
{
    public InputActionReference steerAction;
    public InputActionReference throttleAction;
    public InputActionReference brakeAction;
    public InputActionReference reverseAction;   // pedal del embrague = retroceso

    public float rampSpeed = 20f;
    public float steerSmooth = 12f;
    public float steerDeadzone = 0.02f;
    public float steerGain = 2.5f;
    public float steerCurve = 1f;

    public float Steer { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; }
    public float Reverse { get; private set; }

    void OnEnable()
    {
        steerAction.action.Enable();
        throttleAction.action.Enable();
        brakeAction.action.Enable();
        if (reverseAction != null) reverseAction.action.Enable();
    }

    void Update()
    {
        float raw = steerAction.action.ReadValue<float>();
        float sign = Mathf.Sign(raw);
        float mag = Mathf.Abs(raw);

        mag = mag < steerDeadzone ? 0f : (mag - steerDeadzone) / (1f - steerDeadzone);
        mag = Mathf.Clamp01(mag * steerGain);
        float target = sign * Mathf.Pow(mag, steerCurve);

        Steer = Mathf.Lerp(Steer, target, steerSmooth * Time.deltaTime);

        float throttleTarget = throttleAction.action.ReadValue<float>();
        float brakeTarget = brakeAction.action.ReadValue<float>();
        float reverseTarget = reverseAction != null ? reverseAction.action.ReadValue<float>() : 0f;

        Throttle = Mathf.MoveTowards(Throttle, throttleTarget, rampSpeed * Time.deltaTime);
        Brake = Mathf.MoveTowards(Brake, brakeTarget, rampSpeed * Time.deltaTime);
        Reverse = Mathf.MoveTowards(Reverse, reverseTarget, rampSpeed * Time.deltaTime);
    }
}