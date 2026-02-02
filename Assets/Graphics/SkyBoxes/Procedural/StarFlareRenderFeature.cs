using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StarFlareRenderFeature : ScriptableRendererFeature
{
    class StarFlarePass : ScriptableRenderPass
    {
        private Material _material;
        private RTHandle _tempRT;

        public StarFlarePass(Material material)
        {
            _material = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("Star Flare Pass");

            // Get the camera color target safely inside Execute
            RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Allocate a temporary RT the same size as camera
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            _tempRT = RTHandles.Alloc(
                desc.width, desc.height,
                colorFormat: desc.graphicsFormat,
                filterMode: FilterMode.Bilinear,
                useDynamicScale: true
            );

            // First pass: camera -> temp
            CoreUtils.SetRenderTarget(cmd, _tempRT, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, cameraTarget);

            // Second pass: temp -> camera
            CoreUtils.SetRenderTarget(cmd, cameraTarget, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, _tempRT);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (_tempRT != null)
            {
                _tempRT.Release();
                _tempRT = null;
            }
        }
    }

    [System.Serializable]
    public class Settings
    {
        public Material flareMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public Settings settings = new Settings();
    private StarFlarePass _pass;

    public override void Create()
    {
        if (settings.flareMaterial != null)
        {
            _pass = new StarFlarePass(settings.flareMaterial)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass != null)
        {
            renderer.EnqueuePass(_pass);
        }
    }
}
