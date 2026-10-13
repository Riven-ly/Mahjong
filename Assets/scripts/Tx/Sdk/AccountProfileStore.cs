using UnityEngine;

public static class AccountProfileStore
{
    public static AccountProfileData Load()
    {
        string account = PlayerPrefs.GetString(TxSaveKeys.CandyAccount, string.Empty).Trim();
        string name = PlayerPrefs.GetString(TxSaveKeys.CandyAccountName, string.Empty).Trim();
        int typeValue = PlayerPrefs.GetInt(TxSaveKeys.CandyAcType, (int)AcType.Channel0);
        AcType type = typeValue >= (int)AcType.Channel0 && typeValue <= (int)AcType.Channel3
            ? (AcType)typeValue
            : AcType.Channel0;
        return new AccountProfileData(account, name, type);
    }

    public static bool HasCompleteProfile()
    {
        AccountProfileData profile = Load();
        int typeValue = PlayerPrefs.GetInt(TxSaveKeys.CandyAcType, (int)AcType.Channel0);
        return !string.IsNullOrEmpty(profile.Account)
               && !string.IsNullOrEmpty(profile.Name)
               && typeValue >= (int)AcType.Channel0
               && typeValue <= (int)AcType.Channel3;
    }

    public static void Save(AccountProfileData data)
    {
        PlayerPrefs.SetString(TxSaveKeys.CandyAccount, data.Account.Trim());
        PlayerPrefs.SetString(TxSaveKeys.CandyAccountName, data.Name.Trim());
        PlayerPrefs.SetInt(TxSaveKeys.CandyAcType, (int)data.Type);
        PlayerPrefs.Save();
    }
}
