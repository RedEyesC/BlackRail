Shader "Universal Render Pipeline/Checkerboard"
{
    Properties
    {
        _GridSize("Grid Size", Float) = 10
        _Color1("Color 1", Color) = (1,1,1,1)
        _Color2("Color 2", Color) = (0,0,0,1)
        _HighlightColor("Highlight Color", Color) = (1,0,0,1)
        _HighlightCoord("Highlight Coordinate", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionHCS  : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _GridSize;
                float4 _Color1;
                float4 _Color2;
                float4 _HighlightColor;
                float2 _HighlightCoord;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv * _GridSize;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 gridPos = floor(IN.uv);
                float pattern = fmod(gridPos.x + gridPos.y, 2.0);

                if (gridPos.x == _HighlightCoord.x && gridPos.y == _HighlightCoord.y)
                {
                    return _HighlightColor;
                }

                return pattern < 1.0 ? _Color1 : _Color2;
            }
            ENDHLSL
        }
    }
}