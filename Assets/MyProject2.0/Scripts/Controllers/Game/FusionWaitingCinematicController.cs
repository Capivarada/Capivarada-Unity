using UnityEngine;

/// <summary>
/// Drives the authored player camera through map waypoints while Fusion fills
/// the room. It never owns match state; it is purely local presentation.
/// Add it to GameManager and assign at least two waypoint transforms.
/// </summary>
public sealed class FusionWaitingCinematicController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private FusionArenaMatchController match;
    [SerializeField] private PlayerCameraController playerCamera;
    [SerializeField] private MatchUI matchUI;

    [Header("Flyover path")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField, Min(0.1f)] private float secondsPerWaypoint = 2f;
    [SerializeField] private bool loopPath = true;

    private bool isPlaying;
    private int fromIndex;
    private int toIndex;
    private float segmentTime;
    private int lastDisplayedSecond = -1;
    private int lastHumanCount = -1;

    private void OnEnable()
    {
        ResolveReferences();
        if (match != null)
        {
            match.WaitingChanged += HandleWaitingChanged;
            match.MatchReady += HandleMatchReady;
        }
    }

    private void Start()
    {
        ResolveReferences();
        if (match != null && !match.IsMatchReady)
            BeginCinematic();
    }

    private void OnDisable()
    {
        if (match != null)
        {
            match.WaitingChanged -= HandleWaitingChanged;
            match.MatchReady -= HandleMatchReady;
        }

        EndCinematic();
    }

    private void Update()
    {
        if (!isPlaying || waypoints == null || waypoints.Length < 2 || playerCamera == null)
            return;

        Transform from = waypoints[fromIndex];
        Transform to = waypoints[toIndex];
        if (from == null || to == null)
            return;

        segmentTime += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(segmentTime / secondsPerWaypoint);
        float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
        playerCamera.SetCinematicPose(
            Vector3.Lerp(from.position, to.position, smoothProgress),
            Quaternion.Slerp(from.rotation, to.rotation, smoothProgress));

        if (progress < 1f)
            return;

        segmentTime = 0f;
        fromIndex = toIndex;
        toIndex++;
        if (toIndex < waypoints.Length)
            return;

        if (loopPath)
        {
            toIndex = 0;
            return;
        }

        toIndex = waypoints.Length - 1;
        isPlaying = false;
    }

    private void HandleWaitingChanged(int humans, int required)
    {
        if (match == null || match.IsMatchReady)
            return;

        BeginCinematic();
        int seconds = Mathf.CeilToInt(match.RemainingWaitSeconds);
        if (seconds == lastDisplayedSecond && humans == lastHumanCount)
            return;

        lastDisplayedSecond = seconds;
        lastHumanCount = humans;
        if (matchUI != null)
            matchUI.ShowNetworkWaiting($"AGUARDANDO CAPIVARAS {humans}/{required}", seconds);
    }

    private void HandleMatchReady()
    {
        EndCinematic();
        if (matchUI != null)
            matchUI.HideNetworkWaiting();
    }

    private void BeginCinematic()
    {
        if (isPlaying || waypoints == null || waypoints.Length < 2)
            return;

        isPlaying = true;
        fromIndex = 0;
        toIndex = 1;
        segmentTime = 0f;
        lastDisplayedSecond = -1;
        lastHumanCount = -1;
    }

    private void EndCinematic()
    {
        isPlaying = false;
        if (playerCamera != null)
            playerCamera.EndCinematic();
    }

    private void ResolveReferences()
    {
        if (match == null) match = FindFirstObjectByType<FusionArenaMatchController>();
        if (playerCamera == null) playerCamera = FindFirstObjectByType<PlayerCameraController>();
        if (matchUI == null) matchUI = FindFirstObjectByType<MatchUI>();
    }
}
