using ProjectL.Render.Shader.PostProcessing.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Render.Shader.PostProcessing.FadeInOut
{
    public sealed class FadeInOutFeature : BaseRenderFeature<FadeInOutFeature.FadeInOutPass>
    {
        [SerializeField] private ComputeShader FadeInOutShader;
        [SerializeField] private FadeInOutSettings Settings = new();

        public sealed class FadeInOutPass : ScriptableRenderPass, System.IDisposable
        {
            private sealed class PassData
            {
                public TextureHandle ActiveTexture;
                public TextureHandle Result;
                public TextureHandle FadeTexture;
                public ComputeShader Shader;
                public int Kernel;
                public float FadeValue;
                public Color FadeColor;
                public int Width;
                public int Height;
            }

            private readonly ComputeShader _shader;
            private readonly FadeInOutSettings _settings;
            private readonly int _kernel;
            private Texture2D _fadeTexture;
            private RTHandle _fadeTextureHandle;

            public FadeInOutPass(ComputeShader shader, FadeInOutSettings settings)
            {
                _shader = shader;
                _settings = settings;
                _kernel = shader != null && shader.HasKernel("CSMain")
                    ? shader.FindKernel("CSMain")
                    : -1;
                requiresIntermediateTexture = true;
            }

            public bool IsReady => _shader != null && _kernel >= 0 && _settings.FadeTexture != null;

            public void Dispose()
            {
                _fadeTextureHandle?.Release();
                _fadeTextureHandle = null;
                _fadeTexture = null;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (!IsReady || resourceData.isActiveTargetBackBuffer)
                    return;

                UpdateFadeTextureHandle();

                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.enableRandomWrite = true;

                TextureHandle result = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    descriptor,
                    "_FadeInOutResult",
                    false);

                using (var builder = renderGraph.AddComputePass<PassData>("Fade In Out", out PassData passData))
                {
                    passData.ActiveTexture = resourceData.activeColorTexture;
                    passData.Result = result;
                    passData.FadeTexture = renderGraph.ImportTexture(_fadeTextureHandle);
                    passData.Shader = _shader;
                    passData.Kernel = _kernel;
                    passData.FadeValue = _settings.FadeValue;
                    passData.FadeColor = _settings.FadeColor;
                    passData.Width = descriptor.width;
                    passData.Height = descriptor.height;

                    builder.UseTexture(passData.ActiveTexture, AccessFlags.Read);
                    builder.UseTexture(passData.FadeTexture, AccessFlags.Read);
                    builder.UseTexture(passData.Result, AccessFlags.Write);
                    builder.SetRenderFunc(static (PassData data, ComputeGraphContext context) =>
                    {
                        ExecutePass(context, data);
                    });
                }

                renderGraph.AddCopyPass(result, resourceData.activeColorTexture);
            }

            private static void ExecutePass(ComputeGraphContext context, PassData data)
            {
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_ActiveTexture", data.ActiveTexture);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_FadeTexture", data.FadeTexture);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_Result", data.Result);
                context.cmd.SetComputeFloatParam(data.Shader, "_FadeValue", data.FadeValue);
                context.cmd.SetComputeVectorParam(data.Shader, "_FadeColor", data.FadeColor);
                context.cmd.SetComputeIntParam(data.Shader, "_Width", data.Width);
                context.cmd.SetComputeIntParam(data.Shader, "_Height", data.Height);
                context.cmd.DispatchCompute(
                    data.Shader,
                    data.Kernel,
                    Mathf.CeilToInt(data.Width / 8f),
                    Mathf.CeilToInt(data.Height / 8f),
                    1);
            }

            private void UpdateFadeTextureHandle()
            {
                if (_fadeTexture == _settings.FadeTexture && _fadeTextureHandle != null)
                    return;

                _fadeTextureHandle?.Release();
                _fadeTexture = _settings.FadeTexture;
                _fadeTextureHandle = RTHandles.Alloc(_fadeTexture);
            }
        }

        public override RenderFeatureType Type => RenderFeatureType.FadeInOut;
        public override ProjectL.Framework.SystemBase.IServiceModule SettingService => Settings;
        protected override RenderPassEvent InjectionPoint => RenderPassEvent.AfterRenderingPostProcessing;

        protected override FadeInOutPass CreatePass() => new(FadeInOutShader, Settings);

        protected override bool PreparePass(FadeInOutPass pass, ref RenderingData renderingData)
        {
            return SystemInfo.supportsComputeShaders && pass.IsReady;
        }
    }
}
