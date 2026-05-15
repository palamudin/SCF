Shader "Hidden/Atlas/AtlasRoadMask"
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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float _Power;

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


				float c = tex2D(_MainTex, i.uv).r;

#if UNITY_COLORSPACE_GAMMA 
				c = pow(c, 2.2f);
#endif

				c = clamp(pow(c, _Power),0,1);

				return float4(c.rrr, 0);
				
			}

			ENDCG
		}
	}
}
