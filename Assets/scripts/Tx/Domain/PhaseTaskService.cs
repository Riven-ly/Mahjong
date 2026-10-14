using System;
using UnityEngine;

public static class PhaseTaskService
{
    const string SaveKey = "Tx.PhaseTask.SaveData";
    const int SaveVersion = 3;
    const int MinuteSlotCount = 19;
    const int MinuteDisplaySlotCount = 20;
    const int MinuteSlotDuration = 60;
    static readonly int[] MinuteEscapeTargets = { 20, 21, 22, 23, 25, 27, 29, 32, 35, 38, 42, 46, 50, 55, 60, 65, 71, 77, 83 };

    static PhaseTaskConfigSO config;
    static PhaseOneHudConfigSO phaseOneHudConfig;
    static PhaseTaskSaveData saveData;
    static bool initialized;
    static bool minuteConfigErrorLogged;
    static bool coinSubscribed;
    static bool initializingCoinState;

    public static float LastKnownGold { get; private set; }

    public static event Action<float> GoldChanged;

    public static PhaseId CurrentPhaseId
    {
        get
        {
            Initialize();
            return saveData == null ? PhaseId.Level8 : ParsePhaseId(saveData.currentPhaseId);
        }
    }

    public static bool IsAllPhaseCompleted => GetState(PhaseId.PlayerLevel30) == PhaseTaskState.Completed;

    public static bool IsCurrentPhaseCompleted => GetState(CurrentPhaseId) == PhaseTaskState.Completed;

    public static bool CanShowLevelTaskTip(bool completedNow) => !completedNow && !IsCurrentPhaseCompleted;

    public static bool HasPendingFlow
    {
        get
        {
            Initialize();
            return saveData != null && saveData.pendingFlow != null;
        }
    }

    public static PhaseFlowSaveData GetPendingFlow()
    {
        Initialize();
        return saveData?.pendingFlow;
    }

    public static void ResetPendingLevelFlow() { }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeState()
    {
        config = null;
        phaseOneHudConfig = null;
        saveData = null;
        initialized = false;
        minuteConfigErrorLogged = false;
        coinSubscribed = false;
        initializingCoinState = false;
        LastKnownGold = 0f;
        GoldChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeOnLaunch() => Initialize();

    public static void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        config = Resources.Load<PhaseTaskConfigSO>("Tx/PhaseTaskConfig");
        phaseOneHudConfig = Resources.Load<PhaseOneHudConfigSO>("Tx/PhaseOneHudConfig");
        if (config == null)
        {
            Debug.LogError("[Tx] 找不到 PhaseTaskConfig 配置");
            return;
        }

        Load();
        EnsureState();
        PlayerProfileService.Initialize();
        initializingCoinState = true;
        NotifyAmountChanged(TxGameBridge.GoldAmount);
        initializingCoinState = false;
    }

    public static PhaseCompleteResult NotifyLevelCompleted(int completedLevel)
    {
        Initialize();
        if (config == null || saveData == null)
            return new PhaseCompleteResult(false, PhaseId.None, PhaseFlowSource.None, string.Empty);

        PhaseId phaseAtEntry = CurrentPhaseId;
        int levelBeforeRefresh = saveData.playerLevelProgress.level;
        PlayerLevelSaveData progress = saveData.playerLevelProgress;
        if (progress.totalCompletedLevels < int.MaxValue)
            progress.totalCompletedLevels++;
        else
            Debug.LogWarning("[Tx] totalCompletedLevels 已达上限");

        RefreshPlayerLevelProgress();
        int levelAfterRefresh = saveData.playerLevelProgress.level;
        Save();

        if (phaseAtEntry == PhaseId.Level8 && completedLevel < config.LevelPhase.ActualTargetLevel)
            TrackTask1(completedLevel);
        else if (phaseAtEntry == PhaseId.Coin2000)
            TrackTask3(completedLevel, CurrentGold());
        else if (phaseAtEntry == PhaseId.PlayerLevel30 && levelAfterRefresh > levelBeforeRefresh)
            TrackTask5(levelAfterRefresh);

        if (CurrentPhaseId == PhaseId.Level8 && completedLevel >= config.LevelPhase.ActualTargetLevel)
        {
            return CompletePhase(
                PhaseId.Level8,
                PhaseId.ActiveMinutes20,
                PhaseFlowSource.LevelComplete,
                PhaseRewards.Phase1Reward,
                true,
                true,
            "phase-Level8-stage-" + completedLevel,
            task1Level: completedLevel);

        }

        if (CurrentPhaseId == PhaseId.ActiveDays7)
            return RecordActiveDayWin(DateTime.Now);

        TryCompletePlayerLevelPhase();
        return new PhaseCompleteResult(false, PhaseId.None, PhaseFlowSource.None, string.Empty);
    }

    public static int GetLevelActualTarget()
    {
        Initialize();
        return config == null ? 0 : config.LevelPhase.ActualTargetLevel;
    }

    public static int GetLevelDisplayLevel()
    {
        Initialize();
        int target = config == null ? 0 : config.LevelPhase.ActualTargetLevel;
        if (target <= 0)
            return 1;
        return Mathf.Clamp(GetCurrentLevel(), 1, target);
    }

    public static int GetLevelDisplayNumber()
    {
        Initialize();
        int stage = Mathf.Max(1, GetCurrentLevel());
        //if (CurrentPhaseId == PhaseId.Level8)
        if (stage <= 29)
            return PhaseOneHudCalculator.Get(stage, phaseOneHudConfig).DisplayLevel;
        return stage - 21;
    }

