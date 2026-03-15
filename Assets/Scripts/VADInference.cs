using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.InferenceEngine;

public class VADInference : MonoBehaviour
{
    [Header("Model File")]
    public ModelAsset modelAsset;
    public TextAsset vocabFile;

    [Header("Testing")]
    public string testSentence = "I am very happy today!";

    private Worker worker;
    private Dictionary<string, int> vocab;
    private Model runtimeModel;

    private const int CLS_TOKEN_ID = 101;
    private const int SEP_TOKEN_ID = 102;
    private const int UNK_TOKEN_ID = 100;

    void Start()
    {
        // 1. 加载词表
        LoadVocab();

        // 2. 加载模型
        SetupModel();

        // 3. 启动时测试一次
        if (modelAsset != null) Analyze(testSentence);
    }

    void LoadVocab()
    {
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
        //Debug.Log($"词表加载完成，共 {vocab.Count} 个词。");
    }

    void SetupModel()
    {
        if (modelAsset == null) return;
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);
    }

    [ContextMenu("Run Test Sentence")]
    public void RunTest()
    {
        Analyze(testSentence);
    }

    public Vector3 Analyze(string sentence)
    {
        if(worker  == null) return Vector3.zero;

        // --- 1. 分词 (Tokenization) ---
        int[] inputIds = EncodeText(sentence);

        // --- 2. 准备 Tensor ---
        // 形状: [1, 序列长度]
        using Tensor<int> inputTensor = new Tensor<int>(new TensorShape(1, inputIds.Length), inputIds);
        using Tensor<int> attentionMaskTensor = new Tensor<int>(new TensorShape(1, inputIds.Length), Enumerable.Repeat(1, inputIds.Length).ToArray());
        using Tensor<int> tokenTypeIdsTensor = new Tensor<int>(new TensorShape(1, inputIds.Length), new int[inputIds.Length]); // 全0

        // --- 3. 设置输入 (注意：这里必须匹配 ONNX 的输入名) ---
        // 通常 HuggingFace 导出的模型输入名为: "input_ids", "attention_mask", "token_type_ids"
        worker.SetInput("input_ids", inputTensor);
        worker.SetInput("attention_mask", attentionMaskTensor);
        worker.SetInput("token_type_ids", tokenTypeIdsTensor);

        // --- 4. 执行推理 ---
        worker.Schedule();

        // --- 5. 获取输出 ---
        // 获取输出 Tensor (通常名为 "logits" 或 "last_hidden_state"，这里取第一个输出)
        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;

        if (outputTensor == null)
        {
            Debug.LogError("输出 Tensor 获取失败！");
            return Vector3.zero;
        }

        // 从 GPU 拉取数据到 CPU
        float[] results = outputTensor.DownloadToArray();

        // 原始数值 (1.0 ~ 5.0)
        float rawV = results[0];
        float rawA = results[1];
        float rawD = results[2];

        // --- 新增：归一化处理 (映射到 0.0 ~ 1.0) ---
        // 使用 Clamp01 防止模型偶尔预测出 5.01 或 0.99 这种越界数值
        float normV = Mathf.Clamp01((rawV - 1f) / 4f);
        float normA = Mathf.Clamp01((rawA - 1f) / 4f);
        float normD = Mathf.Clamp01((rawD - 1f) / 4f);

        Debug.Log($"<color=lightblue>BERT():</color>\n'{sentence}', Valence(愉悦): {normV:F3}, Arousal(激活): {normA:F3}, Dominance(优势): {normD:F3}");

        return new Vector3(normV, normA, normD);
    }

    // 简易 BERT 分词器 (不支持 WordPiece 子词拆分，仅作演示)
    private int[] EncodeText(string text)
    {
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

    private void OnDestroy()
    {
        worker?.Dispose();
    }
}
