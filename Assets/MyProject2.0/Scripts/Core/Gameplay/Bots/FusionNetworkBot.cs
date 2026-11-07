using Fusion;
using UnityEngine;

/// <summary>Small deterministic bot bridge for the Shared-mode multiplayer test.</summary>
public sealed class FusionNetworkBot : NetworkBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private BotController legacyBotController;
    [SerializeField] private PlayerDeath legacyDeath;
    [SerializeField] private Rigidbody body;
    [SerializeField] private float deathHeight = -5f;

    [Networked] public int BotSeed { get; private set; }
    [Networked, Capacity(24)] public string NetworkNickname { get; private set; }
    [Networked] private int DecisionRound { get; set; }
    [Networked] private int LaunchedRound { get; set; }
    [Networked] private NetworkBool Eliminated { get; set; }

    private FusionRoundController rounds;

    public void SetSeed(int seed) => BotSeed = seed;

    public override void Spawned()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (legacyBotController == null) legacyBotController = GetComponent<BotController>();
        if (legacyDeath == null) legacyDeath = GetComponent<PlayerDeath>();
        if (body == null) body = GetComponent<Rigidbody>();
        if (legacyBotController != null) legacyBotController.enabled = false;
        if (legacyDeath != null) legacyDeath.enabled = false;
        rounds = FindFirstObjectByType<FusionRoundController>();
        FusionArenaMatchController match = FindFirstObjectByType<FusionArenaMatchController>();
        playerController?.SetConfiguration(match != null ? match.PlayerConfig : null);
        if (Object.HasStateAuthority && string.IsNullOrWhiteSpace(NetworkNickname)) NetworkNickname = CreateNickname();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || Eliminated) return;
        if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
        if (rounds == null) return;

        if (body != null && transform.position.y <= deathHeight)
        {
            Eliminated = true;
            body.isKinematic = true;
            return;
        }

        if (rounds.Phase == FusionRoundPhase.Choosing && DecisionRound != rounds.RoundIndex)
        {
            DecisionRound = rounds.RoundIndex;
            Vector3 direction = CreateDirection(rounds.RoundIndex);
            float charge = 2f + Mathf.Abs((BotSeed * 17 + rounds.RoundIndex * 7) % 7);
            playerController?.ApplyNetworkSelection(direction, charge);
        }

        if (rounds.Phase == FusionRoundPhase.Moving && LaunchedRound != rounds.RoundIndex)
        {
            LaunchedRound = rounds.RoundIndex;
            playerController?.LaunchFromNetwork();
        }
    }

    public override void Render()
    {
        PlayerNickname nickname = GetComponent<PlayerNickname>();
        if (nickname != null && !string.IsNullOrWhiteSpace(NetworkNickname)) nickname.SetNickname(NetworkNickname);
    }

    private Vector3 CreateDirection(int round)
    {
        float angle = Mathf.Repeat(BotSeed * 53.137f + round * 71.0f, 360f) * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    private string CreateNickname()
    {
        string[] names = { "Breu", "Caju", "Lago", "Tuca", "Nina", "Pingo", "Joca", "Duda" };
        return names[Mathf.Abs(BotSeed) % names.Length];
    }
}
