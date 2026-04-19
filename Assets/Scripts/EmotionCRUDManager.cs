using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class EmotionCRUDManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentParent;
    public GameObject rowPrefab;
    public TextMeshProUGUI emotionClassifierText;

    [Header("Input Form")]
    public GameObject inputForm;
    public TMP_InputField nameInput;
    public Slider vInput, aInput, dInput;
    public Button actionButton;
    public TextMeshProUGUI actionText;
    public TextMeshProUGUI errorMessage;
    public TextMeshProUGUI title;

    private int editingIndex = -1;

    // 【新增】：CRUD 专属的 UI 对象池
    private List<EmotionRowUI> rowPool = new List<EmotionRowUI>();

    void Start()
    {
        RefreshUI();
        actionButton.onClick.AddListener(OnActionButtonClick);
    }

    [ContextMenu("Refresh UI")]
    public void RefreshUI()
    {
       
        if (EmotionClassifier.instance == null || EmotionClassifier.instance.emotionDefinitions == null) return;

        var list = EmotionClassifier.instance.emotionDefinitions;

        emotionClassifierText.text = "Emotion Classifier " + "(" + (list.Count() - 1) + ")";

        int uiIndex = 0;

        bool hasSkippedNeutral = false;

        // Object pool
        for (int i = 0; i < list.Count; i++)
        {
            var def = list[i];

            if (!hasSkippedNeutral && string.Equals(def.emotionName, "Neutral", System.StringComparison.OrdinalIgnoreCase))
            {
                hasSkippedNeutral = true; // Only the first Neutral consider as default emotion
                continue;
            }

            if (uiIndex >= rowPool.Count)
            {
                GameObject newRow = Instantiate(rowPrefab, contentParent, false);
                
                // Normalized
                newRow.transform.localScale = Vector3.one;
                newRow.transform.localPosition = new Vector3(newRow.transform.localPosition.x, newRow.transform.localPosition.y, 0f);

                if (newRow.TryGetComponent<EmotionRowUI>(out var rowUI))
                {
                    rowPool.Add(rowUI);
                }
            }

            EmotionRowUI currentRow = rowPool[uiIndex];
            currentRow.gameObject.SetActive(true);

            // Update text
            currentRow.UpdateEmotionRow(
                (uiIndex + 1).ToString(),
                def.emotionName,
                $"{def.vadCentroid.x:F2}",
                $"{def.vadCentroid.y:F2}",
                $"{def.vadCentroid.z:F2}"
            );

            // Remove previous event listener
            currentRow.deleteBtn.onClick.RemoveAllListeners();
            currentRow.editBtn.onClick.RemoveAllListeners();

            int capturedIndex = i;
            EmotionDefinition capturedDef = def;

            currentRow.deleteBtn.onClick.AddListener(() => {
                EmotionClassifier.instance.emotionDefinitions.RemoveAt(capturedIndex);
                RefreshUI();
            });

            currentRow.editBtn.onClick.AddListener(() => {
                PrepareEdit(capturedIndex, capturedDef);
            });

            uiIndex++;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
        }

        // Hide unused UI row
        for (int i = uiIndex; i < rowPool.Count; i++)
        {
            rowPool[i].gameObject.SetActive(false);
        }
    }

    public void OnAddEmotionClick()
    {
        errorMessage.text = "";
        title.text = "Add Emotion";
        ClearInputs();
        SetMode(true);
        inputForm.SetActive(true);
    }

    private void OnActionButtonClick()
    {
        string eName = nameInput.text.Trim();

        if (eName == "")
        {
            errorMessage.text = "Emotion name is required!";
            return;
        }

        Vector3 vad = new Vector3(vInput.value, aInput.value, dInput.value);

        if (editingIndex == -1)
        {
            // Adding mode
            EmotionClassifier.instance.emotionDefinitions.Add(new EmotionDefinition
            {
                emotionName = eName,
                vadCentroid = vad
            });
        }
        else
        {
            // Edit mode
            EmotionClassifier.instance.emotionDefinitions[editingIndex] = new EmotionDefinition
            {
                emotionName = eName,
                vadCentroid = vad
            };
            SetMode(true);
        }

        RefreshUI();
        ClearInputs();
        inputForm.SetActive(false);
        title.text = "Add Emotion";
    }

    private void PrepareEdit(int index, EmotionDefinition def)
    {
        errorMessage.text = "";
        title.text = "Edit Emotion";
        editingIndex = index;
        nameInput.text = def.emotionName;
        vInput.value = def.vadCentroid.x;
        aInput.value = def.vadCentroid.y;
        dInput.value = def.vadCentroid.z;
        SetMode(false);
        inputForm.SetActive(true);
    }

    private void SetMode(bool isAdding)
    {
        if (isAdding)
        {
            editingIndex = -1;
            actionText.text = "Add New";
        }
        else
        {
            actionText.text = "Save Changes";
        }
    }

    private void ClearInputs()
    {
        nameInput.text = "";
        vInput.value = 0;
        aInput.value = 0;
        dInput.value = 0;
        errorMessage.text = "";
    }
}