    public static void NotifyProgressRefresh()
    {
        Initialize();
        PhaseTaskEvents.RaiseProgressChanged();
    }

    public static void SetGameplayActive(bool active) { }

    public static void Tick(float activeDeltaSeconds)
    {
        Initialize();
        if (activeDeltaSeconds <= 0f || !CanAdvanceMinutePhase())
            return;

        int previousSecond = Mathf.FloorToInt(saveData.minuteProgress.activeSeconds);
        saveData.minuteProgress.activeSeconds = Mathf.Min(
            saveData.minuteProgress.activeSeconds + activeDeltaSeconds,
            config.ActiveMinutePhase.SlotDurationSeconds);

        if (TryCompleteMinuteSlot())
            return;

        if (Mathf.FloorToInt(saveData.minuteProgress.activeSeconds) > previousSecond)
        {
            Save();
            PhaseTaskEvents.RaiseProgressChanged();
        }
    }

    public static void NotifyArrowEscaped()
    {
        Initialize();
        if (!CanAdvanceMinutePhase())
            return;

        MinuteSlotDefinition slot = GetCurrentMinuteSlot();
        int previous = saveData.minuteProgress.successfulEscapes;
        saveData.minuteProgress.successfulEscapes = Mathf.Min(previous + 1, slot.RequiredSuccessfulEscapes);
        if (saveData.minuteProgress.successfulEscapes == previous)
            return;

        if (!TryCompleteMinuteSlot())
        {
            Save();
            PhaseTaskEvents.RaiseProgressChanged();
        }
    }

    public static void NotifyAmountChanged(float amount)
    {
        Initialize();
        LastKnownGold = amount;
        if (GoldChanged != null)
            GoldChanged(amount);

        if (config == null || CurrentPhaseId != PhaseId.Coin2000 || amount < config.CoinPhase.TargetCoin)
            return;

        CompletePhase(
            PhaseId.Coin2000,
            PhaseId.ActiveDays7,
            PhaseFlowSource.RuntimeProgress,
            PhaseRewards.Phase3Target,
            false,
            false,
            "phase-Coin2000-complete",
            !initializingCoinState);
    }

    public static void SetFlowStep(PhaseFlowStep step)
    {
        Initialize();
        if (saveData?.pendingFlow == null)
            return;

        saveData.pendingFlow.step = step;
        Save();
    }

    public static float ApplyPhaseReward(string flowId)
    {
        Initialize();
        PhaseFlowSaveData flow = saveData?.pendingFlow;
        if (flow == null || flow.flowId != flowId)
            return 0f;

        if (flow.grantedReward <= 0f && !flow.rewardApplied && !IsRewardGranted(flowId))
        {
            PhaseId phaseId = ParsePhaseId(flow.completedPhaseId);
            flow.grantedReward = NormalizeReward(
                phaseId == PhaseId.Level8 || phaseId == PhaseId.Coin2000
                    ? flow.displayReward
                    : Mathf.Max(0f, flow.displayReward - CurrentGold()));
        }

        flow.step = PhaseFlowStep.Granting;
        Save();
        if (!IsRewardGranted(flowId))
            GrantRewardOnce(flowId, flow.grantedReward);

        flow.rewardApplied = true;
        flow.step = PhaseFlowStep.RewardTip;
        Save();
        return flow.grantedReward;
    }

    public static void ActivateNextPhase(string flowId)
    {
        Initialize();
        PhaseFlowSaveData flow = saveData?.pendingFlow;
        if (flow == null || flow.flowId != flowId)
            return;

        PhaseId nextPhase = ParsePhaseId(flow.nextPhaseId);
        SetState(nextPhase, PhaseTaskState.InProgress);
        saveData.currentPhaseId = nextPhase.ToString();
        Save();
        PhaseTaskEvents.RaiseCurrentPhaseChanged(nextPhase);
        PhaseTaskEvents.RaiseProgressChanged();
    }

    public static void CompleteFlow(string flowId)
    {
        Initialize();
        if (saveData?.pendingFlow == null || saveData.pendingFlow.flowId != flowId)
            return;

        saveData.pendingFlow = null;
        Save();
    }

    public static void Flush()
    {
        Initialize();
        if (saveData != null)
            Save();
    }

    public static PhaseTaskState GetState(PhaseId phaseId)
    {
        Initialize();
        if (saveData == null || saveData.phases == null)
            return PhaseTaskState.Locked;

        for (int index = 0; index < saveData.phases.Length; index++)
        {
            PhaseSaveState state = saveData.phases[index];
            if (state.phaseId == phaseId.ToString())
                return state.state;
        }

        return PhaseTaskState.Locked;
    }

    public static PhaseTaskProgress GetProgress(PhaseId phaseId)
    {
        Initialize();
        if (phaseId == PhaseId.Level8 && config != null)
        {
            float target = config.LevelPhase.ActualTargetLevel;
            float current = Mathf.Min(GetCurrentLevel() - 1, target);
            bool complete = GetState(PhaseId.Level8) == PhaseTaskState.Completed;
            return new PhaseTaskProgress(PhaseId.Level8, current, target, target > 0f ? current / target : 0f, 0f, 0f, complete);
        }

        if (phaseId == PhaseId.ActiveMinutes20)
        {
            ActiveMinuteProgress progress = GetMinuteProgress();
            float completedSlots = progress.IsComplete ? progress.SlotCount : Mathf.Max(0, progress.SlotIndex - 1);
            return new PhaseTaskProgress(
                phaseId,
                completedSlots,
                progress.SlotCount,
                progress.TotalNormalized,
                progress.SuccessfulEscapes,
                progress.RequiredEscapes,
                progress.IsComplete);
        }

        if (phaseId == PhaseId.ActiveDays7)
        {
            float target = config.ActiveDayPhase.RequiredActiveDays;
            float current = saveData.activeDayProgress.completedDates.Length;
            bool complete = GetState(phaseId) == PhaseTaskState.Completed;
            return new PhaseTaskProgress(phaseId, current, target, target > 0f ? current / target : 0f, saveData.activeDayProgress.todayWins, config.ActiveDayPhase.WinsRequiredPerDay, complete);
        }

        return new PhaseTaskProgress(phaseId, 0f, 0f, 0f, 0f, 0f, false);
    }

