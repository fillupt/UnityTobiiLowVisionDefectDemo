Shader "Custom/GlaucomaShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _GazeCenter ("Gaze Center", Vector) = (0.5, 0.5, 0, 0)
        _VignetteColor ("Vignette Color", Color) = (0.2, 0.2, 0.2, 1)
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
            fixed4 _VignetteColor;
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

                fixed4 col = tex2D(_MainTex, i.uv);

                // Calculate parameters from disease severity (0-1)
                float vignetteSize = lerp(1.0, 0.1, _DiseaseSeverity); // Shrinks from 1.0 to 0.1
                float glaucomaIrregularity = 0.3; // Fixed irregularity

                // Calculate peripheral vision loss (glaucoma)
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 normalizedUV = (i.uv - _GazeCenter) / vignetteSize;
                normalizedUV.x *= aspectRatio;
                
                // Add organic distortion to the peripheral boundary
                float angle = atan2(normalizedUV.y, normalizedUV.x);
                float baseRadius = length(normalizedUV);
                
                // Multi-frequency noise for irregular glaucoma boundary with random time-based variation
                // Using cosine ensures smooth wrapping at -pi/pi boundary
                float irregularity = 0.0;
                if (glaucomaIrregularity > 0.01) {
                    float noise1 = cos(angle * 2.0 + _Time.y * 0.05);
                    float noise2 = cos(angle * 3.5 + 0.5 - _Time.y * 0.08);
                    float noise3 = cos(angle * 5.2 - 0.3 + _Time.y * 0.06);
                    // Random fluctuation between constraints instead of scaling with parameter
                    float randomFactor = (sin(_Time.y * 0.3) * 0.5 + 0.5) * 0.5 + 0.5; // 0.5 to 1.0
                    irregularity = (noise1 * 0.5 + noise2 * 0.3 + noise3 * 0.2) * glaucomaIrregularity * 0.1 * randomFactor;
                }
                
                float normalizedDistance = baseRadius + irregularity;

                // Calculate peripheral vision degradation zone (scales with vignetteSize)
                // Lower vignetteSize = worse glaucoma = larger affected area
                float peripheralZone = smoothstep(0.2, 0.7, normalizedDistance);
                
                // Reduce saturation in peripheral vision (scales with disease severity)
                float3 gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float saturationLoss = peripheralZone * _DiseaseSeverity; // More loss as disease worsens
                col.rgb = lerp(col.rgb, gray, saturationLoss * 0.7);
                
                // Reduce contrast in peripheral vision
                float contrastReduction = peripheralZone * _DiseaseSeverity;
                float3 flatGray = gray * 0.6 + 0.2; // Flatten towards mid-gray
                col.rgb = lerp(col.rgb, flatGray, contrastReduction * 0.5);

                // Final peripheral darkening - complete vision loss at edges
                // Wider smoothstep range for softer transition
                float vignette = smoothstep(0.0, 1.15, normalizedDistance);

                // Gradient darkening from grey to black at edges
                float darkenFactor = smoothstep(0.4, 1.0, normalizedDistance);
                fixed3 darkenedVignetteColor = _VignetteColor.rgb * (1.0 - darkenFactor * 0.8);
                
                col.rgb = col.rgb * (1 - vignette) + darkenedVignetteColor * vignette;

                return col;
            }
            ENDCG
        }
    }
}
