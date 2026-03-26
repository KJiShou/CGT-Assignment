using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class SelectCharacter : MonoBehaviour
{
    public static SelectCharacter instance;

    private Camera mainCamera;

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI npcName;
    [SerializeField] GameObject personalityChartPanel;
    [SerializeField] TextMeshProUGUI opennessValue;
    [SerializeField] TextMeshProUGUI consientiousnessValue;
    [SerializeField] TextMeshProUGUI extraversionValue;
    [SerializeField] TextMeshProUGUI agreeablenessValue;
    [SerializeField] TextMeshProUGUI neuroticismValue;
    [SerializeField] GameObject toggleablePanel;
    [SerializeField] TextMeshProUGUI currentEmotionVAD;
    [SerializeField] TextMeshProUGUI currentEmotionText;
    [SerializeField] TextMeshProUGUI longTermMoodVAD;
    [SerializeField] TextMeshProUGUI longTermMoodText;

    [Header("Marker Settings")]
    [SerializeField] GameObject marker;
    [SerializeField] float makerElevation = 1f;

    [Header("Dependencies")]
    public ChatUIManager chatUIManager;
    public PersonalityRadarController personalityRadarController;

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
            clickingUI = false;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObj = hit.transform.gameObject;

                // hit NPC
                if (hitObj.CompareTag("NPC"))
                {
                    // prevent repeatedly refresh UI
                    if (currentSelectedNPC == hitObj) return;

                    // ClosePrevNPCMessage();

                    currentSelectedNPC = hitObj;

                    // Use TryGetComponent for safety
                    if (currentSelectedNPC.TryGetComponent<NPCDoubleDecay>(out npcState))
                    {
                        chatUIManager.targetNPC = npcState;
                        personalityRadarController.npcSource = npcState;

                        if (npcState.historyInputs.Count > 0)
                        {
                            foreach (var input in npcState.historyInputs)
                            {
                                input.SetActive(true);
                            }
                        }

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

        opennessValue.text = $"Openness\n({npcState.personality.openness:F2})";
        consientiousnessValue.text = $"Consientiousness\n({npcState.personality.conscientiousness:F2})";
        extraversionValue.text = $"Extraversion\n({npcState.personality.extraversion:F2})";
        agreeablenessValue.text = $"Agreeableness\n({npcState.personality.agreeableness:F2})";
        neuroticismValue.text = $"Neuroticism\n({npcState.personality.neuroticism:F2})";

        currentEmotionVAD.text = $"Valence: {npcState.currentEmotion.x:F2},\nArousal: {npcState.currentEmotion.y:F2}\nDominance: {npcState.currentEmotion.z:F2}";
        longTermMoodVAD.text = $"Valence: {npcState.longTermMood.x:F2},\nArousal: {npcState.longTermMood.y:F2}\nDominance: {npcState.longTermMood.z:F2}";

        currentEmotionText.text = $"Tag: {npcState.currentEmotionTag}";
        longTermMoodText.text = $"Tag: {npcState.longTermMoodTag}";
    }

    /// <summary>
    /// Process deselect logic
    /// </summary>
    private void DeselectNPC()
    {
        if (currentSelectedNPC == null) return;

        if (npcState.historyInputs.Count > 0)
        {
            foreach (var input in npcState.historyInputs)
            {
                input.SetActive(false);
            }
        }

        chatUIManager.targetNPC = null;
        personalityRadarController.npcSource = null;
        currentSelectedNPC = null;
        npcState = null;

        UnsetMarker();
        toggleablePanel.SetActive(false);
    }

    //private void ClosePrevNPCMessage()
    //{
    //    if (npcState == null) { return; }

    //    if (npcState.historyInputs.Count > 0)
    //    {
    //        foreach (var input in npcState.historyInputs)
    //        {
    //            input.SetActive(false);
    //        }
    //    }
    //}

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
}