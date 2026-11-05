using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ProjectL.Render.RendererFeatures.Systems.TransparentScreenCapture
{
    /// <summary>일반 투명 렌더를 포함한 화면을 복사하고 선택한 왜곡 Layer를 뒤에 그린다.</summary>
    public sealed class TransparentScreenCaptureFeature : ScriptableRendererFeature
    {
        public const string TextureName = "_ProjectLTransparentScreenTexture";

        [SerializeField, Tooltip("화면 복사 후 그릴 투명 Layer. Renderer Data의 Transparent Layer Mask에서 제외하고 카메라에는 포함하세요. Nothing은 화면 복사만 합니다.")]
        private LayerMask distortionLayerMask;

        private CapturePass _pass;

        public override void Create()
        {
            _pass = new CapturePass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            if (_pass == null || camera == null ||
                (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView))
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
            _pass.DistortionLayers = distortionLayerMask.value;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass = null;
        }

        private sealed class CapturePass : ScriptableRenderPass
        {
            private static readonly int TextureId = UnityEngine.Shader.PropertyToID(TextureName);
            private static readonly List<ShaderTagId> ShaderTags = new()
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly")
            };

            private sealed class CopyData
            {
                public TextureHandle Source;
            }

            private sealed class DrawData
            {
                public RendererListHandle RendererList;
            }

            public int DistortionLayers { private get; set; }

            public CapturePass()
            {
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                TextureHandle source = resources.activeColorTexture;
                if (resources.isActiveTargetBackBuffer || !source.IsValid())
                {
                    return;
                }

                TextureDesc descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = TextureName;
                descriptor.depthBufferBits = DepthBits.None;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                descriptor.enableRandomWrite = false;
                descriptor.clearBuffer = false;
                descriptor.filterMode = FilterMode.Bilinear;
                descriptor.wrapMode = TextureWrapMode.Clamp;
                TextureHandle captured = renderGraph.CreateTexture(descriptor);

                using (var builder = renderGraph.AddRasterRenderPass<CopyData>("Transparent Screen Capture", out CopyData data))
                {
                    data.Source = source;
                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(captured, 0, AccessFlags.WriteAll);
                    builder.SetGlobalTextureAfterPass(captured, TextureId);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (CopyData pass, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, pass.Source, new Vector4(1f, 1f, 0f, 0f), 0, false);
                    });
                }

                int layers = DistortionLayers;
                if (layers != 0)
                {
                    DrawDistortion(renderGraph, frameData, resources, captured, layers);
                }
            }

            private static void DrawDistortion(RenderGraph renderGraph, ContextContainer frameData,
                UniversalResourceData resources, TextureHandle captured, int layers)
            {
                UniversalRenderingData rendering = frameData.Get<UniversalRenderingData>();
                UniversalCameraData camera = frameData.Get<UniversalCameraData>();
                UniversalLightData lights = frameData.Get<UniversalLightData>();
                DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(
                    ShaderTags, rendering, camera, lights, SortingCriteria.CommonTransparent);
                var filtering = new FilteringSettings(RenderQueueRange.transparent, layers);
                var listParameters = new RendererListParams(rendering.cullResults, drawing, filtering);
                RendererListHandle rendererList = renderGraph.CreateRendererList(listParameters);

                using (var builder = renderGraph.AddRasterRenderPass<DrawData>("Transparent Screen Distortion", out DrawData data))
                {
                    data.RendererList = rendererList;
                    builder.UseRendererList(rendererList);
                    builder.UseTexture(captured, AccessFlags.Read);
                    // Shader Graph가 읽는 화면/깊이 등 전역 텍스처의 수명도 명시한다.
                    builder.UseAllGlobalTextures(true);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    if (resources.activeDepthTexture.IsValid())
                    {
                        builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                    }
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (DrawData pass, RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(pass.RendererList);
                    });
                }
            }
        }
    }
}
