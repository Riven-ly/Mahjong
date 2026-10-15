using UnityEngine;

public static class TxGameBridge
{
    public const string SourceAmount = "amount";
    public const string SourceLevelId = "level_id";
    public const string SourceNode = "node";
    public const string SourceDay = "day";
    public const string SourceLevel = "level";

    public static int CurrentLevel
    {
        get
        {
            GameManager manager = GameManager.Instance;
            return manager == null || manager.playerInfo == null ? 1 : Mathf.Max(1, manager.playerInfo.level);
        }
    }

    public static float GoldAmount
    {
        get
        {
            GameManager manager = GameManager.Instance;
            return manager == null || manager.playerInfo == null ? 0f : manager.playerInfo.Gold;
        }
    }

    public static void AddGold(float amount)
    {
        if (amount <= 0f)
            return;

        GameManager manager = GameManager.Instance;
        if (manager == null || manager.playerInfo == null)
            return;

        manager.playerInfo.Add_gold(Mathf.RoundToInt(amount * PlayerInfo.CurrencyUnitScale));
    }

    public static string GetMark() => GetEncrypted("Special_Diamond__unit");

    public static string GetWithdraw() => GetEncrypted("wd");

    public static string GetEncrypted(string key)
    {
        LanguageManager manager = LanguageManager.Instance;
        return manager == null ? string.Empty : manager.GetText_Encrypt(key);
    }

    public static void PlaySfx()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBtnMusic();
    }

    public static void Toast(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        UIManager manager = UIManager.Instance;
        if (manager != null)
            manager.OpenUI<GeneralTipsPanel>(message);
    }
}
