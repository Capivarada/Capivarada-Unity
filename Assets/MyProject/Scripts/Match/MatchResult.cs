public class MatchResult
{
    public int Placement { get; set; }
    public bool Won { get; set; }

    public int Kills { get; set; }
    public int RoundsSurvived { get; set; }

    public MatchResult(
        int placement,
        int kills,
        int roundsSurvived,
        bool won = false)
    {
        Placement = placement;
        Kills = kills;
        RoundsSurvived = roundsSurvived;
        Won = won;
    }
}