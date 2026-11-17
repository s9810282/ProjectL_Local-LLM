using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem
{
    #region 서비스

    public interface IRunStartService : IServiceModule
    {
        /// <summary>새 Run에 사용할 수 있는 메인 후보와 로드아웃의 작성 기본값을 조회한다.</summary>
        RunStartMainOptions ReadMainOptions(int loadoutKey);

        /// <summary>
        /// 이전 Run을 비우고 시작 로드아웃의 로스터와 시작 Run 재화로 새 Run을 시작한다.
        /// 거절이면 아무것도 바꾸지 않는다. 다시 호출해도 매번 새로 시작하므로 중복 지급되지 않는다.
        /// 챕터 진입과 화면 전환은 호출하는 흐름이 이어서 한다.
        /// </summary>
        RosterAcquireResult StartNewRun(int loadoutKey);

        /// <summary>지정한 메인과 로드아웃의 시작 구성으로 새 Run을 시작한다.</summary>
        RosterAcquireResult StartNewRun(int loadoutKey, EntityKey mainCharacterKey);
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

        public RunStartMainOptions ReadMainOptions(int loadoutKey)
        {
            if (_units == null || _loadouts == null || _buffers == null)
                throw new InvalidOperationException("Run 시작 옵션을 읽으려면 DB 초기화와 서비스 바인딩이 필요합니다.");

            var loadout = _loadouts.GetPlayerLoadout(loadoutKey);
            loadout.Validate();

            var candidates = new List<EntityKey>();
            foreach (var unit in _units.GetAllUnits())
            {
                unit.Validate();
                if (unit.Role == UnitRole.MainCharacter)
                    candidates.Add(unit.Key);
            }

            if (!candidates.Contains(loadout.MainCharacterKey))
                throw new InvalidOperationException(
                    $"로드아웃 {loadoutKey}의 기본 메인 {loadout.MainCharacterKey}이 선택 가능한 메인 후보에 없습니다.");

            candidates.Sort();
            return new RunStartMainOptions(loadout.LoadoutKey, loadout.MainCharacterKey, candidates);
        }

        public RosterAcquireResult StartNewRun(int loadoutKey) =>
            StartNewRunCore(loadoutKey, mainCharacterOverride: null);

        public RosterAcquireResult StartNewRun(int loadoutKey, EntityKey mainCharacterKey) =>
            StartNewRunCore(loadoutKey, mainCharacterKey);

        private RosterAcquireResult StartNewRunCore(int loadoutKey, EntityKey? mainCharacterOverride)
        {
            if (_units == null || _loadouts == null || _buffers == null)
                throw new InvalidOperationException("Run 시작 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
            // 교체할 이전 Run 원본을 잡는다. 재화·Run 결과는 로스터 버전이 덮지 않으므로 따로 비교한다.
            var source = RosterSourceVersion.Capture(_buffers);
            int expectedGold = _buffers.CurrencyReadSink.Amount;
            var expectedOutcome = _buffers.RunProgressReadSink.Outcome;
            // 끝난 Run에서도 새로 시작할 수 있다. 새 Run은 이전 로스터를 비운 뒤 설치하므로 빈 로스터 기준으로 판정한다.
            CharacterAcquisitionDecision decision = PlayerLoadoutRosterPlanner.Plan(_loadouts, _units, loadoutKey,
                rosterIsEmpty: true, mainCharacterOverride, out var loadout, out var definitions);
            if (!decision.Succeeded) return RosterAcquireResult.Failure(decision.FailureReason);
            var selection = DeckRules.EvaluateInitial(loadout.InitialDeckGrantIndices, decision.Requests, definitions);
            // 작성 데이터 오류를 찾을 수 있도록 덱 규칙 사유를 그대로 돌려준다.
            if (selection.Reason != DeckFailureReason.None)
                return RosterAcquireResult.InvalidInitialDeck(selection.Reason);
            if (!source.IsCurrent(_buffers) || _buffers.CurrencyReadSink.Amount != expectedGold ||
                _buffers.RunProgressReadSink.Outcome != expectedOutcome)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.InvalidRequest);
            return _buffers.RunProgressWriteSink.PublishNewRun(
                new NewRunPreparation(new RunId(Guid.NewGuid()), decision.Requests, loadout.StartingRunCurrency, loadout.StartingHandSize, selection.CanonicalGrantIndices));
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRunStartService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
