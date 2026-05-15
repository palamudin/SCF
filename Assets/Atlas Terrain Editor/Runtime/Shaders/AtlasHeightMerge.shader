Shader "Hidden/Atlas/AtlasHeightMerge"
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

			int _Mode;
			float _Opacity;
			float _BlendRatio;

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

				float a = tex2D(_PreMainTex, i.uv).r;
				float b = tex2D(_MainTex, i.uv).r;
				float m = tex2D(_MaskTex, i.uv).r;
				float pm = 1.0 - tex2D(_PersistantMaskTex, i.uv).r;
				
				float v = 0;

				if (_Mode == 0) {
					//blend
					v = lerp(a,b, _BlendRatio);
				}
				else if (_Mode == 1) {
					//min
					v = min(a, b);
				}
				else if (_Mode == 2) {
					//max
					v = max(a, b);
				}
				else if (_Mode == 3) {
					//add
					v = a + b;
				}
				else {
					//sub
					v = a - b;
				}

				return float4(clamp(lerp(a ,v ,(m*pm) * _Opacity).rrr,0.0,0.5),1);

			}

			ENDCG
		}
	}
}
