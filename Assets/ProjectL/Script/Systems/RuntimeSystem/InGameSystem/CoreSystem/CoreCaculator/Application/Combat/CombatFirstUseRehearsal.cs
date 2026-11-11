using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 전투 첫 동작에 쓰는 계산 경로를 가상 상태에서 한 번씩 미리 실행합니다(첫 동작 Core 준비 계획 §4, S1~S8).
    /// 결과는 모두 버리며 Buffer·사건·난수 원본을 건드리지 않습니다. 계산기는 입력만으로 계산하고 정적 가변 상태가 없다는 전제 위에 있습니다.
    /// </summary>
    /// <remarks>
    /// 가상 상태는 <see cref="CombatCommandComposition.Project"/>로 잇습니다(자동 명령이 중간 결과를 게시하지 않고 다음 입력을 만드는 입구).
    /// 다음 행동의 기대 식별자(전투 번호·컨트롤러 턴 순번)와 처리 종류는 호출자가 주는 시작 입력(seed)에서 받고,
    /// 가상 세계는 공개 입구 <see cref="CombatCommandInput.WithWorld"/>·<see cref="CombatCommandInput.WithPersistentFacts"/>로 그 위에 옮깁니다.
    /// 칸·조준은 Core 자신의 조회(배치 구역·배치 미리보기·이동 범위·공격 범위) 결과에서 고르므로 규칙을 베끼지 않습니다.
    /// 공격은 사거리 안 대상이 있는 아군·조준을 먼저 골라 피해·넉백·사망 정산 경로를 지나게 하고, 첫 턴에 대상을 맞히지 못했으면
    /// 적 턴 뒤(적이 다가온 상태) 한 번 더 공격합니다.
    /// 거절은 예외가 아니며, 이어 갈 상태가 없으면 남은 의존 단계를 건너뜁니다. <see cref="RunNext"/> 한 번이 단계 하나입니다.
    /// </remarks>
    public sealed class CombatFirstUseRehearsal
    {
        // 무한 반복을 막는 상한입니다. 값이 결과를 바꾸지 않고 덮는 경로의 양만 바꿉니다.
        private const int MaxPlacementCandidates = 48;
        private const int MaxSubPlacements = 6;
        private const int MaxAdvanceSteps = 24;
        private const int MaxSteps = 96;

        private readonly Func<CombatProcessingKind, long, CombatCommandInput> _seed;
        private readonly IReadOnlyList<IMovementTileRule> _rules;
        private readonly List<EntityId> _attackers = new();
        private CombatCommandInput _world;
        private Stage _stage = Stage.PlaceMain;
        private bool _playerTurnPassed;
        private bool _struckTarget;
        private bool _retriedAttack;
        private int _subPlacements;
        private int _advanceSteps;
        private int _attackerIndex;
        private int _steps;

        /// <param name="start">지금 Buffer의 전투로 조립한 명령 입력입니다. 가상 세계의 출발점입니다.</param>
        /// <param name="seed">처리 종류와 기대 컨트롤러 턴 순번을 받아 새로 조립한 입력을 돌려줍니다. 가상 세계를 올려놓을 바탕입니다.</param>
        /// <param name="rules">명령 처리와 같은 이동 칸 규칙입니다.</param>
        public CombatFirstUseRehearsal(CombatCommandInput start, Func<CombatProcessingKind, long, CombatCommandInput> seed,
            IReadOnlyList<IMovementTileRule> rules)
        {
            _world = start ?? throw new ArgumentNullException(nameof(start));
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>남은 단계 하나를 실행합니다. 더 할 단계가 남았으면 true입니다.</summary>
        public bool RunNext()
        {
            if (_stage == Stage.Done)
                return false;

            using var sample = CoreProfiling.Sample(CoreProfileSection.FirstUseRehearsal);
            _stage = ++_steps > MaxSteps ? Stage.Done : Run(_stage);
            return _stage != Stage.Done;
        }

        private Stage Run(Stage stage)
        {
            switch (stage)
            {
                case Stage.PlaceMain:
                    // S1. 메인이 놓이지 않으면 전투 시작이 거절되므로 뒤 단계는 모두 건너뜁니다.
                    return TryPlace(FindHand(CharacterOwnershipRole.Main), CombatCommandKind.PlaceMainCharacter)
                        ? Stage.PlaceSubs
                        : Stage.Done;
                case Stage.PlaceSubs:
                    // S2. 손패의 서브를 하나씩 놓습니다. 못 놓으면 같은 유닛을 다시 고르지 않게 전투 시작으로 넘어갑니다.
                    var sub = _subPlacements < MaxSubPlacements ? FindHand(CharacterOwnershipRole.Sub) : null;
                    if (sub == null || !TryPlace(sub, CombatCommandKind.PlaceUnit))
                        return Stage.EnterCombat;
                    _subPlacements++;
                    return Stage.PlaceSubs;
                case Stage.EnterCombat:
                    // S3. 전투 시작 뒤 자동 국면을 플레이어 입력까지 진행합니다.
                    return Adopt(CombatCommandCalculator.Calculate(Act(Base(CombatProcessingKind.Command),
                        CombatCommandKind.EnterCombat, EntityId.Empty), _rules))
                        ? Stage.Advance
                        : Stage.Done;
                case Stage.Advance:
                    return Advance();
                case Stage.PlayerQueries:
                    return PlayerQueries();
                case Stage.AttackPreviews:
                    return AttackPreview();
                case Stage.Move:
                    MoveFirstAttacker();
                    return Stage.Attack;
                case Stage.Attack:
                    return AttackPreferTarget();
                case Stage.EndTurn:
                    // S8. 턴 종료 뒤 적 턴을 자동 진행으로 돌립니다. 자동 진행 상한은 구간마다 새로 셉니다.
                    if (!Adopt(CombatCommandCalculator.Calculate(Act(Base(CombatProcessingKind.Command),
                            CombatCommandKind.EndPlayerTurn, EntityId.Empty), _rules)))
                        return Stage.Done;
                    _playerTurnPassed = true;
                    _advanceSteps = 0;
                    return Stage.Advance;
                default:
                    return Stage.Done;
            }
        }

        // 자동 국면이면 한 걸음 진행합니다. 플레이어 입력에 닿으면 첫 플레이어 턴이면 S4로 갑니다.
        // 턴 종료 뒤 둘째 플레이어 턴이면, 첫 턴에 대상을 맞히지 못했을 때만 적이 다가온 상태로 한 번 더 공격하고 끝냅니다.
        private Stage Advance()
        {
            CombatCommandState state = _world.State;
            if (IsAutomatic(state))
            {
                if (++_advanceSteps > MaxAdvanceSteps)
                    return Stage.Done;
                CombatCommandKind kind = state.Turn.ControllerPhase == CombatControllerPhase.Enemy
                    ? CombatCommandKind.CompleteEnemyTurn
                    : CombatCommandKind.StartControllerTurn;
                return Adopt(CombatCommandCalculator.Advance(Act(Base(CombatProcessingKind.Advance), kind, EntityId.Empty), _rules))
                    ? Stage.Advance
                    : Stage.Done;
            }
            if (!IsPlayerTurn(state))
                return Stage.Done;
            if (_playerTurnPassed)
            {
                if (_struckTarget || _retriedAttack)
                    return Stage.Done;
                _retriedAttack = true;
                return Stage.Attack;
            }

            CollectAttackers();
            return _attackers.Count > 0 ? Stage.PlayerQueries : Stage.Done;
        }

        // S4. 첫 아군으로 행동 가능 여부·유닛 공격 범위·이동 범위를 조회합니다.
        private Stage PlayerQueries()
        {
            CombatCommandInput attack = Act(Base(CombatProcessingKind.Command), CombatCommandKind.Attack, _attackers[0]);
            _ = CombatCommandCalculator.GetActionAvailability(attack);
            _ = CombatCommandCalculator.GetUnitAttackRange(attack);
            _ = CombatMovementCalculator.Range(Act(Base(CombatProcessingKind.Command), CombatCommandKind.Move, _attackers[0]));
            return Stage.AttackPreviews;
        }

        // S6·S7. 공격 종류마다 공격 범위와 공격 미리보기를 계산합니다. 사거리 안 대상이 있으면 그 대상을 조준합니다.
        private Stage AttackPreview()
        {
            CombatCommandInput attack = Act(Base(CombatProcessingKind.Command), CombatCommandKind.Attack, _attackers[_attackerIndex]);
            if (TryAim(attack, out AttackAim aim, out _))
                _ = CombatCommandCalculator.Preview(Act(attack, CombatCommandKind.Attack, attack.ActorId, aim), _rules);
            return ++_attackerIndex < _attackers.Count ? Stage.AttackPreviews : Stage.Move;
        }

        // S5. 첫 아군을 한 걸음 거리 칸으로 옮기는 미리보기와 명령을 계산합니다. 한 걸음 칸이 없으면 건너뜁니다.
        private void MoveFirstAttacker()
        {
            CombatCommandInput baseInput = Base(CombatProcessingKind.Command);
            CombatCommandInput range = Act(baseInput, CombatCommandKind.Move, _attackers[0]);
            CharacterCombatFacts actor = FindCharacter(_attackers[0]);
            if (actor?.Anchor == null || !TryAdjacentDestination(CombatMovementCalculator.Range(range), out CellCoord target))
                return;

            var path = new MovementPath(new[] { actor.Anchor.Value, target });
            CombatCommandInput move = Act(baseInput, CombatCommandKind.Move, actor.EntityId, path: path);
            _ = CombatMovementCalculator.Preview(move, _rules);
            MovementPreviewContext context = MovementContextCalculator.Capture(move, Owner.Player, _rules);
            CombatCommandInput command = Act(baseInput, CombatCommandKind.Move, actor.EntityId, path: path, context: context);
            // 명령 처리(CombatCommandService)와 같은 순서로, 이동 맥락이 맞을 때만 계산합니다.
            if (MovementContextCalculator.Matches(context, command, Owner.Player, _rules))
                Adopt(CombatCommandCalculator.Calculate(command, _rules));
        }

        // S6. 사거리 안에 대상이 있는 첫 아군이 그 대상을 조준해 공격합니다(피해·넉백·사망·특성 정산 경로를 지나게).
        // 대상이 있는 아군이 없으면 첫 아군의 첫 조준으로 공격 명령 경로만 지납니다. 첫 턴이면 턴 종료로, 둘째 턴이면 끝입니다.
        private Stage AttackPreferTarget()
        {
            CollectAttackers();
            CombatCommandInput baseInput = Base(CombatProcessingKind.Command);
            CombatCommandInput fallback = null;
            AttackAim fallbackAim = default;
            foreach (EntityId attacker in _attackers)
            {
                CombatCommandInput attack = Act(baseInput, CombatCommandKind.Attack, attacker);
                if (!TryAim(attack, out AttackAim aim, out bool hitsTarget))
                    continue;
                if (hitsTarget)
                {
                    _struckTarget = true;
                    Adopt(CombatCommandCalculator.Calculate(Act(attack, CombatCommandKind.Attack, attacker, aim), _rules));
                    return NextAfterAttack();
                }
                if (fallback == null)
                {
                    fallback = attack;
                    fallbackAim = aim;
                }
            }
            if (fallback != null)
                Adopt(CombatCommandCalculator.Calculate(Act(fallback, CombatCommandKind.Attack, fallback.ActorId, fallbackAim), _rules));
            return NextAfterAttack();
        }

        private Stage NextAfterAttack() => _playerTurnPassed ? Stage.Done : Stage.EndTurn;

        // 배치 구역 칸을 차례로 보며 배치 미리보기가 허락한 첫 칸에 놓습니다.
        private bool TryPlace(CharacterCombatFacts unit, CombatCommandKind kind)
        {
            if (unit == null)
                return false;

            CombatCommandInput baseInput = Base(CombatProcessingKind.Command);
            CombatCommandState state = baseInput.State;
            PlayerDeploymentZone zone = baseInput.Battle.PlayerZone;
            int tried = 0;
            for (int y = 0; y < state.BoardHeight; y++)
            {
                for (int x = 0; x < state.BoardWidth; x++)
                {
                    var cell = new CellCoord(x, y);
                    if (!zone.Contains(cell))
                        continue;
                    if (++tried > MaxPlacementCandidates)
                        return false;
                    CombatCommandInput place = Act(baseInput, kind, unit.EntityId, cell: cell);
                    if (CombatPlacementCalculator.Preview(place).CanPlace)
                        return Adopt(CombatCommandCalculator.Calculate(place, _rules));
                }
            }
            return false;
        }

        // 지금 가상 세계를 새 기대 식별자의 입력 위에 옮깁니다. 보유·재고·잔액도 함께 옮깁니다.
        private CombatCommandInput Base(CombatProcessingKind processing)
        {
            CombatCommandInput seed = _seed(processing, _world.State.Turn.ControllerTurnSequence);
            return seed
                .WithWorld(_world.State, _world.Characters, _world.Main, _world.SoulCounts, _world.Souls, _world.EnvironmentBindings)
                .WithPersistentFacts(_world.Owned, _world.ItemInventory, _world.GoldBalance, _world.FameBalance);
        }

        private static CombatCommandInput Act(CombatCommandInput input, CombatCommandKind kind, EntityId actor,
            AttackAim aim = default, CellCoord cell = default, MovementPath path = null, MovementPreviewContext context = null) =>
            CombatCommandComposition.Intent(input, kind, aim, cell, path, context).WithActor(actor);

        // 수락됐고 전투가 끝나지 않았을 때만 그 결과를 다음 가상 세계로 씁니다.
        private bool Adopt(CombatCommandTransition transition)
        {
            if (!transition.Accepted || transition.Outcome.IsStageEnded)
                return false;
            _world = CombatCommandComposition.Project(transition);
            return true;
        }

        // 공격 범위의 조준을 고릅니다. 사거리 안 대상이 있으면 칸 조준은 그 대상이 선 칸을, 전체 조준은 그대로 대상을 맞힙니다.
        // 방향 조준은 어느 방향이 대상을 맞히는지 여기서 판단하지 않으므로 첫 조준이며 대상 없음으로 봅니다(hitsTarget = false).
        private static bool TryAim(CombatCommandInput attack, out AttackAim aim, out bool hitsTarget)
        {
            AttackRangeResult range = CombatCommandCalculator.GetAttackRange(attack);
            aim = default;
            hitsTarget = false;
            if (!range.Succeeded || range.AttackAims.Length == 0)
                return false;

            ReadOnlySpan<AttackAim> aims = range.AttackAims.Span;
            aim = aims[0];
            if (range.InRangeTargetIds.Length == 0)
                return true;
            if (aim.Kind == AttackAimKind.All)
            {
                hitsTarget = true;
                return true;
            }
            foreach (EntityId target in range.InRangeTargetIds.Span)
            {
                IReadOnlyList<CellCoord> cells = OccupiedCellsOf(attack, target);
                if (cells == null)
                    continue;
                foreach (AttackAim candidate in aims)
                {
                    if (candidate.Kind != AttackAimKind.Cell || !candidate.Cell.HasValue || !Contains(cells, candidate.Cell.Value))
                        continue;
                    aim = candidate;
                    hitsTarget = true;
                    return true;
                }
            }
            return true;
        }

        private static IReadOnlyList<CellCoord> OccupiedCellsOf(CombatCommandInput input, EntityId id)
        {
            foreach (CharacterCombatFacts character in input.Characters)
            {
                if (character.EntityId == id)
                    return character.OccupiedCells;
            }
            foreach (StructureCombatFacts structure in input.State.Structures)
            {
                if (structure.EntityId == id)
                    return structure.OccupiedCells;
            }
            return null;
        }

        private static bool Contains(IReadOnlyList<CellCoord> cells, CellCoord cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].Equals(cell))
                    return true;
            }
            return false;
        }

        private static bool TryAdjacentDestination(MovementRangeResult range, out CellCoord target)
        {
            target = default;
            if (!range.Succeeded)
                return false;
            foreach (MovementDestination destination in range.Destinations.Span)
            {
                if (destination.StepDistance != 1)
                    continue;
                target = destination.TargetAnchor;
                return true;
            }
            return false;
        }

        // 공격 종류(유닛 정의 키)마다 필드의 살아 있는 아군 하나씩입니다. 첫 항목이 이동·공격 명령의 행위자입니다.
        private void CollectAttackers()
        {
            _attackers.Clear();
            var kinds = new List<EntityKey>();
            foreach (CharacterCombatFacts character in _world.Characters)
            {
                if (!IsFieldAlly(character) || kinds.Contains(character.CharacterKey))
                    continue;
                kinds.Add(character.CharacterKey);
                _attackers.Add(character.EntityId);
            }
        }

        private CharacterCombatFacts FindHand(CharacterOwnershipRole role)
        {
            foreach (CharacterCombatFacts character in _world.Characters)
            {
                if (character.Owner == Owner.Player && character.Area == CharacterRuntimeArea.Hand &&
                    character.OwnershipRole == role && !character.Health.IsDead)
                    return character;
            }
            return null;
        }

        // 이동·공격 뒤에도 같은 개체가 살아 필드에 있을 때만 이어서 씁니다.
        private CharacterCombatFacts FindCharacter(EntityId id)
        {
            foreach (CharacterCombatFacts character in _world.Characters)
            {
                if (character.EntityId == id)
                    return IsFieldAlly(character) ? character : null;
            }
            return null;
        }

        private static bool IsFieldAlly(CharacterCombatFacts character) =>
            character.Owner == Owner.Player && character.Area == CharacterRuntimeArea.Field &&
            character.Anchor.HasValue && !character.Health.IsDead;

        private static bool IsAutomatic(CombatCommandState state) =>
            state.Phase == CombatFlowPhase.Automatic && state.Turn.StagePhase == StagePhase.Combat &&
            (state.Turn.ControllerPhase == CombatControllerPhase.Enemy ||
             state.Turn.ControllerPhase == CombatControllerPhase.StartingPlayer ||
             state.Turn.ControllerPhase == CombatControllerPhase.StartingEnemy);

        private static bool IsPlayerTurn(CombatCommandState state) =>
            state.Phase == CombatFlowPhase.PlayerInput && state.Turn.StagePhase == StagePhase.Combat &&
            state.Turn.ControllerPhase == CombatControllerPhase.Player;

        private enum Stage
        {
            PlaceMain,
            PlaceSubs,
            EnterCombat,
            Advance,
            PlayerQueries,
            AttackPreviews,
            Move,
            Attack,
            EndTurn,
            Done
        }
    }
}
