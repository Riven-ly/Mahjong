using UnityEngine;
using UnityEngine.UI;

public sealed class AcSelectContainer : MonoBehaviour
{
    public Image currentImage;
    public Toggle currentToggle;
    public GameObject selectedIco1;
    public GameObject selectedIco2;
    public GameObject noSelectIco;
    public AcType currentAcType;

    public AcType Type => currentAcType;

    void Awake()
    {
        currentToggle.onValueChanged.AddListener(OnToggleValueChanged);
        RefreshVisual(currentToggle.isOn);
    }

    void Start()
    {
        if (currentToggle.isOn)
            SdkCandyEvents.RaiseSelectedAcChanged(currentImage.sprite, currentAcType);
    }

    void OnDestroy() => currentToggle.onValueChanged.RemoveListener(OnToggleValueChanged);

    void OnToggleValueChanged(bool selected)
    {
        RefreshVisual(selected);
        if (selected)
            SdkCandyEvents.RaiseSelectedAcChanged(currentImage.sprite, currentAcType);
    }

    void RefreshVisual(bool selected)
    {
        selectedIco1.SetActive(selected);
        selectedIco2.SetActive(selected);
        noSelectIco.SetActive(!selected);
    }

    public void Select()
    {
        currentToggle.isOn = true;
        RefreshVisual(true);
    }

    public void SyncVisual() => RefreshVisual(currentToggle.isOn);
}
