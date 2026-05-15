Shader "Hidden/Atlas/AtlasMask"
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
				float2 fade: TEXCOORD2;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float2 fade: TEXCOORD2;
			};

			sampler2D _HeightTex;
			sampler2D _MainTex;
			float _Power;
			float _EdgeFade;
			float _EdgeFadePower;
			int _Mode;
			float _Opacity;
			float _Fade;
			float _Size;
			float _Invert;
			float _FromHeight;
			float _ToHeight;

			float EdgeFade(float2 uv) {
			
				//uv = (uv - 0.5) * 2;
				//
				//return clamp(pow(clamp(1 - length(uv)*_EdgeFade, 0, 1), 0.2)*1.1,0,1);

				uv.x = frac(uv.x);
				uv.y = frac(uv.y);

				if (_EdgeFade <= 0) {

					return 1.0;

				} else {

					float f = (1.0 - abs((uv.x - 0.5) * 2.0)) * (1.0 - abs((uv.y - 0.5) * 2.0));

					f = pow(f, 2);

					return clamp(f * 100.0 * clamp(1.0 - _EdgeFade, 0.0, 1.0), 0.0, 1.0);

				}

			}

			float InverseLerp(float4 a, float4 b, float4 f) {

				return (f - a) / (b - a);

			}

			float SCurve(float f) {

				return  (f * f) / (2.0f * ((f * f) - f) + 1.0f);

			}

			float RangeFade(float f, float size, float fade) {

				float edgeOffset = size;

				float edgeDistance = edgeOffset - (fade);

				return clamp(InverseLerp(edgeOffset, edgeDistance, f), 0, 1);

			}

			float EdgeFade(float f, float size, float fade) {

				f = frac(f);

				float edgeOffset = size;

				float edgeDistance = edgeOffset - (fade * 0.5);

				return clamp(InverseLerp(edgeOffset, edgeDistance, f), 0, 1) * clamp(InverseLerp(1.0 - edgeOffset, 1.0 - edgeDistance, f), 0, 1);

			}

			float2 DistanceFromCenter(float u, float v) {

				u = frac(u);
				v = frac(v);

				return clamp(length((float2(u, v) - float2(0.5, 0.5)) * 2.0), 0, 1);

			}

			float Blob(float u, float v, float opacity, float size, float fade) {

				return lerp(0, opacity, SCurve(RangeFade(DistanceFromCenter(u, v), size, fade)));

			}

			float Edge(float u, float v, float opacity, float size, float fade) {

				return lerp(0, opacity, SCurve(EdgeFade(u, size, fade)) * SCurve(EdgeFade(v, size, fade)));

			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = float4(v.vertex.xy * 2 - 1, 0, 1);
				o.vertex.y *= -1;
				o.uv = v.uv;
				o.fade = v.fade;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{

				float opacity = tex2D(_MainTex, i.uv).r * i.fade.r;

				if (_Invert > 0) {

					opacity = 1.0 - opacity;

				}

				if (_Mode == 0) {

#if UNITY_COLORSPACE_GAMMA 
					opacity = pow(opacity, 2.2f);
#endif

					//simple

					opacity = clamp(pow(opacity, _Power), 0.0, 1.0) * EdgeFade(i.uv);

				}
				else if (_Mode == 1) {
					
					//height

					float height = tex2D(_HeightTex, i.uv).r;

					opacity *= clamp(InverseLerp(_FromHeight, _ToHeight, height), 0, 1);

					opacity = clamp(pow(opacity, _Power), 0, 1);

				}
				else if (_Mode == 2) {

					//blob

					opacity *= Blob(i.uv.x, i.uv.y, _Opacity, _Size, _Fade);

					opacity = clamp(pow(opacity, _Power), 0, 1);

				}
				else if (_Mode == 3) {

					//edge

					opacity *= Edge(i.uv.x, i.uv.y, _Opacity, _Size, _Fade);

					opacity = clamp(pow(opacity, _Power), 0, 1);

				}
				else if (_Mode == 4) {

					//height and blob

					float height = tex2D(_HeightTex, i.uv).r;

					opacity *= clamp(InverseLerp(_FromHeight, _ToHeight, height), 0, 1) * Blob(i.uv.x, i.uv.y, _Opacity, _Size, _Fade);

					opacity = clamp(pow(opacity, _Power), 0, 1);

				}
				else {

					//height and edge

					float height = tex2D(_HeightTex, i.uv).r;

					opacity *= clamp(InverseLerp(_FromHeight, _ToHeight, height), 0, 1) * Edge(i.uv.x, i.uv.y, _Opacity, _Size, _Fade);

					opacity = clamp(pow(opacity, _Power), 0, 1);

				}

				return float4(opacity.rrr, 0);

			}

			ENDCG
		}
	}
}
