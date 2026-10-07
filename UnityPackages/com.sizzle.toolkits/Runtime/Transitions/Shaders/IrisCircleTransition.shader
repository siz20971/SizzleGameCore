Shader "Sizzle/Transitions/IrisCircleTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 1.5
        _Softness ("Softness", Range(0, 0.5)) = 0.02
        _AspectRatio ("Aspect Ratio (W/H)", Float) = 1.777777
        _Invert ("Invert", Float) = 0
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
            float _Radius;
            float _Softness;
            float _AspectRatio;
            float _Invert;

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
                // 화면 픽셀 종횡비 계산 (_ScreenParams 우선, 폴백 _AspectRatio)
                float aspect = (_ScreenParams.y > 0.0) ? (_ScreenParams.x / _ScreenParams.y) : _AspectRatio;
                aspect = max(0.001, aspect);

                float2 diff = float2((i.uv.x - _Center.x) * aspect, i.uv.y - _Center.y);
                float dist = length(diff);

                float alpha = 0.0;
                if (_Softness <= 0.00001)
                {
                    alpha = (dist > _Radius) ? 1.0 : 0.0;
                }
                else
                {
                    float innerEdge = _Radius - _Softness;
                    float outerEdge = _Radius + _Softness;

                    if (dist >= outerEdge)
                    {
                        alpha = 1.0;
                    }
                    else if (dist <= innerEdge)
                    {
                        alpha = 0.0;
                    }
                    else
                    {
                        alpha = smoothstep(innerEdge, outerEdge, dist);
                    }
                }

                if (_Invert > 0.5)
                {
                    alpha = 1.0 - alpha;
                }

                fixed4 finalCol = _Color * i.color;
                finalCol.a *= alpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
