using UnityEngine;

public static class TxGameBridge
{
    public const string SourceSpace = "tx_space";
    public const string SourceAmount = "tx_amount";
    public const string SourceLevelId = "tx_level_id";
    public const string SourceNode = "tx_node";
    public const string SourceDay = "tx_day";
    public const string SourceLevel = "tx_level";
    public const string SourceValue = "tx_value";

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

    public static void Track(string eventName, string key, object value)
    {
        if (OtherSdkManager.Instance == null)
            return;

        OtherSdkManager.Instance.CustomEvent(eventName, key, value);
    }

    public static void Track(string eventName)
    {
        if (OtherSdkManager.Instance == null)
            return;

        OtherSdkManager.Instance.CustomEvent(eventName, SourceValue, 1);
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
