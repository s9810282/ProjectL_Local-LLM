using System.Collections.Generic;
using ProjectL.Render.RendererFeatures.Systems.Stencil;
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
        [System.Serializable]
        private struct LinePreset
        {
            public LineKey key;
            public LineData data;
        }

        [SerializeField, Tooltip("경계선 프리셋의 유일한 편집 원본이다. 챕터 키는 중복될 수 없다.")]
        private List<LinePreset> linePresets = new List<LinePreset>
        {
            new LinePreset
            {
                key = LineKey.Capter1,
                data = new LineData(0.1f, Color.white, null, Vector2.one, 0.2f)
            },
            new LinePreset
            {
                key = LineKey.Capter2,
                data = new LineData(0.1f, Color.white, null, Vector2.one, 0.2f)
            },
            new LinePreset
            {
                key = LineKey.Capter3,
                data = new LineData(0.1f, Color.white, null, Vector2.one, 0.2f)
            }
        };

        private readonly Dictionary<LineKey, LineData> _lines = new Dictionary<LineKey, LineData>();
        private LineKey _selectedLineKey;
        private LineData _selectedLineData;
        private bool _hasSelectedLine;
        private readonly LineNoiseAnimationState _lineNoiseAnimation = new LineNoiseAnimationState();

        // Inspector의 반경 편집으로 Pass가 재생성되어도 이동 위상은 Feature 수명 동안 유지한다.
        internal sealed class LineNoiseAnimationState
        {
            private float _offset;
            private int _lastFrame = -1;

            public float Offset => _offset;

            public void Advance(float speed, float deltaTime, int frame)
            {
                if (_lastFrame == frame)
                {
                    return;
                }

                _offset = Mathf.Repeat(_offset + speed * deltaTime, 1f);
                _lastFrame = frame;
            }
        }

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
                public float LineDistortion;
                public Matrix4x4 InverseViewProjection;
            }

            private RenderTexture _target;
            private Texture2D _noise;
            private RTHandle _targetHandle, _noiseHandle;
            public const string KernelName = "CSMain";
            private readonly ComputeShader _shader;
            private readonly int _kernel = -1;
            private readonly uint _threadsX, _threadsY;
            private readonly LineNoiseAnimationState _noiseAnimation;
            private LineData _lineData;

            public bool IsReady => _shader != null && _shader == _setting.BlendingShader &&
                _kernel >= 0 && _threadsX > 0 && _threadsY > 0;

            public readonly StencilSettings _setting;

            internal StencilBlendingPass(StencilSettings setting, LineData lineData, LineNoiseAnimationState noiseAnimation)
            {
                _setting = setting;
                _lineData = lineData;
                _noiseAnimation = noiseAnimation;
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

            public void SetLineData(LineData lineData)
            {
                _lineData = lineData;
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

                // 카메라가 여럿이거나 같은 프레임에 Pass가 재생성되어도 위상은 한 번만 진행한다.
                if (Application.isPlaying)
                {
                    _noiseAnimation.Advance(_lineData.LineNoiseSpeed, Time.deltaTime, Time.frameCount);
                }

                if (_target != _setting.TargetRenderTexture || _targetHandle == null)
                {
                    _targetHandle?.Release();
                    _target = _setting.TargetRenderTexture;
                    // Import only the color view: a camera RT also has a depth attachment.
                    _targetHandle = RTHandles.Alloc(new RenderTargetIdentifier(_target));
                }

                Texture2D noise = _lineData.LineNoiseTexture != null
                    ? _lineData.LineNoiseTexture : Texture2D.grayTexture;
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
                    passData.LineThickness = Mathf.Max(0f, _lineData.LineThickness);
                    passData.LineNoiseOffset = _noiseAnimation.Offset;
                    passData.LineNoiseTiling = _lineData.LineNoiseTiling;
                    passData.LineDistortion = Mathf.Max(0f, _lineData.LineDistortion);
                    Color lineColor = _setting.ResolveLineColor(_lineData.LineColor);
                    passData.LineColor = QualitySettings.activeColorSpace == ColorSpace.Linear
                        ? lineColor.linear : lineColor;
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
                _setting.NotifyPresented();
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
                context.cmd.SetComputeFloatParam(data.Shader, "_LineDistortion", data.LineDistortion);
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

        protected override StencilBlendingPass CreatePass()
        {
            RebuildLineLookup();
            return new StencilBlendingPass(setting, GetSelectedLineData(), _lineNoiseAnimation);
        }

        private void RebuildLineLookup()
        {
            _lines.Clear();
            _hasSelectedLine = false;

            if (linePresets == null)
            {
                throw new System.InvalidOperationException("경계선 프리셋 목록이 없습니다.");
            }

            foreach (LinePreset preset in linePresets)
            {
                if (!System.Enum.IsDefined(typeof(LineKey), preset.key))
                {
                    throw new System.InvalidOperationException($"정의되지 않은 경계선 프리셋 키입니다: {preset.key}");
                }

                if (!_lines.TryAdd(preset.key, preset.data))
                {
                    throw new System.InvalidOperationException($"중복된 경계선 프리셋 키입니다: {preset.key}");
                }
            }
        }

        private LineData GetSelectedLineData()
        {
            LineKey key = setting.LineKey;
            if (!_hasSelectedLine || _selectedLineKey != key)
            {
                if (!_lines.TryGetValue(key, out LineData data))
                {
                    throw new System.InvalidOperationException($"등록되지 않은 경계선 프리셋 키입니다: {key}");
                }

                _selectedLineKey = key;
                _selectedLineData = data;
                _hasSelectedLine = true;
            }

            return _selectedLineData;
        }

        protected override bool PreparePass(StencilBlendingPass pass, ref RenderingData renderingData)
        {
            if (!string.IsNullOrEmpty(setting.OutputCameraName) &&
                !string.Equals(renderingData.cameraData.camera.gameObject.name,
                    setting.OutputCameraName, System.StringComparison.Ordinal))
            {
                return false;
            }

            if (!(SystemInfo.supportsComputeShaders && pass.IsReady &&
                renderingData.cameraData.cameraType == CameraType.Game &&
                renderingData.cameraData.renderType == CameraRenderType.Base &&
                !renderingData.cameraData.xr.enabled && setting.TargetRenderTexture != null &&
                renderingData.cameraData.camera.targetTexture != setting.TargetRenderTexture))
            {
                return false;
            }

            pass.SetLineData(GetSelectedLineData());
            return true;
        }

        public override ProjectL.Framework.SystemBase.IServiceModule SettingService =>
            setting;
    }
}
