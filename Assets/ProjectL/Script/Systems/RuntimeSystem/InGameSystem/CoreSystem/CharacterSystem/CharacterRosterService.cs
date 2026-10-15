using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    public interface ICharacterRosterService : IServiceModule
    {
        RosterAcquireResult InitializeRoster(int loadoutKey);
        RosterAcquireResult AcquireSub(EntityKey key);
        InitialDrawPlan PlanInitialDraw(int seed);
    }

    /// <summary>DB와 Buffer의 로스터 사실을 Core 규칙에 전달하고 결정된 획득을 반영한다.</summary>
    internal sealed class CharacterRosterService : ICharacterRosterService,
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

        public RosterAcquireResult InitializeRoster(int loadoutKey)
        {
            RequireReady();
            if (RunEnded) return RosterAcquireResult.Failure(RosterAcquireFailureReason.RunEnded);
            // 현재 Run에 로스터만 채운다(빈 로스터 필요). 이전 Run을 비우고 시작하는 경로는 IRunStartService.StartNewRun이다.
            return Publish(PlayerLoadoutRosterPlanner.Plan(_loadouts, _units, loadoutKey,
                _buffers.CharacterReadSink.Owned.Count == 0, out _));
        }

        public RosterAcquireResult AcquireSub(EntityKey key)
        {
            RequireReady();
            if (RunEnded) return RosterAcquireResult.Failure(RosterAcquireFailureReason.RunEnded);
            return Publish(CharacterRosterRules.PlanAcquireSub(CharacterDefinitionAdapter.Read(_units, key)));
        }

        public InitialDrawPlan PlanInitialDraw(int seed)
        {
            RequireReady();
            var candidates = new List<StageDrawCandidate>();
            foreach (OwnedCharacterRecord record in _buffers.CharacterReadSink.Owned)
                if (record.Role == CharacterOwnershipRole.Sub)
                    candidates.Add(new StageDrawCandidate(record.Id, record.CharacterKey));

            // 첫 손패 수는 새 Run 시작 때 로드아웃에서 Run에 기록된 값이다. 새 Run 시작을 거치지 않은 로스터는 기본 규칙(메인 1 + 서브 3)을 쓴다.
            int startingHandSize = _buffers.RunProgressReadSink.StartingHandSize ?? InitialDrawPlan.DefaultStartingHandSize;
            return InitialDrawCalculator.Plan(candidates, seed, startingHandSize);
        }

        private RosterAcquireResult Publish(CharacterAcquisitionDecision decision)
        {
            if (RunEnded) return RosterAcquireResult.Failure(RosterAcquireFailureReason.RunEnded);
            return decision.Succeeded
                ? _buffers.CharacterWriteSink.Acquire(decision.Requests, decision.InitialMain)
                : RosterAcquireResult.Failure(decision.FailureReason);
        }

        private bool RunEnded => _buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress;

        private void RequireReady()
        {
            if (_units == null || _loadouts == null || _buffers == null)
                throw new InvalidOperationException("로스터 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICharacterRosterService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
