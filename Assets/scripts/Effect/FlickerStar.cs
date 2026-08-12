using UnityEngine;

/// <summary>
/// 独立控制单颗星星透明度与缩放呼吸闪烁的组件。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class FlickerStar : MonoBehaviour
{
    [SerializeField] private float flickerSpeed = 1.2f; // 每秒闪烁次数
    [SerializeField] private float minAlpha = 0.28f; // 闪烁最低透明度
    [SerializeField] private float maxAlpha = 1f; // 闪烁最高透明度
    [SerializeField] private float minScale = 0.82f; // 闪烁最小缩放倍率
    [SerializeField] private float maxScale = 1.16f; // 闪烁最大缩放倍率
    [SerializeField] private float phaseOffset; // 闪烁相位偏移

    private CanvasGroup canvasGroup; // 星星透明度控制组件
    private Vector3 initialScale; // 星星初始缩放

    /// <summary>
    /// 缓存星星透明度组件与初始缩放。
    /// </summary>
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        initialScale = transform.localScale;
    }

    /// <summary>
    /// 持续更新星星的透明度和缩放闪烁表现。
    /// </summary>
    private void Update()
    {
        float flickerValue = (Mathf.Sin((Time.unscaledTime + phaseOffset) * flickerSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, flickerValue);
        transform.localScale = initialScale * Mathf.Lerp(minScale, maxScale, flickerValue);
    }
}