    public static ActiveMinuteProgress GetMinuteProgress()
    {
        Initialize();
        if (!IsMinuteConfigValid() || saveData == null || saveData.minuteProgress == null)
            return new ActiveMinuteProgress(0, 0, 0f, 0f, 0, 0, 0f, 0f, 0f, false, false, false, false);

        int slotIndex = Mathf.Clamp(saveData.minuteProgress.slotIndex, 1, config.ActiveMinutePhase.Slots.Length);
        MinuteSlotDefinition slot = config.ActiveMinutePhase.Slots[slotIndex - 1];
        float requiredSeconds = config.ActiveMinutePhase.SlotDurationSeconds;
        int requiredEscapes = slot.RequiredSuccessfulEscapes;
        float activeSeconds = Mathf.Clamp(saveData.minuteProgress.activeSeconds, 0f, requiredSeconds);
        int successfulEscapes = Mathf.Clamp(saveData.minuteProgress.successfulEscapes, 0, requiredEscapes);
        bool complete = GetState(PhaseId.ActiveMinutes20) == PhaseTaskState.Completed;
        bool timeComplete = activeSeconds >= requiredSeconds;
        bool escapeComplete = successfulEscapes >= requiredEscapes;
        int displaySlotIndex = complete ? MinuteDisplaySlotCount : slotIndex;
        float total = complete ? 1f : (slotIndex - 1f) / MinuteDisplaySlotCount;
        return new ActiveMinuteProgress(
            displaySlotIndex,
            MinuteDisplaySlotCount,
            activeSeconds,
            requiredSeconds,
            successfulEscapes,
            requiredEscapes,
            requiredSeconds > 0f ? activeSeconds / requiredSeconds : 0f,
            requiredEscapes > 0 ? successfulEscapes / (float)requiredEscapes : 0f,
            total,
            timeComplete,
            escapeComplete,
            complete,
            true);
    }

    public static SdkTaskProgress GetCurrentSdkProgress() => GetSdkProgress(CurrentPhaseId);

    public static SdkTaskProgress GetSdkProgress(PhaseId phaseId)
    {
        Initialize();
        if (config == null)
            return new SdkTaskProgress(phaseId, 0f, 0f, 0f, false);

        switch (phaseId)
        {
            case PhaseId.Level8:
                return GetLevelSdkProgress();
            case PhaseId.ActiveMinutes20:
                return GetMinuteSdkProgress();
            case PhaseId.Coin2000:
                return CreateSdkProgress(phaseId, CurrentGold(), config.CoinPhase.TargetCoin);
            case PhaseId.ActiveDays7:
                return CreateSdkProgress(phaseId, saveData.activeDayProgress.completedDates.Length, config.ActiveDayPhase.RequiredActiveDays);
            case PhaseId.PlayerLevel30:
                return new SdkTaskProgress(phaseId, 1f, config.PlayerLevelPhase.TargetPlayerLevel, 0f, true);
            default:
                return new SdkTaskProgress(phaseId, 0f, 0f, 0f, false);
        }
    }

    public static PhaseTaskPresentation GetCurrentPresentation() => GetPresentation(CurrentPhaseId);

    public static PhaseTaskPresentation GetPresentation(PhaseId phaseId)
    {
        PhaseTaskProgress progress = GetProgress(phaseId);
        return new PhaseTaskPresentation(phaseId, GetState(phaseId), string.Empty, string.Empty, GetCurrentProgressText(), string.Empty, progress);
    }

    public static string GetCurrentTaskText()
    {
        Initialize();
        if (config == null)
            return "Complete the current task";

        string action = TxGameBridge.GetWithdraw();
        switch (CurrentPhaseId)
        {
            case PhaseId.Level8:
                return "Clear level " + config.LevelPhase.DisplayTargetLevel + " to <color=#FF6B6B>" + action + " all</color>";
            case PhaseId.ActiveMinutes20:
                return "Play " + MinuteDisplaySlotCount + " more minutes to " + action + " all";
            case PhaseId.Coin2000:
                return "Reach " + TxGameBridge.GetMark() + config.CoinPhase.TargetCoin.ToString("0") + " to " + action + " all instantly";
            case PhaseId.ActiveDays7:
                return "Stay active for " + config.ActiveDayPhase.RequiredActiveDays + " days to " + action + " all";
            case PhaseId.PlayerLevel30:
                return "Reach Lv." + config.PlayerLevelPhase.TargetPlayerLevel + " to " + action + " all instantly";
            default:
                return "Complete the current task";
        }
    }

    public static string GetCurrentProgressText()
    {
        if (CurrentPhaseId == PhaseId.ActiveMinutes20)
        {
            ActiveMinuteProgress minuteProgress = GetMinuteProgress();
            return minuteProgress.IsValid ? $"Minute {minuteProgress.SlotIndex}/{minuteProgress.SlotCount}" : string.Empty;
        }

        PhaseTaskProgress progress = GetProgress(CurrentPhaseId);
        return progress.Target > 0f ? $"{progress.Current:0}/{progress.Target:0}" : string.Empty;
    }

