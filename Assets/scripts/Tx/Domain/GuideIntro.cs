using UnityEngine;

public static class GuideIntro
{
    public static bool Shown => PlayerPrefs.GetInt(TxSaveKeys.GuideIntroShown, 0) == 1;

    public static void MarkShown()
    {
        if (Shown)
            return;

        PlayerPrefs.SetInt(TxSaveKeys.GuideIntroShown, 1);
        PlayerPrefs.Save();
    }
}
