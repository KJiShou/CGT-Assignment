using System;
using TMPro;
using Unity.Cinemachine;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    public static SettingsController instance;

    [SerializeField] InputAction escapeAction;

    [SerializeField] GameObject settingsPanel;

    [SerializeField] bool _isOpen = false;
    public bool isOpen
    {
        get { return _isOpen; }
        private set { _isOpen = value; }
    }

    [SerializeField] TextMeshProUGUI cameraMoveSpeedText;
    [SerializeField] Slider cameraMoveSpeedSlider;
    [Space(10)]

    [SerializeField] TextMeshProUGUI cameraRotationSpeedText;
    [SerializeField] Slider cameraRotationSpeedSlider;
    [Space(10)]

    [SerializeField] TextMeshProUGUI cameraVisionText;
    [SerializeField] Slider cameraVisionSlider;
    [Space(10)]

    [Header("Perspective View")]
    // [SerializeField] Toggle perspectiveToggle;
    [SerializeField] GameObject perspectiveSettingsSection;
    [Space(10)]

    [SerializeField] TextMeshProUGUI cameraMinFOVText;
    [SerializeField] Slider cameraMinFOVSlider;
    [Space(10)]

    [SerializeField] TextMeshProUGUI cameraMaxFOVText;
    [SerializeField] Slider cameraMaxFOVSlider;
    [Space(10)]

    [SerializeField] TextMeshProUGUI cameraZoomSensitivityText;
    [SerializeField] Slider cameraZoomSensitivitySlider;
    [Space(10)]

    [Header("Orthographic View")]
    [SerializeField] GameObject orthographicSettingsSection;
    [Space(10)]

    [SerializeField] TextMeshProUGUI cameraMinOrthoSizeText;
    [SerializeField] Slider cameraMinOrthoSizeSlider;
    [Space(10)]

    [SerializeField] TextMeshProUGUI cameraMaxOrthoSizeText;
    [SerializeField] Slider cameraMaxOrthoSizeSlider;
    [Space(10)]

    [SerializeField] TextMeshProUGUI cameraOrthoZoomSensitivityText;
    [SerializeField] Slider cameraOrthoZoomSensitivitySlider;
    [Space(10)]

    [SerializeField] CameraController cameraController;
    [SerializeField] CameraVisionTrigger cameraVisionTrigger;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        cameraMoveSpeedSlider.value = cameraController.moveSpeed;
        cameraRotationSpeedSlider.value = cameraController.rotationSpeed;
        cameraVisionSlider.value = cameraVisionTrigger.visionDepth;

        // Perspective
        cameraMaxFOVSlider.value = cameraController.maxFOV;
        cameraMinFOVSlider.value = cameraController.minFOV;
        cameraZoomSensitivitySlider.value = cameraController.fovZoomSensitivity;

        // Orthographic
        cameraMaxOrthoSizeSlider.value = cameraController.maxOrthoSize;
        cameraMinOrthoSizeSlider.value = cameraController.minOrthoSize;
        cameraOrthoZoomSensitivitySlider.value = cameraController.orthoZoomSensitivity;
    }

    private void OnEnable()
    {
        escapeAction.Enable();
        escapeAction.performed += OnEscPressed;
    }

    private void OnDisable()
    {
        escapeAction.performed -= OnEscPressed;
        escapeAction.Disable();
    }

    private void OnEscPressed(InputAction.CallbackContext context)
    {
        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);
    }

    public void OnCameraMoveSpeedChange(float value)
    {
        if (cameraController == null) return;
        float v = ConvertFloatToInt(value);
        cameraMoveSpeedText.text = v.ToString();
        cameraController.moveSpeed = v;
    }

    public void OnCameraRotationSpeedChange(float value)
    {
        if (cameraController == null) return;
        float v = ConvertFloatToInt(value);
        cameraRotationSpeedText.text = v.ToString();
        cameraController.rotationSpeed = v;
    }

    public void OnCameraVisionChanged(float value)
    {
        if (cameraVisionTrigger == null) return;
        float v = ConvertFloatToInt(value);
        cameraVisionText.text = v.ToString();
        cameraVisionTrigger.visionDepth = v;
    }

    public void OnCameraMinFOVChange(float value)
    {
        if (cameraController == null) return;
        float v = ConvertFloatToInt(value);
        cameraMinFOVText.text = v.ToString();
        cameraController.minFOV = v;
        cameraMaxFOVSlider.minValue = v;
        cameraMaxFOVSlider.maxValue = v + 100;

        if (cameraMaxFOVSlider.value <= cameraMinFOVSlider.value)
        {
            cameraController.maxFOV = v;
        }
    }

    public void OnCameraMaxFOVChange(float value)
    {
        if (cameraController == null) return;
        float v = ConvertFloatToInt(value);
        cameraMaxFOVText.text = v.ToString();
        cameraController.maxFOV = v;
    }

    public void OnCameraZoomSensitivityChange(float value)
    {
        if (cameraController == null) return;
        float v = ConvertFloatToInt(value);
        cameraZoomSensitivityText.text = v.ToString();
        cameraController.fovZoomSensitivity = v;
    }

    public void OnCameraMinOrthoSizeChange(float value)
    {
        if (cameraController == null) return;
        float v = ConvertFloatToInt(value);
        cameraMinOrthoSizeText.text = v.ToString();
        cameraController.minOrthoSize = v;
        cameraMaxOrthoSizeSlider.minValue = v;
        cameraMaxOrthoSizeSlider.maxValue = v + 100;

        if (cameraMaxOrthoSizeSlider.value <= cameraMinOrthoSizeSlider.value)
        {
            cameraController.maxFOV = v;
        }
    }

    public void OnCameraMaxOrthoSizeChange(float value)
    {
        if (cameraController == null) return;
        float v = ConvertFloatToInt(value);
        cameraMaxOrthoSizeText.text = v.ToString();
        cameraController.maxOrthoSize = v;
    }

    public void OnOrthoCameraZoomSensitivityChange(float value)
    {
        if (cameraController == null) return;
        float v = ConvertFloatToInt(value);
        cameraOrthoZoomSensitivityText.text = v.ToString();
        cameraController.orthoZoomSensitivity = v;
    }

    public void OnPerspectiveToggleChanged(bool value)
    {
        if (value)
        {
            perspectiveSettingsSection.SetActive(true);
            var lens = cameraController.cmCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Perspective;
            cameraController.cmCamera.Lens = lens;
        }
        else
        {
            perspectiveSettingsSection.SetActive(false);
        }
    }

    public void OnOrthographicToggleChanged(bool value)
    {
        if (value)
        {
            orthographicSettingsSection.SetActive(true);
            var lens = cameraController.cmCamera.Lens;
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            cameraController.cmCamera.Lens = lens;
        }
        else
        {
            orthographicSettingsSection.SetActive(false);
        }
    }

    private float RoundToAnyFloatingPoint(float value, int numOfFloatingPoint)
    {
        return (float)Math.Round(value, numOfFloatingPoint);
    }

    private int ConvertFloatToInt(float value)
    {
        return (int)value;
    }

}
