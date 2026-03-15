using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public struct EmotionDefinition
{
    public string emotionName;
    [Tooltip("标准VAD坐标 (-1 到 1)")]
    public Vector3 vadCentroid;
}

public class EmotionClassifier : MonoBehaviour
{
    public static EmotionClassifier instance;

    [Header("Configuration")]
    public List<EmotionDefinition> emotionDefinitions = new List<EmotionDefinition>();

    [Header("Debug View")]
    [Tooltip("是否在控制台打印概率详情")]
    public bool showDebugLogs = false;
    [SerializeField] private string currentEmotion;
    [SerializeField] private List<EmotionScore> debugScores;
    public Dictionary<string, float> probabilityMap = new Dictionary<string, float>();

    [System.Serializable]
    public struct EmotionScore
    {
        public string emotion;
        [Range(0, 1)] public float probability;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(instance);
        }
    }

    /// <summary>
    /// 主函数：输入当前VAD，返回情绪名称
    /// (合并了之前的 Classify 和 PredictEmotion)
    /// </summary>
    public string Classify(Vector3 currentVad)
    {
        if (emotionDefinitions == null || emotionDefinitions.Count == 0) return "Unknown";

        int count = emotionDefinitions.Count;
        float[] logits = new float[count];

        // 1. 计算 Logits (得分)
        for (int i = 0; i < count; i++)
        {
            float distance = Vector3.Distance(currentVad, emotionDefinitions[i].vadCentroid);

            // 推荐数学公式: 1 / (1 + d)
            // 结果范围 (0, 1]。距离为0时得分为1，距离无穷大时得分为0。
            // 这种数值范围对 Softmax 非常友好。
            logits[i] = 1.0f / (1.0f + distance);
        }

        // 2. Softmax
        float[] probabilities = Softmax(logits);

        // =================================================
        // 核心修改：同时更新 Dictionary 和 Debug列表
        // =================================================
        // A. 清理旧数据
        probabilityMap.Clear();
        debugScores.Clear();


        for (int i = 0; i < count; i++)
        {
            string eName = emotionDefinitions[i].emotionName;
            float prob = probabilities[i];

            // B. 存入字典 (方便代码调用: probabilityMap["Joy"])
            probabilityMap.Add(eName, prob);

            // C. 存入列表 (方便 Inspector 查看)
            debugScores.Add(new EmotionScore { emotion = eName, probability = prob });
        }

        // 3. 排序 Debug 列表 (可选：让概率最高的排前面，看起来更爽)
        debugScores.Sort((a, b) => b.probability.CompareTo(a.probability));

        // 4. ArgMax (可以直接取列表的第一个，因为已经排好序了)
        string maxEmotion = debugScores[0].emotion;
        currentEmotion = debugScores[0].emotion;
        // Debug.Log($"<color=green>NPC feels {currentEmotion} ({Time.time})</color>");

        return maxEmotion;
    }

    // --- 测试入口 ---

    [ContextMenu("Test Update Emotion")] // 添加这个，让你能在 Inspector 右键测试
    public void UpdateEmotion()
    {
        // 模拟一个测试数据
        Vector3 testVAD = new Vector3(-0.5f, 0.7f, 0.2f);

        // 强制开启 Log 来看结果
        bool originalDebug = showDebugLogs;
        showDebugLogs = true;

        string result = Classify(testVAD);
        Debug.Log($"<color=green>最终判定结果: {result}</color>");

        showDebugLogs = originalDebug; // 还原设置
    }

    // ================== 数学核心实现 (保持不变) ==================

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

    private int ArgMax(float[] array)
    {
        float maxVal = float.MinValue;
        int maxIndex = 0;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] > maxVal)
            {
                maxVal = array[i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }

    // ================== 初始化工具 ==================

    [ContextMenu("Reset to Standard VAD")]
    public void ResetToStandardEmotions()
    {
        emotionDefinitions = new List<EmotionDefinition>()
        {
            new EmotionDefinition { emotionName = "Neutral (中性)", vadCentroid = Vector3.zero },
            new EmotionDefinition { emotionName = "Joy (快乐)",     vadCentroid = new Vector3(0.75f, 0.60f, 0.40f) },
            new EmotionDefinition { emotionName = "Anger (愤怒)",   vadCentroid = new Vector3(-0.51f, 0.59f, 0.25f) },
            new EmotionDefinition { emotionName = "Fear (恐惧)",    vadCentroid = new Vector3(-0.64f, 0.60f, -0.43f) },
            new EmotionDefinition { emotionName = "Sadness (悲伤)", vadCentroid = new Vector3(-0.60f, -0.30f, -0.20f) },
            new EmotionDefinition { emotionName = "Boredom (无聊)", vadCentroid = new Vector3(-0.65f, -0.62f, -0.33f) },
            new EmotionDefinition { emotionName = "Disdain (鄙视)", vadCentroid = new Vector3(-0.60f, 0.50f, 0.70f) },
            new EmotionDefinition { emotionName = "Docile (温顺)",  vadCentroid = new Vector3(0.40f, -0.20f, -0.40f) }
        };
        Debug.Log("已重置为标准 VAD 情绪值");
    }
}