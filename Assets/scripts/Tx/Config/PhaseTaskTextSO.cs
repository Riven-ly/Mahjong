using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Tx/Phase Task Text", fileName = "PhaseTaskText")]
public sealed class PhaseTaskTextSO : ScriptableObject
{
    public PhaseTaskTextEntry[] entries;
    public string finalQualificationText = "You are qualified for the final reward";

    public string FinalQualificationText => finalQualificationText;

    public PhaseTaskTextEntry Get(string textId)
    {
        return null;
    }
}

[Serializable]
public sealed class PhaseTaskTextEntry
{
    public string textId;
    public string title;
    public string inProgress;
    public string completed;
    public string progress;
    public string action;

    public string TextId => textId;
    public string Title => title;
    public string InProgress => inProgress;
    public string Completed => completed;
    public string Progress => progress;
    public string Action => action;
}
