// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Particles/Additive (Negative)" {
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
	_Neg ("Neg Multiplier", Range(-100, 0)) = -1
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

	Blend One One
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off

	SubShader {
		Pass {

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_particles
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed4 _TintColor;
			half _Neg;

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				#ifdef SOFTPARTICLES_ON
				float4 projPos : TEXCOORD2;
				#endif
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				#ifdef SOFTPARTICLES_ON
				o.projPos = ComputeScreenPos (o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
				#endif
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			float _InvFade;

			fixed4 frag (v2f i) : SV_Target
			{
				half4 col = i.color * tex2D(_MainTex, i.texcoord);
				col.rgb *= col.a;
				col.rgb *= _Neg;
				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0)); // fog towards black due to our blend mode
				return col;
			}
			ENDCG
		}
	}
}
}
