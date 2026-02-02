Shader "Custom/StarFlare"
{
    Properties
    {
        _StarTex ("Star Texture", 2D) = "white" {}
        _Strength ("Falloff Strength", Float) = 3.0
        _Steps ("Flare Steps", Int) = 12
        _Gain ("Flare Gain", Float) = 1.0
        _UseDiagonals ("Use Diagonals", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _StarTex;
            float4 _StarTex_TexelSize;
            float _Strength;
            int _Steps;
            float _Gain;
            float _UseDiagonals;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float baseStar = tex2D(_StarTex, i.uv).r;
                float acc = 0.0;

                [loop]
                for (int s = 1; s <= _Steps; s++)
                {
                    float w = 1.0 / (s * _Strength + 1.0);

                    float2 offX = float2(_StarTex_TexelSize.x * s, 0);
                    float2 offY = float2(0, _StarTex_TexelSize.y * s);

                    acc += w * tex2D(_StarTex, i.uv + offX).r;
                    acc += w * tex2D(_StarTex, i.uv - offX).r;
                    acc += w * tex2D(_StarTex, i.uv + offY).r;
                    acc += w * tex2D(_StarTex, i.uv - offY).r;

                    if (_UseDiagonals > 0.5)
                    {
                        float2 od = float2(offX.x, offY.y);
                        acc += w * tex2D(_StarTex, i.uv + od).r;
                        acc += w * tex2D(_StarTex, i.uv - od).r;
                        acc += w * tex2D(_StarTex, i.uv + float2( od.x, -od.y)).r;
                        acc += w * tex2D(_StarTex, i.uv + float2(-od.x,  od.y)).r;
                    }
                }

                float flare = saturate(baseStar + acc * _Gain);
                return float4(1, 0, 0, 1.0);
            }
            ENDHLSL
        }
    }
}
