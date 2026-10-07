Shader "Sizzle/Transitions/RadialClockTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _StartAngle ("Start Angle", Range(0, 360)) = 90
        _Progress ("Progress", Range(0, 1)) = 0
        _Clockwise ("Clockwise", Float) = 1
        _Softness ("Softness", Range(0, 0.2)) = 0.01
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 1.777777
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
            float _StartAngle;
            float _Progress;
            float _Clockwise;
            float _Softness;
            float _AspectRatio;

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
                // 진행도 0/1 절대 경계값 보장 (어떤 Softness 값이든 100% 완전 투명 / 100% 완전 덮임)
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

                // 종횡비 보정된 중심 벡터 (각속도가 4분면에서 균일하게 회전)
                float2 diff = float2((i.uv.x - _Center.x) * aspect, i.uv.y - _Center.y);
                float angle = atan2(diff.y, diff.x);
                float deg = degrees(angle);
                if (deg < 0.0) deg += 360.0;

                float normAngle = 0.0;
                if (_Clockwise > 0.5)
                {
                    normAngle = _StartAngle - deg;
                }
                else
                {
                    normAngle = deg - _StartAngle;
                }
                normAngle = fmod(normAngle + 360.0, 360.0) / 360.0;

                float alpha = 0.0;
                if (_Softness <= 0.00001)
                {
                    alpha = (normAngle < _Progress) ? 1.0 : 0.0;
                }
                else
                {
                    // 진행도 0 -> 1에 맞춰 소프트니스 밴드가 0 이전(-softness*2)부터 1 이후(1+softness*2)까지 스윕
                    float threshold = lerp(-_Softness * 2.0, 1.0 + _Softness * 2.0, _Progress);
                    float edge0 = threshold + _Softness;
                    float edge1 = threshold - _Softness;

                    if (normAngle <= edge1)
                    {
                        alpha = 1.0;
                    }
                    else if (normAngle >= edge0)
                    {
                        alpha = 0.0;
                    }
                    else
                    {
                        alpha = smoothstep(edge0, edge1, normAngle);
                    }
                }

                fixed4 finalCol = _Color * i.color;
                finalCol.a *= alpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
