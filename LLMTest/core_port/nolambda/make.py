"""람다 걷기 측정용 Core 사본을 만든다(2026-11-18, 측정 전용).

Core 원본(Assets/.../CoreCaculator)은 바꾸지 않는다. Contracts·Domain·Application을 src/로 복사한 뒤 아래 EDITS를
적용한다(LINQ·람다 → 반복문, 같은 결과). 고칠 문장이 원본에 정확히 한 번 있어야 한다(develop 갱신으로 바뀌면 멈춘다).
측정 도구 Harness가 이 사본으로 빌드된다(README.md).

저장소 루트에서: python -I LLMTest/core_port/nolambda/make.py
"""

from __future__ import annotations

import shutil
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
CORE = ROOT / "Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator"
SRC = HERE / "src"

# (파일, 원본 문장, 바꾼 문장). 묶음 1 = 할당 표본에서 자리가 드러난 상위 람다(allocbysite, README §2).
EDITS: list[tuple[str, str, str]] = [
    # 1. 헛보(모래 구름) 지정 판정: OfType + All 람다 → 반복문.
    ("Application/Combat/CombatAttackQueries.cs",
     """        private static bool CanDesignate(CombatCommandInput input, CharacterCombatFacts actor, CellCoord primary) =>
            input.State.Gimmicks.Entries.OfType<HaboobGimmickState>().All(h =>
                HaboobTargetingRules.CanDesignate(h.Cells, actor.Anchor.Value, primary));""",
     """        private static bool CanDesignate(CombatCommandInput input, CharacterCombatFacts actor, CellCoord primary)
        {
            var entries = input.State.Gimmicks.Entries;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i] is HaboobGimmickState h && !HaboobTargetingRules.CanDesignate(h.Cells, actor.Anchor.Value, primary))
                    return false;
            return true;
        }"""),
    # 2. 프로필 찾기: SingleOrDefault 람다 → 반복문(둘 이상이면 같은 예외).
    ("Application/Combat/CombatCommandCalculator.cs",
     """            if (character.OwnedId.HasValue)
            {
                var exact = input.Profiles.SingleOrDefault(p => p.OwnedId == character.OwnedId && p.Definition.Key == character.CharacterKey);
                if (exact != null) return exact;
                if (input.Owned.SingleOrDefault(o => o.Id == character.OwnedId.Value)?.Growth.IsEmpty == false)
                    throw new InvalidOperationException("특성이 성장한 개체의 전용 프로필이 없습니다.");
            }
            return input.Profiles.SingleOrDefault(p => !p.OwnedId.HasValue && p.Definition.Key == character.CharacterKey) ??
                throw new InvalidOperationException("명시적으로 연결한 지정 방식과 유닛 정의가 필요합니다.");""",
     """            var profiles = input.Profiles;
            if (character.OwnedId.HasValue)
            {
                CombatAttackProfile exact = null;
                for (int i = 0; i < profiles.Count; i++)
                {
                    var p = profiles[i];
                    if (p.OwnedId != character.OwnedId || p.Definition.Key != character.CharacterKey) continue;
                    if (exact != null) throw new InvalidOperationException("Sequence contains more than one matching element");
                    exact = p;
                }
                if (exact != null) return exact;
                var owned = input.Owned;
                OwnedCharacterRecord record = null;
                for (int i = 0; i < owned.Count; i++)
                {
                    if (owned[i].Id != character.OwnedId.Value) continue;
                    if (record != null) throw new InvalidOperationException("Sequence contains more than one matching element");
                    record = owned[i];
                }
                if (record?.Growth.IsEmpty == false)
                    throw new InvalidOperationException("특성이 성장한 개체의 전용 프로필이 없습니다.");
            }
            CombatAttackProfile shared = null;
            for (int i = 0; i < profiles.Count; i++)
            {
                var p = profiles[i];
                if (p.OwnedId.HasValue || p.Definition.Key != character.CharacterKey) continue;
                if (shared != null) throw new InvalidOperationException("Sequence contains more than one matching element");
                shared = p;
            }
            return shared ?? throw new InvalidOperationException("명시적으로 연결한 지정 방식과 유닛 정의가 필요합니다.");"""),
    # 3. 상태 대조: 메서드 그룹 → 대리자(부를 때마다 새로 만듦) 한 번만, Where·Select·Distinct·All → 반복문.
    ("Contracts/CombatEffects/CharacterStatusCompatibility.cs",
     """        private static bool Matches(StatusEffectSet statuses, StatusEffectSet imported, Func<AppliedStatus, bool> owns)
        {
            if (statuses.Entries.Count == 0 && imported.Entries.Count == 0) return true;
            var current = statuses.Entries.Where(owns).ToArray();
            var incoming = imported.Entries.Where(owns).ToArray();
            return current.Select(s => s.Key).Concat(incoming.Select(s => s.Key)).Distinct().All(key =>
                new StatusEffectSet(current.Where(s => s.Key == key).ToArray()).SameValues(
                    new StatusEffectSet(incoming.Where(s => s.Key == key).ToArray())));
        }

        internal static bool MatchesDebuffs(StatusEffectSet statuses, CharacterDebuffState debuffs) => Matches(statuses, Import(debuffs, null), IsDebuff);
        internal static bool MatchesTraitEffects(StatusEffectSet statuses, CharacterTraitEffects effects) => Matches(statuses, Import(null, effects), IsTraitEffect);""",
     """        private static readonly Func<AppliedStatus, bool> DebuffFilter = IsDebuff;
        private static readonly Func<AppliedStatus, bool> TraitEffectFilter = IsTraitEffect;

        private static bool Matches(StatusEffectSet statuses, StatusEffectSet imported, Func<AppliedStatus, bool> owns)
        {
            if (statuses.Entries.Count == 0 && imported.Entries.Count == 0) return true;
            List<AppliedStatus> current = null, incoming = null;
            foreach (var s in statuses.Entries) if (owns(s)) (current ??= new List<AppliedStatus>()).Add(s);
            foreach (var s in imported.Entries) if (owns(s)) (incoming ??= new List<AppliedStatus>()).Add(s);
            if (current == null && incoming == null) return true;
            current ??= new List<AppliedStatus>();
            incoming ??= new List<AppliedStatus>();
            var keys = new List<StatusEffectKey>();
            foreach (var s in current) if (!keys.Contains(s.Key)) keys.Add(s.Key);
            foreach (var s in incoming) if (!keys.Contains(s.Key)) keys.Add(s.Key);
            foreach (var key in keys)
                {
                    var mine = new List<AppliedStatus>();
                    var theirs = new List<AppliedStatus>();
                    foreach (var c in current) if (c.Key == key) mine.Add(c);
                    foreach (var c in incoming) if (c.Key == key) theirs.Add(c);
                    if (!new StatusEffectSet(mine.ToArray()).SameValues(new StatusEffectSet(theirs.ToArray()))) return false;
                }
            return true;
        }

        internal static bool MatchesDebuffs(StatusEffectSet statuses, CharacterDebuffState debuffs) => Matches(statuses, Import(debuffs, null), DebuffFilter);
        internal static bool MatchesTraitEffects(StatusEffectSet statuses, CharacterTraitEffects effects) => Matches(statuses, Import(null, effects), TraitEffectFilter);"""),
    ("Contracts/CombatEffects/CharacterStatusCompatibility.cs",
     "            debuffs == null ? current : Replace(current, Import(debuffs, null), IsDebuff);",
     "            debuffs == null ? current : Replace(current, Import(debuffs, null), DebuffFilter);"),
    ("Contracts/CombatEffects/CharacterStatusCompatibility.cs",
     "            effects == null ? current : Replace(current, Import(null, effects), IsTraitEffect);",
     "            effects == null ? current : Replace(current, Import(null, effects), TraitEffectFilter);"),
    # 4. 도발 판정: ToArray + Any 람다 둘 → 반복문.
    ("Application/Combat/EnemyAi/TauntTargeting.cs",
     """        private static bool Affects(CombatCommandInput input, CharacterCombatFacts source, CharacterCombatFacts actor,
            CombatProfileReadSet profileReads) =>
            !source.Health.IsDead && source.Area == CharacterRuntimeArea.Field &&
            source.Stats.TraitStatuses.ToArray().Any(t => t.Key == Contracts.Common.TraitKey.Taunt) &&
            CombatCommandEffects.Capture(source, CombatCommandCalculator.RequiredProfile(input, source, profileReads))
                .Any(binding => binding.Rule is TauntEffectRule rule &&
                    rule.Affects(new EffectRuleInput(binding.Origin, binding.ExpectedState), source, actor));""",
     """        private static bool Affects(CombatCommandInput input, CharacterCombatFacts source, CharacterCombatFacts actor,
            CombatProfileReadSet profileReads)
        {
            if (source.Health.IsDead || source.Area != CharacterRuntimeArea.Field) return false;
            bool taunt = false;
            foreach (var t in source.Stats.TraitStatuses.Span)
                if (t.Key == Contracts.Common.TraitKey.Taunt) { taunt = true; break; }
            if (!taunt) return false;
            foreach (var binding in CombatCommandEffects.Capture(source, CombatCommandCalculator.RequiredProfile(input, source, profileReads)))
                if (binding.Rule is TauntEffectRule rule &&
                    rule.Affects(new EffectRuleInput(binding.Origin, binding.ExpectedState), source, actor)) return true;
            return false;
        }"""),
    # 5. 지형 판정: OfType + Any 람다 → 반복문.
    ("Domain/Movement/EnvironmentTerrainRules.cs",
     """            ValidateTerrainQuery(environment, bindings, kind);
            return environment.Tiles.OfType<StaticTerrainFact>().Any(t => t.Cell == cell && IsTerrain(t, bindings, kind));""",
     """            ValidateTerrainQuery(environment, bindings, kind);
            var tiles = environment.Tiles;
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i] is StaticTerrainFact t && t.Cell == cell && IsTerrain(t, bindings, kind)) return true;
            return false;"""),
    # 6. 먼지 막이 출처: OfType·Where·Select·Max·OrderBy 람다 → 반복문(같은 순서: Id 오름차순).
    ("Domain/Traits/Rules/DustScreenTraitRule.cs",
     """        private static IReadOnlyList<TraitTrailFact> SelectRangeSourcesAt(CellCoord cell, MovementEnvironmentFacts environment, Owner? queryingSide) =>
            Select(environment.Tiles.OfType<TraitTrailFact>().Where(t => t.Cell == cell &&
                (!t.ExemptsAlliedAttackRangePenalty || t.CreatingSide != queryingSide)), false);
        internal static IReadOnlyList<TraitTrailFact> DefenseSourcesAt(CharacterCombatFacts recipient, MovementEnvironmentFacts environment) =>
            Select(environment.Tiles.OfType<TraitTrailFact>().Where(t => recipient.OccupiedCells.Contains(t.Cell)), true);
        private static IReadOnlyList<TraitTrailFact> Select(IEnumerable<TraitTrailFact> candidates, bool defense)
        {
            var sources = candidates.Where(t => t.Active && t.Key == TraitKey.DustScreen &&
                (defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction) > 0).ToArray();
            int maximum = sources.Select(t => defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction).DefaultIfEmpty(0).Max();
            return sources.Where(t => (defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction) == maximum).OrderBy(t => t.Id).ToArray();
        }""",
     """        private static IReadOnlyList<TraitTrailFact> SelectRangeSourcesAt(CellCoord cell, MovementEnvironmentFacts environment, Owner? queryingSide)
        {
            List<TraitTrailFact> found = null;
            var tiles = environment.Tiles;
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i] is TraitTrailFact t && t.Cell == cell &&
                    (!t.ExemptsAlliedAttackRangePenalty || t.CreatingSide != queryingSide))
                    (found ??= new List<TraitTrailFact>()).Add(t);
            return Select(found, false);
        }
        internal static IReadOnlyList<TraitTrailFact> DefenseSourcesAt(CharacterCombatFacts recipient, MovementEnvironmentFacts environment)
        {
            List<TraitTrailFact> found = null;
            var tiles = environment.Tiles;
            var cells = recipient.OccupiedCells;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!(tiles[i] is TraitTrailFact t)) continue;
                for (int c = 0; c < cells.Count; c++)
                    if (cells[c] == t.Cell) { (found ??= new List<TraitTrailFact>()).Add(t); break; }
            }
            return Select(found, true);
        }
        private static IReadOnlyList<TraitTrailFact> Select(List<TraitTrailFact> candidates, bool defense)
        {
            if (candidates == null) return System.Array.Empty<TraitTrailFact>();
            int maximum = 0;
            bool any = false;
            foreach (var t in candidates)
            {
                int value = defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction;
                if (!t.Active || t.Key != TraitKey.DustScreen || value <= 0) continue;
                if (!any || value > maximum) maximum = value;
                any = true;
            }
            if (!any) return System.Array.Empty<TraitTrailFact>();
            var result = new List<TraitTrailFact>();
            foreach (var t in candidates)
                if (t.Active && t.Key == TraitKey.DustScreen && (defense ? t.AttackDamageReductionPercent : t.AttackRangeReduction) == maximum)
                    result.Add(t);
            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            return result.ToArray();
        }"""),
    # ---- 묶음 2 (2026-11-18 사용자 "둘째 묶음도 진행해")
    # 7. 특성 보유 맞춤: TraitStatuses.ToArray() + Any 람다 둘 → Span 반복문.
    ("Application/Combat/TraitPresenceReconciler.cs",
     """                    var traits = source.Stats.TraitStatuses.ToArray();
                    if (traits.Any(t => t.Key == TraitKey.SoloAction || t.Key == TraitKey.Cohesion))""",
     """                    // 반복기 안에서는 Span을 못 돌려 지역 함수로(LINQ 없이 같은 판정).
                    static void Scan(ReadOnlyMemory<Contracts.Snapshots.Traits.TraitStatusSnapshot> statuses, out bool presence, out bool taunts)
                    {
                        presence = false; taunts = false;
                        foreach (var t in statuses.Span)
                        {
                            if (t.Key == TraitKey.SoloAction || t.Key == TraitKey.Cohesion) presence = true;
                            if (t.Key == TraitKey.Taunt) taunts = true;
                        }
                    }
                    Scan(source.Stats.TraitStatuses, out bool presence, out bool taunts);
                    if (presence)"""),
    ("Application/Combat/TraitPresenceReconciler.cs",
     "                    if (source.Area == CharacterRuntimeArea.Field && traits.Any(t => t.Key == TraitKey.Taunt)) anyTaunter = true;",
     "                    if (source.Area == CharacterRuntimeArea.Field && taunts) anyTaunter = true;"),
    # 8. 적용 상태 규칙: OfType·Any 람다 → 반복문.
    ("Domain/CombatEffects/Rules/AppliedStatusRules.cs",
     "            return current.Entries.Any(s => s is ActionRestrictionStatus && s.Key == key);",
     """            foreach (var s in current.Entries) if (s is ActionRestrictionStatus && s.Key == key) return true;
            return false;"""),
    ("Domain/CombatEffects/Rules/AppliedStatusRules.cs",
     "            foreach (var status in current.Entries.OfType<AttackIncreaseStatus>()) total = checked(total + status.Amount);",
     "            foreach (var entry in current.Entries) if (entry is AttackIncreaseStatus status) total = checked(total + status.Amount);"),
    ("Domain/CombatEffects/Rules/AppliedStatusRules.cs",
     "            int percent = current.Entries.OfType<AttackDecreaseStatus>().SingleOrDefault()?.ReductionPercent ?? 0;",
     """            AttackDecreaseStatus decrease = null;
            foreach (var entry in current.Entries)
                if (entry is AttackDecreaseStatus d)
                {
                    if (decrease != null) throw new InvalidOperationException("Sequence contains more than one element");
                    decrease = d;
                }
            int percent = decrease?.ReductionPercent ?? 0;"""),
    ("Domain/CombatEffects/Rules/AppliedStatusRules.cs",
     """            foreach (var status in current.Entries.OfType<DefenseDecreaseStatus>())
            {""",
     """            foreach (var entry in current.Entries)
            {
                if (!(entry is DefenseDecreaseStatus status)) continue;"""),
    # 9. 공격 선택: 주 대상 고르기(Where·Select·DefaultIfEmpty·Single) 두 곳과 주 칸 판정 Any 넷 → 반복문.
    ("Application/Combat/CombatAttackQueries.cs",
     """                selectedTargetId = targets.Where(t => t.Value == AttackTargetRole.Primary)
                    .Select(t => t.Key).DefaultIfEmpty(EntityId.Empty).Single();""",
     "                selectedTargetId = PrimaryTargetOf(targets);"),
    ("Application/Combat/CombatAttackQueries.cs",
     """                var targetId = targets.Where(t => t.Value == AttackTargetRole.Primary)
                    .Select(t => t.Key).DefaultIfEmpty(EntityId.Empty).Single();
                if (!targetId.IsEmpty &&
                    (input.Characters.Any(c => c.EntityId == targetId && AimHostility.AreHostile(actor.Owner, c.Owner)) ||
                     input.State.Structures.Any(s => s.EntityId == targetId && AimHostility.AreHostile(actor.Owner, s.Owner))))
                    return AttackFailureReason.None;
            }
            else if (policy.AllowsEmptySingle || targets.Any(t => t.Value == AttackTargetRole.Primary))
            {
                return AttackFailureReason.None;
            }
            // 주변의 유효 대상이 빈 칸이나 아군 중심 지정의 자격을 대신하지 않습니다.
            bool occupied = input.Characters.Any(c => c.Area == CharacterRuntimeArea.Field &&
                !c.Health.IsDead && c.OccupiedCells.Contains(primary)) ||
                input.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(primary));
            return occupied ? AttackFailureReason.NonHostile : AttackFailureReason.InvalidPrimaryTarget;
        }""",
     """                var targetId = PrimaryTargetOf(targets);
                if (!targetId.IsEmpty)
                {
                    foreach (var c in input.Characters)
                        if (c.EntityId == targetId && AimHostility.AreHostile(actor.Owner, c.Owner)) return AttackFailureReason.None;
                    foreach (var s in input.State.Structures)
                        if (s.EntityId == targetId && AimHostility.AreHostile(actor.Owner, s.Owner)) return AttackFailureReason.None;
                }
            }
            else
            {
                if (policy.AllowsEmptySingle) return AttackFailureReason.None;
                foreach (var t in targets) if (t.Value == AttackTargetRole.Primary) return AttackFailureReason.None;
            }
            // 주변의 유효 대상이 빈 칸이나 아군 중심 지정의 자격을 대신하지 않습니다.
            bool occupied = false;
            foreach (var c in input.Characters)
                if (c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && Holds(c.OccupiedCells, primary)) { occupied = true; break; }
            if (!occupied)
                foreach (var s in input.State.Structures)
                    if (!s.Health.IsDead && Holds(s.OccupiedCells, primary)) { occupied = true; break; }
            return occupied ? AttackFailureReason.NonHostile : AttackFailureReason.InvalidPrimaryTarget;
        }

        private static bool Holds(IReadOnlyList<CellCoord> cells, CellCoord cell)
        {
            for (int i = 0; i < cells.Count; i++) if (cells[i] == cell) return true;
            return false;
        }

        private static EntityId PrimaryTargetOf(IReadOnlyList<KeyValuePair<EntityId, AttackTargetRole>> targets)
        {
            EntityId found = EntityId.Empty;
            bool any = false;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Value != AttackTargetRole.Primary) continue;
                if (any) throw new InvalidOperationException("Sequence contains more than one element");
                found = targets[i].Key;
                any = true;
            }
            return found;
        }"""),
    # 10. 도발 출처: Where·OrderBy·Min 람다 → 반복문 + 정렬(거리, 개체, 원래 차례 — OrderBy·ThenBy와 같은 안정 정렬).
    ("Application/Combat/EnemyAi/TauntTargeting.cs",
     """            CharacterCombatFacts actor, CombatProfileReadSet profileReads = null) => characters
            .Where(source => Affects(input, source, actor, profileReads))
            .OrderBy(source => source.OccupiedCells.Min(cell => actor.OccupiedCells.Min(origin => Distance(cell, origin))))
            .ThenBy(source => source.EntityId).ToArray();""",
     """            CharacterCombatFacts actor, CombatProfileReadSet profileReads = null)
        {
            List<(long distance, CharacterCombatFacts source)> found = null;
            foreach (var source in characters)
            {
                if (!Affects(input, source, actor, profileReads)) continue;
                long distance = long.MaxValue;
                foreach (var cell in source.OccupiedCells)
                    foreach (var origin in actor.OccupiedCells)
                        distance = Math.Min(distance, Distance(cell, origin));
                (found ??= new List<(long, CharacterCombatFacts)>()).Add((distance, source));
            }
            if (found == null) return System.Array.Empty<CharacterCombatFacts>();
            var order = new int[found.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            System.Array.Sort(order, (a, b) =>
            {
                int c = found[a].distance.CompareTo(found[b].distance);
                if (c != 0) return c;
                c = found[a].source.EntityId.CompareTo(found[b].source.EntityId);
                return c != 0 ? c : a.CompareTo(b);
            });
            var result = new CharacterCombatFacts[order.Length];
            for (int i = 0; i < order.Length; i++) result[i] = found[order[i]].source;
            return result;
        }"""),
    # ---- 묶음 3 (2026-11-18 사용자 "셋째 묶음도 진행해")
    # 11. 공격 범위 조회(계획 AI가 후보 판마다 부름): 대상 ID·조준·마무리 사격 대상의 Where·Select·Concat·Distinct·OrderBy → 반복문.
    ("Application/Combat/CombatAttackQueries.cs",
     """            var ids = input.Characters.Where(c => AttackTargetEligibility.CanTargetCharacter(actor, c, policy) &&
                c.OccupiedCells.Any(mask.Contains)).Select(c => c.EntityId)
                .Concat(input.State.Structures.Where(s => !preparation.Sweep.HasValue && AttackTargetEligibility.CanTargetStructure(actor, s,
                    input.EnvironmentBindings.ObstacleEntityIds.Contains(s.EntityId), policy) &&
                    s.OccupiedCells.Any(basicMask.Contains)).Select(s => s.EntityId)).Distinct().OrderBy(id => id).ToArray();""",
     """            var idSet = new HashSet<EntityId>();
            var idList = new List<EntityId>();
            foreach (var c in input.Characters)
            {
                if (!AttackTargetEligibility.CanTargetCharacter(actor, c, policy)) continue;
                var occupied = c.OccupiedCells;
                for (int i = 0; i < occupied.Count; i++)
                    if (mask.Contains(occupied[i])) { if (idSet.Add(c.EntityId)) idList.Add(c.EntityId); break; }
            }
            if (!preparation.Sweep.HasValue)
                foreach (var st in input.State.Structures)
                {
                    if (!AttackTargetEligibility.CanTargetStructure(actor, st,
                        input.EnvironmentBindings.ObstacleEntityIds.Contains(st.EntityId), policy)) continue;
                    var occupied = st.OccupiedCells;
                    for (int i = 0; i < occupied.Count; i++)
                        if (basicMask.Contains(occupied[i])) { if (idSet.Add(st.EntityId)) idList.Add(st.EntityId); break; }
                }
            idList.Sort();
            var ids = idList.ToArray();"""),
    ("Application/Combat/CombatAttackQueries.cs",
     """            var aims = candidates.Where(a => Selection(input, actor, profile, a, environment, walls, elevation,
                preparation: preparation, prepareSweep: false).FailureReason == AttackFailureReason.None)
                .Distinct().ToArray();""",
     """            var aimSet = new HashSet<AttackAim>();
            var aimList = new List<AttackAim>();
            foreach (var a in candidates)
                if (Selection(input, actor, profile, a, environment, walls, elevation,
                    preparation: preparation, prepareSweep: false).FailureReason == AttackFailureReason.None && aimSet.Add(a))
                    aimList.Add(a);
            var aims = aimList.ToArray();"""),
    ("Application/Combat/CombatAttackQueries.cs",
     """            var instantDefeat = ids.Where(id => FinishingShotApplication.Qualifies(input, actor,
                input.Characters.SingleOrDefault(c => c.EntityId == id),
                input.State.Structures.SingleOrDefault(s => s.EntityId == id))).ToArray();""",
     """            var defeatList = new List<EntityId>();
            foreach (var id in ids)
            {
                CharacterCombatFacts character = null;
                foreach (var c in input.Characters)
                    if (c.EntityId == id)
                    {
                        if (character != null) throw new InvalidOperationException("Sequence contains more than one matching element");
                        character = c;
                    }
                StructureCombatFacts structure = null;
                foreach (var st in input.State.Structures)
                    if (st.EntityId == id)
                    {
                        if (structure != null) throw new InvalidOperationException("Sequence contains more than one matching element");
                        structure = st;
                    }
                if (FinishingShotApplication.Qualifies(input, actor, character, structure)) defeatList.Add(id);
            }
            var instantDefeat = defeatList.ToArray();"""),
    # 12. 범위 칸: Where·OrderBy·ThenBy → 반복문 + 정렬(Y, X). 같은 키는 같은 칸이라 안정 정렬이 아니어도 결과가 같다.
    ("Application/Combat/CombatAttackQueries.cs",
     """            return new AppointRangeCalculator().CalculateRangeCells(profile.Method, range, actor.Anchor.Value, actor.Owner)
                .Where(c => IsRangeCell(input, actor, profile, policy, c))
                .OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();""",
     """            var kept = new List<CellCoord>();
            foreach (var c in new AppointRangeCalculator().CalculateRangeCells(profile.Method, range, actor.Anchor.Value, actor.Owner))
                if (IsRangeCell(input, actor, profile, policy, c)) kept.Add(c);
            kept.Sort(CellOrder);
            return kept.ToArray();"""),
    ("Application/Combat/CombatAttackQueries.cs",
     """        private static bool IsRangeCell(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile,""",
     """        private static readonly Comparison<CellCoord> CellOrder = (a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);

        private static bool IsRangeCell(CombatCommandInput input, CharacterCombatFacts actor, CombatAttackProfile profile,"""),
    # 13. 특성 보유 맞춤: 방어막 Where·SingleOrDefault 람다 → 반복문.
    ("Application/Combat/TraitPresenceReconciler.cs",
     """                    : recipient.Health.IsDead ? new List<TraitShieldGrant>() : currentEffects.Shields
                        .Where(g => !IsPresenceManagedShield(g.Origin)).ToList();""",
     """                    : recipient.Health.IsDead ? new List<TraitShieldGrant>() : UnmanagedShields(currentEffects.Shields);"""),
    ("Application/Combat/TraitPresenceReconciler.cs",
     """                        var prior = currentEffects.Shields.SingleOrDefault(g => g.Origin == entry.Binding.Origin);""",
     """                        var prior = ShieldOf(currentEffects.Shields, entry.Binding.Origin);"""),
    ("Application/Combat/TraitPresenceReconciler.cs",
     """        private static IEnumerable<int> CalculateSteps(CombatCommandInput input, IReadOnlyList<CharacterCombatFacts> projected,""",
     """        private static List<TraitShieldGrant> UnmanagedShields(IReadOnlyList<TraitShieldGrant> shields)
        {
            var result = new List<TraitShieldGrant>();
            for (int i = 0; i < shields.Count; i++) if (!IsPresenceManagedShield(shields[i].Origin)) result.Add(shields[i]);
            return result;
        }

        private static TraitShieldGrant ShieldOf(IReadOnlyList<TraitShieldGrant> shields, EffectOriginKey origin)
        {
            TraitShieldGrant found = null;
            for (int i = 0; i < shields.Count; i++)
            {
                if (shields[i].Origin != origin) continue;
                if (found != null) throw new InvalidOperationException("Sequence contains more than one matching element");
                found = shields[i];
            }
            return found;
        }

        private static IEnumerable<int> CalculateSteps(CombatCommandInput input, IReadOnlyList<CharacterCombatFacts> projected,"""),
    # ---- 묶음 4 (2026-11-18 사용자 "넷째 묶음도 진행해")
    # 15. 효과 붙잡기 Capture: 단계 반복기(CaptureSteps) + ToArray 둘 + Single 람다 → 반복기 없는 반복문(같은 순서·같은 예외 조건).
    ("Application/Combat/CombatCommandEffects.cs",
     """            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackEffectBinding);
            var bindings = new List<EffectRuleBinding>();
            foreach (var binding in CaptureSteps(character, profile))
                if (binding != null) bindings.Add(binding);
            return bindings.ToArray();""",
     """            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackEffectBinding);
            var statuses = character.Stats.TraitStatuses.Span;
            var specs = profile.Definition.Traits.Span;
            if (statuses.Length != specs.Length)
                throw new InvalidOperationException("특성 정의와 공개 상태의 개수가 다릅니다.");
            var bindings = new List<EffectRuleBinding>(specs.Length + 1);
            foreach (var spec in specs)
            {
                TraitStatusSnapshot status = null;
                bool found = false;
                foreach (var candidate in statuses)
                {
                    if (candidate.Key != spec.TraitKey) continue;
                    if (found) throw new InvalidOperationException("Sequence contains more than one matching element");
                    status = candidate;
                    found = true;
                }
                if (!found) throw new InvalidOperationException("Sequence contains no matching element");
                var registration = _traitRegistry.GetRequiredRegistration(spec.TraitKey);
                var definition = registration.TraitDefinitionFactory.BuildDefinition(spec);
                if (!DeferredCombatFeatures.IsTraitAvailable(spec.TraitKey)) continue;
                bindings.Add(new EffectRuleBinding(TraitEffectOrigin.Create(character.EntityId, spec.TraitKey),
                    registration.EffectRuleFactory(definition), Capture(status)));
            }
            var category = character.Stats.Category;
            if (category.Key != profile.Definition.CombatCategoryKey ||
                character.Stats.SpecializationCategory != profile.Definition.SpecializationCategory)
                throw new InvalidOperationException("캐릭터와 정의의 Category가 다릅니다.");
            if (category.Key == CategoryKey.Cannon)
                bindings.Add(new EffectRuleBinding(CategoryEffectOrigin.Create(character.EntityId, category.Key,
                    CategoryRuleKey.SafeBombardment),
                    CategoryRuleFactories.CreateRule(new SafeBombardmentCategoryRuleDefinition()),
                    StatelessEffectStateSnapshot.Instance));
            return bindings.ToArray();"""),
    # 16. 받는 효과 붙잡기: Where·SelectMany·Concat·FirstOrDefault 람다 → 반복문(같은 순서: 자기 것 → 다른 유닛의 참호).
    ("Application/Combat/CombatCommandEffects.cs",
     """            var own = Capture(recipient, CombatCommandCalculator.RequiredProfile(input, recipient, profileReads));
            var auras = characters.Where(c => c.EntityId != recipient.EntityId && !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                    c.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Entrenchment))
                .SelectMany(c => Capture(c, CombatCommandCalculator.RequiredProfile(input, c, profileReads)).Where(b => b.Rule is EntrenchmentEffectRule));
            var bindings = own.Concat(auras).ToList();
            selectedDust = DustScreenTraitRule.DefenseSourcesAt(recipient, environment ?? input.State.Environment);
            var dust = selectedDust.FirstOrDefault();""",
     """            var own = Capture(recipient, CombatCommandCalculator.RequiredProfile(input, recipient, profileReads));
            var bindings = new List<EffectRuleBinding>(own);
            for (int i = 0; i < characters.Count; i++)
            {
                var c = characters[i];
                if (c.EntityId == recipient.EntityId || c.Health.IsDead || c.Area != CharacterRuntimeArea.Field ||
                    !global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitStatusScan.Has(c.Stats.TraitStatuses, TraitKey.Entrenchment))
                    continue;
                foreach (var b in Capture(c, CombatCommandCalculator.RequiredProfile(input, c, profileReads)))
                    if (b.Rule is EntrenchmentEffectRule) bindings.Add(b);
            }
            selectedDust = DustScreenTraitRule.DefenseSourcesAt(recipient, environment ?? input.State.Environment);
            var dust = selectedDust.Count > 0 ? selectedDust[0] : null;"""),
    # 17. 포효 정의 있나(정산마다): Any + ToArray + OfType → 반복문.
    ("Application/Combat/BossRoarCalculator.cs",
     """            if (!transition.Input.Profiles.Any(p => p.Definition.Traits.ToArray().OfType<RoarTraitSpec>().Any())) return transition;""",
     """            if (!HasRoarProfile(transition.Input.Profiles)) return transition;"""),
    # 18. 도발 출처 정렬: 람다 비교(묶음 2) → 삽입 정렬(출처는 몇 개뿐, 같은 안정 순서).
    ("Application/Combat/EnemyAi/TauntTargeting.cs",
     """            if (found == null) return System.Array.Empty<CharacterCombatFacts>();
            var order = new int[found.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            System.Array.Sort(order, (a, b) =>
            {
                int c = found[a].distance.CompareTo(found[b].distance);
                if (c != 0) return c;
                c = found[a].source.EntityId.CompareTo(found[b].source.EntityId);
                return c != 0 ? c : a.CompareTo(b);
            });
            var result = new CharacterCombatFacts[order.Length];
            for (int i = 0; i < order.Length; i++) result[i] = found[order[i]].source;
            return result;""",
     """            if (found == null) return System.Array.Empty<CharacterCombatFacts>();
            for (int i = 1; i < found.Count; i++)
            {
                var item = found[i];
                int j = i - 1;
                while (j >= 0 && (found[j].distance > item.distance ||
                    found[j].distance == item.distance && found[j].source.EntityId.CompareTo(item.source.EntityId) > 0))
                {
                    found[j + 1] = found[j];
                    j--;
                }
                found[j + 1] = item;
            }
            var result = new CharacterCombatFacts[found.Count];
            for (int i = 0; i < result.Length; i++) result[i] = found[i].source;
            return result;"""),
    ("Application/Combat/BossRoarCalculator.cs",
     """        internal static CombatCommandTransition ApplyPendingReactions(CombatCommandTransition transition,
            IReadOnlyList<IMovementTileRule> rules)""",
     """        private static bool HasRoarProfile(IReadOnlyList<CombatAttackProfile> profiles)
        {
            for (int i = 0; i < profiles.Count; i++)
                foreach (var trait in profiles[i].Definition.Traits.Span)
                    if (trait is RoarTraitSpec) return true;
            return false;
        }

        internal static CombatCommandTransition ApplyPendingReactions(CombatCommandTransition transition,
            IReadOnlyList<IMovementTileRule> rules)"""),
    # ---- 묶음 7: 다른 호출자(공격 전달 등)가 쓰는 동기 입구를 묶음 6의 단계 없는 판으로 잇기.
    ("Domain/Combat/AttackOcclusionResolver.cs",
     """            AttackBlockedContact result = null;
            foreach (var _ in CheckDirectSteps(attacker, contact, walls, elevation, policy, value => result = value)) { }
            return result;""",
     "            return CheckDirectNow(attacker, contact, walls, elevation, policy);"),
    ("Domain/Combat/AttackOcclusionResolver.cs",
     """            (AttackCellSelection, IReadOnlyList<AttackBlockedContact>) result = default;
            foreach (var _ in ResolveCellSteps(attacker, selected, walls, elevation, policy, (cells, blocked) => result = (cells, blocked))) { }
            return result;""",
     "            return ResolveCellsDirect(attacker, selected, walls, elevation, policy);"),
    ("Domain/Combat/AttackOcclusionResolver.cs",
     """            AttackBlockedContact result = null;
            foreach (var _ in FindBlockSteps(origin, contact, walls, phase, ignoreWalls, value => result = value)) { }
            return result;""",
     "            return FindBlockDirect(origin, contact, walls, phase, ignoreWalls);"),
    ("Domain/Combat/AttackSelectionResolver.cs",
     """            AttackCellSelection resolved = null;
            AttackFailureReason failure = default;
            foreach (var _ in ResolveSteps(method, range, origin, owner, aim, (value, reason) =>
                { resolved = value; failure = reason; })) { }
            cells = resolved;
            failureReason = failure;
            return failure == AttackFailureReason.None;""",
     """            (cells, failureReason) = ResolveDirect(method, range, origin, owner, aim);
            return failureReason == AttackFailureReason.None;"""),
    ("Domain/Combat/AttackTargetResolver.cs",
     """            KeyValuePair<EntityId, AttackTargetRole>[] result = null;
            foreach (var _ in CollectSteps(selection, eligibleCharacters.ToArray(), eligibleStructures.ToArray(),
                value => result = value)) { }
            return result;""",
     "            return CollectDirect(selection, eligibleCharacters.ToArray(), eligibleStructures.ToArray());"),
]


