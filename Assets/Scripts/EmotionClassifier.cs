using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public struct EmotionDefinition
{
    public string emotionName;
    [Tooltip("VAD coordinate (-1 ~ 1)")]
    public Vector3 vadCentroid;
}

public class EmotionClassifier : MonoBehaviour
{
    public static EmotionClassifier instance;

    [Header("Configuration")]
    public List<EmotionDefinition> emotionDefinitions = new List<EmotionDefinition>();

    //[SerializeField] private List<EmotionScore> emotionRanking;

    [Tooltip("Select UpdateEmotion() from context menu to test")]
    [SerializeField] Vector3 testingVAD = Vector3.zero;

    [System.Serializable]
    public struct EmotionScore
    {
        public string emotion;
        [Range(0, 1)] public float probability;
    }
    public List<EmotionScore> emotionRanking;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Debug.LogWarning("Duplicate EmotionClassifier found and destroyed.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Pass current VAD, return emotion name
    /// </summary>
    public string Classify(Vector3 currentVad)
    {
        if (emotionDefinitions == null || emotionDefinitions.Count == 0) return "Unknown";

        int count = emotionDefinitions.Count;
        float[] logits = new float[count];

        // Calculate points
        for (int i = 0; i < count; i++)
        {
            float distance = Vector3.Distance(currentVad, emotionDefinitions[i].vadCentroid);

            // closer can get higher point
            logits[i] = 1.0f / (1.0f + distance);
        }

        // 2. Softmax
        float[] probabilities = Softmax(logits);

        emotionRanking.Clear();

        for (int i = 0; i < count; i++)
        {
            string eName = emotionDefinitions[i].emotionName;
            float prob = probabilities[i];

            emotionRanking.Add(new EmotionScore { emotion = eName, probability = prob });
        }

        // sort ascendingly
        emotionRanking.Sort((a, b) => b.probability.CompareTo(a.probability));
        //foreach (var emotion in emotionRanking)
        //{
        //    Debug.Log($"{emotion.emotion.ToString()}: {emotion.probability.ToString()}");
        //}
        // find the highest scores
        string maxEmotion = emotionRanking[0].emotion;
        // Debug.Log($"<color=green>NPC feels {currentEmotion} ({Time.time})</color>");

        return maxEmotion;
    }

    [ContextMenu("Test Update Emotion")]
    public void TestEmotionClassify()
    {
        string result = Classify(testingVAD);
        Debug.Log($"<color=green>Testing Result: {result}</color>");
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

    // ================== Initialization ==================

    [ContextMenu("Initialize Emotion")]
    public void ResetToStandardEmotions()
    {
        emotionDefinitions = new List<EmotionDefinition>()
        {
            // ==================== 0. Neutral ====================
            new EmotionDefinition { emotionName = "Neutral", vadCentroid = Vector3.zero },

            // ==================== 1. Joy | Positive, becoming excited, with high control ====================
            new EmotionDefinition { emotionName = "Serenity",  vadCentroid = new Vector3(0.30f, -0.20f, 0.10f) }, // low intensity
            new EmotionDefinition { emotionName = "Joy",       vadCentroid = new Vector3(0.75f, 0.60f, 0.40f) },  // medium intensity
            new EmotionDefinition { emotionName = "Ecstasy",   vadCentroid = new Vector3(0.95f, 0.90f, 0.60f) },  // high intensity

            // ==================== 2. Trust | Positive, calm, with feel secure ====================
            new EmotionDefinition { emotionName = "Acceptance",vadCentroid = new Vector3(0.40f, -0.40f, 0.20f) }, // low intensity
            new EmotionDefinition { emotionName = "Trust",     vadCentroid = new Vector3(0.70f, -0.30f, 0.40f) }, // medium intensity
            new EmotionDefinition { emotionName = "Admiration",vadCentroid = new Vector3(0.80f, 0.20f, -0.30f) }, // high intensity

            // ==================== 3. Fear | Negative, agitated, extremely low control ====================
            new EmotionDefinition { emotionName = "Apprehension",vadCentroid=new Vector3(-0.30f, 0.30f, -0.20f) },// low intensity
            new EmotionDefinition { emotionName = "Fear",      vadCentroid = new Vector3(-0.64f, 0.60f, -0.43f) },// medium intensity
            new EmotionDefinition { emotionName = "Terror",    vadCentroid = new Vector3(-0.95f, 0.95f, -0.80f) },// high intensity

            // ==================== 4. Surprise | Neutral, extremely excited, out of control ====================
            new EmotionDefinition { emotionName = "Distraction",vadCentroid= new Vector3(0.00f, 0.40f, -0.10f) }, // low intensity
            new EmotionDefinition { emotionName = "Surprise",  vadCentroid = new Vector3(0.10f, 0.80f, -0.30f) }, // medium intensity
            new EmotionDefinition { emotionName = "Amazement", vadCentroid = new Vector3(0.20f, 0.95f, -0.50f) }, // high intensity

            // ==================== 5.Sadness | Negative, depressed, lost of control ====================
            new EmotionDefinition { emotionName = "Pensiveness",vadCentroid= new Vector3(-0.20f, -0.20f, -0.10f) },// low intensity
            new EmotionDefinition { emotionName = "Sadness",   vadCentroid = new Vector3(-0.60f, -0.30f, -0.20f) },// medium intensity
            new EmotionDefinition { emotionName = "Grief",     vadCentroid = new Vector3(-0.95f, -0.10f, -0.80f) },// high intensity

            // ==================== 6. Disgust | Negative, becoming excited, high control ====================
            new EmotionDefinition { emotionName = "Boredom",   vadCentroid = new Vector3(-0.65f, -0.62f, -0.33f) },// low intensity
            new EmotionDefinition { emotionName = "Disgust",   vadCentroid = new Vector3(-0.80f, 0.20f, 0.30f) },  // medium intensity
            new EmotionDefinition { emotionName = "Loathing",  vadCentroid = new Vector3(-0.95f, 0.60f, 0.60f) },  // high intensity

            // ==================== 7. Anger | Negative, extremely excited, highly controlling ====================
            new EmotionDefinition { emotionName = "Annoyance", vadCentroid = new Vector3(-0.25f, 0.30f, 0.10f) }, // low intensity
            new EmotionDefinition { emotionName = "Anger",     vadCentroid = new Vector3(-0.51f, 0.59f, 0.25f) }, // medium intensity
            new EmotionDefinition { emotionName = "Rage",      vadCentroid = new Vector3(-0.90f, 0.95f, 0.40f) }, // high intensity

            // ==================== 8. Anticipation | Positive, excited, in control of the situation ====================
            new EmotionDefinition { emotionName = "Interest",  vadCentroid = new Vector3(0.30f, 0.40f, 0.20f) },  // low intensity
            new EmotionDefinition { emotionName = "Anticipation",vadCentroid=new Vector3(0.50f, 0.70f, 0.40f) },  // medium intensity
            new EmotionDefinition { emotionName = "Vigilance", vadCentroid = new Vector3(0.60f, 0.90f, 0.60f) }   // high intensity
        };
    }
}