using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhotonLobbyManager : MonoBehaviour
{
    public static PhotonLobbyManager Instance { get; private set; }

    [Header("Network")]
    [SerializeField] private int maxPlayers = 8;

    private NetworkRunner runner;

    public bool IsConnected { get; private set; } 

    public NetworkRunner Runner => runner;

    public event Action OnLobbyConnected;
    public event Action<string> OnLobbyError;

    private const string LobbySessionName = "RivalsArena-Lobby";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private async void Start()
    {
        await ConnectToLobby();
    }

    public async Task ConnectToLobby()
    {
        if (IsConnected)
            return;

        Debug.Log("[Fusion] Conectando ao lobby...");

        runner = gameObject.AddComponent<NetworkRunner>();
        
        NetworkSceneManagerDefault sceneManager =
            gameObject.AddComponent<NetworkSceneManagerDefault>();
        
        runner.ProvideInput = true;
        

        var result = await runner.StartGame(
            new StartGameArgs
            {
                GameMode = GameMode.Shared,

                SessionName = LobbySessionName,

                PlayerCount = maxPlayers,

                SceneManager = sceneManager
            }
        );

        if (!result.Ok)
        {
            Debug.LogError(
                $"[Fusion] Erro ao conectar: {result.ShutdownReason}"
            );

            OnLobbyError?.Invoke(
                result.ShutdownReason.ToString()
            );

            return;
        }

        IsConnected = true;

        Debug.Log("=================================");
        Debug.Log("[Fusion] CONECTADO AO LOBBY");
        Debug.Log($"[Fusion] Session: {runner.SessionInfo.Name}");
        Debug.Log($"[Fusion] Local PlayerRef: {runner.LocalPlayer}");
        Debug.Log($"[Fusion] Players: {runner.ActivePlayers.Count()}");
        Debug.Log("=================================");

        OnLobbyConnected?.Invoke();
    }
    
    public void LoadGameScene()
    {
        if (!IsConnected)
            return;

        if (!runner.IsSceneAuthority)
        {
            Debug.Log(
                "[Fusion] Este jogador não possui autoridade " +
                "para trocar a cena."
            );

            return;
        }

        int sceneIndex =
            SceneUtility.GetBuildIndexByScenePath(
                "Assets/Scenes/Game.unity"
            );

        if (sceneIndex < 0)
        {
            Debug.LogError(
                "[Fusion] Game.unity não está no Build Settings."
            );

            return;
        }

        Debug.Log(
            "[Fusion] Carregando Game para todos..."
        );

        runner.LoadScene(
            SceneRef.FromIndex(sceneIndex),
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}