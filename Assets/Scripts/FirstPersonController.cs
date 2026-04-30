using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;

    private CharacterController _cc;
    private float _yawAngle;
    private float _pitchAngle;
    private float _verticalVelocity;
    private const float Gravity = -20f;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        gameObject.tag = "Player";
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsPlaying)
            return;

        HandleLook();
        HandleMove();
    }

    private void HandleLook()
    {
        Vector2 look = lookAction.action.ReadValue<Vector2>();
        _yawAngle += look.x * mouseSensitivity;
        _pitchAngle -= look.y * mouseSensitivity;
        _pitchAngle = Mathf.Clamp(_pitchAngle, -80f, 80f);
        transform.localEulerAngles = new Vector3(0f, _yawAngle, 0f);
        cameraTransform.localEulerAngles = new Vector3(_pitchAngle, 0f, 0f);
    }

    private void HandleMove()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 horizontal = Vector3.ClampMagnitude(
            transform.forward * input.y + transform.right * input.x, 1f) * moveSpeed;

        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        _verticalVelocity += Gravity * Time.deltaTime;

        _cc.Move((horizontal + Vector3.up * _verticalVelocity) * Time.deltaTime);
    }
}