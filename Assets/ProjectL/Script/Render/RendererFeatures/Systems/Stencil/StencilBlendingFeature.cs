using ProjectL.Render.Shader.PostProcessing.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Render.Shader.PostProcessing.Stencil
{

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Render.Shader.PostProcessing.Stencil", sourceAssembly: "Assembly-CSharp", sourceClassName: "StencilBlending")]
    public class StencilBlendingFeature : BaseRenderFeature<StencilBlendingFeature.StencilBlendingPass>
    {
        public class StencilBlendingPass : ScriptableRenderPass, System.IDisposable
        {
            public class PassData
            {
                public TextureHandle Source, Target, Depth, Noise, Result;
                public ComputeShader Shader;
                public int Kernel, Width, Height, GroupsX, GroupsY;
                public Vector2 CircleCenter;
                public float CircleRadius;
                public float TargetRenderTextureThickness;
                public float LineThickness;
                public Color LineColor;
                public float LineNoiseOffset;
                public Vector2 LineNoiseTiling;
                public Matrix4x4 InverseViewProjection;
            }

            private RenderTexture _target;
            private Texture2D _noise;
            private RTHandle _targetHandle, _noiseHandle;
            public const string KernelName = "CSMain";
            private readonly ComputeShader _shader;
            private readonly int _kernel = -1;
            private readonly uint _threadsX, _threadsY;
            private float _noiseOffset;
            private int _lastNoiseFrame = -1;

            public bool IsReady => _shader != null && _shader == _setting.BlendingShader &&
                _kernel >= 0 && _threadsX > 0 && _threadsY > 0;

            public readonly StencilSettings _setting;

            public StencilBlendingPass(StencilSettings setting)
            {
                _setting = setting;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                _shader = setting.BlendingShader;
                if (_shader != null && _shader.HasKernel(KernelName))
                {
                    _kernel = _shader.FindKernel(KernelName);
                    _shader.GetKernelThreadGroupSizes(_kernel, out uint threadsX, out uint threadsY, out _);
                    _threadsX = threadsX;
                    _threadsY = threadsY;
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.isActiveTargetBackBuffer || !IsReady || _setting.TargetRenderTexture == null)
                    return;

                TextureHandle source = resourceData.activeColorTexture;
                TextureHandle depth = resourceData.cameraDepthTexture;
                if (!source.IsValid() || !depth.IsValid()) return;

                // Integrate once per game frame: speed changes do not jump the UV phase,
                // and additional cameras cannot advance the animation twice.
                if (Application.isPlaying && _lastNoiseFrame != Time.frameCount)
                {
                    // The polar radial UV repeats every texture unit, so wrapping is seamless
                    // and avoids unbounded phase growth during long sessions.
                    _noiseOffset = Mathf.Repeat(
                        _noiseOffset + _setting.LineNoiseSpeed * Time.deltaTime, 1f);
                    _lastNoiseFrame = Time.frameCount;
                }

                if (_target != _setting.TargetRenderTexture || _targetHandle == null)
                {
                    _targetHandle?.Release();
                    _target = _setting.TargetRenderTexture;
                    // Import only the color view: a camera RT also has a depth attachment.
                    _targetHandle = RTHandles.Alloc(new RenderTargetIdentifier(_target));
                }

                Texture2D noise = _setting.LineNoiseTexture != null
                    ? _setting.LineNoiseTexture : Texture2D.grayTexture;
                if (_noise != noise || _noiseHandle == null)
                {
                    _noiseHandle?.Release();
                    _noise = noise;
                    _noiseHandle = RTHandles.Alloc(_noise);
                }

                var descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = "StencilBlendingResult";
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.enableRandomWrite = true;
                TextureHandle result = renderGraph.CreateTexture(descriptor);
                TextureHandle target = renderGraph.ImportTexture(_targetHandle, new RenderTargetInfo
                {
                    width = _target.width,
                    height = _target.height,
                    volumeDepth = _target.volumeDepth,
                    msaaSamples = _target.antiAliasing,
                    format = _target.graphicsFormat,
                    bindMS = _target.bindTextureMS
                });
                TextureHandle noiseTexture = renderGraph.ImportTexture(_noiseHandle);

                using (var builder = renderGraph.AddComputePass<PassData>("StencilBlendingPass", out PassData passData))
                {
                    passData.Source = source;
                    passData.Target = target;
                    passData.Depth = depth;
                    passData.Noise = noiseTexture;
                    passData.Result = result;
                    passData.Shader = _shader;
                    passData.Kernel = _kernel;
                    passData.Width = descriptor.width;
                    passData.Height = descriptor.height;
                    passData.GroupsX = Mathf.CeilToInt(descriptor.width / (float)_threadsX);
                    passData.GroupsY = Mathf.CeilToInt(descriptor.height / (float)_threadsY);
                    passData.CircleCenter = _setting.CircleCenter;
                    passData.CircleRadius = Mathf.Max(0f, _setting.CircleRadius);
                    passData.TargetRenderTextureThickness = _setting.TargetRenderTextureThickness;
                    passData.LineThickness = Mathf.Max(0f, _setting.LineThickness);
                    passData.LineNoiseOffset = _noiseOffset;
                    passData.LineNoiseTiling = _setting.LineNoiseTiling;
                    passData.LineColor = QualitySettings.activeColorSpace == ColorSpace.Linear
                        ? _setting.LineColor.linear : _setting.LineColor;
                    // RecordRenderGraph cannot query the legacy cameraColorTargetHandle.
                    // This pass requires an intermediate texture and skips the backbuffer,
                    // so explicitly use the render-to-texture GPU projection convention.
                    Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(
                        cameraData.GetProjectionMatrix(), true);
                    passData.InverseViewProjection =
                        (gpuProjection * cameraData.GetViewMatrix()).inverse;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(target, AccessFlags.Read);
                    builder.UseTexture(depth, AccessFlags.Read);
                    builder.UseTexture(noiseTexture, AccessFlags.Read);
                    builder.UseTexture(result, AccessFlags.Write);
                    builder.SetRenderFunc(static (PassData data, ComputeGraphContext context) => Execute(context, data));
                }

                renderGraph.AddBlitPass(result, source, Vector2.one, Vector2.zero,
                    passName: "Stencil Blending Copy");
            }

            private static void Execute(ComputeGraphContext context, PassData data)
            {
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_ActiveTex", data.Source);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_TargetRenderTexture", data.Target);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_DepthTex", data.Depth);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_LineNoiseTexture", data.Noise);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_Result", data.Result);
                context.cmd.SetComputeIntParam(data.Shader, "_Width", data.Width);
                context.cmd.SetComputeIntParam(data.Shader, "_Height", data.Height);
                context.cmd.SetComputeVectorParam(data.Shader, "_CircleCenter",
                    new Vector4(data.CircleCenter.x, data.CircleCenter.y, 0f, 0f));
                context.cmd.SetComputeFloatParam(data.Shader, "_CircleRadius", data.CircleRadius);
                context.cmd.SetComputeFloatParam(data.Shader, "_TargetRenderTextureThickness", data.TargetRenderTextureThickness);
                context.cmd.SetComputeFloatParam(data.Shader, "_LineThickness", data.LineThickness);
                context.cmd.SetComputeVectorParam(data.Shader, "_LineColor", data.LineColor);
                context.cmd.SetComputeFloatParam(data.Shader, "_LineNoiseOffset", data.LineNoiseOffset);
                context.cmd.SetComputeVectorParam(data.Shader, "_LineNoiseTiling",
                    new Vector4(data.LineNoiseTiling.x, data.LineNoiseTiling.y, 0f, 0f));
                context.cmd.SetComputeMatrixParam(data.Shader, "_InverseViewProjection", data.InverseViewProjection);
                context.cmd.DispatchCompute(data.Shader, data.Kernel, data.GroupsX, data.GroupsY, 1);
            }

            public void Dispose()
            {
                _targetHandle?.Release();
                _noiseHandle?.Release();
                _targetHandle = null;
                _noiseHandle = null;
                _target = null;
                _noise = null;
            }
        }

        public override RenderFeatureType Type => RenderFeatureType.StencilBlending;
        protected override RenderPassEvent InjectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;

        public StencilSettings setting = new StencilSettings();

        protected override StencilBlendingPass CreatePass() => new StencilBlendingPass(setting);

        protected override bool PreparePass(StencilBlendingPass pass, ref RenderingData renderingData)
        {
            return SystemInfo.supportsComputeShaders && pass.IsReady &&
                renderingData.cameraData.cameraType == CameraType.Game &&
                renderingData.cameraData.renderType == CameraRenderType.Base &&
                !renderingData.cameraData.xr.enabled && setting.TargetRenderTexture != null &&
                renderingData.cameraData.camera.targetTexture != setting.TargetRenderTexture;
        }

        public override ProjectL.Framework.SystemBase.IServiceModule SettingService =>
            setting;
    }
}
