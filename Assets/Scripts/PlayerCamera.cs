using System.Diagnostics;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Vector3 offset;
    public float minAngle = -20f;
    public float maxAngle = 35f;

    public bool constraintX = false;
    public float xAngleRange = 120f;
    public float constraintMiddle = 0f;

    public bool isCinematic;
    Quaternion targetRotation;
    Vector2 turn;
    public Camera cam { get; private set; }

    public float targetFOV = 60;
    public float currentFOV
    {
        get => cam.fieldOfView;
    }
    [SerializeField] Transform xRotator;
    [SerializeField] Transform yRotator;
    [SerializeField] Transform Movement;
    [SerializeField] Transform RotationBase;

    [SerializeField] float fovSpeed = 3;

    [Range(0f, 1f)]
    public float cameraSmoothness = 0.5f;
    public float sensitivity;

    [Range(0f, 1f)]
    public float movementSmoothness = 0.5f;

    private void Start()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
            UnityEngine.Debug.Log("Camera not found");
        else
            targetFOV = cam.fieldOfView;
        ReadRotation();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void SetViewRotation(Vector2 rotation)
    {
        if (xRotator == yRotator)
        {
            xRotator.localRotation = Quaternion.Euler(-rotation.y, rotation.x, 0);
        }
        else
        {
            xRotator.localRotation = Quaternion.Euler(-rotation.y, 0, 0);
            yRotator.localRotation = Quaternion.Euler(0, rotation.x, 0);
        }
    }
    public void ReadRotation()
    {
        turn.x = yRotator.rotation.eulerAngles.y;
        turn.y = -yRotator.rotation.eulerAngles.x;

        if (turn.y < -90)
            turn.y += 360;
        if (turn.y > 90)
            turn.y -= 360;
    }
    Vector2 mouseDelta;
    private void Update()
    {
        mouseDelta.x = Input.GetAxis("Mouse X");
        mouseDelta.y = Input.GetAxis("Mouse Y");


        cam.fieldOfView = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovSpeed);
    }

    private void LateUpdate()
    {
        if (isCinematic)
            return;

        // Accumulate mouse look
        turn.x += mouseDelta.x * sensitivity;
        turn.y += mouseDelta.y * sensitivity;

        turn.y = Mathf.Clamp(turn.y, minAngle, maxAngle);

        if (constraintX)
        {
            float dev = Mathf.DeltaAngle(constraintMiddle, turn.x);
            dev = Mathf.Clamp(dev, -xAngleRange, xAngleRange);
            turn.x = constraintMiddle + dev;
        }

        Quaternion baseRotation = RotationBase.rotation;

        Quaternion yaw = Quaternion.AngleAxis(turn.x, baseRotation * Vector3.up);

        Quaternion pitch = Quaternion.AngleAxis(-turn.y, yaw * baseRotation * Vector3.right);

        Quaternion targetRotation = pitch * yaw * baseRotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            cameraSmoothness);
    }

    private void FixedUpdate()
    {

        transform.position = Vector3.Lerp(transform.position, Movement.position + offset, movementSmoothness);
    }

    public void ChangeFOV(float FOV)
    {
        if (cam == null) return;
        targetFOV = FOV;
    }
}
