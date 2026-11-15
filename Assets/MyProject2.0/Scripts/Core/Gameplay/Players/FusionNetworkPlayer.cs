using System;
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
    [Networked, Capacity(32)] public string NetworkAvatarId { get; private set; }
    [Networked, Capacity(32)] public string NetworkPlayerId { get; private set; }
    [Networked, Capacity(512)] private string SerializedCosmetics { get; set; }
    [Networked] public int Eliminations { get; private set; }
    [Networked] public int RoundsSurvived { get; private set; }
    [Networked] public int Placement { get; private set; }
    [Networked] private int LastLaunchedRound { get; set; }

    private FusionRoundController rounds;
    private FusionArenaMatchController match;
    private Camera localCamera;
    private PlayerCameraLook localCameraLook;
    private bool wasEliminated;
    private Coroutine attachPresentationCoroutine;
    private int preparedRound = -1;
    private int clientPreparedRound = -1;
    private FusionRoundPhase presentedPhase = (FusionRoundPhase)(-1);
    private int presentedRound = -1;
    private Vector3 clientAimDirection;
    private float clientCharge;
    private float nextClientSubmitTime;
    private string serverJoinToken = string.Empty;
    private string presentedCosmetics = string.Empty;
    // Spawned can be replayed while Fusion catches a client up. Do not send the
    // identity RPC again during that replay.
    private bool dedicatedIdentitySubmitted;
    private NetworkObject lastAttacker;
    private PlayerProfileService cosmeticProfile;

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
        bool isLocalPlayer = HasLocalInput();
        if (localInput != null) localInput.enabled = isLocalPlayer;
        PlayerCameraLook cameraLook = GetComponentInChildren<PlayerCameraLook>(true);
        localCameraLook = cameraLook;
        if (cameraLook != null) cameraLook.enabled = isLocalPlayer;
        rounds = FindFirstObjectByType<FusionRoundController>();
        match = FindFirstObjectByType<FusionArenaMatchController>();
        playerController?.SetConfiguration(match != null ? match.PlayerConfig : null);

        if (Object.HasStateAuthority)
        {
            if (AimDirection.sqrMagnitude < 0.001f) AimDirection = transform.forward;
            if (string.IsNullOrWhiteSpace(NetworkNickname)) NetworkNickname = ResolveLocalNickname();
        }

        if (isLocalPlayer)
        {
            cosmeticProfile = PlayerProfileService.Instance;
            if (cosmeticProfile != null)
            {
                cosmeticProfile.Changed += HandleLocalProfileChanged;
                HandleLocalProfileChanged(cosmeticProfile.Current);
            }

            attachPresentationCoroutine = StartCoroutine(AttachLocalPresentation());
            SubmitDedicatedIdentity();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (rounds == null) rounds = FindFirstObjectByType<FusionRoundController>();
        if (rounds == null || IsEliminated) return;

        if (!Object.HasStateAuthority)
        {
            if (HasLocalInput() && rounds.Phase == FusionRoundPhase.Choosing) ReadClientChoice();
            else if (HasLocalInput()) localInput?.ClearPendingChargePresses();
            return;
        }

        if (body != null && transform.position.y <= (match != null ? match.DeathHeight : 2.87f))
        {
            IsEliminated = true;
            body.isKinematic = true;
            AwardEliminationToLastAttacker();
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

    /// <summary>
    /// Collision is requested by the peer that deterministically owns the
    /// contact, then applied by this object's State Authority.
    /// </summary>
    public void RequestCollisionResponse(Vector3 direction, float velocityChange)
    {
        if (velocityChange <= 0f) return;
        RpcApplyCollisionResponse(direction, velocityChange);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcApplyCollisionResponse(Vector3 direction, float velocityChange)
    {
        if (IsEliminated || body == null) return;
        ApplyCollisionResponse(body, direction, velocityChange);
    }

    private static void ApplyCollisionResponse(Rigidbody target, Vector3 direction, float velocityChange)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return;
        direction.Normalize();

        Vector3 velocity = target.linearVelocity;
        float alongPush = Vector3.Dot(new Vector3(velocity.x, 0f, velocity.z), direction);
        if (alongPush < 0f) velocity -= direction * alongPush;
        target.linearVelocity = velocity;
        target.AddForce(direction * velocityChange, ForceMode.VelocityChange);
    }

    public override void Render()
    {
        // During local choosing, the networked value is necessarily behind the
        // camera preview by at least one round trip. Reapplying it every render
        // used to overwrite the direction the player had just selected.
        bool presentingLocalChoice = HasLocalInput() && rounds != null &&
            rounds.Phase == FusionRoundPhase.Choosing &&
            playerController != null && !playerController.IsChoiceLocked;
        if (!presentingLocalChoice)
            playerController?.ApplyNetworkSelection(AimDirection, Charge);
        PresentCosmetics();
        PresentRoundPhase();
        PlayerNickname nickname = GetComponent<PlayerNickname>();
        if (nickname != null && !string.IsNullOrWhiteSpace(NetworkNickname)) nickname.SetNickname(NetworkNickname);
        if (!wasEliminated && IsEliminated)
        {
            wasEliminated = true;
            SetVisible(false);
            if (HasLocalInput())
            {
                PlayerCameraLook cameraLook = GetComponentInChildren<PlayerCameraLook>(true);
                if (cameraLook != null) cameraLook.enabled = false;
                FindFirstObjectByType<MatchUI>()?.HandleNetworkLocalElimination(gameObject);
            }
        }
    }

    private void ReadLocalChoice()
    {
        // On a dedicated server this object has State Authority, but it is not
        // the local player. Reading its disabled prefab camera here overwrote
        // every RPC choice with the spawn-facing direction on every tick.
        if (!Object.HasStateAuthority || !HasLocalInput() || localInput == null) return;
        if (localInput.ConsumeLockChoicePresses() > 0)
            playerController?.ToggleChoiceLock();
        if (playerController != null && playerController.IsChoiceLocked) return;
        if (TryGetLocalAimDirection(out Vector3 direction))
            AimDirection = direction;

        int addPresses = localInput.ConsumeAddChargePresses();
        int removePresses = localInput.ConsumeRemoveChargePresses();
        Charge += addPresses * 0.5f;
        Charge -= removePresses * 0.5f;
        playerController?.ApplyNetworkSelection(AimDirection, Charge);
        Charge = playerController != null ? playerController.CurrentCharge : Mathf.Max(0f, Charge);
        // Apply the camera choice before previewing it. Previously the preview
        // used the direction from the preceding network tick, making the mesh
        // turn while its arrow looked fixed or one step behind.
        playerController?.PreviewLocalAimRotation();
        if (playerController != null)
        {
            if (Charge > 0f) playerController.ShowIndicator();
            else playerController.HideIndicator();
        }
    }

    private void ReadClientChoice()
    {
        if (!HasLocalInput() || localInput == null || rounds == null) return;
        if (clientPreparedRound != rounds.RoundIndex)
        {
            clientPreparedRound = rounds.RoundIndex;
            clientCharge = 0f;
            clientAimDirection = AimDirection.sqrMagnitude > 0.001f ? AimDirection : transform.forward;
            playerController?.ResetRound();
        }

        bool lockToggled = localInput.ConsumeLockChoicePresses() > 0;
        if (lockToggled)
            playerController?.ToggleChoiceLock();
        if (playerController != null && playerController.IsChoiceLocked) return;

        if (TryGetLocalAimDirection(out Vector3 direction))
            clientAimDirection = direction;

        int addPresses = localInput.ConsumeAddChargePresses();
        int removePresses = localInput.ConsumeRemoveChargePresses();
        clientCharge += addPresses * 0.5f;
        clientCharge -= removePresses * 0.5f;
        playerController?.ApplyNetworkSelection(clientAimDirection, clientCharge);
        clientCharge = playerController != null ? playerController.CurrentCharge : Mathf.Max(0f, clientCharge);
        playerController?.PreviewLocalAimRotation();
        if (clientCharge > 0f) playerController?.ShowIndicator(); else playerController?.HideIndicator();

        bool chargeChanged = addPresses != 0 || removePresses != 0 || lockToggled;
        if (chargeChanged || Time.unscaledTime >= nextClientSubmitTime)
        {
            nextClientSubmitTime = Time.unscaledTime + 0.1f;
            RpcSubmitChoice(rounds.RoundIndex, clientAimDirection, clientCharge);
        }
    }

    private bool HasLocalInput()
    {
        return Object != null && (Object.HasInputAuthority || (Runner != null && Runner.GameMode == GameMode.Shared && Object.HasStateAuthority));
    }

    private bool TryGetLocalAimDirection(out Vector3 direction)
    {
        if (localCameraLook == null)
            localCameraLook = GetComponentInChildren<PlayerCameraLook>(true);

        direction = localCameraLook != null
            ? localCameraLook.AimForward
            : localCamera != null ? localCamera.transform.forward : Vector3.zero;
        if (localCamera == null && localCameraLook == null)
            localCamera = Camera.main;
        if (localCameraLook == null && localCamera != null)
            direction = localCamera.transform.forward;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return false;
        direction.Normalize();
        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcSubmitChoice(int round, Vector3 direction, float charge)
    {
        if (rounds == null || rounds.Phase != FusionRoundPhase.Choosing || rounds.RoundIndex != round || IsEliminated) return;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) AimDirection = direction.normalized;
        PlayerConfig configuration = match != null ? match.PlayerConfig : null;
        float maximum = configuration != null ? configuration.MaxCharge : 5f;
        float minimum = configuration != null ? configuration.MinCharge : 0f;
        Charge = Mathf.Round(Mathf.Clamp(charge, minimum, maximum) * 2f) * 0.5f;
    }

    private void SubmitDedicatedIdentity()
    {
        if (dedicatedIdentitySubmitted) return;
        if (!FusionDedicatedMatchContext.TryGet(out FusionDedicatedIdentity identity)) return;
        PlayerCosmeticLoadout loadout = PlayerProfileService.Instance != null &&
            PlayerProfileService.Instance.Current != null
            ? PlayerProfileService.Instance.Current.cosmetics
            : null;
        dedicatedIdentitySubmitted = true;

        // Fusion limits one RPC payload to 512 bytes. A join token plus five
        // strings and cosmetic JSON exceeded that limit, so the server never
        // received the player's identity. Keep the credential separate from
        // the cosmetic presentation payload.
        RpcSubmitIdentity(identity.PlayerId, identity.Nickname, identity.AvatarId,
            identity.JoinToken);
        RpcSubmitCosmetics(JsonUtility.ToJson(loadout ?? new PlayerCosmeticLoadout()));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcSubmitIdentity(string playerId, string nickname, string avatarId,
        string joinToken)
    {
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(joinToken)) return;
        NetworkPlayerId = playerId.Trim();
        NetworkNickname = string.IsNullOrWhiteSpace(nickname) ? "Capivara" : nickname.Trim();
        NetworkAvatarId = string.IsNullOrWhiteSpace(avatarId) ? "avatar1" : avatarId.Trim();
        serverJoinToken = joinToken;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcSubmitCosmetics(string cosmeticsJson)
    {
        // Keep this bounded independently from the identity RPC. A normal
        // loadout is far below 512 bytes; refusing an oversized payload is
        // safer than silently corrupting the match packet.
        if (!string.IsNullOrEmpty(cosmeticsJson) &&
            System.Text.Encoding.UTF8.GetByteCount(cosmeticsJson) > 512)
            return;

        SerializedCosmetics = cosmeticsJson ?? string.Empty;
    }

    private void PresentCosmetics()
    {
        if (string.IsNullOrWhiteSpace(SerializedCosmetics) ||
            SerializedCosmetics == presentedCosmetics)
            return;

        PlayerCosmeticLoadout loadout;
        try
        {
            loadout = JsonUtility.FromJson<PlayerCosmeticLoadout>(
                SerializedCosmetics);
        }
        catch (ArgumentException)
        {
            return;
        }

        if (loadout == null)
            return;

        presentedCosmetics = SerializedCosmetics;
        CapybaraGameplayCosmetics.Apply(gameObject, loadout);
    }

    public void RegisterSurvivedRound()
    {
        if (Object != null && Object.HasStateAuthority && !IsEliminated) RoundsSurvived++;
    }

    /// <summary>Published once by the dedicated server when the arena ends.</summary>
    public void SetPlacement(int placement)
    {
        if (Object != null && Object.HasStateAuthority)
            Placement = Mathf.Max(0, placement);
    }

    public void RegisterAttacker(NetworkObject attacker)
    {
        if (Object != null && Object.HasStateAuthority && attacker != null) lastAttacker = attacker;
    }

    public void AwardElimination()
    {
        if (Object != null && Object.HasStateAuthority && !IsEliminated) Eliminations++;
    }

    private void AwardEliminationToLastAttacker()
    {
        if (lastAttacker == null || lastAttacker == Object) return;
        FusionNetworkPlayer attacker = lastAttacker.GetComponent<FusionNetworkPlayer>();
        attacker?.AwardElimination();
    }

    public FusionMatchResult BuildMatchResult()
    {
        return new FusionMatchResult
        {
            PlayerRef = Object != null ? Object.InputAuthority : PlayerRef.None,
            IsBot = false,
            IsEliminated = IsEliminated,
            PlayerId = NetworkPlayerId,
            JoinToken = Object != null && Object.HasStateAuthority ? serverJoinToken : string.Empty,
            Nickname = NetworkNickname,
            AvatarId = NetworkAvatarId,
            Eliminations = Eliminations,
            RoundsSurvived = RoundsSurvived,
            Placement = Placement
        };
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
            case FusionRoundPhase.Moving:
                playerController.BeginNetworkMovementPresentation();
                playerController.HideIndicator();
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
        if (cosmeticProfile != null)
            cosmeticProfile.Changed -= HandleLocalProfileChanged;

        cosmeticProfile = null;
        if (attachPresentationCoroutine != null)
            StopCoroutine(attachPresentationCoroutine);
    }

    private void HandleLocalProfileChanged(PlayerProfileData _)
    {
        CapybaraGameplayCosmetics.ApplySavedPlayer(gameObject);
    }

    private static string ResolveLocalNickname()
    {
        PlayFabAuthManager authentication = ServiceHub.Instance != null ? ServiceHub.Instance.Authentication : null;
        return authentication != null && !string.IsNullOrWhiteSpace(authentication.Nickname)
            ? authentication.Nickname
            : "Capivara";
    }
}
