Shader "Hidden/Atlas/AtlasSplatSum"
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

			sampler2D _Splat1;
			sampler2D _Splat2;
			sampler2D _Splat3;
			sampler2D _Splat4;

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

				float total = 0.0;

				int ii = 0;

				for ( ii = 0; ii < 4; ii++) {

					total += tex2D(_Splat1, i.uv)[ii];

				}

				for ( ii = 0; ii < 4; ii++) {

					total += tex2D(_Splat2, i.uv)[ii];

				}

				for ( ii = 0; ii < 4; ii++) {

					total += tex2D(_Splat3, i.uv)[ii];

				}

				for ( ii = 0; ii < 4; ii++) {

					total += tex2D(_Splat4, i.uv)[ii];

				}

				return total.rrrr;

			}

			ENDCG
		}
	}
}