# (정규식, 바꿀 꼴). 모든 복사본 .cs에 적용한다. 묶음 3의 14번.
PATTERNS: list[tuple[str, str]] = [
    (r"(?P<x>[A-Za-z_][\w\.]*?)\.TraitStatuses\.ToArray\(\)\.Any\(t => (?P<keys>t\.Key == [\w\.]+(?: \|\| t\.Key == [\w\.]+)*)\)",
     "PATTERN_TRAIT_ANY"),
]

HELPER = """namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>람다 걷기 측정 사본 전용: 특성 상태 목록에 그 키가 있나(ToArray·Any 없이).</summary>
    public static class TraitStatusScan
    {
        public static bool Has(System.ReadOnlyMemory<Snapshots.Traits.TraitStatusSnapshot> statuses, TraitKey a)
        {
            foreach (var t in statuses.Span) if (t.Key == a) return true;
            return false;
        }

        public static bool Has(System.ReadOnlyMemory<Snapshots.Traits.TraitStatusSnapshot> statuses, TraitKey a, TraitKey b)
        {
            foreach (var t in statuses.Span) if (t.Key == a || t.Key == b) return true;
            return false;
        }
    }
}
"""


def _trait_any(match) -> str:
    import re as _re
    keys = _re.findall(r"t\.Key == ([\w\.]+)", match.group("keys"))
    if len(keys) > 2:
        return match.group(0)
    return ("global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitStatusScan.Has("
            + match.group("x") + ".TraitStatuses, " + ", ".join(keys) + ")")


