using UnityEngine;

/// <summary>Único ponto de acesso aos serviços persistentes instalados no GameObject Services da Boot_Scene.</summary>
public sealed class ServiceHub : MonoBehaviour
{
    public static ServiceHub Instance { get; private set; }
    [SerializeField] private PlayFabAuthManager authentication;
    [SerializeField] private PlayerProfileService profile;
    [SerializeField] private EconomyService economy;
    [SerializeField] private LeaderboardService leaderboard;
    [SerializeField] private SettingsService settings;
    [Header("Economy V2 and social")]
    [SerializeField] private CapivaradaBackendService backend;
    [SerializeField] private EconomyV2Service economyV2;
    [SerializeField] private LeaderboardV2Service leaderboardV2;
    [SerializeField] private FriendsService friends;
    [SerializeField] private PartyService party;
    [SerializeField] private ChatService chat;
    [Header("Session and availability")]
    [SerializeField] private SessionAccessService session;
    [SerializeField] private BackendHealthService health;
    [SerializeField] private OnlinePresenceService presence;
    [Header("Realtime")]
    [SerializeField] private FusionSessionService fusion;
    [SerializeField] private FusionMatchJoinTokenService fusionJoinTokens;
    [SerializeField] private FusionMatchmakingService matchmaking;
    [SerializeField] private VivoxVoiceChannelService voice;
    public PlayFabAuthManager Authentication => authentication;
    public PlayerProfileService Profile => profile;
    public EconomyService Economy => economy;
    public LeaderboardService Leaderboard => leaderboard;
    public SettingsService Settings => settings;
    public CapivaradaBackendService Backend => backend;
    public EconomyV2Service EconomyV2 => economyV2;
    public LeaderboardV2Service LeaderboardV2 => leaderboardV2;
    public FriendsService Friends => friends;
    public PartyService Party => party;
    public ChatService Chat => chat;
    public SessionAccessService Session => session;
    public BackendHealthService Health => health;
    public OnlinePresenceService Presence => presence;
    public FusionSessionService Fusion => fusion;
    public FusionMatchJoinTokenService FusionJoinTokens => fusionJoinTokens;
    public FusionMatchmakingService Matchmaking => matchmaking;
    public VivoxVoiceChannelService Voice => voice;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (authentication == null) authentication = GetComponent<PlayFabAuthManager>();
        if (profile == null) profile = GetComponent<PlayerProfileService>();
        if (economy == null) economy = GetComponent<EconomyService>();
        if (leaderboard == null) leaderboard = GetComponent<LeaderboardService>();
        if (settings == null) settings = GetComponent<SettingsService>();
        if (backend == null) backend = GetComponent<CapivaradaBackendService>();
        if (economyV2 == null) economyV2 = GetComponent<EconomyV2Service>();
        if (leaderboardV2 == null) leaderboardV2 = GetComponent<LeaderboardV2Service>();
        if (friends == null) friends = GetComponent<FriendsService>();
        if (party == null) party = GetComponent<PartyService>();
        if (chat == null) chat = GetComponent<ChatService>();
        if (session == null) session = GetComponent<SessionAccessService>();
        if (health == null) health = GetComponent<BackendHealthService>();
        if (presence == null) presence = GetComponent<OnlinePresenceService>();
        if (presence == null) presence = gameObject.AddComponent<OnlinePresenceService>();
        if (fusion == null) fusion = GetComponent<FusionSessionService>();
        if (fusionJoinTokens == null) fusionJoinTokens = GetComponent<FusionMatchJoinTokenService>();
        if (matchmaking == null) matchmaking = GetComponent<FusionMatchmakingService>();
        if (voice == null) voice = GetComponent<VivoxVoiceChannelService>();
        DontDestroyOnLoad(gameObject);
    }
}
