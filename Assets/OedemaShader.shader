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
                float distortionRadius = lerp(0.12, 0.63, _DiseaseSeverity); // Reduced max by ~10% (0.7 -> 0.63)
                float distortionAmount = lerp(0.15, 1.08, _DiseaseSeverity); // Reduced max by ~10% (1.2 -> 1.08)
                float irregularity = lerp(0.05, 0.3, _DiseaseSeverity); // Organic boundary variation
                float blurStrength = 1.5; // Increased for softer falloff

                // Apply aspect ratio correction for circular shape
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 center = i.uv - _GazeCenter;
                center.x *= aspectRatio;
                
                // Add irregular boundary variation
                float angle = atan2(center.y, center.x);
                float radiusVariation = 0.0;
                if (irregularity > 0.01) {
                    float noise1 = cos(angle * 2.5 + _Time.y * 0.12);
                    float noise2 = cos(angle * 4.0 - _Time.y * 0.08);
                    float noise3 = cos(angle * 5.5 + _Time.y * 0.15);
                    radiusVariation = (noise1 * 0.5 + noise2 * 0.3 + noise3 * 0.2) * irregularity * 0.15;
                }
                
                float effectiveRadius = distortionRadius * (1.0 + radiusVariation);
                float distance = length(center) / effectiveRadius;

                // Use Gaussian falloff for smooth transition
                float blurFade = exp(-(distance * distance) / (2.0 * blurStrength * blurStrength));
                
                // Apply distortion with smooth falloff
                float distortion = distortionAmount * (1.0 - smoothstep(0.0, 1.2, distance)) * blurFade;

                // Magnification effect: pull UVs toward center (negative offset)
                float2 offset = -distortion * normalize(center) * length(center);
                float2 sampleUV = i.uv + offset;
                
                // Clamp UV to prevent sampling outside texture bounds (prevents vertical blocking artifacts)
                sampleUV = clamp(sampleUV, 0.0, 1.0);
                
                fixed4 col = tex2D(_MainTex, sampleUV);

                // Apply contrast reduction with smooth falloff (no hard boundary)
                float contrastLoss = blurFade * distortionAmount * 0.3; // Scale down since distortion is stronger
                float3 gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float3 flatColor = gray * 0.7 + 0.15; // Flatten towards mid-gray
                col.rgb = lerp(col.rgb, flatColor, contrastLoss * 0.85);

                return col;
            }
            ENDCG
        }
    }
}
