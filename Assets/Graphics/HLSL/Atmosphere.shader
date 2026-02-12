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

            float scatter0(float3 sphereNormal, float3 lightDir)
            {
                float light = saturate(dot(sphereNormal, lightDir));
                return light;
            }

            float scatter1(float3 sphereNormal, float3 lightDir)
            {
                float NdotL = saturate(dot(sphereNormal, lightDir));
                float light = pow(NdotL, 3); 
                return light;
            }
            float scatter2(float3 sphereNormal, float3 lightDir)
            {
                float NdotL = dot(sphereNormal, lightDir);
                float light = smoothstep(-0.3, 1.5, NdotL);
                return light;
            }
            float scatter3(float3 sphereNormal, float3 lightDir)
            {
                float light = dot(sphereNormal, lightDir) * 0.5 + 0.5;
                light = light * light * light; // optional shaping

                return light;
            }
            
            float easeInExpo(float x){
                return x == 0 ? 0 : pow(2, 10 * x - 10);
             }

            float3 DoAtmosphereOnPlanet(PlanetData planetData, float3 ray, float linearDepth) 
            {
                float3 planetToCam = _CameraPosition - planetData.position;
                // b from quadratic formula
                float b = dot(planetToCam, ray);
                // using quadratic formula
                float discriminant = pow(b, 2) - dot(planetToCam, planetToCam) + pow(planetData.outerRadius, 2);

                if(discriminant <= 0) return float4(0,0,0,0);
                // earlier was skipped multiplying by 4 because we didn't need it to determine the exact intersection
                float sqrtDiscriminant = sqrt(discriminant);
                float nearIntersection = -b - sqrtDiscriminant;
                float farIntersection = -b + sqrtDiscriminant;

                nearIntersection = max(0, nearIntersection); // avoids getting the intersection if the hit is behind
                farIntersection = min(farIntersection, linearDepth); // linearDepth is where the ray is hitting the planet
                
                float diff = (farIntersection-nearIntersection);
                diff = diff * planetData.atmosphereIntensity;
                
                if(diff <= 0) return float4(0,0,0,0);
                // return float4(planetData.color, 1) * diff ;
                
                float3 fragWorldPos = _CameraPosition + ray * nearIntersection;
                float3 sphereNormal = normalize(fragWorldPos - planetData.position);
                float3 lightDir = normalize(_LightPosition - planetData.position);
                float light = scatter2(sphereNormal, lightDir); 


                float3 finalColor = light * planetData.color + (1-light) * planetData.darkColor;
                // finalColor
                return float4(finalColor, 1) * easeInExpo( diff);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 ray = GetCameraRayDirection(input.texcoord);
                
                float3 fragSceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;

                float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).r;
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float3 cameraForward = UNITY_MATRIX_V[2].xyz * -1; 
                float rayDepth = linearDepth / dot(ray, cameraForward);

                for(int i = 0; i < 8; i++) {
                    fragSceneColor += DoAtmosphereOnPlanet(_PlanetDataBuffer[i], ray, rayDepth);
                }
                return float4(fragSceneColor,0.0);
            }

            ENDHLSL
        }
    }
}