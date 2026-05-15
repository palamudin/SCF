
Shader "Hidden/Atlas/AtlasThumbnail"
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
            Blend One OneMinusSrcAlpha

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
            sampler2D _Mask;
            sampler2D _Height;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 mask = tex2D(_Mask, i.uv);

                float h = tex2D(_Height, i.uv).r; 

                float f = (h % 0.2) * (1 / 0.2);

                float l = 0;
                
                if (f > 0.95) {
                
                    l = 1;
                
                }
                
                return lerp(col*col*col , lerp(fixed4(0,0,h,1),fixed4(h,0,0,1),h),l) * mask.r;
            }

            ENDCG
        }
    }
}
