using UnityEngine;

/// <summary>Chooses exactly one Lobby root without touching the shared Background or TopUI.</summary>
public sealed class LobbyPresentationController : MonoBehaviour
{
    [SerializeField] private GameObject authenticatedOnlineRoot;
    [SerializeField] private GameObject guestOnlineRoot;
    [SerializeField] private GameObject guestOfflineRoot;
    [SerializeField] private SessionAccessService session;
    [SerializeField] private PlayerProfileService profile;
    [SerializeField] private CosmeticCatalog cosmeticCatalog;

    private void Awake()
    {
        if (session == null) session = ServiceHub.Instance != null ? ServiceHub.Instance.Session : SessionAccessService.Instance;
        if (profile == null) profile = ServiceHub.Instance != null ? ServiceHub.Instance.Profile : PlayerProfileService.Instance;
    }

    private void OnEnable()
    {
        if (session != null) session.Changed += Apply;
        if (profile != null)
        {
            profile.Changed += ApplyCosmetics;
            profile.Load();
            ApplyCosmetics(profile.Current);
        }
        Apply(session != null ? session.Mode : SessionAccessMode.GuestOffline);
    }

    private void OnDisable()
    {
        if (session != null) session.Changed -= Apply;
        if (profile != null) profile.Changed -= ApplyCosmetics;
    }

    private void Apply(SessionAccessMode mode)
    {
        Set(authenticatedOnlineRoot, mode == SessionAccessMode.AuthenticatedOnline);
        Set(guestOnlineRoot, mode == SessionAccessMode.GuestOnline);
        Set(guestOfflineRoot, mode == SessionAccessMode.GuestOffline);
    }

    private void ApplyCosmetics(PlayerProfileData data)
    {
        PlayerCosmeticLoadout loadout = CapybaraGameplayCosmetics.ResolveLoadout(data != null ? data.cosmetics : null);
        foreach (CapybaraAppearanceApplier appearance in FindObjectsByType<CapybaraAppearanceApplier>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            appearance.Apply(loadout, cosmeticCatalog);
    }

    private static void Set(GameObject root, bool active) { if (root != null) root.SetActive(active); }
}
