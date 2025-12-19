Shader "Custom/DiabeticRetinopathyShader" {
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

            // Hash function for pseudo-random values
            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float hash13(float3 p3) {
                p3 = frac(p3 * 0.1031);
                p3 += dot(p3, p3.zyx + 31.32);
                return frac((p3.x + p3.y) * p3.z);
            }

            // 2D noise function
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

            // Fractional Brownian Motion for organic patterns
            float fbm(float2 p) {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++) {
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

                fixed4 col = tex2D(_MainTex, i.uv);

                // Calculate distance from gaze center for effects
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 centerOffset = i.uv - _GazeCenter;
                centerOffset.x *= aspectRatio;
                float distFromCenter = length(centerOffset);

                // Microaneurysms and hemorrhages - small dark spots scattered across retina
                // Gaze-contingent: spots positioned relative to retinal location
                float2 gazeRelativeUV = (i.uv - _GazeCenter + 0.5); // Center at gaze, wrap to 0-1 range
                float2 spotUV = gazeRelativeUV * float2(_ScreenParams.x / 50.0, _ScreenParams.y / 50.0);
                float spotDensity = lerp(0.0, 0.85, _DiseaseSeverity); // More spots with severity
                
                float darkening = 0.0;
                // Generate multiple layers of spots at different scales with organic positioning
                for(int layer = 0; layer < 3; layer++) {
                    float scale = float(layer + 1) * 2.5;
                    
                    // Use continuous noise field instead of grid cells for organic distribution
                    int numSpots = int(20.0 * scale * spotDensity);
                    
                    for(int spot = 0; spot < numSpots; spot++) {
                        // Generate pseudo-random spot positions using hash
                        float2 spotSeed = float2(float(spot) * 7.13, float(layer) * 3.17);
                        float2 spotCenter = float2(hash(spotSeed), hash(spotSeed + float2(13.7, 27.3)));
                        
                        // Calculate distance from current pixel to this spot center
                        float2 toSpot = (gazeRelativeUV - spotCenter) * float2(aspectRatio, 1.0);
                        float spotDist = length(toSpot);
                        
                        // Vary spot size randomly
                        float spotSize = lerp(0.008, 0.025, hash(spotSeed + float2(5.5, 9.9)));
                        
                        // Add organic shape variation using noise
                        float angle = atan2(toSpot.y, toSpot.x);
                        float shapeNoise = noise(spotCenter * 100.0 + float2(layer, spot));
                        float irregularity = 1.0 + (shapeNoise - 0.5) * 0.4;
                        float effectiveSize = spotSize * irregularity;
                        
                        // Very soft falloff for organic appearance
                        float spotStrength = 1.0 - smoothstep(effectiveSize * 0.3, effectiveSize * 1.8, spotDist);
                        darkening += spotStrength * 0.25;
                    }
                }
                
                // Apply spot darkening
                darkening = saturate(darkening);
                col.rgb *= (1.0 - darkening * 0.7); // Max 70% darkening from spots

                // Macular edema - blur and distortion in central/paracentral region
                // More pronounced as severity increases, accelerates after 50%
                if(_DiseaseSeverity > 0.1) {
                    // Scale edema radius more aggressively after 50% severity
                    float edemaScaling = _DiseaseSeverity < 0.5 ? _DiseaseSeverity : 
                                         0.5 + (_DiseaseSeverity - 0.5) * 2.2; // Stronger acceleration above 50%
                    float edemaRadius = lerp(0.0, 0.6, edemaScaling); // Larger max radius
                    float edemaStrength = smoothstep(edemaRadius * 1.5, 0.0, distFromCenter);
                    
                    if(edemaStrength > 0.01) {
                        // Increase distortion strength at higher severities
                        float distortionScale = _DiseaseSeverity < 0.5 ? _DiseaseSeverity : 
                                               0.5 + (_DiseaseSeverity - 0.5) * 2.5; // More distortion at max
                        
                        // Add subtle distortion in edema region
                        float2 distortionUV = i.uv * 10.0 + _Time.y * 0.1;
                        float distortNoise = fbm(distortionUV) - 0.5;
                        float2 distortion = float2(distortNoise, fbm(distortionUV + float2(5.3, 2.1)) - 0.5);
                        distortion *= edemaStrength * 0.02 * distortionScale; // Increased from 0.015 to 0.02
                        
                        // Sample with slight distortion for blur-like effect
                        float2 blurredUV = saturate(i.uv + distortion);
                        fixed4 blurredCol = tex2D(_MainTex, blurredUV);
                        
                        // Increase blur mixing at high severity - more aggressive
                        float blurMix = lerp(0.6, 0.95, saturate((_DiseaseSeverity - 0.5) * 2.0)); // Up to 95%
                        col.rgb = lerp(col.rgb, blurredCol.rgb, edemaStrength * blurMix);
                        
                        // Reduce contrast in edema region, more at high severity
                        float contrastLoss = lerp(0.4, 0.85, saturate((_DiseaseSeverity - 0.5) * 2.0)); // Up to 85%
                        float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                        col.rgb = lerp(col.rgb, float3(gray, gray, gray) * 0.6 + 0.2, edemaStrength * contrastLoss);
                    }
                }

                // Cotton wool spots - larger, softer whitish patches (ischemic areas)
                // Appear at higher severity levels, gaze-contingent
                if(_DiseaseSeverity > 0.3) {
                    float2 gazeRelativeCW = (i.uv - _GazeCenter + 0.5);
                    float cottonWoolDensity = (_DiseaseSeverity - 0.3) * 1.4; // Scale from 0.3-1.0 severity
                    
                    float cottonWool = 0.0;
                    int numCottonWool = int(8.0 * cottonWoolDensity);
                    
                    for(int cw = 0; cw < numCottonWool; cw++) {
                        // Generate pseudo-random cotton wool spot positions
                        float2 cwSeed = float2(float(cw) * 11.37, float(cw) * 19.83);
                        float2 cwCenter = float2(hash(cwSeed + float2(200, 0)), hash(cwSeed + float2(0, 200)));
                        
                        // Calculate distance from current pixel to cotton wool center
                        float2 toCW = (gazeRelativeCW - cwCenter) * float2(aspectRatio, 1.0);
                        float cwDist = length(toCW);
                        
                        // Larger, more varied sizes for cotton wool spots
                        float cwSize = lerp(0.025, 0.06, hash(cwSeed + float2(7.7, 13.3)));
                        
                        // Add organic irregular shape using noise
                        float cwAngle = atan2(toCW.y, toCW.x);
                        float cwShapeNoise = noise(cwCenter * 80.0 + float2(cw, 50));
                        float cwIrregularity = 1.0 + (cwShapeNoise - 0.5) * 0.6; // More variation than dark spots
                        float cwEffectiveSize = cwSize * cwIrregularity;
                        
                        // Very soft, diffuse edges for cotton wool appearance
                        float cwStrength = 1.0 - smoothstep(cwEffectiveSize * 0.2, cwEffectiveSize * 2.0, cwDist);
                        cottonWool += cwStrength * 0.5;
                    }
                    
                    // Apply cotton wool lightening (whitish patches)
                    cottonWool = saturate(cottonWool);
                    col.rgb = lerp(col.rgb, float3(0.9, 0.9, 0.85), cottonWool * 0.4);
                }

                // Overall vision degradation at high severity
                if(_DiseaseSeverity > 0.5) {
                    float overallDegradation = (_DiseaseSeverity - 0.5) * 2.0; // 0-1 range for severity 0.5-1.0
                    
                    // Reduce overall contrast
                    float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
                    col.rgb = lerp(col.rgb, float3(gray, gray, gray) * 0.6 + 0.2, overallDegradation * 0.3);
                    
                    // Slight overall darkening
                    col.rgb *= lerp(1.0, 0.7, overallDegradation * 0.5);
                }

                return col;
            }
            ENDCG
        }
    }
}
