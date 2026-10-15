using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Render.Shader.PostProcessing.Core
{
    public abstract class BaseRenderFeature<TPass> : ScriptableRendererFeature, IRenderFeatureNotifireService
        where TPass : ScriptableRenderPass, IDisposable
    {
        [SerializeField, Tooltip("Register this feature's pass while enabled. Disabling retains its resources for reuse.")]
        protected bool IsAddRenderPass = true;
        public abstract RenderFeatureType Type { get; }
        public abstract ProjectL.Framework.SystemBase.IServiceModule SettingService { get; }
        protected TPass Pass { get; private set; }
        protected abstract RenderPassEvent InjectionPoint { get; }

        public void UseRenderFreature(bool isRender)
        {
            IsAddRenderPass = isRender;
        }

        protected abstract TPass CreatePass();

        // Return false when required resources are missing or this camera should be skipped.
        protected virtual bool PreparePass(TPass pass, ref RenderingData renderingData) { return true; }

        public sealed override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!IsAddRenderPass || Pass == null ||
                !RenderFeatureCameraFilter.ShouldRender(renderingData.cameraData.camera))
                return;

            if (!PreparePass(Pass, ref renderingData))
                return;

            renderer.EnqueuePass(Pass);
        }

        public sealed override void Create()
        {
            ReleasePass();
            Pass = CreatePass();
            if (Pass != null)
                Pass.renderPassEvent = InjectionPoint;
        }

        protected sealed override void Dispose(bool disposing)
        {
            ReleasePass();
        }

        private void ReleasePass()
        {
            TPass previousPass = Pass;
            Pass = null;
            previousPass?.Dispose();
        }
    }
}
