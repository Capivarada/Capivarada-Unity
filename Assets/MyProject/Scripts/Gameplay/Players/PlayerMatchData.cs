using System;
using UnityEngine;

/// <summary>
/// Per-match statistics for a player.
/// Temporary data that resets for each new match.
/// Attached to player GameObject during match.
/// </summary>
public class PlayerMatchData : MonoBehaviour
{
    #region Match Statistics

    [SerializeField] private int kills = 0;
    [SerializeField] private int roundsSurvived = 0;

    // Placement within the match (1 = first). 0 = not set yet.
    [SerializeField] private int placement = 0;

    // Whether this player is marked as a winner for the match.
    [SerializeField] private bool won = false;

    #endregion

    #region Properties

    public int Kills => kills;
    public int RoundsSurvived => roundsSurvived;
    public int Placement => placement;
    public bool Won => won;

    #endregion

    #region Events

    public event Action<int> OnKillsChanged;
    public event Action<int> OnRoundsSurvivedChanged;
    public event Action<int> OnPlacementChanged;
    public event Action<bool> OnWonChanged;

    #endregion

    #region Public Methods

    /// <summary>
    /// Register a kill for this player.
    /// Called when this player eliminates another player.
    /// </summary>
    public void AddKill()
    {
        kills++;
        Debug.Log($"[PlayerMatchData] {gameObject.name} got a kill! Total: {kills}");
        OnKillsChanged?.Invoke(kills);
    }

    /// <summary>
    /// Register that this player survived another round.
    /// Called by RoundManager at end of each round.
    /// </summary>
    public void AddRoundSurvived()
    {
        roundsSurvived++;
        Debug.Log($"[PlayerMatchData] {gameObject.name} survived round! Total: {roundsSurvived}");
        OnRoundsSurvivedChanged?.Invoke(roundsSurvived);
    }

    /// <summary>
    /// Reset all match data for a new match.
    /// </summary>
    public void ResetMatchData()
    {
        kills = 0;
        roundsSurvived = 0;
        placement = 0;
        won = false;

        Debug.Log($"[PlayerMatchData] {gameObject.name} match data reset");

        OnKillsChanged?.Invoke(kills);
        OnRoundsSurvivedChanged?.Invoke(roundsSurvived);
        OnPlacementChanged?.Invoke(placement);
        OnWonChanged?.Invoke(won);
    }

    /// <summary>
    /// Set final placement for this player.
    /// </summary>
    public void SetPlacement(int value)
    {
        placement = value;
        OnPlacementChanged?.Invoke(placement);
    }

    /// <summary>
    /// Mark this player as winner/loser for the match.
    /// </summary>
    public void SetWon(bool value)
    {
        won = value;
        OnWonChanged?.Invoke(won);
    }

    #endregion
}
