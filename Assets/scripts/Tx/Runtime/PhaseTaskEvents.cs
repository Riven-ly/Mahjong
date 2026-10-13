using System;
using UnityEngine;

public static class PhaseTaskEvents
{
    public static event Action ProgressChanged;
    public static event Action<PhaseId> PhaseCompleted;
    public static event Action<PhaseId> CurrentPhaseChanged;
    public static event Action FinalQualificationReached;

    internal static void RaiseProgressChanged() => ProgressChanged?.Invoke();

    internal static void RaisePhaseCompleted(PhaseId phaseId) =>
        PhaseCompleted?.Invoke(phaseId);

    internal static void RaiseCurrentPhaseChanged(PhaseId phaseId) =>
        CurrentPhaseChanged?.Invoke(phaseId);

    internal static void RaiseFinalQualificationReached() =>
        FinalQualificationReached?.Invoke();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        ProgressChanged = null;
        PhaseCompleted = null;
        CurrentPhaseChanged = null;
        FinalQualificationReached = null;
    }
}
