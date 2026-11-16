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

    [Serializable]
    public sealed class Message
    {
        public string senderId;
        public string senderName;
        public string avatarId;
        public string text;
        public string channel;
        public DateTime receivedAtUtc;
        public bool fromSelf;
    }

    public static ChatService Instance { get; private set; }

    [SerializeField] private VivoxVoiceChannelService voice;
    [SerializeField] private PartyService party;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private PlayerProfileService profile;
    [SerializeField, Min(10)] private int maximumMessagesPerChannel = 100;

    public bool HasPartyChannel => party != null && party.HasOtherMember && !string.IsNullOrWhiteSpace(voice != null ? voice.PartyChannel : null);
    public IReadOnlyList<Message> PartyMessages => partyMessages;
    public IReadOnlyList<Message> SelectedMessages => PartyMessages;
    public event Action Changed;
    public event Action<string> Failed;

    private readonly List<Message> partyMessages = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (voice == null) voice = GetComponent<VivoxVoiceChannelService>();
        if (party == null) party = GetComponent<PartyService>();
        if (session == null) session = GetComponent<SessionAccessService>();
        if (profile == null) profile = GetComponent<PlayerProfileService>();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (voice != null)
        {
            voice.ChannelMessageReceived += Receive;
            voice.Failed += Report;
            voice.PartyChannelChanged += OnPartyChanged;
        }
        if (party != null) party.Changed += OnPartyChanged;
    }

    private void OnDisable()
    {
        if (voice != null)
        {
            voice.ChannelMessageReceived -= Receive;
            voice.Failed -= Report;
            voice.PartyChannelChanged -= OnPartyChanged;
        }
        if (party != null) party.Changed -= OnPartyChanged;
    }

    public void InitializeChat()
    {
        if (!CanUseChat()) return;
        // There is no global channel. Vivox is initialized only after the
        // party service confirms a group with at least two members.
        Changed?.Invoke();
    }

    public void SelectParty()
    {
        if (!CanUseChat() || party == null || !party.HasOtherMember)
        {
            Report("O chat fica disponível quando houver outra pessoa no grupo.");
            return;
        }
        Changed?.Invoke();
    }

    public void Send(string text)
    {
        if (!CanUseChat()) return;
        if (string.IsNullOrWhiteSpace(text)) return;
        if (voice == null) { Report("VivoxVoiceChannelService não foi configurado em Services."); return; }

        string channel = voice.PartyChannel;
        if (string.IsNullOrWhiteSpace(channel))
        {
            Report("Canal de chat indisponível.");
            return;
        }
        PlayerProfileData current = profile != null ? profile.Current : null;
        string metadata = JsonUtility.ToJson(new MessageMetadata
        {
            playFabId = current != null ? current.playerId : string.Empty,
            avatarId = current != null ? current.avatarId : string.Empty,
            nickname = current != null ? current.nickname : string.Empty
        });
        _ = voice.SendTextAsync(channel, text, metadata);
    }

    private void OnPartyChanged()
    {
        Changed?.Invoke();
    }

    private void Receive(VivoxMessage vivoxMessage)
    {
        if (vivoxMessage == null || string.IsNullOrWhiteSpace(vivoxMessage.ChannelName)) return;
        List<Message> destination;
        if (voice != null && string.Equals(vivoxMessage.ChannelName, voice.PartyChannel, StringComparison.Ordinal)) destination = partyMessages;
        else return;

        MessageMetadata metadata = ParseMetadata(vivoxMessage.Metadata);
        destination.Add(new Message
        {
            senderId = !string.IsNullOrWhiteSpace(metadata.playFabId) ? metadata.playFabId : vivoxMessage.SenderPlayerId,
            senderName = !string.IsNullOrWhiteSpace(metadata.nickname) ? metadata.nickname : vivoxMessage.SenderDisplayName,
            avatarId = metadata.avatarId,
            text = vivoxMessage.MessageText,
            channel = vivoxMessage.ChannelName,
            receivedAtUtc = vivoxMessage.ReceivedTime.ToUniversalTime(),
            fromSelf = vivoxMessage.FromSelf
        });
        int maximum = Mathf.Max(10, maximumMessagesPerChannel);
        while (destination.Count > maximum) destination.RemoveAt(0);
        Changed?.Invoke();
    }

    [Serializable]
    private sealed class MessageMetadata
    {
        public string playFabId;
        public string avatarId;
        public string nickname;
    }

    private static MessageMetadata ParseMetadata(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new MessageMetadata();
        try { return JsonUtility.FromJson<MessageMetadata>(json) ?? new MessageMetadata(); }
        catch { return new MessageMetadata(); }
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
