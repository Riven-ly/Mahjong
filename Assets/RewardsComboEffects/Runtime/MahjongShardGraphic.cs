using System;
using UnityEngine;
using UnityEngine.UI;

namespace CashRewards.ComboEffects
{
	public sealed class MahjongShardGraphic : MaskableGraphic
	{
		private struct Shard
		{
			public Vector2 A;

			public Vector2 B;

			public Vector2 C;

			public Vector2 UvA;

			public Vector2 UvB;

			public Vector2 UvC;

			public Vector2 Origin;

			public Vector2 Velocity;

			public float Spin;

			public float Phase;

			public float Shade;
		}

		private const int Columns = 8;

		private const int Rows = 8;

		private readonly Shard[] shards = new Shard[128];

		private Texture texture;

		private float width;

		private bool initialized;

		public float Age { get; set; }

		public override Texture mainTexture => texture ? texture : Texture2D.whiteTexture;

		public void Initialize(Image image, float side, float tileWidth, System.Random random)
		{
			// Native 0x0111D1CC and UV closure 0x0111E4E8: jittered 8x8 grid, packed/rotated sprite UV mapping.
			var sprite = image.overrideSprite;
			if (!sprite) return;
			texture = sprite.texture;
			width = tileWidth;
			Vector2 size = image.rectTransform.rect.size * 1.02f;
			if (image.preserveAspect)
			{
			    float aspect = sprite.rect.width / sprite.rect.height;
			    if (size.x / size.y <= aspect) size.y = size.x / aspect;
			    else size.x = size.y * aspect;
			}
			var vertices = sprite.vertices; var uv = sprite.uv; var triangles = sprite.triangles;
			int triangle = 0; float determinant = 0;
			Vector2 edge1 = default, edge2 = default;
			for (; triangle + 2 < triangles.Length; triangle += 3)
			{
			    edge1 = vertices[triangles[triangle+1]] - vertices[triangles[triangle]];
			    edge2 = vertices[triangles[triangle+2]] - vertices[triangles[triangle]];
			    determinant = edge1.x*edge2.y-edge1.y*edge2.x;
			    if (Mathf.Abs(determinant) > .000001f) break;
			}
			if (Mathf.Abs(determinant) <= .000001f) return;
			Vector2 baseVertex = vertices[triangles[triangle]], baseUv = uv[triangles[triangle]];
			Vector2 uv1 = uv[triangles[triangle+1]]-baseUv, uv2 = uv[triangles[triangle+2]]-baseUv;
			Func<Vector2,Vector2> mapUv = point =>
			{
			    Vector2 local = (Vector2.Scale(point+Vector2.one*.5f,sprite.rect.size)-sprite.pivot)/sprite.pixelsPerUnit-baseVertex;
			    float a = (local.x*edge2.y-local.y*edge2.x)/determinant;
			    float b = (local.y*edge1.x-local.x*edge1.y)/determinant;
			    return baseUv+uv1*a+uv2*b;
			};
			var grid = new Vector2[Columns+1,Rows+1];
			for (int x=0;x<=Columns;x++) for (int y=0;y<=Rows;y++)
			{
			    float dx = x>0 && x<Columns ? Next(random,-.24f,.24f)/Columns : 0;
			    float dy = y>0 && y<Rows ? Next(random,-.24f,.24f)/Rows : 0;
			    grid[x,y] = new Vector2(x/(float)Columns-.5f+dx,y/(float)Rows-.5f+dy);
			}
			int index = 0;
			for (int x=0;x<Columns;x++) for (int y=0;y<Rows;y++)
			{
			    Vector2 a=grid[x,y], b=grid[x+1,y], c=grid[x+1,y+1], d=grid[x,y+1];
			    if (((x+y)&1)==0)
			    {
			        AddShard(ref index,a,b,c,size,side,random,mapUv);
			        AddShard(ref index,a,c,d,size,side,random,mapUv);
			    }
			    else
			    {
			        AddShard(ref index,a,b,d,size,side,random,mapUv);
			        AddShard(ref index,b,c,d,size,side,random,mapUv);
			    }
			}
			initialized = true;
			SetMaterialDirty(); SetVerticesDirty();
		}

		private void AddShard(ref int index, Vector2 a, Vector2 b, Vector2 c, Vector2 size, float side, System.Random random, Func<Vector2, Vector2> mapUv)
		{
			// Native 0x0111DBD4; all ranges decoded from this ELF.
			Vector2 center = (a+b+c)/3;
			var shard = new Shard { A=Vector2.Scale(a-center,size), B=Vector2.Scale(b-center,size), C=Vector2.Scale(c-center,size),
			    UvA=mapUv(a), UvB=mapUv(b), UvC=mapUv(c), Origin=Vector2.Scale(center,size)+Vector2.right*(side*width*.47f) };
			float outward = Next(random,.1f,.35f), horizontal = Next(random,-.25f,.25f), vertical = Next(random,-.5f,1.6f);
			shard.Velocity = new Vector2(horizontal+.9f*center.x+outward*side,.8f*center.y+vertical)*width;
			shard.Spin = Next(random,-940,940)*Mathf.Deg2Rad;
			shard.Phase = Next(random,0,Mathf.PI*2);
			shard.Shade = Next(random,.86f,1);
			shards[index++] = shard;
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			// Native 0x0111DEB4: damped horizontal travel, gravity, spin, tumble, textured front and thickness.
			vh.Clear(); if (!initialized) return;
			float alpha = 1-Mathf.SmoothStep(0,1,(Age-.68f)/.52000004f);
			float shrink = Mathf.Lerp(1,.78f,Age/1.2f);
			foreach (var shard in shards)
			{
			    Vector2 origin = shard.Origin + new Vector2(shard.Velocity.x*MahjongMatchShatterEffect.HorizontalTravel(Age),
			        shard.Velocity.y*Age-Age*Age*width*7);
			    float angle=shard.Spin*Age, cos=Mathf.Cos(angle), sin=Mathf.Sin(angle);
			    float tumble=Mathf.Lerp(1,Mathf.Abs(Mathf.Cos(shard.Phase+Age*8))*.52f+.48f,Age*8);
			    Vector2 a=origin+Turn(shard.A,cos,sin,tumble,shrink), b=origin+Turn(shard.B,cos,sin,tumble,shrink), c=origin+Turn(shard.C,cos,sin,tumble,shrink);
			    Vector2 offset=new Vector2(.008f,-.012f)*width;
			    Triangle(vh,a+offset,b+offset,c+offset,shard,new Color(.48f,.42f,.24f,alpha*.7f));
			    Triangle(vh,a,b,c,shard,new Color(shard.Shade,shard.Shade,shard.Shade,alpha));
			}
		}

		private static Vector2 Turn(Vector2 point, float cos, float sin, float tumble, float shrink)
		{
			// Native 0x0111E318.
			return new Vector2(point.x*tumble*cos-point.y*sin,point.y*cos+point.x*tumble*sin)*shrink;
		}

		private static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Shard shard, Color color)
		{
			// Native 0x0111E340.
			int i=vh.currentVertCount;
			vh.AddVert(a,color,shard.UvA); vh.AddVert(b,color,shard.UvB); vh.AddVert(c,color,shard.UvC);
			vh.AddTriangle(i,i+1,i+2);
		}

		private static float Next(System.Random random, float min, float max)
		{
			// Native 0x0111DB80.
			return min+(max-min)*(float)random.NextDouble();
		}
	}
}
