Shader "Sizzle/Transitions/BlindsTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _SlatCount ("Slat Count", Float) = 16
        _Direction ("Direction (0=Horizontal, 1=Vertical)", Float) = 0
        _WaveStagger ("Wave Stagger (0=Simultaneous, 1=Cascade)", Range(0, 1)) = 0.5
        _ShadingIntensity ("3D Shading Depth", Range(0, 1)) = 0.35
        _Softness ("Edge Softness", Range(0, 0.1)) = 0.01
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
            float _SlatCount;
            float _Direction;
            float _WaveStagger;
            float _ShadingIntensity;
            float _Softness;
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

                float coord = (_Direction > 0.5) ? i.uv.x : i.uv.y;
                float totalSlats = max(1.0, _SlatCount);
                float slatIdx = floor(coord * totalSlats);
                float slatLocal = frac(coord * totalSlats);

                // 파동 지연(Cascade) 계산
                float normIdx = slatIdx / max(1.0, totalSlats - 1.0);
                float maxProg = 1.0 + _WaveStagger * 0.8;
                float slatProg = saturate((_Progress * maxProg - normIdx * (_WaveStagger * 0.8)));

                // 슬랫 회전 투영 폭 (0 -> 1)
                float angle = slatProg * 1.5707963;
                float coverage = sin(angle);

                float alpha = 0.0;
                if (_Softness <= 0.00001)
                {
                    alpha = (slatLocal <= coverage) ? 1.0 : 0.0;
                }
                else
                {
                    alpha = 1.0 - smoothstep(coverage - _Softness, coverage + _Softness, slatLocal);
                }

                if (alpha <= 0.0001)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 회전 시 입체 음영 그라디언트 (블라인드 윗면/아랫면 입체감)
                float shade = 1.0 - (1.0 - slatLocal / max(0.001, coverage)) * _ShadingIntensity * cos(angle);

                fixed4 finalCol = _Color * i.color;
                finalCol.rgb *= saturate(shade);
                finalCol.a *= alpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
