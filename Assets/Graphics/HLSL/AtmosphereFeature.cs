using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AtmosphereFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public LayerMask planetLayer = -1; // Which layer planets are on
        public Color atmosphereColor = new Color(0.4f, 0.6f, 1.0f);
        public float atmosphereThickness = 100f;
        public float atmosphereIntensity = 1f;
    }

    public Settings settings = new Settings();

    class AtmospherePass : ScriptableRenderPass
    {
        private readonly Material material;
        private RTHandle tempTexture;
        private Settings settings;

        // For storing planet data
        private Vector4[] planetPositions = new Vector4[8]; // Max 8 planets
        private Vector4[] planetData = new Vector4[8]; // radius, atmosphere thickness, etc.
        private int planetCount;

        public AtmospherePass(Material mat, Settings settings)
        {
            material = mat;
            this.settings = settings;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, descriptor, name: "_AtmosphereTempTex");

            // Find all planets in the scene
            GatherPlanetData();
        }

        private void GatherPlanetData()
        {
            // Find all objects with a specific component or tag
            Planet[] planets = Object.FindObjectsOfType<Planet>();
            planetCount = Mathf.Min(planets.Length, 8);

            for (int i = 0; i < planetCount; i++)
            {
                // Position (xyz) and radius (w)
                planetPositions[i] = new Vector4(
                    planets[i].transform.position.x,
                    planets[i].transform.position.y,
                    planets[i].transform.position.z,
                    planets[i].shapeSettings.planetRadius
                );

                // Additional data: atmosphere start, atmosphere end, intensity, (unused)
                planetData[i] = new Vector4(
                    planets[i].shapeSettings.planetRadius, // Inner radius
                    planets[i].shapeSettings.planetRadius + settings.atmosphereThickness, // Outer radius
                    settings.atmosphereIntensity,
                    0
                );
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get("Atmosphere");

            // Pass data to shader
            cmd.SetGlobalInt("_PlanetCount", planetCount);
            cmd.SetGlobalVectorArray("_PlanetPositions", planetPositions);
            cmd.SetGlobalVectorArray("_PlanetData", planetData);
            cmd.SetGlobalColor("_AtmosphereColor", settings.atmosphereColor);

            // Also pass camera position for view direction calculations
            cmd.SetGlobalVector("_CameraPosition", renderingData.cameraData.camera.transform.position);

            Blitter.BlitCameraTexture(cmd, source, tempTexture, material, 0);
            Blitter.BlitCameraTexture(cmd, tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTexture?.Release();
        }
    }

    public Shader shader;
    private Material material;
    private AtmospherePass pass;

    public override void Create()
    {
        if (shader != null)
            material = CoreUtils.CreateEngineMaterial(shader);

        pass = new AtmospherePass(material, settings)
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
        pass?.Dispose();
        CoreUtils.Destroy(material);
    }
}