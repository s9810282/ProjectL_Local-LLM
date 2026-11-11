using ProjectL.Render.RendererFeatures.Systems.VolumetricLight;
using ProjectL.Render.Shader.PostProcessing.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Render.Shader.PostProcessing.VolmetrickLight
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "VolmetrickLightFeature")]
    public class VolmetrickLightFeature : BaseRenderFeature<VolmetrickLightFeature.VolmetrickLightPass>
    {
        [SerializeField] private ComputeShader VolmetrickLightShader;
        [SerializeField] private VolmetrickLightSettings Settings = new VolmetrickLightSettings();

        public sealed class VolmetrickLightPass : ScriptableRenderPass, System.IDisposable
        {
            private readonly ComputeShader _shader;
            private readonly VolmetrickLightSettings _settings;
            private readonly int _traceKernel;
            private readonly int _compositeKernel;
            private readonly VolmetrickLightHistoryCache _history = new VolmetrickLightHistoryCache();

            private sealed class PassData
            {
                public TextureHandle Source;
                public TextureHandle LightDepth;
                public TextureHandle CameraDepth;
                public TextureHandle History;
                public TextureHandle Result;
                public ComputeShader Shader;
                public int Kernel;
                public int Width;
                public int Height;
                public int TraceWidth;
                public int Parity;
                public int FrameIndex;
                public bool FullRefresh;
                public Matrix4x4 InverseViewProjection;
                public VolmetrickLightHistoryCache.Inputs Inputs;
                public VolmetrickLightHistoryCache.State HistoryState;
            }

            public VolmetrickLightPass(ComputeShader volmetrickLightShader, VolmetrickLightSettings settings)
            {
                _shader = volmetrickLightShader;
                _settings = settings;
                _traceKernel = _shader != null && _shader.HasKernel("CSMain") ? _shader.FindKernel("CSMain") : -1;
                _compositeKernel = _shader != null && _shader.HasKernel("CSComposite") ? _shader.FindKernel("CSComposite") : -1;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public bool IsReady => _shader != null && _traceKernel >= 0 && _compositeKernel >= 0;

            public void Dispose()
            {
                _history.Dispose();
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (!IsReady || resources.isActiveTargetBackBuffer)
                {
                    return;
                }

                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                if (descriptor.dimension != TextureDimension.Tex2D || descriptor.volumeDepth != 1)
                {
                    return;
                }

                UniversalShadowData shadows = frameData.Get<UniversalShadowData>();
                UniversalLightData lights = frameData.Get<UniversalLightData>();
                int cascadeCount = shadows.supportsMainLightShadows
                    ? Mathf.Clamp(shadows.mainLightShadowCascadesCount, 0, 4)
                    : 0;

                Matrix4x4 lightTransform = Matrix4x4.zero;
                Light mainLight = null;
                if (lights.mainLightIndex >= 0 && lights.mainLightIndex < lights.visibleLights.Length)
                {
                    VisibleLight visibleLight = lights.visibleLights[lights.mainLightIndex];
                    lightTransform = visibleLight.localToWorldMatrix;
                    mainLight = visibleLight.light;
                }

                Matrix4x4 projection = GL.GetGPUProjectionMatrix(cameraData.camera.projectionMatrix, true);
                Matrix4x4 viewProjection = projection * cameraData.GetViewMatrix();
                var inputs = new VolmetrickLightHistoryCache.Inputs(viewProjection, lightTransform,
                    mainLight != null ? mainLight.GetInstanceID() : 0,
                    mainLight != null ? mainLight.shadowStrength : 0.0f, cascadeCount, _settings);

                int frameIndex = Time.frameCount;
                VolmetrickLightHistoryCache.State historyState = _history.Acquire(cameraData.camera, frameIndex);
                // 재투영 없이 재사용하므로 편집 표시와 동적 해상도에서는 매번 전체 갱신한다.
                bool allowReuse = Application.isPlaying && cameraData.camera.cameraType == CameraType.Game &&
                    !descriptor.useDynamicScale && !descriptor.useDynamicScaleExplicit &&
                    cameraData.antialiasing != AntialiasingMode.TemporalAntiAliasing;
                bool fullRefresh = historyState.Prepare(descriptor, inputs, frameIndex, allowReuse);
                TextureHandle history = renderGraph.ImportTexture(historyState.Texture);
                TextureHandle lightDepth = resources.mainShadowsTexture;
                if (!lightDepth.IsValid())
                {
                    lightDepth = renderGraph.defaultResources.whiteTexture;
                }

                descriptor.depthBufferBits = 0;
                descriptor.depthStencilFormat = GraphicsFormat.None;
                descriptor.enableRandomWrite = true;
                descriptor.msaaSamples = 1;
                TextureHandle result = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, descriptor, "VolmetrickLightResult", false);

                Matrix4x4 inverseViewProjection = viewProjection.inverse;
                using (var builder = renderGraph.AddComputePass<PassData>("VolmetrickLightPass", out var data))
                {
                    FillPassData(data, resources, lightDepth, history, historyState, inputs,
                        inverseViewProjection, descriptor, frameIndex, fullRefresh);
                    data.Kernel = _traceKernel;

                    builder.UseTexture(data.LightDepth, AccessFlags.Read);
                    builder.UseTexture(data.CameraDepth, AccessFlags.Read);
                    builder.UseTexture(data.History, AccessFlags.ReadWrite);
                    builder.SetRenderFunc(static (PassData passData, ComputeGraphContext context) =>
                    {
                        BindTraceInputs(context, passData);
                        context.cmd.DispatchCompute(passData.Shader, passData.Kernel,
                            (passData.TraceWidth + 7) / 8, (passData.Height + 7) / 8, 1);
                    });
                }

                using (var builder = renderGraph.AddComputePass<PassData>("VolmetrickLight Composite", out var data))
                {
                    FillPassData(data, resources, lightDepth, history, historyState, inputs,
                        inverseViewProjection, descriptor, frameIndex, fullRefresh);
                    data.Kernel = _compositeKernel;
                    data.Result = result;

                    builder.UseTexture(data.Source, AccessFlags.Read);
                    builder.UseTexture(data.LightDepth, AccessFlags.Read);
                    builder.UseTexture(data.CameraDepth, AccessFlags.Read);
                    builder.UseTexture(data.History, AccessFlags.ReadWrite);
                    builder.UseTexture(data.Result, AccessFlags.Write);
                    builder.SetRenderFunc(static (PassData passData, ComputeGraphContext context) =>
                    {
                        BindTraceInputs(context, passData);
                        context.cmd.SetComputeTextureParam(passData.Shader, passData.Kernel, "_ActiveColor", passData.Source);
                        context.cmd.SetComputeTextureParam(passData.Shader, passData.Kernel, "_Result", passData.Result);
                        context.cmd.DispatchCompute(passData.Shader, passData.Kernel,
                            (passData.Width + 7) / 8, (passData.Height + 7) / 8, 1);
                        passData.HistoryState.Complete(passData.Inputs, passData.FrameIndex,
                            passData.Parity, passData.FullRefresh);
                    });
                }

                renderGraph.AddCopyPass(result, resources.activeColorTexture);
            }

            private void FillPassData(PassData data, UniversalResourceData resources, TextureHandle lightDepth,
                TextureHandle history, VolmetrickLightHistoryCache.State historyState,
                in VolmetrickLightHistoryCache.Inputs inputs, Matrix4x4 inverseViewProjection,
                in RenderTextureDescriptor descriptor, int frameIndex, bool fullRefresh)
            {
                data.Source = resources.activeColorTexture;
                data.LightDepth = lightDepth;
                data.CameraDepth = resources.cameraDepthTexture;
                data.History = history;
                data.Shader = _shader;
                data.Width = descriptor.width;
                data.Height = descriptor.height;
                data.TraceWidth = fullRefresh ? descriptor.width : (descriptor.width + 1) / 2;
                data.Parity = historyState.Parity;
                data.FrameIndex = frameIndex;
                data.FullRefresh = fullRefresh;
                data.InverseViewProjection = inverseViewProjection;
                data.Inputs = inputs;
                data.HistoryState = historyState;
            }

            private static void BindTraceInputs(ComputeGraphContext context, PassData data)
            {
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_LightDepth", data.LightDepth);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_CameraDepth", data.CameraDepth);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_History", data.History);
                context.cmd.SetComputeIntParam(data.Shader, "_width", data.Width);
                context.cmd.SetComputeIntParam(data.Shader, "_height", data.Height);
                context.cmd.SetComputeIntParam(data.Shader, "_FullRefresh", data.FullRefresh ? 1 : 0);
                context.cmd.SetComputeIntParam(data.Shader, "_FrameParity", data.Parity);
                context.cmd.SetComputeIntParam(data.Shader, "_VolumetricCascadeCount", data.Inputs.CascadeCount);
                context.cmd.SetComputeIntParam(data.Shader, "_LightingSteps", data.Inputs.LightingSteps);
                context.cmd.SetComputeFloatParam(data.Shader, "_LightingRange", data.Inputs.LightingRange);
                context.cmd.SetComputeFloatParam(data.Shader, "_LightingIntensity", data.Inputs.LightingIntensity);
                context.cmd.SetComputeFloatParam(data.Shader, "_LightingIntensityMax", data.Inputs.LightingIntensityMax);
                context.cmd.SetComputeFloatParam(data.Shader, "_ShadowIntensity", data.Inputs.ShadowIntensity);
                context.cmd.SetComputeFloatParam(data.Shader, "_ShadowIntensityMax", data.Inputs.ShadowIntensityMax);
                context.cmd.SetComputeMatrixParam(data.Shader, "_InverseViewProj", data.InverseViewProjection);
            }
        }

        public override RenderFeatureType Type => RenderFeatureType.VolumetricLight;
        public override ProjectL.Framework.SystemBase.IServiceModule SettingService => Settings;
        protected override RenderPassEvent InjectionPoint => RenderPassEvent.BeforeRenderingTransparents;
        protected override VolmetrickLightPass CreatePass() => new VolmetrickLightPass(VolmetrickLightShader, Settings);

        protected override bool PreparePass(VolmetrickLightPass pass, ref RenderingData renderingData)
        {
            return SystemInfo.supportsComputeShaders && pass.IsReady &&
                SystemInfo.IsFormatSupported(GraphicsFormat.R32G32_SFloat,
                    GraphicsFormatUsage.Sample | GraphicsFormatUsage.LoadStore);
        }
    }
}
