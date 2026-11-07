using Fusion;
using UnityEngine;

/// <summary>Small deterministic bot bridge for the Shared-mode multiplayer test.</summary>
public sealed class FusionNetworkBot : NetworkBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private BotController legacyBotController;
    [SerializeField] private PlayerDeath legacyDeath;
    [SerializeField] private Rigidbody body;

    [Networked] public int BotSeed { get; private set; }
    [Networked, Capacity(24)] public string NetworkNickname { get; private set; }
    [Networked] private Vector3 AimDirection { get; set; }
    [Networked] private float Charge { get; set; }
    [Networked] private int DecisionRound { get; set; }
    [Networked] private int LaunchedRound { get; set; }
    [Networked] private NetworkBool Eliminated { get; set; }

    private FusionRoundController rounds;
    private FusionArenaMatchController match;
    private FusionRoundPhase presentedPhase = (FusionRoundPhase)(-1);
    private int presentedRound = -1;
    private bool wasEliminated;

    public bool IsEliminated => Eliminated;

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
        match = FindFirstObjectByType<FusionArenaMatchController>();
        playerController?.SetConfiguration(match != null ? match.PlayerConfig : null);
        if (Object.HasStateAuthority && string.IsNullOrWhiteSpace(NetworkNickname)) NetworkNickname = CreateNickname();
        if (Object.HasStateAuthority && AimDirection.sqrMagnitude < 0.001f) AimDirection = transform.forward;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || Eliminated) return;
        if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
        if (rounds == null) return;

        if (body != null && transform.position.y <= (match != null ? match.DeathHeight : 2.87f))
        {
            Eliminated = true;
            body.isKinematic = true;
            return;
        }

        if (rounds.Phase == FusionRoundPhase.Choosing && DecisionRound != rounds.RoundIndex)
        {
            DecisionRound = rounds.RoundIndex;
            playerController?.ResetRound();
            AimDirection = CreateDirection(rounds.RoundIndex);
            Charge = 2f + Mathf.Abs((BotSeed * 17 + rounds.RoundIndex * 7) % 7);
            playerController?.ApplyNetworkSelection(AimDirection, Charge);
        }

        if (rounds.Phase == FusionRoundPhase.Moving && LaunchedRound != rounds.RoundIndex)
        {
            LaunchedRound = rounds.RoundIndex;
            playerController?.LaunchFromNetwork();
        }
    }

    /// <summary>See FusionNetworkPlayer.RequestCollisionResponse.</summary>
    public void RequestCollisionResponse(Vector3 direction, float velocityChange)
    {
        if (velocityChange <= 0f) return;
        RpcApplyCollisionResponse(direction, velocityChange);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcApplyCollisionResponse(Vector3 direction, float velocityChange)
    {
        if (Eliminated || body == null) return;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return;
        direction.Normalize();

        Vector3 velocity = body.linearVelocity;
        float alongPush = Vector3.Dot(new Vector3(velocity.x, 0f, velocity.z), direction);
        if (alongPush < 0f) velocity -= direction * alongPush;
        body.linearVelocity = velocity;
        body.AddForce(direction * velocityChange, ForceMode.VelocityChange);
    }

    public override void Render()
    {
        playerController?.ApplyNetworkSelection(AimDirection, Charge);
        PlayerNickname nickname = GetComponent<PlayerNickname>();
        if (nickname != null && !string.IsNullOrWhiteSpace(NetworkNickname)) nickname.SetNickname(NetworkNickname);
        if (!wasEliminated && Eliminated)
        {
            wasEliminated = true;
            SetVisible(false);
        }
        PresentRoundPhase();
    }

    private void SetVisible(bool value)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = value;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = value;
    }

    private void PresentRoundPhase()
    {
        if (rounds == null || playerController == null) return;
        if (presentedPhase == rounds.Phase && presentedRound == rounds.RoundIndex) return;

        presentedPhase = rounds.Phase;
        presentedRound = rounds.RoundIndex;
        if (rounds.Phase == FusionRoundPhase.Revealing)
        {
            playerController.ShowIndicator();
            playerController.BeginRevealPresentation(rounds.RevealDuration, Object.HasStateAuthority);
        }
        else
        {
            playerController.HideIndicator();
            playerController.StopMovementAnimation();
        }
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
