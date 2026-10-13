using System;

[Serializable]
public sealed class PhaseFlowSaveData
{
    public string flowId;
    public string completedPhaseId;
    public string nextPhaseId;
    public PhaseFlowStep step;
    public PhaseFlowSource source;
    public float displayReward;
    public float grantedReward;
    public bool rewardApplied;
    public bool requiresLevelTaskTip;
    public bool waitsForNextLevel;
}
