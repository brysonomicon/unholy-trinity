using UnityEngine;
using UnityEngine.InputSystem;

/// Put on the Main Camera.
/// P1 turn -> hovers over P1, P2 turn -> hovers over P2,
/// resolution -> pulls back to frame both.
/// Optional: swoop to a close third-person view of the active player
/// (Tab to toggle, or automatically after a delay). Resets every phase.
[RequireComponent(typeof(Camera))]
public class CameraHandler : MonoBehaviour
{
    public TurnManager turns;

    [Header("Top-Down Hover")]
    [Range(40f, 90f)] public float pitch = 65f;
    public float distance = 14f;
    public float fov = 40f;

    [Header("Resolution")]
    [Tooltip("Frames the midpoint; pulls back further as the players separate.")]
    public float spreadFactor = 1.1f;

    [Header("Optional: Close Third-Person Swoop")]
    public bool allowSwoop = true;
    public bool autoSwoopIn = false;
    public float autoSwoopDelay = 1f;
    public float tpDistance = 4f;
    public float tpHeight = 1.8f;
    public float tpShoulder = 0.6f;
    public float tpLookHeight = 1.4f;
    public float tpLookAhead = 4f;
    public float tpFov = 65f;

    [Header("Smoothing")]
    public float sharpness = 5f;

    [Header("Input")]
    [SerializeField] InputAction toggleSwoopAction =
        new InputAction("ToggleSwoop", InputActionType.Button, "<Keyboard>/tab");

    public bool Swooped { get; private set; }

    Camera cam;
    float phaseTime;

    void Awake() => cam = GetComponent<Camera>();

    void OnEnable()
    {
        turns.PhaseChanged += OnPhaseChanged;
        toggleSwoopAction.performed += OnToggleSwoop;
        toggleSwoopAction.Enable();
    }

    void OnDisable()
    {
        if (turns != null) turns.PhaseChanged -= OnPhaseChanged;
        toggleSwoopAction.performed -= OnToggleSwoop;
        toggleSwoopAction.Disable();
    }

    void Start()
    {
        ComputePose(out var p, out var r, out var f);
        transform.SetPositionAndRotation(p, r);
        cam.fieldOfView = f;
    }

    void OnToggleSwoop(InputAction.CallbackContext _) => ToggleSwoop();

    void OnPhaseChanged(TurnPhase _)
    {
        Swooped = false;
        phaseTime = 0f;
    }

    public void ToggleSwoop()
    {
        if (allowSwoop && turns.Current != null) Swooped = !Swooped;
    }

    void LateUpdate()
    {
        phaseTime += Time.deltaTime;
        if (allowSwoop && autoSwoopIn && !Swooped && turns.Current != null
            && phaseTime >= autoSwoopDelay)
            Swooped = true;

        ComputePose(out var pos, out var rot, out var f);
        float k = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, pos, k);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, k);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, f, k);
    }

    void ComputePose(out Vector3 pos, out Quaternion rot, out float f)
    {
        GameObject cur = turns.Current;

        if (cur == null)                       // resolution
        {
            Vector3 a = turns.playerOne.transform.position;
            Vector3 b = turns.playerTwo.transform.position;
            float d = Mathf.Max(distance, Vector3.Distance(a, b) * spreadFactor);
            Hover((a + b) * 0.5f, d, out pos, out rot);
            f = fov;
        }
        else if (Swooped)                      // optional close third-person
        {
            ThirdPerson(cur, turns.Other(cur), out pos, out rot);
            f = tpFov;
        }
        else                                   // hover over active player
        {
            Hover(cur.transform.position, distance, out pos, out rot);
            f = fov;
        }
    }

    void Hover(Vector3 focus, float dist, out Vector3 pos, out Quaternion rot)
    {
        rot = Quaternion.Euler(pitch, 0f, 0f);
        pos = focus - rot * Vector3.forward * dist;
    }

    void ThirdPerson(GameObject who, GameObject opponent, out Vector3 pos, out Quaternion rot)
    {
        Vector3 p = who.transform.position;
        Vector3 fwd = opponent.transform.position - p;
        fwd.y = 0f;
        fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : who.transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        pos = p - fwd * tpDistance + Vector3.up * tpHeight + right * tpShoulder;
        Vector3 look = p + fwd * tpLookAhead + Vector3.up * tpLookHeight;
        rot = Quaternion.LookRotation(look - pos, Vector3.up);
    }
}