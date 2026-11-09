using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the selected chat feed and submits text. PanelNavigationController owns
/// all tab/panel visibility; tab buttons can additionally call SelectGlobal/SelectParty.
/// </summary>
public sealed class ChatController : MonoBehaviour
{
    [SerializeField] private ChatService chatService;
    [SerializeField] private OtherProfileController otherProfile;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform messagesContent;
    [SerializeField] private ChatMessagePrefabController messagePrefab;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private TMP_Text feedbackText;

    private readonly List<GameObject> spawned = new();

    private void Awake()
    {
        if (chatService == null) chatService = ServiceHub.Instance != null ? ServiceHub.Instance.Chat : ChatService.Instance;
    }

    private void OnEnable()
    {
        if (chatService != null)
        {
            chatService.Changed += Rebuild;
            chatService.Failed += Report;
            chatService.InitializeChat();
        }
        Rebuild();
    }

    private void OnDisable()
    {
        if (chatService != null)
        {
            chatService.Changed -= Rebuild;
            chatService.Failed -= Report;
        }
    }

    public void SelectGlobal()
    {
        chatService?.SelectGlobal();
        Rebuild();
    }

    public void SelectParty()
    {
        chatService?.SelectParty();
        Rebuild();
    }

    public void Send()
    {
        if (chatService == null || messageInput == null) return;
        string message = messageInput.text;
        if (string.IsNullOrWhiteSpace(message)) return;
        chatService.Send(message);
        messageInput.text = string.Empty;
    }

    private void Rebuild()
    {
        Clear();
        if (chatService == null || messagesContent == null || messagePrefab == null) return;
        int count = 0;
        foreach (ChatService.Message message in chatService.SelectedMessages)
        {
            ChatMessagePrefabController row = Instantiate(messagePrefab, messagesContent);
            spawned.Add(row.gameObject);
            row.Bind(message);
            count++;
        }
        if (emptyState != null) emptyState.SetActive(count == 0);
        if (sendButton != null) sendButton.interactable = chatService.SelectedScope != ChatService.Scope.Party || chatService.HasPartyChannel;
    }

    private void Report(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
        else Debug.LogWarning(message, this);
    }

    private void Clear()
    {
        foreach (GameObject item in spawned) if (item != null) Destroy(item);
        spawned.Clear();
    }
}
