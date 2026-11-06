using UnityEngine;

/// <summary>Chooses exactly one Lobby root without touching the shared Background or TopUI.</summary>
public sealed class LobbyPresentationController : MonoBehaviour
{
    [SerializeField] private GameObject authenticatedOnlineRoot;
    [SerializeField] private GameObject guestOnlineRoot;
    [SerializeField] private GameObject guestOfflineRoot;
    [SerializeField] private SessionAccessService session;

    private void Awake()
    {
        if (session == null) session = ServiceHub.Instance != null ? ServiceHub.Instance.Session : SessionAccessService.Instance;
    }

    private void OnEnable()
    {
        if (session != null) session.Changed += Apply;
        Apply(session != null ? session.Mode : SessionAccessMode.GuestOffline);
    }

    private void OnDisable()
    {
        if (session != null) session.Changed -= Apply;
    }

    private void Apply(SessionAccessMode mode)
    {
        Set(authenticatedOnlineRoot, mode == SessionAccessMode.AuthenticatedOnline);
        Set(guestOnlineRoot, mode == SessionAccessMode.GuestOnline);
        Set(guestOfflineRoot, mode == SessionAccessMode.GuestOffline);
    }

    private static void Set(GameObject root, bool active) { if (root != null) root.SetActive(active); }
}
