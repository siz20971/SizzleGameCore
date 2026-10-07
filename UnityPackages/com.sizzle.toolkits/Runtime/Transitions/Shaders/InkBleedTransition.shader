Shader "Sizzle/Transitions/InkBleedTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Ink Color", Color) = (0.03, 0.03, 0.04, 1.0)
        _Center ("Center (X, Y)", Vector) = (0.5, 0.5, 0, 0)
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 1.777777
        _Roughness ("Roughness (Organic Splatter)", Range(0, 1)) = 0.4
        _Feather ("Bleed Feather (Softness)", Range(0, 0.3)) = 0.06
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
            float4 _Center;
            float _AspectRatio;
            float _Roughness;
            float _Feather;
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

                float2 p = float2((i.uv.x - _Center.x) * aspect, i.uv.y - _Center.y);
                float dist = length(p);
                float angle = atan2(p.y, p.x);

                // 유기적인 먹물 외곽선 노이즈 (조화 주파수 합성)
                float n1 = sin(angle * 3.0 + 0.8) * 0.4 + sin(angle * 7.0 - 1.4) * 0.25;
                float n2 = sin(angle * 13.0 + 2.7) * 0.15 + sin(angle * 29.0) * 0.08;
                float organicVariation = (n1 + n2) * _Roughness;

                // 2D 미세 격자 노이즈로 종이 결(Fiber) 및 비산 효과 시뮬레이션
                float2 gridCoord = p * 8.0;
                float2 gridId = floor(gridCoord);
                float hash = frac(sin(dot(gridId, float2(12.9898, 78.233))) * 43758.5453);
                float fiberNoise = (hash - 0.5) * 0.15 * _Roughness;

                float effDist = dist - (organicVariation + fiberNoise);

                // 주변 물방울 비산 (Secondary Droplets)
                float drop1 = length(p - float2(0.45 * aspect * 0.5, 0.25)) - (_Progress * 0.35);
                float drop2 = length(p - float2(-0.4 * aspect * 0.5, -0.28)) - (_Progress * 0.32);
                float drop3 = length(p - float2(0.2 * aspect * 0.5, -0.38)) - (_Progress * 0.28);
                effDist = min(effDist, min(drop1, min(drop2, drop3)));

                // 화면 모서리까지 완전 도달 보장 반경
                float maxCorner = sqrt(pow(max(_Center.x, 1.0 - _Center.x) * aspect, 2.0) + pow(max(_Center.y, 1.0 - _Center.y), 2.0));
                float maxR = maxCorner + 0.35;
                float targetR = _Progress * maxR;

                float alpha = 0.0;
                if (_Feather <= 0.00001)
                {
                    alpha = (effDist <= targetR) ? 1.0 : 0.0;
                }
                else
                {
                    alpha = saturate((targetR - effDist) / _Feather);
                }

                fixed4 finalCol = _Color * i.color;
                finalCol.a *= alpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
