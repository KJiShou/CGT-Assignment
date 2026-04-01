using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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
            float v = 0.21f * extraversion + 0.59f * agreeableness + 0.19f * neuroticism;
            float a = 0.15f * openness + 0.30f * agreeableness - 0.57f * neuroticism;
            float d = 0.25f * openness + 0.17f * conscientiousness + 0.60f * extraversion - 0.32f * agreeableness;
            return new Vector3(v, a, d);
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

    // Emotion decaying
    void Update()
    {
        // If not talking to player, NPC emotion will gradually back to VAD(0, 0, 0) means Neutral
        // If talk to player again, NPC emotion will gradually back to longTermMood, means NPC will remember what player said
        if (isTalkWithPlayer)
        {
            Vector3 targetEmotion = longTermMood * historyInfluence;
            
            if (currentEmotion != targetEmotion)
            {
                currentEmotion = Vector3.MoveTowards(currentEmotion, targetEmotion, timeDecaySpeed * Time.deltaTime);

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
    }

    // For UI Script to call
    public void ReceivePlayerMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        Vector3 rawInput = TwitterSentimentVAD.instance.Analyze(message);
        Vector3 processedInput = ProcessInput(rawInput);
        Vector3 pFactors = personality.GetVAD();

        float vDelta = pFactors.x * processedInput.x;
        float aDelta = pFactors.y * processedInput.y;
        float dDelta = pFactors.z * processedInput.z;
        Vector3 currentDelta = new Vector3(vDelta, aDelta, dDelta);

        AddToHistory(currentDelta);
        RecalculateLongTermMood();

        currentEmotion += currentDelta;
        currentEmotion.x = Mathf.Clamp(currentEmotion.x, -1f, 1f);
        currentEmotion.y = Mathf.Clamp(currentEmotion.y, -1f, 1f);
        currentEmotion.z = Mathf.Clamp(currentEmotion.z, -1f, 1f);

        currentEmotionTag = EmotionClassifier.instance.Classify(currentEmotion);
        longTermMoodTag = EmotionClassifier.instance.Classify(longTermMood);

        OnEmotionProcessed?.Invoke(message, smoothVAD, currentTrend, rawInput);
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
    }

    [ContextMenu("Clear History")]
    public void ClearHistory()
    {
        emotionHistory.Clear();
        longTermMood = Vector3.zero;
        currentEmotion = Vector3.zero;
        playerInputHistoryVAD.Clear();
        smoothVAD = Vector3.zero;
    }
}
