using UnityEngine;
using Unity.InferenceEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class TwitterSentimentVAD : MonoBehaviour
{
    [Header("Twitter RoBERTa Files")]
    public ModelAsset modelAsset;
    public TextAsset vocabFile;  
    public TextAsset mergesFile; 

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
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Debug.LogWarning("Duplicate TwitterSentimentVAD found and destroyed.");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (vocabFile && mergesFile && modelAsset)
        {
            tokenizer = new RobertaTokenizer(vocabFile, mergesFile);
            var model = ModelLoader.Load(modelAsset);
            worker = new Worker(model, BackendType.GPUCompute);

            //Analyze(inputSentence);
        }
    }

    [ContextMenu("Run Analysis")]
    public void Test() => Analyze(inputSentence);

    public Vector3 Analyze(string text)
    {
        if (worker == null) return Vector3.zero;

        // 1. BPE
        List<int> tokens = tokenizer.Encode(text);
        int[] inputIds = tokens.ToArray();
        TensorShape shape = new TensorShape(1, inputIds.Length);

        // 2. Prepare Tensor
        using Tensor<int> tInput = new Tensor<int>(shape, inputIds);
        using Tensor<int> tMask = new Tensor<int>(shape, Enumerable.Repeat(1, inputIds.Length).ToArray());

        worker.SetInput("input_ids", tInput);
        worker.SetInput("attention_mask", tMask);

        // 3. Reasoning
        worker.Schedule();

        // 4. Obtain Logits (vector: 1x3 -> [Neg, Neu, Pos])
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        float[] logits = output.DownloadToArray();

        // 5. Softmax
        float[] probs = Softmax(logits);
        float pNeg = probs[0]; // Negative probability
        float pNeu = probs[1]; // Neutral probability
        float pPos = probs[2]; // Positive probability

        Debug.Log($"<color=yellow>Sentence Sentiment Probability:</color>\nInput: {text}\nNeg:{pNeg:P1}, Neu:{pNeu:P1}, Pos:{pPos:P1}");

        // Mapping 0 ~ 1 value to -1 ~ 1 value

        // [Valence] (-1: sadness, 1: joy)
        // 100% Positive = 1 - 0 = 1
        // 100% Negative = 0 - 1 = -1
        // 100% Neutral = 0 - 0 = 0
        float valence = pPos - pNeg;

        // [Arousal] (-1: inactive, 1: active)
        // 100% Neutral -> (0 * 2) - 1 = -1 (Calm)
        // 100% Positive/Negative -> (1 * 2) - 1 = 1 (Excited)
        float emotionIntensity = pPos + pNeg;
        float arousal = (emotionIntensity * 2f) - 1f;

        // [Dominance] (-1: submission, 1: control)
        // Positive -> 1
        // Negative -> 0.5 ~ 0.8
        // Neutral -> -1
        float dominance = (pPos + (pNeg * 0.6f)) - pNeu;

        valence = Mathf.Clamp(valence, -1f, 1f);
        arousal = Mathf.Clamp(arousal, -1f, 1f);
        dominance = Mathf.Clamp(dominance, -1f, 1f);

        Vector3 vad = new Vector3((float)Math.Round(valence, 2), (float)Math.Round(arousal, 2), (float)Math.Round(dominance, 2));

        string tag = EmotionClassifier.instance.Classify(vad);

        Debug.Log($"<color=yellow>TwitterSentimentVAD Analyze Output: </color>\nInput: {text}\nValence: {vad.x:F2}, Arousal: {vad.y:F2}, Dominance: {vad.z:F2}\nClassify: {(tag)}");

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
