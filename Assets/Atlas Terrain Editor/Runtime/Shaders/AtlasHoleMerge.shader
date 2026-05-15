Shader "Hidden/Atlas/AtlasHoleMerge"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
		_PreMainTex("Texture", 2D) = "black" {}
		_MaskTex("Texture", 2D) = "black" {}
		_PersistantMaskTex("Texture", 2D) = "black" {}
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
			sampler2D _PreMainTex;
			sampler2D _MaskTex;
			sampler2D _PersistantMaskTex;

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

				float4 a = tex2D(_PreMainTex, i.uv);
				float4 b = tex2D(_MainTex, i.uv);
				float4 m = tex2D(_MaskTex, i.uv);
				float pm = 1.0 - tex2D(_PersistantMaskTex, i.uv).r;
				
				float f = b.r * m.r * pm;

				if (f > 0.5) {
					f = 1;
				}
				else {
					f = 0;
				}

				float4 r = float4(a.r-f, 0, 0, 1);

				return r;
			}

			ENDCG
		}
	}
}
