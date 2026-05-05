using UnityEngine;

/// <summary>Wrapper de logging eliminado en Release. Activar con ENABLE_LOGS en Player Settings > Scripting Define Symbols.</summary>
public static class GameDebug
{
#if ENABLE_LOGS
    public static void Log(string msg)              => Debug.Log(msg);
    public static void LogWarning(string msg)       => Debug.LogWarning(msg);
    public static void LogError(string msg)         => Debug.LogError(msg);
    public static void Log(object msg)              => Debug.Log(msg);
#else
    public static void Log(string msg)              { }
    public static void LogWarning(string msg)       { }
    public static void LogError(string msg)         { }
    public static void Log(object msg)              { }
#endif
}
