Shader "Hidden/Atlas/AtlasOtherMask"
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
			int _InputChannelIndex;
			float _Power;
			float _CutoffMin;
			float _CutoffMax;
			int _Invert;
			float _Offset;
			float _Multiplier;

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

				float4 final = float4(0, 0, 0, 0);

				float c = tex2D(_MainTex,i.uv)[_InputChannelIndex];

				c = c + _Offset;

				c = pow(c, _Power);

				c = c * _Multiplier;

				c = clamp(c, _CutoffMin, _CutoffMax);


				if (_Invert > 0) {

					c = 1.0 - c;

				}

				final.r = c;

				final.r = clamp(0, 1, final.r);

				return final;
				
			}

			ENDCG
		}
	}
}
