using System.Collections;
using UnityEngine;

public sealed class PhaseFlowCoordinator : MonoBehaviour
{
    public static PhaseFlowCoordinator Instance { get; private set; }

    bool running;
    string closingFlowId;
    string deferredFlowId;

    public bool IsRunning => running;

    void Awake() => Instance = this;

    void Update()
    {
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (!running && flow != null && !IsResumeBlocked(flow) && CanStartFromGameplay())
            TryResume();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public IEnumerator RunLevelFlow(PhaseCompleteResult result)
    {
        if (!result.Completed)
            yield break;

        TryResume();
        while (ShouldWaitLevelFlow(PhaseTaskService.GetPendingFlow()))
            yield return null;
    }

    bool CanStartFromGameplay()
    {
        if (UIManager.Instance == null)
            return false;

        return true;
    }

    public void TryResume()
    {
        if (running || UIManager.Instance == null)
            return;

        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (flow == null || IsResumeBlocked(flow))
            return;

        if ((flow.source == PhaseFlowSource.LevelComplete || ParsePhase(flow.completedPhaseId) == PhaseId.Level8)
            && (flow.step == PhaseFlowStep.Pending || flow.step == PhaseFlowStep.Restore))
        {
            UIManager.Instance.OpenUI<CandyPanel>();
            return;
        }

        RunFlow();
    }

    public bool BeginDefer(string flowId)
    {
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (!CanDefer(flow, flowId))
            return false;

        closingFlowId = flowId;
        if (deferredFlowId == flowId)
            deferredFlowId = string.Empty;
        return true;
    }

    public void CompleteDefer(string flowId)
    {
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (closingFlowId != flowId || !CanDefer(flow, flowId))
            return;

        closingFlowId = string.Empty;
        deferredFlowId = flowId;
    }

    public void CancelDefer(string flowId)
    {
        if (closingFlowId == flowId)
            closingFlowId = string.Empty;
    }

    public void RunFromSdkPanel()
    {
        if (running || !PhaseTaskService.IsCurrentPhaseCompleted)
            return;

        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (flow != null)
            ClearDefer(flow.flowId);
        UIManager.Instance.HideUI<CandyPanel>();
        RunFlow();
    }

    bool IsResumeBlocked(PhaseFlowSaveData flow) =>
        flow != null && (flow.flowId == closingFlowId || flow.flowId == deferredFlowId);

    bool ShouldWaitLevelFlow(PhaseFlowSaveData flow) =>
        flow != null && flow.flowId != deferredFlowId;

    bool CanDefer(PhaseFlowSaveData flow, string flowId)
    {
        return flow != null
            && flow.flowId == flowId
            && ParsePhase(flow.completedPhaseId) == PhaseId.Level8
            && (flow.step == PhaseFlowStep.Pending || flow.step == PhaseFlowStep.Restore)
            && PhaseTaskService.GetState(PhaseId.Level8) == PhaseTaskState.Completed;
    }

    void ClearDefer(string flowId)
    {
        if (closingFlowId == flowId)
            closingFlowId = string.Empty;
        if (deferredFlowId == flowId)
            deferredFlowId = string.Empty;
    }

    void RunFlow()
    {
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (flow == null)
            return;

        running = true;
        if (flow.step == PhaseFlowStep.Granting || flow.step == PhaseFlowStep.RewardTip)
        {
            ResumeReward(flow);
            return;
        }

        PhaseId phaseId = ParsePhase(flow.completedPhaseId);
        if (flow.step == PhaseFlowStep.ProcessComplete || phaseId == PhaseId.Coin2000)
        {
            PhaseTaskService.SetFlowStep(PhaseFlowStep.ProcessComplete);
            UIManager.Instance.OpenUI<ProcessCompletePop>(new ProcessCompletePopData(OpenInProgress));
            return;
        }

        if (flow.step == PhaseFlowStep.SpecialBonus || phaseId == PhaseId.ActiveMinutes20)
        {
            OpenSpecialBonus();
            return;
        }

        OpenInProgress();
    }

    void OpenInProgress()
    {
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (flow == null)
        {
            running = false;
            return;
        }

        PhaseTaskService.SetFlowStep(PhaseFlowStep.InProgress);
        UIManager.Instance.OpenUI<InProgressPop>(new InProgressPopData(ParsePhase(flow.completedPhaseId), OnInProgressContinued));
    }

    void OnInProgressContinued()
    {
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (flow == null)
        {
            running = false;
            return;
        }

        if (ParsePhase(flow.completedPhaseId) == PhaseId.ActiveDays7)
        {
            PhaseTaskService.ActivateNextPhase(flow.flowId);
            FinishFlow(flow.flowId);
            return;
        }

        OpenSpecialBonus();
    }

    void OpenSpecialBonus()
    {
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (flow == null)
        {
            running = false;
            return;
        }

        PhaseTaskService.SetFlowStep(PhaseFlowStep.SpecialBonus);
        UIManager.Instance.OpenUI<SpecialBonusPop>(new SpecialBonusPopData(flow.displayReward, ClaimReward));
    }

    void ClaimReward()
    {
        PhaseFlowSaveData flow = PhaseTaskService.GetPendingFlow();
        if (flow == null)
        {
            running = false;
            return;
        }

        PhaseTaskService.ApplyPhaseReward(flow.flowId);
        ResumeReward(PhaseTaskService.GetPendingFlow());
    }

    void ResumeReward(PhaseFlowSaveData flow)
    {
        if (flow == null)
        {
            running = false;
            return;
        }

        if (flow.step == PhaseFlowStep.Granting)
            PhaseTaskService.ApplyPhaseReward(flow.flowId);

        flow = PhaseTaskService.GetPendingFlow();
        if (flow == null)
        {
            running = false;
            return;
        }

        PhaseTaskService.ActivateNextPhase(flow.flowId);
        if (flow.grantedReward <= 0f)
        {
            FinishFlow(flow.flowId);
            return;
        }

        UIManager.Instance.OpenUI<RewardTipPanel>(new RewardTipPanelData(
            flow.grantedReward,
            () => FinishFlow(flow.flowId)));
    }

    void FinishFlow(string flowId)
    {
        PhaseTaskService.CompleteFlow(flowId);
        running = false;
    }

    static PhaseId ParsePhase(string value) =>
        System.Enum.TryParse(value, out PhaseId phaseId) ? phaseId : PhaseId.None;
}
