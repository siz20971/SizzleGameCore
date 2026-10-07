Shader "Sizzle/Transitions/CheckerboardGridTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _Columns ("Columns", Float) = 16
        _Rows ("Rows (When Not Square)", Float) = 9
        _SquareGrid ("Force Square Grid", Float) = 1
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 1.777777
        _CheckerboardMode ("Mode (0=Alternating, 1=DirectionalWave, 2=Simultaneous)", Float) = 0
        _WaveDirection ("Wave Direction (0=BL->TR, 1=TL->BR, 2=BR->TL, 3=TR->BL, 4=L->R, 5=R->L, 6=B->T, 7=T->B)", Float) = 0
        _CellShape ("Shape (0=Square, 1=Diamond)", Float) = 0
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
            float _Columns;
            float _Rows;
            float _SquareGrid;
            float _AspectRatio;
            float _CheckerboardMode;
            float _WaveDirection;
            float _CellShape;
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

                float cols = max(1.0, _Columns);
                float rows = max(1.0, _Rows);
                float2 tileCoord;

                if (_SquareGrid > 0.5)
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

                    tileCoord = float2(i.uv.x * cols, i.uv.y * rowSpan + rowOffset);
                    rows = rowSpan;
                }
                else
                {
                    tileCoord = float2(i.uv.x * cols, i.uv.y * rows);
                }

                float2 cellId = floor(tileCoord);
                float2 cellUV = frac(tileCoord);
                float2 localPos = cellUV - 0.5;

                // 체커보드 홀짝 패리티 (0 또는 1)
                float parity = fmod(abs(cellId.x + cellId.y), 2.0);

                // 셀별 진행률 계산 (모드별)
                float cellProgress = 0.0;
                if (_CheckerboardMode < 0.5)
                {
                    // 0: Alternating (전반 50%는 짝수 타일, 후반 50%는 홀수 타일 전개)
                    if (parity < 0.5)
                    {
                        cellProgress = saturate(_Progress * 2.0);
                    }
                    else
                    {
                        cellProgress = saturate((_Progress - 0.5) * 2.0);
                    }
                }
                else if (_CheckerboardMode < 1.5)
                {
                    // 1: Directional Wave (방향 지정 파동 전개)
                    float maxCol = max(1.0, cols);
                    float maxRow = max(1.0, rows);
                    float normX = saturate(cellId.x / maxCol);
                    float normY = saturate(cellId.y / maxRow);

                    float normDist = 0.0;
                    int dir = (int)(_WaveDirection + 0.5);

                    if (dir == 0) // BottomLeftToTopRight (좌하단 -> 우상단)
                    {
                        normDist = (normX + normY) * 0.5;
                    }
                    else if (dir == 1) // TopLeftToBottomRight (좌상단 -> 우하단)
                    {
                        normDist = (normX + (1.0 - normY)) * 0.5;
                    }
                    else if (dir == 2) // BottomRightToTopLeft (우하단 -> 좌상단)
                    {
                        normDist = ((1.0 - normX) + normY) * 0.5;
                    }
                    else if (dir == 3) // TopRightToBottomLeft (우상단 -> 좌하단)
                    {
                        normDist = ((1.0 - normX) + (1.0 - normY)) * 0.5;
                    }
                    else if (dir == 4) // LeftToRight (좌 -> 우)
                    {
                        normDist = normX;
                    }
                    else if (dir == 5) // RightToLeft (우 -> 좌)
                    {
                        normDist = 1.0 - normX;
                    }
                    else if (dir == 6) // BottomToTop (하 -> 상)
                    {
                        normDist = normY;
                    }
                    else // 7: TopToBottom (상 -> 하)
                    {
                        normDist = 1.0 - normY;
                    }

                    cellProgress = saturate((_Progress * 1.5 - normDist * 0.5) / 0.5);
                }
                else
                {
                    // 2: Simultaneous (동시 전개)
                    cellProgress = _Progress;
                }

                // 타일 확장 거리 (Square vs Diamond)
                float dist = 0.0;
                if (_CellShape > 0.5)
                {
                    // 다이아몬드 확장
                    dist = (abs(localPos.x) + abs(localPos.y));
                }
                else
                {
                    // 사각형 확장
                    dist = max(abs(localPos.x), abs(localPos.y)) * 2.0;
                }

                float alpha = 0.0;
                if (_Softness <= 0.00001)
                {
                    alpha = (dist <= cellProgress) ? 1.0 : 0.0;
                }
                else
                {
                    float threshold = lerp(-_Softness, 1.0 + _Softness, cellProgress);
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
