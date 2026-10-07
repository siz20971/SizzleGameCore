Shader "Sizzle/Transitions/VHSRewindTransition"
{
    Properties
    {
        // UGUI Image 컴포넌트에서 material.mainTexture 참조 시 _MainTex 누락 에러 방지용 (CanvasRenderer 바인딩)
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0.02, 0.02, 0.05, 1.0)
        _NoiseColor ("Static Noise Color", Color) = (0.85, 0.9, 0.95, 1.0)
        _TrackingSpeed ("Tracking Bar Speed", Float) = 12.0
        _NoiseIntensity ("Noise Grain Intensity", Range(0, 1)) = 0.5
        _TrackingBarHeight ("Tracking Bar Height", Range(0.01, 0.3)) = 0.12
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
            fixed4 _NoiseColor;
            float _TrackingSpeed;
            float _NoiseIntensity;
            float _TrackingBarHeight;
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

                // 1. VHS 트래킹 노이즈 바 (화면을 상하로 오르내리는 굵은 왜곡 띠)
                float bandPos = frac(_Time.y * _TrackingSpeed * 0.08);
                float distToBand = abs(i.uv.y - bandPos);
                distToBand = min(distToBand, 1.0 - distToBand);
                float trackingBar = 1.0 - smoothstep(0.0, _TrackingBarHeight, distToBand);

                // 2. 아날로그 고주파 스노우 노이즈
                float2 noiseSeed = i.uv + float2(sin(_Time.y * 80.0), cos(_Time.y * 60.0));
                float snow = frac(sin(dot(noiseSeed, float2(12.9898, 78.233))) * 43758.5453);

                // 3. 인터레이스 주사선
                float scanline = sin(i.uv.y * 320.0 * 3.14159) * 0.5 + 0.5;

                // 노이즈와 트래킹 바 합성
                float glitchSignal = trackingBar * 0.6 + snow * _NoiseIntensity + (1.0 - scanline) * 0.25;

                // 진행도에 따른 화면 덮힘 임계값
                float threshold = lerp(-0.15, 1.15, _Progress);
                float coveragePattern = snow * 0.4 + (1.0 - i.uv.y) * 0.35 + trackingBar * 0.25;
                float mainAlpha = saturate((threshold - coveragePattern) * 8.0);

                // 초반 노이즈 플래시 가산
                float flashAlpha = glitchSignal * saturate(_Progress * 2.5) * (1.0 - _Progress * 0.6);
                float totalAlpha = max(mainAlpha, flashAlpha);

                if (totalAlpha <= 0.0001)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 정전기 노이즈 스파크 색상 합성
                fixed4 sparkCol = lerp(_Color, _NoiseColor, snow * trackingBar * 0.85);
                fixed4 finalCol = sparkCol * i.color;
                finalCol.a = totalAlpha;

                return finalCol;
            }
            ENDCG
        }
    }
}
