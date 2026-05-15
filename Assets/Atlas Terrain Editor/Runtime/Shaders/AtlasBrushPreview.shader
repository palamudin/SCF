Shader "Hidden/Atlas/AtlasBrushPreview"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        LOD 100

        Pass
        {

            Ztest Always
             Blend SrcAlpha OneMinusSrcAlpha


            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
            float _Opacity;
            float _Invert;
            float _Rotation;

            float2 Rotate(float2 uv, float rotation) {

                rotation = rotation * (3.1415926f / 180.0f);
                uv -= float2(0.5, 0.5);
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rMatrix = float2x2(c, -s, s, c);
                rMatrix *= 0.5;
                rMatrix += 0.5;
                rMatrix = rMatrix * 2 - 1;
                uv.xy = mul(uv.xy, rMatrix);
                uv += float2(0.5, 0.5);

                return uv;

            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {

                float opacity = _Opacity * 0.75;

                float2 ruv = Rotate(i.uv, _Rotation);

                float tex = tex2D(_MainTex, ruv).r;

                if (ruv.x < 0 || ruv.y < 0 || ruv.x > 1 || ruv.y > 1) {
                    tex = 0;
                }

                float edge = tex > 0.05 && tex < 0.075 ? 1 : 0;

                float o = edge + (tex * opacity);

                float3 col = float3(o, o * 0.5, 0);

                float3 invCol = float3(col.b,col.g, col.r);

                col = lerp(col, invCol, _Invert);

                return float4(col.rgb,o);
            }
            ENDCG
        }
    }
}
