using UnityEngine;

/// <summary>
/// Configuration for match timing and rules.
/// Central point for all match-level game balance.
/// </summary>
[CreateAssetMenu(fileName = "MatchConfig", menuName = "Capivarada/Game/Match Config")]
public class MatchConfig : ScriptableObject
{
    #region Match Timing

    [Header("Match Timing")]
    [SerializeField, Tooltip("Countdown before the first round.")] [Range(1f, 30f)] private float initialCountdownDuration = 10f;

    public float InitialCountdownDuration => initialCountdownDuration;

    #endregion

    #region Round Timing

    [Header("Round Timing")]
    [SerializeField, Tooltip("Time available to choose aim and power.")] [Range(1f, 30f)] private float choosingDuration = 8f;
    [SerializeField, Tooltip("Countdown between completed rounds.")] [Range(0.1f, 10f)] private float nextRoundCountdown = 3f;
    [Header("Round Reveal")]
    [SerializeField, Tooltip("Tempo em que as direções ficam visíveis antes dos jogadores serem lançados.")]
    [Range(0.1f, 5f)]
    private float revealDuration = 1.5f;

    public float RevealDuration => revealDuration;
    public float ChoosingDuration => choosingDuration;
    public float NextRoundCountdown => nextRoundCountdown;

    #endregion

    #region Rules

    [Header("Rules")]
    [SerializeField, Tooltip("Round at which remaining alive players share victory.")] [Range(1, 100)] private int maxRounds = 10;

    public int MaxRounds => maxRounds;

    #endregion

    #region Movement / Stop Detection

    [Header("Movement / Stop Detection")]
    [SerializeField, Tooltip("Horizontal velocity below which a player is considered stopped.")] [Range(0.01f, 0.5f)] private float stopVelocityThreshold = 0.15f;
    [SerializeField, Tooltip("Extra confirmation time after all living players stop.")] [Range(0.1f, 2f)] private float stopCheckDelay = 0.75f;

    public float StopVelocityThreshold => stopVelocityThreshold;
    public float StopCheckDelay => stopCheckDelay;

    #endregion
}