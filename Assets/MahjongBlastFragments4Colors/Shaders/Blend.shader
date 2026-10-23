Shader "MahjongBlast/Fragments4/Blend" {
Properties {
 _MainTex("Texture",2D)="white"{} _Diffuse("Diffuse",2D)="white"{} _diffuse("Additive Diffuse",2D)="white"{}
 _Color("Color",Color)=(1,1,1,1) _Intensity("Intensity",Float)=1
 _StencilComp("Stencil Comparison",Float)=8 _Stencil("Stencil ID",Float)=0 _StencilOp("Stencil Operation",Float)=0
 _StencilWriteMask("Stencil Write Mask",Float)=255 _StencilReadMask("Stencil Read Mask",Float)=255 _ColorMask("Color Mask",Float)=15
}
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
 Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
 #include "UnityCG.cginc"
 #include "UnityUI.cginc"
 struct appdata {float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0;};
 struct v2f {float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 local:TEXCOORD1;};
 sampler2D _MainTex,_Diffuse,_diffuse; float4 _MainTex_ST,_Diffuse_ST,_diffuse_ST,_Color,_ClipRect; float _Intensity;
 v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target{fixed4 c=tex2D(_MainTex,TRANSFORM_TEX(i.uv,_MainTex))*i.color;c.rgb*=_Intensity;
 #ifdef UNITY_UI_CLIP_RECT
 c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
 #endif
 #ifdef UNITY_UI_ALPHACLIP
 clip(c.a-0.001);
 #endif
 return c;}
 ENDCG }
} }
