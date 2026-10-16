using UnityEngine;

public static class VibrationManager
{
    private const long DurationMilliseconds = 60L;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static AndroidJavaObject vibrationEffect;
    private static bool initialized;
    private static bool supportsVibrationEffect;
#endif

    public static void Vibrate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Initialize();
        if (vibrator == null)
        {
            return;
        }

        if (supportsVibrationEffect)
        {
            vibrator.Call("vibrate", vibrationEffect);
        }
        else
        {
            vibrator.Call("vibrate", DurationMilliseconds);
        }
#else
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }

        using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            supportsVibrationEffect = version.GetStatic<int>("SDK_INT") >= 26;
        }

        if (supportsVibrationEffect)
        {
            using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
            {
                vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", DurationMilliseconds, -1);
            }
        }
    }
#endif
}
