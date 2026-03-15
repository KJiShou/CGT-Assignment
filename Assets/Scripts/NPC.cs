using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class NPC : MonoBehaviour
{
    [System.Serializable]
    public struct Personality
    {
        [Header("Big Five Traits (OCEAN)")]
        [Tooltip("开放性 (O): -1(保守) 到 1(好奇/创造力)")]
        [Range(-1f, 1f)]
        public float openness;

        [Tooltip("尽责性 (C): -1(散漫) 到 1(严谨/自律)")]
        [Range(-1f, 1f)]
        public float conscientiousness;

        [Tooltip("外向性 (E): -1(内向) 到 1(热情/社交)")]
        [Range(-1f, 1f)]
        public float extraversion;

        [Tooltip("宜人性 (A): -1(冷漠/挑剔) 到 1(友善/合作)")]
        [Range(-1f, 1f)]
        public float agreeableness;

        [Tooltip("神经质 (N): -1(自信/情绪稳定) 到 1(焦虑/敏感)")]
        [Range(-1f, 1f)]
        public float neuroticism;

        // 添加一个辅助函数，直接利用你刚才给的公式计算 PAD
        public Vector3 GetVAD()
        {
            // 1. Valence (愉悦度) = 0.21E + 0.59A + 0.19N
            // 注意：这里的N通常指"情绪稳定性"，但在Big5里N是神经质(负面)。
            // 如果你的公式来源里N代表"Neuroticism"(神经质)，那么通常高神经质会导致低愉悦。
            // *但在你给的图片公式里系数是 +0.19N*。
            // 这有点反直觉（通常神经质越高越不开心）。
            // 请确认你的公式来源定义。如果严格按照图片公式写：
            float v = 0.21f * extraversion + 0.59f * agreeableness + 0.19f * neuroticism;

            // 2. Arousal (激活度) = 0.15O + 0.30A - 0.57N
            float a = 0.15f * openness + 0.30f * agreeableness - 0.57f * neuroticism;

            // 3. Dominance (优势度) = 0.25O + 0.17C + 0.60E - 0.32A
            float d = 0.25f * openness + 0.17f * conscientiousness + 0.60f * extraversion - 0.32f * agreeableness;

            Debug.Log($"Personality VAD: V：{v}, A: {a}, D: {d}");
            return new Vector3(v, a, d);
        }
    }

    public Personality personality;

    [Header("Time Decay Settings")]
    [Tooltip("每秒钟情绪回归平静的速度 (0~1)。例如 0.1 代表每秒恢复 10%")]
    [Range(0f, 1f)] public float timeDecaySpeed = 0.001f;


    [Tooltip("当前 NPC 的情绪状态 (History)")]
    public Vector3 currentEmotion = Vector3.zero; // 初始为中性

    //private void OnEnable()
    //{
    //    // 订阅事件：当 Player 触发事件时，执行 MyFunction
    //    Player.OnMessageSent += AnalyzePlayerDialog();
    //}

    //private void OnDisable()
    //{
    //    // ⚠️ 养成好习惯：脚本禁用或销毁时必须取消订阅，防止内存泄漏
    //    Player.OnMessageSent -= AnalyzePlayerDialog(input);
    //}

    // 使用 Update 进行实时衰减
    void Update()
    {
        // 如果情绪不是 0，就慢慢让它归零
        if (currentEmotion != Vector3.zero)
        {
            // 使用 MoveTowards 让数值匀速回归 0 (线性衰减)
            // 或者使用 Lerp (指数衰减，一开始快后来慢)

            // 方案 A: 线性回归 (推荐，比较可控)
            // 每秒钟向 0 移动 timeDecaySpeed 的距离
            currentEmotion = Vector3.MoveTowards(currentEmotion, Vector3.zero, timeDecaySpeed * Time.deltaTime);
            currentEmotion.x = Mathf.Clamp(currentEmotion.x, -1f, 1f);
            currentEmotion.y = Mathf.Clamp(currentEmotion.y, -1f, 1f);
            currentEmotion.z = Mathf.Clamp(currentEmotion.z, -1f, 1f);

            // 方案 B: 指数回归 (更自然，但永远不会完全变0)
            // currentEmotion = Vector3.Lerp(currentEmotion, Vector3.zero, timeDecaySpeed * Time.deltaTime);

            // 可选：实时更新 UI 或 表情
            // EmotionClassifier.instance.Classify(currentEmotion); 
        }
    }

    [ContextMenu("Clear NPC Emotion")]
    public void ClearNPCEmotion()
    {
        currentEmotion = Vector3.zero;
    }

    [ContextMenu("Get VAD")]
    void GetVAD()
    {
        personality.GetVAD();
    }

    [ContextMenu("Analyze Player Dialog")]
    public void AnalyzePlayerDialog(string input)
    {
        // 1. 获取输入 VAD (映射到 -1 ~ 1)
        Vector3 rawInput = TwitterSentimentVAD.instance.Analyze(input);
        Vector3 processedInput = DialogContextAnalyzer.instance.ProcessInput(rawInput);


        // 2. 获取性格系数 (Personality Factors)
        // 对应公式里的 P_V, P_A, P_D
        // 注意：这里我们直接用 GetVAD() 算出的值作为系数是可行的，
        // 但论文里的 P 可能特指 "性格对情绪变化的增益率"，
        // 简单起见，我们假设性格 VAD 本身就是这个增益系数。
        Vector3 pFactors = personality.GetVAD();

        // 3. 计算情绪增量 (Delta)
        // 这里的逻辑是：性格决定了 NPC 对这句话有多敏感
        // 比如：神经质(N)高的人，对负面输入(V<0)反应更大

        float vDelta = pFactors.x * processedInput.x;
        float aDelta = pFactors.y * processedInput.y;
        float dDelta = pFactors.z * processedInput.z;

        // 4. 叠加到当前情绪 (Add Impact)
        // 注意：这里不再乘 decayRate，因为 decay 在 Update 里每秒都在做
        currentEmotion.x += vDelta;
        currentEmotion.y += aDelta;
        currentEmotion.z += dDelta;

        // 5. 强制限制在 [-1, 1] 之间 (防止数值爆炸)
        currentEmotion.x = Mathf.Clamp(currentEmotion.x, -1f, 1f);
        currentEmotion.y = Mathf.Clamp(currentEmotion.y, -1f, 1f);
        currentEmotion.z = Mathf.Clamp(currentEmotion.z, -1f, 1f);

        string currentTag = EmotionClassifier.instance.Classify(currentEmotion);

        Debug.Log($"<color=white>NPC Emotion Updated | Current: {currentEmotion} ({currentTag})");
    }
}
