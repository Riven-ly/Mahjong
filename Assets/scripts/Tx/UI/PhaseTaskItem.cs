using UnityEngine;
using TMPro;
using UnityEngine.UI;

public sealed class PhaseTaskItem : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI titleLabel;
    public TextMeshProUGUI progressLabel;
    public Image progressFill;
    public GameObject lockedState;
    public GameObject completedState;

    public void Bind(PhaseTaskPresentation presentation)
    {
    }
}
