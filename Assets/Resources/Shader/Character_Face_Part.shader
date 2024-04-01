Shader "Character/Face/Face_Part"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" { }
        _Color ("Color", Color) = (0.93, 0.93, 0.93, 0.93)
        _EnvColor ("Env Color", Color) = (1, 1, 1, 1)
        _HueOffset ("Hue Offset", Float) = 0.405
        _SaturateOffset ("Saturate Offset", Float) = 0
        _ReverseHue ("Reverse Hue", Float) = 0
        _ValueOffset ("Value Offset", Float) = 0.019
        _EmissionFactor ("Emission Factor", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType" = "Opaque" }
        
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
        CBUFFER_END

        //TEXTURE2D 声明一个纹理贴图   SAMPLER 声明一个采样器对象
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
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
                return mainColor;
            }
            ENDHLSL
        }
    }
}



