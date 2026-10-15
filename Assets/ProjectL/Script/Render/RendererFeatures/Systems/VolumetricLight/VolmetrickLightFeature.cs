using ProjectL.Render.Shader.PostProcessing.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using UnityEngine;
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

        public class VolmetrickLightPass : ScriptableRenderPass, System.IDisposable
        {
            private readonly ComputeShader VolmetrickLightShader;
            private readonly VolmetrickLightSettings VolmetrickLightSettings;
            private readonly int kernel;

            public class PassData
            {
                public TextureHandle ActiveColorTexture;
                public TextureHandle LightDepthTexture;
                public TextureHandle CameraDepthTexture;
                public TextureHandle Result;

                public ComputeShader VolmetrickLightShader;
                public int kernel;
                public int width;
                public int height;
                public int CascadeCount;

                public int LightingSteps;
                public float LightingIntensity;
                public float LightingIntensityMax;
                public float LightingRange;

                public int ShadowSteps;
                public float ShadowIntensity;
                public float ShadowIntensityMax;
                public Color ShadowsColor;

                public Matrix4x4 InverseViewProj;
                public Vector3 CameraPos;
            }

            public VolmetrickLightPass(ComputeShader volmetrickLightShader, VolmetrickLightSettings settings)
            {
                VolmetrickLightShader = volmetrickLightShader;
                VolmetrickLightSettings = settings;
                kernel = VolmetrickLightShader != null && VolmetrickLightShader.HasKernel("CSMain") ? VolmetrickLightShader.FindKernel("CSMain") : -1;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public bool IsReady => VolmetrickLightShader != null && kernel >= 0;

            // RenderGraph owns the transient textures.
            public void Dispose() { }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (!IsReady || resourceData.isActiveTargetBackBuffer) return;
                UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

                TextureHandle ActiveColor = resourceData.activeColorTexture;
                TextureHandle LightDepth = resourceData.mainShadowsTexture;
                TextureHandle CameraDepth = resourceData.cameraDepthTexture;

                RenderTextureDescriptor Descriptor = cameraData.cameraTargetDescriptor;
                Descriptor.depthBufferBits = 0;
                Descriptor.enableRandomWrite = true;
                Descriptor.msaaSamples = 1;
                Descriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;

                TextureHandle Result = UniversalRenderer.CreateRenderGraphTexture(renderGraph, Descriptor, "VolmetrickLightResult", false);

                using (var builder = renderGraph.AddComputePass<PassData>("VolmetrickLightPass", out var passData))
                {
                    passData.ActiveColorTexture = ActiveColor;
                    passData.LightDepthTexture = LightDepth;
                    passData.CameraDepthTexture = CameraDepth;
                    passData.Result = Result;

                    passData.VolmetrickLightShader = VolmetrickLightShader;
                    passData.kernel = kernel;

                    passData.width = Descriptor.width;
                    passData.height = Descriptor.height;
                    passData.CascadeCount = shadowData.supportsMainLightShadows
                        ? Mathf.Clamp(shadowData.mainLightShadowCascadesCount, 0, 4)
                        : 0;

                    passData.LightingSteps = VolmetrickLightSettings.LightingSteps;
                    passData.LightingIntensity = VolmetrickLightSettings.LightingIntensity;
                    passData.LightingIntensityMax = VolmetrickLightSettings.LightingIntensityMax;
                    passData.LightingRange = VolmetrickLightSettings.LightingRange;

                    passData.ShadowSteps = VolmetrickLightSettings.ShadowSteps;
                    passData.ShadowIntensity = VolmetrickLightSettings.ShadowIntensity;
                    passData.ShadowIntensityMax = VolmetrickLightSettings.ShadowIntensityMax;
                    passData.ShadowsColor = VolmetrickLightSettings.ShadowsColor;

                    Matrix4x4 viewMat = cameraData.GetViewMatrix();
                    Matrix4x4 projMat = GL.GetGPUProjectionMatrix(cameraData.camera.projectionMatrix, true);
                    Matrix4x4 vpMat = projMat * viewMat;

                    passData.InverseViewProj = vpMat.inverse;
                    passData.CameraPos = cameraData.camera.transform.position;

                    builder.UseTexture(ActiveColor, AccessFlags.Read);
                    builder.UseTexture(LightDepth, AccessFlags.Read);
                    builder.UseTexture(CameraDepth, AccessFlags.Read);
                    builder.UseTexture(Result, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, ComputeGraphContext context) =>
                    {
                        ExecutePass(context, ref data);
                    });
                }

                renderGraph.AddCopyPass(
                    Result,
                    resourceData.activeColorTexture
                );
            }

            private static void ExecutePass(ComputeGraphContext context, ref PassData data)
            {
                context.cmd.SetComputeTextureParam(data.VolmetrickLightShader, data.kernel, "_ActiveColor", data.ActiveColorTexture);
                context.cmd.SetComputeTextureParam(data.VolmetrickLightShader, data.kernel, "_LightDepth", data.LightDepthTexture);
                context.cmd.SetComputeTextureParam(data.VolmetrickLightShader, data.kernel, "_CameraDepth", data.CameraDepthTexture);
                context.cmd.SetComputeTextureParam(data.VolmetrickLightShader, data.kernel, "_Result", data.Result);

                context.cmd.SetComputeIntParam(data.VolmetrickLightShader, "_width", data.width);
                context.cmd.SetComputeIntParam(data.VolmetrickLightShader, "_height", data.height);
                context.cmd.SetComputeIntParam(data.VolmetrickLightShader, "_VolumetricCascadeCount", data.CascadeCount);

                context.cmd.SetComputeIntParam(data.VolmetrickLightShader, "_LightingSteps", data.LightingSteps);
                context.cmd.SetComputeFloatParam(data.VolmetrickLightShader, "_LightingIntensity", data.LightingIntensity);
                context.cmd.SetComputeFloatParam(data.VolmetrickLightShader, "_LightingIntensityMax", data.LightingIntensityMax);
                context.cmd.SetComputeFloatParam(data.VolmetrickLightShader, "_LightingRange", data.LightingRange);

                context.cmd.SetComputeIntParam(data.VolmetrickLightShader, "_ShadowSteps", data.ShadowSteps);
                context.cmd.SetComputeFloatParam(data.VolmetrickLightShader, "_ShadowIntensity", data.ShadowIntensity);
                context.cmd.SetComputeFloatParam(data.VolmetrickLightShader, "_ShadowIntensityMax", data.ShadowIntensityMax);
                context.cmd.SetComputeVectorParam(data.VolmetrickLightShader, "_ShadowsColor", data.ShadowsColor);

                context.cmd.SetComputeMatrixParam(data.VolmetrickLightShader, "_InverseViewProj", data.InverseViewProj);
                context.cmd.SetComputeVectorParam(data.VolmetrickLightShader, "_CameraPos", data.CameraPos);

                context.cmd.DispatchCompute(
                    data.VolmetrickLightShader,
                    data.kernel,
                    Mathf.CeilToInt(data.width / 8.0f),
                    Mathf.CeilToInt(data.height / 8.0f),
                    1);
            }
        }

        public override RenderFeatureType Type => RenderFeatureType.VolumetricLight;
        public override ProjectL.Framework.SystemBase.IServiceModule SettingService =>
            Settings;
        protected override RenderPassEvent InjectionPoint => RenderPassEvent.BeforeRenderingTransparents;

        protected override VolmetrickLightPass CreatePass() => new VolmetrickLightPass(VolmetrickLightShader, Settings);

        protected override bool PreparePass(VolmetrickLightPass pass, ref RenderingData renderingData)
        {
            return SystemInfo.supportsComputeShaders && pass.IsReady;
        }

    }
}
