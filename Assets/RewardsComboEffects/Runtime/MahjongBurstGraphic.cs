using System;
using UnityEngine;
using UnityEngine.UI;

namespace CashRewards.ComboEffects
{
	public sealed class MahjongBurstGraphic : MaskableGraphic
	{
		private struct Particle
		{
			public Vector2 Origin;

			public Vector2 Velocity;

			public float Size;

			public float Spin;

			public float Phase;

			public float Duration;

			public float Delay;
		}

		private readonly Particle[] chips = new Particle[96];

		private readonly Particle[] sparks = new Particle[40];

		private float width;

		private bool initialized;

		public float Age { get; set; }

		public void Initialize(float tileWidth, System.Random random)
		{
			// Native MahjongBurstGraphic.Initialize; ctor counts 96 ceramic chips / 40 sparks.
			width=tileWidth;
			for (int i=0;i<chips.Length;i++) chips[i]=CreateParticle(random,false);
			for (int i=0;i<sparks.Length;i++) sparks[i]=CreateParticle(random,true);
			initialized=true; SetVerticesDirty();
		}

		private Particle CreateParticle(System.Random random, bool spark)
		{
			// Native 0x0111EFB8, preserving random draw order and separate spark/chip ranges.
			float phase=Next(random,0,Mathf.PI*2);
			Vector2 origin=new Vector2(Next(random,-.65f,.65f),Next(random,-.4f,.4f))*width;
			Vector2 velocity=new Vector2(Next(random,spark?-1.6f:-1.3f,spark?1.6f:1.3f),Next(random,spark?.1f:-1.2f,spark?1.6f:1.4f))*width;
			return new Particle { Origin=origin,Velocity=velocity,Size=width*Next(random,spark?.035f:.015f,spark?.085f:.038f),
			    Spin=Next(random,-18,18),Phase=phase,Duration=Next(random,spark?.55f:.7f,spark?1.03f:1.12f),Delay=spark?Next(random,0,.09f):0 };
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			// Native 0x0111F208: ceramic triangle chips, ballistic spark trails and flickering stars.
			vh.Clear(); if (!initialized) return;
			for (int i=0;i<chips.Length;i++)
			{
			    Particle p=chips[i]; float age=Age-p.Delay;
			    if (age<0 || age>=p.Duration) continue;
			    float t=age/p.Duration, alpha=1-Mathf.SmoothStep(0,1,(t-.5f)*2);
			    Vector2 pos=Position(p,age,7);
			    float angle=p.Phase+age*p.Spin,size=p.Size*(1-t*.25f);
			    Vector2 forward=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*size;
			    Vector2 side=new Vector2(-forward.y,forward.x)*.65f;
			    Color color=(i&3)==0 ? new Color(1,.81f,.38f,alpha) : new Color(1,.99f,.89f,alpha);
			    int n=vh.currentVertCount;
			    vh.AddVert(pos+forward,color,Vector2.zero); vh.AddVert(pos-forward*.7f+side,color,Vector2.zero);
			    vh.AddVert(pos-forward*.7f-side,color,Vector2.zero); vh.AddTriangle(n,n+1,n+2);
			}
			foreach (Particle p in sparks)
			{
			    float age=Age-p.Delay; if (age<0 || age>=p.Duration) continue;
			    float t=age/p.Duration,flicker=Mathf.Abs(Mathf.Sin(age*19+p.Phase))*.35f+.65f;
			    float alpha=(1-t)*flicker,size=Mathf.Lerp(1.15f,.2f,t)*p.Size*flicker;
			    Vector2 pos=Position(p,age,3.8f);
			    if (age<.35f)
			    {
			        Vector2 tail=Position(p,Mathf.Max(0,age-.06f),3.8f),delta=pos-tail;
			        Vector2 side=new Vector2(-delta.y,delta.x).normalized*(size*.24f);
			        int n=vh.currentVertCount;
			        var color=new Color(1,.84f,.3f,alpha*.8f);
			        vh.AddVert(pos+side,color,Vector2.zero); vh.AddVert(pos-side,color,Vector2.zero);
			        vh.AddVert(tail,new Color(1,.54f,.06f,0),Vector2.zero); vh.AddTriangle(n,n+1,n+2);
			    }
			    Star(vh,pos,size,new Color(1,.76f,.15f,alpha));
			    Star(vh,pos,size*.52f,new Color(1,1,.95f,alpha));
			}
		}

		private Vector2 Position(Particle p, float age, float gravity)
		{
			// Native MahjongBurstGraphic.Position.
			return p.Origin+new Vector2(p.Velocity.x*MahjongMatchShatterEffect.HorizontalTravel(age),p.Velocity.y*age-age*age*width*gravity);
		}

		private static void Star(VertexHelper vh, Vector2 center, float size, Color color)
		{
			// Native MahjongBurstGraphic.Star.
			int n=vh.currentVertCount; vh.AddVert(center,color,Vector2.zero);
			for (int i=0;i<=8;i++)
			{
			    float a=i*Mathf.PI/4,r=(i&1)==0?1:.2f;
			    vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a)*1.4f)*(r*size),color,Vector2.zero);
			    if (i>0) vh.AddTriangle(n,n+i,n+i+1);
			}
		}

		private static float Next(System.Random random, float min, float max)
		{
			// Native MahjongBurstGraphic.Next.
			return min+(max-min)*(float)random.NextDouble();
		}
	}
}
