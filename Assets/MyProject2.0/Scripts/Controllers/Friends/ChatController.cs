using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders the party-only chat feed and submits text. PanelNavigationController owns
/// panel visibility; legacy tab callbacks are retained solely for existing Inspector wiring.
/// </summary>
public sealed class ChatController : MonoBehaviour
{
    [SerializeField] private ChatService chatService;
    [SerializeField] private OtherProfileController otherProfile;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button partyTabButton;
    [SerializeField] private TMP_Text contextualTabText;
    [Header("Group chat availability")]
    [Tooltip("Botões do lobby que permanecem visíveis, mas desativados, enquanto o grupo não tiver outra pessoa.")]
    [SerializeField] private Selectable[] lobbyChatControls;
    [Tooltip("Objetos da aba Grupo que desaparecem enquanto o grupo não tiver outra pessoa.")]
    [SerializeField] private GameObject[] groupChatObjects;
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
        // Legacy Inspector entry point. Global chat no longer exists.
        SelectContextual();
        Rebuild();
    }

    public void SelectParty()
    {
        SelectContextual();
        Rebuild();
    }

    /// <summary>The only chat scope is Grupo and it exists with two or more members.</summary>
    public void SelectContextual()
    {
        if (chatService == null) return;
        if (chatService.HasPartyChannel) chatService.SelectParty();
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
        if (chatService == null) return;
        bool available = chatService.HasPartyChannel;
        if (contextualTabText == null && partyTabButton != null)
            contextualTabText = partyTabButton.GetComponentInChildren<TMP_Text>(true);
        if (contextualTabText != null)
            contextualTabText.text = "Grupo";
        if (partyTabButton != null)
            partyTabButton.gameObject.SetActive(available);
        if (sendButton != null) sendButton.interactable = available;
        ApplyAvailability(available);

        if (messagesContent == null || messagePrefab == null) return;
        int count = 0;
        foreach (ChatService.Message message in chatService.SelectedMessages)
        {
            ChatMessagePrefabController row = Instantiate(messagePrefab, messagesContent);
            spawned.Add(row.gameObject);
            row.Bind(message, playerId => otherProfile?.Open(playerId));
            count++;
        }
        if (emptyState != null) emptyState.SetActive(count == 0);
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

    private void ApplyAvailability(bool available)
    {
        if (lobbyChatControls != null)
            foreach (Selectable control in lobbyChatControls) if (control != null) control.interactable = available;
        if (groupChatObjects != null)
            foreach (GameObject target in groupChatObjects) if (target != null) target.SetActive(available);
    }
}
