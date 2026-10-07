Shader "Sizzle/Transitions/DirectionalWipeTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _Angle ("Angle (Degrees)", Range(0, 360)) = 0
        _Progress ("Progress", Range(0, 1)) = 0
        _Softness ("Softness", Range(0, 0.5)) = 0.05
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
            float _Angle;
            float _Progress;
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

                float rad = radians(_Angle);
                float2 dir = float2(cos(rad), sin(rad));

                // 최대 반경: 종횡비가 보정된 화면 중심에서 모서리까지의 프로젝션 최대치
                float r = 0.5 * (abs(dir.x) * aspect + abs(dir.y));

                // 중심 기준 진행 축으로의 사영 거리 d: [-r, r]
                float2 centered = float2((i.uv.x - 0.5) * aspect, i.uv.y - 0.5);
                float d = dot(centered, dir);

                float alpha = 0.0;
                if (_Softness <= 0.00001)
                {
                    float threshold = lerp(-r, r, _Progress);
                    alpha = (d <= threshold) ? 1.0 : 0.0;
                }
                else
                {
                    float threshold = lerp(-r - _Softness * 2.0, r + _Softness * 2.0, _Progress);
                    alpha = 1.0 - smoothstep(threshold - _Softness, threshold + _Softness, d);
                }

                fixed4 finalCol = _Color * i.color;
                finalCol.a *= alpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
