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
        _ScotomaIrregularity ("Scotoma Irregularity", Range(0, 1)) = 0.0
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
            float _ScotomaIrregularity;
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

                // Central scotoma (reverse vignette with irregular shape)
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 normalizedUV = (i.uv - _GazeCenter) / _VignetteSize;
                normalizedUV.x *= aspectRatio;
                
                // Add organic distortion to the scotoma shape
                float angle = atan2(normalizedUV.y, normalizedUV.x);
                float baseRadius = length(normalizedUV);
                
                // Multi-frequency noise for irregular scotoma boundary
                // Using cosine ensures smooth wrapping at -pi/pi boundary
                float irregularity = 0.0;
                if (_ScotomaIrregularity > 0.01) {
                    float noise1 = cos(angle * 3.0 + _Time.y * 0.1);
                    float noise2 = cos(angle * 5.0 - _Time.y * 0.15);
                    float noise3 = cos(angle * 7.0 + _Time.y * 0.08);
                    irregularity = (noise1 * 0.5 + noise2 * 0.3 + noise3 * 0.2) * _ScotomaIrregularity * 0.2;
                }
                
                float radius = baseRadius + irregularity;
                
                // Use wider smoothstep range for softer transition
                float vignette = smoothstep(_VignetteAlpha - 0.1, 1.0 + 0.1, radius);
                
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
