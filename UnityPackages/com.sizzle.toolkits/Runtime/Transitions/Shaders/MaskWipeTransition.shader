Shader "Sizzle/Transitions/MaskWipeTransition"
{
    Properties
    {
        _MainTex ("Mask Texture", 2D) = "white" {}
        _Color ("Transition Color", Color) = (0, 0, 0, 1)
        _CutOff ("CutOff", Range(-0.1, 1.1)) = -0.1
        _Invert ("Invert", Float) = 0
        _Softness ("Softness", Range(0, 0.5)) = 0.0
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
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _CutOff;
            float _Invert;
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
                fixed4 maskCol = tex2D(_MainTex, i.uv);
                float maskVal = _Invert > 0.5 ? (1.0 - maskCol.r) : maskCol.r;

                float alpha = 0.0;
                if (_Softness > 0.001)
                {
                    alpha = smoothstep(_CutOff + _Softness, _CutOff - _Softness, maskVal);
                }
                else
                {
                    alpha = maskVal < _CutOff ? 1.0 : 0.0;
                }

                fixed4 finalCol = _Color * i.color;
                finalCol.a *= alpha;
                return finalCol;
            }
            ENDCG
        }
    }
}
