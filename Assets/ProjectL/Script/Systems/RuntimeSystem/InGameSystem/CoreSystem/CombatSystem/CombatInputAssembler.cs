using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Linq;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 기능 소유자가 해석한 요청과 현재 Buffer 사실을 기존 전투 계산 입력으로 묶는다.
    /// 요청 수락, 요청 종류 해석, 행위자 보충, 사용처별 정책 분기를 하지 않는다.
    /// </summary>
    internal sealed class CombatInputAssembler : ISystemPostBindService
    {
        private readonly IBattleDefinitionRead _definitions;
        private readonly ArtifactLoadoutReader _loadout;
        private ProjectL.Systems.RuntimeSystem.BufferSystems.Run.IItemInventoryReadSink _items;
        private ProjectL.Systems.RuntimeSystem.BufferSystems.Currency.ICurrencyBufferReadSink _currency;
        private ProjectL.Systems.RuntimeSystem.BufferSystems.Fame.IFameBufferReadSink _fame;
        private ICharacterBufferReadSink _characters;
        private ICombatStateBufferReadSink _combat;
        private IArtifactBufferReadSink _artifacts;

        internal CombatInputAssembler(IBattleDefinitionRead definitions, ArtifactLoadoutReader loadout)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _currency = buffers.CurrencyReadSink;
            _fame = buffers.FameReadSink;
            _characters = buffers.CharacterReadSink;
            _items = buffers.ItemInventoryReadSink;
            _combat = buffers.CombatStateReadSink;
            _artifacts = buffers.ArtifactReadSink;
        }

        internal CombatCommandInput AssembleDecision(CombatActionRequest request, out EnemyDecisionSourceStamp stamp)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            RequireBound();
            stamp = null;
            if (!_combat.HasBattle || _combat.Completion != null) return null;
            var source = new EnemyDecisionSourceStamp(_combat, _characters, _definitions, _loadout,
                _artifacts.Current, _items.Current, _currency.Amount, _fame.Amount);
            if (!IsCurrent(source)) return null;
            var input = Assemble(request);
            if (!IsCurrent(source)) return null;
            stamp = source;
            return input;
        }

        internal bool IsCurrent(EnemyDecisionSourceStamp stamp)
        {
            RequireBound();
            using var sample = CoreProfiling.Sample(CoreProfileSection.EnemySourceValidation);
            return stamp != null && stamp.Matches(_combat, _characters, _definitions, _loadout,
                _artifacts.Current, _items.Current, _currency.Amount, _fame.Amount);
        }

        private void RequireBound()
        {
            if (_combat == null || _characters == null || _artifacts == null || _items == null || _currency == null || _fame == null)
                throw new InvalidOperationException("전투 입력 조립은 Buffer 바인딩이 필요합니다.");
        }

        public CombatCommandInput Assemble(CombatActionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (_combat == null || _characters == null || _artifacts == null)
                throw new InvalidOperationException("전투 입력 조립은 Buffer 바인딩이 필요합니다.");
            using var sample = CoreProfilerMarkers.Assemble.Auto();
            if (!_combat.HasBattle)
                throw new InvalidOperationException("활성 전투가 없습니다.");
            // 조립을 시작한 전투를 먼저 고정한다. 조립 도중 전투가 바뀌면 호출자의 재검증이 이 값과 비교해 결과를 버린다.
            long battleInstanceId = _combat.BattleInstanceId;
            var battle = _combat.Current;
            var membership = battle.Membership ?? throw new InvalidOperationException("전투 진입 참가 목록이 없습니다.");
            var capturedMainId = membership.MainId;
            var owned = membership.OrderedIds.Select(id => _characters.TryGetOwned(id, out var record) ? record :
                throw new InvalidOperationException("전투 참가자의 보유 기록이 없습니다.")).ToArray();
            if (!_characters.TryGetMainState(capturedMainId, out var main))
                throw new InvalidOperationException("메인 지속 상태가 필요합니다.");
            BattleInitialization definitions = _definitions.Require(battleInstanceId)
                ?? throw new InvalidOperationException("전투 정의 읽기는 정의를 돌려줘야 합니다.");

            var characters = _characters.Characters.Select(character => character.ToFacts()).ToArray();
            var souls = owned.ToDictionary(o => o.Id, o => _characters.GetSoulCount(o.Id));
            var loadout = _loadout.Read();
            // 환경 원본은 설치된 CommandState에서 읽는다. 정의를 다시 읽어 소비된 원본을 되살리지 않는다.
            var context = new CombatBattleFacts(battle.ReserveCapacity, battle.TimelineSeed,
                battle.PlayerZone, battle.DrawOrder, battle.DrawCursor,
                battle.RailCells, battle.StageGimmicks, definitions.Categories, definitions.Progression, definitions.ClearRewards,
                battle.NexusReinforcements);
            return new CombatCommandInput(request.Number, request.Kind, request.ActorId,
                request.Aim, battle.CommandState, characters,
                OwnedUnitSpecResolver.BuildProfiles(definitions.AttackProfiles, owned, definitions.TraitGrowth), main, owned,
                souls, _combat.SoulField, loadout, request.Cell, request.Rotation, context, request.ProcessingKind,
                request.Path, request.MovementContext, battleInstanceId,
                _artifacts.Current.PlacedItems.Select(i => new ArtifactItemState(i.ArtifactId, i.InstanceId,
                    true, default, default, Array.Empty<CellCoord>())).ToArray(), battle.EnvironmentBindings, battle.ObstacleContents, request.ItemKey, _items.Current, definitions.ItemDefinitions, _currency.Amount, _fame.Amount, membership,
                expectedBattleInstanceId: request.ExpectedBattleInstanceId,
                expectedControllerTurnSequence: request.ExpectedControllerTurnSequence,
                expectedMoveCommandNumber: request.ExpectedMoveCommandNumber);
        }

    }
}
