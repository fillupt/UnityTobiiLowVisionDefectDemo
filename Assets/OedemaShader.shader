Shader "Custom/OedemaShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _GazeCenter ("Gaze Center", Vector) = (0.5, 0.5, 0, 0)
        _DistortionAmount ("Distortion Amount", Range(0, 1)) = 0.08
        _DistortionRadius ("Distortion Radius", Range(0, 1)) = 0.08
        _BlurStrength ("Blur Strength", Range(0, 10)) = 1
        _VignetteSize ("Vignette Size", Range(0, 1)) = 0.001
        _EnableShader ("Enable Shader", Float) = 1
    }

    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        Pass {
            ZTest Always
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _GazeCenter;
            float _DistortionAmount;
            float _DistortionRadius;
            float _BlurStrength;
            float _VignetteSize;
            float _EnableShader;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                if (_EnableShader == 0) {
                    return tex2D(_MainTex, i.uv);
                }

                // Localized distortion for oedema
                float2 center = i.uv - _GazeCenter;
                float distance = length(center) / _DistortionRadius;

                float distortion = 0;
                if (distance <= 1.0) {
                    distortion = _DistortionAmount * (1 - distance);
                }

                float blurFade = exp(-(distance * distance) / (2.0 * _BlurStrength * _BlurStrength));
                distortion *= blurFade;

                float2 offset = distortion * center;
                fixed4 col = tex2D(_MainTex, i.uv + offset);

                return col;
            }
            ENDCG
        }
    }
}
