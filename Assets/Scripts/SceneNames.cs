/// <summary>
/// Single source of truth for scene names the code needs to branch on, so
/// "am I in the hub?" isn't a string literal repeated across scripts.
/// </summary>
public static class SceneNames
{
    public const string Hub = "Hub";

    public static bool IsHub(string sceneName) => sceneName == Hub;
}
