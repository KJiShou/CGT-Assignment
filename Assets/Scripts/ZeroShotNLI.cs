using UnityEngine;
using Unity.InferenceEngine; // Sentis 2.x
using System.Collections.Generic;
using System.Linq;

public class ZeroShotNLI : MonoBehaviour
{
    [Header("Model Files")]
    public ModelAsset modelAsset;
    public TextAsset vocabFile;   // vocab.txt
    public TextAsset mergesFile;  // merges.txt

    [System.Serializable]
    public struct EmotionMapping
    {
        public string label;
        [Tooltip("X=Valence(愉悦), Y=Arousal(激活), Z=Dominance(优势)")]
        public Vector3 vad;
    }

    // --- 核心配置：标签与 VAD 的映射表 ---
    // 你可以随意增加标签，只要给它配好 VAD 值即可
    [Header("Emotion VAD Settings")]
    [Tooltip("在这里添加、修改或删除情绪标签及其对应的 VAD 值")]
    public List<EmotionMapping> emotionSettings = new List<EmotionMapping>()
    {
        // 默认值 (你可以直接在 Inspector 里改，这里的代码只是初始值)
        new EmotionMapping { label = "Joy",       vad = new Vector3(1.0f, 0.8f, 0.7f) },
        new EmotionMapping { label = "Anger",     vad = new Vector3(0.1f, 0.9f, 0.8f) },
        new EmotionMapping { label = "Fear",      vad = new Vector3(0.1f, 0.9f, 0.1f) },
        new EmotionMapping { label = "Sadness",   vad = new Vector3(0.1f, 0.2f, 0.2f) },
        new EmotionMapping { label = "Neutral",   vad = new Vector3(0.5f, 0.5f, 0.5f) },
        new EmotionMapping { label = "Sarcastic", vad = new Vector3(0.2f, 0.7f, 0.8f) },
        new EmotionMapping { label = "Disgust",   vad = new Vector3(0.1f, 0.6f, 0.6f) },
        new EmotionMapping { label = "Surprise",  vad = new Vector3(0.8f, 0.9f, 0.4f) },
        new EmotionMapping { label = "Craving",  vad = new Vector3(0.25f, 0.60f, 0.30f) },
        new EmotionMapping { label = "Greedy",  vad = new Vector3(0.65f, 0.75f, 0.80f) },
        new EmotionMapping { label = "Murder",  vad = new Vector3(0.05f, 0.95f, 0.90f) },
    };

    private Dictionary<string, Vector3> labelDefinitions;

    [Header("Test Input")]
    public string testSentence = "Thanks for nothing.";

    private Worker worker;
    private BPETokenizer tokenizer;

    // RoBERTa 特殊 Token
    private const int BOS = 0; // <s>
    private const int EOS = 2; // </s>

    void Start()
    {
        if (vocabFile && mergesFile && modelAsset)
        {
            tokenizer = new BPETokenizer(vocabFile.text, mergesFile.text);
            var model = ModelLoader.Load(modelAsset);
            worker = new Worker(model, BackendType.GPUCompute);

            Analyze(testSentence);
        }
    }

    [ContextMenu("Run Analysis")]
    public void Test() => Analyze(testSentence);

    [ContextMenu("Rebuild Dictionary")]
    // 如果你在运行时通过代码修改了 List，可以调用这个方法刷新字典
    public void RebuildDictionary()
    {
        labelDefinitions = new Dictionary<string, Vector3>();
        foreach (var mapping in emotionSettings)
        {
            if (!string.IsNullOrEmpty(mapping.label) && !labelDefinitions.ContainsKey(mapping.label))
            {
                labelDefinitions.Add(mapping.label, mapping.vad);
            }
        }
    }

    class EmotionScore { public string label; public float score; }

    public Vector3 Analyze(string text)
    {
        if (worker == null) return Vector3.zero;

        if (labelDefinitions == null || labelDefinitions.Count == 0) RebuildDictionary();

        List<string> activeLabels = labelDefinitions.Keys.ToList();

        // 使用一个类来存结果，方便排序
        List<EmotionScore> scores = new List<EmotionScore>();

        foreach (string label in activeLabels)
        {
            float score = RunNLI(text, label);
            scores.Add(new EmotionScore { label = label, score = score });
        }

        // --- 核心算法优化开始 ---

        // 1. 处决中性 (Neutral Killing)
        // 找出除了 Neutral 以外的最高分
        float maxEmotionScore = scores.Where(x => x.label != "Neutral").Max(x => x.score);

        // 如果有任何情绪超过 0.25 (25%)，说明这就不是中性的
        if (maxEmotionScore > 0.25f)
        {
            var neutralItem = scores.FirstOrDefault(x => x.label == "Neutral");
            if (neutralItem != null) neutralItem.score = 0f; // 强制归零
        }

        // 2. 排序并取 Top 3 (Top-K Strategy)
        // 我们只关心最强烈的 3 个信号，忽略 Joy: 16% 这种噪声
        var topScores = scores.OrderByDescending(x => x.score).Take(3).ToList();

        Vector3 weightedVAD = Vector3.zero;
        float totalWeight = 0f;
        string debugInfo = $"'{text}' 深度解析 (Top 3):\n";

        foreach (var item in topScores)
        {
            if (item.score < 0.05f) continue; // 忽略极小值

            // 3. 信号锐化 (Signal Sharpening)
            // 使用平方函数 Math.Pow(score, 2)
            // 例子: 0.4^2 = 0.16, 0.2^2 = 0.04
            // 差距从 2倍 变成了 4倍！这样高分标签的话语权更大。
            float sharpWeight = Mathf.Pow(item.score, 2);

            Vector3 definedVAD = labelDefinitions[item.label];
            weightedVAD += definedVAD * sharpWeight;
            totalWeight += sharpWeight;

            debugInfo += $"[{item.label}]: {item.score:P0} (Weight: {sharpWeight:F2}), ";
        }

        if (totalWeight > 0.001f)
        {
            weightedVAD /= totalWeight;
        }
        else
        {
            weightedVAD = new Vector3(0.5f, 0.5f, 0.5f);
        }

        Debug.Log($"<color=green>ZeroShotNLI():</color>{debugInfo}, VAD: {weightedVAD}");
        return weightedVAD;
    }

    // 运行单次 NLI 推理 (代码与之前相同，封装了一下)
    private float RunNLI(string premise, string label)
    {
        string hypothesis = $"This example is about {label}.";

        List<int> tokens = new List<int>();
        tokens.Add(BOS);
        tokens.AddRange(tokenizer.Encode(premise).Where(x => x != BOS && x != EOS));
        tokens.Add(EOS);
        tokens.Add(EOS);
        tokens.AddRange(tokenizer.Encode(hypothesis).Where(x => x != BOS && x != EOS));
        tokens.Add(EOS);

        int[] inputIds = tokens.ToArray();
        TensorShape shape = new TensorShape(1, inputIds.Length);

        using Tensor<int> tInput = new Tensor<int>(shape, inputIds);
        using Tensor<int> tMask = new Tensor<int>(shape, Enumerable.Repeat(1, inputIds.Length).ToArray());

        worker.SetInput("input_ids", tInput);
        worker.SetInput("attention_mask", tMask);
        worker.Schedule();

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        float[] logits = output.DownloadToArray();

        // 这是一个 Logit，需要转概率。
        // Entailment 是 index 1 (对于 cross-encoder 模型)
        return 1f / (1f + Mathf.Exp(-logits[1]));
    }

    private void OnDestroy() => worker?.Dispose();
}