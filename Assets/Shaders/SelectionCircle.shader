Shader "Custom/SelectionCircle"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Mult("Mult", Range(0,12)) = 1
		_ForegroundMask("Mask", 2D) = "white" {}
		_Cutoff1("Cutoff1", Range(0,1)) = 0.5
		_Cutoff2("Cutoff2", Range(0,1)) = 0.5
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Cull Off
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
			};

			sampler2D _ForegroundMask;
			float4 _ForegroundMask_ST;
			half _Cutoff1;
			half _Cutoff2;
			half _Mult;
			fixed4 _Color;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _ForegroundMask);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_ForegroundMask, i.uv);
				clip(1 - (col.r + _Cutoff1));
				clip(col.r - _Cutoff2);
				col = _Color * _Mult;
				return col;
			}
			ENDCG
		}
	}
}
