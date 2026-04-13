using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEditor.VersionControl.Message;

public class NPCDoubleDecay : MonoBehaviour
{
    /// <summary>
    /// Action<Player input, Latest emotion VAD, current emotion trend>
    /// </summary>
    public event Action<string, Vector3, string, Vector3> OnEmotionProcessed;

    [System.Serializable]
    public class EmotionMemory
    {
        public Vector3 vadImpact; // VAD Delta
        public float timestamp;
        // public GameObject messagePrefab;

        public EmotionMemory(Vector3 vad, float time)
        {
            vadImpact = vad;
            timestamp = time;
            // messagePrefab = message;
        }

        //public void ClearPrefab()
        //{
        //    Destroy(messagePrefab);
        //    messagePrefab = null;
        //}
    }

    [System.Serializable]
    public struct Personality
    {
        [Header("Big Five Traits (OCEAN)")]
        [Range(-1f, 1f)] public float openness;
        [Range(-1f, 1f)] public float conscientiousness;
        [Range(-1f, 1f)] public float extraversion;
        [Range(-1f, 1f)] public float agreeableness;
        [Range(-1f, 1f)] public float neuroticism;

        public Vector3 GetVAD()
        {
            float v = 0.21f * extraversion + 0.59f * agreeableness - 0.19f * neuroticism;
            float a = 0.15f * openness + 0.30f * agreeableness + 0.57f * neuroticism;
            float d = 0.25f * openness + 0.17f * conscientiousness + 0.60f * extraversion - 0.32f * agreeableness;

            return new Vector3(
                Mathf.Clamp(v, -1f, 1f),
                Mathf.Clamp(a, -1f, 1f),
                Mathf.Clamp(d, -1f, 1f)
            );
        }

        /// <summary>
        /// Obtain Emotional Sensitivity/Filter Multiplier
        /// Determines how much an NPC reacts to the same player input.
        /// </summary>
        public Vector3 GetReactivityMultiplier(Vector3 inputVAD)
        {
            // Base sensitivity is 1
            Vector3 reactivity = Vector3.one;

            // Valence
            if (inputVAD.x > 0)
            {
                // If players input positive emotions, extroverted and agreeable individuals will amplify that happiness.
                reactivity.x += extraversion * 0.4f + agreeableness * 0.2f;
            }
            else
            {
                // If a player inputs negative emotions, highly neurotic individuals will amplify that pain/anger to an extreme.
                reactivity.x += neuroticism * 0.6f;
                // People with a high degree of conscientiousness are better able to withstand negative emotions
                reactivity.x -= conscientiousness * 0.3f;
            }

            // Arousal
            if (inputVAD.y > 0)
            {
                // Neuroticism greatly amplifies intense emotional fluctuations.
                reactivity.y += neuroticism * 0.5f;
            }

            // Limit the minimum sensitivity to prevent NPCs from becoming completely emotionless (0.2x ~ 2.5x reaction).
            reactivity.x = Mathf.Clamp(reactivity.x, 0.2f, 2.5f);
            reactivity.y = Mathf.Clamp(reactivity.y, 0.2f, 2.5f);
            reactivity.z = Mathf.Clamp(reactivity.z, 0.2f, 2.5f);

            return reactivity;
        }
    }

    public Personality personality;

    [Header("Memory Settings")]
    [Tooltip("History emotions decay rate：higher value forget faster")]
    public float memoryForgetRate = 0.5f;
    [Tooltip("History memory influence rate (0~1)。higher means emotion will more affected by history emotion")]
    [Range(0f, 1f)] public float historyInfluence = 0.3f;
    [Tooltip("Maximum number of keeping emotion data")]
    public int maxEmotionHistory = 50;
    public List<EmotionMemory> emotionHistory { get; private set; } = new List<EmotionMemory>();

    [Header("Runtime State")]
    [Tooltip("Calculate by history emotions 'Long-Term mood tone'")]
    public Vector3 longTermMood = Vector3.zero;
    public string longTermMoodTag = "Neutral";
    [Tooltip("Current emotion value")]
    public Vector3 currentEmotion = Vector3.zero;
    public string currentEmotionTag = "Neutral";
    public string prevEmotionTag = "Neutral";
    public Vector3 finalEmotion = Vector3.zero;
    public string finalEmotionTag = "Neutral";

    [Header("Decay Settings")]
    [Range(0f, 5f)] public float timeDecaySpeed = 0.1f;
    
