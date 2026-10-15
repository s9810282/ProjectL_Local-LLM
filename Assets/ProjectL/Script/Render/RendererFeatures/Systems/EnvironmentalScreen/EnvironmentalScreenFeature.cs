using ProjectL.Render.Shader.PostProcessing.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Render.Shader.PostProcessing.EnvironmentalScreenPost
{

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Render.Shader.PostProcessing.EnvironmentalScreenPost", sourceAssembly: "Assembly-CSharp", sourceClassName: "EnvironmentalScreen")]
    public class EnvironmentalScreenFeature : BaseRenderFeature<EnvironmentalScreenFeature.EnvironmentalScreenPass>
    {
        public sealed class EnvironmentalScreenPass : ScriptableRenderPass, System.IDisposable
        {
            private const string PassName = "Environmental Screen Post";

            private ComputeShader _shader;
            private EnvironmentalScreenSettings _settings;
            private int _kernel;

            private Texture2D _layer1Texture;
            private Texture2D _layer2Texture;
            private Texture2D _layer3Texture;

            private RTHandle _layer1TextureHandle;
            private RTHandle _layer2TextureHandle;
            private RTHandle _layer3TextureHandle;

            private sealed class PassData
            {
                public TextureHandle Source;
                public TextureHandle Result;
                public TextureHandle Layer1Texture;
                public TextureHandle Layer2Texture;
                public TextureHandle Layer3Texture;

                public ComputeShader Shader;
                public int Kernel;

                public int Width;
                public int Height;
                public float Time;
                public float Aspect;

                public Vector2 MaskCenter;

                public Vector2 Layer1Radius;
                public float Layer1EdgeSoftness;
                public Color Layer1Color;
                public float Layer1Blending;
                public float Layer1Tiling;
                public float Layer1Speed;
                public Vector2 Layer1Direction;

                public Vector2 Layer2Radius;
                public float Layer2EdgeSoftness;
                public Color Layer2Color;
                public float Layer2Blending;
                public float Layer2Tiling;
                public float Layer2Speed;
                public Vector2 Layer2Direction;

                public Vector2 Layer3Radius;
                public float Layer3EdgeSoftness;
                public Color Layer3Color;
                public float Layer3Blending;
                public float Layer3Tiling;
                public float Layer3Speed;
                public Vector2 Layer3Direction;
            }

            public EnvironmentalScreenPass()
            {
                requiresIntermediateTexture = true;
            }

            public void Setup(EnvironmentalScreenSettings settings)
            {
                _settings = settings;

                if (_shader != settings.EnvironmentalShader)
                {
                    _shader = settings.EnvironmentalShader;
                    _kernel = _shader.FindKernel("CSMain");
                }

                EnvironmentalThemeSettings theme = GetTheme(settings);

                UpdateTextureHandle(
                    ref _layer1Texture,
                    ref _layer1TextureHandle,
                    theme.Layer1.Texture == null ? Texture2D.blackTexture : theme.Layer1.Texture);

                UpdateTextureHandle(
                    ref _layer2Texture,
                    ref _layer2TextureHandle,
                    theme.Layer2.Texture == null ? Texture2D.blackTexture : theme.Layer2.Texture);

                UpdateTextureHandle(
                    ref _layer3Texture,
                    ref _layer3TextureHandle,
                    theme.Layer3.Texture == null ? Texture2D.blackTexture : theme.Layer3.Texture);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_settings == null || _settings.Effect == EnvironmentalEffectType.None)
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid())
                    return;

                var descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = "EnvironmentalScreenResult";
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.enableRandomWrite = true;
                descriptor.msaaSamples = MSAASamples.None;

                TextureHandle layer1Texture = renderGraph.ImportTexture(_layer1TextureHandle);
                TextureHandle layer2Texture = renderGraph.ImportTexture(_layer2TextureHandle);
                TextureHandle layer3Texture = renderGraph.ImportTexture(_layer3TextureHandle);
                TextureHandle result = renderGraph.CreateTexture(descriptor);

                EnvironmentalThemeSettings theme = GetTheme(_settings);

                using (var builder = renderGraph.AddComputePass<PassData>(PassName, out PassData passData))
                {
                    passData.Source = source;
                    passData.Result = result;
                    passData.Layer1Texture = layer1Texture;
                    passData.Layer2Texture = layer2Texture;
                    passData.Layer3Texture = layer3Texture;

                    passData.Shader = _shader;
                    passData.Kernel = _kernel;

                    passData.Width = descriptor.width;
                    passData.Height = descriptor.height;
                    passData.Time = Time.time;
                    passData.Aspect = descriptor.height > 0 ? (float)descriptor.width / descriptor.height : 1.0f;

                    passData.MaskCenter = theme.MaskCenter;

                    passData.Layer1Radius = theme.Layer1.Radius;
                    passData.Layer1EdgeSoftness = theme.Layer1.EdgeSoftness;
                    passData.Layer1Color = theme.Layer1.Color;
                    passData.Layer1Blending = theme.Layer1.Blending;
                    passData.Layer1Tiling = theme.Layer1.Tiling;
                    passData.Layer1Speed = theme.Layer1.Speed;
                    passData.Layer1Direction = theme.Layer1.Direction;

                    passData.Layer2Radius = theme.Layer2.Radius;
                    passData.Layer2EdgeSoftness = theme.Layer2.EdgeSoftness;
                    passData.Layer2Color = theme.Layer2.Color;
                    passData.Layer2Blending = theme.Layer2.Blending;
                    passData.Layer2Tiling = theme.Layer2.Tiling;
                    passData.Layer2Speed = theme.Layer2.Speed;
                    passData.Layer2Direction = theme.Layer2.Direction;

                    passData.Layer3Radius = theme.Layer3.Radius;
                    passData.Layer3EdgeSoftness = theme.Layer3.EdgeSoftness;
                    passData.Layer3Color = theme.Layer3.Color;
                    passData.Layer3Blending = theme.Layer3.Blending;
                    passData.Layer3Tiling = theme.Layer3.Tiling;
                    passData.Layer3Speed = theme.Layer3.Speed;
                    passData.Layer3Direction = theme.Layer3.Direction;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(layer1Texture, AccessFlags.Read);
                    builder.UseTexture(layer2Texture, AccessFlags.Read);
                    builder.UseTexture(layer3Texture, AccessFlags.Read);
                    builder.UseTexture(result, AccessFlags.Write);

                    builder.SetRenderFunc(static (PassData data, ComputeGraphContext context) =>
                        ExecutePass(context, data));
                }

                renderGraph.AddBlitPass(
                    result,
                    source,
                    Vector2.one,
                    Vector2.zero,
                    passName: "Environmental Screen Copy");
            }

            private static EnvironmentalThemeSettings GetTheme(EnvironmentalScreenSettings settings)
            {
                return settings.Effect switch
                {
                    EnvironmentalEffectType.Sandstorm => settings.Sandstorm,
                    EnvironmentalEffectType.Rain => settings.Rain,
                    EnvironmentalEffectType.Darkness => settings.Darkness,
                    _ => settings.Sandstorm
                };
            }

            private static void UpdateTextureHandle(
                ref Texture2D currentTexture,
                ref RTHandle currentHandle,
                Texture2D nextTexture)
            {
                if (currentTexture == nextTexture && currentHandle != null)
                    return;

                currentHandle?.Release();
                currentTexture = nextTexture;
                currentHandle = RTHandles.Alloc(nextTexture);
            }

            public void Dispose()
            {
                _layer1TextureHandle?.Release();
                _layer2TextureHandle?.Release();
                _layer3TextureHandle?.Release();

                _layer1TextureHandle = null;
                _layer2TextureHandle = null;
                _layer3TextureHandle = null;

                _layer1Texture = null;
                _layer2Texture = null;
                _layer3Texture = null;
            }

            private static void ExecutePass(ComputeGraphContext context, PassData data)
            {
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_Source", data.Source);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_Layer1Texture", data.Layer1Texture);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_Layer2Texture", data.Layer2Texture);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_Layer3Texture", data.Layer3Texture);
                context.cmd.SetComputeTextureParam(data.Shader, data.Kernel, "_Result", data.Result);

                context.cmd.SetComputeIntParam(data.Shader, "_Width", data.Width);
                context.cmd.SetComputeIntParam(data.Shader, "_Height", data.Height);
                context.cmd.SetComputeFloatParam(data.Shader, "_Time", data.Time);
                context.cmd.SetComputeFloatParam(data.Shader, "_Aspect", data.Aspect);
                context.cmd.SetComputeVectorParam(data.Shader, "_MaskCenter", data.MaskCenter);

                context.cmd.SetComputeVectorParam(data.Shader, "_Layer1Radius", data.Layer1Radius);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer1EdgeSoftness", data.Layer1EdgeSoftness);
                context.cmd.SetComputeVectorParam(data.Shader, "_Layer1Color", data.Layer1Color);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer1Blending", data.Layer1Blending);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer1Tiling", data.Layer1Tiling);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer1Speed", data.Layer1Speed);
                context.cmd.SetComputeVectorParam(data.Shader, "_Layer1Direction", data.Layer1Direction);

                context.cmd.SetComputeVectorParam(data.Shader, "_Layer2Radius", data.Layer2Radius);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer2EdgeSoftness", data.Layer2EdgeSoftness);
                context.cmd.SetComputeVectorParam(data.Shader, "_Layer2Color", data.Layer2Color);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer2Blending", data.Layer2Blending);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer2Tiling", data.Layer2Tiling);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer2Speed", data.Layer2Speed);
                context.cmd.SetComputeVectorParam(data.Shader, "_Layer2Direction", data.Layer2Direction);

                context.cmd.SetComputeVectorParam(data.Shader, "_Layer3Radius", data.Layer3Radius);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer3EdgeSoftness", data.Layer3EdgeSoftness);
                context.cmd.SetComputeVectorParam(data.Shader, "_Layer3Color", data.Layer3Color);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer3Blending", data.Layer3Blending);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer3Tiling", data.Layer3Tiling);
                context.cmd.SetComputeFloatParam(data.Shader, "_Layer3Speed", data.Layer3Speed);
                context.cmd.SetComputeVectorParam(data.Shader, "_Layer3Direction", data.Layer3Direction);

                context.cmd.DispatchCompute(
                    data.Shader,
                    data.Kernel,
                    Mathf.CeilToInt(data.Width / 8.0f),
                    Mathf.CeilToInt(data.Height / 8.0f),
                    1);
            }
        }

        public EnvironmentalScreenSettings Settings = new EnvironmentalScreenSettings();

        public override RenderFeatureType Type => RenderFeatureType.EnvironmentalScreen;
        public override ProjectL.Framework.SystemBase.IServiceModule SettingService =>
            Settings;
        protected override RenderPassEvent InjectionPoint => RenderPassEvent.BeforeRenderingPostProcessing;

        protected override EnvironmentalScreenPass CreatePass() => new EnvironmentalScreenPass();

        protected override bool PreparePass(EnvironmentalScreenPass pass, ref RenderingData renderingData)
        {
            if (!SystemInfo.supportsComputeShaders || Settings.EnvironmentalShader == null ||
                !Settings.EnvironmentalShader.HasKernel("CSMain") || Settings.Effect == EnvironmentalEffectType.None)
                return false;
            pass.Setup(Settings);
            return true;
        }

    }
}
