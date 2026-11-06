using Fusion;
using UnityEngine;

/// <summary>
/// Bridges the existing capybara gameplay components into a Fusion player object.
/// The player's Shared state authority publishes only direction and charge; the
/// scene round controller decides when the already selected impulse is launched.
/// Add NetworkTransform and NetworkRigidbody3D on the same prefab for movement.
/// </summary>
public sealed class FusionNetworkPlayer : NetworkBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInput localInput;
    [SerializeField] private HumanPlayerController legacyHumanController;
    [SerializeField] private PlayerDeath legacyDeath;
    [SerializeField] private Rigidbody body;
    [SerializeField] private float deathHeight = -5f;

    [Networked] public Vector3 AimDirection { get; private set; }
    [Networked] public float Charge { get; private set; }
    [Networked] public NetworkBool IsEliminated { get; private set; }
    [Networked, Capacity(32)] public string NetworkNickname { get; private set; }
    [Networked] private int LastLaunchedRound { get; set; }

    private FusionRoundController rounds;
    private Camera localCamera;
    private bool wasEliminated;

    public override void Spawned()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (localInput == null) localInput = GetComponent<PlayerInput>();
        if (legacyHumanController == null) legacyHumanController = GetComponent<HumanPlayerController>();
        if (legacyDeath == null) legacyDeath = GetComponent<PlayerDeath>();
        if (body == null) body = GetComponent<Rigidbody>();
        if (legacyHumanController != null) legacyHumanController.enabled = false;
        if (legacyDeath != null) legacyDeath.enabled = false;
        if (localInput != null) localInput.enabled = Object.HasInputAuthority;
        rounds = FindFirstObjectByType<FusionRoundController>();

        if (Object.HasStateAuthority)
        {
            if (AimDirection.sqrMagnitude < 0.001f) AimDirection = transform.forward;
            if (string.IsNullOrWhiteSpace(NetworkNickname)) NetworkNickname = ResolveLocalNickname();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
        if (rounds == null || !Object.HasStateAuthority || IsEliminated) return;

        if (body != null && transform.position.y <= deathHeight)
        {
            IsEliminated = true;
            body.isKinematic = true;
            return;
        }

        if (rounds.Phase == FusionRoundPhase.Choosing)
            ReadLocalChoice();

        if (rounds.Phase == FusionRoundPhase.Moving && LastLaunchedRound != rounds.RoundIndex)
        {
            LastLaunchedRound = rounds.RoundIndex;
            playerController?.ApplyNetworkSelection(AimDirection, Charge);
            playerController?.LaunchFromNetwork();
        }
    }

    public override void Render()
    {
        playerController?.ApplyNetworkSelection(AimDirection, Charge);
        PlayerNickname nickname = GetComponent<PlayerNickname>();
        if (nickname != null && !string.IsNullOrWhiteSpace(NetworkNickname)) nickname.SetNickname(NetworkNickname);
        if (!wasEliminated && IsEliminated)
        {
            wasEliminated = true;
            SetVisible(false);
        }
    }

    private void ReadLocalChoice()
    {
        if (!Object.HasInputAuthority || localInput == null) return;
        if (localCamera == null) localCamera = Camera.main;
        if (localCamera != null)
        {
            Vector3 direction = localCamera.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) AimDirection = direction.normalized;
        }

        if (localInput.AddChargePressed) Charge += 0.5f;
        if (localInput.RemoveChargePressed) Charge -= 0.5f;
        playerController?.ApplyNetworkSelection(AimDirection, Charge);
        Charge = playerController != null ? playerController.CurrentCharge : Mathf.Max(0f, Charge);
    }

    private void SetVisible(bool value)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = value;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = value;
    }

    private static string ResolveLocalNickname()
    {
        PlayFabAuthManager authentication = ServiceHub.Instance != null ? ServiceHub.Instance.Authentication : null;
        return authentication != null && !string.IsNullOrWhiteSpace(authentication.Nickname)
            ? authentication.Nickname
            : "Capivara";
    }
}
