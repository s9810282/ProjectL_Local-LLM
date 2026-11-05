using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ProjectL.Render.RendererFeatures.Systems.EffectOcclusionOutline
{
    /// <summary>선택한 이펙트가 가린 캐릭터의 바깥 윤곽을 투명 렌더 뒤에 표시한다.</summary>
    [DisallowMultipleRendererFeature("Effect Occlusion Outline")]
    public sealed class EffectOcclusionOutlineFeature : ScriptableRendererFeature
    {
        [Serializable]
        public sealed class Configuration
        {
            [Tooltip("윤곽을 표시할 불투명 캐릭터. DepthOnly 패스가 필요합니다.")]
            public LayerMask characterLayers;
            [Tooltip("가림 원인인 이펙트. Cartoon 전용 마스크 패스를 지원해야 합니다. 캐릭터/벽과 다른 레이어를 사용하세요.")]
            public LayerMask effectLayers;
            [Range(0f, 20f)] public float widthPixels = 3f;
            [Tooltip("프로젝트의 평균 노멀 베이크가 UV3에 적용된 캐릭터만 켜세요.")]
            public bool useBakedNormals;
            public Color outlineColor = new Color(1f, 0.8f, 0.4f, 0.9f);
            [Range(0f, 0.99f)] public float minimumOpacity = 0.08f;
            [Range(0.001f, 1f)] public float opacitySoftness = 0.4f;
            [Min(0.0001f), Tooltip("시점 공간 거리의 깊이 오차 허용량.")]
            public float depthBias = 0.01f;
        }

        [SerializeField] private Configuration settings = new Configuration();
        [SerializeField, HideInInspector] private UnityEngine.Shader outlineShader;
        private Material _material;
        private OutlinePass _pass;

        public Configuration Settings => settings;

        /// <summary>에디터 조립에서 빌드에 포함할 셰이더 참조를 명시한다.</summary>
        public void SetShader(UnityEngine.Shader shader)
        {
            outlineShader = shader != null ? shader : throw new ArgumentNullException(nameof(shader));
            Create();
        }

        public override void Create()
        {
            CoreUtils.Destroy(_material);
            _material = null;
            _pass = null;
#if UNITY_EDITOR
            // Add Renderer Feature로 직접 추가한 경우에도 빌드용 직렬화 참조를 보관한다.
            if (outlineShader == null)
            {
                outlineShader = UnityEngine.Shader.Find("Hidden/ProjectL/EffectOcclusionOutline");
            }
#endif
            if (outlineShader != null)
            {
                _material = CoreUtils.CreateEngineMaterial(outlineShader);
                _pass = new OutlinePass(_material, settings);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            if (camera == null || renderingData.cameraData.renderType == CameraRenderType.Overlay ||
                (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) ||
                settings.characterLayers.value == 0 || settings.effectLayers.value == 0 || settings.widthPixels <= 0f)
            {
                return;
            }
#if UNITY_EDITOR
            if (EditorSceneManager.IsPreviewSceneObject(camera.gameObject) ||
                (camera.cameraType == CameraType.SceneView && PrefabStageUtility.GetCurrentPrefabStage() != null))
            {
                return;
            }
#endif
            if ((settings.characterLayers.value & settings.effectLayers.value) != 0)
            {
                throw new InvalidOperationException(name + ": Character Layers와 Effect Layers는 겹칠 수 없다.");
            }
            if (_pass == null)
            {
                throw new InvalidOperationException(name + ": 윤곽선 셰이더 참조가 없다. 설치 메뉴로 기능을 추가하세요.");
            }
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
            _pass = null;
        }

        private sealed class OutlinePass : ScriptableRenderPass
        {
            private static readonly int WorldDepthId = UnityEngine.Shader.PropertyToID("_ProjectLEOOWorldDepth");
            private static readonly int OriginalDepthId = UnityEngine.Shader.PropertyToID("_ProjectLEOOOriginalDepth");
            private static readonly int HullDepthId = UnityEngine.Shader.PropertyToID("_ProjectLEOOHullDepth");
            private static readonly int CoverageId = UnityEngine.Shader.PropertyToID("_ProjectLEOOCoverage");
            private static readonly int ParametersId = UnityEngine.Shader.PropertyToID("_ProjectLEOOParameters");
            private static readonly List<ShaderTagId> ForwardTags = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("UniversalForward"), new ShaderTagId("SRPDefaultUnlit")
            };
            private static readonly List<ShaderTagId> DepthTags = new List<ShaderTagId> { new ShaderTagId("DepthOnly") };
            private static readonly List<ShaderTagId> CoverageTags = new List<ShaderTagId> { new ShaderTagId("ProjectLEffectOcclusion") };

            private readonly Material _material;
            private readonly Configuration _settings;

            private sealed class DrawData
            {
                public RendererListHandle list;
                public Vector4 parameters;
            }

            public OutlinePass(Material material, Configuration settings)
            {
                _material = material;
                _settings = settings;
                requiresIntermediateTexture = true;
                renderPassEvent = (RenderPassEvent)((int)RenderPassEvent.AfterRenderingTransparents + 2);
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer || !resources.activeColorTexture.IsValid())
                {
                    return;
                }
                _material.SetFloat("_WidthPixels", _settings.widthPixels);
                _material.SetFloat("_UseBakedNormals", _settings.useBakedNormals ? 1f : 0f);
                _material.SetColor("_OutlineColor", _settings.outlineColor);
                Vector4 parameters = new Vector4(_settings.minimumOpacity, Mathf.Max(0.001f, _settings.opacitySoftness),
                    Mathf.Max(0.0001f, _settings.depthBias), 0f);

                TextureDesc descriptor = graph.GetTextureDesc(resources.activeColorTexture);
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                descriptor.enableRandomWrite = false;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                descriptor.memoryless = RenderTextureMemoryless.None;
                descriptor.clearBuffer = true;
                descriptor.filterMode = FilterMode.Point;
                descriptor.wrapMode = TextureWrapMode.Clamp;
                descriptor.depthBufferBits = DepthBits.Depth32;
                TextureHandle world = CreateTexture(graph, descriptor, "Effect Outline World Depth");
                TextureHandle original = CreateTexture(graph, descriptor, "Effect Outline Original Depth");
                TextureHandle hull = CreateTexture(graph, descriptor, "Effect Outline Hull Depth");

                DrawDepth(graph, frameData, world, ~_settings.effectLayers.value, null, 0, WorldDepthId);
                DrawDepth(graph, frameData, original, _settings.characterLayers.value, null, 0, OriginalDepthId);
                DrawDepth(graph, frameData, hull, _settings.characterLayers.value, _material, 0, HullDepthId);

                descriptor.colorFormat = GraphicsFormat.R16_SFloat;
                descriptor.clearColor = Color.clear;
                TextureHandle coverage = CreateTexture(graph, descriptor, "Effect Outline Coverage");
                RendererListHandle effects = CreateList(graph, frameData, CoverageTags, RenderQueueRange.all,
                    _settings.effectLayers.value, null, 0);
                using (var builder = graph.AddRasterRenderPass<DrawData>("Effect Outline Coverage", out var data))
                {
                    data.list = effects;
                    data.parameters = parameters;
                    builder.UseRendererList(effects);
                    builder.UseTexture(world, AccessFlags.Read);
                    builder.UseTexture(hull, AccessFlags.Read);
                    // 부분 메시와 알파 누적이므로 초기화된 나머지 픽셀도 보존한다.
                    builder.SetRenderAttachment(coverage, 0, AccessFlags.ReadWrite);
                    builder.SetGlobalTextureAfterPass(coverage, CoverageId);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (DrawData pass, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalVector(ParametersId, pass.parameters);
                        context.cmd.DrawRendererList(pass.list);
                    });
                }

                RendererListHandle outline = CreateList(graph, frameData, ForwardTags, RenderQueueRange.opaque,
                    _settings.characterLayers.value, _material, 1);
                using (var builder = graph.AddRasterRenderPass<DrawData>("Effect Outline Composite", out var data))
                {
                    data.list = outline;
                    data.parameters = parameters;
                    builder.UseRendererList(outline);
                    builder.UseTexture(world, AccessFlags.Read);
                    builder.UseTexture(original, AccessFlags.Read);
                    builder.UseTexture(hull, AccessFlags.Read);
                    builder.UseTexture(coverage, AccessFlags.Read);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (DrawData pass, RasterGraphContext context) =>
                    {
                        context.cmd.SetGlobalVector(ParametersId, pass.parameters);
                        context.cmd.DrawRendererList(pass.list);
                    });
                }
            }

            private static TextureHandle CreateTexture(RenderGraph graph, TextureDesc descriptor, string label)
            {
                descriptor.name = label;
                return graph.CreateTexture(descriptor);
            }

            private static RendererListHandle CreateList(RenderGraph graph, ContextContainer frameData,
                List<ShaderTagId> tags, RenderQueueRange queue, int layers, Material material, int pass)
            {
                UniversalRenderingData rendering = frameData.Get<UniversalRenderingData>();
                DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(tags, rendering,
                    frameData.Get<UniversalCameraData>(), frameData.Get<UniversalLightData>(), SortingCriteria.CommonOpaque);
                drawing.overrideMaterial = material;
                drawing.overrideMaterialPassIndex = pass;
                var filtering = new FilteringSettings(queue, layers);
                var parameters = new RendererListParams(rendering.cullResults, drawing, filtering);
                return graph.CreateRendererList(parameters);
            }

            private static void DrawDepth(RenderGraph graph, ContextContainer frameData, TextureHandle depth,
                int layers, Material material, int pass, int globalId)
            {
                RendererListHandle list = CreateList(graph, frameData, material == null ? DepthTags : ForwardTags,
                    RenderQueueRange.opaque, layers, material, pass);
                using (var builder = graph.AddRasterRenderPass<DrawData>("Effect Outline Depth", out var data))
                {
                    data.list = list;
                    builder.UseRendererList(list);
                    builder.SetRenderAttachmentDepth(depth, AccessFlags.ReadWrite);
                    builder.SetGlobalTextureAfterPass(depth, globalId);
                    builder.SetRenderFunc(static (DrawData draw, RasterGraphContext context) => context.cmd.DrawRendererList(draw.list));
                }
            }
        }
    }
}
