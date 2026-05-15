Shader "Hidden/Atlas/AtlasNormal"
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
            float3 _Size;
            float2 _Resolution;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {

                float2 uv00  = i.uv;
                float2 uv2x  = i.uv + float2(1.0 / _Resolution.x,0);
                float2 uv2y  = i.uv + float2(0,1.0 / _Resolution.y);
                float2 uv2xm = i.uv - float2(1.0 / _Resolution.x, 0);
                float2 uv2ym = i.uv - float2(0, 1.0 / _Resolution.y);

                float height00 = tex2D(_MainTex, uv00).r;
                float height2x = tex2D(_MainTex, uv2x).r;
                float height2y = tex2D(_MainTex, uv2y).r;
                float height2xm = tex2D(_MainTex, uv2xm).r;
                float height2ym = tex2D(_MainTex, uv2ym).r;

                float3 p00 = float3(uv00.x, height00, uv00.y) * _Size;
                float3 p2x = float3(uv2x.x, height2x, uv2x.y) * _Size;
                float3 p2y = float3(uv2y.x, height2y, uv2y.y) * _Size;
                float3 p2xm = float3(uv2xm.x, height2xm, uv2xm.y) * _Size;
                float3 p2ym = float3(uv2ym.x, height2ym, uv2ym.y) * _Size;

                float3 normal = normalize(cross(normalize(p2x - p00), normalize(p2y - p00)));
                
                float3 normalm = normalize(cross(normalize(p2xm - p00), normalize(p2ym - p00)));

                normal = lerp(normal, normalm, 0.5);
                
                normal.z *= -1;

                return float4((1.0 - normal.x) * 0.5, (1.0 - normal.z) * 0.5, (1.0 - normal.y) * 0.5,1);

            }

            ENDCG
        }
    }
}
