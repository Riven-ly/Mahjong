public readonly struct PhaseTaskProgress
{
    public PhaseId PhaseId { get; }
    public float Current { get; }
    public float Target { get; }
    public float Normalized { get; }
    public float SecondaryCurrent { get; }
    public float SecondaryTarget { get; }
    public bool IsComplete { get; }

    public PhaseTaskProgress(
        PhaseId phaseId,
        float current,
        float target,
        float normalized,
        float secondaryCurrent,
        float secondaryTarget,
        bool isComplete)
    {
        PhaseId = phaseId;
        Current = current;
        Target = target;
        Normalized = normalized;
        SecondaryCurrent = secondaryCurrent;
        SecondaryTarget = secondaryTarget;
        IsComplete = isComplete;
    }
}
