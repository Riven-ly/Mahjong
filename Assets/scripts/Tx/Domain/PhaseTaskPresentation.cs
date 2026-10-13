public readonly struct PhaseTaskPresentation
{
    public PhaseId PhaseId { get; }
    public PhaseTaskState State { get; }
    public string Title { get; }
    public string Description { get; }
    public string ProgressText { get; }
    public string ActionText { get; }
    public PhaseTaskProgress Progress { get; }

    public PhaseTaskPresentation(
        PhaseId phaseId,
        PhaseTaskState state,
        string title,
        string description,
        string progressText,
        string actionText,
        PhaseTaskProgress progress)
    {
        PhaseId = phaseId;
        State = state;
        Title = title;
        Description = description;
        ProgressText = progressText;
        ActionText = actionText;
        Progress = progress;
    }
}
