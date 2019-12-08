// Copyright (c) 2018 Jakub Boksansky - All Rights Reserved
// Final Vignette Unity Plugin 1.0

Shader "Hidden/Wilberforce/FinalVignette"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Pox("PosX", Float) = .5 
		_Poy("PosY", Float) = .5 
	}
		CGINCLUDE

		#include "UnityCG.cginc"

		// ========================================================================
		// Uniform definitions 
		// ========================================================================

		float4 _ProjInfo;

		sampler2D _MainTex;
		float4 _MainTex_TexelSize;
		float4 _MainTex_ST;

		uniform int debugMode;
		uniform float aspectRatio;
		uniform int needsYFlip;
		uniform int needsStereoAdjust;

		// Vignette ===============================================================
		uniform float vignetteFalloff;
		uniform float vignetteMin;
		uniform float vignetteMax;
		uniform float _Pox;
		uniform float _Poy;
		uniform float2 vignetteCenter;
		uniform float4 vignetteInnerColor;
		uniform float4 vignetteOuterColor;
		uniform float vignetteSaturationMin;
		uniform float vignetteSaturationMax;
		uniform float vignetteMaxDistance;
		uniform float vignetteMinDistance;
		uniform int isAnamorphicVignette;

		// ========================================================================
		// Structs definitions 
		// ========================================================================

		struct v2fDouble {
			float4 pos : SV_POSITION;
			float2 uv[2] : TEXCOORD0;
		};

		// ========================================================================
		// Helper functions
		// ========================================================================

		float luma(half3 color) {
			return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
		}

		float3 setSaturation(float3 color, float saturation) {
			return lerp(luma(color), color, saturation);
		}

		#ifndef SHADER_API_GLCORE
		#ifndef SHADER_API_OPENGL
		#ifndef SHADER_API_GLES
		#ifndef SHADER_API_GLES3
		#ifndef SHADER_API_VULKAN
		#define WFORCE_VAO_OPENGL_OFF
		#endif
		#endif
		#endif
		#endif
		#endif

		// ========================================================================
		// Vertex shaders 
		// ========================================================================

		v2fDouble vertDouble(appdata_img v)
		{
			v2fDouble o;
			o.pos = UnityObjectToClipPos(v.vertex);

	#ifdef UNITY_SINGLE_PASS_STEREO
			float2 temp = UnityStereoScreenSpaceUVAdjust(v.texcoord, _MainTex_ST);
	#else
			float2 temp = TRANSFORM_TEX(v.texcoord, _MainTex);
	#endif
			o.uv[0] = temp;
			o.uv[1] = temp;

	#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0)
				o.uv[1].y = 1.0f - o.uv[1].y;
	#endif

#ifdef WFORCE_VAO_OPENGL_OFF
			if (needsYFlip != 0) {
				o.uv[1].y = 1.0f - o.uv[1].y;
			}
#endif

			if (needsStereoAdjust != 0) {
				o.uv[1].x *= 0.5f;
				if (unity_StereoEyeIndex > 0)
					o.uv[1].x += 0.5f;
			}

			return o;
		}

		// ========================================================================
		// Image effects 
		// ========================================================================

		half4 vignette(v2fDouble input, float4 color, int isSaturationMode, int vignetteMode) : SV_Target
		{
				
			if (isAnamorphicVignette == 0) {
				input.uv[0].y = (((input.uv[0].y * 2.0f - 1.0f) * aspectRatio) + 1.0f) * 0.5f;
			}

			// get distance from  screen center
			//vignetteCenter.x = _Pox;
			//vignetteCenter.y = _Poy;

			float2 center = UnityStereoScreenSpaceUVAdjust(vignetteCenter, _MainTex_ST);
			float2 dir = center - input.uv[0];
					
			// Raise to the power of "radialness"
			float u = saturate((length(dir) - vignetteMinDistance) / (vignetteMaxDistance - vignetteMinDistance));
			u = saturate(pow(u, vignetteFalloff));

			// Saturation
			if (isSaturationMode != 0) {
				float usat = lerp(vignetteSaturationMin, vignetteSaturationMax, u);
				color.rgb = setSaturation(color.rgb, usat);
			}

			if (vignetteMode == 1) {
				float result = lerp(vignetteMax, vignetteMin, u);
				color = float4(color.rgb * result, color.a);
			} else if (vignetteMode == 2) {
				
				float4 mixingColor = lerp(vignetteInnerColor, vignetteOuterColor, u);
				color = float4(lerp(color.rgb, mixingColor.rgb, mixingColor.a), color.a);
			}
			
			return color;
		}

	ENDCG
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		// 0 - VignetteOnlyNoSaturationStandardMode
		Pass{CGPROGRAM
			#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target { return vignette(i, tex2Dlod(_MainTex, float4(i.uv[1], 0.0f, 0.0f)), 0, 1); }
			ENDCG}

		// 1 - VignetteOnlyNoSaturationStandardModeBlend
		Pass{Blend DstColor Zero // Multiplicative
			CGPROGRAM
			#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, float4(1.0f, 1.0f, 1.0f, 1.0f), 0, 1); }
			ENDCG }
			
		// 2 - VignetteOnlySaturationStandardMode
		Pass{ CGPROGRAM
			#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, tex2Dlod(_MainTex, float4(i.uv[1], 0.0f, 0.0f)), 1, 1); }
			ENDCG }

		// 3 - VignetteOnlySaturationStandardModeBlend
		Pass{ Blend DstColor Zero // Multiplicative
			CGPROGRAM
			#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, float4(1.0f, 1.0f, 1.0f, 1.0f), 1, 1); }
			ENDCG }

		// 4 - VignetteOnlyNoSaturationCustomMode
		Pass{ CGPROGRAM
			#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, tex2Dlod(_MainTex, float4(i.uv[1], 0.0f, 0.0f)), 0, 2); }
			ENDCG }

		// 5 - VignetteOnlyNoSaturationCustomModeBlend
		Pass{ Blend DstColor Zero // Multiplicative
			CGPROGRAM
			#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, float4(1.0f, 1.0f, 1.0f, 1.0f), 0, 2); }
			ENDCG }

		// 6 - VignetteOnlySaturationCustomMode
		Pass{ CGPROGRAM
			#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, tex2Dlod(_MainTex, float4(i.uv[1], 0.0f, 0.0f)), 1, 2); }
			ENDCG }

		// 7 - VignetteOnlySaturationCustomModeBlend
		Pass{ Blend DstColor Zero // Multiplicative
			CGPROGRAM
			#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, float4(1.0f, 1.0f, 1.0f, 1.0f), 1, 2); }
			ENDCG }

		// 8 - DebugDisplayPassStandard
		Pass{ CGPROGRAM
#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, float4(1.0f, 1.0f, 1.0f, 1.0f), 0, 1); }
			ENDCG }

		// 9 - DebugDisplayPassCustom
		Pass{ CGPROGRAM
#pragma vertex vertDouble #pragma fragment frag
			half4 frag(v2fDouble i) : SV_Target{ return vignette(i, float4(1.0f, 1.0f, 1.0f, 1.0f), 0, 2); }
			ENDCG }

	}
}
