using System;
using UnityEngine;

// 临时账号资料 UI 事件。
public static class SdkCandyEvents
{
    public static event Action<AccountProfileData> AccountProfileUpdated;
    public static event Action<Sprite, AcType> SelectedAcChanged;

    public static void RaiseAccountProfileUpdated(AccountProfileData data) => AccountProfileUpdated?.Invoke(data);

    public static void RaiseSelectedAcChanged(Sprite sprite, AcType acType) => SelectedAcChanged?.Invoke(sprite, acType);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        AccountProfileUpdated = null;
        SelectedAcChanged = null;
    }
}
