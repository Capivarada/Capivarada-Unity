using System;

/// <summary>Small replicated end-of-match view consumed by the lobby results UI.</summary>
[Serializable]
public sealed class FusionPublishedResult
{
    public string playerId;
    public string nickname;
    public string avatarId;
    public bool isBot;
    public bool forfeited;
    public int placement;
    public int eliminations;
    public int roundsSurvived;
}
