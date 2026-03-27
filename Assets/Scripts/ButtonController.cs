using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    [Header("Button Event Color")]
    [SerializeField] Color32 buttonOnSelectedColor = new Color32(126, 216, 242, 255);
    [SerializeField] Color32 buttonDefaultColor = new Color32(200, 200, 200, 255);

    [Header("Button Images")]
    [SerializeField] Image emotionImage;
    [SerializeField] Image probabilityImage;
    [SerializeField] Image personalityImage;

    private void Start()
    {
        // default select emotion panel
        UpdateButtonColors(emotionImage);
    }

    public void EmotionButtonOnClick()
    {
        UpdateButtonColors(emotionImage);
    }

    public void ProbabilityButtonOnClick()
    {
        UpdateButtonColors(probabilityImage);
    }

    public void PersonalityButtonOnClick()
    {
        UpdateButtonColors(personalityImage);
    }

    private void UpdateButtonColors(Image selectedButton)
    {
        if (emotionImage != null)
            emotionImage.color = (emotionImage == selectedButton) ? buttonOnSelectedColor : buttonDefaultColor;

        if (probabilityImage != null)
            probabilityImage.color = (probabilityImage == selectedButton) ? buttonOnSelectedColor : buttonDefaultColor;

        if (personalityImage != null)
            personalityImage.color = (personalityImage == selectedButton) ? buttonOnSelectedColor : buttonDefaultColor;
    }
}