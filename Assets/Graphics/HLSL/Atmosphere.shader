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

            struct PlanetData
            {
                float3 color;
                float outerRadius;
                float3 darkColor;
                float atmosphereIntensity;
                float3 position;
                float padding1;
                float4 padding2;
            };

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            
            int _PlanetCount;
            float3 _CameraPosition;
            float3 _LightPosition;
            StructuredBuffer<PlanetData> _PlanetDataBuffer;

            float3 GetCameraRayDirection(float2 uv)
            {
                float3 nearPoint = ComputeWorldSpacePosition(uv, 0.0, UNITY_MATRIX_I_VP);
                float3 rayDir = normalize(nearPoint - _CameraPosition);
                return rayDir;
            }

            float inverseLerp(float a, float b, float v)
            {
                return (v - a) / (b - a);
            }

            float3 DoAtmosphereOnPlanet(PlanetData planetData, float3 ray, float linearDepth) 
            {
                float3 planetToCam = _CameraPosition - planetData.position;
                // d = ray
                // l = planetToCam
                float LD = dot(planetToCam, ray);
                // using quadratic formula
                float discriminant = pow(LD, 2) - dot(planetToCam, planetToCam) + pow(planetData.outerRadius, 2);

                if(discriminant <= 0) return float4(0,0,0,0);
                // earlier was skipped multiplying by 4 because we didn't need it to determine the exact intersection
                float sqrtDiscriminant = sqrt(4 * discriminant);
                float b = -dot(planetToCam, ray);
                float farIntersection = b + sqrtDiscriminant;
                float nearIntersection = b - sqrtDiscriminant;

                nearIntersection = max(0, nearIntersection);
                farIntersection = min(farIntersection, linearDepth);
                
                float diff = (farIntersection-nearIntersection);
                diff = diff * planetData.atmosphereIntensity;
                
                if(diff <= 0) return float4(0,0,0,0);
                
                float3 fragWorldPos = _CameraPosition + ray * farIntersection;
                float3 sphereNormal = normalize(fragWorldPos - planetData.position);
                float3 lightToFragDirection = normalize(planetData.position - _LightPosition);

                // return float4(planetData.color, 1) * diff ;
                // inverseLerp(-1,1, dot(sphereNormal, lightToFragDirection))
                float light = max(0, dot(sphereNormal, lightToFragDirection));
                float3 finalColor = light * planetData.darkColor + (1-light) * planetData.color;

                return float4(finalColor, 1) * diff*(diff) ;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 ray = GetCameraRayDirection(input.texcoord);
                
                float3 result = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;
                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).r;
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);

                for(int i = 0; i < 8; i++) {
                    result += DoAtmosphereOnPlanet(_PlanetDataBuffer[i], ray, linearDepth);
                }
                return float4(result,0.0);
            }

            ENDHLSL
        }
    }
}