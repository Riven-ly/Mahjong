using UnityEngine;

/// <summary>
/// 计算两个物体的中心点，并将目标物体放在中间
/// </summary>
public class TwoPointMiddle : MonoBehaviour
{
    [Header("第一个物体")]
    public Transform objA;

    [Header("第二个物体")]
    public Transform objB;

    [Header("是否每帧更新")]
    public bool updateEveryFrame = false;

    private void OnEnable()
    {
        SetMiddleY();
    }

    void Update()
    {
        if (updateEveryFrame)
        {
            SetMiddleY();
        }
    }

    /// <summary>
    /// 计算并设置中间位置
    /// </summary>
    public void SetMiddleY()
    {
        if (!objA || !objB) return;

        // 只计算 Y 轴中间
        float middleY = (objA.position.y + objB.position.y) / 2;

        // 赋值：X Z 不变，只改 Y
        Vector3 pos = transform.position;
        pos.y = middleY;
        transform.position = pos;
    }
}