// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

 // Unlit alpha-blended shader.
 // - no lighting
 // - no lightmap support
 // - no per-material color

Shader "Custom/Grid" {
	Properties{
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_SpeedX("Speed X", Range(-8, 8)) = 1
		_SpeedY("Speed Y", Range(-8, 8)) = 1

		_Color("Color", Color) = (1,1,1,1)
		_Mult("Multiplier", Range(0, 4)) = 1
		_BaseMult("Alpha Mult", Range(0, 1)) = 1
		_MainMult("Ring Alpha Mult", Range(0, 1)) = 1
		
		_MainAlphaScale("Ring Alpha Scale", Range(0.00000001, 36)) = 5
		_MainAlphaOffset("Ring Alpha Radius Offset", Range(-50, 50)) = 0
	}

		SubShader{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			LOD 100
			ZWrite Off
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass {
			
				CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma multi_compile_fog

					#include "UnityCG.cginc"

					struct appdata_t {
						float4 vertex : POSITION;
						float2 texcoord : TEXCOORD0;
						float3 normal : TEXCOORD2; //you don't need these semantics except for XBox360
					};

					struct v2f {
						float4 vertex : SV_POSITION;

						half2 texcoord : TEXCOORD0;
						float3 normal : TEXCOORD2; //you don't need these semantics except for XBox360
						float3 viewT : TEXCOORD3; //you don't need these semantics except for XBox360
						half dist : TEXCOORD4;
						UNITY_FOG_COORDS(1)
					};

					sampler2D _MainTex;
					float4 _MainTex_ST;
					half _SpeedX;
					half _SpeedY;


					half _Mult;
					half _BaseMult;
					half _MainMult;
					fixed4 _Color;
					half _MainAlphaScale;
					half _MainAlphaOffset;

					v2f vert(appdata_t v) {
						v2f o;
						o.normal = normalize(v.normal);
						o.viewT = normalize(ObjSpaceViewDir(v.vertex));//ObjSpaceViewDir is similar, but localspace.
						o.vertex = UnityObjectToClipPos(v.vertex);
						o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
						o.dist = length(ObjSpaceViewDir(v.vertex));
						UNITY_TRANSFER_FOG(o,o.vertex);
						return o;
					}

					fixed4 frag(v2f i) : SV_Target {
						fixed4 col;
						/*
						half2 texCoord = i.texcoord;
						texCoord.x += _Time * _SpeedX;
						texCoord.y += _Time * _SpeedY;
						half4 tex = (tex2D(_MainTex, texCoord)
							+ tex2D(_MainTex, (texCoord + half2(1, 1)) * 5)
							+ tex2D(_MainTex, (texCoord) * 50) * 0.5) / 2;
						*/

						half2 texCoord = i.texcoord;
						half2 offset = 0;
						offset.x = _Time * _SpeedX;
						offset.y = _Time * _SpeedY;
						half4 tex = (tex2D(_MainTex, texCoord + offset)
							+ tex2D(_MainTex, (texCoord + offset * 0.5) * 5)
							+ tex2D(_MainTex, (texCoord + offset * 0.2) * 20)) / 2;

						half biasA = clamp(((i.dist - _MainAlphaOffset) / _MainAlphaScale) - 0.5, 0, 1);
						biasA += clamp((1 - biasA) - 0.5, 0, 1) * 2;
						biasA = clamp(1 - biasA, 0, 1);

						half biasB = clamp(((i.dist - _MainAlphaOffset) / _MainAlphaScale) - 0.5, 0, 1);
						
						col = _Color * _Mult;
						col.a = _MainMult * biasA * tex.a + _BaseMult * tex.a * biasB;
						UNITY_APPLY_FOG(i.fogCoord, col);
						return col;
					}
				ENDCG
			}
		}

}