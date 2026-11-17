using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornBombs;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 한 공격 명령이 바꾸는 세계의 투영이다. 캐릭터·구조물·가시 폭탄·영혼·사건 목록을 단계들이 함께 갱신한다.
    /// 어떤 단계가 무엇을 쓰는지는 설계 2026-10-12 D4 표를 따른다. 명령 계산이 끝나면 버린다.
    /// </summary>
    internal sealed class CommandAttackProjection
    {
        private MovementProjection _arrival;
        private CommandEnvironmentState _environment;
        private TraitTurnFacts _traitTurn;
        internal TraitTurnFacts TraitTurn
        {
            get => _arrival?.TraitTurn ?? _traitTurn;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (_arrival == null) _traitTurn = value;
                else _arrival.TraitTurn = value;
            }
        }

        // 추가 공격이 갱신하는 deliveredVictims와 분리한 일반 공격 전달 시점의 위치다.
        internal Dictionary<EntityId, ItemAttackCandidate> ItemDirectHits { get; } = new();
        internal Dictionary<EntityId, CharacterCombatFacts> Characters { get; }
        /// <summary>이 공격과 파생 피해 전체가 공유하는 피해 적용 경계다. 도착 투영·장애물 반응도 이 인스턴스를 쓴다.</summary>
        internal CombatDamageApplication Damage { get; private set; }
        internal ThornBombResolution ThornBombs { get; private set; }
        internal Dictionary<EntityId, StructureCombatFacts> Structures { get; }
        internal Dictionary<CharacterInstanceId, int> SoulCounts { get; }
        internal SoulFieldSnapshot SoulField { get; private set; }
        internal List<long> ConsumedSouls { get; }
        internal List<CombatEvent> Events { get; }
        internal Dictionary<EntityId, CellCoord> DeathAnchors { get; }
        internal CommandAttackGroups AttackGroups { get; }
        /// <summary>이 공격과 그 파생 피해(다단·넉백 경로·도착 환경·장애물 폭발·처치 연쇄) 전체의 포효 피격 원인이다(D7, G14).</summary>
        internal CombatHitCause HitCause { get; }
        /// <summary>공격과 도착 투영이 한 명령 동안 공유하는 장애물 반응이다.</summary>
        internal ObstacleReactionCalculator Obstacles { get; private set; }
        internal SoulEffectCalculator Souls { get; private set; }
        internal List<AttackBlockedContact> BlockedContacts { get; }

        internal CommandAttackProjection(CombatCommandInput input, MovementEnvironmentFacts environment,
            IEnumerable<AttackBlockedContact> blockedContacts, bool captureMovementUses = true)
            : this()
        {
            using var work = new Construction(this, input, environment, blockedContacts, captureMovementUses);
            while (!work.IsComplete) work.Step();
        }

        private CommandAttackProjection()
        {
            Characters = new Dictionary<EntityId, CharacterCombatFacts>();
            SoulCounts = new Dictionary<CharacterInstanceId, int>();
            ConsumedSouls = new List<long>();
            Events = new List<CombatEvent>();
            DeathAnchors = new Dictionary<EntityId, CellCoord>();
            Structures = new Dictionary<EntityId, StructureCombatFacts>();
            AttackGroups = new CommandAttackGroups();
            HitCause = new CombatHitCause();
            BlockedContacts = new List<AttackBlockedContact>();
        }

        internal static Construction BeginCreate(CombatCommandInput input, MovementEnvironmentFacts environment,
            IEnumerable<AttackBlockedContact> blockedContacts, bool captureMovementUses = true) =>
            new Construction(null, input, environment, blockedContacts, captureMovementUses);

        /// <summary>완성 전에는 투영을 넘기지 않고 원본 컬렉션의 항목을 하나씩 포착한다.</summary>
        internal sealed class Construction : IDisposable
        {
            private IEnumerator<int> _steps;
            private CommandAttackProjection _result;
            private bool _complete;
            private bool _disposed;
            private bool _faulted;
            private long _version;
            internal bool IsComplete
            {
                get
                {
                    RequireActive();
                    return _complete;
                }
            }
            internal long WorkVersion
            {
                get
                {
                    RequireActive();
                    return _version;
                }
            }
            internal CommandAttackProjection Result
            {
                get
                {
                    RequireActive();
                    if (!_complete) throw new InvalidOperationException("투영 생성이 끝나지 않았습니다.");
                    return _result;
                }
            }

            internal Construction(CommandAttackProjection target, CombatCommandInput input, MovementEnvironmentFacts environment,
                IEnumerable<AttackBlockedContact> blockedContacts, bool captureMovementUses)
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                _steps = Build(target, input, environment, blockedContacts, captureMovementUses).GetEnumerator();
            }

            internal void Step()
            {
                RequireActive();
                if (_complete) return;
                using var sample = CoreProfiling.Sample(CoreProfileSection.AttackProjectionSetup);
                try
                {
                    _complete = !_steps.MoveNext();
                    _version++;
                    if (_complete)
                    {
                        _steps.Dispose();
                        _steps = null;
                    }
                }
                catch
                {
                    _faulted = true;
                    _steps?.Dispose();
                    _steps = null;
                    _result = null;
                    throw;
                }
            }

            private IEnumerable<int> Build(CommandAttackProjection target, CombatCommandInput input, MovementEnvironmentFacts environment,
                IEnumerable<AttackBlockedContact> blockedContacts, bool captureMovementUses)
            {
                _result = target ?? new CommandAttackProjection();
                _result._traitTurn = input.State.TraitTurn;
                _result.ThornBombs = new ThornBombResolution(input.State.ThornBombs);
                _result.SoulField = input.Souls;
                yield return 0;
                foreach (var character in input.Characters)
                {
                    _result.Characters.Add(character.EntityId, character);
                    yield return 0;
                }
                foreach (var count in input.SoulCounts)
                {
                    _result.SoulCounts.Add(count.Key, count.Value);
                    yield return 0;
                }
                _result.Damage = CombatDamageApplication.For(input, _result.Characters, _result.Events, captureMovementUses);
                yield return 0;
                foreach (var structure in input.State.Structures)
                {
                    _result.Structures.Add(structure.EntityId, structure);
                    yield return 0;
                }
                _result._environment = new CommandEnvironmentState(environment, input.EnvironmentBindings, input.State.EnvironmentProgress);
                _result.Obstacles = new ObstacleReactionCalculator(input, _result._environment, _result.Damage, _result.Structures,
                    _result.DeathAnchors, _result.AttackGroups, _result.HitCause);
                yield return 0;
                _result.Souls = new SoulEffectCalculator(input.Artifacts);
                yield return 0;
                foreach (var contact in blockedContacts ?? Array.Empty<AttackBlockedContact>())
                { _result.BlockedContacts.Add(contact); yield return 0; }
            }

            private void RequireActive()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Construction));
                if (_faulted) throw new InvalidOperationException("실패한 투영 생성은 다시 진행할 수 없습니다.");
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _steps?.Dispose();
                _steps = null;
                _result = null;
            }
        }

        /// <summary>
        /// 같은 사전 위에 도착 투영을 한 번만 만든다. 사전 이동이 있으면 직접 피해 전에 만들어 이후 실행기에 전달한다.
        /// 직접 피해 사망은 실행기의 DeferDeathsUntilCompletion으로 마지막 Finish에서 정산한다(I-3).
        /// 원본 명령 상태를 넘겨 부착 폭탄과 다음 식별자도 도착·미리보기 투영에 보존한다.
        /// 두 번 만들면 환경·사망 정산 경계가 흐려지므로 거부한다.
        /// </summary>
        internal MovementProjection CreateArrivalProjection(CommandAttackPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (_arrival != null) throw new InvalidOperationException("도착 투영은 공격 명령마다 한 번만 만들 수 있습니다.");
            _arrival = new MovementProjection(plan.Input, plan.Input.State, Damage, Structures, SoulCounts,
                SoulField, ConsumedSouls, DeathAnchors, Souls, AttackGroups, _environment, HitCause, Obstacles, ThornBombs, plan.Rules);
            _arrival.TraitTurn = _traitTurn;
            // 공개 미리보기의 관찰자는 계획이 정한다(기본 공격자 진영, 포효 예고는 플레이어 — 포효 예고 계획 TP-2).
            _arrival.TornadoObserver = plan.Input.CalculationObserver ?? (plan.PublicPreview ? plan.PreviewObserver : (Owner?)null);
            // 기존 공개 미리보기의 생략은 유지하고, 완전 계산은 관찰 가능한 세계 안에서 사망 효과도 실행합니다.
            _arrival.DeathTraitsEnabled = plan.CompleteConsequences;
            _traitTurn = null;
            return _arrival;
        }
    }
}
