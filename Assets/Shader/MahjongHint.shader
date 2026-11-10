Shader "Mahjong/UI/Hint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="False"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.localPosition = input.position.xy;
                output.color = input.color;
                return output;
            }

            static const float2 SpriteAspect = float2(0.8, 1.0);
            static const float CornerRadius = 0.11;
            static const float2 StraightExtent = float2(0.29, 0.39);

            float PerimeterPhase(float2 position, float2 corner)
            {
                // Measure the top edge, round corner and side in travel order.
                const float arcLength = CornerRadius * UNITY_PI * 0.5;
                const float quarterLength = StraightExtent.x + arcLength + StraightExtent.y;
                float fromTop;
                if (corner.x <= 0.0)
                    fromTop = abs(position.x);
                else if (corner.y <= 0.0)
                    fromTop = StraightExtent.x + arcLength - corner.y;
                else
                    fromTop = StraightExtent.x + CornerRadius * atan2(corner.x, corner.y);

                float quarter = fromTop / quarterLength;
                quarter = position.y < 0.0 ? 2.0 - quarter : quarter;
                quarter = position.x < 0.0 ? 4.0 - quarter : quarter;
                return quarter * 0.25;
            }

            float Spark(float2 position, float2 center, float phase, float radius)
            {
                float flicker = saturate(1.0 - 5.0 * abs(frac(phase) - 0.5));
                return flicker * (1.0 - smoothstep(0.0, radius, length(position - center)));
            }

            float SpriteCoverage(float2 uv)
            {
                // Treat samples beyond the sprite as transparent even where the texture is clamped.
                float2 pixelsToBounds = min(uv, 1.0 - uv) * _MainTex_TexelSize.zw;
                float bounds = saturate(min(pixelsToBounds.x, pixelsToBounds.y) + 0.5);
                return (tex2D(_MainTex, uv).a + _TextureSampleAdd.a) * bounds;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                // Use the Background sprite itself for both its paint and its outer silhouette.
                fixed4 tile = (tex2D(_MainTex, input.uv) + _TextureSampleAdd) * input.color;
                float2 position = (input.uv - 0.5) * SpriteAspect;
                float2 corner = abs(position) - StraightExtent;
                float2 outward = normalize(max(corner, 0.0) + 0.00001) * sign(position) / SpriteAspect;
                float rim = 1.0 - SpriteCoverage(input.uv + outward * 0.011);
                float feather = 1.0 - SpriteCoverage(input.uv + outward * 0.022);
                float halo = 1.0 - SpriteCoverage(input.uv + outward * 0.043);

                float time = _Time.y;
                float phase = frac(2.0 * PerimeterPhase(position, corner) - time * 0.73);
                float sweep = 1.0 - smoothstep(0.04, 0.28, min(phase, 1.0 - phase));
                float sparkle = Spark(position, float2(-0.16, 0.21), time * 1.07, 0.029)
                              + Spark(position, float2(0.12, -0.13), time * 0.91 + 0.42, 0.022);

                float3 edgeColor = lerp(float3(0.025, 0.73, 0.055), float3(0.005, 0.99, 0.84), sweep);
                float edge = saturate((0.30 + 0.50 * sweep) * rim + (0.08 + 0.15 * sweep) * feather
                                    + 0.13 * sweep * halo);
                tile.rgb = lerp(tile.rgb, edgeColor, edge);
                tile.rgb = lerp(tile.rgb, float3(0.88, 1.0, 0.93), 0.45 * sparkle);
                #ifdef UNITY_UI_CLIP_RECT
                tile.a *= UnityGet2DClipping(input.localPosition, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(tile.a - 0.001);
                #endif
                return tile;
            }
            ENDCG
        }
    }
}
