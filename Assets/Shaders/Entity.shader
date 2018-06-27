Shader "Custom/Entity" {
	Properties{
		_MainTex("Diffuse Map", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		//_NoiseSteps("Dissolve Resolution", Range(1, 1000)) = 200

		_SSSTex("SSS Map", 2D) = "black" {}
		_SSSStrength("SSS Strength", Range(0, 4)) = 0.5
		_SSSColor("SSS Color", Color) = (1,1,1,1)
		
		_AOTex("AO Map", 2D) = "white" {}
		_AmbientMult("Ambient Multiplier", Range(0,4)) = 1
		_AmbientFallL("Ambient Falloff Light", Range(0,1)) = 0.5
		_AmbientFallD("Ambient Falloff Dark", Range(0,1)) = 0
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf Custom noambient
//#pragma target 3.0
//#pragma debug
	sampler2D _MainTex;
	fixed4 _Color;

	sampler2D _SSSTex;
	fixed4 _SSSColor;
	half _SSSStrength;

	sampler2D _AOTex;
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
		//float2 uv_AOTex;
		//float2 uv_SSSTex;
		float4 screenPos;
		float3 worldPos;
		float3 viewDir;
		//float4 _ScreenParams;
	};

	float4 screenPos;

	half3 ac(SurfaceOutputCustom s, half NdotL)
	{
		half3 ac = 0;
		half height = clamp(dot(s.Normal, float3(0, 1, 0)) + 1, 0, 1) * 0.5 + clamp(dot(s.Normal, float3(0, 1, 0)) * 0.5, 0, 1);
		ac = height > 0.5 ? lerp(unity_AmbientEquator, unity_AmbientSky, abs(height - 0.5) * 2) : lerp(unity_AmbientEquator, unity_AmbientGround, abs(height - 0.5) * 2);

		half acMult = (clamp(1 - NdotL * _AmbientFallL, 0, 1) + clamp(1 - -NdotL * _AmbientFallD, 0, 1) - 1);

		ac *= acMult;
		return clamp(ac * _AmbientMult * 0.5, 0, _AmbientMult);
	}

	half3 mod(half3 a, half3 b)
	{
		return clamp(2 * a - ceil(a * b) / b, 0, 1);
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
		half lightMod = 0.5;
		//half minLight = 0;
		//half steps = 5;
		//half stepBlend = 0.0;
		

		//half3 shadeStepped = _LightColor0.rgb * ceil(clamp(light, 0, 1) * steps) / steps;
		//half3 shadeStepped = _LightColor0.rgb * mod(clamp(light, 0, 1) * steps, steps);
		//half3 shadeSmooth = _LightColor0.rgb * clamp(light, 0, 1);
		//half3 shade = shadeStepped * stepBlend + shadeSmooth * (1 - stepBlend);
		//half3 shade = _LightColor0.rgb * clamp(light, 0, 1);

		half3 shade = _LightColor0.rgb * clamp((1 / (1 + lightMod)) * (light + lightMod), 0, 1);

		c.rgb = clamp((s.AmbientOcclusion), 0, 1) * (shade * s.Albedo + ac(s, NdotL) * (s.Albedo + 1) * 0.5) + sss;
		//c.rgb = s.AmbientOcclusion * (shade * s.Albedo + ac(s, NdotL)) + sss;
		//c.rgb += (1 - s.AmbientOcclusion) * _SSSColor * _SSSStrength;

		/*
		half missingAlpha = ((1 - s.Alpha));
		half dissolve = rand(s.screenUV); // Random noise
		clip(1 - (missingAlpha * clamp(dissolve, 0, 1) + missingAlpha));
		*/
		half dissolveAlpha = (1 - s.Alpha);
		half dissolveNoise = rand(s.screenUV);
		half dissolve = dissolveAlpha + ceil(clamp(dissolveNoise - (1 - dissolveAlpha), 0, 1)) + 0.0001;
		dissolve = (1 - dissolve);
		c.a = dissolve;
		clip(c.a);
		c.a = clamp(dissolve, 0, 1);
		
		return c;
	}





	void surf(Input IN, inout SurfaceOutputCustom o) {
		float2 uv = IN.uv_MainTex;
		o.Albedo = tex2D(_MainTex, uv).rgb * _Color;
		o.Alpha = _Color.a;
		//uv = IN.uv_AOTex;
		o.AmbientOcclusion = 1;
		o.AmbientOcclusion = tex2D(_AOTex, uv).rgb;
		o.SSS = tex2D(_SSSTex, uv).rgb;

		o.viewDir = IN.viewDir;

		float4 screenParams = _ScreenParams;
		float2 screenUV = IN.screenPos.xy / (IN.screenPos.w == 0 ? 1 : IN.screenPos.w);
		o.screenUV = screenUV;
		o.screenUV.x = floor(screenUV.x * screenParams.x) / screenParams.x;
		o.screenUV.y = floor(screenUV.y * screenParams.y) / screenParams.y;
	}
	ENDCG
		}
			Fallback "Diffuse"
}