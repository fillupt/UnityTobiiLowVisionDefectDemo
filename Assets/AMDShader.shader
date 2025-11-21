Shader "Custom/AMDShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _GazeCenter ("Gaze Center", Vector) = (0.5, 0.5, 0, 0)
        _DistortionAmount ("Distortion Amount", Range(0, 1)) = 0.5
        _DistortionRadius ("Distortion Radius", Range(0, 1)) = 0.05
        _BlurStrength ("Blur Strength", Range(0, 10)) = 1
        _VignetteColor ("Vignette Color", Color) = (0.5, 0.5, 0.5, 1)
        _VignetteAlpha ("Vignette Alpha", Range(0, 1)) = 0.0
        _VignetteSize ("Vignette Size", Range(0, 1)) = 0.05
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
            fixed4 _VignetteColor;
            float _VignetteAlpha;
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

                // Apply distortion for wet AMD
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

                // Central scotoma (reverse vignette)
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 normalizedUV = (i.uv - _GazeCenter) / _VignetteSize;
                normalizedUV.x *= aspectRatio;
                
                float normalizedDistance = length(normalizedUV);
                float vignette = smoothstep(_VignetteAlpha, 1.0, normalizedDistance);
                
                // Reverse for central scotoma
                vignette = 1.0 - vignette;

                col.rgb = col.rgb * (1 - vignette) + _VignetteColor.rgb * vignette;

                return col;
            }
            ENDCG
        }
    }
}
