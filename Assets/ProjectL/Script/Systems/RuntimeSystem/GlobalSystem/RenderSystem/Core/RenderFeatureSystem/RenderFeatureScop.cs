using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.RenderFeatureSystem
{
    public interface IRenderFeatureScop : IDisposable
    {
        TService UseRenderFeature<TService>(RenderFeatureKey<TService> key)
            where TService : class, IServiceModule;

        void ReleaseRenderFeature<TService>(RenderFeatureKey<TService> key)
            where TService : class, IServiceModule;

        void ReleaseAll();
    }

    internal interface IRenderFeatureScopService
    {
        TService UseRenderFeature<TService>(RenderFeatureKey<TService> key)
            where TService : class, IServiceModule;

        void ReleaseRenderFeature(RenderFeatureType featureType);
        void ReturnScop(RenderFeatureScop scop);
    }

    public sealed class RenderFeatureScop : IRenderFeatureScop
    {
        private readonly IRenderFeatureScopService _system;
        private readonly Dictionary<RenderFeatureType, int> _useCounts = new();
        private bool _isDisposed;

        internal RenderFeatureScop(IRenderFeatureScopService system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public TService UseRenderFeature<TService>(RenderFeatureKey<TService> key)
            where TService : class, IServiceModule
        {
            ThrowIfDisposed();

            TService service = _system.UseRenderFeature(key);
            _useCounts.TryGetValue(key.FeatureType, out int count);
            _useCounts[key.FeatureType] = count + 1;
            return service;
        }

        public void ReleaseRenderFeature<TService>(RenderFeatureKey<TService> key)
            where TService : class, IServiceModule
        {
            ThrowIfDisposed();

            if (!_useCounts.TryGetValue(key.FeatureType, out int count) || count == 0)
            {
                throw new InvalidOperationException(
                    $"[RenderFeatureScop] Feature was not acquired by this scope: {key}");
            }

            _system.ReleaseRenderFeature(key.FeatureType);
            if (count == 1)
                _useCounts.Remove(key.FeatureType);
            else
                _useCounts[key.FeatureType] = count - 1;
        }

        public void ReleaseAll()
        {
            ThrowIfDisposed();

            foreach ((RenderFeatureType featureType, int count) in _useCounts)
            {
                for (int i = 0; i < count; i++)
                    _system.ReleaseRenderFeature(featureType);
            }

            _useCounts.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            ReleaseAll();
            _isDisposed = true;
            _system.ReturnScop(this);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RenderFeatureScop));
        }
    }
}
