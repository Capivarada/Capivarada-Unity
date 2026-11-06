using System.Collections;
using UnityEngine;

/// <summary>
/// Central manager for the game.
/// Orchestrates game initialization, player spawning, and manager configuration.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager Instance { get; private set; }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Configuration

    [Header("Configuration")]
    [SerializeField] private PlayerConfig playerConfig;
    [SerializeField] private MatchConfig matchConfig;

    #endregion

    #region Managers

    [Header("Managers")]
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private PlayerCameraController cameraController;
    [SerializeField] private MatchUI matchUI;

    private ArenaBounds arenaBounds;

    #endregion

    #region Prefabs

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject botPrefab;

    #endregion

    #region Player Setup

    [Header("Player Setup")]
    [SerializeField] private int maxPlayers = 8;

    private GameObject humanPlayer;

    #endregion

    #region Properties

    public PlayerConfig PlayerConfig => playerConfig;
    public MatchConfig MatchConfig => matchConfig;
    public int MaxPlayers => maxPlayers;

    #endregion

    #region Initialization

    private void Awake()
    {
        ValidateConfiguration();
        ValidateManagers();

    }

    private void Start()
    {
        // This legacy manager owns the fully local simulation. It remains the
        // implementation for Practice_Scene only; an online Fusion scene uses
        // FusionArenaMatchController instead and must not spawn local clones.
        if (!MatchParticipationContext.IsPractice)
        {
            Debug.Log("[GameManager] Online match detected. Local practice simulation is disabled.");
            return;
        }

        arenaBounds = FindObjectOfType<ArenaBounds>();

        if (arenaBounds == null)
        {
            Debug.LogError("[GameManager] ArenaBounds not found in the scene!");
        }

        InitializeGame();
    }

    #endregion

    #region Validation

    private void ValidateConfiguration()
    {
        if (playerConfig == null)
        {
            Debug.LogError("[GameManager] PlayerConfig not assigned!");
        }

        if (matchConfig == null)
        {
            Debug.LogError("[GameManager] MatchConfig not assigned!");
        }
    }

    private void ValidateManagers()
    {
        if (spawnManager == null)
        {
            spawnManager = GetComponent<SpawnManager>();
            if (spawnManager == null)
                Debug.LogError("[GameManager] SpawnManager not found!");
        }

        if (matchManager == null)
        {
            matchManager = GetComponent<MatchManager>();
            if (matchManager == null)
                Debug.LogError("[GameManager] MatchManager not found!");
        }

        if (roundManager == null)
        {
            roundManager = GetComponent<RoundManager>();
            if (roundManager == null)
                Debug.LogError("[GameManager] RoundManager not found!");
        }

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<PlayerCameraController>();
            if (cameraController == null)
                Debug.LogWarning("[GameManager] PlayerCameraController not found!");
        }

        if (matchUI == null)
        {
            matchUI = FindObjectOfType<MatchUI>();
            if (matchUI == null)
                Debug.LogError("[GameManager] MatchUI not found!");
        }
    }

    #endregion

    #region Initialization Flow

    private void InitializeGame()
    {
        if (spawnManager == null || matchManager == null || roundManager == null || playerConfig == null || matchConfig == null)
        {
            Debug.LogError("[GameManager] Cannot initialize game without required managers.");
            return;
        }

        spawnManager.ResetSpawns();
        matchManager.Reset();

        StartCoroutine(SpawnPlayersCoroutine());
    }

    private IEnumerator SpawnPlayersCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[GameManager] Spawning players...");

        SpawnHumanPlayer();

        yield return new WaitForSeconds(0.3f);

        int botCount = Mathf.Max(0, maxPlayers - 1);
        SpawnBots(botCount);

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[GameManager] {matchManager.AllPlayers.Count} players spawned.");

        if (matchUI != null)
        {
            matchUI.SetTotalPlayers(matchManager.AllPlayers.Count);
        }

        // Start match flow via MatchManager
        if (matchManager != null)
        {
            matchManager.StartMatch();
        }
    }

    #endregion

    #region Player Spawning

    private void SpawnHumanPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] Player Prefab not assigned.");
            return;
        }

        Transform spawnPoint = spawnManager.GetAvailableSpawnPoint();

        if (spawnPoint == null)
        {
            Debug.LogError("[GameManager] No spawn point available for human player.");
            return;
        }

        humanPlayer = Instantiate(
            playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        humanPlayer.name = "Player";

        ApplyCustomization(humanPlayer, null);

        // Set nickname
        PlayerNickname nickname = humanPlayer.GetComponent<PlayerNickname>();
        if (nickname != null)
        {
            nickname.SetNickname("Você");
        }

        // Ensure PlayerController has config reference (if needed)
        PlayerController playerController = humanPlayer.GetComponent<PlayerController>();
        if (playerController != null && playerConfig != null)
        {
            // PlayerController can now access via GameManager if needed
            // Or we can add a method to set config
        }

        // Ensure PlayerMatchData exists
        PlayerMatchData matchData = humanPlayer.GetComponent<PlayerMatchData>();
        if (matchData == null)
        {
            matchData = humanPlayer.AddComponent<PlayerMatchData>();
        }

        // Register with match manager
        matchManager.RegisterPlayer(humanPlayer);
        matchManager.SetHumanPlayer(humanPlayer);

        // Setup UI references
        if (matchUI != null)
        {
            matchUI.SetHumanPlayer(humanPlayer);
            matchUI.SetPlayer(humanPlayer);
        }

        // Setup camera
        if (cameraController != null)
        {
            Transform cameraTarget = humanPlayer.transform.Find("CameraTarget");

            if (cameraTarget == null)
            {
                PlayerCameraLook cameraLook = humanPlayer.GetComponentInChildren<PlayerCameraLook>();
                if (cameraLook != null)
                {
                    cameraTarget = cameraLook.transform;
                }
            }

            if (cameraTarget != null)
            {
                cameraController.SetTarget(cameraTarget);
            }
        }

        Debug.Log($"[GameManager] Human player spawned at {spawnPoint.name}");
    }

    private void SpawnBots(int count)
    {
        if (botPrefab == null)
        {
            Debug.LogError("[GameManager] Bot Prefab not assigned.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Transform spawnPoint = spawnManager.GetAvailableSpawnPoint();

            if (spawnPoint == null)
            {
                Debug.LogWarning($"[GameManager] No spawn point available for bot {i + 1}");
                break;
            }

            GameObject bot = Instantiate(
                botPrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            bot.name = $"Bot {i + 1:D2}";

            ApplyCustomization(bot, null);

            // Set nickname
            PlayerNickname nickname = bot.GetComponent<PlayerNickname>();
            if (nickname != null)
            {
                nickname.SetNickname(bot.name);
            }

            // Remove human player components
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

            // Ensure BotController exists
            BotController botController = bot.GetComponent<BotController>();
            if (botController == null)
            {
                botController = bot.AddComponent<BotController>();
            }

            // Pass references
            if (arenaBounds != null)
            {
                botController.SetArenaBounds(arenaBounds);
            }

            // Ensure PlayerMatchData exists
            PlayerMatchData matchData = bot.GetComponent<PlayerMatchData>();
            if (matchData == null)
            {
                matchData = bot.AddComponent<PlayerMatchData>();
            }

            // Register with match manager
            matchManager.RegisterPlayer(bot);

            Debug.Log($"[GameManager] Bot {i + 1} spawned at {spawnPoint.name}");
        }
    }

    #endregion

    private static void ApplyCustomization(
        GameObject player,
        PlayerCustomization customization)
    {
        if (player == null || customization == null)
            return;

        PlayerCustomizationApplier applier = player.GetComponent<PlayerCustomizationApplier>();
        if (applier == null)
            applier = player.GetComponentInChildren<PlayerCustomizationApplier>();

        // The original Player and Bot prefabs predate customization. Add the
        // applier at runtime so their seven SkinnedMeshRenderer materials are
        // still customized without altering their visual prefab hierarchy.
        if (applier == null)
            applier = player.AddComponent<PlayerCustomizationApplier>();

        applier.Apply(customization);
    }

    #region Cleanup

    private void OnDestroy()
    {
        // Cleanup if needed
    }

    #endregion
}
