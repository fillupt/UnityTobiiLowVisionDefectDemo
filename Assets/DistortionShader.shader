Shader "Custom/DistortionShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionCenter ("Distortion Center", Vector) = (0.5, 0.5, 0, 0)
        _DistortionSize ("Distortion Size", Range(0, 1)) = 0.1
        _DistortionAmount ("Distortion Amount", Range(0, 1)) = 0.2
        _DistortionRadius ("Distortion Radius", Range(0, 1)) = 0.3
        _BlurStrength ("Blur Strength", Range(0, 10)) = 1
    }
 
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
 
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
 
            struct v2f {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
 
            sampler2D _MainTex;
            float2 _DistortionCenter;
            float _DistortionSize;
            float _DistortionAmount;
            float _DistortionRadius;
            float _BlurStrength;
 
            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
 
            // Gaussian blur function
            float Gaussian(float x, float sigma) {
                float coeff = 1.0 / sqrt(2.0 * 3.14159 * sigma);
                return coeff * exp(-(x * x) / (2.0 * sigma * sigma));
            }
 
            fixed4 frag(v2f i) : SV_Target {
                // Calculate the distance from the distortion center
                float2 center = i.uv - _DistortionCenter;
                float distance = length(center) / _DistortionRadius;
 
                // Calculate the distortion amount based on the distance and radius
                float distortion = 0;
                if (distance <= 1.0) {
                    distortion = _DistortionAmount * (1 - distance / _DistortionSize);
                }
 
                // Apply Gaussian blur fade
                float blurFade = Gaussian(distance, _BlurStrength);
                distortion *= blurFade;
 
                // Calculate the offset based on the distortion amount
                float2 offset = distortion * center;
 
                // Sample the color from the texture with the distorted UV coordinates
                fixed4 col = tex2D(_MainTex, i.uv + offset);
 
                return col;
            }
            ENDCG
        }
    }
}
