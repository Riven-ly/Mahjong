using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PhaseOneHudEntry
{
    [SerializeField] int actualLevel;

    [SerializeField] int displayLevel;

    [SerializeField] int dotCount;

    [SerializeField] int currentDot;

    [SerializeField] int completedDots;

    [SerializeField] bool showPercent;

    [Range(0f, 1f)]
    [SerializeField] float percent;

    public int ActualLevel => actualLevel;
    public int DisplayLevel => displayLevel;
    public int DotCount => dotCount;
    public int CurrentDot => currentDot;
    public int CompletedDots => completedDots;
    public bool ShowPercent => showPercent;
    public float Percent => percent;
}

[CreateAssetMenu(menuName = "Game/Phase One HUD Config", fileName = "PhaseOneHudConfig")]
public sealed class PhaseOneHudConfigSO : ScriptableObject
{
    [SerializeField] PhaseOneHudEntry[] entries;

    public PhaseOneHudEntry Get(int actualLevel)
    {
        if (entries == null)
            return null;

        for (int index = 0; index < entries.Length; index++)
        {
            PhaseOneHudEntry entry = entries[index];
            if (entry != null && entry.ActualLevel == actualLevel)
                return entry;
        }

        return null;
    }

    void OnValidate()
    {
        if (entries == null || entries.Length != 30)
        {
            Debug.LogWarning("101", this);
            return;
        }

        var levels = new HashSet<int>();
        int previousDisplayLevel = 0;
        float previousPercent = 0f;
        for (int index = 0; index < entries.Length; index++)
        {
            PhaseOneHudEntry entry = entries[index];
            if (entry == null)
            {
                Debug.LogWarning("101", this);
                continue;
            }

            if (!levels.Add(entry.ActualLevel) || entry.ActualLevel != index + 1)
                Debug.LogWarning("101", this);
            if (entry.DisplayLevel < 1 || entry.DisplayLevel > 8 || entry.DisplayLevel < previousDisplayLevel)
                Debug.LogWarning("101", this);
            if (entry.DotCount < 0 || entry.DotCount > 5 || entry.CurrentDot < 0 || entry.CurrentDot > entry.DotCount || entry.CompletedDots < 0 || entry.CompletedDots > entry.DotCount)
                Debug.LogWarning("101", this);
            if (entry.CurrentDot > 0 && entry.CompletedDots >= entry.CurrentDot)
                Debug.LogWarning("101", this);
            if (entry.ShowPercent && (entry.DisplayLevel != 8 || entry.CurrentDot != 5 || entry.Percent <= 0f || entry.Percent < previousPercent))
                Debug.LogWarning("101", this);

            previousDisplayLevel = entry.DisplayLevel;
            previousPercent = Mathf.Max(previousPercent, entry.Percent);
        }

        PhaseOneHudEntry finalEntry = entries[28];
        if (finalEntry == null || !finalEntry.ShowPercent || !Mathf.Approximately(finalEntry.Percent, 0.9999f))
            Debug.LogWarning("101", this);
    }
}
