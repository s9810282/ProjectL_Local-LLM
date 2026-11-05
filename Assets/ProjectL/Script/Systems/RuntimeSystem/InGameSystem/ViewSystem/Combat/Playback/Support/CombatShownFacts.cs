using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 결과 재생 중 지금 화면에 보인 캐릭터·구조물 사실의 원본입니다(Presenter 분리 설계 S1).
    /// 중간 체력·사망 시점은 최신 Buffer가 아니라 사건 스냅샷을 따르며, 사건 Presenter·걸음·공격 재생·소리가 이 사실을 읽습니다.
    /// </summary>
    internal sealed class CombatShownFacts : ICombatInspectionFactsReader
    {
        private Action _rendered;
        internal void BindRendered(Action rendered) => _rendered = rendered;

        public bool TryGet(EntityId id, out CombatInspectionFacts facts)
        {
            facts = default;
            if (id.IsEmpty) return false;
            CharacterCombatFacts character = FindCharacter(id);
            if (character != null)
            {
                if (!IsOnField(character)) return false;
                facts = new CombatInspectionFacts(character);
                return true;
            }
            foreach (StructureCombatFacts structure in _structures)
                if (structure.EntityId == id && CombatInspectionFacts.CanInspectStructure(structure))
                {
                    facts = new CombatInspectionFacts(structure);
                    return true;
                }
            return false;
        }
        public bool TryPick(CellCoord cell, out EntityId id)
        {
            foreach (CharacterCombatFacts character in _characters)
                if (IsOnField(character))
                    foreach (CellCoord occupied in character.OccupiedCells)
                        if (occupied == cell)
                        {
                            id = character.EntityId;
                            return true;
                        }
            foreach (StructureCombatFacts structure in _structures)
                if (CombatInspectionFacts.CanInspectStructure(structure))
                    foreach (CellCoord occupied in structure.OccupiedCells)
                        if (occupied == cell)
                        {
                            id = structure.EntityId;
                            return true;
                        }
            id = EntityId.Empty;
            return false;
        }
        private readonly FieldUnitPresenter _units;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly List<CharacterCombatFacts> _characters = new();
        private readonly HashSet<EntityId> _pullArrivals = new();
        // 이 결과에서 낭떠러지에 떨어진 개체입니다(추락 사건 순간에 넣음). 뒤따르는 사망 사건이 추락 연출을 고를 때 읽습니다.
        private readonly HashSet<EntityId> _falls = new();
        private readonly List<StructureCombatFacts> _structures = new();
        // 오래 보이는 이펙트(상태 유지·특성 상시)입니다. 그릴 때마다 맞춥니다(이펙트 끝 규칙 설계 EL4).
        private readonly CombatPersistentEffects _persistent;
        private bool _entryEffectsDeferred;
        // 유지 이펙트 맞추기에 넘기는 몸 자리 읽기입니다. 그릴 때마다 대리자를 만들지 않게 한 번 만듭니다.
        private readonly Func<EntityId, Pose?> _bodyOf;

        /// <param name="mapper">사건 Presenter가 소유한 현재 Mapper를 읽습니다. 사본을 갖지 않습니다.</param>
        /// <param name="persistent">오래 보이는 이펙트 부품입니다. 사실을 그린 바로 뒤 맞춥니다(계획 TP-E15).</param>
        internal CombatShownFacts(FieldUnitPresenter units, Func<FieldGridMapper> mapper, CombatPersistentEffects persistent)
        {
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _persistent = persistent ?? throw new ArgumentNullException(nameof(persistent));
            _bodyOf = StandingBodyOf;
        }

        internal IReadOnlyList<CharacterCombatFacts> Characters => _characters;

        /// <summary>주어진 사실대로 서 있을 때 그려질 위치·회전입니다(<see cref="FieldUnitPresenter.StandingPoseOf(CharacterCombatFacts, FieldGridMapper)"/>). 토네이도 떨어지기의 끝 자세입니다.</summary>
        internal Pose StandingPoseOf(CharacterCombatFacts facts) => _units.StandingPoseOf(facts, _mapper());

        internal void ApplyCharacter(CharacterCombatFacts facts)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (_placements.PreservesPlacement(facts.EntityId))
                facts = AtShownPlacement(facts);
            else
            {
                // 기존 위치 이동 행동은 더 새 사실이 이깁니다. 시간 후 동시 교환만 위에서 위치 보류를 유지합니다.
                _placements.Drop(facts.EntityId);
            }
            // 사건마다 돌아 람다 없이 지웁니다(GC 끊김 설계 GH4). 같은 개체를 모두 지우고 나머지 순서는 그대로입니다.
            for (int i = _characters.Count - 1; i >= 0; i--)
                if (_characters[i].EntityId == facts.EntityId)
                    _characters.RemoveAt(i);
            _characters.Add(facts);
        }

        /// <summary>전체 상태 사건을 반영하되 이미 보여 준 끌어오기 도착점만 보존합니다. 사건의 식별은 바꾸지 않습니다.</summary>
        internal void ApplyTraitState(CombatTraitStateEvent state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            CharacterCombatFacts final = state.Final;
            if (_pullArrivals.Contains(final.EntityId))
            {
                CharacterCombatFacts shown = FindCharacter(final.EntityId);
                if (final.Area != CharacterRuntimeArea.Field)
                    _pullArrivals.Remove(final.EntityId);
                else if (shown?.Anchor != null && final.Anchor != shown.Anchor)
                    final = final.WithPlacement(final.Area, null, shown.Anchor, shown.Rotation, shown.OccupiedCells);
            }
            ApplyCharacter(final);
        }

        internal void ApplyStructure(StructureCombatFacts facts)
        {
            // 캐릭터와 같은 규칙입니다(람다 없이, 같은 개체를 모두 지움).
            for (int i = _structures.Count - 1; i >= 0; i--)
                if (_structures[i].EntityId == facts.EntityId)
                    _structures.RemoveAt(i);
            _structures.Add(facts);
        }

        // 위치 교환 승객의 미뤄 둔 끝 사실입니다(이펙트 끝 규칙 설계 EL11, 계획 TP-E7). 상태는 보관 하나(CombatDeferredPlacements)가 갖고,
        // 반영·그리기만 여기서 합니다(묶음 끝 검토 M3). 특성 처리기는 위치 이동이면 가져가기만 합니다(검토 R1).
        private readonly CombatDeferredPlacements _placements = new();
        private readonly List<CharacterCombatFacts> _releasing = new();

        /// <summary>
        /// 위치 교환 승객의 끝 사실을 쥡니다(교환 쌍이 시작될 때, 계획 TP-E7 ①). 이동자 도착 때 함께 반영하지 않고, 위치 이동 행동이 가져가거나
        /// 발동 사건에서 <see cref="ReleaseUnclaimedPlacements"/>가 놓을 때까지 승객은 옛 자리에 보입니다.
        /// </summary>
        internal void HoldPlacement(CharacterCombatFacts facts) => _placements.Hold(facts);

        /// <summary>미뤄 둔 배치를 가져갑니다(위치 이동 행동, 계획 TP-E7 ②). 없으면 false입니다(그 유닛은 이미 사실 자리 — 행동하지 않음, 계획 TP-E6).</summary>
        internal bool TryClaimPlacement(EntityId id, out CharacterCombatFacts facts) => _placements.TryClaim(id, out facts);

        /// <summary>가져간 배치를 다시 쥔 것으로 돌립니다. 행동을 시작하지 못했을 때 씁니다(사건 Presenter의 놓기가 반영함).</summary>
        internal void UnclaimPlacement(EntityId id) => _placements.Unclaim(id);

        /// <summary>교환 명령의 첫 걸음부터 두 위치를 보류합니다. HP·상태는 사건 순서대로 반영합니다.</summary>
        internal void ClaimPositionSwap(CombatPlacementBatchPlan.Pair pair)
        {
            if (pair == null) throw new ArgumentNullException(nameof(pair));
            // 두 표시 위치를 모두 확인한 다음 보류를 등록합니다. 표시 위치의 원본은 이 목록 하나입니다.
            RequireShownPlacement(pair.First.Final);
            RequireShownPlacement(pair.Partner.Final);
            _placements.ClaimPair(pair.First.Final, pair.Partner.Final);
        }

        /// <summary>대기가 끝나 원본 쌍의 두 최종 사실을 함께 반영하고 한 번 그립니다.</summary>
        internal void ReleasePositionSwap(CombatPlacementBatchPlan.Pair pair)
        {
            if (pair == null) throw new ArgumentNullException(nameof(pair));
            _placements.TakePair(pair.First.Final.EntityId, pair.Partner.Final.EntityId, out var first, out var partner);
            _pullArrivals.Remove(first.EntityId);
            _pullArrivals.Remove(partner.EntityId);
            ApplyCharacter(first);
            ApplyCharacter(partner);
            Render();
        }

        /// <summary>실패한 교환의 보류만 버립니다. 정상 완료나 최신 사실 반영을 흉내 내지 않습니다.</summary>
        internal void DiscardPositionSwap(CombatPlacementBatchPlan.Pair pair)
        {
            if (pair == null) throw new ArgumentNullException(nameof(pair));
            _placements.Drop(pair.First.Final.EntityId);
            _placements.Drop(pair.Partner.Final.EntityId);
        }

        /// <summary>
        /// 위치 이동 행동이 끝났습니다. 아직 가져간 상태이면(그 사이 더 새 사실이 오지 않았으면) 반영하고, 어느 쪽이든 한 번 그립니다(계획 TP-E7 ②).
        /// 서는 자리 대여가 옛 자리로 되돌린 바로 뒤(같은 프레임) 불려서 승객이 새 자리로 튀지 않고 이어집니다. 그 사이 더 새 사실이 반영됐어도
        /// 대여 동안은 위치를 그리지 않았으므로 여기서 다시 그려 옛 자리에 남지 않게 합니다(묶음 끝 검토 M2).
        /// </summary>
        internal void ReleaseClaimedPlacement(EntityId id)
        {
            if (_placements.TryTakeClaimed(id, out CharacterCombatFacts facts))
            {
                _pullArrivals.Remove(facts.EntityId);
                ApplyCharacter(facts);
            }
            Render();
        }

        /// <summary>
        /// 아무도 가져가지 않은 미뤄 둔 배치를 모두 반영하고 한 번 그립니다(계획 TP-E7 ③ — 줄·행동이 없을 때의 안전장치, 옛 순간 이동과 같음).
        /// 사건 Presenter가 사건 처리기 시작 바로 뒤 한 곳에서 부릅니다. 놓을 것이 없으면 아무것도 하지 않습니다.
        /// </summary>
        internal void ReleaseUnclaimedPlacements()
        {
            if (!_placements.HasHeld)
                return;
            _releasing.Clear();
            _placements.TakeAllHeld(_releasing);
            for (int i = 0; i < _releasing.Count; i++)
            {
                _pullArrivals.Remove(_releasing[i].EntityId);
                ApplyCharacter(_releasing[i]);
            }
            _releasing.Clear();
            Render();
        }

        /// <summary>미뤄 둔·가져간 배치를 모두 버립니다(결과 시작·취소·끝 — 최종 사실이 맞춤, 계획 TP-E7 ⑤).</summary>
        internal void ForgetHeldPlacements() => _placements.Clear();

        /// <summary>걸음 사실을 반영한 뒤 한 번 그립니다.</summary>
        internal void ShowCharacter(CharacterCombatFacts facts)
        {
            _pullArrivals.Remove(facts.EntityId);
            ApplyCharacter(facts);
            Render();
        }

        /// <summary>결과 끝의 최종 사실로 바꿉니다. 그리기는 호출자가 합니다.</summary>
        internal void Replace(IReadOnlyList<CharacterData> characters, IReadOnlyList<StructureCombatFacts> structures)
            => Replace((characters ?? throw new ArgumentNullException(nameof(characters))).Select(c => c.ToFacts()).ToArray(), structures);

        /// <summary>표시용 불변 사실 목록을 받습니다.</summary>
        internal void Replace(IReadOnlyList<CharacterCombatFacts> characters, IReadOnlyList<StructureCombatFacts> structures)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (structures == null) throw new ArgumentNullException(nameof(structures));
            ForgetHeldPlacements();
            ForgetPullArrivals();
            ForgetFalls();
            _characters.Clear();
            for (int i = 0; i < characters.Count; i++)
                _characters.Add(characters[i]);
            _structures.Clear();
            for (int i = 0; i < structures.Count; i++)
                _structures.Add(structures[i]);
        }

        internal void Clear()
        {
            _entryEffectsDeferred = false;
            ForgetHeldPlacements();
            ForgetPullArrivals();
            ForgetFalls();
            _characters.Clear();
            _structures.Clear();
            _persistent.Clear();
        }

        /// <summary>낭떠러지 추락 사건을 보였습니다(임시 추락 연출). 결과를 시작·취소·끝낼 때 비웁니다.</summary>
        internal void RetainFall(EntityId id) => _falls.Add(id);

        internal void ForgetFalls() => _falls.Clear();

        /// <summary>이 결과에서 낭떠러지에 떨어진 개체인지입니다. 사망 사건이 추락 연출을 고를 때 읽습니다.</summary>
        internal bool HasFallen(EntityId id) => _falls.Contains(id);

        // 도착 위치 자체는 기존 표시 사실에만 둡니다. 이 집합은 뒤늦은 체력 사건의 역사적 위치를 구분하는 재생 표지입니다.
        internal void RetainPullArrival(EntityId id) => _pullArrivals.Add(id);
        internal void ForgetPullArrivals() => _pullArrivals.Clear();

        internal CombatEvent ForPresentation(CombatEvent item)
        {
            if (item is not CombatCharacterEvent character) return item;
            bool healthOnly = character.Kind == CombatEventKind.HealthChanged || character.Kind == CombatEventKind.HealthNormalized ||
                character.Kind == CombatEventKind.Died;
            if (healthOnly && _placements.PreservesPlacement(character.Final.EntityId))
                return new CombatCharacterEvent(character.Kind, AtShownPlacement(character.Expected), AtShownPlacement(character.Final),
                    character.DamageOrigins, character.TornadoSourceId, character.DamageCause, character.MovementCause, character.TornadoContact);
            if (!_pullArrivals.Contains(character.Final.EntityId)) return item;
            if (!healthOnly || character.Final.Area != CharacterRuntimeArea.Field)
            {
                _pullArrivals.Remove(character.Final.EntityId);
                return item;
            }
            CharacterCombatFacts shown = FindCharacter(character.Final.EntityId);
            if (shown?.Anchor == null || character.Final.Anchor == shown.Anchor)
            {
                _pullArrivals.Remove(character.Final.EntityId);
                return item;
            }
            // HP·상태·사망은 사건 값, 이미 완료된 경로의 위치는 현재 표시 값입니다. 실제 이동/이탈 사건은 위에서 그대로 통과합니다.
            CharacterCombatFacts AtArrival(CharacterCombatFacts facts) => facts.Area == CharacterRuntimeArea.Field
                ? facts.WithPlacement(facts.Area, null, shown.Anchor, shown.Rotation, shown.OccupiedCells)
                : facts;
            return new CombatCharacterEvent(character.Kind, AtArrival(character.Expected), AtArrival(character.Final), character.DamageOrigins);
        }

        private CharacterCombatFacts AtShownPlacement(CharacterCombatFacts facts)
        {
            CharacterCombatFacts shown = RequireShownPlacement(facts);
            if (facts.Anchor == shown.Anchor && facts.Rotation == shown.Rotation && facts.OccupiedCells.SequenceEqual(shown.OccupiedCells))
                return facts;
            return facts.WithPlacement(facts.Area, null, shown.Anchor, shown.Rotation, shown.OccupiedCells);
        }

        private CharacterCombatFacts RequireShownPlacement(CharacterCombatFacts facts)
        {
            CharacterCombatFacts shown = FindCharacter(facts.EntityId);
            if (facts.Area != CharacterRuntimeArea.Field || shown?.Area != CharacterRuntimeArea.Field || !shown.Anchor.HasValue)
                throw new InvalidOperationException($"[CombatShownFacts] 위치 교환을 기다리는 필드 참여자의 표시 위치가 없습니다: {facts.EntityId}");
            return shown;
        }

        internal CharacterCombatFacts FindCharacter(EntityId id)
        {
            for (int i = 0; i < _characters.Count; i++)
                if (_characters[i].EntityId == id) return _characters[i];
            return null;
        }

        // 같은 사실 목록을 바로 그려 중간 Select/ToArray 복사를 만들지 않는다.
        // 유지 이펙트는 유닛을 그린 바로 뒤 같은 사실로 맞춥니다. 그래서 HUD 아이콘과 같은 순간에 켜지고 꺼집니다(이펙트 끝 규칙 설계 EL4).
        internal void Render()
        {
            _units.RenderFacts(_characters, _structures, _mapper());
            SyncPersistent();
            _rendered?.Invoke();
        }

        /// <summary>
        /// 유닛을 다시 그리지 않고 유지 이펙트만 지금 보인 사실로 맞춥니다. 보인 사실이 아닌 입력(공격 모드 대상)이 바뀌었을 때 씁니다(계획 Ed.3).
        /// </summary>
        internal void SyncPersistent()
        {
            if (!_entryEffectsDeferred) _persistent.Sync(_characters, _bodyOf);
        }

        /// <summary>최초 그리기 전 상시 효과만 보류합니다. 해제 자체는 새 효과를 재생하지 않습니다.</summary>
        internal void SetEntryEffectsDeferred(bool deferred) => _entryEffectsDeferred = deferred;

        /// <summary>등장 완료 후 현재 표시 사실로 상시 효과를 한 번 맞춥니다.</summary>
        internal void RevealEntryEffects()
        {
            if (!_entryEffectsDeferred) return;
            _entryEffectsDeferred = false;
            SyncPersistent();
        }

        /// <summary>
        /// 공격 재생의 몸 조회입니다(히트 타이밍 설계 H4). 지금 보인 사실에서 필드 위의 대상을 찾아
        /// <see cref="FieldUnitPresenter.BodyOf(CharacterCombatFacts, FieldGridMapper)"/>로 몸을 만듭니다.
        /// 사실 목록을 넘기거나 최신 Buffer를 읽지 않습니다. 찾지 못하면 false(몸 조회 실패)입니다.
        /// </summary>
        internal bool TryGetBody(EntityId id, out CombatBody body, out EntityKey key)
        {
            body = default;
            key = default;
            FieldGridMapper mapper = _mapper();
            if (mapper == null || !TryFindTarget(id, out CharacterCombatFacts character, out StructureCombatFacts structure)) return false;
            if (character != null)
            {
                // 유닛 행동이 옮긴 서는 자리가 있으면 그 자리의 몸입니다(유닛 행동 설계 AA3). 투사체 접촉이 수호자가 지금 서 있는 곳에서 납니다.
                body = TryGetStand(id, out Pose stand) ? _units.BodyAt(character, stand, mapper) : _units.BodyOf(character, mapper);
                key = character.CharacterKey;
                return true;
            }
            body = _units.BodyOf(structure, mapper);
            key = structure.Definition.Key;
            return true;
        }

        /// <summary>
        /// 지금 보인 사실에서 대상(캐릭터·구조물) 하나의 서 있는 몸 자리입니다(특성 발동 큐의 발동 유닛·대상 몸 자리, 특성 발동 연출 설계 TA4).
        /// 캐릭터는 <see cref="StandingBodyOf(CharacterCombatFacts)"/>, 구조물은 몸 상자 가운데와 그 표시 회전입니다. 찾는 규칙은 <see cref="TryFindTarget"/>입니다.
        /// 찾지 못하면 null이고, 그 자리의 이펙트 큐는 항목을 넣지 않습니다.
        /// </summary>
        internal Pose? StandingBodyOf(EntityId id)
        {
            FieldGridMapper mapper = _mapper();
            if (mapper == null || !TryFindTarget(id, out CharacterCombatFacts character, out StructureCombatFacts structure)) return null;
            if (character != null)
                // 유닛 행동이 옮긴 서는 자리가 있으면 그 자리입니다(유닛 행동 설계 AA3). 사실로 묻는 StandingBodyOf(facts)는 그대로 사실 자리입니다.
                return TryGetStand(id, out Pose stand)
                    ? new Pose(_units.BodyCenterAt(character, stand), stand.rotation)
                    : StandingBodyOf(character);
            return new Pose(_units.BodyCenterOf(structure, mapper), mapper.RotationToWorld(structure.Rotation));
        }

        /// <summary>
        /// 피격 이펙트의 몸 중심 조회입니다(피격 이펙트 설계 HE3). 지금 보인 사실에서 필드 위의 캐릭터를 찾아
        /// <see cref="FieldUnitPresenter.BodyCenterOf"/>로 월드 몸 중심을 줍니다. 찾는 규칙은 <see cref="TryGetBody"/>와 같고 캐릭터만 봅니다.
        /// 찾지 못하면 false입니다.
        /// </summary>
        internal bool TryGetBodyCenter(EntityId id, out Vector3 worldCenter)
        {
            if (id.IsEmpty)
            {
                worldCenter = default;
                return false;
            }
            CharacterCombatFacts character = FindCharacter(id);
            // 유닛 행동이 옮긴 서는 자리가 있으면 그 자리의 몸 중심입니다(유닛 행동 설계 AA3). 수호자의 피격 이펙트가 지금 서 있는 곳에 납니다.
            if (character != null && IsOnField(character) && _mapper() != null && TryGetStand(id, out Pose stand))
            {
                worldCenter = _units.BodyCenterAt(character, stand);
                return true;
            }
            return TryGetBodyCenterOf(character, out worldCenter);
        }

        /// <summary>
        /// 지금 보인 사실에서 캐릭터 하나가 서 있는 자리(발밑 위치·방향)입니다(유닛 행동 설계 AA4). 유닛 행동이 옮긴 서는 자리가 있으면 그 자리,
        /// 아니면 그 사실대로 서 있는 자세(<see cref="StandingPoseOf"/>)입니다. 행동 자리 계산(지켜진 유닛 앞·공격자 쪽)의 기준점입니다.
        /// 필드 위에 없거나 캐릭터가 아니면 null입니다.
        /// </summary>
        internal Pose? StandingPlaceOf(EntityId id)
        {
            if (id.IsEmpty || _mapper() == null) return null;
            CharacterCombatFacts character = FindCharacter(id);
            if (character == null || !IsOnField(character)) return null;
            return TryGetStand(id, out Pose stand) ? stand : StandingPoseOf(character);
        }

        // ID로 묻는 지금 몸 자리가 먼저 보는 서는 자리입니다(유닛 행동 설계 AA3, 계획 TP-20). 원본은 유닛 표시 창구 하나입니다.
        private bool TryGetStand(EntityId id, out Pose stand) => _units.Presentation.TryGetStandPose(id, out stand);

        /// <summary>
        /// 사실 값 하나의 몸 중심입니다(이펙트 창구 확장 계획 P19). 아직 보이지 않은 사실(토네이도로 내려앉은 뒤)의 몸 자리에 씁니다.
        /// 몸을 찾을 수 있는 조건은 <see cref="TryGetBodyCenter"/>와 같습니다(필드 위·위치 있음·살아 있음). 아니면 false입니다.
        /// </summary>
        internal bool TryGetBodyCenterOf(CharacterCombatFacts facts, out Vector3 worldCenter)
        {
            worldCenter = default;
            FieldGridMapper mapper = _mapper();
            if (mapper == null || facts == null || !IsOnField(facts)) return false;
            worldCenter = _units.BodyCenterOf(facts, mapper);
            return true;
        }

        /// <summary>
        /// 사실 값 하나대로 서 있는 유닛의 몸 자리입니다. 위치 = 몸 상자 가운데, 회전 = 그 사실로 서 있는 방향(<see cref="StandingPoseOf"/>)입니다.
        /// 사실 사건(아이템 = 소지자)·토네이도 큐의 주체 몸 자리가 이것 하나를 씁니다(이펙트 창구 확장 설계 EC2·EC8, 묶음 끝 검토 L1).
        /// 몸을 찾지 못하면(<see cref="TryGetBodyCenterOf"/>가 false) null이고, 그 자리의 이펙트 큐는 항목을 넣지 않습니다.
        /// </summary>
        internal Pose? StandingBodyOf(CharacterCombatFacts facts)
        {
            if (!TryGetBodyCenterOf(facts, out Vector3 center))
                return null;
            return new Pose(center, StandingPoseOf(facts).rotation);
        }

        // 지금 보인 사실에서 대상을 찾는 한 곳입니다(몸·칸·몸 자리 조회가 함께 씀, 특성 발동 연출 묶음 끝 검토 L1). 캐릭터가 먼저이고(필드 위·위치 있음·살아 있음),
        // 없으면 구조물(살아 있음)입니다. 찾으면 둘 중 하나만 null이 아닙니다.
        private bool TryFindTarget(EntityId id, out CharacterCombatFacts character, out StructureCombatFacts structure)
        {
            character = null;
            structure = null;
            if (id.IsEmpty) return false;
            CharacterCombatFacts found = FindCharacter(id);
            if (found != null)
            {
                if (!IsOnField(found)) return false;
                character = found;
                return true;
            }
            for (int i = 0; i < _structures.Count; i++)
            {
                if (_structures[i].EntityId != id) continue;
                if (_structures[i].Health.IsDead) return false;
                structure = _structures[i];
                return true;
            }
            return false;
        }

        // 몸을 찾을 수 있는 캐릭터인지입니다(필드 위·위치 있음·살아 있음). 몸 조회들이 함께 쓰는 한 곳입니다.
        private static bool IsOnField(CharacterCombatFacts character) =>
            character.Area == CharacterRuntimeArea.Field && character.Anchor.HasValue && !character.Health.IsDead;
    }
}