    [Header("Player Input Settings")]
    [Tooltip("Weight of new VAD value (0~1). 0.3 means history VAD proportion 70%，new VAD proportion 30%")]
    [Range(0.1f, 1f)] public float smoothingFactor = 0.3f;
    [Tooltip("Smooth context VAD value")]
    public Vector3 smoothVAD = Vector3.zero;
    [Tooltip("Only over this threshold, then consider this is player's emotion trend")]
    [Range(0f, 1f)]
    public float playerEmotionTrendThreshold = 0.15f;
    public int maxPlayerInputHistory = 50;
    [Tooltip("Player message history VAD")]
    public Queue<Vector3> playerInputHistoryVAD = new Queue<Vector3>();
    public string currentTrend = "Stable"; // player input's emotion trend
    public bool isTalkWithPlayer = false;

    public List<GameObject> historyInputs = new List<GameObject>();

    [Header("Camera Setup")]
    public Camera faceCamera;

    [Header("Light Setup")]
    public GameObject topLight;

    [HideInInspector]
    public AIController controller;

    [HideInInspector]
    [System.Serializable]
    public struct EmotionScore
    {
        public string emotion;
        [Range(0, 1)] public float probability;
    }
    public List<EmotionScore> emotionRanking;
    public List<EmotionScore> longTermEmotionRanking;

    private void Start()
    {
        controller = GetComponent<AIController>();
    }

    // Emotion decaying
    void Update()
    {
        // If not talking to player, NPC emotion will gradually back to VAD(0, 0, 0) means Neutral
        // If talk to player again, NPC emotion will gradually back to longTermMood, means NPC will remember what player said
        if (isTalkWithPlayer)
        {
            if (currentEmotion != finalEmotion)
            {
                currentEmotion = Vector3.MoveTowards(currentEmotion, finalEmotion, timeDecaySpeed * Time.deltaTime);

                currentEmotion.x = Mathf.Clamp(currentEmotion.x, -1f, 1f);
                currentEmotion.y = Mathf.Clamp(currentEmotion.y, -1f, 1f);
                currentEmotion.z = Mathf.Clamp(currentEmotion.z, -1f, 1f);
            }
        } 
        else
        {
            if (currentEmotion != Vector3.zero)
            {
                currentEmotion = Vector3.MoveTowards(currentEmotion, new Vector3(0, 0, 0), timeDecaySpeed * Time.deltaTime);

                currentEmotion.x = Mathf.Clamp(currentEmotion.x, -1f, 1f);
                currentEmotion.y = Mathf.Clamp(currentEmotion.y, -1f, 1f);
                currentEmotion.z = Mathf.Clamp(currentEmotion.z, -1f, 1f);
            }
        }

        string newlyClassifiedTag = Classify(currentEmotion);

        if (currentEmotionTag != newlyClassifiedTag)
        {
            prevEmotionTag = currentEmotionTag;
            currentEmotionTag = newlyClassifiedTag;

            ShowNPCEmotion();
        }
    }

    /// <summary>
    /// Classify VAD into emotion probabilities and store to outputList
    /// </summary>
    private void ClassifyToList(Vector3 vad, List<EmotionScore> outputList)
    {
        if (EmotionClassifier.instance.emotionDefinitions == null || EmotionClassifier.instance.emotionDefinitions.Count == 0) return;

        int count = EmotionClassifier.instance.emotionDefinitions.Count;
        float[] logits = new float[count];

        // Calculate points
        for (int i = 0; i < count; i++)
        {
            float distance = Vector3.Distance(vad, EmotionClassifier.instance.emotionDefinitions[i].vadCentroid);
            logits[i] = 1.0f / (1.0f + distance);
        }

        // Softmax
        float[] probabilities = Softmax(logits);

        outputList.Clear();

        for (int i = 0; i < count; i++)
        {
            string eName = EmotionClassifier.instance.emotionDefinitions[i].emotionName;
            float prob = probabilities[i];
            outputList.Add(new EmotionScore { emotion = eName, probability = prob });
        }

        // sort descending by probability
        outputList.Sort((a, b) => b.probability.CompareTo(a.probability));
    }

    private string Classify(Vector3 currentVad)
    {
        ClassifyToList(currentVad, emotionRanking);
        return emotionRanking.Count > 0 ? emotionRanking[0].emotion : "Unknown";
    }

    private float[] Softmax(float[] logits)
    {
        float[] probs = new float[logits.Length];
        float maxLogit = logits.Max();
        float sumExp = 0f;

        for (int i = 0; i < logits.Length; i++)
        {
            probs[i] = Mathf.Exp(logits[i] - maxLogit);
            sumExp += probs[i];
        }

        for (int i = 0; i < probs.Length; i++)
        {
            probs[i] /= sumExp;
        }
        return probs;
    }