def apply_patterns() -> int:
    import re
    (SRC / "Contracts/Common/TraitStatusScan.cs").write_text(HELPER, encoding="utf-8")
    total = 0
    for path in SRC.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        new, n = re.subn(PATTERNS[0][0], _trait_any, text)
        if n:
            path.write_text(new, encoding="utf-8", newline="")
            total += n
    return total


# ---- 묶음 5 (2026-11-18 사용자 "다섯째 묶음도 진행해")
# 공격 선택 Selection()은 단계 함수 SelectionSteps를 끝까지 돌리기만 한다. 측정 사본에서만 SelectionSteps·SelectionCellSteps를
# 단계 없는 판(SelectionDirect·SelectionCellsDirect)으로 기계적으로 바꿔 Selection()이 부르게 한다. 단계 판은 다른 호출자를 위해 그대로 둔다.
# 바꾸는 규칙: 단계 알림(yield return "…")은 지움, 안쪽 단계 함수의 foreach 본문 "yield return phase;"는 빈 문장으로,
# "completed(X); yield break;"와 마지막 completed(X)는 return X로. 결과·순서는 같다(단계 알림은 값을 바꾸지 않는다).
def _method(text: str, header: str) -> tuple[int, int]:
    start = text.index(header)
    start = text.rfind("\n", 0, start) + 1
    brace = text.index("{", text.index(")", start))
    depth, i = 0, brace
    while True:
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return start, i + 1
        i += 1


