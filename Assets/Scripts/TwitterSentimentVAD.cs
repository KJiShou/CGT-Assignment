using UnityEngine;
using Unity.InferenceEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

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

            // Warmup inference on scene start to avoid first-message lag
            Analyze("warmup");
        }
    }

    private TaskCompletionSource<Vector3> pendingTcs;
    private bool inferenceRequested;
    private string pendingText;

    [ContextMenu("Run Analysis")]
    public void Test() => _ = AnalyzeRoutine(inputSentence, null);

    // Synchronous version for backwards compatibility
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

        // 3. Reasoning (blocking but GPU-based, fast enough for short texts)
        worker.Schedule();

        // 4. Obtain Logits
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        float[] logits = output.DownloadToArray();

        // 5. Softmax
        float[] probs = Softmax(logits);
        float pNeg = probs[0];
        float pNeu = probs[1];
        float pPos = probs[2];

        float valence = pPos - pNeg;
        float emotionIntensity = pPos + pNeg;
        float arousal = (emotionIntensity * 2f) - 1f;
        float dominance = (pPos + (pNeg * 0.6f)) - pNeu;

        valence = Mathf.Clamp(valence, -1f, 1f);
        arousal = Mathf.Clamp(arousal, -1f, 1f);
        dominance = Mathf.Clamp(dominance, -1f, 1f);

        Vector3 vad = new Vector3((float)Math.Round(valence, 2), (float)Math.Round(arousal, 2), (float)Math.Round(dominance, 2));

        Debug.Log($"<color=yellow>Sentence Sentiment Probability:</color>\nInput: {text}\nNeg:{pNeg:P1}, Neu:{pNeu:P1}, Pos:{pPos:P1}");
        Debug.Log($"<color=yellow>TwitterSentimentVAD Analyze Output: </color>\nInput: {text}\nValence: {vad.x:F2}, Arousal: {vad.y:F2}, Dominance: {vad.z:F2}\nClassify: {EmotionClassifier.instance?.Classify(vad)}");

        return vad;
    }

    // Non-blocking version - request inference and return immediately
    public void AnalyzeRequest(string text, System.Action<Vector3> onComplete)
    {
        if (worker == null)
        {
            onComplete?.Invoke(Vector3.zero);
            return;
        }

        pendingText = text;
        pendingTcs = new TaskCompletionSource<Vector3>();
        pendingTcs.Task.ContinueWith(t => onComplete?.Invoke(t.Result));
    }

    private void Update()
    {
        // Process pending inference on main thread
        if (pendingTcs != null && !pendingTcs.Task.IsCompleted)
        {
            // Run inference on main thread (fast enough with GPU)
            Vector3 result = Analyze(pendingText);
            pendingTcs.SetResult(result);
        }
    }

    // Async wrapper
    public async Task<Vector3> AnalyzeAsync(string text)
    {
        var tcs = new TaskCompletionSource<Vector3>();
        AnalyzeRequest(text, result => tcs.SetResult(result));
        await tcs.Task;
        return await tcs.Task;
    }

    // Coroutine version - proper non-blocking with yield
    public Coroutine AnalyzeRoutine(string text, System.Action<Vector3> onComplete)
    {
        return StartCoroutine(AnalyzeCoroutine(text, onComplete));
    }

    private IEnumerator AnalyzeCoroutine(string text, System.Action<Vector3> onComplete)
    {
        if (worker == null)
        {
            onComplete?.Invoke(Vector3.zero);
            yield break;
        }

        // Prepare tensors on main thread
        List<int> tokens = tokenizer.Encode(text);
        int[] inputIds = tokens.ToArray();
        TensorShape shape = new TensorShape(1, inputIds.Length);

        using Tensor<int> tInput = new Tensor<int>(shape, inputIds);
        using Tensor<int> tMask = new Tensor<int>(shape, Enumerable.Repeat(1, inputIds.Length).ToArray());

        worker.SetInput("input_ids", tInput);
        worker.SetInput("attention_mask", tMask);

        // Schedule GPU work - this runs async but returns quickly
        worker.Schedule();

        // Yield to let Unity render - inference happens in background
        yield return new WaitForEndOfFrame();

        // Get result (blocks until ready, but frame has rendered)
        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        float[] logits = output.DownloadToArray();

        float[] probs = Softmax(logits);
        float valence = Mathf.Clamp(probs[2] - probs[0], -1f, 1f);
        float arousal = Mathf.Clamp((probs[2] + probs[0]) * 2f - 1f, -1f, 1f);
        float dominance = Mathf.Clamp((probs[2] + probs[0] * 0.6f) - probs[1], -1f, 1f);

        Vector3 vad = new Vector3(Round2(valence), Round2(arousal), Round2(dominance));

        Debug.Log($"<color=yellow>TwitterSentimentVAD:</color> {text} -> VAD:{vad}");
        onComplete?.Invoke(vad);
    }

    private float Round2(float value) => Mathf.Round(value * 100f) / 100f;

    private float[] Softmax(float[] z)
    {
        var exps = z.Select(Mathf.Exp).ToArray();
        var sum = exps.Sum();
        return exps.Select(x => x / sum).ToArray();
    }

    private void OnDestroy() => worker?.Dispose();
}
