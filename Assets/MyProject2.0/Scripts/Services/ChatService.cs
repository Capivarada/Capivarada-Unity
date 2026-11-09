using System;
using System.Collections.Generic;
using Unity.Services.Vivox;
using UnityEngine;

/// <summary>
/// Keeps chat state for the persistent session. It never owns panel visibility;
/// ChatController decides which already-visible view receives the selected feed.
/// </summary>
public sealed class ChatService : MonoBehaviour
{
    public enum Scope { Global, Party }

    [Serializable]
    public sealed class Message
    {
        public string senderId;
        public string senderName;
        public string text;
        public string channel;
        public DateTime receivedAtUtc;
        public bool fromSelf;
    }

    public static ChatService Instance { get; private set; }

    [SerializeField] private VivoxVoiceChannelService voice;
    [SerializeField] private PartyService party;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private string globalChannelName = "global-br";
    [SerializeField, Min(10)] private int maximumMessagesPerChannel = 100;

    public Scope SelectedScope { get; private set; } = Scope.Global;
    public string GlobalChannelName => globalChannelName;
    public bool HasPartyChannel => party != null && party.IsInParty && !string.IsNullOrWhiteSpace(voice != null ? voice.PartyChannel : null);
    public IReadOnlyList<Message> GlobalMessages => globalMessages;
    public IReadOnlyList<Message> PartyMessages => partyMessages;
    public IReadOnlyList<Message> SelectedMessages => SelectedScope == Scope.Party ? PartyMessages : GlobalMessages;
    public event Action Changed;
    public event Action<string> Failed;

    private readonly List<Message> globalMessages = new();
    private readonly List<Message> partyMessages = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (voice == null) voice = GetComponent<VivoxVoiceChannelService>();
        if (party == null) party = GetComponent<PartyService>();
        if (session == null) session = GetComponent<SessionAccessService>();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (voice != null)
        {
            voice.ChannelMessageReceived += Receive;
            voice.Failed += Report;
        }
        if (party != null) party.Changed += OnPartyChanged;
    }

    private void OnDisable()
    {
        if (voice != null)
        {
            voice.ChannelMessageReceived -= Receive;
            voice.Failed -= Report;
        }
        if (party != null) party.Changed -= OnPartyChanged;
    }

    public void InitializeChat()
    {
        if (!CanUseChat()) return;
        if (voice == null) { Report("VivoxVoiceChannelService não foi configurado em Services."); return; }
        voice.JoinGlobalChat(globalChannelName);
    }

    public void SelectGlobal()
    {
        SelectedScope = Scope.Global;
        InitializeChat();
        Changed?.Invoke();
    }

    public void SelectParty()
    {
        if (!CanUseChat() || party == null || !party.IsInParty)
        {
            Report("Entre em um grupo para usar o chat do grupo.");
            return;
        }
        SelectedScope = Scope.Party;
        Changed?.Invoke();
    }

    public void Send(string text)
    {
        if (!CanUseChat()) return;
        if (string.IsNullOrWhiteSpace(text)) return;
        if (voice == null) { Report("VivoxVoiceChannelService não foi configurado em Services."); return; }

        string channel = SelectedScope == Scope.Party ? voice.PartyChannel : globalChannelName;
        if (string.IsNullOrWhiteSpace(channel))
        {
            Report("Canal de chat indisponível.");
            return;
        }
        _ = voice.SendTextAsync(channel, text);
    }

    private void OnPartyChanged()
    {
        if (party == null || party.IsInParty) { Changed?.Invoke(); return; }
        if (SelectedScope == Scope.Party) SelectedScope = Scope.Global;
        Changed?.Invoke();
    }

    private void Receive(VivoxMessage vivoxMessage)
    {
        if (vivoxMessage == null || string.IsNullOrWhiteSpace(vivoxMessage.ChannelName)) return;
        List<Message> destination;
        if (string.Equals(vivoxMessage.ChannelName, globalChannelName, StringComparison.Ordinal)) destination = globalMessages;
        else if (voice != null && string.Equals(vivoxMessage.ChannelName, voice.PartyChannel, StringComparison.Ordinal)) destination = partyMessages;
        else return;

        destination.Add(new Message
        {
            senderId = vivoxMessage.SenderPlayerId,
            senderName = vivoxMessage.SenderDisplayName,
            text = vivoxMessage.MessageText,
            channel = vivoxMessage.ChannelName,
            receivedAtUtc = vivoxMessage.ReceivedTime.ToUniversalTime(),
            fromSelf = vivoxMessage.FromSelf
        });
        int maximum = Mathf.Max(10, maximumMessagesPerChannel);
        while (destination.Count > maximum) destination.RemoveAt(0);
        Changed?.Invoke();
    }

    private bool CanUseChat()
    {
        if (session == null) session = ServiceHub.Instance != null ? ServiceHub.Instance.Session : SessionAccessService.Instance;
        if (session == null || session.CanUseSocial) return true;
        Report("Faça login para usar o chat.");
        return false;
    }

    private void Report(string error)
    {
        Failed?.Invoke(error);
        Debug.LogWarning(error, this);
    }
}
