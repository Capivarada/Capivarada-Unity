using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MatchmakingManager matchmakingManager;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private PlayerCameraController cameraController;
    private ArenaBounds arenaBounds;
    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject botPrefab;

    [Header("Match Configuration")]
    [SerializeField] private int maxPlayers = 8;
    
    [SerializeField]
    private MatchUI matchUI;

    private GameObject humanPlayer;

    private void Awake()
    {
        ValidateManagers();
    }

    private void Start()
    {
        arenaBounds =
            FindObjectOfType<ArenaBounds>();

        if (arenaBounds == null)
        {
            Debug.LogError(
                "[GameManager] ArenaBounds não encontrado na arena!"
            );
        }
        
        StartMatchFlow();
    }

    private void ValidateManagers()
    {
        if (matchUI == null)
        {
            matchUI =
                FindObjectOfType<MatchUI>();
        }
        if (matchmakingManager == null)
        {
            matchmakingManager = GetComponent<MatchmakingManager>();

            if (matchmakingManager == null)
            {
                Debug.LogError("[GameManager] MatchmakingManager não encontrado!");
            }
        }

        if (spawnManager == null)
        {
            spawnManager = GetComponent<SpawnManager>();

            if (spawnManager == null)
            {
                Debug.LogError("[GameManager] SpawnManager não encontrado!");
            }
        }

        if (matchManager == null)
        {
            matchManager = GetComponent<MatchManager>();

            if (matchManager == null)
            {
                Debug.LogError("[GameManager] MatchManager não encontrado!");
            }
        }

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<PlayerCameraController>();

            if (cameraController == null)
            {
                Debug.LogWarning("[GameManager] PlayerCameraController não encontrado!");
            }
        }
    }

    private void StartMatchFlow()
    {
        if (matchmakingManager == null)
        {
            Debug.LogError("[GameManager] Não posso iniciar fluxo de partida sem MatchmakingManager.");
            return;
        }

        spawnManager.ResetSpawns();
        matchManager.Reset();

        matchmakingManager.OnStateChanged += HandleMatchmakingStateChanged;
        matchmakingManager.StartSearching();
    }

    private void HandleMatchmakingStateChanged(MatchmakingState state)
    {
        if (state == MatchmakingState.Ready)
        {
            StartCoroutine(SpawnPlayersCoroutine());
        }
    }

    private IEnumerator SpawnPlayersCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[GameManager] Spawning players...");

        SpawnHumanPlayer();

        yield return new WaitForSeconds(0.3f);

        int botCount = maxPlayers - 1;
        SpawnBots(botCount);

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[MATCH] {matchManager.AllPlayers.Count} participantes criados.");

        matchManager.StartCountdown();
    }

    private void SpawnHumanPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] Player Prefab não configurado.");
            return;
        }

        Transform spawnPoint = spawnManager.GetAvailableSpawnPoint();

        if (spawnPoint == null)
        {
            Debug.LogError("[GameManager] Nenhum spawn point disponível para o jogador humano.");
            return;
        }

        humanPlayer = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        humanPlayer.name = "Player";

        PlayerNickname nickname = humanPlayer.GetComponent<PlayerNickname>();
        if (nickname != null)
        {
            nickname.SetNickname("Você");
        }

        matchManager.RegisterPlayer(humanPlayer);
        matchManager.SetHumanPlayer(humanPlayer);
        matchUI.SetHumanPlayer(humanPlayer);

        if (cameraController != null)
        {
            Transform cameraTarget = humanPlayer.transform.Find("CameraTarget");

            if (cameraTarget == null)
            {
                cameraTarget = humanPlayer.GetComponentInChildren<PlayerCameraLook>()?.transform;
            }

            if (cameraTarget != null)
            {
                cameraController.SetTarget(cameraTarget);
            }
        }

        Debug.Log($"[GameManager] Jogador humano spawned em {spawnPoint.name}");
    }

    private void SpawnBots(int count)
    {
        if (botPrefab == null)
        {
            Debug.LogError("[GameManager] Bot Prefab não configurado.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Transform spawnPoint = spawnManager.GetAvailableSpawnPoint();

            if (spawnPoint == null)
            {
                Debug.LogWarning("[GameManager] Sem spawn points disponíveis para bot " + (i + 1));
                break;
            }

            GameObject bot = Instantiate(
                botPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            bot.name = $"Bot {i + 1:D2}";

            PlayerNickname nickname = bot.GetComponent<PlayerNickname>();
            if (nickname != null)
            {
                nickname.SetNickname(bot.name);
            }

            HumanPlayerController humanController = bot.GetComponent<HumanPlayerController>();
            if (humanController != null)
            {
                Destroy(humanController);
            }

            PlayerInput playerInput = bot.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                Destroy(playerInput);
            }

            BotController botController = bot.GetComponent<BotController>();
            if (botController == null)
            {
                botController = bot.AddComponent<BotController>();
            }
            
            if (arenaBounds != null)
            {
                botController.SetArenaBounds(
                    arenaBounds
                );
            }

            matchManager.RegisterPlayer(bot);

            Debug.Log($"[GameManager] Bot {i + 1} spawned em {spawnPoint.name}");
        }
    }

    private void OnDestroy()
    {
        if (matchmakingManager != null)
        {
            matchmakingManager.OnStateChanged -= HandleMatchmakingStateChanged;
        }
    }
}