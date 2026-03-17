Shader "Hidden/KawaseBlur" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Blur Offset", Float) = 1.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize; // Automatically filled by Unity (x=1/w, y=1/h)
            float _Offset;

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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // The core Kawase algorithm: sample 4 diagonal pixels
                // We use _MainTex_TexelSize to ensure offsets are pixel-perfect
                float2 res = _MainTex_TexelSize.xy;
                float2 offset = (_Offset + 0.5) * res;

                fixed4 col = 0;
                col += tex2D(_MainTex, i.uv + float2(offset.x, offset.y));
                col += tex2D(_MainTex, i.uv + float2(-offset.x, offset.y));
                col += tex2D(_MainTex, i.uv + float2(offset.x, -offset.y));
                col += tex2D(_MainTex, i.uv + float2(-offset.x, -offset.y));

                return col * 0.25;
            }
            ENDCG
        }
    }
}