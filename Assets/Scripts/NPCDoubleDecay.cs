using UnityEngine;
using System.Collections.Generic;

public class NPCDoubleDecay : MonoBehaviour
{
    // ================== 1. 定义记忆结构 ==================
    [System.Serializable]
    public struct EmotionMemory
    {
        public Vector3 vadImpact; // 当时受到的情绪冲击 (Delta)
        public float timestamp;   // 发生的时间

        public EmotionMemory(Vector3 vad, float time)
        {
            vadImpact = vad;
            timestamp = time;
        }
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
    [Tooltip("记忆遗忘速度：值越大，忘得越快。0.1表示记得很牢，2.0表示金鱼记忆")]
    public float memoryForgetRate = 0.5f;

    [Tooltip("历史记忆对当前情绪的权重 (0~1)。0.5表示历史积淀占一半影响")]
    [Range(0f, 1f)] public float historyInfluence = 0.3f;

    [Tooltip("最多保留多少条记忆 (防止内存无限增长)")]
    public int maxMemoryCount = 50;

    // 存储历史记录的列表
    [SerializeField] // 加上这个是为了能在 Inspector 里看到记忆列表，方便调试
    private List<EmotionMemory> emotionHistory = new List<EmotionMemory>();

    [Header("Runtime State")]
    [Tooltip("由历史记录计算出的'长期心情基调'")]
    public Vector3 longTermMood = Vector3.zero;

    [Tooltip("当前实时情绪")]
    public Vector3 currentEmotion = Vector3.zero;

    [Header("Decay Settings")]
    [Range(0f, 5f)] public float timeDecaySpeed = 0.1f;

    public bool isTalkWithPlayer = false;

    // 使用 Update 进行实时衰减
    void Update()
    {
        // ================== 核心修改：衰减逻辑 ==================
        // 以前是衰减到 Vector3.zero (绝对中性)
        // 现在是衰减到 longTermMood (长期基调)
        // 含义：如果我很恨你(LongTerm是负的)，即使我不生气了，我也会回到"恨"的状态，而不是"平静"

        if (isTalkWithPlayer)
        {
            Vector3 targetEmotion = longTermMood * historyInfluence;

            if (currentEmotion != targetEmotion)
            {
                currentEmotion = Vector3.MoveTowards(currentEmotion, targetEmotion, timeDecaySpeed * Time.deltaTime);

                // 钳制
                currentEmotion.x = Mathf.Clamp(currentEmotion.x, -1f, 1f);
                currentEmotion.y = Mathf.Clamp(currentEmotion.y, -1f, 1f);
                currentEmotion.z = Mathf.Clamp(currentEmotion.z, -1f, 1f);
            }
        } else
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

    [ContextMenu("Analyze Player Dialog")]
    public void AnalyzePlayerDialog(string input)
    {
        // 1. 获取输入 VAD
        Vector3 rawInput = TwitterSentimentVAD.instance.Analyze(input);

        // 假设你有 DialogContextAnalyzer，没有的话就直接用 rawInput
        Vector3 processedInput = DialogContextAnalyzer.instance.ProcessInput(rawInput);
        //Vector3 processedInput = rawInput;

        // 2. 获取性格系数
        Vector3 pFactors = personality.GetVAD();

        // 3. 计算本次冲击 (Delta)
        float vDelta = pFactors.x * processedInput.x;
        float aDelta = pFactors.y * processedInput.y;
        float dDelta = pFactors.z * processedInput.z;
        Vector3 currentDelta = new Vector3(vDelta, aDelta, dDelta);

        // ================== 4. 记录历史 (Memory System) ==================
        AddToHistory(currentDelta);

        // ================== 5. 重新计算长期基调 ==================
        RecalculateLongTermMood();

        // 6. 叠加到当前情绪
        // 这里我们把"本次冲击"加上去
        currentEmotion += currentDelta;

        // 7. 钳制
        currentEmotion.x = Mathf.Clamp(currentEmotion.x, -1f, 1f);
        currentEmotion.y = Mathf.Clamp(currentEmotion.y, -1f, 1f);
        currentEmotion.z = Mathf.Clamp(currentEmotion.z, -1f, 1f);

        string currentTag = EmotionClassifier.instance.Classify(currentEmotion);
        string longTermTag = EmotionClassifier.instance.Classify(longTermMood);

        Debug.Log($"<color=white>NPC Emotion Updated | Current: {currentEmotion} ({currentTag}) | LongTerm Mood: {longTermMood} ({longTermTag})</color>");
    }

    // ================== 历史记录核心算法 ==================

    /// <summary>
    /// 添加记忆并修剪旧数据
    /// </summary>
    void AddToHistory(Vector3 delta)
    {
        // 添加新记录
        emotionHistory.Add(new EmotionMemory(delta, Time.time));

        // 限制数量 (移除最老的)
        if (emotionHistory.Count > maxMemoryCount)
        {
            emotionHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// 计算历史综合情绪 (带遗忘机制)
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

        // 遍历所有历史记录
        for (int i = 0; i < emotionHistory.Count; i++)
        {
            EmotionMemory memory = emotionHistory[i];
            float timeElapsed = currentTime - memory.timestamp;

            // === 遗忘算法 ===
            // 权重公式: Weight = 1 / (1 + (时间间隔 * 遗忘速率))
            // 刚发生时: 1 / (1+0) = 1.0 (100% 影响)
            // 很久以后: 1 / (1+大数) ≈ 0.0 (几乎无影响)
            float weight = 1.0f / (1.0f + (timeElapsed * memoryForgetRate));

            // 累加
            weightedSum += memory.vadImpact * weight;
            totalWeight += weight;
        }

        // 计算加权平均值
        if (totalWeight > 0)
        {
            longTermMood = weightedSum / totalWeight;
        }
        else
        {
            longTermMood = Vector3.zero;
        }

        // 限制一下长期情绪的范围，防止积压过大
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
    }
}
