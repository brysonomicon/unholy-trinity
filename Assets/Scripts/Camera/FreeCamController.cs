using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnholyTrinity.Camera
{
    /// Lives on the FreeCam object. CameraManager enables it only while free-cam is on,
    /// so it costs nothing the rest of the time.
    /// WASD = move across the map, E/Q = up/down, Shift = faster, hold right mouse = look.
    [RequireComponent(typeof(CinemachineCamera))]
    public class FreeCamController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMultiplier = 3f;
        [SerializeField] private float lookSensitivity = 0.1f;

        [Header("Input (bindings editable in Inspector)")]
        [SerializeField] private InputAction forwardAction =
            new("FreeCamForward", InputActionType.Button, "<Keyboard>/w");
        [SerializeField] private InputAction backAction =
            new("FreeCamBack", InputActionType.Button, "<Keyboard>/s");
        [SerializeField] private InputAction leftAction =
            new("FreeCamLeft", InputActionType.Button, "<Keyboard>/a");
        [SerializeField] private InputAction rightAction =
            new("FreeCamRight", InputActionType.Button, "<Keyboard>/d");
        [SerializeField] private InputAction upAction =
            new("FreeCamUp", InputActionType.Button, "<Keyboard>/e");
        [SerializeField] private InputAction downAction =
            new("FreeCamDown", InputActionType.Button, "<Keyboard>/q");
        [SerializeField] private InputAction fastAction =
            new("FreeCamFast", InputActionType.Button, "<Keyboard>/leftShift");
        [SerializeField] private InputAction lookHoldAction =
            new("FreeCamLookHold", InputActionType.Button, "<Mouse>/rightButton");
        [SerializeField] private InputAction lookAction =
            new("FreeCamLook", InputActionType.Value, "<Mouse>/delta");

        private float _yaw;
        private float _pitch;

        private void OnEnable()
        {
            // CameraManager sets the pose before enabling this, so read it here.
            Vector3 e = transform.eulerAngles;
            _yaw = e.y;
            _pitch = e.x > 180f ? e.x - 360f : e.x;

            forwardAction.Enable();
            backAction.Enable();
            leftAction.Enable();
            rightAction.Enable();
            upAction.Enable();
            downAction.Enable();
            fastAction.Enable();
            lookHoldAction.Enable();
            lookAction.Enable();
        }

        private void OnDisable()
        {
            forwardAction.Disable();
            backAction.Disable();
            leftAction.Disable();
            rightAction.Disable();
            upAction.Disable();
            downAction.Disable();
            fastAction.Disable();
            lookHoldAction.Disable();
            lookAction.Disable();
        }

        private void Update()
        {
            if (lookHoldAction.IsPressed())
            {
                Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;
                _yaw += look.x;
                _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            // Move across the map plane using only the yaw, so W always pans "forward"
            // across the board even when the camera is looking steeply downward.
            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);

            float x = Axis(rightAction, leftAction);
            float z = Axis(forwardAction, backAction);
            float y = Axis(upAction, downAction);

            Vector3 direction =
                yawRotation * Vector3.forward * z +
                yawRotation * Vector3.right * x +
                Vector3.up * y;

            float speed = moveSpeed * (fastAction.IsPressed() ? fastMultiplier : 1f);
            transform.position += direction * (speed * Time.unscaledDeltaTime);
        }

        private static float Axis(InputAction positive, InputAction negative) =>
            (positive.IsPressed() ? 1f : 0f) - (negative.IsPressed() ? 1f : 0f);
    }
}