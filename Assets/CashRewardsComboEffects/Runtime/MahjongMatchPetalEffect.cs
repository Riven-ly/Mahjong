using Spine.Unity;
using UnityEngine;

namespace CashRewards.ComboEffects
{
	public sealed class MahjongMatchPetalEffect : MonoBehaviour
	{
		private const string ResourcePath = "MahjongCore/Art/Effects/MatchPetals/";

		private const string AnimationName = "animation";

		private const float AuthoredWidth = 512f;

		private static SkeletonDataAsset skeletonAsset;

		private static Material uiMaterial;

		private SkeletonGraphic graphic;

		private float elapsed;

		private float duration;

		public static void Preload()
		{
			// Native 0x0111C514.
			if (!skeletonAsset) skeletonAsset = Resources.Load<SkeletonDataAsset>(ResourcePath + "mj_piaohua_SkeletonData");
			if (!uiMaterial) uiMaterial = Resources.Load<Material>(ResourcePath + "mj_piaohua_UI");
			if (skeletonAsset) skeletonAsset.GetSkeletonData(false);
		}

		public static void Spawn(RectTransform parent, Vector2 center, float tileWidth)
		{
			// Native 0x0111C684, original mj_piaohua Spine animation and scale.
			Preload();
			var animation = skeletonAsset ? skeletonAsset.GetSkeletonData(false)?.FindAnimation(AnimationName) : null;
			if (animation == null || !uiMaterial) { Debug.LogError("Mahjong impact petal animation or UI material is missing."); return; }
			var root = new GameObject("MahjongMatchPetals", typeof(RectTransform));
			root.layer = parent.gameObject.layer;
			var rect = (RectTransform)root.transform;
			rect.SetParent(parent,false);
			rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
			rect.anchoredPosition = center;
			rect.sizeDelta = new Vector2(tileWidth*3,tileWidth*6);
			var effect = root.AddComponent<MahjongMatchPetalEffect>();
			effect.duration = animation.Duration;
			var g = effect.graphic = SkeletonGraphic.NewSkeletonGraphicGameObject(skeletonAsset,rect,uiMaterial);
			g.gameObject.name = "Petals";
			g.gameObject.layer = root.layer;
			g.raycastTarget = false;
			g.UpdateTiming = UpdateTiming.ManualUpdate;
			g.MeshGenerator.settings.canvasGroupCompatible = true;
			g.MeshGenerator.settings.pmaVertexColors = false;
			g.rectTransform.anchorMin = g.rectTransform.anchorMax = g.rectTransform.pivot = Vector2.one * .5f;
			g.rectTransform.anchoredPosition = Vector2.zero;
			float ppu = g.canvas ? g.canvas.referencePixelsPerUnit : 100;
			g.rectTransform.sizeDelta = new Vector2(512,768) * ppu;
			g.rectTransform.localScale = Vector3.one * (tileWidth * 2.8f / AuthoredWidth / ppu);
			g.AnimationState.SetAnimation(0,AnimationName,false);
			g.Update(0);
		}

		private void Update()
		{
			// Native 0x0111CCBC.
			float dt = Mathf.Min(Time.unscaledDeltaTime,duration-elapsed);
			elapsed += dt;
			graphic.Update(dt);
			if (elapsed >= duration) Destroy(gameObject);
		}
	}
}
