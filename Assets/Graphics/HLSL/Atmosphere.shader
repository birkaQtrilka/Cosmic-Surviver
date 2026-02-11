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
            
            int _PlanetCount;
            float4 _PlanetPositions[8];
            float4 _PlanetData[8];
            float3 _AtmosphereColor;
            float3 _CameraPosition;

            float3 GetCameraRayDirection(float2 uv)
            {
                float3 nearPoint = ComputeWorldSpacePosition(uv, 0.0, UNITY_MATRIX_I_VP);
                float3 rayDir = normalize(nearPoint - _CameraPosition);
                return rayDir;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 ray = GetCameraRayDirection(input.texcoord);
                // d= ray
                // l = planetToCam
                float3 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                float3 atmosphereColor = _AtmosphereColor;
                float4 result = float4(sceneColor,1);
                for(int i = 0; i < 8; i++) {
                    float radius = _PlanetData[i].g;
                    float3 planetToCam = _CameraPosition - _PlanetPositions[i].rgb;
                    float LD = dot(planetToCam, ray);
                    
                    if(LD > 0) continue; // prevents drawing fogs in opposite direction

                    float discriminant = pow(LD, 2) - dot(planetToCam, planetToCam) + radius * radius;

                    if(discriminant > 0) {
                        discriminant *= 4;// earlier was skipped because we didn't need it to determine intersection
                        float diff = sqrt(discriminant);
                        // normalizing difference
                        diff = diff/radius;
                        result += float4(atmosphereColor, 1) * diff;                    
                    }
                }



                return result;
            }

            ENDHLSL
        }
    }
}