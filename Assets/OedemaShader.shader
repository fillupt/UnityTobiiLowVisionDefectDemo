Shader "Custom/OedemaShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _GazeCenter ("Gaze Center", Vector) = (0.5, 0.5, 0, 0)
        _DiseaseSeverity ("Disease Severity", Range(0, 1)) = 0.0
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
            float _DiseaseSeverity;
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

                // Calculate parameters from disease severity (0-1)
                float distortionRadius = lerp(0.12, 0.7, _DiseaseSeverity); // Grows from 0.12 to 0.7
                float distortionAmount = distortionRadius; // Linked to radius
                float blurStrength = 1.0;

                // Localized distortion for oedema
                float2 center = i.uv - _GazeCenter;
                float distance = length(center) / distortionRadius;

                float distortion = 0;
                if (distance <= 1.0) {
                    distortion = distortionAmount * (1 - distance);
                }

                float blurFade = exp(-(distance * distance) / (2.0 * blurStrength * blurStrength));
                distortion *= blurFade;

                float2 offset = distortion * center;
                fixed4 col = tex2D(_MainTex, i.uv + offset);

                // Apply contrast reduction in the distorted area (fluid accumulation reduces clarity)
                if (distance <= 1.0) {
                    float contrastLoss = blurFade * distortionAmount;
                    float3 gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                    float3 flatColor = gray * 0.7 + 0.15; // Flatten towards mid-gray
                    col.rgb = lerp(col.rgb, flatColor, contrastLoss * 0.85);
                }

                return col;
            }
            ENDCG
        }
    }
}
