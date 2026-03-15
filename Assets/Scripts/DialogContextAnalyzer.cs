using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DialogContextAnalyzer : MonoBehaviour
{
    public static DialogContextAnalyzer instance;

    [Header("Settings")]
    [Tooltip("新句子的权重 (0~1)。0.3 表示历史占 70%，新句子占 30%")]
    [Range(0.1f, 1f)] public float smoothingFactor = 0.4f;

    [Tooltip("保留多少句历史记录来分析趋势")]
    public int historyCapacity = 100;

    [Header("Runtime State")]
    // 上下文平滑后的 VAD (Contextual VAD)
    public Vector3 smoothVAD = Vector3.zero;

    // 原始历史记录 (用于计算斜率/方差)
    public Queue<Vector3> rawHistory = new Queue<Vector3>();

    [Header("Analysis Result")]
    public string currentTrend = "Stable"; // 趋势标签

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

    /// <summary>
    /// 输入当前句子的 VAD，返回结合了历史上下文的 VAD
    /// </summary>
    public Vector3 ProcessInput(Vector3 currentRawVAD)
    {
        // 1. 更新原始历史队列
        rawHistory.Enqueue(currentRawVAD);
        if (rawHistory.Count > historyCapacity) rawHistory.Dequeue();

        // 2. 计算 EMA (指数移动平均) - 这是你的"语境 VAD"
        if (smoothVAD == Vector3.zero)
        {
            smoothVAD = currentRawVAD; // 第一句
        }
        else
        {
            // 公式：New = Current * alpha + Old * (1 - alpha)
            smoothVAD = Vector3.Lerp(smoothVAD, currentRawVAD, smoothingFactor);
        }

        // 3. 分析趋势 (Trend Analysis)
        AnalyzeTrend(currentRawVAD);

        Debug.Log($"<color=orange>History Player's Dialog Smooth Transition : {smoothVAD} | Trend: {currentTrend}</color>");

        // 返回平滑后的 VAD 用于 NPC 反应，这样 NPC 不会一惊一乍
        return smoothVAD;
    }

    private void AnalyzeTrend(Vector3 current)
    {
        //if (rawHistory.Count < 2)
        //{
        //    currentTrend = "Insufficient Data";
        //    return;
        //}

        // 计算过去几句的平均值 (不包括当前这句)
        // 这里的逻辑是：拿"现在"跟"过去平均"比
        Vector3 pastAverage = Vector3.zero;
        int count = 0;
        foreach (var h in rawHistory)
        {
            // 简单的跳过当前这句的逻辑(如果是Queue其实都在里面，这里简化处理全算)
            pastAverage += h;
            count++;
        }
        pastAverage /= count;

        // 计算增量
        float deltaV = current.x - pastAverage.x;
        float deltaA = current.y - pastAverage.y;
        float deltaD = current.z - pastAverage.z;

        // 定义阈值，只有变化超过这个值才算趋势
        float threshold = 0.15f;

        // === 简单的趋势规则引擎 ===

        // 1. 激进化检测 (Arousal 飙升)
        if (deltaA > threshold)
        {
            if (deltaV < -threshold) currentTrend = "Escalating Anger (激怒中)";
            else if (deltaV > threshold) currentTrend = "Getting Excited (越来越兴奋)";
            else currentTrend = "Becoming Alert (警觉)";
        }
        // 2. 冷静化检测 (Arousal 下降)
        else if (deltaA < -threshold)
        {
            if (deltaV < -threshold) currentTrend = "Becoming Depressed (陷入低落)";
            else currentTrend = "Calming Down (冷静下来)";
        }
        // 3. 愉悦度检测 (Arousal 没变，单纯变脸)
        else
        {
            if (deltaV > threshold) currentTrend = "Warming Up (变暖)";
            else if (deltaV < -threshold) currentTrend = "Turning Cold (变冷淡)";
            else currentTrend = "Stable (稳定)";
        }
    }

    [ContextMenu("Reset Context")]
    public void ResetContext()
    {
        smoothVAD = Vector3.zero;
        rawHistory.Clear();
        currentTrend = "Reset";
    }
}

[CustomEditor(typeof(DialogContextAnalyzer))] // 告诉 Unity 这是给 TestQueue 用的编辑器
public class TestQueueEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 绘制默认的 Inspector (显示原本的变量)
        base.OnInspectorGUI();

        // 2. 获取目标脚本
        DialogContextAnalyzer script = (DialogContextAnalyzer)target;

        // 3. 绘制 Queue 标题
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("History Dialog Emotions", EditorStyles.boldLabel);

        // 4. 遍历并显示 Queue 的内容
        // 注意：Queue 不能用 for 循环索引，只能用 foreach
        if (script.rawHistory != null && script.rawHistory.Count > 0)
        {
            int index = 0;
            foreach (var item in script.rawHistory)
            {
                // 显示每一项： "[0]: Attack"
                EditorGUILayout.LabelField($"[{index}]", item.ToString());
                index++;
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Queue is Empty", MessageType.Info);
        }

        // 强制刷新 Inspector (因为 Queue 变化时 Unity 不会自动重绘)
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}