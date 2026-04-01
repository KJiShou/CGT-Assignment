using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    Vector2 moveInput = Vector2.zero;
    public float moveSpeed = 2.5f;
    public float rotationSpeed = 1f;
    float rotationAngle;
    public CinemachineCamera cmCamera;
    float zoomInput;

    [Header("Perspective Settings")]
    public float minFOV = 35f;
    public float maxFOV = 90f;
    public float fovZoomSensitivity = 5f;
    float targetFOV;

    [Header("Orthographic Settings")]
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 15f;
    public float orthoZoomSensitivity = 0.5f;
    private float targetOrthoSize;

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
        targetOrthoSize = cmCamera.Lens.OrthographicSize;
    }

    private void Update()
    {
        if (SelectCharacter.instance.clickingUI || SettingsController.instance.isOpen) return;
        CameraMove();
        RotateCamera();
        Zoom();
    }

    private void Zoom()
    {
        if (zoomInput == 0f) return;

        if (cmCamera.Lens.Orthographic)
        {
            // Modify OrthographicSize
            targetOrthoSize += -(zoomInput) * orthoZoomSensitivity;
            targetOrthoSize = Mathf.Clamp(targetOrthoSize, minOrthoSize, maxOrthoSize);
            cmCamera.Lens.OrthographicSize = targetOrthoSize;
        }
        else
        {
            // Modify FieldOfView
            targetFOV += -(zoomInput) * fovZoomSensitivity;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
            cmCamera.Lens.FieldOfView = targetFOV;
        }
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
