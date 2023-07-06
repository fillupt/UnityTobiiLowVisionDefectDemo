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
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert

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

        struct Input {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o) {
            // Calculate the distance from the distortion center
            float2 center = IN.uv_MainTex - _DistortionCenter;
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
            fixed4 col = tex2D(_MainTex, IN.uv_MainTex + offset);

            // Apply vignette effect
            float2 vignetteCenter = _DistortionCenter;
            float vignetteSize = _VignetteSize;
            float vignetteStrength = _VignetteAlpha;
            float2 vignetteOffset = IN.uv_MainTex - vignetteCenter;
            float vignetteDistance = length(vignetteOffset) / vignetteSize;
            float vignette = smoothstep(vignetteStrength, 1.0, vignetteDistance);

            if (_ReverseVignette > 0.5) {
                vignette = 1.0 - vignette;
            }

            float aspectRatio = _ScreenParams.y / _ScreenParams.x;
            float2 normalizedUV = (IN.uv_MainTex - _DistortionCenter) / vignetteSize * float2(1.0, aspectRatio);
            float normalizedDistance = length(normalizedUV);

            vignette = smoothstep(vignetteStrength, 1.0, normalizedDistance);

            if (_ReverseVignette > 0.5) {
                vignette = 1.0 - vignette;
            }

            fixed4 vignetteColor = _VignetteColor * vignette;

            // Combine the color with the vignette
            col.rgb = col.rgb * (1 - vignette) + vignetteColor.rgb * vignette;

            o.Albedo = col.rgb;
            o.Alpha = col.a;
        }
        ENDCG
    }
}

