Shader "Custom/GlaucomaShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _GazeCenter ("Gaze Center", Vector) = (0.5, 0.5, 0, 0)
        _VignetteColor ("Vignette Color", Color) = (0.2, 0.2, 0.2, 1)
        _VignetteSize ("Vignette Size", Range(0, 1)) = 1.0
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
            float _VignetteSize;
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

                // Calculate peripheral vision loss (glaucoma)
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 normalizedUV = (i.uv - _GazeCenter) / _VignetteSize;
                normalizedUV.x *= aspectRatio;
                
                float normalizedDistance = length(normalizedUV);

                // Peripheral darkening - no vignette at center, darkens towards edges
                float vignette = smoothstep(0.0, 1.0, normalizedDistance);

                // Gradient darkening from grey to black at edges
                float darkenFactor = smoothstep(0.3, 1.0, normalizedDistance);
                fixed3 darkenedVignetteColor = _VignetteColor.rgb * (1.0 - darkenFactor * 0.8);
                
                col.rgb = col.rgb * (1 - vignette) + darkenedVignetteColor * vignette;

                return col;
            }
            ENDCG
        }
    }
}
