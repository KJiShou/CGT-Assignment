using System;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class SelectCharacter : MonoBehaviour
{
    public static SelectCharacter instance;

    private Camera mainCamera;

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI npcName;
    [SerializeField] GameObject personalityChartPanel;
    [SerializeField] TextMeshProUGUI opennessValue;
    [SerializeField] TextMeshProUGUI conscientiousnessValue;
    [SerializeField] TextMeshProUGUI extraversionValue;
    [SerializeField] TextMeshProUGUI agreeablenessValue;
    [SerializeField] TextMeshProUGUI neuroticismValue;
    [SerializeField] GameObject toggleablePanel;
    [SerializeField] Slider currentEmotionVSlider;
    [SerializeField] Slider currentEmotionASlider;
    [SerializeField] Slider currentEmotionDSlider;
    [SerializeField] TextMeshProUGUI currentEmotionText;
    [SerializeField] Slider longTermMoodVSlider;
    [SerializeField] Slider longTermMoodASlider;
    [SerializeField] Slider longTermMoodDSlider;
    [SerializeField] TextMeshProUGUI longTermMoodText;

    [Header("Marker Settings")]
    [SerializeField] GameObject marker;
    [SerializeField] float makerElevation = 1f;

    [Header("Dependencies")]
    public ChatUIManager chatUIManager;
    public PersonalityRadarController personalityRadarController;
    public SliderController sliderController;

    [Header("UI Panels")]
    [SerializeField] GameObject emotionPanel;
    [SerializeField] GameObject probabilityPanel;
    [SerializeField] GameObject personalityPanel;

    [Header("Tooltip Trigger")]
    [SerializeField] TooltipTrigger opennessTooltipTrigger;    
    [SerializeField] TooltipTrigger conscientiousnessTooltipTrigger;    
    [SerializeField] TooltipTrigger extraversionTooltipTrigger;    
    [SerializeField] TooltipTrigger agreeablenessTooltipTrigger;    
    [SerializeField] TooltipTrigger neuroticismTooltipTrigger;

    [Header("Face Camera")]
    [SerializeField] GameObject faceCameraImage;

    [Header("Raycast Settings")]
    [Tooltip("Layer that allow to click")]
    public LayerMask clickableLayers;

    // Internal Status
    private GameObject currentSelectedNPC;
    private NPCDoubleDecay npcState;
    public bool clickingUI { get; private set; } = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);
    }

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        UnsetMarker();
        if (toggleablePanel != null) toggleablePanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Prevent pass through UI
            if (EventSystem.current.IsPointerOverGameObject())
            {
                clickingUI = true;
                
                return;
            }

            // convert mouse position from bottom-left based to top-left based
            Vector2 guiMousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            if (npcState != null && npcState.currentPanelRect.width > 0)
            {
                if (npcState.currentPanelRect.Contains(guiMousePos))
                {
                    clickingUI = true;
                    return;
                }
            }

            clickingUI = false;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            // Raycast distance 1000m, only collide with clickableLayers
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, clickableLayers))
            {
                GameObject hitObj = hit.transform.gameObject;

                // hit NPC
                if (hitObj.CompareTag("NPC"))
                {
                    // prevent repeatedly refresh UI
                    if (currentSelectedNPC == hitObj) return;

                    DeselectNPC();

                    currentSelectedNPC = hitObj;

                    // Use TryGetComponent for safety
                    if (currentSelectedNPC.TryGetComponent<NPCDoubleDecay>(out npcState))
                    {
                        chatUIManager.targetNPC = npcState;
                        personalityRadarController.npcSource = npcState;
                        npcState.isTalkWithPlayer = true;
                        sliderController.npcState = npcState;
                        npcState.controller.isSelecting = true;

                        //npcState.ShowNPCEmotion();

                        if (npcState.historyInputs.Count > 0)
                        {
                            foreach (var input in npcState.historyInputs)
                            {
                                input.SetActive(true);
                            }
                        }
                        npcState.showOnGUI = true;
                        UpdatePersonalityTooltipText();
                        UpdateNPCSettingsSliderValue();
                        UpdatePlayerSettingsSliderValue();
                        OpenNPCSetup();
                        SetMarker();
                        toggleablePanel.SetActive(true);
                    }
                }
                // Clicked other than NPC
                else
                {
                    DeselectNPC();
                }
            }
            // Clicked SkyBox
            else
            {
                DeselectNPC();
            }
        }

        UpdateUIData();
    }

    /// <summary>
    /// When selected NPC, update UI
    /// </summary>
    private void UpdateUIData()
    {
        if (npcState == null) return;

        npcName.text = $"Selected NPC: {currentSelectedNPC.name}";

        if (emotionPanel.activeSelf)
        {
            currentEmotionVSlider.value = npcState.currentEmotion.x;
            currentEmotionASlider.value = npcState.currentEmotion.y;
            currentEmotionDSlider.value = npcState.currentEmotion.z;
            longTermMoodVSlider.value = npcState.longTermMood.x;
            longTermMoodASlider.value = npcState.longTermMood.y;
            longTermMoodDSlider.value = npcState.longTermMood.z;
            
            currentEmotionText.text = $"Tag: {npcState.currentEmotionTag}";
            longTermMoodText.text = $"Tag: {npcState.longTermMoodTag}";
        }

        opennessValue.text = $"Openness\n({npcState.personality.openness:F2})";
        conscientiousnessValue.text = $"Conscientiousness\n({npcState.personality.conscientiousness:F2})";
        extraversionValue.text = $"Extraversion\n({npcState.personality.extraversion:F2})";
        agreeablenessValue.text = $"Agreeableness\n({npcState.personality.agreeableness:F2})";
        neuroticismValue.text = $"Neuroticism\n({npcState.personality.neuroticism:F2})";
    }

    /// <summary>
    /// Process deselect logic
    /// </summary>
    private void DeselectNPC()
    {
        if (currentSelectedNPC == null) return;

        npcName.text = $"Selected NPC: ";

        if (npcState.historyInputs.Count > 0)
        {
            foreach (var input in npcState.historyInputs)
            {
                input.SetActive(false);
            }
        }

        npcState.showOnGUI = false;
        npcState.onGuiIsExpanded = false;

        npcState.controller.isSelecting = false;
        npcState.controller.resetAnim = true;

        opennessTooltipTrigger.targetNPC = null;
        conscientiousnessTooltipTrigger.targetNPC = null;
        extraversionTooltipTrigger.targetNPC = null;
        agreeablenessTooltipTrigger.targetNPC = null;
        neuroticismTooltipTrigger.targetNPC = null;

        npcState.isTalkWithPlayer = false;

        if (npcState != null && npcState.faceCamera != null)
        {
            faceCameraImage.SetActive(false);
            npcState.faceCamera.gameObject.SetActive(false);
        }

        if (npcState.topLight != null)
        {
            npcState.topLight.SetActive(false);
        }

        chatUIManager.targetNPC = null;
        personalityRadarController.npcSource = null;
        currentSelectedNPC = null;
        npcState = null;
        sliderController.npcState = null;

        UnsetMarker();
        toggleablePanel.SetActive(false);
    }

    private void SetMarker()
    {
        if (currentSelectedNPC != null && marker != null)
        {
            marker.transform.SetParent(currentSelectedNPC.transform);
            marker.transform.localPosition = Vector3.up * makerElevation;
            marker.SetActive(true);
        }
    }

    private void UnsetMarker()
    {
        if (marker != null)
        {
            marker.SetActive(false);
            marker.transform.SetParent(null);
        }
    }

    private void UpdateNPCSettingsSliderValue()
    {
        if (npcState == null || sliderController == null) return;

        sliderController.memoryForgetRateSlider.value = npcState.memoryForgetRate;
        sliderController.historyInfluenceSlider.value = npcState.historyInfluence;
        sliderController.maxEmotionHistorySlider.value = npcState.maxEmotionHistory;
        sliderController.timeDecaySpeedSlider.value = npcState.timeDecaySpeed;
        sliderController.opennessSlider.value = npcState.personality.openness;
        sliderController.conscientiousnessSlider.value = npcState.personality.conscientiousness;
        sliderController.extraversionSlider.value = npcState.personality.extraversion;
        sliderController.agreeablenessSlider.value = npcState.personality.agreeableness;
        sliderController.neuroticismSlider.value = npcState.personality.neuroticism;
    }

    private void UpdatePlayerSettingsSliderValue()
    {
        if (npcState == null || sliderController == null) return;

        sliderController.smoothingFactorSlider.value = npcState.smoothingFactor;
        sliderController.emotionTrendThresholdSlider.value = npcState.playerEmotionTrendThreshold;
        sliderController.maxInputHistorySlider.value = npcState.maxPlayerInputHistory;
    }

    private void UpdatePersonalityTooltipText()
    {
        if(npcState == null) return;

        opennessTooltipTrigger.targetNPC = npcState;
        conscientiousnessTooltipTrigger.targetNPC = npcState;
        extraversionTooltipTrigger.targetNPC = npcState;
        agreeablenessTooltipTrigger.targetNPC = npcState;
        neuroticismTooltipTrigger.targetNPC = npcState;
    }

    private void OpenNPCSetup()
    {
        if (npcState == null) return;
        if (npcState.faceCamera != null)
        {
            faceCameraImage.SetActive(true);
            npcState.faceCamera.gameObject.SetActive(true);
        }

        if(npcState.topLight != null)
        {
            npcState.topLight.SetActive(true);
        }
    }
}