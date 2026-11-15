using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Fusion.Photon.Realtime;
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
    private int dedicatedJoinVersion;

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
    /// Enters a session hosted by a dedicated Fusion server. The caller must
    /// first obtain the match-scoped identity from FusionMatchJoinTokenService.
    /// Unlike Shared mode, the client never loads the gameplay scene: the
    /// dedicated server owns the network scene transition and all match state.
    /// </summary>

public async Task<bool> JoinDedicatedMatchAsync(
    FusionDedicatedIdentity identity)
{
    if (!identity.IsValid)
    {
        Failed?.Invoke(
            "Identidade da partida dedicada inválida.");
        return false;
    }

    if (starting)
        return false;

    int joinVersion = ++dedicatedJoinVersion;
    starting = true;
    FusionDedicatedMatchContext.Set(identity);

    try
    {
        if (runner != null && runner.SessionInfo.IsValid)
        {
            Failed?.Invoke(
                "Já existe uma partida Fusion ativa.");
            FusionDedicatedMatchContext.Clear();
            return false;
        }

        // Encerra a conexão anterior ao lobby Photon.
        if (runner != null)
        {
            NetworkRunner oldRunner = runner;
            runner = null;

            if (oldRunner.IsRunning ||
                oldRunner.LobbyInfo.IsValid)
            {
                await oldRunner.Shutdown();
            }

            Destroy(oldRunner.gameObject);

            // Permite que o Unity conclua a destruição.
            await Task.Yield();
        }

        // MPS reports its public endpoint before the Linux process has
        // necessarily registered its Fusion room. Give that startup sequence
        // a bounded 30-second window instead of declaring GameNotFound after
        // only a few seconds.
        const int maxAttempts = 15;

        for (int attempt = 1;
             attempt <= maxAttempts;
             attempt++)
        {
            if (joinVersion != dedicatedJoinVersion) return false;
            PrepareRunner();

            if (runner == null)
            {
                Failed?.Invoke(
                    "Não foi possível criar o NetworkRunner.");
                FusionDedicatedMatchContext.Clear();
                return false;
            }

            Debug.Log(
                $"[Fusion] Tentativa {attempt}/{maxAttempts} " +
                $"de entrar na sessão {identity.MatchId}");

            StartGameResult result =
                await runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Client,
                    SessionName = identity.MatchId,
                    PlayerCount = maximumPlayers,
                    // StartGameArgs.Address is the local address on which this
                    // client binds its own UDP socket. Supplying the MPS public
                    // endpoint makes Windows try to bind an IP it does not own
                    // ("Failed to bind socket"). Fusion finds the dedicated
                    // session through the match name and Photon region instead.
                    CustomPhotonAppSettings =
                        CreatePhotonSettings(identity.PhotonRegion),
                    SceneManager =
                        runner.GetComponent<
                            NetworkSceneManagerDefault>()
                });

            if (joinVersion != dedicatedJoinVersion)
            {
                if (runner != null && runner.IsRunning) await runner.Shutdown();
                DisposeRunner();
                FusionDedicatedMatchContext.Clear();
                return false;
            }

            if (result.Ok)
            {
                Debug.Log(
                    "[Fusion] Conectado à partida dedicada.");

                Connected?.Invoke(runner);
                return true;
            }

            Debug.LogWarning(
                $"[Fusion] Falha na tentativa {attempt}: " +
                result.ShutdownReason);

            bool retry =
                result.ShutdownReason ==
                ShutdownReason.GameNotFound;

            NetworkRunner failedRunner = runner;
            runner = null;

            if (failedRunner != null)
            {
                if (failedRunner.IsRunning)
                    await failedRunner.Shutdown();

                Destroy(failedRunner.gameObject);
                await Task.Yield();
            }

            if (!retry || attempt == maxAttempts)
            {
                if (retry)
                    AbandonUnreachableDedicatedMatch(identity.MatchId);

                Failed?.Invoke(
                    $"Falha ao entrar na sessão: " +
                    result.ShutdownReason);

                FusionDedicatedMatchContext.Clear();
                return false;
            }

            // Aguarda a criação da sala Photon pelo servidor.
            await Task.Delay(2000);
        }

        FusionDedicatedMatchContext.Clear();
        return false;
    }
    catch (Exception exception)
    {
        FusionDedicatedMatchContext.Clear();

        Failed?.Invoke(
            "Erro na conexão dedicada: " +
            exception.Message);

        Debug.LogException(exception);
        return false;
    }
    finally
    {
        starting = false;
    }
}

    /// <summary>Cancels an in-flight dedicated join so its allocated server is not kept active by a stalled client.</summary>
    public async void CancelDedicatedJoin()
    {
        dedicatedJoinVersion++;
        FusionDedicatedMatchContext.Clear();
        if (runner == null) return;
        NetworkRunner current = runner;
        runner = null;
        if (current.IsRunning || current.LobbyInfo.IsValid)
            await current.Shutdown();
        Destroy(current.gameObject);
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

        if (string.IsNullOrWhiteSpace(onlineGameScenePath))
        {
            Failed?.Invoke("A cena online não foi configurada.");
            return false;
        }

        try
        {
            runner.LoadScene(SceneRef.FromPath(onlineGameScenePath), LoadSceneMode.Single);
            Debug.Log($"[FusionSessionService] Shared Master carregando a cena online '{onlineGameScenePath}'.");
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
        dedicatedJoinVersion++;
        if (runner != null && runner.IsRunning) await runner.Shutdown();
        DisposeRunner();
        FusionDedicatedMatchContext.Clear();
    }

    private void DisposeRunner()
    {
        if (runner == null) return;
        Destroy(runner.gameObject);
        runner = null;
    }

    private static void AbandonUnreachableDedicatedMatch(string matchId)
    {
        if (string.IsNullOrWhiteSpace(matchId) ||
            CapivaradaBackendService.Instance == null)
            return;

        CapivaradaBackendService.Instance.Post<
            CapivaradaBackendService.EmptyResponse,
            CapivaradaBackendService.EmptyResponse>(
                "matches/" + Uri.EscapeDataString(matchId) + "/abandon",
                new CapivaradaBackendService.EmptyResponse(),
                _ => Debug.LogWarning("[Fusion] Alocação sem sala removida; a próxima tentativa pedirá um servidor novo."),
                error => Debug.LogWarning("[Fusion] Não foi possível remover a alocação sem sala: " + error));
    }

    private static FusionAppSettings CreatePhotonSettings(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
            return null;

        FusionAppSettings settings =
            PhotonAppSettings.Global.AppSettings.GetCopy();
        settings.FixedRegion = region;
        return settings;
    }
}
