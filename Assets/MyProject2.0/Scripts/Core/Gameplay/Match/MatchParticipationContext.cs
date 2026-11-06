/// <summary>Per-session match intent. It is transient and must never be used as a persistence source.</summary>
public static class MatchParticipationContext
{
    public static bool IsPractice { get; private set; }
    public static bool CanEarnRewards { get; private set; }

    public static void StartPractice()
    {
        IsPractice = true;
        CanEarnRewards = false;
    }

    public static void StartOnline(SessionAccessService session)
    {
        IsPractice = false;
        CanEarnRewards = session != null && session.IsAuthenticated;
    }

    public static void Clear()
    {
        IsPractice = false;
        CanEarnRewards = false;
    }
}
