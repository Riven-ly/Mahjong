using UnityEngine;
using UnityEngine.UI;

namespace CashRewards.ComboEffects
{
    /// <summary>Original impact geometry and mj_piaohua animation, independent of game state.</summary>
    public sealed class MatchEffectPlayer : MonoBehaviour
    {
        public enum EffectMode { Combined, LightOnly, PeachBlossomsOnly }
        [SerializeField] EffectMode mode = EffectMode.Combined;
        [SerializeField] RectTransform effectParent;
        [SerializeField] Image leftTile;
        [SerializeField] Image rightTile;
        [SerializeField, Min(1)] float tileWidth = 120;
        [SerializeField] bool includeTileShards = true;

        public void Play() { PlayAt(Vector2.zero); }
        public void PlayAt(Vector2 localPosition)
        {
            if(mode == EffectMode.PeachBlossomsOnly)
                MahjongMatchPetalEffect.Spawn(effectParent,localPosition,tileWidth);
            else
                MahjongMatchShatterEffect.Spawn(effectParent,localPosition,leftTile,rightTile,tileWidth,
                    mode == EffectMode.Combined, includeTileShards && mode == EffectMode.Combined);
        }
        public void PlayWithFaces(Sprite left, Sprite right)
        {
            leftTile.sprite=left;rightTile.sprite=right;Play();
        }
    }
}
