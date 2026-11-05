using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.ScreenTransitionSystem
{
    /// <summary>
    /// Pool에 보관된 전환 Scop을 대여합니다. 요청은 대여한 Scop에 전달합니다.
    /// </summary>
    public interface IScreenTransitionSystemService : IServiceModule
    {
        IScreenTransitionScop CreateScop();
    }

    /// <summary>
    /// 활성화된 화면 전환 Scop에만 프레임 진행을 전달합니다.
    /// Scop의 대여 수명과 Tick 참여 수명은 별개입니다.
    /// </summary>
    internal sealed class ScreenTransitionSystem :
        IScreenTransitionSystemService,
        IScreenTransitionScopService,
        IScreenTransitionScopPoolService,
        ISystemTick,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly ScreenTransitionScopPool _scopPool;
        private readonly IScreenTransitionOutputProvider _outputProvider;
        private readonly IScreenTransitionInitModule _initModule;
        private readonly HashSet<ScreenTransitionScop> _rentedScops = new();
        private readonly List<ISystemTick> _activeScops = new();
        private readonly List<ISystemTick> _pendingScops = new();
        private bool _isTicking;
        private bool _isShutdown;
        private bool _isInitialized;

        internal ScreenTransitionSystem(IScreenTransitionOutputProvider outputProvider,
            IScreenTransitionInitModule initModule)
        {
            _outputProvider = outputProvider ?? throw new ArgumentNullException(nameof(outputProvider));
            _initModule = initModule ?? throw new ArgumentNullException(nameof(initModule));
            _scopPool = new ScreenTransitionScopPool(outputProvider.Create);
        }

        public void Initialize()
        {
            ThrowIfShutdown();
            if (_isInitialized)
                throw new InvalidOperationException("화면 전환 시스템은 이미 초기화됐습니다.");
            _outputProvider.Init(_initModule.Load());
            _isInitialized = true;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var render = resolver.GetService(SystemGroupsService.Global).RenderServiceCommand.CoreGroup;
            _outputProvider.BindServices(render.CameraGroup.CameraCatalogSystem,
                render.CameraGroup.UnityCameraCatalogSystem, render.RenderFeatureSystemServices);
        }

        public IScreenTransitionScop CreateScop()
        {
            ThrowIfShutdown();
            ScreenTransitionScop scop = _scopPool.Get();
            scop.Bind(this, this);
            _rentedScops.Add(scop);
            return scop;
        }

        void IScreenTransitionScopPoolService.Return(ScreenTransitionScop scop)
        {
            if (!_rentedScops.Remove(scop))
                return;

            _scopPool.Return(scop);
        }

        void IScreenTransitionScopService.Activate(ISystemTick scop)
        {
            if (scop == null)
                throw new ArgumentNullException(nameof(scop));
            ThrowIfShutdown();

            if (Find(_activeScops, scop) >= 0 || Find(_pendingScops, scop) >= 0)
                return;

            if (_isTicking)
                _pendingScops.Add(scop);
            else
                _activeScops.Add(scop);
        }

        void IScreenTransitionScopService.Deactivate(ISystemTick scop)
        {
            if (scop == null)
                throw new ArgumentNullException(nameof(scop));

            int index = Find(_activeScops, scop);
            if (index >= 0)
            {
                if (_isTicking)
                    _activeScops[index] = null;
                else
                    _activeScops.RemoveAt(index);
            }

            index = Find(_pendingScops, scop);
            if (index >= 0)
                _pendingScops.RemoveAt(index);
        }

        public void Tick(float deltaTime)
        {
            ThrowIfShutdown();
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (_isTicking)
                throw new InvalidOperationException("화면 전환 Tick을 중첩 호출할 수 없습니다.");
            if (_activeScops.Count == 0)
                return;

            _isTicking = true;
            try
            {
                for (int i = 0; i < _activeScops.Count; i++)
                {
                    _activeScops[i]?.Tick(deltaTime);
                }
            }
            finally
            {
                CompactActiveScops();
                _activeScops.AddRange(_pendingScops);
                _pendingScops.Clear();
                _isTicking = false;
            }
        }

        public void Shutdown()
        {
            if (_isShutdown)
                return;

            _isShutdown = true;
            var rented = new ScreenTransitionScop[_rentedScops.Count];
            _rentedScops.CopyTo(rented);
            Exception failure = null;
            for (int i = 0; i < rented.Length; i++)
            {
                try
                {
                    rented[i].Dispose();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }

            _activeScops.Clear();
            _pendingScops.Clear();
            _outputProvider.Shutdown();
            if (failure != null)
                throw new InvalidOperationException("화면 전환 자원 정리 중 오류가 발생했습니다.", failure);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IScreenTransitionSystemService>(this);
            // Scop 전용 활성화 서비스와 Pool 반환 서비스는 내부 주입으로만 사용합니다.
            sink.AddModule(this);
        }

        private void CompactActiveScops()
        {
            int writeIndex = 0;
            for (int i = 0; i < _activeScops.Count; i++)
            {
                ISystemTick scop = _activeScops[i];
                if (scop != null)
                    _activeScops[writeIndex++] = scop;
            }

            _activeScops.RemoveRange(writeIndex, _activeScops.Count - writeIndex);
        }

        private static int Find(List<ISystemTick> scops, ISystemTick scop)
        {
            for (int i = 0; i < scops.Count; i++)
            {
                if (ReferenceEquals(scops[i], scop))
                    return i;
            }

            return -1;
        }

        private void ThrowIfShutdown()
        {
            if (_isShutdown)
                throw new ObjectDisposedException(nameof(ScreenTransitionSystem));
        }
    }
}
