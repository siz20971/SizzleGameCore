Shader "Sizzle/Transitions/CRTTurnOffTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _BeamColor ("Beam Flash Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _BeamIntensity ("Beam Intensity", Float) = 2.5
        _BeamThickness ("Beam Thickness", Float) = 0.005
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
            fixed4 _BeamColor;
            float _BeamIntensity;
            float _BeamThickness;
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

                float dy = abs(i.uv.y - 0.5);
                float dx = abs(i.uv.x - 0.5);

                fixed4 finalCol = _Color * i.color;

                if (_Progress < 0.6)
                {
                    // 1단계: 수직 방향 압축 (화면 상하가 중앙선으로 수축)
                    float t1 = _Progress / 0.6;
                    // 이징 커브 적용 (점점 빠르게 수축)
                    float curHalfH = 0.5 * (1.0 - t1 * t1);

                    if (dy > curHalfH)
                    {
                        // 닫힌 영역: 완전 불투명 전환색
                        return finalCol;
                    }

                    // 수축 중인 전자빔 가장자리 섬광
                    float edgeDist = abs(dy - curHalfH);
                    float glow = exp(-edgeDist * 180.0) * t1 * _BeamIntensity;
                    fixed4 glowCol = _BeamColor * glow;

                    // 열려있는 내부: 투명하지만 엣지 글로우 가산
                    return fixed4(glowCol.rgb, saturate(glow));
                }
                else if (_Progress < 0.88)
                {
                    // 2단계: 수평 방향 압축 (수평선이 중앙 점으로 수축)
                    float t2 = (_Progress - 0.6) / 0.28;
                    float curHalfW = 0.5 * (1.0 - t2);
                    float curLineH = max(0.001, _BeamThickness * (1.0 - t2 * 0.5));

                    if (dx > curHalfW || dy > curLineH)
                    {
                        // 닫힌 영역
                        return finalCol;
                    }

                    // 수평 빔 중심 발광
                    float beamGlow = (1.0 - (dy / curLineH)) * (1.0 - (dx / max(0.001, curHalfW)) * 0.3);
                    beamGlow *= _BeamIntensity;

                    fixed4 beam = _BeamColor * beamGlow;
                    return fixed4(beam.rgb, saturate(beamGlow));
                }
                else
                {
                    // 3단계: 중앙 잔광 점 소멸 (Phosphor Dot Fade) - 종횡비 보정으로 완벽한 원형 유지
                    float t3 = (_Progress - 0.88) / 0.12;
                    float2 dotOffset = float2((i.uv.x - 0.5) * aspect, i.uv.y - 0.5);
                    float dist = length(dotOffset);
                    float dotRadius = max(0.0001, 0.015 * (1.0 - t3));

                    if (dist > dotRadius * 3.0)
                    {
                        return finalCol;
                    }

                    float dotGlow = exp(-dist / dotRadius) * (1.0 - t3) * _BeamIntensity;
                    fixed4 dotCol = _BeamColor * dotGlow;

                    // 전환색과 잔광 합성
                    fixed4 res = lerp(finalCol, dotCol, saturate(dotGlow));
                    res.a = 1.0;
                    return res;
                }
            }
            ENDCG
        }
    }
}
