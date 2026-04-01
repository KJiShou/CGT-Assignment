using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Data Source")]
    [Tooltip("For manually writing tooltip text")]
    [TextArea]
    public string manualText;

    [Tooltip("If using personality scriptable object as data, then check this")]
    public bool isPersonalityTrait = false;

    public TraitDictionarySO traitDictionary;
    public NPCDoubleDecay targetNPC;

    // when cursor enter this UI
    public void OnPointerEnter(PointerEventData eventData)
    {
        string contentToShow = manualText;

        if (isPersonalityTrait && traitDictionary != null && targetNPC != null)
        {
            float traitValue = GetTraitValueFromNPC(traitDictionary.traitName);
            var traitInfo = traitDictionary.GetTraitInfo(traitValue);

            contentToShow = $"<b>{traitInfo.shortLabel}</b>\n{traitInfo.detailedDescription}";
        }

        if (!string.IsNullOrEmpty(contentToShow))
        {
            TooltipManager.instance.ShowTooltip(contentToShow);
        }
    }

    // when cursor exit this UI
    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.instance.HideTooltip();
    }

    private float GetTraitValueFromNPC(string traitName)
    {
        if (traitName.Contains("Openness")) return targetNPC.personality.openness;
        if (traitName.Contains("Conscientiousness")) return targetNPC.personality.conscientiousness;
        if (traitName.Contains("Extraversion")) return targetNPC.personality.extraversion;
        if (traitName.Contains("Agreeableness")) return targetNPC.personality.agreeableness;
        if (traitName.Contains("Neuroticism")) return targetNPC.personality.neuroticism;
        return 0f;
    }
}