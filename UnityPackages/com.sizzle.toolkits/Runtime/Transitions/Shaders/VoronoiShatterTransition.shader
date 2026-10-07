Shader "Sizzle/Transitions/VoronoiShatterTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0.02, 0.02, 0.03, 1.0)
        _CrackColor ("Crack Highlight Color", Color) = (0.7, 0.85, 1.0, 1.0)
        _ShardScale ("Shard Density", Float) = 8.0
        _CrackWidth ("Crack Width", Range(0.01, 0.15)) = 0.05
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 1.777777
        _Progress ("Progress", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            fixed4 _Color;
            fixed4 _CrackColor;
            float _ShardScale;
            float _CrackWidth;
            float _AspectRatio;
            float _Progress;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_Progress <= 0.0001)
                {
                    return fixed4(0, 0, 0, 0);
                }
                if (_Progress >= 0.9999)
                {
                    return _Color * i.color;
                }

                // 화면 픽셀 종횡비 계산 (_ScreenParams 우선, 폴백 _AspectRatio)
                float aspect = (_ScreenParams.y > 0.0) ? (_ScreenParams.x / _ScreenParams.y) : _AspectRatio;
                aspect = max(0.001, aspect);

                // 종횡비 보정된 중심 좌표계 및 보로노이 격자
                float2 centeredUV = float2((i.uv.x - 0.5) * aspect, i.uv.y - 0.5);
                float2 p = centeredUV * _ShardScale;
                float2 i_st = floor(p);
                float2 f_st = frac(p);

                float d1 = 8.0;
                float d2 = 8.0;
                float2 closestSeed = float2(0, 0);

                // 3x3 Voronoi F1 / F2 셀룰러 연산
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(float(x), float(y));
                        float2 cellSeed = i_st + neighbor;

                        // 셀 고유 시드 지점 해시
                        float2 h = frac(sin(float2(dot(cellSeed, float2(127.1, 311.7)), dot(cellSeed, float2(269.5, 183.3)))) * 43758.5453);
                        float2 pt = 0.5 + 0.38 * sin(h * 6.2831853);
                        float dist = length(neighbor + pt - f_st);

                        if (dist < d1)
                        {
                            d2 = d1;
                            d1 = dist;
                            closestSeed = cellSeed;
                        }
                        else if (dist < d2)
                        {
                            d2 = dist;
                        }
                    }
                }

                // 크랙(파편 경계면) 판정: F2 - F1
                float crackDist = d2 - d1;
                float crackIntensity = 1.0 - smoothstep(0.0, _CrackWidth, crackDist);

                // 파편별 고유 난수 및 전개 임계값
                float shardHash = frac(sin(dot(closestSeed, float2(12.9898, 78.233))) * 43758.5453);
                float centerDist = length(centeredUV) * 0.7;
                float shardThreshold = saturate(centerDist * 0.7 + shardHash * 0.3);

                // 1단계 크랙 발광선 (초반에 화면 전체로 번짐)
                float crackTimeline = saturate(_Progress * 2.5);
                float crackGlow = crackIntensity * crackTimeline;

                // 2단계 파편 암전 채움 (중심 및 랜덤 순환으로 덮임)
                float shardFill = saturate((_Progress * 1.5 - shardThreshold) * 4.0);

                // 최종 색상 합성
                fixed4 shardCol = _Color * i.color;
                fixed4 crackCol = _CrackColor * crackGlow * 1.5;

                fixed4 finalCol = lerp(crackCol, shardCol, shardFill);
                float finalAlpha = max(shardFill, crackGlow);

                finalCol.a = saturate(finalAlpha);
                return finalCol;
            }
            ENDCG
        }
    }
}