    // For UI Script to call
    public void ReceivePlayerMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        Vector3 rawInput = TwitterSentimentVAD.instance.Analyze(message);
        Vector3 processedInput = ProcessInput(rawInput);

        Vector3 sensitivity = personality.GetReactivityMultiplier(processedInput);

        float vDelta = processedInput.x * sensitivity.x;
        float aDelta = processedInput.y * sensitivity.y;
        float dDelta = processedInput.z * sensitivity.z;

        Vector3 currentDelta = new Vector3(vDelta, aDelta, dDelta);

        AddToHistory(currentDelta);
        RecalculateLongTermMood();

        currentEmotion += currentDelta;
        currentEmotion.x = Mathf.Clamp(currentEmotion.x, -1f, 1f);
        currentEmotion.y = Mathf.Clamp(currentEmotion.y, -1f, 1f);
        currentEmotion.z = Mathf.Clamp(currentEmotion.z, -1f, 1f);

        if (emotionHistory.Count > 3)
        {
            finalEmotion = Vector3.Lerp(currentEmotion, longTermMood, historyInfluence);
        }
        else
        {
            finalEmotion = currentEmotion;
        }

        finalEmotionTag = Classify(finalEmotion);
        longTermMoodTag = Classify(longTermMood);

        OnEmotionProcessed?.Invoke(message, smoothVAD, currentTrend, rawInput);
    }

    [ContextMenu("ShowNPCEmotion")]
    public void ShowNPCEmotion()
    {    
        EmotionClassifier.instance.emotionDefinitions.ForEach(e =>
        {
            if (e.emotionName.Equals(currentEmotionTag))
            { 
                controller.animTrigger = currentEmotionTag;
                //controller.animTrigger = "Serenity";
            }
        });
    }

    /// <summary>
    /// Pass current input VAD, return contextual VAD
    /// </summary>
    private Vector3 ProcessInput(Vector3 currentRawVAD)
    {
        AnalyzeTrend(currentRawVAD);

        // Calculate EMA (Exponential Moving Average) - "Contextual VAD"
        if (playerInputHistoryVAD.Count == 0)
        {
            smoothVAD = currentRawVAD; // for first input
        }
        else
        {
            // New = Current * alpha + Old * (1 - alpha)
            smoothVAD = Vector3.Lerp(smoothVAD, currentRawVAD, smoothingFactor);
        }

        playerInputHistoryVAD.Enqueue(currentRawVAD);
        int excessCount = playerInputHistoryVAD.Count - maxPlayerInputHistory;

        if (excessCount > 0)
        {
            for (int i = 0; i < excessCount; i++)
            {
                playerInputHistoryVAD.Dequeue();

                if (historyInputs[i] != null)
                {
                    Destroy(historyInputs[i]);
                }
            }
            
            historyInputs.RemoveRange(0, excessCount);
        }

        // if (playerInputHistoryVAD.Count > maxPlayerInputHistory) playerInputHistoryVAD.Dequeue();

        Debug.Log($"<color=orange>History Player's Dialog Smooth Transition : {smoothVAD} | Trend: {currentTrend}</color>");

        return smoothVAD;
    }

    private void AnalyzeTrend(Vector3 current)
    {
        if (playerInputHistoryVAD.Count == 0) return;

        // Calculate average
        Vector3 pastAverage = Vector3.zero;
        foreach (var h in playerInputHistoryVAD)
        {
            pastAverage += h;
        }
        pastAverage /= playerInputHistoryVAD.Count;

        // Use current VAD compare average VAD to get delta
        float deltaV = current.x - pastAverage.x;
        float deltaA = current.y - pastAverage.y;
        float deltaD = current.z - pastAverage.z;

        // 1. Arousal Increase
        if (deltaA > playerEmotionTrendThreshold)
        {
            if (deltaV < -playerEmotionTrendThreshold) currentTrend = "Escalating Anger";
            else if (deltaV > playerEmotionTrendThreshold) currentTrend = "Getting Excited";
            else currentTrend = "Becoming Alert";
        }
        // 2. Arousal Decrease
        else if (deltaA < -playerEmotionTrendThreshold)
        {
            if (deltaV < -playerEmotionTrendThreshold) currentTrend = "Becoming Depressed";
            else currentTrend = "Calming Down";
        }
        // 3. Arousal Not too much difference
        else
        {
            if (deltaV > playerEmotionTrendThreshold) currentTrend = "Warming Up";
            else if (deltaV < -playerEmotionTrendThreshold) currentTrend = "Turning Cold";
            else currentTrend = "Stable";
        }
    }

    /// <summary>
    /// Add NPC emotion history and remove over capacity memory
    /// </summary>
    void AddToHistory(Vector3 delta)
    {
        // emotionHistory.Add(new EmotionMemory(delta, Time.time, messagePrefab));
        emotionHistory.Add(new EmotionMemory(delta, Time.time));

        int excessCount = emotionHistory.Count - maxEmotionHistory;

        if (excessCount > 0)
        {
            emotionHistory.RemoveRange(0, excessCount);
        }
    }

    /// <summary>
    /// Calculate historical comprehensive sentiment (with forgetting mechanism)
    /// </summary>
    void RecalculateLongTermMood()
    {
        if (emotionHistory.Count == 0)
        {
            longTermMood = Vector3.zero;
            longTermMoodTag = "Neutral";
            longTermEmotionRanking?.Clear();
            return;
        }

        Vector3 weightedSum = Vector3.zero;
        float totalWeight = 0f;
        float currentTime = Time.time;

        for (int i = 0; i < emotionHistory.Count; i++)
        {
            EmotionMemory memory = emotionHistory[i];
            float timeElapsed = currentTime - memory.timestamp;

            // === decay algorithm ===
            // Weight = 1 / (1 + (time interval * decay rate))
            // Just happen: 1 / (1+0) = 1.0 (100% affect)
            // Happen long time: 1 / (1+<Any Number>) ≈ 0.0 (almost no affect)
            float weight = 1.0f / (1.0f + (timeElapsed * memoryForgetRate));

            weightedSum += memory.vadImpact * weight;
            totalWeight += weight;
        }

        // Calculate the weighted average
        if (totalWeight > 0)
        {
            longTermMood = weightedSum / totalWeight;
        }
        else
        {
            longTermMood = Vector3.zero;
        }

        longTermMood.x = Mathf.Clamp(longTermMood.x, -1f, 1f);
        longTermMood.y = Mathf.Clamp(longTermMood.y, -1f, 1f);
        longTermMood.z = Mathf.Clamp(longTermMood.z, -1f, 1f);

        // Compute long-term emotion ranking
        ClassifyToList(longTermMood, longTermEmotionRanking);
        longTermMoodTag = longTermEmotionRanking.Count > 0 ? longTermEmotionRanking[0].emotion : "Neutral";
    }

    [ContextMenu("Clear Emotion History")]
    public void ClearEmotionHistory()
    {
        emotionHistory.Clear();
        longTermMood = Vector3.zero;
        longTermMoodTag = "Neutral";
        longTermEmotionRanking?.Clear();
        currentEmotion = Vector3.zero;
        finalEmotion = Vector3.zero;
        finalEmotionTag = "Neutral";
    }

    [ContextMenu("Clear Player Input History")]
    public void ClearPlayerInputHistory()
    {
        foreach (var item in historyInputs)
        {
            Destroy(item);
        }

        historyInputs.Clear();
        smoothVAD = Vector3.zero;
        playerInputHistoryVAD.Clear();
        currentTrend = "Stable";
    }

    // ================== OnGUI Real-Time Emotion Probability Ranking ==================

    [Header("GUI Settings")]
    [Tooltip("Show emotion ranking?")]
    public bool showOnGUI = false;
    [Tooltip("Show top N of emotion")]
    public int displayTopN = 5;

    private Texture2D barTexture;
    private GUIStyle labelStyle;
    //private GUIStyle buttonStyle;

    public bool onGuiIsExpanded = false;

    public float posX = 0f; // Offset from right edge (negative = left, positive = right)
    public float posY = 90f;

    [HideInInspector]
    public Rect currentPanelRect = new Rect(0, 0, 0, 0);
    private void OnGUI()
    {
        // No open or no emotion ranking data then skip
        if (SettingsController.instance.isOpen || !showOnGUI || emotionRanking == null || emotionRanking.Count == 0) return;

        // Init painter and texture
        if (barTexture == null)
        {
            barTexture = new Texture2D(1, 1);
            barTexture.SetPixel(0, 0, Color.white);
            barTexture.Apply();

            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 14;
            labelStyle.fontStyle = FontStyle.Bold;
        }

        // define panel area
        int panelWidth = 300;
        int rowHeight = 25;
        int sectionGap = 8;
        int labelHeight = 20;
        int displayCount = onGuiIsExpanded ? Mathf.Min(displayTopN, emotionRanking.Count) : 1;
        int longTermDisplayCount = onGuiIsExpanded ? (longTermEmotionRanking != null ? Mathf.Min(displayTopN, longTermEmotionRanking.Count) : 0) : 1;

        // When collapsed: title + button + 2 rows (current + longterm top 1 each)
        // When expanded: title + button + header + current list + header + longterm list
        int collapsedHeight = 40 + (4 * rowHeight) + sectionGap;
        int expandedHeight = 40 + sectionGap + labelHeight + (displayCount * rowHeight) + sectionGap + labelHeight + (longTermDisplayCount * rowHeight) + sectionGap;
        int panelHeight = onGuiIsExpanded ? expandedHeight : collapsedHeight;

        float rightBaseX = Screen.width - 340f; // 300 panel + 40 margin
        float panelY = (Screen.height * 0.2f) + posY;
        currentPanelRect = new Rect(rightBaseX + posX, panelY, panelWidth, panelHeight);

        // half transparent black background
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(currentPanelRect, barTexture);
        GUI.color = Color.white;

        // Title
        GUI.Label(new Rect((currentPanelRect.x + 10f), (panelY + 8f), 200, 20), "Emotion Ranking", labelStyle);

        string buttonText = onGuiIsExpanded ? "Collapse" : "Expand";
        if (GUI.Button(new Rect(currentPanelRect.x + 220f, panelY + 5f, 75f, 20f), buttonText))
        {
            onGuiIsExpanded = !onGuiIsExpanded;
        }

        float yOffset = 40;

        // ========== Current Emotion Section ==========
        GUI.Label(new Rect(currentPanelRect.x + 10f, panelY + yOffset, 200, labelHeight), "Current:", labelStyle);
        yOffset += labelHeight;

        for (int i = 0; i < displayCount; i++)
        {
            EmotionScore score = emotionRanking[i];
            float yPos = (yOffset + (i * rowHeight)) + panelY;

            // Emotion name
            GUI.Label(new Rect((currentPanelRect.x + 20), yPos, 100, 20), score.emotion, labelStyle);

            // Percentage
            string percentText = (score.probability * 100f).ToString("F1") + "%";
            GUI.Label(new Rect((currentPanelRect.x + 200), yPos, 50, 20), percentText, labelStyle);

            // Dynamic progress bar
            float maxBarWidth = 90f;
            float currentBarWidth = maxBarWidth * score.probability;
            Rect barRect = new Rect((currentPanelRect.x + 150), yPos + 5, currentBarWidth, 10);

            if (i == 0) GUI.color = Color.green;
            else if (i == 1) GUI.color = Color.yellow;
            else GUI.color = Color.gray;

            GUI.DrawTexture(barRect, barTexture);
            GUI.color = Color.white;
        }

        yOffset += (displayCount * rowHeight) + sectionGap;

        // ========== Long Term Mood Section ==========
        GUI.Label(new Rect(currentPanelRect.x + 10f, panelY + yOffset, 200, labelHeight), "Long Term:", labelStyle);
        yOffset += labelHeight;

        if (longTermEmotionRanking != null && longTermEmotionRanking.Count > 0)
        {
            for (int i = 0; i < longTermDisplayCount; i++)
            {
                EmotionScore score = longTermEmotionRanking[i];
                float yPos = (yOffset + (i * rowHeight)) + panelY;

                // Emotion name
                GUI.Label(new Rect((currentPanelRect.x + 20), yPos, 100, 20), score.emotion, labelStyle);

                // Percentage
                string percentText = (score.probability * 100f).ToString("F1") + "%";
                GUI.Label(new Rect((currentPanelRect.x + 200), yPos, 50, 20), percentText, labelStyle);

                // Dynamic progress bar
                float maxBarWidth = 90f;
                float currentBarWidth = maxBarWidth * score.probability;
                Rect barRect = new Rect((currentPanelRect.x + 150), yPos + 5, currentBarWidth, 10);

                if (i == 0) GUI.color = Color.cyan;
                else if (i == 1) GUI.color = Color.magenta;
                else GUI.color = Color.gray;

                GUI.DrawTexture(barRect, barTexture);
                GUI.color = Color.white;
            }
        }
        else
        {
            // No long-term data yet
            float yPos = (yOffset) + panelY;
            GUI.Label(new Rect((currentPanelRect.x + 20), yPos, 150, 20), "No data", labelStyle);
        }
    }
}
