using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the Fusion runner lifecycle. This is deliberately separate from match
/// rules: Fusion transports simulation; the dedicated server owns rewards.
/// </summary>
public sealed class FusionSessionService : MonoBehaviour
{
    public static FusionSessionService Instance { get; private set; }
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField, Min(1)] private int maximumPlayers = 8;
    [SerializeField] private string roomPrefix = "capivarada";
    [SerializeField] private string onlineGameScenePath = "Assets/MyProject2.0/Scenes/Game_Scene.unity";
    private NetworkRunner runner;
    private bool starting;
    private bool connectingLobby;

    public NetworkRunner Runner => runner;
    public bool IsRunning => runner != null && runner.IsRunning;
    public event Action<NetworkRunner> Connected;
    public event Action<string> Failed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PrepareRunner()
    {
        if (runner != null) return;
        if (runnerPrefab == null)
        {
            Failed?.Invoke("Configure um NetworkRunner prefab no FusionSessionService.");
            return;
        }

        runner = Instantiate(runnerPrefab);
        runner.name = "FusionRunner";
        DontDestroyOnLoad(runner.gameObject);
        if (runner.GetComponent<NetworkSceneManagerDefault>() == null) runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
    }

    public async void JoinRandomMatch() => await JoinRandomMatchAsync();

    /// <summary>
    /// Connects the persistent runner to Fusion's Shared lobby during Boot. It
    /// does not create a game room; matchmaking happens only when Play is used.
    /// </summary>
    public async void ConnectLobby() => await ConnectLobbyAsync();

    public async Task<bool> ConnectLobbyAsync()
    {
        if (connectingLobby) return false;
        PrepareRunner();
        if (runner == null) return false;
        if (runner.LobbyInfo.IsValid || runner.SessionInfo.IsValid) return true;

        connectingLobby = true;
        try
        {
            StartGameResult result = await runner.JoinSessionLobby(SessionLobby.Shared);
            if (!result.Ok)
            {
                Failed?.Invoke($"Falha ao conectar ao lobby Fusion: {result.ShutdownReason}");
                DisposeRunner();
                return false;
            }

            Connected?.Invoke(runner);
            return true;
        }
        catch (Exception exception)
        {
            Failed?.Invoke($"Falha ao conectar Fusion: {exception.Message}");
            DisposeRunner();
            return false;
        }
        finally
        {
            connectingLobby = false;
        }
    }

    public async Task<bool> JoinRandomMatchAsync()
    {
        if (starting) return false;
        PrepareRunner();
        if (runner == null) return false;
        if (runner.SessionInfo.IsValid) return true;

        starting = true;
        try
        {
            StartGameResult result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                // Empty SessionName is Fusion's documented random-join path;
                // if none exists it creates a compatible Shared session.
                PlayerCount = maximumPlayers,
                MatchmakingMode = Photon.Realtime.MatchmakingMode.FillRoom,
                SessionProperties = new Dictionary<string, SessionProperty>
                {
                    { "mode", "arena" }
                },
                // A real Fusion scene manager is required so scene NetworkObjects
                // (the round gate and spawners) are replicated after the transition.
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
            });
            if (!result.Ok)
            {
                Failed?.Invoke($"Falha ao entrar na sessão Fusion: {result.ShutdownReason}");
                return false;
            }

            Connected?.Invoke(runner);
            return true;
        }
        catch (Exception exception)
        {
            Failed?.Invoke($"Falha ao iniciar Fusion: {exception.Message}");
            return false;
        }
        finally
        {
            starting = false;
        }
    }

    /// <summary>
    /// Enters a random Shared session and lets the Shared Master perform the
    /// network scene transition. Other peers receive that transition from Fusion;
    /// they must never call Unity's SceneManager directly for an online match.
    /// </summary>
    public async Task<bool> JoinRandomMatchAndLoadAsync()
    {
        if (!await JoinRandomMatchAsync()) return false;

        if (!runner.IsSceneAuthority) return true;

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(onlineGameScenePath);
        if (buildIndex < 0)
        {
            Failed?.Invoke($"A cena online '{onlineGameScenePath}' não está no Build Profile.");
            return false;
        }

        try
        {
            runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
            Debug.Log($"[FusionSessionService] Shared Master carregando a cena online (build index {buildIndex}).");
            return true;
        }
        catch (Exception exception)
        {
            Failed?.Invoke($"Falha ao carregar a cena Fusion: {exception.Message}");
            return false;
        }
    }

    public async void LeaveSession()
    {
        if (runner != null && runner.IsRunning) await runner.Shutdown();
        DisposeRunner();
    }

    private void DisposeRunner()
    {
        if (runner == null) return;
        Destroy(runner.gameObject);
        runner = null;
    }
}
