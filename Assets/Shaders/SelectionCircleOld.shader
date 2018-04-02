Shader "Custom/SelectionCircleOld" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_ForegroundMask ("Mask", 2D) = "white" {}
		_Cutoff1 ("Cutoff1", Range(0,1)) = 0.5
		_Cutoff2 ("Cutoff2", Range(0,1)) = 0.5
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Cull Off
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		//#pragma surface surf Standard fullforwardshadows
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _ForegroundMask;

		struct Input {
			float2 uv_ForegroundMask;
		};


		fixed4 _Color;
		half _Cutoff1;
		half _Cutoff2;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_ForegroundMask, IN.uv_ForegroundMask);
			clip(1 - (c.r + _Cutoff1));
			clip(c.r - _Cutoff2);
			o.Albedo = _Color;
			o.Alpha = c.r - _Cutoff1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
