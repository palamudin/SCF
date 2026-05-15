Shader "Hidden/Atlas/AtlasSplatNormalize"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	SubShader
	{

		ZTest Always
		ZWrite Off
		Cull Off

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

			sampler2D _MainTex;
			sampler2D _Sum;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = float4(v.vertex.xy * 2 - 1, 0, 1);
				o.vertex.y *= -1;
				o.uv = v.uv;
				return o;
			}
			
			float4 frag(v2f i) : SV_Target
			{

				float sum = tex2D(_Sum, i.uv).r;

				float4 color = tex2D(_MainTex, i.uv).rgba;

				if (sum == 0.0) {

					color.r = 1;

				} else {

					color /= sum;

				}

				return color;

			}

			ENDCG
		}
	}
}
