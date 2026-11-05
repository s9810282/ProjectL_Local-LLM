using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 특성 상시 이펙트 "보유 동안"의 출처입니다(이펙트 끝 규칙 설계 EL3, 계획 TP-E14). 보인 사실의 유닛이 필드 위에 살아 있고, 그 특성을 지금 쓸 수 있으면
    /// (<c>Stats.TraitStatuses</c>에 있고 <see cref="TraitStatusSnapshot.IsAvailable"/>, 결정 Q-EL5 A) 특성 외형 상시 목록의 보유 동안 항목을 보유 유닛 몸에 냅니다.
    /// 숨김(끌기·사망 유지)은 받침이 그리기만 끕니다.
    /// </summary>
    internal sealed class CombatOwnedTraitEffectSource : ICombatPersistentEffectSource
    {
        private readonly ITraitVisualTableQuery _traits;

        internal CombatOwnedTraitEffectSource(ITraitVisualTableQuery traits)
        {
            _traits = traits ?? throw new ArgumentNullException(nameof(traits));
        }

        public void Collect(IReadOnlyList<CharacterCombatFacts> shown, List<CombatPersistentEffectWant> wants)
        {
            if (shown == null) throw new ArgumentNullException(nameof(shown));
            if (wants == null) throw new ArgumentNullException(nameof(wants));
            for (int i = 0; i < shown.Count; i++)
            {
                CharacterCombatFacts unit = shown[i];
                if (unit.Area != CharacterRuntimeArea.Field || unit.Health.IsDead)
                    continue;
                ReadOnlySpan<TraitStatusSnapshot> traits = unit.Stats.TraitStatuses.Span;
                for (int t = 0; t < traits.Length; t++)
                {
                    TraitStatusSnapshot trait = traits[t];
                    if (trait == null || !trait.IsAvailable || !_traits.TryGetTraitVisual(trait.Key, out TraitVisualData visual))
                        continue;
                    IReadOnlyList<TraitAlwaysEffect> always = visual.AlwaysEffects;
                    for (int a = 0; a < always.Count; a++)
                    {
                        if (always[a].Condition != CombatTraitAlwaysCondition.WhileOwned)
                            continue;
                        wants.Add(new CombatPersistentEffectWant(
                            CombatPersistentEffectKey.OfTrait(unit.EntityId, trait.Key, unit.EntityId, a), always[a].Effect));
                    }
                }
            }
        }
    }
}
