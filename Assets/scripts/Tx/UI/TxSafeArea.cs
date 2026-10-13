using UnityEngine;

public sealed class TxSafeArea : MonoBehaviour
{
    RectTransform topBar;
    Vector2 initialPosition;

    void Awake()
    {
        topBar = transform as RectTransform;
        initialPosition = topBar.anchoredPosition;
        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        RectTransform canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
        float topInsetPixels = Screen.height - Screen.safeArea.yMax;
        float topInset = topInsetPixels * canvasRect.rect.height / Screen.height;
        topBar.anchoredPosition = initialPosition + Vector2.down * topInset;
    }
}
