Shader "Character/Face/Face_Default"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" { }
        _LightMapTex ("Light Map", 2D) = "white" { }
        _FaceMapTex ("Face Map", 2D) = "white" { }
        _BaseColor ("Base Color", Color) = (0.93, 0.93, 0.93, 0.95)
        _ShadowColor ("Shadow Color", Color) = (0.95, 0.7, 0.8, 1)
        _BloomFactor ("Bloom Factor", Float) = 0.5
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
        TEXTURE2D(_FaceMapTex); SAMPLER(sampler_FaceMapTex);

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

                half lightColor = SAMPLE_TEXTURE2D(_LightMapTex, sampler_LightMapTex, i.uv).y;
                lightColor = min(lightColor, 1.0);

                half3 inverShadowColr = (-_ShadowColor.xyz) + half3(1.0, 1.0, 1.0);
                half3 shadowColor = half3(lightColor, lightColor, lightColor) * inverShadowColr.xyz + _ShadowColor.xyz;//根据贴图得到的第一层固定的阴影颜色，当 ligthColor 为1，color为0，当 ligthColor 为1，color为_ShadowColor


                float leftTex = SAMPLE_TEXTURE2D(_FaceMapTex, sampler_FaceMapTex, i.uv).a;
                float rightTex = SAMPLE_TEXTURE2D(_FaceMapTex, sampler_FaceMapTex, float2(1 - i.uv.x, i.uv.y)).a;
                
                Light light = GetMainLight();
                float3 lightDir = normalize(light.direction); //光线入射向量
                float3 RightSide = TransformObjectToWorld(float3(1.0, 0.0, 0.0)); //世界空间下右向量
                float isRight = dot(lightDir.x, RightSide.x); //光线是否在右边
                float shadow = isRight > 0 ? rightTex : leftTex; //采样的阈值

                float3 forward = TransformObjectToWorld(float3(0.0, 0.0, 1.0));
                float reflectVal = 0.5 * dot(forward, lightDir) + 0.5; //计算得到的阈值
                float is_shadow = shadow < reflectVal ? 1.0 : 0;

                half3 color = lerp(mainColor.rgb,_ShadowColor.rgb, is_shadow) * shadowColor.rgb *_BaseColor.rgb;   //在阴影里用配置颜色叠加，不在阴影里就贴图本身颜色        
                half alpha = _BloomFactor;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}



