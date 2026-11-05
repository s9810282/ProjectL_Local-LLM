using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 특성 상시 이펙트 "공격 모드 대상"의 출처입니다(이펙트 끝 규칙 설계 EL3, 마무리 사격 FS-b, 계획 TP-E16·TP-E23).
    /// 공격 조준이 넘긴 (행위자, 즉시 처치 대상)을 들고, 행위자가 그 특성을 지금 쓸 수 있으면 특성 외형 상시 목록의 공격 모드 대상 항목을 대상마다 그 몸에 냅니다.
    /// </summary>
    /// <remarks>
    /// 대상 목록의 판정 원본은 Core 공격 범위 응답입니다. 이 출처는 받은 목록을 보인 사실(필드 위 살아 있음)로 거르기만 합니다.
    /// 공격 대상에 호버해도 목록은 그대로라 유지됩니다(결정 Q-FS4 A). 지우는 때는 공격 조준이 정합니다(모드 끝·결과 재생 직전·새 범위 응답).
    /// </remarks>
    internal sealed class CombatAttackModeEffectSource : ICombatPersistentEffectSource
    {
        private readonly ITraitVisualTableQuery _traits;
        private EntityId _actor;
        private EntityId[] _targets = Array.Empty<EntityId>();

        internal CombatAttackModeEffectSource(ITraitVisualTableQuery traits)
        {
            _traits = traits ?? throw new ArgumentNullException(nameof(traits));
        }

        /// <summary>행위자와 대상을 바꿉니다. 바뀌었으면 true입니다(부르는 쪽이 그때만 다시 맞춤).</summary>
        internal bool Show(EntityId actor, ReadOnlyMemory<EntityId> targets)
        {
            if (actor.IsEmpty || targets.IsEmpty)
                return Clear();
            if (_actor == actor && targets.Span.SequenceEqual(_targets))
                return false;
            _actor = actor;
            _targets = targets.ToArray();
            return true;
        }

        /// <summary>대상을 지웁니다. 지울 것이 있었으면 true입니다.</summary>
        internal bool Clear()
        {
            if (_actor.IsEmpty && _targets.Length == 0)
                return false;
            _actor = EntityId.Empty;
            _targets = Array.Empty<EntityId>();
            return true;
        }

        public void Collect(IReadOnlyList<CharacterCombatFacts> shown, List<CombatPersistentEffectWant> wants)
        {
            if (shown == null) throw new ArgumentNullException(nameof(shown));
            if (wants == null) throw new ArgumentNullException(nameof(wants));
            if (_targets.Length == 0)
                return;
            CharacterCombatFacts actor = FindAlive(shown, _actor);
            if (actor == null)
                return;
            ReadOnlySpan<TraitStatusSnapshot> traits = actor.Stats.TraitStatuses.Span;
            for (int t = 0; t < traits.Length; t++)
            {
                TraitStatusSnapshot trait = traits[t];
                if (trait == null || !trait.IsAvailable || !_traits.TryGetTraitVisual(trait.Key, out TraitVisualData visual))
                    continue;
                IReadOnlyList<TraitAlwaysEffect> always = visual.AlwaysEffects;
                for (int a = 0; a < always.Count; a++)
                {
                    if (always[a].Condition != CombatTraitAlwaysCondition.AttackModeTarget)
                        continue;
                    for (int i = 0; i < _targets.Length; i++)
                    {
                        if (FindAlive(shown, _targets[i]) == null)
                            continue;
                        wants.Add(new CombatPersistentEffectWant(
                            CombatPersistentEffectKey.OfTrait(_targets[i], trait.Key, _actor, a), always[a].Effect));
                    }
                }
            }
        }

        // 보인 사실에서 필드 위에 살아 있는 유닛입니다. 없으면 null입니다.
        private static CharacterCombatFacts FindAlive(IReadOnlyList<CharacterCombatFacts> shown, EntityId id)
        {
            for (int i = 0; i < shown.Count; i++)
            {
                CharacterCombatFacts unit = shown[i];
                if (unit.EntityId == id)
                    return unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead ? unit : null;
            }
            return null;
        }
    }
}