def _syncify(body: str) -> str:
    import re
    body = re.sub(r"[ \t]*yield return \"[^\"]*\";\r?\n", "", body)
    body = body.replace("yield return phase;", "{ }")
    body = re.sub(r"completed\((.*?)\);\s*yield break;", r"return \1;", body, flags=re.S)
    return body


def apply_batch5() -> None:
    import re
    path = SRC / "Application/Combat/CombatAttackQueries.cs"
    text = path.read_text(encoding="utf-8")
    a, b = _method(text, "internal static IEnumerable<string> SelectionSteps(")
    steps = text[a:b]
    direct = _syncify(steps)
    direct = direct.replace("internal static IEnumerable<string> SelectionSteps(", "private static AttackSelectionEvaluation SelectionDirect(", 1)
    direct = direct.replace("AttackElevation elevation, Action<AttackSelectionEvaluation> completed, SelectionPreparation preparation = null,",
                            "AttackElevation elevation, SelectionPreparation preparation = null,", 1)
    old_call = re.search(r"foreach \(var phase in SelectionCellSteps\(input, actor, profile, aim \?\? input\.Aim, range, policy,\s*"
                         r"\(value, reason\) => \{ selected = value; failure = reason; \}, captureSweepCells\)\) \{ \}", direct)
    assert old_call, "SelectionCellSteps call not found"
    direct = direct[:old_call.start()] + "(selected, failure) = SelectionCellsDirect(input, actor, profile, aim ?? input.Aim, range, policy, captureSweepCells);" + direct[old_call.end():]
    last = direct.rfind("completed(")
    assert last > 0
    direct = direct[:last] + "return (" + direct[last + len("completed("):]
    assert "completed" not in direct and "yield" not in direct, "SelectionDirect still has completed/yield"

    c, d = _method(text, "internal static IEnumerable<string> SelectionCellSteps(")
    cells = _syncify(text[c:d])
    cells = cells.replace("internal static IEnumerable<string> SelectionCellSteps(",
                          "private static (AttackCellSelection, AttackFailureReason) SelectionCellsDirect(", 1)
    cells = cells.replace("Action<AttackCellSelection, AttackFailureReason> completed, Action<AttackCellSelection> captureSweepCells = null)",
                          "Action<AttackCellSelection> captureSweepCells = null)", 1)
    cells = cells.replace("foreach (var phase in AttackSelectionResolver.ResolveSteps(", "foreach (var _ in AttackSelectionResolver.ResolveSteps(", 1)
    cells = cells.replace("return null, failure;", "return (null, failure);")
    last = cells.rfind("completed(")
    cells = cells[:last] + "return (" + cells[last + len("completed("):]
    assert "completed" not in cells and "yield" not in cells, "SelectionCellsDirect still has completed/yield"

    text = text[:d] + "\n\n" + cells + text[d:]
    text = text[:b] + "\n\n" + direct + text[b:]
    old = """            foreach (var _ in SelectionSteps(input, actor, profile, aim, environment, walls, elevation, value => result = value,
                preparation, damageKind, prepareSweep)) { }
            return result;"""
    nl = "\r\n" if "\r\n" in text else "\n"
    assert text.count(old.replace("\n", nl)) == 1
    text = text.replace(old.replace("\n", nl), ("            result = SelectionDirect(input, actor, profile, aim, environment, walls, elevation,\n"
                                                "                preparation, damageKind, prepareSweep);\n            return result;").replace("\n", nl))
    path.write_text(text, encoding="utf-8", newline="")


