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
    private string requestedPartyChannel;
    private bool initialized;
    private bool initializationFailed;
    private bool vivoxEventsSubscribed;
    private Task<bool> initializationTask;
    public event Action<string> Failed;
    public event Action<VivoxMessage> ChannelMessageReceived;
    /// <summary>Raised only after Vivox confirms the party channel was joined or left.</summary>
    public event Action PartyChannelChanged;

    public string PartyChannel => partyChannel;

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
#if UNITY_SERVER
        // MPS is authoritative gameplay only. It must never load a Vivox native
        // library or attempt a UGS/Vivox login.
        return Task.FromResult(false);
#else
        if (initialized && VivoxService.Instance.IsLoggedIn)
            return Task.FromResult(true);
        if (initializationFailed || VivoxService.Instance.InitializationState == VivoxInitializationState.Failed)
            return Task.FromResult(false);
        // Chat, party and voice can request initialization during the same
        // frame. Vivox rejects concurrent login attempts, particularly in
        // WebGL, so all callers share one in-flight initialization.
        if (initializationTask != null)
            return initializationTask;

        initializationTask = InitializeForCurrentSessionInternalAsync();
        return initializationTask;
#endif
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
            if (VivoxService.Instance.InitializationState == VivoxInitializationState.Uninitialized)
                await VivoxService.Instance.InitializeAsync();

            SubscribeToVivoxEvents();
            string nickname = authentication != null && !string.IsNullOrWhiteSpace(authentication.Nickname) ? authentication.Nickname : "Capivara";
            // When Unity Authentication is active, Vivox owns the immutable player ID.
            // The display name is presentation only; profile clicks resolve through PlayFab IDs
            // supplied by the UI/service layer where available.
            await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = nickname });
            initialized = true;
            return true;
        }
        catch (Exception exception)
        {
            initializationFailed = true;
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
        if (string.Equals(partyChannel, requestedChannel, StringComparison.Ordinal) ||
            string.Equals(requestedPartyChannel, requestedChannel, StringComparison.Ordinal)) return;
        try
        {
            await LeavePartyAsync();
            requestedPartyChannel = requestedChannel;
            await VivoxService.Instance.JoinGroupChannelAsync(requestedChannel,
#if UNITY_WEBGL && !UNITY_EDITOR
                ChatCapability.TextOnly);
#else
                ChatCapability.TextAndAudio);
#endif
        }
        catch (Exception exception)
        {
            requestedPartyChannel = null;
            partyChannel = null;
            Failed?.Invoke($"Não foi possível entrar no canal do grupo: {exception.Message}");
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
        requestedPartyChannel = null;
        if (VivoxService.Instance.IsLoggedIn && !string.IsNullOrWhiteSpace(partyChannel))
            await VivoxService.Instance.LeaveChannelAsync(partyChannel);
        bool changed = !string.IsNullOrWhiteSpace(partyChannel);
        partyChannel = null;
        if (changed) PartyChannelChanged?.Invoke();
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

    private void SubscribeToVivoxEvents()
    {
        if (vivoxEventsSubscribed) return;
        VivoxService.Instance.ChannelMessageReceived += ForwardChannelMessage;
        VivoxService.Instance.ChannelJoined += OnChannelJoined;
        VivoxService.Instance.ChannelLeft += OnChannelLeft;
        vivoxEventsSubscribed = true;
    }

    private void OnChannelJoined(string channelName)
    {
        if (!string.Equals(channelName, requestedPartyChannel, StringComparison.Ordinal)) return;
        requestedPartyChannel = null;
        partyChannel = channelName;
        PartyChannelChanged?.Invoke();
    }

    private void OnChannelLeft(string channelName)
    {
        if (!string.Equals(channelName, partyChannel, StringComparison.Ordinal)) return;
        partyChannel = null;
        PartyChannelChanged?.Invoke();
    }

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
        if (!vivoxEventsSubscribed || VivoxService.Instance == null) return;
        VivoxService.Instance.ChannelMessageReceived -= ForwardChannelMessage;
        VivoxService.Instance.ChannelJoined -= OnChannelJoined;
        VivoxService.Instance.ChannelLeft -= OnChannelLeft;
    }
}