    static float CurrentGold() => TxGameBridge.GoldAmount;

    static int GetCurrentLevel() => TxGameBridge.CurrentLevel;

    static SdkTaskProgress GetLevelSdkProgress()
    {
        int target = config.LevelPhase.DisplayTargetLevel;
        bool complete = GetState(PhaseId.Level8) == PhaseTaskState.Completed;
        int actualLevel = complete ? config.LevelPhase.ActualTargetLevel : Mathf.Clamp(GetCurrentLevel(), 1, config.LevelPhase.ActualTargetLevel);
        PhaseOneHudProgress hudProgress = PhaseOneHudCalculator.Get(actualLevel, phaseOneHudConfig);
        float normalized = complete
            ? 1f
            : CalculateLevelSdkNormalized(hudProgress, target);
        return new SdkTaskProgress(PhaseId.Level8, hudProgress.DisplayLevel, target, normalized, hudProgress.IsValid);
    }

    static float CalculateLevelSdkNormalized(PhaseOneHudProgress progress, int target)
    {
        if (!progress.IsValid || target <= 0)
            return 0f;

        float subProgress = progress.ShowPercent
            ? progress.Percent
            : progress.DotCount > 0 ? progress.CompletedDots / (float)progress.DotCount : 0f;
        return Mathf.Clamp01((progress.DisplayLevel - 1 + subProgress) / target);
    }

    static SdkTaskProgress GetMinuteSdkProgress()
    {
        ActiveMinuteProgress progress = GetMinuteProgress();
        if (!progress.IsValid)
            return new SdkTaskProgress(PhaseId.ActiveMinutes20, 0f, 0f, 0f, false);

        float normalized = progress.IsComplete
            ? 1f
            : ((progress.SlotIndex - 1) + Mathf.Min(progress.TimeNormalized, progress.EscapeNormalized)) / progress.SlotCount;
        return new SdkTaskProgress(PhaseId.ActiveMinutes20, progress.SlotIndex, progress.SlotCount, normalized, true);
    }

    static SdkTaskProgress CreateSdkProgress(PhaseId phaseId, float current, float target)
    {
        float clampedCurrent = Mathf.Clamp(current, 0f, target);
        bool complete = GetState(phaseId) == PhaseTaskState.Completed;
        return new SdkTaskProgress(
            phaseId,
            clampedCurrent,
            target,
            complete ? 1f : target > 0f ? clampedCurrent / target : 0f,
            target > 0f);
    }

    static void RefreshPlayerLevelProgress()
    {
        PlayerLevelCalculation calculation = PlayerLevelCalculator.Calculate(
            saveData.playerLevelProgress.totalCompletedLevels,
            config.PlayerLevelPhase.LevelRequirements);
        saveData.playerLevelProgress.level = calculation.Level;
        saveData.playerLevelProgress.completedLevelsInCurrentLevel = calculation.CurrentCompletedLevels;
    }

    static void TryCompletePlayerLevelPhase()
    {
        if (CurrentPhaseId != PhaseId.PlayerLevel30 || IsAllPhaseCompleted)
            return;

        PlayerLevelProgress progress = GetPlayerLevelProgress();
        if (!progress.IsValid || progress.Level < config.PlayerLevelPhase.TargetPlayerLevel)
            return;

        SetState(PhaseId.PlayerLevel30, PhaseTaskState.Completed);
        saveData.finalQualificationRecorded = true;
        Save();
        PhaseTaskEvents.RaisePhaseCompleted(PhaseId.PlayerLevel30);
        PhaseTaskEvents.RaiseProgressChanged();
        PhaseTaskEvents.RaiseFinalQualificationReached();
    }

    public static PlayerLevelProgress GetPlayerLevelProgress()
    {
        Initialize();
        if (config == null || saveData?.playerLevelProgress == null)
            return new PlayerLevelProgress(1, 0, 0, 0, false);

        PlayerLevelSaveData saved = saveData.playerLevelProgress;
        PlayerLevelCalculation calculation = PlayerLevelCalculator.Calculate(saved.totalCompletedLevels, config.PlayerLevelPhase.LevelRequirements);
        return new PlayerLevelProgress(
            calculation.Level,
            calculation.CurrentCompletedLevels,
            calculation.RequiredCompletedLevels,
            saved.totalCompletedLevels,
            calculation.IsValid);
    }

    static PhaseCompleteResult RecordActiveDayWin(DateTime date)
    {
        ActiveDaySaveData progress = saveData.activeDayProgress;
        string currentDate = date.ToString("yyyy-MM-dd");
        if (progress.currentDate != currentDate)
        {
            if (config.ActiveDayPhase.RequireConsecutiveDays
                && DateTime.TryParse(progress.currentDate, out DateTime previousDate)
                && date.Date > previousDate.Date
                && previousDate.Date != date.Date.AddDays(-1))
            {
                progress.completedDates = Array.Empty<string>();
            }

            progress.currentDate = currentDate;
            progress.todayWins = 0;
        }

        progress.todayWins++;
        if (progress.todayWins < config.ActiveDayPhase.WinsRequiredPerDay || ContainsDate(progress.completedDates, currentDate))
        {
            Save();
            PhaseTaskEvents.RaiseProgressChanged();
            return new PhaseCompleteResult(false, PhaseId.None, PhaseFlowSource.None, string.Empty);
        }

        AddCompletedDate(progress, currentDate);
        int completedDay = progress.completedDates.Length;
        if (completedDay < config.ActiveDayPhase.RequiredActiveDays)
        {
            Save();
            TrackTask4(completedDay);
            PhaseTaskEvents.RaiseProgressChanged();
            return new PhaseCompleteResult(false, PhaseId.None, PhaseFlowSource.None, string.Empty);
        }

        return CompletePhase(
            PhaseId.ActiveDays7,
            PhaseId.PlayerLevel30,
            PhaseFlowSource.RuntimeProgress,
            0f,
            false,
            false,
            "phase-ActiveDays7-complete",
            task4Day: completedDay);
    }