# ---- 묶음 6 (2026-11-18 사용자 "묶음 다 진행하고 정리해서 보고")
# 공격 선택 안쪽 단계 함수(대상 모으기·칸 고르기·가림 판정)도 단계 없는 판을 만들어 SelectionDirect·SelectionCellsDirect가 부르게 한다.
def _direct(text: str, header: str, new_header: str, completed_param: str) -> str:
    """header로 시작하는 단계 함수를 복사해 단계 없는 판(new_header)을 바로 뒤에 넣는다."""
    a, b = _method(text, header)
    body = text[a:b]
    import re
    body = body.replace(header, new_header, 1)
    body, n = re.subn(r",\s*Action<[^\n]*?> completed(?=\))", "", body, count=1)
    assert n == 1, new_header
    body = re.sub(r"yield return [^;]*;", "{ }", body)
    body = body.replace("yield break;", "{ }")
    body = body.replace("completed(", "return (")
    assert "yield" not in body and "completed" not in body, new_header
    return text[:b] + "\n\n" + body + text[b:]


def _sub_once(text: str, pattern: str, repl: str) -> str:
    import re
    new, n = re.subn(pattern, repl, text, flags=re.S)
    assert n >= 1, pattern
    return new


def apply_batch6() -> None:
    # 대상 모으기
    path = SRC / "Domain/Combat/AttackTargetResolver.cs"
    t = path.read_text(encoding="utf-8")
    t = _direct(t, "internal static IEnumerable<string> CollectSteps(",
                "internal static KeyValuePair<EntityId, AttackTargetRole>[] CollectDirect(",
                "")
    path.write_text(t, encoding="utf-8", newline="")

    # 칸 고르기
    path = SRC / "Domain/Combat/AttackSelectionResolver.cs"
    t = path.read_text(encoding="utf-8")
    t = _direct(t, "internal static IEnumerable<string> ResolveSteps(",
                "internal static (AttackCellSelection, AttackFailureReason) ResolveDirect(",
                ", Action<AttackCellSelection, AttackFailureReason> completed")
    path.write_text(t, encoding="utf-8", newline="")

    # 가림 판정: FindBlock → CheckDirect → ResolveCells
    path = SRC / "Domain/Combat/AttackOcclusionResolver.cs"
    t = path.read_text(encoding="utf-8")
    t = _direct(t, "private static IEnumerable<string> FindBlockSteps(",
                "private static AttackBlockedContact FindBlockDirect(",
                ", Action<AttackBlockedContact> completed")
    t = _direct(t, "internal static IEnumerable<string> ResolveCellSteps(",
                "internal static (AttackCellSelection, IReadOnlyList<AttackBlockedContact>) ResolveCellsDirect(",
                ", Action<AttackCellSelection, IReadOnlyList<AttackBlockedContact>> completed")
    a, b = _method(t, "internal static (AttackCellSelection, IReadOnlyList<AttackBlockedContact>) ResolveCellsDirect(")
    cells = t[a:b]
    cells = _sub_once(cells, r"foreach \(var step in CheckDirectSteps\(attacker, cell, walls, elevation, policy, value => contact = value\)\)\s*\{ \}",
                      "contact = CheckDirectNow(attacker, cell, walls, elevation, policy);")
    cells = _sub_once(cells, r"foreach \(var step in FindBlockSteps\(primary\.Value, cell, walls, AttackOcclusionPhase\.Propagation,\s*"
                             r"policy\.Propagation == AttackWallPropagation\.IgnoreWalls, value => contact = value\)\) \{ \}",
                      "contact = FindBlockDirect(primary.Value, cell, walls, AttackOcclusionPhase.Propagation,\n"
                      "                            policy.Propagation == AttackWallPropagation.IgnoreWalls);")
    cells += """

        private static AttackBlockedContact CheckDirectNow(CellCoord attacker, CellCoord contact, WallGeometry walls,
            AttackElevation elevation, AttackOcclusionSpec policy)
        {
            if (elevation == null) throw new ArgumentNullException(nameof(elevation));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            return FindBlockDirect(attacker, contact, walls, AttackOcclusionPhase.Direct,
                policy.Delivery == AttackWallDelivery.IgnoreWalls || elevation.BypassesWalls(attacker, contact));
        }"""
    t = t[:a] + cells + t[b:]
    path.write_text(t, encoding="utf-8", newline="")

    # 부르는 쪽(묶음 5의 단계 없는 판)
    path = SRC / "Application/Combat/CombatAttackQueries.cs"
    t = path.read_text(encoding="utf-8")
    a, b = _method(t, "private static AttackSelectionEvaluation SelectionDirect(")
    d = t[a:b]
    d = _sub_once(d, r"foreach \(var phase in AttackTargetResolver\.CollectSteps\((\w+), eligible, structures, value => targets = value\)\)\s*\{ \}",
                  r"targets = AttackTargetResolver.CollectDirect(\1, eligible, structures);")
    d = _sub_once(d, r"foreach \(var phase in AttackOcclusionResolver\.ResolveCellSteps\(actor\.Anchor\.Value, selected, walls, elevation,\s*"
                     r"EffectiveOcclusion\(profile, preparation\.Sweep\.HasValue\), \(cells, contacts\) => \{ reachable = cells; blocked = contacts; \}\)\) \{ \}",
                  "(reachable, blocked) = AttackOcclusionResolver.ResolveCellsDirect(actor.Anchor.Value, selected, walls, elevation,\n"
                  "                EffectiveOcclusion(profile, preparation.Sweep.HasValue));")
    t = t[:a] + d + t[b:]
    a, b = _method(t, "private static (AttackCellSelection, AttackFailureReason) SelectionCellsDirect(")
    c = t[a:b]
    c = _sub_once(c, r"foreach \(var _ in AttackSelectionResolver\.ResolveSteps\(profile\.Method, range, actor\.Anchor\.Value, actor\.Owner,\s*"
                     r"aim, \(value, reason\) => \{ raw = value; failure = reason; \}\)\) \{ \}",
                  "(raw, failure) = AttackSelectionResolver.ResolveDirect(profile.Method, range, actor.Anchor.Value, actor.Owner, aim);")
    t = t[:a] + c + t[b:]
    path.write_text(t, encoding="utf-8", newline="")


