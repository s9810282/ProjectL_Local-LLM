using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem
{
    public interface IRenderFeatureSystemService : IServiceModule
    {
        IRenderFeatureScop CreateScop();
    }

    internal sealed class RenderFeatureSystem :
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices,
        IRenderFeatureSystemService,
        IRenderFeatureScopService
    {
        private sealed class FeatureEntry
        {
            public IRenderFeatureNotifireService Feature;
            public int UseCount;
        }

        private readonly Dictionary<RenderFeatureType, FeatureEntry> _features = new();
        private readonly HashSet<RenderFeatureScop> _scopes = new();
        private bool _isInitialized;

        public void Initialize()
        {
            if (_isInitialized)
                return;

            RegisterRendererFeatures();
            _isInitialized = true;
        }

        public void Shutdown()
        {
            RenderFeatureScop[] scopes = new RenderFeatureScop[_scopes.Count];
            _scopes.CopyTo(scopes);
            for (int i = 0; i < scopes.Length; i++)
                scopes[i].Dispose();

            foreach (FeatureEntry entry in _features.Values)
            {
                entry.UseCount = 0;
                entry.Feature.UseRenderFreature(false);
            }

            _scopes.Clear();
            _features.Clear();
            _isInitialized = false;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRenderFeatureSystemService>(this);
            sink.AddModule(this);
        }

        public IRenderFeatureScop CreateScop()
        {
            ThrowIfNotInitialized();

            var scop = new RenderFeatureScop(this);
            _scopes.Add(scop);
            return scop;
        }

        TService IRenderFeatureScopService.UseRenderFeature<TService>(RenderFeatureKey<TService> key)
        {
            ThrowIfNotInitialized();

            if (!_features.TryGetValue(key.FeatureType, out FeatureEntry entry))
            {
                throw new KeyNotFoundException(
                    $"[RenderFeatureSystem] Renderer Feature is not registered: {key}");
            }

            if (entry.Feature.SettingService is not TService settingService)
            {
                throw new InvalidOperationException(
                    $"[RenderFeatureSystem] Setting service type does not match key. " +
                    $"Feature: {key}, Requested: {typeof(TService).Name}, " +
                    $"Actual: {entry.Feature.SettingService.GetType().Name}");
            }

            if (entry.UseCount++ == 0)
                entry.Feature.UseRenderFreature(true);

            return settingService;
        }

        void IRenderFeatureScopService.ReleaseRenderFeature(RenderFeatureType featureType)
        {
            ThrowIfNotInitialized();

            if (!_features.TryGetValue(featureType, out FeatureEntry entry) || entry.UseCount == 0)
            {
                throw new InvalidOperationException(
                    $"[RenderFeatureSystem] Renderer Feature is not in use: {featureType}");
            }

            if (--entry.UseCount == 0)
                entry.Feature.UseRenderFreature(false);
        }

        void IRenderFeatureScopService.ReturnScop(RenderFeatureScop scop)
        {
            _scopes.Remove(scop);
        }

        private void RegisterRendererFeatures()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline)
            {
                throw new InvalidOperationException(
                    "[RenderFeatureSystem] An active Universal Render Pipeline renderer is required.");
            }

            ReadOnlySpan<ScriptableRendererData> rendererDataList = pipeline.rendererDataList;
            for (int dataIndex = 0; dataIndex < rendererDataList.Length; dataIndex++)
            {
                ScriptableRendererData rendererData = rendererDataList[dataIndex];
                if (rendererData == null)
                    continue;

                RegisterRendererFeatures(rendererData.rendererFeatures);
            }

            if (_features.Count == 0)
            {
                throw new InvalidOperationException(
                    "[RenderFeatureSystem] No managed Renderer Features were found in the active URP Renderer Data.");
            }
        }

        private void RegisterRendererFeatures(IReadOnlyList<ScriptableRendererFeature> rendererFeatures)
        {
            for (int i = 0; i < rendererFeatures.Count; i++)
            {
                if (rendererFeatures[i] is not IRenderFeatureNotifireService feature ||
                    feature.Type == RenderFeatureType.None)
                {
                    continue;
                }

                if (!_features.TryAdd(feature.Type, new FeatureEntry { Feature = feature }))
                {
                    throw new InvalidOperationException(
                        $"[RenderFeatureSystem] Duplicate Renderer Feature type: {feature.Type}");
                }

                feature.UseRenderFreature(false);
            }
        }

        private void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(
                    "[RenderFeatureSystem] Initialize must be called before creating a scope.");
        }
    }
}
