using UnityEngine;

namespace MahjongGame.View
{
    /// <summary>
    /// 高亮期间让卡牌短暂左右摇摆，再停顿一拍。
    /// </summary>
    public sealed class MahjongHintEffect : MonoBehaviour
    {
        private const float CycleDuration = 1.3f;
        private const float ShakeDuration = 0.68f;
        private const float ShakeAmplitude = 6.8f;

        [SerializeField] private MahjongCell mahjongCell;
        private Quaternion restRotation;
        private float startedAt;
        private float previousAngle;

        private void OnEnable()
        {
            restRotation = mahjongCell.transform.localRotation;
            startedAt = Time.time;
            previousAngle = 0f;
        }

        private void LateUpdate()
        {
            float elapsed = (Time.time - startedAt) % CycleDuration;
            float progress = Mathf.Clamp01(elapsed / ShakeDuration);
            float angle = ShakeAmplitude * (1f - progress * progress) * Mathf.Sin(progress * 4f * Mathf.PI);
            if (angle == previousAngle)
            {
                return;
            }

            mahjongCell.transform.localRotation = restRotation * Quaternion.AngleAxis(angle, Vector3.forward);
            previousAngle = angle;
        }

        private void OnDisable()
        {
            if (mahjongCell != null)
            {
                mahjongCell.transform.localRotation = restRotation;
            }
        }
    }
}
