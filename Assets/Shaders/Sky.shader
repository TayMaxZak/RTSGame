Shader "Unlit/Sky"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		//[Toggle] _UseEnvironment("Use Scene Ambient Colors", Float) = 0
		_MainTex ("Texture", 2D) = "white" {}
		_Mult("Multiplier", Range(0,4)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Off
		Fog{ Mode Off }
		LOD 100

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
				half3 worldNormal : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			half _Mult;
			fixed4 _Color;
			
			v2f vert (appdata v, float3 normal : NORMAL)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(normal);
				return o;
			}

			half3 ac(v2f s)
			{
				half3 ac = 0;
				half height = clamp(dot(-s.worldNormal, float3(0, 1, 0)) + 1, 0, 1) * 0.5 + clamp(dot(-s.worldNormal, float3(0, 1, 0)) * 0.5, 0, 1);
				ac = height > 0.5 ? lerp(unity_AmbientEquator, unity_AmbientSky, abs(height - 0.5) * 2) : lerp(unity_AmbientEquator, unity_AmbientGround, abs(height - 0.5) * 2);

				return clamp(ac * 0.5, 0, 1);
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 c = tex2D(_MainTex, i.uv);
				c *= _Color;
				c *= _Mult;
				c.rgb *= 1;
				c.rgb *= ac(i);
				return c;
			}
			ENDCG
		}
	}
}
