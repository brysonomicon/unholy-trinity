using UnityEngine;
using UnityEngine.InputSystem;


public class TitleParallax : MonoBehaviour
{
    const float MaxInputMagnitude = 1f;
    const float SwayOff = 0f;
    const float SwayOn = 1f;
    const float Roll = 0f;

    // Separate Perlin rows so each axis wanders independently.
    const float SwayNoiseRowX = 0f;
    const float SwayNoiseRowY = 100f;

    [SerializeField] Vector2 maxAngle = new(3f, 5f); // pitch, yaw
    [SerializeField] float smoothTime = 0.25f;

    [Header("Idle Sway")]
    [SerializeField] float idleDelay = 3f;          // seconds without input before swaying
    [SerializeField] float swayBlendIn = 1.5f;      // how slowly sway takes over
    [SerializeField] float swayBlendOut = 0.2f;     // how quickly input takes back control
    [SerializeField, Range(0f, 1f)] float swayAmount = 0.5f; // fraction of maxAngle
    [SerializeField] float swaySpeed = 0.15f;
    [SerializeField] float stickDeadzone = 0.15f;

    Vector2 current, velocity;
    float lastInputTime = float.NegativeInfinity; // start idle so the title sways on load
    float swayWeight, swayWeightVelocity;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;

        Vector2 input = ReadInput(out bool hadInput);
        if (hadInput)
        {
            lastInputTime = now;
        }

        bool idle = now - lastInputTime > idleDelay;
        swayWeight = Mathf.SmoothDamp(swayWeight, idle ? SwayOn : SwayOff, ref swayWeightVelocity,
            idle ? swayBlendIn : swayBlendOut, Mathf.Infinity, dt);

        Vector2 sway = new Vector2(Noise(now, SwayNoiseRowX), Noise(now, SwayNoiseRowY)) * swayAmount;
        Vector2 target = Vector2.Lerp(input, sway, swayWeight);

        current = Vector2.SmoothDamp(current, target, ref velocity, smoothTime, Mathf.Infinity, dt);
        transform.localRotation = Quaternion.Euler(-current.y * maxAngle.x, current.x * maxAngle.y, Roll);
    }

    Vector2 ReadInput(out bool hadInput)
    {
        Vector2 n = Vector2.zero;
        hadInput = false;

        if (Mouse.current != null)
        {
            Vector2 p = Mouse.current.position.ReadValue();
            n = new(ToSignedRange(p.x / Screen.width), ToSignedRange(p.y / Screen.height));
            if (Mouse.current.delta.ReadValue() != Vector2.zero)
            {
                hadInput = true;
            }
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            if (stick.magnitude > stickDeadzone)
            {
                n += stick;
                hadInput = true;
            }
        }

        return Vector2.ClampMagnitude(n, MaxInputMagnitude);
    }

    float Noise(float time, float row) => ToSignedRange(Mathf.PerlinNoise(time * swaySpeed, row));

    // Remaps 0..1 to -1..1.
    static float ToSignedRange(float unit) => Mathf.LerpUnclamped(-1f, 1f, unit);
}
