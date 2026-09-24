using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

[Preserve]
internal sealed class UnitySplashScreen
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void OnBeforeSplashScreen()
    {
        SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }
}