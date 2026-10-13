using UnityEngine;

public sealed class TxHudSliders : MonoBehaviour
{
    public Task1Slider task1Slider;
    public Task2Slider task2Slider;
    public Task3Slider task3Slider;
    public Task4Slider task4Slider;
    public Task5Slider task5Slider;

    void Awake()
    {
        if (task1Slider == null)
            task1Slider = FindChild<Task1Slider>("Task1Slider");
        if (task2Slider == null)
            task2Slider = FindChild<Task2Slider>("Task2Slider");
        if (task3Slider == null)
            task3Slider = FindChild<Task3Slider>("Task3Slider");
        if (task4Slider == null)
            task4Slider = FindChild<Task4Slider>("Task4Slider");
        if (task5Slider == null)
            task5Slider = FindChild<Task5Slider>("Task5Slider");
    }

    void OnEnable()
    {
        PhaseTaskEvents.CurrentPhaseChanged += OnCurrentPhaseChanged;
        PhaseTaskEvents.ProgressChanged += RefreshAll;
    }

    void OnDisable()
    {
        PhaseTaskEvents.CurrentPhaseChanged -= OnCurrentPhaseChanged;
        PhaseTaskEvents.ProgressChanged -= RefreshAll;
    }

    void Start() => RefreshAll();

    void OnCurrentPhaseChanged(PhaseId phaseId) => RefreshAll();

    T FindChild<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }

    public void RefreshAll()
    {
        PhaseId phaseId = PhaseTaskService.CurrentPhaseId;

        if (phaseId == PhaseId.None)
            phaseId = PhaseId.Level8;

        if (phaseId == PhaseId.PlayerLevel30 && PhaseTaskService.IsAllPhaseCompleted)
        {
            CloseAll();
            return;
        }

        CloseAll();

        switch (phaseId)
        {
            case PhaseId.Level8:
                task1Slider?.Open();
                if (task1Slider != null)
                    task1Slider.Refresh(
                        PhaseOneHudCalculator.Get(PhaseTaskService.GetLevelDisplayLevel(), task1Slider.Config),
                        PhaseTaskService.GetState(PhaseId.Level8) == PhaseTaskState.Completed);
                break;
            case PhaseId.ActiveMinutes20:
                task2Slider?.Open();
                task2Slider?.Refresh(PhaseTaskService.GetMinuteProgress());
                break;
            case PhaseId.Coin2000:
                task3Slider?.Open();
                task3Slider?.Refresh();
                break;
            case PhaseId.ActiveDays7:
                task4Slider?.Open();
                task4Slider?.Refresh();
                break;
            case PhaseId.PlayerLevel30:
                task5Slider?.Open();
                task5Slider?.Refresh();
                break;
        }
    }

    void CloseAll()
    {
        task1Slider?.Close();
        task2Slider?.Close();
        task3Slider?.Close();
        task4Slider?.Close();
        task5Slider?.Close();
    }
}
