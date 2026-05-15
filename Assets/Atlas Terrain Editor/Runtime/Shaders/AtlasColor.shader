Shader "Hidden/Atlas/AtlasColor"
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
			#include "ColorChanger.cginc"

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
			float _Brightness;
			float _Contrast;
			float _Saturation;
			float _Hue;

			float4 _Color;
			float _Selected;
			

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

				float3 color = tex2D(_MainTex, i.uv).rgb;

#if UNITY_COLORSPACE_GAMMA 
				color = pow(color, 2.2f);
#endif

				float4 finalColor = ChangeColor(float4(color.rgb, 0), float4(_Hue, _Saturation, _Brightness, _Contrast));

				return lerp(finalColor, _Color, _Selected);

			}

			ENDCG
		}
	}
}
