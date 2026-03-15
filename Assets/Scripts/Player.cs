using UnityEngine;
using System;
using UnityEngine.Events;
using System.Linq;
using System.Collections.Generic;




#if UNITY_EDITOR
using UnityEditor; // 必须引用这个命名空间
#endif

public class Player : MonoBehaviour
{
    [Header("Player Input")]
    [TextArea] public string input;

    // 定义一个静态事件，参数是 string
    // static 意味着这个事件属于类，而不是某个实例，方便全局访问
    // public static event Action<string> OnMessageSent;

    // 定义一个可以传 string 的 UnityEvent
    // [System.Serializable] 确保它能在 Inspector 里显示
    [System.Serializable]
    public class StringEvent : UnityEvent<string> { }

    [Header("Put the Script going to run here")]
    public StringEvent onMessageSent;

    public List<string> historyDialog = new List<string>();

    // 为了避免歧义，改名为 SendInput
    public void SendInput()
    {
        Debug.Log("<color=aqua>Player 发送了消息: " + input + "</color>");

        // 触发事件！所有订阅了这个事件的脚本都会收到通知
        // ?.Invoke 是一种安全写法，如果没人订阅就不执行，防止报错
        historyDialog.Add(input);
        onMessageSent?.Invoke(input);
    }
}

// ========================================================
// 下面的代码定义了 Inspector 的长相
// 你可以把这段放在同一个文件里（放在最下面），也可以单独建一个脚本
// ========================================================
#if UNITY_EDITOR
[CustomEditor(typeof(Player))] // 告诉 Unity 这个编辑器是给 MyScript 用的
public class MyScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 绘制默认的 Inspector（否则原本的变量会消失）
        base.OnInspectorGUI();

        // 2. 获取目标脚本对象
        Player script = (Player)target;

        // 3. 添加空行（可选，为了美观）
        EditorGUILayout.Space();

        // 4. 绘制按钮
        if (GUILayout.Button("Send Message"))
        {
            // 点击按钮后执行的逻辑
            script.SendInput();
        }
    }
}
#endif