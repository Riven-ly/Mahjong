using UnityEngine;

public enum PhaseLevelNodeState
{
    NoComplete,
    Complete,
    Current
}

public sealed class PhaseLevelNodeView : MonoBehaviour
{
    public GameObject noComplete;
    public GameObject complete;
    public GameObject current;

    public void SetState(PhaseLevelNodeState state)
    {
        noComplete.SetActive(state == PhaseLevelNodeState.NoComplete);
        complete.SetActive(state == PhaseLevelNodeState.Complete);
        current.SetActive(state == PhaseLevelNodeState.Current);
    }
}
