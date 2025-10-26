// CutsceneGate.cs
using System.Collections.Generic;

public static class CutsceneGate
{
    static readonly HashSet<string> Busy = new();

    public static bool TryEnter(string group)
    {
        if (Busy.Contains(group)) return false;
        Busy.Add(group);
        return true;
    }

    public static void Exit(string group) => Busy.Remove(group);
    public static bool IsBusy(string group) => Busy.Contains(group);
}
