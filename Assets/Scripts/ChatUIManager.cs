using System;
using System.Linq;
using TMPro;
using UnityEngine;

public class ChatUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField playerInputField;
    public GameObject playerMessagePrefab;
    public Transform playerMessageParentTransform;

    [Header("Target AI")]
    [Tooltip("Receive message NPC")]
    private NPCDoubleDecay _targetNPC;
    public NPCDoubleDecay targetNPC
    {
        get { return _targetNPC; }
        set
        {
            if (_targetNPC == value) return;

            if (_targetNPC != null)
            {
                _targetNPC.isTalkWithPlayer = false;
            }

            // If current UI is open and has old target, cancel the old target event
            if (this.isActiveAndEnabled && _targetNPC != null)
            {
                _targetNPC.OnEmotionProcessed -= OnNPCReacted;
            }

            // When switch target，set last NPC message to false
            ClearChatUI();

            // update target
            _targetNPC = value;

            // If current UI is open and has new target, add new target event
            if (this.isActiveAndEnabled && _targetNPC != null)
            {
                _targetNPC.OnEmotionProcessed += OnNPCReacted;
            }
        }
    }

    private void OnEnable()
    {
        if (_targetNPC != null)
        {
            _targetNPC.OnEmotionProcessed += OnNPCReacted;
        }
    }

    private void OnDisable()
    {
        if (_targetNPC != null)
        {
            _targetNPC.OnEmotionProcessed -= OnNPCReacted;
        }
    }

    // When player click send button
    public void SendMessageToNPC()
    {
        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message) || _targetNPC == null) return;

        playerInputField.text = "";
        _targetNPC.ReceivePlayerMessage(message);
    }

    private void OnNPCReacted(string originalMessage, Vector3 newEmotion, string currentTrend)
    {
        GameObject messageObject = Instantiate(playerMessagePrefab, playerMessageParentTransform);

        TextMeshProUGUI txt = messageObject.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text = $"{originalMessage} -- {DateTime.Now:HH:mm:ss}";
        }

        if(_targetNPC.historyInputs.Count > _targetNPC.maxPlayerInputHistory)
        {
            _targetNPC.historyInputs.RemoveAt(0);
        }

        _targetNPC.historyInputs.Add(messageObject);

        Debug.Log($"<color=cyan>UI Manager Instantiated Chat Message. NPC emotion: {newEmotion}</color>");
    }

    private void ClearChatUI()
    {
        if (playerMessageParentTransform == null || _targetNPC == null) return;

        foreach (GameObject child in _targetNPC.historyInputs)
        {
            child.SetActive(false);
        }
    }
}