HELPER_REL = "Contracts/Common/TraitStatusScan.cs"
# Assets에 넣을 때 쓰는 고정 .meta(GUID가 실행마다 바뀌지 않게).
HELPER_META = """fileFormatVersion: 2
guid: 7c1f0a4e2b9d4f6e8a35d0c9e1b2f347
"""


def _load_base(rev: str | None) -> None:
    """src/에 고치기 전 Core를 놓는다. rev가 있으면 그 git 판에서, 없으면 Assets에서."""
    if SRC.exists():
        shutil.rmtree(SRC)
    if rev is None:
        if (CORE / HELPER_REL).exists():
            raise SystemExit("Assets의 Core에는 이미 적용돼 있습니다. 고치기 전 판을 --base <git 판>으로 알려 주세요(README).")
        for part in ("Contracts", "Domain", "Application"):
            shutil.copytree(CORE / part, SRC / part, ignore=shutil.ignore_patterns("*.meta"))
        return
    import io, subprocess, tarfile
    rel = CORE.relative_to(ROOT).as_posix()
    data = subprocess.run(["git", "-C", str(ROOT), "archive", "--format=tar", f"{rev}:{rel}", "Contracts", "Domain", "Application"],
                          check=True, capture_output=True).stdout
    with tarfile.open(fileobj=io.BytesIO(data)) as tar:
        members = [m for m in tar.getmembers() if m.isfile() and m.name.endswith(".cs")]
        tar.extractall(SRC, members=members)


