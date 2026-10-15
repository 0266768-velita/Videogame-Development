
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Orbit")]
    [SerializeField] private float distance = 6f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 65f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.05f;

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private LayerMask collisionLayers = ~0;

    private float yaw;
    private float pitch = 20f;
    private Vector3 positionVelocity;

    private InputAction lookAction;
    private InputAction zoomAction;

    private void Awake()
    {
        lookAction = new InputAction(
            name: "Look",
            type: InputActionType.Value,
            binding: "<Mouse>/delta"
        );

        zoomAction = new InputAction(
            name: "Zoom",
            type: InputActionType.Value,
            binding: "<Mouse>/scroll"
        );
    }

    private void OnEnable()
    {
        lookAction.Enable();
        zoomAction.Enable();
    }

    private void OnDisable()
    {
        lookAction.Disable();
        zoomAction.Disable();
    }

    private void OnDestroy()
    {
        lookAction.Dispose();
        zoomAction.Dispose();
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("OrbitCamera: Assign a target in the Inspector.");
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
    }

    private void LateUpdate()
    {
        RotateCamera();
        ZoomCamera();
        FollowTarget();
    }

    private void RotateCamera()
    {
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();

        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minVerticalAngle,
            maxVerticalAngle
        );
    }

    private void ZoomCamera()
    {
        float scroll = zoomAction.ReadValue<Vector2>().y;

        distance -= scroll * 0.01f;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void FollowTarget()
    {
        Vector3 focusPoint = target.position + targetOffset;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 desiredPosition =
            focusPoint - rotation * Vector3.forward * distance;

        Vector3 direction = (desiredPosition - focusPoint).normalized;
        float adjustedDistance = distance;

        if (Physics.SphereCast(
            focusPoint,
            collisionRadius,
            direction,
            out RaycastHit hit,
            distance,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            adjustedDistance = Mathf.Max(
                minDistance * 0.5f,
                hit.distance - collisionRadius
            );
        }

        desiredPosition =
            focusPoint - rotation * Vector3.forward * adjustedDistance;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        transform.rotation = rotation;
    }
}
