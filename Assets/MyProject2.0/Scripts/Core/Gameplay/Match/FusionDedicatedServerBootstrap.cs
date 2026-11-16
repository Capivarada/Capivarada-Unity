using Fusion.Sockets;
using System;
using System.Collections;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_PLAYFABSERVER_API
using PlayFab;
#endif

public sealed class FusionDedicatedServerBootstrap : MonoBehaviour
{
    [Header("Fusion")]
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private string gameScenePath =
        "Assets/MyProject2.0/Scenes/Game_Scene.unity";
    [SerializeField, Min(1)] private int maximumPlayers = 8;
    [SerializeField, Min(30f)] private float activeWithoutPlayersTimeoutSeconds = 120f;

    [Header("Network")]
    [SerializeField, Min(1)] private ushort fallbackGamePort = 30000;

    private bool starting;
    private bool started;
    private NetworkRunner activeRunner;

    private async void Start()
    {
        if (!Application.isBatchMode)
        {
            enabled = false;
            return;
        }

#if ENABLE_PLAYFABSERVER_API
        try
        {
            PlayFabMultiplayerAgentAPI.OnServerActiveCallback +=
                OnPlayFabServerActive;

            PlayFabMultiplayerAgentAPI.OnShutDownCallback +=
                OnPlayFabShutdown;

            PlayFabMultiplayerAgentAPI.OnAgentErrorCallback +=
                OnPlayFabAgentError;

            Debug.Log("[Dedicated] Inicializando PlayFab GSDK.");

            PlayFabMultiplayerAgentAPI.Start();

            StartCoroutine(ReadyForPlayFabAllocation());
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Dedicated] Falha ao inicializar GSDK: " + ex);
        }
#else
        try
        {
            await StartLocalOrExternalServerAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Dedicated] Falha no servidor local: " + ex);
        }
#endif
    }

    private async Task StartLocalOrExternalServerAsync()
    {
        string matchId =
            Environment.GetEnvironmentVariable("CAPIVARADA_MATCH_ID");

        if (string.IsNullOrWhiteSpace(matchId))
        {
            Debug.LogError(
                "[Dedicated] CAPIVARADA_MATCH_ID não definido.");
            return;
        }

        ushort port = fallbackGamePort;

        string configuredPort =
            Environment.GetEnvironmentVariable("CAPIVARADA_GAME_PORT");

        if (ushort.TryParse(configuredPort, out ushort parsedPort)
            && parsedPort > 0)
        {
            port = parsedPort;
        }

        await StartServerAsync(matchId.Trim(), port);
    }

#if ENABLE_PLAYFABSERVER_API
    private IEnumerator ReadyForPlayFabAllocation()
    {
        yield return null;

        try
        {
            PlayFabMultiplayerAgentAPI.ReadyForPlayers();

            Debug.Log(
                "[Dedicated] GSDK pronto. Aguardando alocação.");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Dedicated] Falha em ReadyForPlayers: " + ex);
        }
    }

    private async void OnPlayFabServerActive()
    {
        if (starting || started)
        {
            Debug.LogWarning(
                "[Dedicated] Callback de ativação duplicado.");
            return;
        }

        try
        {
            var settings =
                PlayFabMultiplayerAgentAPI.GetConfigSettings();

            if (settings == null)
            {
                Debug.LogError(
                    "[Dedicated] Configurações GSDK indisponíveis.");
                return;
            }

            if (!settings.TryGetValue(
                    PlayFabMultiplayerAgentAPI.SessionIdKey,
                    out string matchId)
                || string.IsNullOrWhiteSpace(matchId))
            {
                Debug.LogError(
                    "[Dedicated] Alocação sem SessionId válido.");
                return;
            }

            settings.TryGetValue(
                PlayFabMultiplayerAgentAPI.PublicIpV4AddressKey,
                out string publicIp);
            settings.TryGetValue(
                PlayFabMultiplayerAgentAPI.RegionKey,
                out string playFabRegion);

            ushort port = fallbackGamePort;

            if (settings.TryGetValue(
                    "game_port", out string suppliedPort))
            {
                if (ushort.TryParse(
                        suppliedPort, out ushort parsedPort)
                    && parsedPort > 0)
                {
                    port = parsedPort;
                }
                else
                {
                    Debug.LogWarning(
                        "[Dedicated] Porta inválida: " + suppliedPort);
                }
            }
            else
            {
                Debug.LogWarning(
                    "[Dedicated] game_port não encontrado nas " +
                    "configurações. Usando fallback: " + port);
            }

            Debug.Log(
                $"[Dedicated] Alocado. SessionId={matchId}, " +
                $"Port={port}");

            await StartServerAsync(
                matchId.Trim(), port, publicIp,
                ToPhotonRegion(playFabRegion));
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Dedicated] Erro no callback de alocação: " + ex);
        }
    }

    private void OnPlayFabShutdown()
    {
        Debug.Log("[Dedicated] Encerramento solicitado pelo PlayFab.");
        Application.Quit();
    }

    private void OnPlayFabAgentError(string message)
    {
        Debug.LogError("[Dedicated] Erro GSDK: " + message);
    }

    private void OnDestroy()
    {
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback -=
            OnPlayFabServerActive;

        PlayFabMultiplayerAgentAPI.OnShutDownCallback -=
            OnPlayFabShutdown;

        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback -=
            OnPlayFabAgentError;
    }