def _apply_to_assets() -> None:
    """src/에서 달라진 .cs를 Assets의 Core에 넣는다(연구 저장소 전용, 2026-11-19 사용자 "Project_LLM이니까 해도 된다")."""
    import filecmp
    changed = 0
    for f in SRC.rglob("*.cs"):
        dest = CORE / f.relative_to(SRC)
        if dest.exists() and filecmp.cmp(f, dest, shallow=False):
            continue
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(f, dest)
        changed += 1
    meta = CORE / (HELPER_REL + ".meta")
    if not meta.exists():
        meta.write_text(HELPER_META, encoding="utf-8", newline="\n")
    print(f"Assets Core: {changed} files written")


def main() -> None:
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", help="고치기 전 Core를 읽을 git 판(없으면 Assets에서 복사)")
    parser.add_argument("--apply", action="store_true", help="바꾼 결과를 Assets의 Core에 넣는다")
    args = parser.parse_args()
    _load_base(args.base)
    for rel, old, new in EDITS:
        path = SRC / rel
        text = path.read_text(encoding="utf-8")
        nl = "\r\n" if "\r\n" in text else "\n"
        old_n, new_n = old.replace("\n", nl), new.replace("\n", nl)
        count = text.count(old_n)
        if count != 1:
            raise SystemExit(f"{rel}: 고칠 문장이 {count}번 있습니다(1번이어야 함)")
        path.write_text(text.replace(old_n, new_n), encoding="utf-8", newline="")
    patterned = apply_patterns()
    apply_batch5()
    apply_batch6()
    print(f"src/ made, edits {len(EDITS)}, pattern replacements {patterned}")
    if args.apply:
        _apply_to_assets()


if __name__ == "__main__":
    main()
