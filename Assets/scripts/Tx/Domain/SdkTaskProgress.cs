public readonly struct SdkTaskProgress
{
    public PhaseId PhaseId { get; }
    public float Current { get; }
    public float Target { get; }
    public float Normalized { get; }
    public bool IsValid { get; }

    public SdkTaskProgress(PhaseId phaseId, float current, float target, float normalized, bool isValid)
    {
        PhaseId = phaseId;
        Current = current;
        Target = target;
        Normalized = normalized;
        IsValid = isValid;
    }
}
