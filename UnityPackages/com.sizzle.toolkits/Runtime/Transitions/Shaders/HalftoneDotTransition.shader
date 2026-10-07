Shader "Sizzle/Transitions/HalftoneDotTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _DotDensity ("Dot Density", Float) = 40
        _Angle ("Screen Angle (Degrees)", Range(0, 90)) = 45
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 1.777777
        _Softness ("Softness", Range(0, 0.2)) = 0.02
        _WaveMode ("Wave Mode (0=Uniform, 1=CenterOut, 2=Diagonal)", Float) = 1
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
            float _DotDensity;
            float _Angle;
            float _AspectRatio;
            float _Softness;
            float _WaveMode;
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

                // 종횡비 보정된 중심 좌표계 (화면 픽셀 공간과 1:1 대응하여 왜곡 없는 완벽한 원형 보장)
                float2 centeredUV = float2((i.uv.x - 0.5) * aspect, i.uv.y - 0.5);

                // 스크린 앵글 회전 (코믹스 하프톤 특유의 45도 등)
                float rad = radians(_Angle);
                float c = cos(rad);
                float s = sin(rad);
                float2 rotUV = float2(centeredUV.x * c - centeredUV.y * s, centeredUV.x * s + centeredUV.y * c);

                // 도트 그리드 셀 분할
                float2 cellUV = frac(rotUV * _DotDensity) - 0.5;
                float dist = length(cellUV);

                // 전개 모드에 따른 셀별 진행도 계산
                float cellProgress = _Progress;
                if (_WaveMode > 0.5 && _WaveMode < 1.5)
                {
                    // 1: 중심에서 외곽으로 확산
                    float centerDist = length(centeredUV) * 0.9;
                    cellProgress = saturate((_Progress * 1.8 - centerDist) / 0.8);
                }
                else if (_WaveMode >= 1.5)
                {
                    // 2: 대각선 방향 와이프 (종횡비 보정)
                    float diag = (centeredUV.x + centeredUV.y) * 0.5 + 0.5;
                    cellProgress = saturate((_Progress * 1.6 - diag * 0.6) / 0.4);
                }

                // 모서리까지 완벽히 채우기 위한 최대 반경 (대각선 길이 약 0.707 + 마진)
                float maxRadius = 0.75;
                float currentRadius = cellProgress * maxRadius;

                float alpha = 0.0;
                if (_Softness <= 0.00001)
                {
                    alpha = (dist <= currentRadius) ? 1.0 : 0.0;
                }
                else
                {
                    float threshold = lerp(-_Softness, maxRadius + _Softness, cellProgress);
                    alpha = 1.0 - smoothstep(threshold - _Softness, threshold + _Softness, dist);
                }

                fixed4 finalCol = _Color * i.color;
                finalCol.a *= alpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
