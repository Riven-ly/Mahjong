using UnityEngine;
using UnityEngine.UI;

public sealed class PhaseTaskItem : MonoBehaviour
{
    public Image icon;
    public Text titleLabel;
    public Text progressLabel;
    public Image progressFill;
    public GameObject lockedState;
    public GameObject completedState;

    public void Bind(PhaseTaskPresentation presentation)
    {
    }
}
