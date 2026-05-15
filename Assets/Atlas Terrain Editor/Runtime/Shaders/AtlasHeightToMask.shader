Shader "Hidden/Atlas/AtlasHeightToMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            float4 _MainTex_ST;
            int _Mode;
            float _Size;
            float _Fade;
            float _FromHeight;
            float _ToHeight;
            float _Opacity;
            float _MinHeight;
            float _MaxHeight;

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

                float edgeOffset = size;

                float edgeDistance = edgeOffset - (fade*0.5);

                return clamp(InverseLerp(edgeOffset, edgeDistance, f), 0, 1) * clamp(InverseLerp(1.0 - edgeOffset, 1.0 - edgeDistance, f), 0, 1);

            }

            float2 DistanceFromCenter(float u, float v) {

                return clamp(length((float2(u,v) - float2(0.5,0.5))*2.0),0,1);

            }

            float Blob(float u, float v, float opacity,float size, float fade) {

                return lerp(0, opacity, SCurve(RangeFade(DistanceFromCenter(u, v), size, fade)));

            }

            float Edge(float u, float v, float opacity, float size, float fade) {

                return lerp(0,opacity, SCurve(EdgeFade(u, size, fade)) * SCurve(EdgeFade(v, size, fade)));

            }


            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target {

                float4 color = tex2D(_MainTex, i.uv);

                float opacity = InverseLerp(_MinHeight, _MaxHeight, color.r);

                if (_Mode == 1) {

                    //blob

                    opacity = Blob(i.uv.x, i.uv.y, _Opacity,_Size,_Fade);

                }
                else if (_Mode == 2) {

                    //Edge

                    opacity = Edge(i.uv.x, i.uv.y, _Opacity, _Size, _Fade);

                }
                else if (_Mode == 3) {

                    //blob and height

                    opacity = Blob(i.uv.x, i.uv.y, _Opacity, _Size, _Fade) * clamp(InverseLerp(_FromHeight, _ToHeight, opacity), 0, 1);

                }
                else if (_Mode == 4) {

                    //edge and height

                    opacity = Edge(i.uv.x, i.uv.y, _Opacity, _Size, _Fade) * clamp(InverseLerp(_FromHeight, _ToHeight, opacity), 0, 1);

                }
                else {

                    //height

                    opacity = clamp(InverseLerp(_FromHeight, _ToHeight, opacity),0,1);

                }

                return float4(opacity, opacity, opacity,1);

            }

            ENDCG
        }
    }
}
