Shader "Character/Default/Default"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType" = "Opaque" }
        
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



