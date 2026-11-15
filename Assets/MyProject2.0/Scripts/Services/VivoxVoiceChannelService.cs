using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
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
    private Task<bool> initializationTask;
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

    public void InitializeForCurrentSession() => _ = InitializeForCurrentSessionAsync();

    public Task<bool> InitializeForCurrentSessionAsync()
    {
        if (initialized && VivoxService.Instance.IsLoggedIn)
            return Task.FromResult(true);
        // Chat, party and voice can request initialization during the same
        // frame. Vivox rejects concurrent login attempts, particularly in
        // WebGL, so all callers share one in-flight initialization.
        if (initializationTask != null)
            return initializationTask;

        initializationTask = InitializeForCurrentSessionInternalAsync();
        return initializationTask;
    }

    private async Task<bool> InitializeForCurrentSessionInternalAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            // This anonymous UGS identity is intentionally scoped to Vivox token issuance.
            // PlayFab remains the authoritative identity, profile and progression backend.
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
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
        finally
        {
            initializationTask = null;
        }
    }

    public void JoinParty(string partyId) => _ = JoinPartyAsync(partyId);
    public async Task JoinPartyAsync(string partyId)
    {
        if (!await InitializeForCurrentSessionAsync() || string.IsNullOrWhiteSpace(partyId)) return;
        string requestedChannel = BuildChannelName("party", partyId);
        if (string.Equals(partyChannel, requestedChannel, StringComparison.Ordinal)) return;
        try
        {
            await LeaveMatchAsync();
            // Group chat replaces Global as soon as a second party member is present.
            await LeaveGlobalChatAsync();
            await LeavePartyAsync();
            await VivoxService.Instance.JoinGroupChannelAsync(requestedChannel,
#if UNITY_WEBGL && !UNITY_EDITOR
                ChatCapability.TextOnly);
#else
                ChatCapability.TextAndAudio);
#endif
            partyChannel = requestedChannel;
        }
        catch (Exception exception)
        {
            partyChannel = null;
            Failed?.Invoke($"Não foi possível entrar no canal do grupo: {exception.Message}");
        }
    }

    public void JoinGlobalChat(string channelName) => _ = JoinGlobalChatAsync(channelName);
    public async Task JoinGlobalChatAsync(string channelName)
    {
        if (!await InitializeForCurrentSessionAsync() || string.IsNullOrWhiteSpace(channelName)) return;
        // Keep the global identifier stable because ChatService uses the same
        // configured value to route received messages into its global feed.
        string requestedChannel = BuildChannelName(string.Empty, channelName);
        if (string.Equals(globalChatChannel, requestedChannel, StringComparison.Ordinal)) return;
        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            await LeavePartyAsync();
#endif
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

    public void JoinMatch(string matchId) => _ = JoinMatchAsync(matchId);
    public Task JoinMatchAsync(string matchId)
    {
        // Voice/text belongs to the party and must survive the arena transition.
        // There is intentionally no independent match/proximity channel.
        return Task.CompletedTask;
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
