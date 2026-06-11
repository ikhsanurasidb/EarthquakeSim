Shader "GazeVR/ReticleOverlay"
{
    // A small unlit gaze reticle: a filled center dot plus an outer ring. It draws on top of
    // everything (ZTest Always) so it is always visible at the gazed surface, and is URP-compatible.
    Properties
    {
        _Color ("Color", Color) = (0.2, 0.9, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
            "RenderType" = "Overlay"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Reticle"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 d = IN.uv - 0.5;
                float r = length(d) * 2.0;                                 // 0 at center, 1 at edge
                float core = smoothstep(0.45, 0.33, r);                    // filled center dot
                float ring = smoothstep(0.98, 0.86, r) - smoothstep(0.74, 0.62, r); // thin outer ring
                float a = saturate(core + ring);
                return half4(_Color.rgb, _Color.a * a);
            }
            ENDHLSL
        }
    }
}
