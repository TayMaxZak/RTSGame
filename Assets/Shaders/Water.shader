Shader "Custom/Water" {
	Properties{
		_DepthTex("Depth Map", 2D) = "black" {}
		_WaveTex("Wave Map", 2D) = "white" {}
		_NormTex("Wave Normals", 2D) = "white" {}
		_SpeedX("Speed X", Range(-8, 8)) = 1
		_SpeedY("Speed Y", Range(-8, 8)) = 1
		_Color("Color", Color) = (1,1,1,1)
		_Opacity("Opacity", Range(0, 1)) = 1
		//_NoiseSteps("Dissolve Resolution", Range(1, 1000)) = 200
		
		_AOTex("AO Map", 2D) = "white" {}
		//_AmbientMult("Ambient Multiplier", Range(0,4)) = 1
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf Custom noambient
//#pragma target 3.0
//#pragma debug
	sampler2D _DepthTex;
	sampler2D _WaveTex;
	sampler2D _NormTex;
	half _SpeedX;
	half _SpeedY;

	fixed4 _Color;
	half _Opacity;

	sampler2D _AOTex;
	//half _AmbientMult;


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
		fixed3 Wave;
		fixed3 Depth;
		fixed3 AmbientOcclusion;
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
		float2 uv_DepthTex;
		float2 uv_WaveTex;
		//float2 uv_SSSTex;
		float4 screenPos;
		float3 worldPos;
		float3 viewDir;
		//float4 _ScreenParams;
	};

	float4 screenPos;

	half3 gradient(SurfaceOutputCustom s, half NdotL, half NdotV, half LdotV, half atten)
	{
		half _AmbientMult = 2.0f;
		half groundMix = 0.5f;
		half ground = 1.0f;

		half3 ambientL = 1;
		half3 ambientD = 1;
		//half height = clamp(dot(s.Normal, float3(0, 1, 0)) + 1, 0, 1) * 0.5 + clamp(dot(s.Normal, float3(0, 1, 0)) * 0.5, 0, 1);
		half height = clamp(NdotV * (1 - groundMix) + ground * groundMix, 0, 1);
		//half heightD = clamp((NdotL + 1 - NdotV) / 2, 0, 1);

		half attenStrength = 0.5f;
		half attenModded = (1 - (1 - atten) * attenStrength);
		half shadow = clamp((attenModded * NdotL), 0, 1);
		//half shadow = 1;

		half3 ambientLight = lerp(0, 1, height);

		//half acMult = (clamp(1 - NdotL * _AmbientFallL, 0, 1) + clamp(1 - -NdotL * _AmbientFallD, 0, 1) - 1);
		//half acMult = (clamp(1 - LdotV * _AmbientFallL, 0, 1) + clamp(1 - -LdotV * _AmbientFallD, 0, 1) - 1);
		half ambientLightMult = shadow;

		ambientLight *= ambientLightMult;
		//ambientLight = ambientL;
		return clamp(ambientLight * _AmbientMult, 0, _AmbientMult);
	}

	half3 ambientLight(half val)
	{
		val = clamp(val, 0, 1);
		return val > 0.5 ? lerp(unity_AmbientEquator, unity_AmbientSky, abs(val - 0.5) * 2) : lerp(unity_AmbientEquator, unity_AmbientGround, abs(val - 0.5) * 2);
	}

	half3 ambientLight2(half val)
	{
		return lerp(unity_AmbientEquator, unity_AmbientSky, abs(val));
	}

	half3 mod(half3 a, half3 b)
	{
		return clamp(2 * a - ceil(a * b) / b, 0, 1);
	}

	half getWave(half NdotV, half waveMix, half waveFadeMix)
	{
		half wave = (1 * NdotV) * waveFadeMix + 1 * (1 - waveFadeMix);
		wave = (min(1, wave * 2.0f));
		wave *= waveMix;
		return wave;
	}

	half4 LightingCustom(SurfaceOutputCustom s, half3 lightDir, half atten) {
		half NdotL = dot(s.Normal, lightDir);
		half NdotV = dot(s.Normal, s.viewDir);
		half LdotV = dot(lightDir, s.viewDir);
		half4 c;

		half light = NdotL * atten;

		//half3 shade = _LightColor0.rgb * clamp((1 / (1 + lightMod)) * (light + lightMod), 0, 1);
		half3 shade = _LightColor0.rgb * clamp(light, 0, 10);

		half wave = getWave(NdotV, 0.2f, 1.0f);
		half wave2 = getWave(NdotV, 1.0f, 0.0f);
		

		half3 color = s.Albedo;

		// Ambient light is mixed in with the albedo
		half grad = gradient(s, NdotL, NdotV, LdotV, atten);
		//half val = grad * (1 * ambAlbedoMix + 1 * (1 - ambAlbedoMix)) * s.Depth;
		half waveMap = 0.4f + s.Wave * 0.9f;
		half val = grad * (1 - wave) + (wave) * (waveMap);
		half3 base = s.Depth * light;
		half baseMix = 0.5f;
		half3 diffuse = ambientLight(val * (1 - baseMix) + base * (baseMix));

		half val2 = grad * (1 - wave2) + (wave2) * (waveMap) * atten;
		half3 base2 = 0;
		half3 prespec = ambientLight(val2 * (1 - baseMix) + base2 * (baseMix));
		//c.rgb = diffuse;
		//c.rgb = prespec;





		half3 reflect = normalize(2 * prespec * s.Normal - lightDir);
		half3 specular = pow(saturate(dot(reflect, s.viewDir)), 20);
		specular *= 0.2f;
		//specular *= 1;
		c.rgb = diffuse + specular;




		/*
		half missingAlpha = ((1 - s.Alpha));
		half dissolve = rand(s.screenUV); // Random noise
		clip(1 - (missingAlpha * clamp(dissolve, 0, 1) + missingAlpha));
		*/




		half dissolveAlpha = (1 - _Opacity);
		half dissolveNoise = rand(s.screenUV);
		half dissolve = dissolveAlpha + ceil(clamp(dissolveNoise - (1 - dissolveAlpha), 0, 1)) + 0.0001;
		dissolve = (1 - dissolve);
		c.a = dissolve;
		clip(c.a);
		c.a = clamp(dissolve, 0, 1);
		



		return c;
	}





	void surf(Input IN, inout SurfaceOutputCustom o) {
		float2 uvDepth = IN.uv_DepthTex;
		float2 uvWave = IN.uv_WaveTex;



		o.Albedo = _Color;
		o.Depth = tex2D(_DepthTex, uvDepth).rgb;
		half blend = 0.5f;

		float waveStages = 5;
		//int iMax = pow(2, waveStages);

		half2 offset = 0;
		offset.x = _Time * _SpeedX / waveStages;
		offset.y = _Time * _SpeedY / waveStages;

		for (half i = 1; i <= waveStages; i++)
		{
			uint integ = round(i + 2);
			o.Wave += tex2D(_WaveTex, (uvWave + ((integ % 2) ? -1.0f : 1.0f) * offset) * i) * (1 / waveStages) * 1.2f;
		}

		//for (half j = 1; j <= waveStages; j++)
		//{
		//	uint integ = round(j + 2);
		//	o.Normal += tex2D(_NormTex, (uvWave + ((integ % 2) ? -1.0f : 1.0f) * offset) * j) * (1 / waveStages) * 1.2f;
		//}
		//o.Normal = tex2D(_NormTex, uvWave);


		o.Alpha = _Color.a;
		//uv = IN.uv_AOTex;
		o.AmbientOcclusion = 1;
		o.AmbientOcclusion = tex2D(_AOTex, uvDepth).rgb;

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