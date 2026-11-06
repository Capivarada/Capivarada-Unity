using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies guest restrictions visually and functionally to a Lobby UI root.</summary>
public sealed class GuestAccessController : MonoBehaviour
{
    [Header("Requires authenticated account")]
    [SerializeField] private Selectable[] authenticatedOnlyControls;
    [SerializeField] private GameObject[] authenticatedOnlyLockIcons;
    [Header("Available only in offline sandbox")]
    [SerializeField] private Selectable[] offlineSandboxOnlyControls;
    [SerializeField] private GameObject[] offlineSandboxLockIcons;
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
        bool authenticated = mode == SessionAccessMode.AuthenticatedOnline;
        Set(authenticatedOnlyControls, authenticated);
        Set(authenticatedOnlyLockIcons, !authenticated);

        bool sandbox = mode == SessionAccessMode.GuestOffline;
        Set(offlineSandboxOnlyControls, sandbox);
        Set(offlineSandboxLockIcons, !sandbox);
    }

    private static void Set(Selectable[] controls, bool enabled)
    {
        if (controls == null) return;
        foreach (Selectable control in controls) if (control != null) control.interactable = enabled;
    }

    private static void Set(GameObject[] targets, bool active)
    {
        if (targets == null) return;
        foreach (GameObject target in targets) if (target != null) target.SetActive(active);
    }
}