    static bool CanAdvanceMinutePhase()
    {
        return IsMinuteConfigValid()
               && CurrentPhaseId == PhaseId.ActiveMinutes20
               && GetState(PhaseId.ActiveMinutes20) == PhaseTaskState.InProgress;
    }

    static MinuteSlotDefinition GetCurrentMinuteSlot() =>
        config.ActiveMinutePhase.Slots[saveData.minuteProgress.slotIndex - 1];

    static bool TryCompleteMinuteSlot()
    {
        MinuteSlotDefinition slot = GetCurrentMinuteSlot();
        if (saveData.minuteProgress.activeSeconds < config.ActiveMinutePhase.SlotDurationSeconds
            || saveData.minuteProgress.successfulEscapes < slot.RequiredSuccessfulEscapes)
            return false;

        int completedNode = saveData.minuteProgress.slotIndex;
        if (completedNode < config.ActiveMinutePhase.Slots.Length)
        {
            saveData.minuteProgress.slotIndex++;
            saveData.minuteProgress.activeSeconds = 0f;
            saveData.minuteProgress.successfulEscapes = 0;
            Save();
            TrackTask2(completedNode);
            PhaseTaskEvents.RaiseProgressChanged();
            return true;
        }

        CompletePhase(
            PhaseId.ActiveMinutes20,
            PhaseId.Coin2000,
            PhaseFlowSource.RuntimeProgress,
            PhaseRewards.Phase2Target,
            false,
            false,
            "phase-ActiveMinutes20-complete",
            task2Node: completedNode);
        return true;
    }

    static PhaseCompleteResult CompletePhase(
        PhaseId completedPhase,
        PhaseId nextPhase,
        PhaseFlowSource source,
        float displayReward,
        bool requiresLevelTaskTip,
        bool waitsForNextLevel,
        string flowId,
        bool trackCompletion = true,
        int task4Day = 0,
        int task2Node = 0,
        int task1Level = 0)
    {
        if (GetState(completedPhase) == PhaseTaskState.Completed || saveData.pendingFlow != null)
            return new PhaseCompleteResult(false, PhaseId.None, PhaseFlowSource.None, string.Empty);

        SetState(completedPhase, PhaseTaskState.Completed);
        saveData.pendingFlow = new PhaseFlowSaveData
        {
            flowId = flowId,
            completedPhaseId = completedPhase.ToString(),
            nextPhaseId = nextPhase.ToString(),
            step = PhaseFlowStep.Pending,
            source = source,
            displayReward = displayReward,
            requiresLevelTaskTip = requiresLevelTaskTip,
            waitsForNextLevel = waitsForNextLevel,
        };
        Save();
        if (task1Level > 0)
            TrackTask1(task1Level);
        if (task2Node > 0)
            TrackTask2(task2Node);
        if (task4Day > 0)
            TrackTask4(task4Day);
        if (trackCompletion)
            TrackComplete(completedPhase);
        PhaseTaskEvents.RaisePhaseCompleted(completedPhase);
        PhaseTaskEvents.RaiseProgressChanged();
        return new PhaseCompleteResult(true, completedPhase, source, flowId);
    }

