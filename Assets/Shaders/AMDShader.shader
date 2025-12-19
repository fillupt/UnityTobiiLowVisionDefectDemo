Shader "Custom/AMDShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _GazeCenter ("Gaze Center", Vector) = (0.5, 0.5, 0, 0)
        _VignetteColor ("Vignette Color", Color) = (0.5, 0.5, 0.5, 1)
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

            // Simple 2D noise function for organic variation
            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                
                // Smooth interpolation
                f = f * f * (3.0 - 2.0 * f);
                
                // Four corners of the cell
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                // Bilinear interpolation
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Multi-octave noise for natural variation
            float fbm(float2 p) {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 3; i++) {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            fixed4 frag (v2f i) : SV_Target {
                if (_EnableShader == 0) {
                    return tex2D(_MainTex, i.uv);
                }

                // Calculate parameters from disease severity (0-1)
                float vignetteSize = lerp(0.05, 0.8, _DiseaseSeverity); // Grows from 0.05 to 0.8
                float vignetteAlpha = vignetteSize; // Linked to size
                float distortionRadius = lerp(0.1, 0.3, _DiseaseSeverity); // Subtle distortion area
                float distortionAmount = lerp(0.1, 0.4, _DiseaseSeverity); // Reduced distortion strength
                float scotomaIrregularity = lerp(0.1, 0.8, _DiseaseSeverity); // More irregular as severity increases
                float blurStrength = 2.0; // Increased blur for softer edge

                // Apply distortion for wet AMD
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

                // Central scotoma (reverse vignette with irregular shape)
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 normalizedUV = (i.uv - _GazeCenter) / vignetteSize;
                normalizedUV.x *= aspectRatio;
                
                // Add organic distortion to the scotoma shape
                float angle = atan2(normalizedUV.y, normalizedUV.x);
                float baseRadius = length(normalizedUV);
                
                // Multi-frequency noise for irregular scotoma boundary
                // Using cosine ensures smooth wrapping at -pi/pi boundary
                float irregularity = 0.0;
                if (scotomaIrregularity > 0.01) {
                    float noise1 = cos(angle * 3.0 + _Time.y * 0.1);
                    float noise2 = cos(angle * 5.0 - _Time.y * 0.15);
                    float noise3 = cos(angle * 7.0 + _Time.y * 0.08);
                    irregularity = (noise1 * 0.5 + noise2 * 0.3 + noise3 * 0.2) * scotomaIrregularity * 0.2;
                }
                
                float radius = baseRadius + irregularity;
                
                // Use wider smoothstep range for softer transition
                float vignette = smoothstep(vignetteAlpha - 0.1, 1.0 + 0.1, radius);
                
                // Reverse for central scotoma
                vignette = 1.0 - vignette;

                // Add subtle organic color variation within the scotoma using Perlin-like noise
                // Use gaze-centered UV so noise moves with scotoma
                float2 centeredUV = (i.uv - _GazeCenter) * float2(_ScreenParams.x / 100.0, _ScreenParams.y / 100.0);
                
                // Generate smooth organic noise
                float noiseValue = fbm(centeredUV * 2.0);
                
                // Map noise from [0,1] to [-0.02, 0.02] for subtle variation
                float colorVariation = (noiseValue - 0.5) * 0.04 * vignette;
                
                // Modulate the vignette color with subtle variations
                fixed3 variedColor = _VignetteColor.rgb + colorVariation;

                col.rgb = col.rgb * (1 - vignette) + variedColor * vignette;

                return col;
            }
            ENDCG
        }
    }
}
