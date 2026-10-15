using System.Collections.Generic;

namespace ProjectL.Framework.SystemBase
{
    /// <summary>
    /// 시스템 모듈 저장소의 읽기 전용 인터페이스입니다.
    /// 프레임워크 내부 엔진이나 외부 컴포넌트가 생명주기별 모듈 목록을 조회할 때 사용됩니다.
    /// </summary>
    public interface ISystemModuleStoreReader
    {
        #region 시스템 모듈

        IReadOnlyList<ISystemModule> Modules { get; }

        IReadOnlyList<ISystemBindScene> BindSceneModules { get; }

        IReadOnlyList<ISystemInitData> InitDataModules { get; }

        IReadOnlyList<ISystemPostBindService> PostBindServiceModules { get; }

        IReadOnlyList<ISystemInitialize> InitializeModules { get; }

        IReadOnlyList<ISystemTick> TickModules { get; }

        IReadOnlyList<ISystemShutdown> ShutdownModules { get; }

        #endregion
    }

    internal sealed class SystemModuleStore : ISystemModuleStoreReader
    {
        private readonly IReadOnlyList<ISystemModule> _modules;

        private readonly IReadOnlyList<ISystemBindScene> _bindSceneModules;
        private readonly IReadOnlyList<ISystemInitData> _initDataModules;
        private readonly IReadOnlyList<ISystemPostBindService> _postBindServiceModules;
        private readonly IReadOnlyList<ISystemInitialize> _initializeModules;
        private readonly IReadOnlyList<ISystemTick> _tickModules;
        private readonly IReadOnlyList<ISystemShutdown> _shutdownModules;

        #region 시스템 모듈

        public IReadOnlyList<ISystemModule> Modules =>
            _modules;

        public IReadOnlyList<ISystemBindScene> BindSceneModules =>
            _bindSceneModules;

        public IReadOnlyList<ISystemInitData> InitDataModules =>
            _initDataModules;

        public IReadOnlyList<ISystemPostBindService> PostBindServiceModules =>
            _postBindServiceModules;

        public IReadOnlyList<ISystemInitialize> InitializeModules =>
            _initializeModules;

        public IReadOnlyList<ISystemTick> TickModules =>
            _tickModules;

        public IReadOnlyList<ISystemShutdown> ShutdownModules =>
            _shutdownModules;

        #endregion

        public SystemModuleStore(
            IReadOnlyList<ISystemModule> modules)
        {
            if (modules == null)
            {
                throw new System.ArgumentNullException(nameof(modules));
            }

            ISystemModule[] moduleCopy =
                new ISystemModule[modules.Count];

            for (int i = 0; i < modules.Count; i++)
            {
                ISystemModule module = modules[i];

                if (module == null)
                {
                    throw new System.InvalidOperationException(
                        $"[{nameof(SystemModuleStore)}] " +
                        $"Module is null. Index: {i}");
                }

                moduleCopy[i] = module;
            }

            _modules =
                System.Array.AsReadOnly(moduleCopy);

            _bindSceneModules =
                Collect<ISystemBindScene>(_modules);

            _initDataModules =
                Collect<ISystemInitData>(_modules);

            _postBindServiceModules =
                Collect<ISystemPostBindService>(_modules);

            _initializeModules =
                Collect<ISystemInitialize>(_modules);

            _tickModules =
                Collect<ISystemTick>(_modules);

            _shutdownModules =
                Collect<ISystemShutdown>(_modules);
        }

        private static IReadOnlyList<TModule> Collect<TModule>(
            IReadOnlyList<ISystemModule> modules)
            where TModule : class
        {
            List<TModule> collectedModules =
                new List<TModule>();

            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is TModule module)
                {
                    collectedModules.Add(module);
                }
            }

            return System.Array.AsReadOnly(
                collectedModules.ToArray());
        }
    }
}
