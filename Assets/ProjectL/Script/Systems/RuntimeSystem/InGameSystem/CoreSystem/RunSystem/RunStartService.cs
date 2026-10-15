using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem
{
    #region 서비스

    public interface IRunStartService : IServiceModule
    {
        /// <summary>
        /// 이전 Run을 비우고 시작 로드아웃의 로스터와 시작 Run 재화로 새 Run을 시작한다.
        /// 거절이면 아무것도 바꾸지 않는다. 다시 호출해도 매번 새로 시작하므로 중복 지급되지 않는다.
        /// 챕터 진입과 화면 전환은 호출하는 흐름이 이어서 한다.
        /// </summary>
        RosterAcquireResult StartNewRun(int loadoutKey);
    }

    #endregion

    #region 시스템 모듈

    /// <summary>새 Run 시작 데이터(로드아웃 로스터·시작 Run 재화)를 DB에서 읽고 Core 규칙으로 판정해 Buffer에 제안한다.</summary>
    internal sealed class RunStartService : IRunStartService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private IUnitTableQuery _units;
        private IPlayerLoadoutTableQuery _loadouts;
        private IBufferQuery _buffers;

        public void InitData(IDataBaseQueryContext data)
        {
            var query = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));
            _units = query.UnitTableQuery;
            _loadouts = query.PlayerLoadoutTableQuery;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
        }

        public RosterAcquireResult StartNewRun(int loadoutKey)
        {
            if (_units == null || _loadouts == null || _buffers == null)
                throw new InvalidOperationException("Run 시작 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
            // 끝난 Run에서도 새로 시작할 수 있다. 새 Run은 이전 로스터를 비운 뒤 설치하므로 빈 로스터 기준으로 판정한다.
            CharacterAcquisitionDecision decision = PlayerLoadoutRosterPlanner.Plan(_loadouts, _units, loadoutKey,
                rosterIsEmpty: true, out var loadout);
            if (!decision.Succeeded) return RosterAcquireResult.Failure(decision.FailureReason);
            return _buffers.RunProgressWriteSink.PublishNewRun(
                new NewRunPreparation(decision.Requests, decision.InitialMain, loadout.StartingRunCurrency, loadout.StartingHandSize));
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRunStartService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
