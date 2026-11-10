using System;

/// <summary>
/// Transient identity prepared by the authenticated Unity client immediately
/// before it joins a dedicated Fusion session. This never holds a PlayFab
/// session ticket, secret key, or Azure credential.
/// </summary>
public static class FusionDedicatedMatchContext
{
    private static FusionDedicatedIdentity current;
    private static bool hasCurrent;

    public static bool TryGet(out FusionDedicatedIdentity identity)
    {
        identity = current;
        return hasCurrent && identity.IsValid;
    }

    public static void Set(FusionDedicatedIdentity identity)
    {
        current = identity;
        hasCurrent = identity.IsValid;
    }

    public static void Clear()
    {
        current = default;
        hasCurrent = false;
    }
}

[Serializable]
public struct FusionDedicatedIdentity
{
    public string MatchId;
    public string PlayerId;
    public string Nickname;
    public string AvatarId;
    public string JoinToken;
    public string Address;
    public ushort Port;

    public bool IsValid => !string.IsNullOrWhiteSpace(MatchId) &&
                           !string.IsNullOrWhiteSpace(PlayerId) &&
                           !string.IsNullOrWhiteSpace(JoinToken) &&
                           !string.IsNullOrWhiteSpace(Address) && Port > 0;
}
