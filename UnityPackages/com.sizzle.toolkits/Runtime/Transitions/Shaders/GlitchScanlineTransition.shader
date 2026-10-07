Shader "Sizzle/Transitions/GlitchScanlineTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0.05, 0.05, 0.08, 1)
        _GlitchColor ("Glitch Accent Color", Color) = (0.2, 0.9, 1.0, 1.0)
        _ScanlineCount ("Scanline Count", Float) = 240
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.35
        _JitterAmount ("Horizontal Jitter", Range(0, 0.5)) = 0.15
        _FlickerSpeed ("Flicker Speed", Float) = 25.0
        _BlockColumns ("Block Columns", Float) = 32
        _BlockRows ("Block Rows", Float) = 18
        _GlitchEdgeWidth ("Glitch Edge Width", Range(0.01, 0.3)) = 0.12
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
            fixed4 _GlitchColor;
            float _ScanlineCount;
            float _ScanlineIntensity;
            float _JitterAmount;
            float _FlickerSpeed;
            float _BlockColumns;
            float _BlockRows;
            float _GlitchEdgeWidth;
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

                float timeStep = floor(_Time.y * _FlickerSpeed);

                // 1. 가로 슬라이스 지터 (Horizontal Jitter)
                float sliceId = floor(i.uv.y * 35.0);
                float sliceNoise = frac(sin(sliceId * 78.233 + timeStep) * 43758.5453);
                float jitter = (sliceNoise > 0.65) ? (sliceNoise - 0.8) * _JitterAmount : 0.0;

                float2 gUV = float2(i.uv.x + jitter, i.uv.y);

                // 2. 디지털 블록 노이즈 (Block Noise)
                float2 blockGrid = float2(max(1.0, _BlockColumns), max(1.0, _BlockRows));
                float2 blockId = floor(gUV * blockGrid);
                float blockNoise = frac(sin(dot(blockId, float2(12.9898, 78.233)) + timeStep * 0.7) * 43758.5453);

                // 3. 미세 라인 노이즈
                float lineNoise = frac(sin(floor(i.uv.y * _ScanlineCount * 0.25) + timeStep) * 23421.63);

                // 종합 임계치 계산 (화면 상하 흐름 + 블록 노이즈)
                float wipePattern = saturate(i.uv.y * 0.3 + blockNoise * 0.5 + lineNoise * 0.2);

                // 진행도에 따른 활성화
                float threshold = lerp(-0.08, 1.08, _Progress);
                float diff = threshold - wipePattern;
                float alpha = saturate(diff * 12.0);

                if (alpha <= 0.0001)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 4. CRT 스캔라인 명암 변조
                float scanline = sin(i.uv.y * _ScanlineCount * 3.14159265);
                scanline = scanline * 0.5 + 0.5;
                float scanlineDarken = lerp(1.0, scanline, _ScanlineIntensity);

                // 5. 글리치 전선 엣지 하이라이트 (Glitch Edge Color)
                float edgeFactor = saturate(1.0 - abs(diff) / max(0.001, _GlitchEdgeWidth));
                fixed4 blendedColor = lerp(_Color, _GlitchColor, edgeFactor * 0.85);

                blendedColor.rgb *= scanlineDarken;
                blendedColor *= i.color;
                blendedColor.a *= alpha;

                return blendedColor;
            }
            ENDCG
        }
    }
}
