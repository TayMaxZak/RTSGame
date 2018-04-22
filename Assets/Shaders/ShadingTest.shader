Shader "Example/Shading Test" {
	Properties{
		_MainTex("Diffuse Map", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_AOTex("AO Map", 2D) = "white" {}
		_SSSTex("SSS Map", 2D) = "black" {}
		_SSSStrength("SSS Strength", Range(0, 4)) = 0.5
		_SSSColor("SSS Color", Color) = (1,1,1,1)
		

		_AmbientMult("Ambient Multiplier", Range(0,4)) = 1
		_AmbientFallL("Ambient Falloff Light", Range(0,1)) = 0.5
		_AmbientFallD("Ambient Falloff Dark", Range(0,1)) = 0
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf Custom noambient

	sampler2D _MainTex;
	sampler2D _AOTex;
	sampler2D _SSSTex;
	fixed4 _Color;
	fixed4 _SSSColor;
	half _SSSStrength;
	half _AmbientMult;
	half _AmbientFallL;
	half _AmbientFallD;


	float rand(half2 co)
	{
		float a = 12.9898;
		float b = 78.233;
		float c = 43758.5453;
		float dt = dot(co.xy, half2(a, b));
		float sn = fmod(dt, 3.14);
		return frac(sin(sn) * c) * 1;
	}

	struct SurfaceOutputCustom
	{
		fixed3 Albedo;
		fixed3 AmbientOcclusion;
		fixed3 SSS;
		fixed3 Normal;
		fixed3 Emission;
		half Specular;
		fixed Gloss;
		fixed Alpha;

		float2 screenUV;
		float3 viewDir;
		//half lightCounter;
	};

	struct Input {
		float2 uv_MainTex;
		float2 uv_AOTex;
		float2 uv_SSSTex;
		float4 screenPos;
		float3 worldPos;
		float3 viewDir;
	};

	float4 screenPos;

	half3 ac(SurfaceOutputCustom s, half NdotL)
	{
		half3 ac = 0;
		half height = clamp(dot(s.Normal, float3(0, 1, 0)) + 1, 0, 1) * 0.5 + clamp(dot(s.Normal, float3(0, 1, 0)) * 0.5, 0, 1);
		ac = height > 0.5 ? lerp(unity_AmbientEquator, unity_AmbientSky, abs(height - 0.5) * 2) : lerp(unity_AmbientEquator, unity_AmbientGround, abs(height - 0.5) * 2);

		half acMult = (clamp(1 - NdotL * _AmbientFallL, 0, 1) + clamp(1 - -NdotL * _AmbientFallD, 0, 1) - 1);

		ac *= acMult;
		return clamp(ac * _AmbientMult * 0.5, 0, 1);
	}

	half4 LightingCustom(SurfaceOutputCustom s, half3 lightDir, half atten) {
		half NdotL = dot(s.Normal, lightDir);
		half NdotV = dot(s.Normal, s.viewDir);
		half LdotV = dot(lightDir, s.viewDir);
		half4 c;

		half sssDot = LdotV;
		half sssRange = (clamp(sssDot + 1, 0, 1) * 0.5 + clamp(sssDot * 0.677, 0, 1));
		half sssMult = 2 * clamp(1 - (sssRange + 0.5), 0, 1) + 0 * clamp(sssRange - 0.5, 0, 1);		
		half3 sss = sssMult * s.SSS * _SSSColor * _LightColor0.rgb * _SSSStrength;

		half light = NdotL * atten;
		half3 shade = _LightColor0.rgb * clamp(light, 0, 1);

		c.rgb = s.AmbientOcclusion * (shade * s.Albedo + ac(s, NdotL)) + sss;
		c.a = s.Alpha;
		
		return c;
	}





	void surf(Input IN, inout SurfaceOutputCustom o) {
		o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color;
		o.AmbientOcclusion = tex2D(_AOTex, IN.uv_AOTex).rgb;
		o.SSS = tex2D(_SSSTex, IN.uv_SSSTex).rgb;
		o.screenUV = IN.screenPos;

		o.viewDir = IN.viewDir;
	}
	ENDCG
		}
			Fallback "Diffuse"
}