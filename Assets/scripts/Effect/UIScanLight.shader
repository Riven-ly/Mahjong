Shader "Custom/UIScanLight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Speed", Range(-2, 2)) = 1.04
        _Width ("Width", Range(1, 10)) = 5.83
        _Angle ("Angle", Range(-1, 1)) = 0.33
        _Light ("Light", Range(0, 1)) = 0.51
        _Delay ("Delay", Range(0, 5)) = 0.0
        _LightColor ("Light Color", Color) = (1, 1, 1, 1) // 新增：流光颜色属性
    }
    SubShader
    {
        Tags {"Queue" = "Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Speed;
            float _Width;
            float _Angle;
            float _Light;
            float _Delay;
            float4 _LightColor; // 声明流光颜色变量

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float time = _Time.w - _Delay;
                float x = i.uv.x + i.uv.y * _Angle;
                float v = sin(x - time * _Speed); 
                v = smoothstep(1 - _Width / 1000, 1.0, v);

                // 核心修改：将原白色流光替换为自定义颜色
                float3 lightColor = _LightColor.rgb * v; // 颜色 * 流光强度
                float3 target = lightColor + col.rgb; // 融合到主纹理

                col.rgb = lerp(col.rgb, target, _Light);
                return col;
            }
            ENDCG
        }
    }
}