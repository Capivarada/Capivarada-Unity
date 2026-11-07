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

    [Networked] public Vector3 AimDirection { get; private set; }
    [Networked] public float Charge { get; private set; }
    [Networked] public NetworkBool IsEliminated { get; private set; }
    [Networked, Capacity(32)] public string NetworkNickname { get; private set; }
    [Networked] private int LastLaunchedRound { get; set; }

    private FusionRoundController rounds;
    private FusionArenaMatchController match;
    private Camera localCamera;
    private bool wasEliminated;
    private Coroutine attachPresentationCoroutine;
    private int preparedRound = -1;
    private FusionRoundPhase presentedPhase = (FusionRoundPhase)(-1);
    private int presentedRound = -1;

    public override void Spawned()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (localInput == null) localInput = GetComponent<PlayerInput>();
        if (legacyHumanController == null) legacyHumanController = GetComponent<HumanPlayerController>();
        if (legacyDeath == null) legacyDeath = GetComponent<PlayerDeath>();
        if (body == null) body = GetComponent<Rigidbody>();
        if (legacyHumanController != null) legacyHumanController.enabled = false;
        if (legacyDeath != null) legacyDeath.enabled = false;
        // Shared Authority uses state authority as the local player's ownership.
        // InputAuthority is not assigned the same way as Client/Server sessions.
        bool isLocalPlayer = Object.HasStateAuthority;
        if (localInput != null) localInput.enabled = isLocalPlayer;
        PlayerCameraLook cameraLook = GetComponentInChildren<PlayerCameraLook>(true);
        if (cameraLook != null) cameraLook.enabled = isLocalPlayer;
        rounds = FindFirstObjectByType<FusionRoundController>();
        match = FindFirstObjectByType<FusionArenaMatchController>();
        playerController?.SetConfiguration(match != null ? match.PlayerConfig : null);

        if (Object.HasStateAuthority)
        {
            if (AimDirection.sqrMagnitude < 0.001f) AimDirection = transform.forward;
            if (string.IsNullOrWhiteSpace(NetworkNickname)) NetworkNickname = ResolveLocalNickname();
            attachPresentationCoroutine = StartCoroutine(AttachLocalPresentation());
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
        if (rounds == null || !Object.HasStateAuthority || IsEliminated) return;

        if (body != null && transform.position.y <= (match != null ? match.DeathHeight : 2.87f))
        {
            IsEliminated = true;
            body.isKinematic = true;
            return;
        }

        if (rounds.Phase == FusionRoundPhase.Choosing)
        {
            if (preparedRound != rounds.RoundIndex)
            {
                preparedRound = rounds.RoundIndex;
                Charge = 0f;
                playerController?.ResetRound();
            }
            ReadLocalChoice();
        }
        else
        {
            localInput?.ClearPendingChargePresses();
        }

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
        PresentRoundPhase();
        PlayerNickname nickname = GetComponent<PlayerNickname>();
        if (nickname != null && !string.IsNullOrWhiteSpace(NetworkNickname)) nickname.SetNickname(NetworkNickname);
        if (!wasEliminated && IsEliminated)
        {
            wasEliminated = true;
            SetVisible(false);
            if (Object.HasStateAuthority)
            {
                PlayerCameraLook cameraLook = GetComponentInChildren<PlayerCameraLook>(true);
                if (cameraLook != null) cameraLook.enabled = false;
                FindFirstObjectByType<MatchUI>()?.HandleNetworkLocalElimination(gameObject);
            }
        }
    }

    private void ReadLocalChoice()
    {
        if (!Object.HasStateAuthority || localInput == null) return;
        if (localCamera == null) localCamera = Camera.main;
        if (localCamera != null)
        {
            Vector3 direction = localCamera.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) AimDirection = direction.normalized;
        }

        playerController?.PreviewLocalAimRotation();

        int addPresses = localInput.ConsumeAddChargePresses();
        int removePresses = localInput.ConsumeRemoveChargePresses();
        Charge += addPresses * 0.5f;
        Charge -= removePresses * 0.5f;
        playerController?.ApplyNetworkSelection(AimDirection, Charge);
        Charge = playerController != null ? playerController.CurrentCharge : Mathf.Max(0f, Charge);
        if (playerController != null)
        {
            if (Charge > 0f) playerController.ShowIndicator();
            else playerController.HideIndicator();
        }
    }

    private void PresentRoundPhase()
    {
        if (rounds == null || playerController == null) return;
        if (presentedPhase == rounds.Phase && presentedRound == rounds.RoundIndex) return;

        presentedPhase = rounds.Phase;
        presentedRound = rounds.RoundIndex;
        switch (rounds.Phase)
        {
            case FusionRoundPhase.Choosing:
                playerController.ResetRound();
                playerController.ApplyNetworkSelection(AimDirection, Charge);
                if (Object.HasStateAuthority && Charge > 0f) playerController.ShowIndicator();
                else playerController.HideIndicator();
                break;
            case FusionRoundPhase.Revealing:
                playerController.ClearLocalAimPreview();
                playerController.ShowIndicator();
                playerController.BeginRevealPresentation(rounds.RevealDuration, Object.HasStateAuthority);
                break;
            default:
                playerController.ClearLocalAimPreview();
                playerController.HideIndicator();
                playerController.StopMovementAnimation();
                break;
        }
    }

    private void SetVisible(bool value)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = value;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = value;
    }

    private System.Collections.IEnumerator AttachLocalPresentation()
    {
        // Fusion can spawn before the scene camera and UI complete their
        // Awake/Start pass. Retry briefly so a valid local player replaces
        // the authored camera pose before the round starts.
        for (int attempt = 0; attempt < 30; attempt++)
        {
            PlayerCameraController cameraController = FindFirstObjectByType<PlayerCameraController>();
            PlayerCameraLook cameraLook = GetComponentInChildren<PlayerCameraLook>(true);
            MatchUI matchUI = FindFirstObjectByType<MatchUI>();

            if (cameraController != null && cameraLook != null)
                cameraController.SetTarget(cameraLook.transform);

            if (matchUI != null)
            {
                matchUI.SetHumanPlayer(gameObject);
                matchUI.SetPlayer(gameObject);
            }

            if (cameraController != null && cameraLook != null && matchUI != null)
                yield break;

            yield return null;
        }

        Debug.LogWarning("[FusionNetworkPlayer] Não foi possível conectar completamente a câmera/UI local. Verifique PlayerCameraController, MatchUI e o filho CameraTarget.", this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (attachPresentationCoroutine != null)
            StopCoroutine(attachPresentationCoroutine);
    }

    private static string ResolveLocalNickname()
    {
        PlayFabAuthManager authentication = ServiceHub.Instance != null ? ServiceHub.Instance.Authentication : null;
        return authentication != null && !string.IsNullOrWhiteSpace(authentication.Nickname)
            ? authentication.Nickname
            : "Capivara";
    }
}
