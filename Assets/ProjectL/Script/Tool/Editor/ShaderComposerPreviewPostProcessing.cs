using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Tool.Editor
{
    // 프리뷰 전용 Volume과 계산 결과를 소유하고 렌더 동안만 전역 stack을 교체한다.
    internal sealed class ShaderComposerPreviewPostProcessing : IDisposable
    {
        private readonly UniversalRenderPipeline.SingleCameraRequest _request = new UniversalRenderPipeline.SingleCameraRequest();
        private UniversalAdditionalCameraData _cameraData;
        private Volume _volume;
        private VolumeStack _stack;

        internal bool Render(PreviewRenderUtility preview, ShaderComposerPreviewSettings settings)
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) return false;
            // 리로드 뒤 첫 프리뷰가 파이프라인 초기화보다 먼저 호출될 수 있다.
            if (!VolumeManager.instance.isInitialized)
            {
                preview.Render(true, false);
                if (!VolumeManager.instance.isInitialized) return true;
            }
            Camera camera = preview.camera;
            if (_cameraData == null)
            {
                _cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                _cameraData.renderShadows = false;
                _cameraData.volumeLayerMask = 0;
            }
            _cameraData.renderPostProcessing = settings.postProcessing;
            if (_stack == null || !_stack.isValid)
            {
                if (_stack != null) VolumeManager.instance.DestroyStack(_stack);
                _stack = VolumeManager.instance.CreateStack();
            }
            if (_volume == null)
            {
                var source = EditorUtility.CreateGameObjectWithHideFlags("Shader Composer Preview Volume", HideFlags.HideAndDontSave);
                preview.AddSingleGO(source);
                _volume = source.AddComponent<Volume>();
                _volume.enabled = false;
                _volume.isGlobal = true;
                _volume.weight = 1;
            }
            VolumeStack savedStack = VolumeManager.instance.stack;
            try
            {
                // 빈 Volume을 렌더 동안 등록해 씬 Volume이 하나도 없어도 Update가
                // 조기 반환하지 않고 이전 프로필의 override를 기본값으로 되돌리게 한다.
                _volume.enabled = true;
                VolumeManager.instance.Update(_stack, camera.transform, _cameraData.volumeLayerMask);
                if (settings.postProcessing && settings.volumeProfile != null)
                {
                    foreach (VolumeComponent component in settings.volumeProfile.components)
                    {
                        if (component == null || !component.active) continue;
                        VolumeComponent state = _stack.GetComponent(component.GetType());
                        if (state != null) component.Override(state, 1);
                    }
                }
                VolumeManager.instance.stack = _stack;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = preview.ambientColor;
                _request.destination = camera.targetTexture;
                RenderPipeline.SubmitRenderRequest(camera, _request);
                Graphics.SetRenderTarget(camera.targetTexture);
                return true;
            }
            finally
            {
                _volume.enabled = false;
                VolumeManager.instance.stack = savedStack;
            }
        }

        public void Dispose()
        {
            if (_stack != null) VolumeManager.instance.DestroyStack(_stack);
            _stack = null;
            // GameObject와 카메라 컴포넌트는 PreviewRenderUtility.Cleanup이 해제한다.
            _volume = null;
            _cameraData = null;
        }
    }
}
