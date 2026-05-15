
Shader "Hidden/Atlas/AtlasScatterChannelMerge"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Mask("Texture", 2D) = "white" {}
        _Height("Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Pass
        {
            Blend One One

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            int _ChannelIndex;

            float4 frag(v2f i) : SV_Target
            {

                return float4(tex2D(_MainTex,i.uv)[_ChannelIndex],0,0,1);

            }

            ENDCG
        }
    }
}
