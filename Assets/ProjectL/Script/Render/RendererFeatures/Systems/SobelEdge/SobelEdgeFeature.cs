using ProjectL.Render.Shader.PostProcessing.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Render.Shader.PostProcessing.SovelEdge
{

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "SobelEdgeFeature")]
    public class SobelEdgeFeature : BaseRenderFeature<SobelEdgeFeature.SobelEdgePass>
    {
        [Header("Input Sobel Edge Shader")]
        [SerializeField] private ComputeShader SobelShader;

        [Header("Sobel Edge Settings")]
        [SerializeField] private SobelEdgeSettings Settings = new SobelEdgeSettings();


        public class SobelEdgePass : ScriptableRenderPass, System.IDisposable
        {
            private class PassData
            {
                public TextureHandle NormalTexture;
                public TextureHandle DepthTexture;
                public TextureHandle ActiveTexture;
                public TextureHandle Result;

                public Color Color;

                public ComputeShader computeShader;
                public int kernel;

                public float edgeIntensity;
                public float depthThreshold;
                public float normalThreshold;
                public float depthSensitivity;

                public int width;
                public int height;
            }

            private readonly ComputeShader SobelShader;
            private readonly SobelEdgeSettings SobelEdgeSettings;
            private readonly int kernel;

            public SobelEdgePass(ComputeShader sobelShader, SobelEdgeSettings settings)
            {
                SobelShader = sobelShader;
                SobelEdgeSettings = settings;

                kernel = SobelShader != null && SobelShader.HasKernel("CSMain") ? SobelShader.FindKernel("CSMain") : -1;
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            }

            public bool IsReady => SobelShader != null && kernel >= 0;

            // RenderGraph owns the transient textures.
            public void Dispose() { }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // ������ ������ �����̳ʿ��� ���� ī�޶� �ؽ�ó �ڿ� ȹ��
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (!IsReady || resourceData.isActiveTargetBackBuffer) return;

                if (resourceData.isActiveTargetBackBuffer) return;

                TextureHandle depthTexture = resourceData.cameraDepthTexture;
                TextureHandle normalTexture = resourceData.cameraNormalsTexture;

                // �ӽ� ��� �ؽ�ó ��ũ���� ���� �� TextureHandle ����
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.enableRandomWrite = true;
            
                TextureHandle result = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_OutlineTempTex", false);

                using ( var builder = renderGraph.AddComputePass<PassData>("SobelEdgePass", out var passData)) 
                {
                    passData.NormalTexture = normalTexture;
                    passData.DepthTexture = depthTexture;
                    passData.ActiveTexture = resourceData.activeColorTexture;
                    passData.Result = result;

                    passData.Color = SobelEdgeSettings.Color;

                    passData.computeShader = SobelShader;
                    passData.kernel = this.kernel;

                    passData.edgeIntensity = SobelEdgeSettings.EdgeIntensity;
                    passData.normalThreshold = SobelEdgeSettings.NormalThreshold;
                    passData.depthThreshold = SobelEdgeSettings.DethThreshold;
                    passData.depthSensitivity = SobelEdgeSettings.DepthSensitivity;

                    passData.width = desc.width;
                    passData.height = desc.height;

                    builder.UseTexture(passData.NormalTexture, AccessFlags.Read);
                    builder.UseTexture(passData.DepthTexture, AccessFlags.Read);
                    builder.UseTexture(passData.ActiveTexture, AccessFlags.Read);
                    builder.UseTexture(passData.Result, AccessFlags.Write);

                    builder.SetRenderFunc(static (PassData data, ComputeGraphContext context) =>
                    {
                        ExecutePass(context, ref data);
                    });
                }

                renderGraph.AddCopyPass(
                    result,
                    resourceData.activeColorTexture
                );
            }

            private static void ExecutePass(ComputeGraphContext context, ref PassData data)
            {
                context.cmd.SetComputeTextureParam(
                    data.computeShader,
                    data.kernel,
                    "_NormalTex",
                    data.NormalTexture
                );

                context.cmd.SetComputeTextureParam(
                    data.computeShader,
                    data.kernel,
                    "_DepthTex",
                    data.DepthTexture
                );

                context.cmd.SetComputeTextureParam(
                    data.computeShader,
                    data.kernel,
                    "_ActiveTex",
                    data.ActiveTexture
                );

                context.cmd.SetComputeTextureParam(
                    data.computeShader,
                    data.kernel,
                    "_Result",
                    data.Result
                );

                context.cmd.SetComputeVectorParam(
                    data.computeShader,
                    "_EdgeColor",
                    data.Color
                );

                context.cmd.SetComputeFloatParam(
                    data.computeShader,
                    "_EdgeIntensity",
                    data.edgeIntensity
                );

                context.cmd.SetComputeFloatParam(
                    data.computeShader,
                    "_DepthThreshold",
                    data.depthThreshold
                );

                context.cmd.SetComputeFloatParam(
                    data.computeShader,
                    "_NormalThreshold",
                    data.normalThreshold
                );

                context.cmd.SetComputeFloatParam(
                    data.computeShader,
                    "_DepthSensitivity",
                    data.depthSensitivity
                );

                // Compute Shader�� _Width / _Height�� ����
                context.cmd.SetComputeIntParam(
                    data.computeShader,
                    "_Width",
                    data.width
                );

                context.cmd.SetComputeIntParam(
                    data.computeShader,
                    "_Height",
                    data.height
                );

                context.cmd.DispatchCompute(
                    data.computeShader,
                    data.kernel,
                    Mathf.CeilToInt(data.width / 8.0f),
                    Mathf.CeilToInt(data.height / 8.0f),
                    1
                );
            }
        }

        public override RenderFeatureType Type => RenderFeatureType.SobelEdge;
        public override ProjectL.Framework.SystemBase.IServiceModule SettingService =>
            Settings;
        protected override RenderPassEvent InjectionPoint => RenderPassEvent.AfterRenderingOpaques;

        protected override SobelEdgePass CreatePass() => new SobelEdgePass(SobelShader, Settings);

        protected override bool PreparePass(SobelEdgePass pass, ref RenderingData renderingData)
        {
            return SystemInfo.supportsComputeShaders && pass.IsReady;
        }

    }
}
