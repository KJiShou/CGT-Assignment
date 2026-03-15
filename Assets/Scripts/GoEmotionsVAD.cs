using UnityEngine;
using Unity.InferenceEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class GoEmotionsVAD : MonoBehaviour
{
    [Header("Model")]
    public ModelAsset modelAsset;
    public TextAsset vocabFile; // 继续用之前的 vocab.txt 即可！(因为都是 BERT)

    [Header("Test")]
    public string inputSentence = "This gameplay is absolutely sick!";

    private Worker worker;
    private Dictionary<string, int> vocab;
    private Model runtimeModel;

    // --- 核心：28种情绪到 VAD 的映射表 ---
    // 数据来源：NRC-VAD Lexicon 平均值
    private Dictionary<string, Vector3> emotionToVAD = new Dictionary<string, Vector3>()
    {
        { "admiration",    new Vector3(0.89f, 0.58f, 0.70f) }, // 钦佩
        { "amusement",     new Vector3(0.85f, 0.75f, 0.65f) }, // 娱乐/好笑
        { "anger",         new Vector3(0.16f, 0.86f, 0.65f) }, // 愤怒 (高A, 高D)
        { "annoyance",     new Vector3(0.20f, 0.65f, 0.50f) }, // 烦恼
        { "approval",      new Vector3(0.80f, 0.55f, 0.70f) }, // 赞同
        { "caring",        new Vector3(0.85f, 0.50f, 0.60f) }, // 关心
        { "confusion",     new Vector3(0.40f, 0.55f, 0.30f) }, // 困惑 (低D)
        { "curiosity",     new Vector3(0.70f, 0.65f, 0.60f) }, // 好奇
        { "desire",        new Vector3(0.80f, 0.75f, 0.65f) }, // 渴望
        { "disappointment",new Vector3(0.25f, 0.35f, 0.30f) }, // 失望 (低A, 低D)
        { "disapproval",   new Vector3(0.25f, 0.50f, 0.55f) }, // 反对
        { "disgust",       new Vector3(0.10f, 0.65f, 0.40f) }, // 厌恶
        { "embarrassment", new Vector3(0.30f, 0.60f, 0.20f) }, // 尴尬 (极低D)
        { "excitement",    new Vector3(0.95f, 0.90f, 0.80f) }, // 兴奋 (极高V, 极高A)
        { "fear",          new Vector3(0.15f, 0.85f, 0.20f) }, // 恐惧 (高A, 极低D)
        { "gratitude",     new Vector3(0.90f, 0.50f, 0.60f) }, // 感激
        { "grief",         new Vector3(0.10f, 0.60f, 0.20f) }, // 悲伤
        { "joy",           new Vector3(0.98f, 0.75f, 0.75f) }, // 快乐
        { "love",          new Vector3(1.00f, 0.60f, 0.70f) }, // 爱
        { "nervousness",   new Vector3(0.25f, 0.80f, 0.25f) }, // 紧张
        { "optimism",      new Vector3(0.85f, 0.60f, 0.70f) }, // 乐观
        { "pride",         new Vector3(0.90f, 0.70f, 0.90f) }, // 骄傲 (高D)
        { "realization",   new Vector3(0.60f, 0.55f, 0.60f) }, // 顿悟
        { "relief",        new Vector3(0.80f, 0.20f, 0.50f) }, // 释怀 (极低A)
        { "remorse",       new Vector3(0.20f, 0.50f, 0.30f) }, // 悔恨
        { "sadness",       new Vector3(0.10f, 0.40f, 0.20f) }, // 难过
        { "surprise",      new Vector3(0.70f, 0.85f, 0.50f) }, // 惊讶
        { "neutral",       new Vector3(0.50f, 0.50f, 0.50f) }  // 中性
    };

    // GoEmotions 模型的输出标签顺序 (必须与模型一致)
    private string[] labels = new string[] {
        "admiration", "amusement", "anger", "annoyance", "approval", "caring", "confusion",
        "curiosity", "desire", "disappointment", "disapproval", "disgust", "embarrassment",
        "excitement", "fear", "gratitude", "grief", "joy", "love", "nervousness",
        "optimism", "pride", "realization", "relief", "remorse", "sadness", "surprise", "neutral"
    };

    private const int CLS_TOKEN_ID = 101;
    private const int SEP_TOKEN_ID = 102;
    private const int UNK_TOKEN_ID = 100;

    void Start()
    {
        LoadVocab();
        SetupModel();
        if (modelAsset) Analyze(inputSentence);
    }

    void LoadVocab() {
        vocab = new Dictionary<string, int>();
        string[] lines = vocabFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            string token = lines[i];
            if (!vocab.ContainsKey(token))
            {
                vocab.Add(token, i);
            }
        }
       // Debug.Log($"词表加载完成，共 {vocab.Count} 个词。");
    }
    void SetupModel()
    {
        if (!modelAsset) return;
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }

    [ContextMenu("Run Test Sentence")]
    public void RunTest()
    {
        Analyze(inputSentence);
    }

    public Vector3 Analyze(string text)
    {
        if (worker == null) return Vector3.zero;

        // 1. 推理
        int[] inputIds = EncodeText(text);
        TensorShape shape = new TensorShape(1, inputIds.Length);
        using Tensor<int> inputT = new Tensor<int>(shape, inputIds);
        using Tensor<int> maskT = new Tensor<int>(shape, Enumerable.Repeat(1, inputIds.Length).ToArray());
        using Tensor<int> typeT = new Tensor<int>(shape, new int[inputIds.Length]);

        worker.SetInput("input_ids", inputT);
        worker.SetInput("attention_mask", maskT);
        worker.SetInput("token_type_ids", typeT);
        worker.Schedule();

        // 2. 获取 Logits (28维)
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        float[] logits = output.DownloadToArray();

        // 3. Sigmoid 归一化 (GoEmotions 是多标签分类，通常用 Sigmoid 而不是 Softmax)
        float[] probs = logits.Select(x => 1f / (1f + Mathf.Exp(-x))).ToArray();

        // 4. 加权计算最终 VAD
        Vector3 finalVAD = Vector3.zero;
        float totalWeight = 0f;

        // 打印前3名情绪，方便调试
        var topEmotions = probs.Select((p, i) => new { Prob = p, Label = labels[i] })
                               .OrderByDescending(x => x.Prob)
                               .Take(3);

        string debugStr = $"'{text}' 情绪成分: ";
        foreach (var item in topEmotions)
        {
            if (item.Prob > 0.1f) // 只有概率大于 10% 的才算数
            {
                Vector3 vad = emotionToVAD[item.Label];
                finalVAD += vad * item.Prob;
                totalWeight += item.Prob;
                debugStr += $"{item.Label}({(int)(item.Prob * 100)}%) ";
            }
        }

        if (totalWeight > 0)
        {
            finalVAD /= totalWeight; // 加权平均
        }
        else
        {
            finalVAD = new Vector3(0.5f, 0.5f, 0.5f); // 没识别出来，给中性
        }

        Debug.Log($"<color=cyan>GoEmotion():</color>\n{debugStr}, VAD: {finalVAD}");
        return finalVAD;
    }

    // EncodeText 方法也与之前完全一致
    private int[] EncodeText(string text) {
        List<int> tokens = new List<int> { CLS_TOKEN_ID }; // [CLS]

        // 转小写并简单拆分
        string cleanText = text.ToLower();
        // 简单的标点处理
        string[] rawWords = cleanText.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in rawWords)
        {
            if (vocab.ContainsKey(word))
            {
                tokens.Add(vocab[word]);
            }
            else
            {
                // 如果词表里没有这个词 (比如 "playing")，简单的做法是标记为 [UNK]
                // 进阶做法是拆分为 "play" + "##ing"
                tokens.Add(UNK_TOKEN_ID);
            }
            if (tokens.Count >= 128) break; // 截断
        }

        tokens.Add(SEP_TOKEN_ID); // [SEP]
        return tokens.ToArray();
    }

    private void OnDestroy() => worker?.Dispose();
}