public readonly struct ActiveMinuteProgress
{
    public int SlotIndex { get; }
    public int SlotCount { get; }
    public float ActiveSeconds { get; }
    public float RequiredSeconds { get; }
    public int SuccessfulEscapes { get; }
    public int RequiredEscapes { get; }
    public float TimeNormalized { get; }
    public float EscapeNormalized { get; }
    public float TotalNormalized { get; }
    public bool IsTimeComplete { get; }
    public bool IsEscapeComplete { get; }
    public bool IsComplete { get; }
    public bool IsValid { get; }

    public ActiveMinuteProgress(
        int slotIndex,
        int slotCount,
        float activeSeconds,
        float requiredSeconds,
        int successfulEscapes,
        int requiredEscapes,
        float timeNormalized,
        float escapeNormalized,
        float totalNormalized,
        bool isTimeComplete,
        bool isEscapeComplete,
        bool isComplete,
        bool isValid)
    {
        SlotIndex = slotIndex;
        SlotCount = slotCount;
        ActiveSeconds = activeSeconds;
        RequiredSeconds = requiredSeconds;
        SuccessfulEscapes = successfulEscapes;
        RequiredEscapes = requiredEscapes;
        TimeNormalized = timeNormalized;
        EscapeNormalized = escapeNormalized;
        TotalNormalized = totalNormalized;
        IsTimeComplete = isTimeComplete;
        IsEscapeComplete = isEscapeComplete;
        IsComplete = isComplete;
        IsValid = isValid;
    }
}
