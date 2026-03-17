Shader "Custom/OedemaShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _GazeCenter ("Gaze Center", Vector) = (0.5, 0.5, 0, 0)
        _DiseaseSeverity ("Disease Severity", Range(0, 1)) = 0.0
        _EnableShader ("Enable Shader", Float) = 1
        _BlurTex ("Blurred Texture", 2D) = "white" {}
        _BlurAmount ("Blur Blend Amount", Range(0, 1)) = 0.0
        _BlurRadius ("Blur Gaze Radius", Float) = 0.5
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
            sampler2D _BlurTex;
            float2 _GazeCenter;
            float _DiseaseSeverity;
            float _EnableShader;
            float _BlurAmount;
            float _BlurRadius;

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
                float distortionRadius = lerp(0.12, 0.55, _DiseaseSeverity); // Reduced max (0.63 -> 0.55)
                float distortionAmount = lerp(0.15, 0.85, _DiseaseSeverity); // Reduced max (1.08 -> 0.85)
                float blurStrength = 1.5; // Increased for softer falloff

                // Apply aspect ratio correction for circular base shape
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 center = i.uv - _GazeCenter;
                center.x *= aspectRatio;
                
                // Oedema is fluid accumulation - should be smooth and circular without irregular boundaries
                float distance = length(center) / distortionRadius;

                // Use Gaussian falloff for smooth transition
                float blurFade = exp(-(distance * distance) / (2.0 * blurStrength * blurStrength));
                
                // Apply distortion with smooth falloff
                float distortion = distortionAmount * (1.0 - smoothstep(0.0, 1.2, distance)) * blurFade;

                // Magnification effect: pull UVs toward center (negative offset)
                float2 offset = -distortion * center;
                float2 sampleUV = i.uv + offset;
                
                // Clamp UV to prevent sampling outside texture bounds (prevents vertical blocking artifacts)
                sampleUV = clamp(sampleUV, 0.0, 1.0);
                
                float blurAspect = _ScreenParams.x / _ScreenParams.y;
                float2 blurDelta = (i.uv - _GazeCenter) * float2(blurAspect, 1.0);
                float blurDistanceSq = dot(blurDelta, blurDelta);
                float blurSigma = max(_BlurRadius, 0.001);
                float blurMask = exp(-blurDistanceSq / (2.0 * blurSigma * blurSigma));
                float blurBlend = saturate(_BlurAmount) * blurMask;
                fixed4 col = lerp(tex2D(_MainTex, sampleUV), tex2D(_BlurTex, sampleUV), blurBlend);

                // Apply contrast and color reduction with smooth falloff
                // At higher severity, increase central degradation
                float centralDegradation = _DiseaseSeverity * (1.0 - distance) * blurFade;
                
                // Contrast reduction increases toward center and with severity
                float contrastLoss = (blurFade * distortionAmount * 0.3) + (centralDegradation * 0.5);
                float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float3 flatColor = float3(gray, gray, gray) * 0.7 + 0.15; // Flatten towards mid-gray
                col.rgb = lerp(col.rgb, flatColor, contrastLoss * 0.85);
                
                // Additional desaturation at center for higher severity
                if (_DiseaseSeverity > 0.3) {
                    float desaturation = centralDegradation * 0.7;
                    col.rgb = lerp(col.rgb, float3(gray, gray, gray), desaturation);
                }

                return col;
            }
            ENDCG
        }
    }
}
