using UnityEngine;

public class PersonalityRadarController : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Automatically assign in SelectCharacter")]
    public NPCDoubleDecay npcSource;

    [Header("Chart Components")]
    [Tooltip("Child GameObject with UIRadarPolygonRenderer Scripts")]
    public UIRadarPolygonRenderer polygonRenderer;

    [Tooltip("Clamp value to -1 ~ 1")]
    public bool clampValuesAtRuntime = true;

    [Header("Performance")]
    [Tooltip("Allow to run in Update? run in Update more consume performance")]
    public bool autoUpdateInUpdate = true;

    private float[] currentPersonalityArray = new float[5];

    // Use for same value checking, prevent unnecessary draw
    private Vector3 lastOCEAN_1; // store O, C, E
    private Vector2 lastOCEAN_2; // store A, N

    void Update()
    {
        if (autoUpdateInUpdate)
        {
            RequestChartUpdate();
        }
    }

    /// <summary>
    /// Manual request to update Five Dimension Diagram
    /// </summary>
    [ContextMenu("Manual Request Update")]
    public void RequestChartUpdate()
    {
        if (npcSource == null || polygonRenderer == null) return;

        NPCDoubleDecay.Personality p = npcSource.personality;

        // data unchanged, not drawing
        Vector3 currentOCEAN_1 = new Vector3(p.openness, p.conscientiousness, p.extraversion);
        Vector2 currentOCEAN_2 = new Vector2(p.agreeableness, p.neuroticism);

        if (currentOCEAN_1 == lastOCEAN_1 && currentOCEAN_2 == lastOCEAN_2) return;

        lastOCEAN_1 = currentOCEAN_1;
        lastOCEAN_2 = currentOCEAN_2;

        // Follow OCEAN sequence
        currentPersonalityArray[0] = p.openness;
        currentPersonalityArray[1] = p.conscientiousness;
        currentPersonalityArray[2] = p.extraversion;
        currentPersonalityArray[3] = p.agreeableness;
        currentPersonalityArray[4] = p.neuroticism;

        // Call update
        polygonRenderer.UpdateValues(currentPersonalityArray);
    }

    //void Start()
    //{
    //    if (npcSource == null)
    //    {
    //        npcSource = GetComponentInParent<NPCDoubleDecay>();
    //        if (npcSource == null)
    //        {
    //            npcSource = GetComponentInChildren<NPCDoubleDecay>();
    //        }
    //    }
    //}
}
