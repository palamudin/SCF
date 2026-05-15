Shader "Hidden/Atlas/AtlasHeight"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
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
				float2 height : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 height : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			int _Mode;
			float _Power;
			float _CutoffMin;
			float _CutoffMax;
			int _Invert;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = float4(v.vertex.xy * 2 - 1, 0, 1);
				o.vertex.y *= -1;
				o.uv = v.uv;
				o.height = v.height;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{

				float height = tex2D(_MainTex, i.uv);

#if UNITY_COLORSPACE_GAMMA 
				height = pow(height, 2.2f);
#endif

				height = clamp(height, _CutoffMin, _CutoffMax);

				if (_Invert > 0) {

					height = 1.0 - height;

				}

				height = pow(height, _Power);

				float worldHeight = 0;

				if (_Mode == 3 || _Mode == 4) {

					//add or sub
					worldHeight = height * (i.height.y - i.height.x);
				}
				else {

					worldHeight = (i.height.x ) + (height * (i.height.y - i.height.x) );
				
				}

				return float4(worldHeight.rrr, 0);

			}
			ENDCG
		}
	}
}
