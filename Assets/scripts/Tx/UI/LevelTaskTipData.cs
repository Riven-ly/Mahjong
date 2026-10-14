using System;

public sealed class LevelTaskTipData
{
    readonly bool completedNow;
    bool completed;

    public int NextLevel { get; }
    public string TaskText { get; }
    public Action Completed { get; }
    public bool CanOpen => PhaseTaskService.CanShowLevelTaskTip(completedNow);

    public LevelTaskTipData(int nextLevel, string taskText, Action completed, bool completedNow)
    {
        NextLevel = nextLevel;
        TaskText = taskText;
        Completed = completed;
        this.completedNow = completedNow;
    }

    public void Skip() => Complete();

    public void Complete()
    {
        if (completed)
            return;

        completed = true;
        Completed?.Invoke();
    }
}
