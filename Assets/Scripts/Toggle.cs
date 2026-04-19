using UnityEngine;

public class Toggle : MonoBehaviour
{
    public GameObject debugInputField;
    public void ToggleDebugInputField()
    {
        if (debugInputField != null)
        {
            bool isActive = debugInputField.activeSelf;
            debugInputField.SetActive(!isActive);
        }
    }
}
