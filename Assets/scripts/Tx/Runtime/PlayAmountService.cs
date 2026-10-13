using UnityEngine;

public static class PlayAmountService
{
    public static float Amount => TxGameBridge.GoldAmount;

    public static void Set(float amount)
    {
        float delta = Normalize(amount) - TxGameBridge.GoldAmount;
        if (delta > 0f)
            TxGameBridge.AddGold(delta);
    }

    public static float Fill(float target)
    {
        float delta = Normalize(target) - TxGameBridge.GoldAmount;
        if (delta <= 0f)
            return 0f;

        TxGameBridge.AddGold(delta);
        return delta;
    }

    internal static void Reset()
    {
    }

    static float Normalize(float value) => Mathf.Round(Mathf.Max(0f, value) * 100f) / 100f;
}
