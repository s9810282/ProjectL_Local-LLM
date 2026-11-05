using System;
using ProjectL.Framework.Utility;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    internal sealed class ScreenTransitionScopPool
    {
        private sealed class Lifecycle : IPoolLifecycle<ScreenTransitionScop>
        {
            public void OnGet(ScreenTransitionScop scop) { }
            public void OnBeforeReturn(ScreenTransitionScop scop) => scop.Clear();
            public void OnReturn(ScreenTransitionScop scop) { }
        }

        private readonly CustomPool<ScreenTransitionScop> _pool;

        internal ScreenTransitionScopPool(Func<IScreenTransitionOutput> createOutput)
        {
            if (createOutput == null)
                throw new ArgumentNullException(nameof(createOutput));
            _pool = new CustomPool<ScreenTransitionScop>(new PoolContext<ScreenTransitionScop>(
                () => new ScreenTransitionScop(createOutput()), 0, null, new Lifecycle()));
        }

        internal ScreenTransitionScop Get() => _pool.Get();
        internal void Return(ScreenTransitionScop scop) => _pool.Return(scop);
    }
}
