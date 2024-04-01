Shader "Character/Avatar/Avatar_DualFace"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" { }
        _LightMapTex ("Light Map", 2D) = "white" { }
        _BloomFactor ("Bloom Factor", Float) = 0.5
        _BaseColor ("Base Color", Color) = (0.93, 0.93, 0.93, 0.95)
        _EffectColor ("Effect Color", Color) = (0, 0, 0, 0)
        _EnableBlack ("Enable Black", Float) = 0
        _EnvColror ("Env Color", Color) = (1, 1, 1, 1)
        _FirstShadowMultColor ("First Shadow Color", Vector) = (0.95, 0.77, 0.82, 0)
        _LightArea ("Light Area", Float) = 0.51
        _RenderScale ("Render Scale", Float) = 1
        _ShadowContrast ("Shadow Contrast ", Float) = 0
        _SecodShadow ("Second Shadow", Float) = 0.51
        _SecondShadowMultColor ("Second Shadow Color", Vector) = (0.58, 0.55, 0.79, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType" = "Opaque" }
        
        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _BaseColor;
            half _BloomFactor;
            half4 _ShadowColor;
        CBUFFER_END

        //TEXTURE2D 声明一个纹理贴图   SAMPLER 声明一个采样器对象
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_LightMapTex); SAMPLER(sampler_LightMapTex);

        struct Attributes
        {
            float2 uv : TEXCOORD0;
            float4 positionOS : POSITION;
        };

        struct Varings
        {
            float2 uv : TEXCOORD0;
            float4 positionCS : SV_POSITION;
        };

        ENDHLSL

        Pass
        {

            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha  OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM

            #pragma vertex VERT
            #pragma fragment FRAG

            Varings VERT(Attributes v)
            {
                Varings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 FRAG(Varings i) : SV_TARGET
            {

                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half alpha = 1.0;
                return mainColor;
            }
            ENDHLSL
        }
    }
}



