using UnityEngine;
using UnityEngine.UI;

namespace CashRewards.ComboEffects
{
	public sealed class MahjongMatchShatterEffect : MonoBehaviour
	{
		public const float Lifetime = 1.2f;

		private float age;

		private MahjongShardGraphic first;

		private MahjongShardGraphic second;

		private MahjongImpactGraphic impact;

		private MahjongBurstGraphic burst;

		public static void Spawn(RectTransform parent, Vector2 center, Image left, Image right, float width, bool includePetals = true, bool includeShards = true)
		{
			// Native 0x0111CD84: preserve ordering and original random seed; petals are one layer of the full effect.
			var root=new GameObject("MahjongMatchShatter",typeof(RectTransform));
			var rect=(RectTransform)root.transform;
			rect.SetParent(parent,false);
			rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
			rect.anchoredPosition=center; rect.sizeDelta=new Vector2(width*12,width*18); rect.SetAsLastSibling();
			var effect=root.AddComponent<MahjongMatchShatterEffect>();
			var random=new System.Random(unchecked(left.GetInstanceID() ^ Time.frameCount*397));
			effect.impact=effect.Create<MahjongImpactGraphic>("Impact",rect); effect.impact.Width=width;
			if(includeShards) { effect.first=effect.Create<MahjongShardGraphic>("LeftTileShards",rect); effect.first.Initialize(left,-1,width,random);
			effect.second=effect.Create<MahjongShardGraphic>("RightTileShards",rect); effect.second.Initialize(right,1,width,random); }
			effect.burst=effect.Create<MahjongBurstGraphic>("CeramicDustAndSparks",rect); effect.burst.Initialize(width,random);
			if(includePetals) MahjongMatchPetalEffect.Spawn(parent,center,width);
		}

		private T Create<T>(string name, RectTransform parent) where T : MaskableGraphic
		{
			// Native MahjongMatchShatterEffect.Create<T>: runtime mesh layers, no prefab layout edits.
			var root=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));
			var rect=(RectTransform)root.transform; rect.SetParent(parent,false);
			rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.sizeDelta=Vector2.zero;
			var graphic=root.AddComponent<T>(); graphic.raycastTarget=false;
			return graphic;
		}

		private void Update()
		{
			// Native 0x0111D990, 1.2-second unscaled lifetime.
			age+=Time.unscaledDeltaTime;
			if (age>=Lifetime) { Destroy(gameObject); return; }
			if(first)first.Age=age; if(second)second.Age=age; impact.Age=burst.Age=age;
			if(first)first.SetVerticesDirty(); if(second)second.SetVerticesDirty(); impact.SetVerticesDirty(); burst.SetVerticesDirty();
		}

		internal static float HorizontalTravel(float age)
		{
			// Native 0x0111DABC.
			return (1-Mathf.Exp(-6*age))/6;
		}
	}
}
