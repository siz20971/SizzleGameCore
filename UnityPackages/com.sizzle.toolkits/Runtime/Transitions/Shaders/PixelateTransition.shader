Shader "Sizzle/Transitions/PixelateTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _PixelCount ("Pixel Columns", Float) = 48
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 1.777777
        _SquarePixels ("Square Pixels", Float) = 1
        _Seed ("Random Seed", Float) = 1337
        _Progress ("Progress", Range(0, 1)) = 0
        _Softness ("Softness", Range(0, 0.2)) = 0
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
            float _PixelCount;
            float _AspectRatio;
            float _SquarePixels;
            float _Seed;
            float _Progress;
            float _Softness;

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

                float cols = max(1.0, _PixelCount);
                float2 blockCoord;

                if (_SquarePixels > 0.5)
                {
                    // 화면 픽셀 종횡비 계산 (_ScreenParams 우선, 폴백 _AspectRatio)
                    float aspect = (_ScreenParams.y > 0.0) ? (_ScreenParams.x / _ScreenParams.y) : _AspectRatio;
                    aspect = max(0.001, aspect);

                    // Columns 값을 가로 정사각형 타일 개수 기준으로 하여,
                    // 각 타일이 왜곡 없는 1:1 정사각형이 되도록 세로 타일 수(rowSpan) 자동 결정
                    float rowSpan = cols / aspect;

                    // 상하 여백을 중앙 대칭 정렬
                    float totalRows = ceil(rowSpan);
                    float rowOffset = (totalRows - rowSpan) * 0.5;

                    blockCoord = float2(i.uv.x * cols, i.uv.y * rowSpan + rowOffset);
                }
                else
                {
                    blockCoord = float2(i.uv.x * cols, i.uv.y * cols);
                }

                float2 blockId = floor(blockCoord) + _Seed;

                // 블록별 고유 해시값 계산 [0, 1]
                float2 p = frac(blockId * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                float hash = frac((p.x + p.y) * p.x);

                float alpha = 0.0;
                if (_Softness <= 0.00001)
                {
                    alpha = (hash < _Progress) ? 1.0 : 0.0;
                }
                else
                {
                    float threshold = lerp(-_Softness, 1.0 + _Softness, _Progress);
                    alpha = 1.0 - smoothstep(threshold - _Softness, threshold + _Softness, hash);
                }

                fixed4 finalCol = _Color * i.color;
                finalCol.a *= alpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
