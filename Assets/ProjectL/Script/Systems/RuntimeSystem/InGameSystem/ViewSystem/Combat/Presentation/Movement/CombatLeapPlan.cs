using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>실제 뛰어넘기 발동과 원본 걸음으로 도약 구간을 대응합니다. 지형·이동 규칙은 계산하지 않습니다.</summary>
    internal sealed class CombatLeapPlan
    {
        internal sealed class Span
        {
            internal Span(List<CombatCharacterEvent> steps) => Steps = Array.AsReadOnly(steps.ToArray());
            internal IReadOnlyList<CombatCharacterEvent> Steps { get; }
        }

        internal sealed class Step
        {
            internal Step(Span span, int index)
            {
                Span = span;
                Index = index;
            }

            internal Span Span { get; }
            internal int Index { get; }
            internal CombatCharacterEvent Event => Span.Steps[Index];
            internal bool IsFirst => Index == 0;
            internal bool IsLast => Index == Span.Steps.Count - 1;
            internal bool Follows(Step previous) => previous != null &&
                ReferenceEquals(Span, previous.Span) && Index == previous.Index + 1;
        }

        private readonly Dictionary<CombatCharacterEvent, Step> _steps = new();

        private CombatLeapPlan()
        {
        }

        internal Step Of(CombatCharacterEvent step) => _steps.TryGetValue(step, out Step value) ? value : null;

        internal static CombatLeapPlan Build(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var plan = new CombatLeapPlan();
            var movers = new HashSet<EntityId>();
            foreach (CombatEvent item in result.Events)
                if (item is CombatCharacterEvent moved && IsSelfDrivenStep(moved))
                    movers.Add(moved.Expected.EntityId);

            // 발동만 있는 합성 연출은 이동을 만들지 않습니다. 실제 걸음이 있는 출처만 대응합니다.
            if (movers.Count == 0) return plan;
            var run = new List<CombatCharacterEvent>();
            TraitActivationSnapshot pending = null;
            foreach (CombatEvent item in result.Events)
            {
                // 끊는 사건은 레인과 같은 판정 하나입니다(평가 C1-11). 구간이 비었으면 도약 유닛 조건은 보지 않습니다.
                // 끊김을 먼저 본 뒤 이 사건의 뛰어넘기 발동을 대기로 받습니다.
                if (EndsLeap(item, run.Count > 0 ? run[0].Expected.EntityId : EntityId.Empty))
                    CloseRun();
                if (item is CombatEffectsEvent effects)
                {
                    foreach (TraitActivationSnapshot trait in effects.Traits)
                    {
                        if (trait.TraitKey != TraitKey.Leap || trait.TraitActivationKind != TraitActivationKind.MovementChanged ||
                            !movers.Contains(trait.SourceUnitId)) continue;
                        if (pending != null || trait.TargetCells.Count != 1 || trait.TargetIds.Count != 1 ||
                            trait.TargetIds[0] != trait.SourceUnitId)
                            throw new InvalidOperationException("[CombatLeapPlan] 뛰어넘기 발동은 자기 대상·중간 칸 하나와 다음 걸음으로 대응해야 합니다.");
                        pending = trait;
                    }
                }
                if (item is not CombatCharacterEvent character) continue;
                // 이동이 아닌 캐릭터 사건의 끊김은 위 판정이 이미 봤습니다.
                if (character.Kind != CombatEventKind.Moved) continue;
                bool marked = pending != null;
                if (marked)
                {
                    if (!IsSelfDrivenStep(character) || character.Expected.EntityId != pending.SourceUnitId ||
                        character.Final.Anchor != pending.TargetCells[0])
                        throw new InvalidOperationException("[CombatLeapPlan] 뛰어넘기 발동의 출처·중간 칸이 다음 자발적 걸음과 다릅니다.");
                    pending = null;
                }
                if (!IsSelfDrivenStep(character))
                {
                    CloseRun();
                    continue;
                }
                if (run.Count > 0 && !Continues(run[run.Count - 1], character))
                    CloseRun();
                if (marked || run.Count > 0)
                    run.Add(character);
                // 첫 일반 걸음은 착지입니다. 그 뒤의 일반 이동은 구간에 넣지 않습니다.
                if (!marked) CloseRun();
            }
            if (pending != null)
                throw new InvalidOperationException("[CombatLeapPlan] 뛰어넘기 발동 뒤 대응하는 걸음이 없습니다.");
            CloseRun();
            return plan;

            void CloseRun()
            {
                if (pending != null)
                    throw new InvalidOperationException("[CombatLeapPlan] 뛰어넘기 발동과 다음 걸음 사이에 이동 경계가 있습니다.");
                if (run.Count == 0) return;
                var span = new Span(run);
                for (int i = 0; i < run.Count; i++)
                    if (!plan._steps.TryAdd(run[i], new Step(span, i)))
                        throw new InvalidOperationException("[CombatLeapPlan] 같은 걸음이 여러 도약 구간에 대응합니다.");
                run.Clear();
            }
        }

        /// <summary>
        /// 그 사건이 도약 구간을 끊는지입니다. 계획(<see cref="Build"/>)과 레인(<see cref="CombatUnitMotionPlayback.BeforeEvent"/>)이 함께 부르는 한 곳입니다(평가 C1-11).
        /// 행동 시작·출처 열기·끌어오기, 모든 유닛의 재배치·복귀, 도약 유닛의 사망·필드 이탈·앵커 변경이 끊습니다.
        /// 이동(<c>Moved</c>) 사건은 여기서 보지 않습니다. 계획은 걸음 대응에서 따로 보고, 레인은 걸음 길로 받아 이 질문에 오지 않습니다.
        /// </summary>
        /// <param name="leaper">도약 중인 유닛입니다. 비어 있으면 도약 유닛 조건은 보지 않습니다.</param>
        internal static bool EndsLeap(CombatEvent item, EntityId leaper)
        {
            if (CombatEventTypeTable.IsActionStart(item) || CombatEventTypeTable.OpensAttackSource(item) || item is CombatPullEvent)
                return true;
            if (item is not CombatCharacterEvent character || character.Kind == CombatEventKind.Moved)
                return false;
            if (character.Kind == CombatEventKind.Relocated || character.Kind == CombatEventKind.Returned)
                return true;
            return !leaper.IsEmpty && character.Final.EntityId == leaper &&
                (character.Final.Health.IsDead || character.Final.Area != CharacterRuntimeArea.Field ||
                 character.Expected.Anchor != character.Final.Anchor);
        }

        private static bool IsSelfDrivenStep(CombatCharacterEvent step) => step.Kind == CombatEventKind.Moved &&
            CombatMovementKinds.IsSelfDriven(CombatMovementKinds.Of(step));

        private static bool Continues(CombatCharacterEvent previous, CombatCharacterEvent next) =>
            previous.Final.EntityId == next.Expected.EntityId && previous.Final.Area == CharacterRuntimeArea.Field &&
            next.Expected.Area == CharacterRuntimeArea.Field && !next.Expected.Health.IsDead &&
            previous.Final.Anchor == next.Expected.Anchor;
    }
}
