Shader "Custom/DistortionCircularVignetteShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionCenter ("Distortion Center", Vector) = (0.5, 0.5, 0, 0)
        _DistortionSize ("Distortion Size", Range(0, 1)) = 0.1
        _DistortionAmount ("Distortion Amount", Range(0, 1)) = 0.2
        _DistortionRadius ("Distortion Radius", Range(0, 1)) = 0.3
        _BlurStrength ("Blur Strength", Range(0, 10)) = 1
        _VignetteColor ("Vignette Color", Color) = (0, 0, 0, 1)
        _VignetteAlpha ("Vignette Alpha", Range(0, 1)) = 0.5
        _VignetteSize ("Vignette Size", Range(0, 1)) = 0.5
        _ReverseVignette ("Reverse Vignette", Range(0, 1)) = 0
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
            float2 _DistortionCenter;
            float _DistortionSize;
            float _DistortionAmount;
            float _DistortionRadius;
            float _BlurStrength;
            fixed4 _VignetteColor;
            float _VignetteAlpha;
            float _VignetteSize;
            float _ReverseVignette;
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
                    // Disable the shader effect, output the original color
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                }
                // Calculate the distance from the distortion center
                float2 center = i.uv - _DistortionCenter;
                float distance = length(center) / _DistortionRadius;

                // Calculate the distortion amount based on the distance and radius
                float distortion = 0;
                if (distance <= 1.0) {
                    distortion = _DistortionAmount * (1 - distance / _DistortionSize);
                }

                // Apply Gaussian blur fade
                float blurFade = exp(-(distance * distance) / (2.0 * _BlurStrength * _BlurStrength));
                distortion *= blurFade;

                // Calculate the offset based on the distortion amount
                float2 offset = distortion * center;

                // Sample the color from the texture with the distorted UV coordinates
                fixed4 col = tex2D(_MainTex, i.uv + offset);

                // Apply vignette effect
                float2 vignetteCenter = _DistortionCenter;
                float vignetteSize = _VignetteSize;
                float vignetteStrength = _VignetteAlpha;

                float aspectRatio = _ScreenParams.y / _ScreenParams.x;
                float2 normalizedUV = (i.uv - _DistortionCenter) / vignetteSize * float2(1.0, aspectRatio);
                
                float normalizedDistance = length(normalizedUV);

                float vignette = smoothstep(vignetteStrength, 1.0, normalizedDistance);

                if (_ReverseVignette > 0.5) {
                    vignette = 1.0 - vignette;
                }

                // Combine the color with the vignette
                col.rgb = col.rgb * (1 - vignette) + _VignetteColor.rgb * vignette;

                return col;
            }
            ENDCG
        }
    }
}

