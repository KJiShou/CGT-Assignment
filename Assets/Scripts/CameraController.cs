using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    Vector2 moveInput = Vector2.zero;
    [SerializeField] float moveSpeed = 2.5f;
    [SerializeField] float rotationSpeed = 1f;
    float rotationAngle;
    [SerializeField] CinemachineCamera cmCamera;
    float targetFOV;
    float zoomInput;
    [SerializeField] float minFOV = 35f;
    [SerializeField] float maxFOV = 90f;
    [SerializeField] float zoomSensitivity = 5f;

    public void OnInputCameraMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnInputRotateCamera(InputAction.CallbackContext context)
    {
        rotationAngle = context.ReadValue<float>();
    }

    public void OnInputZoomCamera(InputAction.CallbackContext context)
    {
        zoomInput = context.ReadValue<float>();
    }

    private void Start()
    {
        targetFOV = cmCamera.Lens.FieldOfView;
    }

    private void Update()
    {
        if (SelectCharacter.instance.clickingUI) return;
        CameraMove();
        RotateCamera();
        Zoom();
    }

    private void Zoom()
    {
        targetFOV += -(zoomInput) * zoomSensitivity;
        targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        cmCamera.Lens.FieldOfView = targetFOV;
    }

    private void RotateCamera()
    {
        transform.Rotate(0, rotationAngle * rotationSpeed * Time.deltaTime, 0, Space.World);
    }

    private void CameraMove()
    {
        Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;

        moveDir.y = 0f;

        if (moveDir != Vector3.zero)
        {
            transform.position += moveDir.normalized * moveSpeed * Time.deltaTime;
        }
    }
}
