using UnityEngine;
using Unity.InferenceEngine;
using System.Collections.Generic;
using System.Linq;

public class TwitterSentimentVAD : MonoBehaviour
{
    [Header("Twitter RoBERTa Files")]
    public ModelAsset modelAsset;
    public TextAsset vocabFile;   // 拖入 vocab.txt
    public TextAsset mergesFile;  // 拖入 merges.txt

    [Header("Test Input")]
    [TextArea] public string inputSentence = "This game is straight fire! 🔥";

    private Worker worker;
    private RobertaTokenizer tokenizer;

    public static TwitterSentimentVAD instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        } else
        {
            Destroy(instance);
        }
    }

    void Start()
    {
        if (vocabFile && mergesFile && modelAsset)
        {
            tokenizer = new RobertaTokenizer(vocabFile, mergesFile);
            var model = ModelLoader.Load(modelAsset);
            worker = new Worker(model, BackendType.GPUCompute);

            Analyze(inputSentence);
        }
    }

    [ContextMenu("Run Analysis")]
    public void Test() => Analyze(inputSentence);

    public Vector3 Analyze(string text)
    {
        if (worker == null) return Vector3.zero;

        // 1. 分词 (BPE)
        List<int> tokens = tokenizer.Encode(text);
        int[] inputIds = tokens.ToArray();
        TensorShape shape = new TensorShape(1, inputIds.Length);

        // 2. 准备 Tensor
        using Tensor<int> tInput = new Tensor<int>(shape, inputIds);
        using Tensor<int> tMask = new Tensor<int>(shape, Enumerable.Repeat(1, inputIds.Length).ToArray());

        worker.SetInput("input_ids", tInput);
        worker.SetInput("attention_mask", tMask);

        // 3. 推理
        worker.Schedule();

        // 4. 获取 Logits (维度: 1x3 -> [Neg, Neu, Pos])
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        float[] logits = output.DownloadToArray();

        // 5. Softmax 归一化
        float[] probs = Softmax(logits);
        float pNeg = probs[0]; // 负面概率
        float pNeu = probs[1]; // 中性概率
        float pPos = probs[2]; // 正面概率

        //Debug.Log($"<color=yellow>Twitter Output:</color> Neg:{pNeg:P1}  Neu:{pNeu:P1}  Pos:{pPos:P1}");

        // --- 核心修改：直接映射到 -1 ~ 1 ---

        // [Valence 愉悦度] (-1: 痛苦/负面, 1: 快乐/正面)
        // 逻辑：正面概率减去负面概率，结果自然落在 -1 到 1 之间
        // 例：100% 正面 = 1 - 0 = 1
        // 例：100% 负面 = 0 - 1 = -1
        // 例：100% 中性 = 0 - 0 = 0
        float valence = pPos - pNeg;

        // [Arousal 激活度] (-1: 昏昏欲睡/平静, 1: 激动/警觉)
        // 逻辑：中性 (pNeu) 代表平静 (-1)。非中性 (pPos + pNeg) 代表激动。
        // 公式：(总情绪强度 * 2) - 1
        // 例：100% 中性 -> (0 * 2) - 1 = -1 (非常平静)
        // 例：100% 正/负 -> (1 * 2) - 1 = 1 (非常激动)
        float emotionIntensity = pPos + pNeg;
        float arousal = (emotionIntensity * 2f) - 1f;

        // [Dominance 优势度] (-1: 顺从/弱势, 1: 掌控/强势)
        // 逻辑：
        // - 正面情绪 (Joy) 通常是自信的 -> 趋向 1
        // - 负面情绪 (推特上的 Anger/Hate) 通常是攻击性的 -> 趋向 0.5 ~ 0.8
        // - 中性情绪 (Neutral) 是被动/无感的 -> 趋向 -1
        // 公式：(正面 + 0.6倍负面) - 中性
        float dominance = (pPos + (pNeg * 0.6f)) - pNeu;

        // 最后做一次 Clamp 防止浮点数误差导致越界 (比如变成 1.00001)
        valence = Mathf.Clamp(valence, -1f, 1f);
        arousal = Mathf.Clamp(arousal, -1f, 1f);
        dominance = Mathf.Clamp(dominance, -1f, 1f);

        Vector3 vad = new Vector3(valence, arousal, dominance);

        string tag = EmotionClassifier.instance.Classify(vad);

        Debug.Log($"<color=yellow>TwitterSentimentVAD(): '{text}', {vad} {(tag)}</color>");

        return vad;
    }

    private float[] Softmax(float[] z)
    {
        var exps = z.Select(Mathf.Exp).ToArray();
        var sum = exps.Sum();
        return exps.Select(x => x / sum).ToArray();
    }

    private void OnDestroy() => worker?.Dispose();
}
