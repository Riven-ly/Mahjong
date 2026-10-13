using System;

public sealed class RewardTipPanelData
{
    public float Amount { get; }
    public Action Completed { get; }

    public RewardTipPanelData(float amount, Action completed)
    {
        Amount = amount;
        Completed = completed;
    }
}
