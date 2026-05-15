Shader "Hidden/Atlas/AtlasHeightBasedAlteration"
{
	Properties
	{
		_HeightTex("Texture", 2D) = "black" {}
		_PreMainTex("Texture", 2D) = "black" {}
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

			sampler2D _HeightTex;
			sampler2D _PreMainTex;

			float _EdgeBlend;
			int _Mode;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = float4(v.vertex.xy * 2 - 1, 0, 1);
				o.vertex.y *= -1;
				o.uv = v.uv;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{

				float height = tex2D(_HeightTex, i.uv).r;
				float preheight = tex2D(_PreMainTex, i.uv).r;
				
				float v = 0;

				if (_Mode == 1) {

					//min
					v = clamp((preheight -height)*(_EdgeBlend * 500), 0, 1);

				}
				else if (_Mode == 2) {

					//max
					v = clamp((height - preheight)*(_EdgeBlend * 500), 0, 1);

				}

				//v = 1.010118 + (0.004989013 - 1.010118) / pow(1 + (v / 0.5022587), 6.667967);

				return float4(v.rrr,1);

			}

			ENDCG
		}
	}
}
