Shader "Example/Shading Test B" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_LightSteps("Light Steps", Range(1,100)) = 8
		_SparkleCutoff("Sparkle Cutoff", Range(0.0001,0.995)) = 0.9
		_SparkleBrightness("Sparkle Brightness", Range(-10,10)) = 1
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Custom noambient

		sampler2D _MainTex;
		half _LightSteps;
		half _SparkleCutoff;
		half _SparkleBrightness;
		fixed4 _Color;
		
		float rand(half2 co)
		{
			float a = 12.9898;
			float b = 78.233;
			float c = 43758.5453;
			float dt = dot(co.xy, half2(a, b));
			float sn = fmod(dt, 3.14);
			return frac(sin(sn) * c);
		}

		struct SurfaceOutputCustom
		{
			fixed3 Albedo;
			fixed3 Normal;
			fixed3 Emission;
			half Specular;
			fixed Gloss;
			fixed Alpha;

			float4 ScreenPos, screenPos;
		};

		float4 screenPos;

		half4 LightingCustom(SurfaceOutputCustom s, half3 lightDir, half atten) {
			half NdotL = dot(s.Normal, lightDir);
			half4 c;
			half light = NdotL * atten;
			half lightSin = clamp(light + sin(_Time * 100) * 0.05, 0, 100);
			//float4 screenPos;
			c.rgb = s.Albedo * _LightColor0.rgb * clamp(ceil(light * _LightSteps + rand(screenPos)) / _LightSteps, 0, 1);
			//c.rgb = s.Albedo * _LightColor0.rgb * clamp(ceil(lightSin * _LightSteps + rand(screenPos)) / _LightSteps, 0, 1);
			c.a = s.Alpha;
			return c;
		}



		struct Input {
			float2 uv_MainTex;
			float4 screenPos;
		};


		void surf(Input IN, inout SurfaceOutputCustom o) {
			float noise = _SparkleBrightness * (1 / (1 - _SparkleCutoff)) * clamp(rand(IN.screenPos) - _SparkleCutoff, 0, 1);
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color * (1 + noise);
		}
		ENDCG
		}
			Fallback "Diffuse"
}