using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private async void Start()
    {
        if (!Application.isBatchMode)
        {
            enabled = false;
            return;
        }

        string matchId = Environment.GetEnvironmentVariable("CAPIVARADA_MATCH_ID");
        if (string.IsNullOrWhiteSpace(matchId))
        {
            Debug.LogError("[FusionDedicatedServerBootstrap] CAPIVARADA_MATCH_ID não foi definido. O servidor não iniciará uma partida sem MatchId.", this);
            return;
        }

        await StartServerAsync(matchId.Trim());
    }

    private async Task StartServerAsync(string matchId)
    {
        if (runnerPrefab == null)
        {
            Debug.LogError("[FusionDedicatedServerBootstrap] Configure o prefab NetworkRunner.", this);
            return;
        }
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(gameScenePath);
        if (buildIndex < 0)
        {
            Debug.LogError($"[FusionDedicatedServerBootstrap] A cena '{gameScenePath}' não está no Build Profile.", this);
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
            return;
        }

        runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
        Debug.Log($"[FusionDedicatedServerBootstrap] Servidor dedicado iniciado para a partida '{matchId}'.", this);
    }
}
