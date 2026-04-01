using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;

    [Header("UI References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    public RectTransform tooltipRect;
    public RectTransform canvasRect;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        HideTooltip();
    }

    private void Update()
    {
        // if Tooltip displaying，let tooltip follow cursor
        if (tooltipPanel.activeSelf)
        {
            Vector2 localPoint;
            // convert cursor position to Canvas local position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                null,
                out localPoint);

            // add some offset，prevent Tooltip too close cursor
            tooltipRect.localPosition = localPoint + new Vector2(15f, -15f);
        }
    }

    public void ShowTooltip(string content)
    {
        tooltipText.text = content;
        tooltipPanel.SetActive(true);

        // force refresh UI layout，prevent first frame background size not match
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
        tooltipText.text = "";
    }

}
