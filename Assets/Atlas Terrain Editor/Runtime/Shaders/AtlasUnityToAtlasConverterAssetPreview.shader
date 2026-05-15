Shader "Hidden/Atlas/AtlasUnityToAtlasConverterAssetPreview"
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

            sampler2D _HeightMap;
            sampler2D _MaskMap;
            float _StrayationOpacity;

            float2 RotateUV(float2 uv, float rotation)
            {

                float2 center = float2(0.5, 0.5);

                rotation = rotation * (3.1415926f / 180.0f);
                uv -= center;
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rMatrix = float2x2(c, -s, s, c);
                rMatrix *= 0.5;
                rMatrix += 0.5;
                rMatrix = rMatrix * 2 - 1;
                uv.xy = mul(uv.xy, rMatrix);
                uv += center;

                return uv;

            }

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;// TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target {

                float4 heightmapColor = tex2D(_HeightMap, i.uv);

                float4 maskmapColor = tex2D(_MaskMap, i.uv);

                float height = heightmapColor.r;

                float mask = maskmapColor.r;

                float4 color = float4(0, 0, 0, 0);

                if (_StrayationOpacity == 0) {

                    color = float4(height , 0, 0, 1);

                }
                else {

                    if (mask == 0) {

                        float2 rotatedUV = RotateUV(i.uv, 45);

                        if (frac(rotatedUV.x * 60) >= _StrayationOpacity * 0.5) {

                            color = float4(0.125, 0, 0, 1);

                        }
                        else {

                            color = float4(0, mask, mask, 1);

                        }

                    }
                    else {

                        color = float4(0, mask, mask, 1);

                    }

                }

                return color;

            }

            ENDCG
        }
    }
}
