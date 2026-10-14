using System;

public sealed class RewardTipData
{
    public float Amount { get; }
    public Action Completed { get; }
    public bool ApplyReward { get; }

    public RewardTipData(float amount, Action completed, bool applyReward = true)
    {
        Amount = amount;
        Completed = completed;
        ApplyReward = applyReward;
    }
}
