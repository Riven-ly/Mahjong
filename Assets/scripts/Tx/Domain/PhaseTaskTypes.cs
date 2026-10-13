using System;
using UnityEngine;

public enum PhaseId
{
    None,
    Level8,
    ActiveMinutes20,
    Coin2000,
    ActiveDays7,
    PlayerLevel30
}

public enum PhaseTaskState
{
    Locked,
    InProgress,
    Completed
}

public enum PhaseFlowStep
{
    None,
    Pending,
    ProcessComplete,
    InProgress,
    SpecialBonus,
    Granting,
    RewardTip,
    Restore,
    Completed
}

public enum PhaseFlowSource
{
    None,
    LevelComplete,
    RuntimeProgress,
    Restore,
    SdkPanel
}

public readonly struct PhaseCompleteResult
{
    public bool Completed { get; }
    public PhaseId PhaseId { get; }
    public PhaseFlowSource Source { get; }
    public string FlowId { get; }

    public PhaseCompleteResult(bool completed, PhaseId phaseId, PhaseFlowSource source, string flowId)
    {
        Completed = completed;
        PhaseId = phaseId;
        Source = source;
        FlowId = flowId;
    }
}

public readonly struct PlayerLevelCalculation
{
    public int Level { get; }
    public int CurrentCompletedLevels { get; }
    public int RequiredCompletedLevels { get; }
    public bool IsValid { get; }

    public PlayerLevelCalculation(int level, int currentCompletedLevels, int requiredCompletedLevels, bool isValid)
    {
        Level = level;
        CurrentCompletedLevels = currentCompletedLevels;
        RequiredCompletedLevels = requiredCompletedLevels;
        IsValid = isValid;
    }
}

public readonly struct PlayerLevelProgress
{
    public int Level { get; }
    public int CurrentInLevel { get; }
    public int RequiredForNext { get; }
    public int TotalCompletedLevels { get; }
    public float InLevelNormalized { get; }
    public bool IsMaxLevel { get; }
    public bool IsValid { get; }

    public PlayerLevelProgress(int level, int currentInLevel, int requiredForNext, int totalCompletedLevels, bool isValid)
    {
        Level = level;
        CurrentInLevel = currentInLevel;
        RequiredForNext = requiredForNext;
        TotalCompletedLevels = totalCompletedLevels;
        IsValid = isValid;
        IsMaxLevel = level >= 30;
        InLevelNormalized = IsMaxLevel ? 1f : requiredForNext > 0 ? Mathf.Clamp01(currentInLevel / (float)requiredForNext) : 0f;
    }
}

public enum BonusTargetMode
{
    Off,
    Fixed,
    FillToTarget
}

public static class PhaseIds
{
    public const string Level8 = "level_8";
    public const string ActiveMinutes20 = "active_minutes_20";
    public const string Coin2000 = "coin_2000";
    public const string ActiveDays7 = "active_days_7";
    public const string PlayerLevel30 = "player_level_30";
}

[Serializable]
public sealed class PhaseDefinition
{
    [SerializeField] string id;
    [SerializeField] int order;
    [SerializeField] PhaseId phaseId;
    [SerializeField] string textId;
    [SerializeField] Sprite icon;

    public string Id => id;
    public int Order => order;
    public PhaseId PhaseId => phaseId;
    public string TextId => textId;
    public Sprite Icon => icon;
}

[Serializable]
public sealed class MinuteSlotDefinition
{
    [SerializeField] int slotIndex;
    [SerializeField] int requiredSuccessfulEscapes;

    public int SlotIndex => slotIndex;
    public int RequiredSuccessfulEscapes => requiredSuccessfulEscapes;
}

[Serializable]
public sealed class PhaseRewardConfig
{
    [SerializeField] float phase1Reward = 27.85f;
    [SerializeField] float phase2Target = 1600f;
    [SerializeField] float phase3Target = 1000000f;

    public float Phase1Reward => phase1Reward;
    public float Phase2Target => phase2Target;
    public float Phase3Target => phase3Target;
}

[Serializable]
public sealed class PlayerLevelRequirement
{
    [SerializeField] int level;
    [SerializeField] int requiredCompletedLevels;
    [SerializeField] int queueAhead;

    public int Level => level;
    public int RequiredCompletedLevels => requiredCompletedLevels;
    public int QueueAhead => queueAhead;
}

[Serializable]
public sealed class LevelPhaseConfig
{
    [SerializeField] int actualTargetLevel = 30;

    [SerializeField] int displayTargetLevel = 8;

    public int ActualTargetLevel => actualTargetLevel;
    public int DisplayTargetLevel => displayTargetLevel;
}

[Serializable]
public sealed class ActiveMinutePhaseConfig
{
    [SerializeField] int slotDurationSeconds = 60;
    [SerializeField] MinuteSlotDefinition[] slots;
    [SerializeField] bool carryEscapesToNextSlot;

    public int SlotDurationSeconds => slotDurationSeconds;
    public MinuteSlotDefinition[] Slots => slots;
    public bool CarryEscapesToNextSlot => carryEscapesToNextSlot;
}

[Serializable]
public sealed class CoinPhaseConfig
{
    [SerializeField] float targetCoin = 2000f;
    [SerializeField] bool enableSpecialBonus = true;
    [SerializeField] float bonusAmountMin;
    [SerializeField] float bonusAmountMax;
    [SerializeField] BonusTargetMode bonusTargetMode = BonusTargetMode.FillToTarget;

    public float TargetCoin => targetCoin;
    public bool EnableSpecialBonus => enableSpecialBonus;
    public float BonusAmountMin => bonusAmountMin;
    public float BonusAmountMax => bonusAmountMax;
    public BonusTargetMode BonusTargetMode => bonusTargetMode;
}

[Serializable]
public sealed class ActiveDayPhaseConfig
{
    [SerializeField] int requiredActiveDays = 7;
    [SerializeField] int winsRequiredPerDay = 5;
    [SerializeField] bool requireConsecutiveDays;

    public int RequiredActiveDays => requiredActiveDays;
    public int WinsRequiredPerDay => winsRequiredPerDay;
    public bool RequireConsecutiveDays => requireConsecutiveDays;
}

[Serializable]
public sealed class PlayerLevelPhaseConfig
{
    [SerializeField] int targetPlayerLevel = 30;
    [SerializeField] PlayerLevelRequirement[] levelRequirements;

    public int TargetPlayerLevel => targetPlayerLevel;
    public PlayerLevelRequirement[] LevelRequirements => levelRequirements;
}
