using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

/// <summary>Owns non-positional party voice and positional arena voice channel transitions.</summary>
public sealed class VivoxVoiceChannelService : MonoBehaviour
{
    public static VivoxVoiceChannelService Instance { get; private set; }
    [SerializeField] private PlayFabAuthManager authentication;
    [SerializeField] private float audibleDistance = 25f;
    [SerializeField] private float conversationalDistance = 7f;
    private string partyChannel;
    private string matchChannel;
    private string globalChatChannel;
    private bool initialized;
    public event Action<string> Failed;
    public event Action<VivoxMessage> ChannelMessageReceived;

    public string PartyChannel => partyChannel;
    public string GlobalChatChannel => globalChatChannel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (authentication == null) authentication = GetComponent<PlayFabAuthManager>();
        DontDestroyOnLoad(gameObject);
    }

    public async void InitializeForCurrentSession() => await InitializeForCurrentSessionAsync();

    public async Task<bool> InitializeForCurrentSessionAsync()
    {
        if (initialized && VivoxService.Instance.IsLoggedIn) return true;
        try
        {
            await UnityServices.InitializeAsync();
            await VivoxService.Instance.InitializeAsync();
            string nickname = authentication != null && !string.IsNullOrWhiteSpace(authentication.Nickname) ? authentication.Nickname : "Capivara";
            // When Unity Authentication is active, Vivox owns the immutable player ID.
            // The display name is presentation only; profile clicks resolve through PlayFab IDs
            // supplied by the UI/service layer where available.
            await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = nickname });
            VivoxService.Instance.ChannelMessageReceived -= ForwardChannelMessage;
            VivoxService.Instance.ChannelMessageReceived += ForwardChannelMessage;
            initialized = true;
            return true;
        }
        catch (Exception exception)
        {
            Failed?.Invoke($"Vivox indisponível: {exception.Message}");
            return false;
        }
    }

    public async void JoinParty(string partyId) => await JoinPartyAsync(partyId);
    public async Task JoinPartyAsync(string partyId)
    {
        if (!await InitializeForCurrentSessionAsync() || string.IsNullOrWhiteSpace(partyId)) return;
        string requestedChannel = BuildChannelName("party", partyId);
        if (string.Equals(partyChannel, requestedChannel, StringComparison.Ordinal)) return;
        try
        {
            await LeaveMatchAsync();
            await LeavePartyAsync();
            await VivoxService.Instance.JoinGroupChannelAsync(requestedChannel, ChatCapability.TextAndAudio);
            partyChannel = requestedChannel;
        }
        catch (Exception exception)
        {
            partyChannel = null;
            Failed?.Invoke($"Não foi possível entrar no canal do grupo: {exception.Message}");
        }
    }

    public async void JoinGlobalChat(string channelName) => await JoinGlobalChatAsync(channelName);
    public async Task JoinGlobalChatAsync(string channelName)
    {
        if (!await InitializeForCurrentSessionAsync() || string.IsNullOrWhiteSpace(channelName)) return;
        // Keep the global identifier stable because ChatService uses the same
        // configured value to route received messages into its global feed.
        string requestedChannel = BuildChannelName(string.Empty, channelName);
        if (string.Equals(globalChatChannel, requestedChannel, StringComparison.Ordinal)) return;
        try
        {
            await LeaveGlobalChatAsync();
            await VivoxService.Instance.JoinGroupChannelAsync(requestedChannel, ChatCapability.TextOnly);
            globalChatChannel = requestedChannel;
        }
        catch (Exception exception)
        {
            globalChatChannel = null;
            Failed?.Invoke($"Não foi possível entrar no chat global: {exception.Message}");
        }
    }

    public async void JoinMatch(string matchId) => await JoinMatchAsync(matchId);
    public async Task JoinMatchAsync(string matchId)
    {
        if (!await InitializeForCurrentSessionAsync() || string.IsNullOrWhiteSpace(matchId)) return;
        string requestedChannel = BuildChannelName("match", matchId);
        try
        {
            await LeavePartyAsync();
            await VivoxService.Instance.JoinPositionalChannelAsync(requestedChannel, ChatCapability.AudioOnly, new Channel3DProperties(Mathf.RoundToInt(audibleDistance), Mathf.RoundToInt(conversationalDistance), 1f, AudioFadeModel.InverseByDistance));
            matchChannel = requestedChannel;
        }
        catch (Exception exception)
        {
            matchChannel = null;
            Failed?.Invoke($"Não foi possível entrar no canal da partida: {exception.Message}");
        }
    }

    public async Task LeavePartyAsync()
    {
        if (VivoxService.Instance.IsLoggedIn && !string.IsNullOrWhiteSpace(partyChannel)) await VivoxService.Instance.LeaveChannelAsync(partyChannel);
        partyChannel = null;
    }

    public async Task LeaveMatchAsync()
    {
        if (VivoxService.Instance.IsLoggedIn && !string.IsNullOrWhiteSpace(matchChannel)) await VivoxService.Instance.LeaveChannelAsync(matchChannel);
        matchChannel = null;
    }

    public async Task LeaveGlobalChatAsync()
    {
        if (VivoxService.Instance.IsLoggedIn && !string.IsNullOrWhiteSpace(globalChatChannel)) await VivoxService.Instance.LeaveChannelAsync(globalChatChannel);
        globalChatChannel = null;
    }

    /// <summary>Sends text with optional title-controlled metadata for UI identity mapping.</summary>
    public async Task SendTextAsync(string channelName, string message, string metadata = null)
    {
        if (string.IsNullOrWhiteSpace(channelName) || string.IsNullOrWhiteSpace(message)) return;
        if (!await InitializeForCurrentSessionAsync()) return;
        try
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(channelName, message.Trim(), new MessageOptions { Metadata = metadata });
        }
        catch (Exception exception)
        {
            Failed?.Invoke($"Não foi possível enviar a mensagem: {exception.Message}");
        }
    }

    private void ForwardChannelMessage(VivoxMessage message) => ChannelMessageReceived?.Invoke(message);

    private static string BuildChannelName(string prefix, string value)
    {
        const int maximumLength = 160;
        StringBuilder builder = new();
        if (!string.IsNullOrWhiteSpace(prefix)) builder.Append(prefix).Append('-');
        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character) || character == '-' || character == '_') builder.Append(character);
            else builder.Append('-');
            if (builder.Length >= maximumLength) break;
        }
        return builder.ToString();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (initialized && VivoxService.Instance != null) VivoxService.Instance.ChannelMessageReceived -= ForwardChannelMessage;
    }
}
