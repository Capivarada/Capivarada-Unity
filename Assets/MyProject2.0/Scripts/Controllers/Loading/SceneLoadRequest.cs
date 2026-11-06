using UnityEngine;

/// <summary>Small explicit hand-off between an entry screen and the loading scene.</summary>
public static class SceneLoadRequest
{
    private static string pendingScene;

    public static void Request(string destination)
    {
        pendingScene = destination;
    }

    public static string Consume(string fallback)
    {
        string result = string.IsNullOrWhiteSpace(pendingScene) ? fallback : pendingScene;
        pendingScene = null;
        return result;
    }
}
