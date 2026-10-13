using UnityEngine;

public static class PlayerProfileService
{
    public static string PlayerId
    {
        get
        {
            Initialize();
            return PlayerPrefs.GetString(TxSaveKeys.PlayerId, string.Empty);
        }
    }

    public static void Initialize()
    {
        string value = PlayerPrefs.GetString(TxSaveKeys.PlayerId, string.Empty);
        if (IsValidId(value))
            return;

        PlayerPrefs.SetString(TxSaveKeys.PlayerId, CreateId());
        PlayerPrefs.Save();
    }

    static bool IsValidId(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 9)
            return false;

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] < '0' || value[index] > '9')
                return false;
        }

        return true;
    }

    static string CreateId() => Random.Range(100000000, 1000000000).ToString();
}
