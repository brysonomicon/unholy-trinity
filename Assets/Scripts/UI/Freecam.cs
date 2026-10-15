using UnityEngine;
using UnityEngine.InputSystem;

public class Freecam : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float fastMultiplier = 4f;
    public float lookSensitivity = 0.1f;

    private float _yaw;
    private float _pitch;

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // Hold right mouse to look around
        bool looking = mouse.rightButton.isPressed;
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !looking;

        if (looking)
        {
            Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
            _yaw += delta.x;
            _pitch = Mathf.Clamp(_pitch - delta.y, -89f, 89f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        // Scroll wheel adjusts base speed
        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f)
            moveSpeed = Mathf.Clamp(moveSpeed * (scroll > 0f ? 1.1f : 1f / 1.1f), 0.1f, 1000f);

        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move += transform.forward;
        if (keyboard.sKey.isPressed) move -= transform.forward;
        if (keyboard.dKey.isPressed) move += transform.right;
        if (keyboard.aKey.isPressed) move -= transform.right;
        if (keyboard.eKey.isPressed) move += Vector3.up;
        if (keyboard.qKey.isPressed) move -= Vector3.up;

        float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? fastMultiplier : 1f);
        transform.position += move.normalized * (speed * Time.deltaTime);
    }
}
