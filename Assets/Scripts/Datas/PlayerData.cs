public static class PlayerData
{
    public static string PlayerId;
    public static string Username;
    public static string PlayerName;

    public static bool IsGuest;

    public static void Clear()
    {
        PlayerId = null;
        Username = null;
        PlayerName = null;
        IsGuest = false;
    }
}