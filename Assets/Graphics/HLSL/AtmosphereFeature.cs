using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AtmosphereFeature : ScriptableRendererFeature
{
    private struct PlanetData
    {
        public Vector3 color;
        public float outerRadius;
        public Vector3 darkColor;
        public float atmosphereIntensity;
        public Vector3 position;
        public float padding1;
        public Vector4 padding2;

        public PlanetData(Vector3 col, Vector3 darkCol, Vector3 pos, float rad, float atmIntensity)
        {
            color = col;
            darkColor = darkCol;
            outerRadius = rad;
            atmosphereIntensity = atmIntensity;
            position = pos;
            padding1 = 0;
            padding2 = new();
        }
    }

    class AtmospherePass : ScriptableRenderPass
    {
        private readonly Material material;
        private RTHandle tempTexture;

        // For storing planet data
        private readonly PlanetData[] planetData = new PlanetData[8];
        private Vector3 lightSourcePos;

        private GraphicsBuffer planetBuffer;
        private int planetCount;

        public AtmospherePass(Material mat)
        {
            material = mat;
            planetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 8, 64);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, name: "_AtmosphereTempTex");

            GatherPlanetData();
        }

        private void GatherPlanetData()
        {
            List<Planet> planets = Planet.ActivePlanets;

            planetCount = 0;
            if (planets == null) return;
            for (int i = 0; i < planets.Count; i++)
            {
                Planet planet = planets[i];
                if (planet == null) continue;
                if (planet.isLightSource) lightSourcePos = planet.transform.position;
                if (planet.atmosphereSettings == null) continue;

                planetData[i] = new PlanetData(
                    ColorToVector(planet.atmosphereSettings.color),
                    ColorToVector(planet.atmosphereSettings.darkColor),
                    planet.transform.position,
                    planet.shapeSettings.planetRadius + planet.atmosphereSettings.thickness,
                    planet.atmosphereSettings.intensity
                );
                planetCount++;
            }
            // Only set data if we have planets, though setting 0 is usually safe
            if (planetBuffer != null && planetBuffer.IsValid())
                planetBuffer.SetData(planetData);
        }

        Vector3 ColorToVector(Color color)
        {
            return new Vector3(color.r, color.g, color.b);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || planetBuffer == null || !planetBuffer.IsValid()) return;

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get("Atmosphere");

            cmd.SetGlobalInt("_PlanetCount", planetCount);
            cmd.SetGlobalBuffer("_PlanetDataBuffer", planetBuffer);
            cmd.SetGlobalVector("_LightPosition", lightSourcePos);
            cmd.SetGlobalVector("_CameraPosition", renderingData.cameraData.camera.transform.position);

            Blitter.BlitCameraTexture(cmd, source, tempTexture, material, 0);
            Blitter.BlitCameraTexture(cmd, tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTexture?.Release();

            if (planetBuffer != null)
            {
                planetBuffer.Release();
                planetBuffer = null;
            }
        }
    }

    public Shader shader;
    private Material material;
    private AtmospherePass pass;

    public override void Create()
    {
        pass?.Dispose();

        if (material != null)
        {
            CoreUtils.Destroy(material);
            material = null;
        }

        if (shader != null)
            material = CoreUtils.CreateEngineMaterial(shader);

        pass = new AtmospherePass(material)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null) return;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        // This handles cleanup when the Feature is disabled or the game stops
        pass?.Dispose();
        CoreUtils.Destroy(material);
    }
}