    static bool IsMinuteConfigValid()
    {
        if (config != null
            && config.ActiveMinutePhase != null
            && config.ActiveMinutePhase.SlotDurationSeconds == MinuteSlotDuration
            && !config.ActiveMinutePhase.CarryEscapesToNextSlot
            && config.ActiveMinutePhase.Slots != null
            && config.ActiveMinutePhase.Slots.Length == MinuteSlotCount)
        {
            MinuteSlotDefinition[] slots = config.ActiveMinutePhase.Slots;
            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null
                    || slots[index].SlotIndex != index + 1
                    || slots[index].RequiredSuccessfulEscapes != MinuteEscapeTargets[index])
                    return LogInvalidMinuteConfig();
            }
            return true;
        }

        return LogInvalidMinuteConfig();
    }

    static bool LogInvalidMinuteConfig()
    {
        if (!minuteConfigErrorLogged)
        {
            minuteConfigErrorLogged = true;
            Debug.LogError("[Tx] 分钟档位配置不合法");
        }
        return false;
    }

    static void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        saveData = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<PhaseTaskSaveData>(json);
    }

    static void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
    }

    static void EnsureState()
    {
        bool changed = false;
        if (saveData == null)
        {
            saveData = new PhaseTaskSaveData();
            changed = true;
        }

        if (saveData.schemaVersion != SaveVersion)
        {
            saveData.schemaVersion = SaveVersion;
            changed = true;
        }

        changed |= RepairPhaseStates();
        if (saveData.minuteProgress == null)
        {
            saveData.minuteProgress = new MinutePhaseSaveData { slotIndex = 1 };
            changed = true;
        }
        if (saveData.activeDayProgress == null)
        {
            saveData.activeDayProgress = new ActiveDaySaveData { completedDates = Array.Empty<string>() };
            changed = true;
        }
        else if (saveData.activeDayProgress.completedDates == null)
        {
            saveData.activeDayProgress.completedDates = Array.Empty<string>();
            changed = true;
        }
        if (saveData.playerLevelProgress == null)
        {
            int baseline = Mathf.Max(0, GetCurrentLevel() - 1);
            saveData.playerLevelProgress = new PlayerLevelSaveData { totalCompletedLevels = baseline };
            changed = true;
        }
        int originalLevel = saveData.playerLevelProgress.level;
        int originalInLevel = saveData.playerLevelProgress.completedLevelsInCurrentLevel;
        int originalTotal = saveData.playerLevelProgress.totalCompletedLevels;
        saveData.playerLevelProgress.totalCompletedLevels = Mathf.Max(0, originalTotal);
        RefreshPlayerLevelProgress();
        changed |= originalLevel != saveData.playerLevelProgress.level
                   || originalInLevel != saveData.playerLevelProgress.completedLevelsInCurrentLevel
                   || originalTotal != saveData.playerLevelProgress.totalCompletedLevels;

        if (IsMinuteConfigValid() && GetState(PhaseId.ActiveMinutes20) != PhaseTaskState.Completed)
            changed |= RepairMinuteProgress();
        changed |= RepairPendingFlow();

        if (changed)
            Save();
    }

    static bool RepairPhaseStates()
    {
        string originalCurrentPhaseId = saveData.currentPhaseId;
        string originalPhases = saveData.phases == null ? string.Empty : JsonUtility.ToJson(new PhaseStatesSnapshot { phases = saveData.phases });
        PhaseTaskState[] states = new PhaseTaskState[5];
        bool[] found = new bool[5];
        if (saveData.phases != null)
        {
            for (int index = 0; index < saveData.phases.Length; index++)
            {
                PhaseSaveState entry = saveData.phases[index];
                if (entry == null || !Enum.TryParse(entry.phaseId, out PhaseId phaseId) || phaseId == PhaseId.None)
                    continue;

                int phaseIndex = (int)phaseId - 1;
                if (!found[phaseIndex])
                {
                    states[phaseIndex] = entry.state;
                    found[phaseIndex] = true;
                }
            }
        }

        PhaseId current = ParsePhaseId(saveData.currentPhaseId);
        if (current == PhaseId.None)
            current = PhaseId.Level8;
        int currentIndex = (int)current - 1;
        for (int index = 0; index < states.Length; index++)
        {
            if (!found[index])
                states[index] = index < currentIndex ? PhaseTaskState.Completed : index == currentIndex ? PhaseTaskState.InProgress : PhaseTaskState.Locked;
            else if (index < currentIndex && states[index] == PhaseTaskState.Locked)
                states[index] = PhaseTaskState.Completed;
            else if (index > currentIndex)
                states[index] = PhaseTaskState.Locked;
        }

        if (states[currentIndex] == PhaseTaskState.Locked)
            states[currentIndex] = PhaseTaskState.InProgress;

        saveData.currentPhaseId = current.ToString();
        saveData.phases = new PhaseSaveState[states.Length];
        for (int index = 0; index < states.Length; index++)
        {
            saveData.phases[index] = CreateState((PhaseId)(index + 1), states[index]);
        }
        return originalPhases != JsonUtility.ToJson(new PhaseStatesSnapshot { phases = saveData.phases }) || originalCurrentPhaseId != saveData.currentPhaseId;
    }

    static bool RepairPendingFlow()
    {
        PhaseFlowSaveData flow = saveData.pendingFlow;
        if (flow != null && flow.step == PhaseFlowStep.Completed)
        {
            saveData.pendingFlow = null;
            return true;
        }

        if (flow == null && GetState(CurrentPhaseId) == PhaseTaskState.Completed && CurrentPhaseId != PhaseId.PlayerLevel30)
        {
            saveData.pendingFlow = CreateFlowForCompletedPhase(CurrentPhaseId, PhaseFlowSource.Restore);
            if (saveData.pendingFlow != null)
                saveData.pendingFlow.step = PhaseFlowStep.Restore;
            return saveData.pendingFlow != null;
        }

        if (flow == null)
            return false;

        if (!Enum.TryParse(flow.completedPhaseId, out PhaseId completed)
            || !Enum.TryParse(flow.nextPhaseId, out PhaseId next)
            || completed == PhaseId.None
            || next == PhaseId.None
            || (CurrentPhaseId != completed && CurrentPhaseId != next)
            || GetState(completed) != PhaseTaskState.Completed)
        {
            saveData.pendingFlow = GetState(CurrentPhaseId) == PhaseTaskState.Completed
                ? CreateFlowForCompletedPhase(CurrentPhaseId, PhaseFlowSource.Restore)
                : null;
            if (saveData.pendingFlow != null)
                saveData.pendingFlow.step = PhaseFlowStep.Restore;
            return true;
        }

        if (flow.step == PhaseFlowStep.Granting && IsRewardGranted(flow.flowId))
        {
            flow.rewardApplied = true;
            flow.step = PhaseFlowStep.RewardTip;
            return true;
        }
        if (flow.rewardApplied && !IsRewardGranted(flow.flowId))
        {
            flow.rewardApplied = false;
            flow.step = PhaseFlowStep.Granting;
            return true;
        }
        if (flow.step == PhaseFlowStep.Granting)
            return false;

        return false;
    }

    static PhaseFlowSaveData CreateFlowForCompletedPhase(PhaseId phaseId, PhaseFlowSource source)
    {
        switch (phaseId)
        {
            case PhaseId.Level8:
                return CreateFlow(phaseId, PhaseId.ActiveMinutes20, source, PhaseRewards.Phase1Reward, true, true, "phase-Level8-restore");
            case PhaseId.ActiveMinutes20:
                return CreateFlow(phaseId, PhaseId.Coin2000, source, PhaseRewards.Phase2Target, false, false, "phase-ActiveMinutes20-complete");
            case PhaseId.Coin2000:
                return CreateFlow(phaseId, PhaseId.ActiveDays7, source, PhaseRewards.Phase3Target, false, false, "phase-Coin2000-complete");
            case PhaseId.ActiveDays7:
                return CreateFlow(phaseId, PhaseId.PlayerLevel30, source, 0f, false, false, "phase-ActiveDays7-complete");
            default:
                return null;
        }
    }

    static PhaseFlowSaveData CreateFlow(PhaseId completed, PhaseId next, PhaseFlowSource source, float reward, bool requiresTip, bool waitsForNextLevel, string flowId) => new PhaseFlowSaveData
    {
        flowId = flowId,
        completedPhaseId = completed.ToString(),
        nextPhaseId = next.ToString(),
        step = PhaseFlowStep.Pending,
        source = source,
        displayReward = reward,
        requiresLevelTaskTip = requiresTip,
        waitsForNextLevel = waitsForNextLevel,
    };

    static bool RepairMinuteProgress()
    {
        MinutePhaseSaveData progress = saveData.minuteProgress;
        int originalSlotIndex = progress.slotIndex;
        float originalSeconds = progress.activeSeconds;
        int originalEscapes = progress.successfulEscapes;
        int slotCount = config.ActiveMinutePhase.Slots.Length;
        progress.slotIndex = Mathf.Clamp(progress.slotIndex, 1, slotCount);
        progress.activeSeconds = Mathf.Clamp(progress.activeSeconds, 0f, config.ActiveMinutePhase.SlotDurationSeconds);
        int target = config.ActiveMinutePhase.Slots[progress.slotIndex - 1].RequiredSuccessfulEscapes;
        progress.successfulEscapes = Mathf.Clamp(progress.successfulEscapes, 0, target);
        if (originalSlotIndex > slotCount)
            Debug.LogWarning("[Tx] minuteProgress.slotIndex 越界，已修正");
        return originalSlotIndex != progress.slotIndex
               || !Mathf.Approximately(originalSeconds, progress.activeSeconds)
               || originalEscapes != progress.successfulEscapes;
    }

    static PhaseSaveState CreateState(PhaseId phaseId, PhaseTaskState state) => new PhaseSaveState
    {
        phaseId = phaseId.ToString(),
        state = state,
    };

    static void SetState(PhaseId phaseId, PhaseTaskState state)
    {
        for (int index = 0; index < saveData.phases.Length; index++)
        {
            if (saveData.phases[index].phaseId == phaseId.ToString())
            {
                saveData.phases[index].state = state;
                return;
            }
        }
    }

    static void GrantRewardOnce(string rewardId, float amount)
    {
        if (string.IsNullOrEmpty(rewardId) || IsRewardGranted(rewardId))
            return;

        TxGameBridge.AddGold(NormalizeReward(amount));
        string stored = PlayerPrefs.GetString(TxSaveKeys.RewardIds, string.Empty);
        PlayerPrefs.SetString(TxSaveKeys.RewardIds, string.IsNullOrEmpty(stored) ? rewardId : stored + "|" + rewardId);
        PlayerPrefs.Save();
    }

    static bool IsRewardGranted(string rewardId)
    {
        if (string.IsNullOrEmpty(rewardId))
            return false;

        string stored = PlayerPrefs.GetString(TxSaveKeys.RewardIds, string.Empty);
        if (string.IsNullOrEmpty(stored))
            return false;

        string[] ids = stored.Split('|');
        for (int index = 0; index < ids.Length; index++)
        {
            if (ids[index] == rewardId)
                return true;
        }

        return false;
    }

    [Serializable]
    sealed class PhaseStatesSnapshot
    {
        public PhaseSaveState[] phases;
    }

    static readonly PhaseRewardConfig FallbackPhaseRewards = new PhaseRewardConfig();
    static PhaseRewardConfig PhaseRewards => config.PhaseReward ?? FallbackPhaseRewards;

    static float NormalizeReward(float value) => Mathf.Round(Mathf.Max(0f, value) * 100f) / 100f;

    static void AddCompletedDate(ActiveDaySaveData progress, string date)
    {
        string[] dates = progress.completedDates ?? Array.Empty<string>();
        Array.Resize(ref dates, dates.Length + 1);
        dates[dates.Length - 1] = date;
        progress.completedDates = dates;
    }

    static bool ContainsDate(string[] dates, string date)
    {
        if (dates == null)
            return false;

        for (int index = 0; index < dates.Length; index++)
        {
            if (dates[index] == date)
                return true;
        }

        return false;
    }

    static PhaseId ParsePhaseId(string value) =>
        Enum.TryParse(value, out PhaseId phaseId) && phaseId != PhaseId.None ? phaseId : PhaseId.Level8;

    static void TrackTask1(int levelId) => TrackIncreasing(SolarEventNames.Task1, TxGameBridge.SourceLevelId, levelId, levelId, 1, 29, data => data.task1, (data, value) => data.task1 = value);

    static void TrackTask2(int node) => TrackIncreasing(SolarEventNames.Task2, TxGameBridge.SourceNode, node, node, 1, 19, data => data.task2, (data, value) => data.task2 = value);

    static void TrackTask3(int levelId, float amount)
    {
        float normalized = Mathf.Round(amount * 100f) / 100f;
        TrackIncreasing(SolarEventNames.Task3, TxGameBridge.SourceAmount, normalized, levelId, 1, int.MaxValue, data => data.task3, (data, value) => data.task3 = value);
    }

    static void TrackTask4(int day) => TrackIncreasing(SolarEventNames.Task4, TxGameBridge.SourceDay, day, day, 1, 7, data => data.task4, (data, value) => data.task4 = value);

    static void TrackTask5(int level) => TrackIncreasing(SolarEventNames.Task5, TxGameBridge.SourceLevel, level, level, 1, int.MaxValue, data => data.task5, (data, value) => data.task5 = value);

    static void TrackComplete(PhaseId phaseId)
    {
        int phaseNumber = phaseId switch
        {
            PhaseId.Level8 => 1,
            PhaseId.ActiveMinutes20 => 2,
            PhaseId.Coin2000 => 3,
            PhaseId.ActiveDays7 => 4,
            _ => 0,
        };
        if (phaseNumber == 0)
            return;

        TaskMark data = LoadMark();
        int mask = 1 << (phaseNumber - 1);
        if ((data.completeMask & mask) != 0)
            return;

        string eventName = phaseNumber switch
        {
            1 => SolarEventNames.TaskDone1,
            2 => SolarEventNames.TaskDone2,
            3 => SolarEventNames.TaskDone3,
            _ => SolarEventNames.TaskDone4,
        };
        TxGameBridge.Track(eventName);
        data.completeMask |= mask;
        SaveMark(data);
    }

    static void TrackIncreasing(string eventName, string key, object value, int sequence, int minimum, int maximum, Func<TaskMark, int> read, Action<TaskMark, int> write)
    {
        if (sequence < minimum || sequence > maximum)
            return;

        TaskMark data = LoadMark();
        if (sequence <= read(data))
            return;

        TxGameBridge.Track(eventName, key, value);
        write(data, sequence);
        SaveMark(data);
    }

    [Serializable]
    sealed class TaskMark
    {
        public int task1;
        public int task2;
        public int task3;
        public int task4;
        public int task5;
        public int completeMask;
    }

    static TaskMark mark;

    static TaskMark LoadMark()
    {
        if (mark != null)
            return mark;

        string json = PlayerPrefs.GetString(TxSaveKeys.TaskEventWatermark, string.Empty);
        mark = string.IsNullOrEmpty(json) ? new TaskMark() : JsonUtility.FromJson<TaskMark>(json);
        if (mark == null)
            mark = new TaskMark();
        return mark;
    }

    static void SaveMark(TaskMark data)
    {
        mark = data;
        PlayerPrefs.SetString(TxSaveKeys.TaskEventWatermark, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR || TX_DEBUG_TOOLS
    public static bool DebugAdvanceMinuteSlot()
    {
        Initialize();
        if (!CanDebugAdvancePhase(PhaseId.ActiveMinutes20) || !CanAdvanceMinutePhase())
        {
            Debug.LogWarning("[Tx] 无法跳到下一分钟档");
            return false;
        }

        ActiveMinuteProgress before = GetMinuteProgress();
        saveData.minuteProgress.activeSeconds = before.RequiredSeconds;
        saveData.minuteProgress.successfulEscapes = before.RequiredEscapes;
        TryCompleteMinuteSlot();
        return true;
    }

    public static bool DebugAddCoin(float amount)
    {
        Initialize();
        if (amount <= 0f || !CanDebugAdvancePhase(PhaseId.Coin2000))
        {
            Debug.LogWarning("[Tx] 无法增加金币进度");
            return false;
        }

        TxGameBridge.AddGold(amount);
        NotifyAmountChanged(TxGameBridge.GoldAmount);
        return true;
    }

    public static bool DebugAdvanceActiveDay()
    {
        Initialize();
        if (!CanDebugAdvancePhase(PhaseId.ActiveDays7))
        {
            Debug.LogWarning("[Tx] 无法跳到下一活跃日");
            return false;
        }

        ActiveDaySaveData progress = saveData.activeDayProgress;
        DateTime date = GetNextDebugActiveDate();
        string dateText = date.ToString("yyyy-MM-dd");
        while (!ContainsDate(progress.completedDates, dateText) && !HasPendingFlow)
            RecordActiveDayWin(date);
        return true;
    }

    public static bool DebugAddCoinFromZero(float amount) => DebugAddCoin(amount);

    static DateTime GetNextDebugActiveDate()
    {
        DateTime latest = DateTime.MinValue;
        string[] dates = saveData.activeDayProgress.completedDates;
        for (int index = 0; index < dates.Length; index++)
        {
            if (DateTime.TryParse(dates[index], out DateTime parsed))
            {
                if (parsed.Date > latest)
                    latest = parsed.Date;
            }
        }

        DateTime candidate = latest == DateTime.MinValue ? DateTime.Today.AddDays(-1) : latest.AddDays(1);
        while (ContainsDate(dates, candidate.ToString("yyyy-MM-dd")))
            candidate = candidate.AddDays(1);
        return candidate;
    }

    static bool CanDebugAdvancePhase(PhaseId phaseId)
    {
        return config != null
               && saveData != null
               && !HasPendingFlow
               && CurrentPhaseId == phaseId
               && GetState(phaseId) == PhaseTaskState.InProgress;
    }
#endif
}
