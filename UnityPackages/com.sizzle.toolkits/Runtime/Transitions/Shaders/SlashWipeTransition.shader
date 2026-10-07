Shader "Sizzle/Transitions/SlashWipeTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0.02, 0.02, 0.03, 1.0)
        _SlashColor ("Slash Flash Color", Color) = (0.3, 0.85, 1.0, 1.0)
        _Angle ("Slash Angle (Degrees)", Range(-80, 80)) = 35
        _SlashWidth ("Slash Width", Range(0.01, 0.1)) = 0.03
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
            fixed4 _SlashColor;
            float _Angle;
            float _SlashWidth;
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

                float rad = radians(_Angle);
                float2 normal = float2(-sin(rad), cos(rad)); // 절단선 법선 벡터
                float2 tangent = float2(cos(rad), sin(rad)); // 절단선 진행 방향

                // 종횡비 보정된 중심 좌표계 (설정 각도 및 빔 두께의 등방성 보장)
                float2 cUV = float2((i.uv.x - 0.5) * aspect, i.uv.y - 0.5);
                float perpDist = dot(cUV, normal);
                float tangDist = dot(cUV, tangent);

                // 화면 전체 모서리 도달 반경
                float maxExtent = sqrt(pow(0.5 * aspect, 2.0) + 0.25) + 0.2;

                // 1단계: 검격 궤적 섬광
                float slashTimeline = saturate(_Progress / 0.35);
                float tipPos = lerp(-maxExtent, maxExtent, slashTimeline);
                float distBehindTip = tangDist - tipPos;

                float trail = 0.0;
                if (distBehindTip <= 0.05 && distBehindTip >= -0.7)
                {
                    trail = (distBehindTip > 0.0) ? (1.0 - distBehindTip / 0.05) : (1.0 + distBehindTip / 0.7);
                }

                float beamGlow = exp(-abs(perpDist) / max(0.001, _SlashWidth)) * trail * 2.8;

                // 2단계: 절단선 기준 양측 화면 암전 확산
                float tWipe = saturate((_Progress - 0.2) / 0.8);
                float maxSpread = maxExtent;
                float currentSpread = tWipe * maxSpread;
                float sideFill = saturate((currentSpread - abs(perpDist)) * 20.0);

                // 색상 및 알파 합성
                fixed4 baseCol = _Color * i.color;
                fixed4 glowCol = _SlashColor * beamGlow;

                fixed4 finalCol = lerp(glowCol, baseCol, sideFill);
                float finalAlpha = saturate(max(sideFill, beamGlow));

                finalCol.a = finalAlpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
