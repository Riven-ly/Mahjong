using System;

[Serializable]
public sealed class PhaseTaskSaveData
{
    public int schemaVersion;
    public string currentPhaseId;
    public PhaseSaveState[] phases;
    public MinutePhaseSaveData minuteProgress;
    public float cAmount;
    public ActiveDaySaveData activeDayProgress;
    public PlayerLevelSaveData playerLevelProgress;
    public string pendingTransitionPhaseId;
    public bool finalQualificationRecorded;
    public PhaseFlowSaveData pendingFlow;
}

[Serializable]
public sealed class PhaseSaveState
{
    public string phaseId;
    public PhaseTaskState state;
}

[Serializable]
public sealed class MinutePhaseSaveData
{
    public int slotIndex;
    public float activeSeconds;
    public int successfulEscapes;
}

[Serializable]
public sealed class ActiveDaySaveData
{
    public string currentDate;
    public int todayWins;
    public string[] completedDates;
}

[Serializable]
public sealed class PlayerLevelSaveData
{
    public int level;
    public int completedLevelsInCurrentLevel;
    public int totalCompletedLevels;
}
