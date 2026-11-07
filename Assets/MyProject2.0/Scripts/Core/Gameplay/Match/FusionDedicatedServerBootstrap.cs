using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_PLAYFABSERVER_API
using PlayFab;
#endif

/// <summary>
/// Minimal headless entry point for a Fusion dedicated build. PlayFab MPS (or a
/// local launcher) supplies CAPIVARADA_MATCH_ID. MPS/GSDK integration can own
/// allocation readiness separately; this component owns only the Fusion room.
/// </summary>
public sealed class FusionDedicatedServerBootstrap : MonoBehaviour
{
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private string gameScenePath = "Assets/MyProject2.0/Scenes/Game_Scene.unity";
    [SerializeField, Min(1)] private int maximumPlayers = 8;
    private bool starting;

    private async void Start()
    {
        if (!Application.isBatchMode)
        {
            enabled = false;
            return;
        }

#if ENABLE_PLAYFABSERVER_API
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnPlayFabServerActive;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnPlayFabShutdown;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnPlayFabAgentError;
        PlayFabMultiplayerAgentAPI.Start();
        StartCoroutine(ReadyForPlayFabAllocation());
#else
        await StartLocalOrExternalServerAsync();
#endif
    }

    private async Task StartLocalOrExternalServerAsync()
    {
        string matchId = Environment.GetEnvironmentVariable("CAPIVARADA_MATCH_ID");
        if (string.IsNullOrWhiteSpace(matchId))
        {
            Debug.LogError("[FusionDedicatedServerBootstrap] CAPIVARADA_MATCH_ID não foi definido. Para teste local, defina essa variável antes de iniciar o servidor.", this);
            return;
        }
        await StartServerAsync(matchId.Trim());
    }

#if ENABLE_PLAYFABSERVER_API
    private System.Collections.IEnumerator ReadyForPlayFabAllocation()
    {
        // Aguarda um frame para o agente do GSDK completar sua inicialização
        // antes de anunciar que este processo está disponível para alocação.
        yield return null;
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
        Debug.Log("[FusionDedicatedServerBootstrap] GSDK em StandingBy; aguardando alocação do PlayFab MPS.", this);
    }

    private async void OnPlayFabServerActive()
    {
        var settings = PlayFabMultiplayerAgentAPI.GetConfigSettings();
        if (!settings.TryGetValue(PlayFabMultiplayerAgentAPI.SessionIdKey, out string matchId) || string.IsNullOrWhiteSpace(matchId))
        {
            Debug.LogError("[FusionDedicatedServerBootstrap] O GSDK ativou o servidor sem SessionId.", this);
            return;
        }
        await StartServerAsync(matchId);
    }

    private void OnPlayFabShutdown()
    {
        Debug.Log("[FusionDedicatedServerBootstrap] PlayFab solicitou encerramento do servidor.", this);
        Application.Quit();
    }

    private void OnPlayFabAgentError(string message) => Debug.LogError($"[FusionDedicatedServerBootstrap] Erro do GSDK: {message}", this);

    private void OnDestroy()
    {
        PlayFabMultiplayerAgentAPI.OnServerActiveCallback -= OnPlayFabServerActive;
        PlayFabMultiplayerAgentAPI.OnShutDownCallback -= OnPlayFabShutdown;
        PlayFabMultiplayerAgentAPI.OnAgentErrorCallback -= OnPlayFabAgentError;
    }
#endif

    private async Task StartServerAsync(string matchId)
    {
        if (starting) return;
        starting = true;
        if (runnerPrefab == null)
        {
            Debug.LogError("[FusionDedicatedServerBootstrap] Configure o prefab NetworkRunner.", this);
            starting = false;
            return;
        }
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(gameScenePath);
        if (buildIndex < 0)
        {
            Debug.LogError($"[FusionDedicatedServerBootstrap] A cena '{gameScenePath}' não está no Build Profile.", this);
            starting = false;
            return;
        }

        NetworkRunner runner = Instantiate(runnerPrefab);
        runner.name = "DedicatedFusionRunner";
        runner.ProvideInput = false;
        DontDestroyOnLoad(runner.gameObject);
        NetworkSceneManagerDefault sceneManager = runner.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null) sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        StartGameResult result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Server,
            SessionName = matchId,
            PlayerCount = maximumPlayers,
            SceneManager = sceneManager
        });
        if (!result.Ok)
        {
            Debug.LogError($"[FusionDedicatedServerBootstrap] Falha ao iniciar '{matchId}': {result.ShutdownReason}", this);
            Destroy(runner.gameObject);
            starting = false;
            return;
        }

        runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
        Debug.Log($"[FusionDedicatedServerBootstrap] Servidor dedicado iniciado para a partida '{matchId}'.", this);
    }
}
