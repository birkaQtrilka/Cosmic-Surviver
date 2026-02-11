Shader "Custom/Atmosphere"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Atmosphere"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            float3 ReconstructWorldPos(float2 uv)
            {
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                return worldPos;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Use _BlitTexture which is set by Blitter.BlitCameraTexture
                float3 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;

                float3 worldPos = ReconstructWorldPos(input.texcoord);

                float height = length(worldPos);
                float atmosphere = saturate(1.0 - height * 0.0001);

                float3 atmosphereColor = float3(0.4, 0.6, 1.0) * atmosphere;

                return half4(sceneColor + atmosphereColor, 1);
            }

            ENDHLSL
        }
    }
}