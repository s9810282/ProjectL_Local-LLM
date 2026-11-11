using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>적이 관측한 현재 자리에서 칸별 최대 단일 공격 노출을 추정한다. 이동 후 공격과 대상별 방어 정산은 평가하지 않는다.</summary>
    internal sealed class EnemyThreatField
    {
        private readonly Dictionary<CellCoord, int> _damageByCell;
        private readonly CombatAssessmentGap _gaps;

        private EnemyThreatField(Dictionary<CellCoord, int> damageByCell, CombatAssessmentGap gaps)
        {
            _damageByCell = damageByCell;
            _gaps = gaps;
        }

        internal int CoveredCellCount => _damageByCell.Count;

        internal Exposure Evaluate(CharacterCombatFacts actor, CellCoord endpoint)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            int damage = _damageByCell.GetValueOrDefault(endpoint);
            if (actor.Anchor.HasValue)
            {
                var offset = endpoint - actor.Anchor.Value;
                foreach (var cell in actor.OccupiedCells)
                    damage = Math.Max(damage, _damageByCell.GetValueOrDefault(cell + offset));
            }
            int loss = (int)(Math.Min(damage, actor.Health.CurrentHealth) * 1000L / actor.Health.MaxHealth);
            return new Exposure(loss, _gaps, damage > 0 && damage >= actor.Health.CurrentHealth);
        }

        internal readonly struct Exposure
        {
            internal int EstimatedLossPermille { get; }
            internal CombatAssessmentGap Gaps { get; }
            /// <summary>예상 피해가 남은 체력 이상이다(다음 기회에 처치될 수 있다, 설계 6.5).</summary>
            internal bool IsLethal { get; }

            internal Exposure(int estimatedLossPermille, CombatAssessmentGap gaps, bool isLethal = false)
            {
                EstimatedLossPermille = estimatedLossPermille;
                Gaps = gaps;
                IsLethal = isLethal;
            }
        }

        /// <summary>
        /// 위협 필드 준비(<see cref="Builder"/>)가 읽는 사실의 사본이다(설계 6.8 읽기 집합). 같은 진영 안에서 이 값이 모두 같으면 이전 필드를 다시 쓴다.
        /// - 전투 상태 전체(국면·환경·벽 막힘·기믹의 하붑 구름·구조물·판 크기 등, 관측 환경과 벽·고저의 원본)와 환경 의미 연결.
        /// - 입력의 명령 종류·행위자(효과 범위 점검이 실제 공격의 행위자 규칙을 본다), 프로필 목록·보유 기록, 배치 Artifact.
        /// - 모든 개체의 존재 요약(여러 칸 유닛 보류, 효과 범위 점검의 살아 있는 개체와 그 프로필, 유물 공격력의 생존 목록).
        /// - 플레이어 개체 전부의 사실과 영혼 수(공격자 준비·프로필·공통 피해·사거리·지정 정책).
        /// - 공격 가능한 플레이어 중 스플래시나 관통이 있으면 모든 개체의 사실(스플래시 중심 자격·관통 조우가 적 위치를 읽는다).
        /// 효과 범위 점검의 상태·직군 일치 검사는 전투 준비가 보장하는 불변식이라 따로 담지 않는다.
        /// <see cref="Builder"/>가 읽는 값을 바꾸면 여기도 고친다.
        /// </summary>
        internal sealed class ReadSet
        {
            private readonly CombatCommandState _state;
            private readonly EnvironmentSemanticBindings _bindings;
            private readonly CombatCommandKind _kind;
            private readonly EntityId _actorId;
            private readonly IReadOnlyList<CombatAttackProfile> _profiles;
            private readonly IReadOnlyList<OwnedCharacterRecord> _owned;
            private readonly ArtifactLoadoutSnapshot _artifacts;
            private readonly EnemyReadSetValues.Presence[] _presence;
            private readonly CharacterCombatFacts[] _players;
            private readonly int[] _playerSouls;
            private readonly CharacterCombatFacts[] _all;

            private ReadSet(CombatCommandInput input)
            {
                _state = input.State;
                _bindings = input.EnvironmentBindings;
                _kind = input.Kind;
                // 효과 범위 점검은 공격 입력일 때만 행위자를 읽는다(실제 공격 행위자의 넉백 규칙).
                _actorId = input.Kind == CombatCommandKind.Attack ? input.ActorId : default;
                _profiles = input.Profiles;
                _owned = input.Owned;
                _artifacts = input.Artifacts;
                _presence = new EnemyReadSetValues.Presence[input.Characters.Count];
                var players = new List<CharacterCombatFacts>();
                var souls = new List<int>();
                bool wide = false;
                for (int i = 0; i < input.Characters.Count; i++)
                {
                    var unit = input.Characters[i];
                    _presence[i] = EnemyReadSetValues.PresenceOf(unit);
                    if (unit.Owner != Owner.Player) continue;
                    players.Add(unit);
                    souls.Add(unit.OwnedId.HasValue && input.SoulCounts.TryGetValue(unit.OwnedId.Value, out int soul) ? soul : -1);
                    // 준비가 프로필을 읽는 플레이어(필드에 살아 있고 공격 봉쇄가 없음)만 본다. 프로필 선택 규칙은 실제 계산과 같은 함수다.
                    if (unit.Area != CharacterRuntimeArea.Field || unit.Health.IsDead || CombatActionAdmission.IsAttackRestricted(unit)) continue;
                    var profile = CombatCommandCalculator.RequiredProfile(input, unit);
                    wide |= profile.UsesPiercingShot || profile.Method.Splash != TargetingSplash.None;
                }
                _players = players.ToArray();
                _playerSouls = souls.ToArray();
                if (wide)
                {
                    _all = new CharacterCombatFacts[input.Characters.Count];
                    for (int i = 0; i < _all.Length; i++) _all[i] = input.Characters[i];
                }
            }

            internal static ReadSet Capture(CombatCommandInput input) =>
                new ReadSet(input ?? throw new ArgumentNullException(nameof(input)));

            internal bool Matches(ReadSet other) => Difference(other) == null;

            /// <summary>처음 다른 항목의 이름이다. 모두 같으면 null이다(측정이 놓친 이유를 센다).</summary>
            internal string Difference(ReadSet other)
            {
                if (other == null) return "none";
                if (_kind != other._kind || _actorId != other._actorId) return "intent";
                if (!EnemyReadSetValues.SameCharacters(_players, other._players) || !_playerSouls.SequenceEqual(other._playerSouls)) return "players";
                if (!_presence.SequenceEqual(other._presence)) return "presence";
                if ((_all == null) != (other._all == null) || _all != null && !EnemyReadSetValues.SameCharacters(_all, other._all)) return "characters";
                if (!_state.SameGameplayValues(other._state)) return "state";
                if (!_bindings.SameValues(other._bindings)) return "bindings";
                if (!EnemyReadSetValues.SameProfiles(_profiles, other._profiles)) return "profiles";
                if (!EnemyReadSetValues.SameOwned(_owned, other._owned)) return "owned";
                if (!EnemyReadSetValues.SameArtifacts(_artifacts, other._artifacts)) return "artifacts";
                return null;
            }
        }

        internal sealed class Builder : IDisposable
        {
            private IEnumerator<int> _steps;
            private EnemyThreatField _result;
            private bool _complete;
            private bool _disposed;

            internal Builder(EnemyDecisionContext context)
            {
                if (context == null) throw new ArgumentNullException(nameof(context));
                _steps = Prepare(context).GetEnumerator();
            }

            internal bool IsComplete => _complete;
            internal long WorkVersion { get; private set; }

            internal void Step()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Builder));
                if (_complete) return;
                try
                {
                    _complete = !_steps.MoveNext();
                    WorkVersion++;
                    if (_complete)
                    {
                        _steps.Dispose();
                        _steps = null;
                    }
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal EnemyThreatField Build()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Builder));
                if (!_complete) throw new InvalidOperationException("위협 필드 준비가 끝나지 않았습니다.");
                return _result;
            }

            private IEnumerable<int> Prepare(EnemyDecisionContext context)
            {
                var input = context.Input;
                var damageByCell = new Dictionary<CellCoord, int>();
                var gaps = CombatAssessmentGap.FuturePlayerChoice | CombatAssessmentGap.HiddenEnvironment;
                _result = new EnemyThreatField(damageByCell, gaps);
                if (input.State.Phase == CombatFlowPhase.Finished) yield break;

                foreach (var unit in input.Characters)
                {
                    if (CombatAttackQueries.DefersMultiCellAttack(unit))
                    {
                        _result = new EnemyThreatField(damageByCell, gaps | CombatAssessmentGap.UnsupportedEffect);
                        yield break;
                    }
                    yield return 0;
                }

                EnemyDecisionContext.ObservedGeometry geometry = null;
                foreach (var _ in context.GeometrySteps(value => geometry = value)) yield return 0;
                var visible = geometry.Environment;
                var observed = input.WithWorld(input.State.With(environment: visible), input.Characters,
                    input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
                foreach (var gap in CombatCommandEffects.AssessmentCoverageSteps(observed, enemyAssessment: true))
                {
                    gaps |= gap;
                    yield return 0;
                }

                var walls = geometry.Walls;
                var elevation = geometry.Elevation;

                var engine = new CombatEffectEngine();
                foreach (var attacker in input.Characters)
                {
                    yield return 0;
                    if (attacker.Owner != Owner.Player ||
                        CombatAttackQueries.IntrinsicUnitReadiness(observed, attacker) != AttackFailureReason.None) continue;
                    // 다음 플레이어 기회의 기하를 보므로 현재 턴의 행동 사용은 검사하지 않는다.
                    var profile = CombatCommandCalculator.RequiredProfile(observed, attacker);
                    int damage = CombatAttackCalculator.CommonAttackDamage(observed, attacker, profile);
                    var bindings = new List<EffectRuleBinding>();
                    foreach (var binding in CombatCommandEffects.CaptureSteps(attacker, profile))
                    {
                        if (binding != null)
                        {
                            bindings.Add(binding);
                            if (binding.Rule is IEffectTargetDistanceDamageRule or IEffectTargetDamageRule or IEffectIncomingDamageRule)
                                gaps |= CombatAssessmentGap.UnsupportedEffect;
                        }
                        yield return 0;
                    }
                    if (damage <= 0) continue;
                    var policy = engine.EvaluateAttackTargeting(bindings).Policy;
                    int basicRange = EnvironmentTerrainRules.AppointRange(observed, attacker, visible);
                    int splashDamage = profile.Method.Splash == TargetingSplash.None ? 0
                        : SplashDamageCalculator.CalculateAdditional(damage, profile.Method.SplashPercent);
                    var splashCenters = new HashSet<CellCoord>();
                    if (splashDamage > 0 && !policy.AllowsEmptySingle)
                        foreach (var _ in EligibleCenterSteps(observed, attacker, policy, splashCenters)) yield return 0;

                    foreach (var next in CombatAttackQueries.AiRangeCandidates(observed, attacker, profile, visible))
                    {
                        yield return 0;
                        if (next is not AttackAim aim) continue;
                        if (profile.UsesPiercingShot)
                        {
                            // 현재 관통 조우만 반영한다. 가상의 대상 이동에 따른 관통 순서는 평가하지 않는다.
                            gaps |= CombatAssessmentGap.UnsupportedEffect;
                            ResolvedAttackDelivery delivery = null;
                            foreach (var _ in CombatAttackQueries.DeliverySteps(observed, attacker, profile, aim, damage,
                                visible, walls, elevation, value => delivery = value)) yield return 0;
                            if (delivery == null) continue;
                            for (int i = 0; i < delivery.Hits.Count; i++)
                            {
                                Record(damageByCell, delivery.PiercingShot.Encounters.Span[i].Cell, delivery.Hits[i].BaseDamage);
                                yield return 0;
                            }
                            continue;
                        }

                        AttackCellSelection selected = null;
                        // 끌어오기의 추가 사거리는 대상별 이동 가능성이 필요하므로 공통 사거리까지만 추정한다.
                        foreach (var _ in CombatAttackQueries.SelectionCellSteps(observed, attacker, profile, aim, basicRange,
                            policy, (value, _) => selected = value)) yield return 0;
                        if (selected == null) continue;
                        AttackCellSelection reachable = null;
                        foreach (var _ in AttackOcclusionResolver.ResolveCellSteps(attacker.Anchor.Value, selected, walls,
                            elevation, profile.Occlusion, (value, _) => reachable = value)) yield return 0;
                        for (int i = 0; i < reachable.DesignatedCells.Length; i++)
                        {
                            Record(damageByCell, reachable.DesignatedCells.Span[i], damage);
                            yield return 0;
                        }
                        if (splashDamage == 0 || reachable.PrimaryCell is not CellCoord primary ||
                            !policy.AllowsEmptySingle && !splashCenters.Contains(primary)) continue;
                        for (int i = 0; i < reachable.AdditionalCells.Length; i++)
                        {
                            Record(damageByCell, reachable.AdditionalCells.Span[i], splashDamage);
                            yield return 0;
                        }
                    }
                }

                _result = new EnemyThreatField(damageByCell, gaps);
            }

            private static IEnumerable<int> EligibleCenterSteps(CombatCommandInput input, CharacterCombatFacts attacker,
                AttackTargetingPolicy policy, HashSet<CellCoord> centers)
            {
                foreach (var unit in input.Characters)
                {
                    if (AttackTargetEligibility.CanTargetCharacter(attacker, unit, policy))
                        foreach (var cell in unit.OccupiedCells)
                        {
                            centers.Add(cell);
                            yield return 0;
                        }
                    yield return 0;
                }
                foreach (var structure in input.State.Structures)
                {
                    if (AttackTargetEligibility.CanTargetStructure(attacker, structure,
                        input.EnvironmentBindings.ObstacleEntityIds.Contains(structure.EntityId), policy))
                        foreach (var cell in structure.OccupiedCells)
                        {
                            centers.Add(cell);
                            yield return 0;
                        }
                    yield return 0;
                }
            }

            private static void Record(Dictionary<CellCoord, int> cells, CellCoord cell, int damage)
            {
                if (damage > 0 && (!cells.TryGetValue(cell, out int previous) || damage > previous)) cells[cell] = damage;
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
    }
}
