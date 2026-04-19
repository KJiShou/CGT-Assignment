using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class EmotionDefinitionUI : MonoBehaviour
{
    public static EmotionDefinitionUI instance;

    [SerializeField] InputAction action;
    [SerializeField] GameObject emotionClassifierPanel;
    [SerializeField] GameObject inputForm;
    [SerializeField] bool _isOpen = false;
    public bool isOpen
    {
        get { return _isOpen; }
        private set { _isOpen = value; }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        action.Enable();
        action.performed += OnActionKeyPressed;
    }

    private void OnDisable()
    {
        action.performed -= OnActionKeyPressed;
        action.Disable();
    }

    private void OnActionKeyPressed(InputAction.CallbackContext context)
    {
        if (inputForm != null && !inputForm.activeSelf)
        {
            isOpen = !isOpen;
            emotionClassifierPanel.SetActive(isOpen);
        }
    }
}