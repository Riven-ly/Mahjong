using UnityEngine;
using UnityEngine.UI;

namespace CashRewards.ComboEffects
{
	public sealed class MahjongImpactGraphic : MaskableGraphic
	{
		public float Width { get; set; }

		public float Age { get; set; }

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			// Native 0x0111E634: three expanding rings and eighteen impact rays.
			vh.Clear();
			if (Age<.24f) Glow(vh,(Age*2.5f+.65f)*Width,(1-Age/.24f)*.95f);
			for (int i=0;i<3;i++)
			{
			    float t=(Age-i*.055f)/.36f;
			    if (t<0 || t>=1) continue;
			    float fade=1-t, alpha=fade*fade;
			    float radius=Mathf.Lerp(.2f,1+i*.12f,1-alpha)*Width;
			    Ring(vh,radius-Width*.04f,Width*.12f,new Color(1,.65f,.13f,alpha*.22f));
			    Ring(vh,radius,fade*Width*.025f,new Color(1,.93f,.64f,alpha));
			}
			if (Age>=.26f) return;
			float life=1-Age/.26f, advance=Age/.26f*.2f;
			for (int i=0;i<18;i++)
			{
			    float angle=i*Mathf.PI*2/18+.17f;
			    Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
			    Vector2 center=direction*((advance+.4f)*Width);
			    Vector2 side=new Vector2(-direction.y,direction.x)*(life*Width*.035f);
			    int n=vh.currentVertCount;
			    var color=new Color(1,.9f,.5f,life*.8f);
			    vh.AddVert(center+side,color,Vector2.zero);
			    vh.AddVert(direction*((advance+(i%3)*.15f+.8f)*Width),new Color(1,.75f,.2f,0),Vector2.zero);
			    vh.AddVert(center-side,color,Vector2.zero);
			    vh.AddTriangle(n,n+1,n+2);
			}
		}

		private static void Ring(VertexHelper vh, float radius, float thickness, Color color)
		{
			// Native MahjongImpactGraphic.Ring, 32 segments.
			for (int i=0;i<32;i++)
			{
			    float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
			    Vector2 from=new Vector2(Mathf.Cos(a),Mathf.Sin(a)), to=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
			    int n=vh.currentVertCount;
			    vh.AddVert(from*radius,color,Vector2.zero); vh.AddVert(from*(radius+thickness),color,Vector2.zero);
			    vh.AddVert(to*(radius+thickness),color,Vector2.zero); vh.AddVert(to*radius,color,Vector2.zero);
			    vh.AddTriangle(n,n+1,n+2); vh.AddTriangle(n,n+2,n+3);
			}
		}

		private static void Glow(VertexHelper vh, float radius, float alpha)
		{
			// Native MahjongImpactGraphic.Glow, 24-segment radial gradient.
			int n=vh.currentVertCount;
			vh.AddVert(Vector2.zero,new Color(1,1,.8f,alpha),Vector2.zero);
			for (int i=0;i<=24;i++)
			{
			    float a=i*Mathf.PI/12;
			    vh.AddVert(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Color(1,.7f,.1f,0),Vector2.zero);
			    if (i>0) vh.AddTriangle(n,n+i,n+i+1);
			}
		}
	}
}
