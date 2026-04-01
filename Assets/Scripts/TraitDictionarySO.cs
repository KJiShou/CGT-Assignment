using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Trait Dictionary", menuName = "Personality/Trait Dictionary")]
public class TraitDictionarySO : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("trait name, e.g. Openness")]
    public string traitName;

    [System.Serializable]
    public struct TraitLevel
    {
        [Tooltip("Min value to trigger this description. The list config must arrange from high to low value" +
            " (e.g. 0.6, 0.2, -0.2)")]
        [Range(-1f, 1f)]
        public float minThreshold;

        public string shortLabel;

        [TextArea(3, 5)]
        public string detailedDescription;
    }

    [Header("Trait Levels (arrange from high to low value)")]
    public List<TraitLevel> levels = new List<TraitLevel>();

    /// <summary>
    /// 核心匹配逻辑：传入 NPC 的特质数值，返回对应档位的文本结构体
    /// pass NPC trait value, return corresponding level of info
    /// </summary>
    /// <param name="value">NPC trait value (-1 ~ 1)</param>
    /// <returns>Matched info</returns>
    public TraitLevel GetTraitInfo(float value)
    {
        foreach (var level in levels)
        {
            if (value >= level.minThreshold)
            {
                return level;
            }
        }

        // If value smaller than every min threshold or list is empty
        if (levels.Count > 0)
        {
            return levels[levels.Count - 1]; // return lowest level
        }
        else
        {
            // return new struct
            return new TraitLevel
            {
                shortLabel = "Unknown",
                detailedDescription = "Not in dictionary"
            };
        }
    }
}
