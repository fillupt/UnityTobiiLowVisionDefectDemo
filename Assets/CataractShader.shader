Shader "Custom/CataractShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _GazeCenter ("Gaze Center", Vector) = (0.5, 0.5, 0, 0)
        _CataractColor ("Cataract Color", Color) = (1, 0.9, 0.7, 1)
        _VignetteAlpha ("Vignette Alpha", Range(0, 1)) = 0.0
        _VignetteSize ("Vignette Size", Range(0, 1)) = 0.7
        _CataractIrregularity ("Cataract Irregularity", Range(0, 1)) = 0.0
        _GlobalFilter ("Global Filter", Range(0, 1)) = 0.0
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
            fixed4 _CataractColor;
            float _VignetteAlpha;
            float _VignetteSize;
            float _CataractIrregularity;
            float _GlobalFilter;
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
                
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

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
                fixed4 col = tex2D(_MainTex, i.uv);

                if (_EnableShader == 1) {
                    // Central clouding (reverse vignette with irregular shape)
                    float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                    float2 normalizedUV = (i.uv - _GazeCenter) / _VignetteSize;
                    normalizedUV.x *= aspectRatio;
                
                // Add organic distortion to the cataract boundary
                float angle = atan2(normalizedUV.y, normalizedUV.x);
                float baseRadius = length(normalizedUV);
                
                // Multi-frequency noise for irregular cataract boundary
                // Using cosine ensures smooth wrapping at -pi/pi boundary
                float irregularity = 0.0;
                if (_CataractIrregularity > 0.01) {
                    float noise1 = cos(angle * 2.5);
                    float noise2 = cos(angle * 4.3 + 0.7);
                    float noise3 = cos(angle * 6.7 - 0.4);
                    irregularity = (noise1 * 0.5 + noise2 * 0.3 + noise3 * 0.2) * _CataractIrregularity * 0.15;
                }
                
                float radius = baseRadius + irregularity;
                
                // Use wider smoothstep range for softer transition
                float vignette = smoothstep(_VignetteAlpha - 0.1, 1.0 + 0.1, radius);
                
                // Reverse for central clouding
                vignette = 1.0 - vignette;

                // Add subtle organic color and opacity variation within the cataract
                float2 centeredUV = (i.uv - _GazeCenter) * float2(_ScreenParams.x / 100.0, _ScreenParams.y / 100.0);
                float noiseValue = fbm(centeredUV * 2.0);
                float colorVariation = (noiseValue - 0.5) * 0.05 * vignette; // Slightly more variation than AMD
                
                // Vary both color and opacity for more organic appearance
                fixed3 variedColor = _CataractColor.rgb + colorVariation;
                float variedAlpha = vignette * (1.0 + (noiseValue - 0.5) * 0.2); // ±10% opacity variation

                col.rgb = col.rgb * (1 - variedAlpha) + variedColor * variedAlpha;
                }

                // Apply global filter (0.1 to 0.8) as full-screen catColour overlay
                // This applies regardless of _EnableShader state
                if (_GlobalFilter > 0.001) {
                    col.rgb = lerp(col.rgb, _CataractColor.rgb, _GlobalFilter);
                }

                return col;
            }
            ENDCG
        }
    }
}
