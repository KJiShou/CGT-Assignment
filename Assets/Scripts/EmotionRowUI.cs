using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmotionRowUI : MonoBehaviour
{
    public TextMeshProUGUI numberText, nameText, vText, aText, dText;

    public Button editBtn, deleteBtn;

    public void UpdateEmotionRow(string number, string name, string v, string a, string d)
    {
        numberText.text = number;
        nameText.text = name;
        vText.text = v;
        aText.text = a;
        dText.text = d;
    }
}
