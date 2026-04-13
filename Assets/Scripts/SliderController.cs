using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
    [Header("Memory Settings")]
    public TextMeshProUGUI memoryForgetRateText;
    public Slider memoryForgetRateSlider;
    public TextMeshProUGUI historyInfluenceText;
    public Slider historyInfluenceSlider;
    public TextMeshProUGUI maxEmotionHistoryText;
    public Slider maxEmotionHistorySlider;

    [Header("Decay Settings")]
    public TextMeshProUGUI timeDecaySpeedText;
    public Slider timeDecaySpeedSlider;

    [Header("Personality")]
    public TextMeshProUGUI opennessText;
    public Slider opennessSlider;
    public TextMeshProUGUI conscientiousnessText;
    public Slider conscientiousnessSlider;
    public TextMeshProUGUI extraversionText;
    public Slider extraversionSlider;
    public TextMeshProUGUI agreeablenessText;
    public Slider agreeablenessSlider;
    public TextMeshProUGUI neuroticismText;
    public Slider neuroticismSlider;

    [Header("Player Input Settings")]
    public TextMeshProUGUI smoothingFactorText;
    public Slider smoothingFactorSlider;
    public TextMeshProUGUI emotionTrendThresholdText;
    public Slider emotionTrendThresholdSlider;
    public TextMeshProUGUI maxInputHistoryText;
    public Slider maxInputHistorySlider;

    [Header("Player Settings Panel")]
    [Tooltip("Reference to the Player Settings panel GameObject")]
    public GameObject playerSettingsPanel;
    private bool isPlayerSettingsVisible = true;

    [HideInInspector]
    public NPCDoubleDecay npcState;

    /// <summary>
    /// [UI Simplification] Toggle Player Settings panel visibility via gear icon
    /// </summary>
    public void TogglePlayerSettings()
    {
        if (playerSettingsPanel != null)
        {
            isPlayerSettingsVisible = !isPlayerSettingsVisible;
            playerSettingsPanel.SetActive(isPlayerSettingsVisible);
        }
    }

    public void MemoryForgetRateOnChanged(float value)
    {
        if (npcState == null) return;
        
        if (memoryForgetRateText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            memoryForgetRateText.text = v.ToString();
            npcState.memoryForgetRate = v;
        }
    }
    public void HistoryInfluenceOnChanged(float value)
    {
        if (npcState == null) return;

        if (historyInfluenceText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            historyInfluenceText.text = v.ToString();
            npcState.historyInfluence = v;
        }
    }
    public void MaxEmotionHistoryOnChanged(float value)
    {
        if (npcState == null) return;

        if (maxEmotionHistoryText != null)
        {
            int v = ConvertFloatToInt(value);
            maxEmotionHistoryText.text = v.ToString();
            npcState.maxEmotionHistory = v;
        }
    }
    public void TimeDecaySpeedOnChanged(float value)
    {
        if (npcState == null) return;

        if (timeDecaySpeedText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            timeDecaySpeedText.text = v.ToString();
            npcState.timeDecaySpeed = v;
        }
    }

    public void OpennessOnChanged(float value)
    {
        if (npcState == null) return;

        if (opennessText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            opennessText.text = v.ToString();
            npcState.personality.openness = v;
        }
    }

    public void ConscientiousnessOnChanged(float value)
    {
        if (npcState == null) return;

        if (conscientiousnessText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            conscientiousnessText.text = v.ToString();
            npcState.personality.conscientiousness = v;
        }
    }

    public void ExtraversionOnChanged(float value)
    {
        if (npcState == null) return;

        if (extraversionText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            extraversionText.text = v.ToString();
            npcState.personality.extraversion = v;
        }
    }

    public void AgreeablenessTextOnChanged(float value)
    {
        if (npcState == null) return;

        if (agreeablenessText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            agreeablenessText.text = v.ToString();
            npcState.personality.agreeableness = v;
        }
    }

    public void NeuroticismOnChanged(float value)
    {
        if (npcState == null) return;

        if (neuroticismText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            neuroticismText.text = v.ToString();
            npcState.personality.neuroticism = v;
        }
    }
    public void SmoothingFactorOnChanged(float value)
    {
        if (npcState == null) return;

        if (smoothingFactorText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            smoothingFactorText.text = v.ToString();
            npcState.smoothingFactor = v;
        }
    }
    public void EmotionTrendThresholdOnChanged(float value)
    {
        if (npcState == null) return;

        if (emotionTrendThresholdText != null)
        {
            float v = RoundToAnyFloatingPoint(value, 2);
            emotionTrendThresholdText.text = v.ToString();
            npcState.playerEmotionTrendThreshold = v;
        }
    }
    public void MaxInputHistoryOnChanged(float value)
    {
        if (npcState == null) return;

        if (maxInputHistoryText != null)
        {
            int v = ConvertFloatToInt(value);
            maxInputHistoryText.text = v.ToString();
            npcState.maxPlayerInputHistory = v;
        }
    }

    private float RoundToAnyFloatingPoint(float value, int numOfFloatingPoint)
    {
        return (float)Math.Round(value, numOfFloatingPoint);
    }

    private int ConvertFloatToInt(float value)
    {
        return (int)value;
    }
}