#endif

    private async Task StartServerAsync(
        string matchId, ushort port, string publicIp = null,
        string photonRegion = null)
    {
        if (starting || started)
            return;

        starting = true;

        try
        {
            if (runnerPrefab == null)
            {
                Debug.LogError(
                    "[Dedicated] NetworkRunner prefab não configurado.");
                return;
            }

            if (string.IsNullOrWhiteSpace(gameScenePath))
            {
                Debug.LogError(
                    "[Dedicated] Caminho da cena de jogo não foi configurado.");
                return;
            }

            activeRunner = Instantiate(runnerPrefab);
            activeRunner.name = "DedicatedFusionRunner";
            activeRunner.ProvideInput = false;

            DontDestroyOnLoad(activeRunner.gameObject);

            var sceneManager =
                activeRunner.GetComponent<NetworkSceneManagerDefault>();

            if (sceneManager == null)
            {
                sceneManager =
                    activeRunner.gameObject
                        .AddComponent<NetworkSceneManagerDefault>();
            }

            Debug.Log(
                $"[Dedicated] Criando sessão Photon: {matchId}");

            StartGameResult result =
                await activeRunner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Server,
                    SessionName = matchId,
                    PlayerCount = maximumPlayers,
                    Address = NetAddress.Any(port),
                    // PlayFab already owns the public IP/port mapping. Publishing
                    // it prevents Fusion from advertising the VM's private NIC.
                    CustomPublicAddress =
                        !string.IsNullOrWhiteSpace(publicIp)
                            ? NetAddress.CreateFromIpPort(publicIp, port)
                            : default,
                    CustomPhotonAppSettings =
                        CreatePhotonSettings(photonRegion),
                    SceneManager = sceneManager
                });

            if (!result.Ok)
            {
                Debug.LogError(
                    $"[Dedicated] Falha ao criar sessão {matchId}. " +
                    $"Reason={result.ShutdownReason}");

                Destroy(activeRunner.gameObject);
                activeRunner = null;
                return;
            }

            started = true;

            Debug.Log(
                $"[Dedicated] Sessão Photon criada: {matchId}");

            // Indexes differ between the Linux profile and a client/editor
            // profile. Sending the path lets Fusion resolve Game_Scene on each
            // peer rather than loading Boot_Scene at the same numeric index.
            activeRunner.LoadScene(
                SceneRef.FromPath(gameScenePath),
                LoadSceneMode.Single);

            // MPS marks the process Active as soon as it is allocated. If the
            // client times out before joining Fusion, exit rather than holding
            // that allocated server for hours; MPS replaces it with standby.
            if (Application.isBatchMode)
                StartCoroutine(ShutdownIfNoClientJoins());

            Debug.Log(
                "[Dedicated] Cena da partida solicitada.");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[Dedicated] Exceção ao iniciar Fusion: " + ex);

            if (activeRunner != null)
            {
                Destroy(activeRunner.gameObject);
                activeRunner = null;
            }
        }
        finally
        {
            starting = false;
        }
    }

    private IEnumerator ShutdownIfNoClientJoins()
    {
        yield return new WaitForSeconds(activeWithoutPlayersTimeoutSeconds);
        if (activeRunner == null || !activeRunner.IsRunning || HasConnectedClient())
            yield break;

        Debug.LogWarning("[Dedicated] Nenhum cliente entrou no tempo limite; encerrando alocação MPS.");
        Application.Quit();
    }

    private bool HasConnectedClient()
    {
        if (activeRunner == null) return false;
        foreach (PlayerRef player in activeRunner.ActivePlayers)
        {
            if (player != PlayerRef.None)
                return true;
        }
        // Fusion can briefly report an empty ActivePlayers list while the
        // client is changing to the network scene. A spawned player object is
        // definitive evidence that this is no longer an orphan allocation.
        return FindFirstObjectByType<FusionNetworkPlayer>() != null;
    }

    private static string ToPhotonRegion(string playFabRegion)
    {
        if (string.Equals(playFabRegion, "BrazilSouth",
            StringComparison.OrdinalIgnoreCase))
            return "sa";
        if (string.Equals(playFabRegion, "EastUs",
            StringComparison.OrdinalIgnoreCase))
            return "us";
        return null;
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
