using System;
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
    private bool initialized;
    public event Action<string> Failed;

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
            await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = nickname });
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
        await LeaveMatchAsync();
        partyChannel = $"party:{partyId}";
        await VivoxService.Instance.JoinGroupChannelAsync(partyChannel, ChatCapability.AudioOnly);
    }

    public async void JoinMatch(string matchId) => await JoinMatchAsync(matchId);
    public async Task JoinMatchAsync(string matchId)
    {
        if (!await InitializeForCurrentSessionAsync() || string.IsNullOrWhiteSpace(matchId)) return;
        await LeavePartyAsync();
        matchChannel = $"match:{matchId}";
        await VivoxService.Instance.JoinPositionalChannelAsync(matchChannel, ChatCapability.AudioOnly, new Channel3DProperties(Mathf.RoundToInt(audibleDistance), Mathf.RoundToInt(conversationalDistance), 1f, AudioFadeModel.InverseByDistance));
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
}
