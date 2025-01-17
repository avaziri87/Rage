﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/CameraBloodEffect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_bloodTex("Blood Texture", 2D) = "white" {}
		_bloodBump("Blood Normal", 2D) = "bump" {}
		_bloodAmount("Blood Amount", Range(0,1)) = 0
		_distortion("Blood Distortion", Range(0, 2)) = 0
	}

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _bloodTex;
			sampler2D _bloodBump;
			float _bloodAmount;
			float _distortion;


			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 bloodCol = tex2D(_bloodTex, i.uv);
				bloodCol.a = saturate(bloodCol.a + (_bloodAmount * 2 - 1));

				half2 bump = UnpackNormal(tex2D(_bloodBump, i.uv)).xy;
				fixed4 srcCol = tex2D(_MainTex, i.uv + bump * bloodCol.a * _distortion);
				
				fixed4 overlayCol = srcCol * bloodCol * 4;
				overlayCol = lerp(srcCol, overlayCol, 0.85);
				
				fixed4 output = lerp(srcCol, overlayCol, bloodCol.a);
				return output;

			}
			ENDCG
		}
	}

}