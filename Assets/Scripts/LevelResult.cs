using UnityEngine;

// A snapshot of the completed level, kept only until its report is acknowledged.
public sealed class LevelResult
{
    public static LevelResult Current { get; private set; }

    public float CompletionSeconds { get; }
    public int Deaths { get; }
    public string CompletedScene { get; }
    public string NextScene { get; }
    public bool IsFinalReport { get; }

    private LevelResult(float seconds, int deaths, string completedScene, string nextScene, bool isFinalReport)
    {
        CompletionSeconds = seconds;
        Deaths = deaths;
        CompletedScene = completedScene;
        NextScene = nextScene;
        IsFinalReport = isFinalReport;
    }

    public static void Store(float seconds, int deaths, string completedScene, string nextScene, bool isFinalReport)
    {
        Current = new LevelResult(seconds, deaths, completedScene, nextScene, isFinalReport);
    }

    public static void Clear() => Current = null;

    // Also runs when entering Play mode with domain reload disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForNewSession() => Clear();
}
