using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 재생 쪽이 유닛 표시를 건드리는 유일한 입구입니다(연출 조립 설계 B13). 채널(자세·서는 자리·크기·걸음 배치·표시 회전·사망 유지·이동 투명도·피격 플래시·사건 클립)을
    /// 대여로 내주고, 사실 그리기가 묻는 소유 사실(배치 유지·회전 덮기)의 원본입니다. 연출 값은 대여 없이 읽기로 함께 줍니다.
    /// </summary>
    /// <remarks>
    /// <see cref="FieldUnitPresenter.Presentation"/>이 한 번 만듭니다. 표시 구현은 계약(<see cref="ICombatUnitTargets"/>·<see cref="ICombatUnitPresentationTarget"/>)으로만 압니다.
    /// 자세·크기 대여의 수명 규칙은 표시가 지키고, 여기서는 찾기와 채널 사이 규칙만 맡습니다.
    /// 같은 유닛의 자세와 걸음 배치는 함께 쓸 수 없습니다(결정 Q-R11 A). 지금 순차 재생 규칙에서는 생기지 않는 조합이라, 생기면 예외로 알립니다.
    /// 다만 도약 중간 경유에서 보간을 멈추고 쥔 배치는 양보할 수 있게 표시되어(<see cref="MarkYieldable"/>), 자세·서는 자리를 빌리면
    /// 창구가 그 배치를 빼고 레인에 알립니다. 되찾기는 레인이 그 대여가 끝난 뒤 새 배치로 합니다(평가 C4-05).
    /// </remarks>
    internal sealed class CombatUnitPresentationWindow
    {
        private readonly ICombatUnitTargets _targets;
        // 걸음 보간이 배치를 소유 중인 유닛들입니다(이동 접촉 설계 M5 (a), 동시 처리 설계 C5).
        private readonly Dictionary<EntityId, Placement> _placements = new();
        private readonly Dictionary<EntityId, DeathHold> _deathHolds = new();
        // 사실 회전을 덮는 표시 회전입니다(걸음 전 회전·경로 끝 방향 복귀).
        private readonly Dictionary<EntityId, Quaternion> _facing = new();
        // 이동 투명도 층은 지금처럼 전투에 하나입니다(입력 미리보기의 이동 원본).
        private Appearance _movementOpacity;
        // 서는 자리를 접은 경고를 유닛마다 한 번만 남깁니다(유닛 행동 설계 AA8).
        private readonly HashSet<EntityId> _foldWarned = new();

        internal CombatUnitPresentationWindow(ICombatUnitTargets targets) =>
            _targets = targets ?? throw new ArgumentNullException(nameof(targets));

        /// <summary>공격 자세를 시작합니다. 표시가 없으면 예외이고, 꺼진 표시면 대여가 예외를 던집니다.</summary>
        internal ICombatAttackUnitPose BeginAttackPose(EntityId id, Action cancelled = null)
        {
            if (!_targets.TryGet(id, out var target))
                throw new InvalidOperationException($"[CombatUnitPresentationWindow] 공격 표시가 없습니다: {id}.");
            RequireNoPlacement(id);
            return target.BeginPose(cancelled);
        }

        /// <summary>
        /// 켜진 표시에 조립 실행의 자세를 시작합니다(조각 실행 대여 묶음이 부름 — 소환 등장·피격·사망 반응 등, 보스 활성화 설계 D6b·피격 구조 정리 설계 HC1).
        /// 표시가 없거나 꺼져 있으면 false입니다. 도약 중간 경유의 걸음 배치는 양보받고, 자세는 그 공중 자리에서 시작합니다(평가 C4-05).
        /// 자세를 여는 두 입구(이것과 <see cref="BeginAttackPose"/>)가 같은 규칙입니다. 옛 반응 자세 입구 <c>BeginPose</c>는 반응이 조립으로 옮겨 걷었습니다.
        /// </summary>
        internal bool TryBeginPose(EntityId id, Action cancelled, out ICombatUnitPose pose)
        {
            pose = null;
            if (!TryGetActive(id, out var target))
                return false;
            RequireNoPlacement(id);
            pose = target.BeginPose(cancelled);
            return true;
        }

        /// <summary>
        /// 그 유닛의 서는 자리를 지금 빌릴 수 있는지입니다(켜진 표시·걸음 배치 중 아님·이미 빌리지 않음·자세 대여 없음). 조립이 미리 물어 빌릴 수 없는 행동만 뺍니다(설계 P7).
        /// 양보할 수 있는 걸음 배치(도약 중간 경유, <see cref="MarkYieldable"/>)는 빌릴 수 있음으로 봅니다. 묻기만 하고 배치는 빼지 않습니다(평가 C4-05).
        /// 자세 대여가 있으면 유닛이 밀린 자세라, 그때 빌리면 그 자리가 돌아올 자리가 됩니다(묶음 끝 검토 L5). 그래서 빌리지 않습니다.
        /// </summary>
        internal bool CanBeginStand(EntityId id) => TryGetStandTarget(id, out _);

        /// <summary>
        /// 켜진 표시의 서는 자리 대여를 시작합니다(특성 발동 유닛 행동, 유닛 행동 설계 AA2). 자세 대여의 아래 층이라 그 위에서 피격 반응이 자세를 빌릴 수 있습니다.
        /// 빌릴 수 없으면(<see cref="CanBeginStand"/>) false입니다. 사망 모델 유지 중에는 되돌리기가 자리를 쓰지 않습니다(설계 P8).
        /// 양보할 수 있는 걸음 배치는 여기서 빼고 레인에 알립니다(평가 C4-05). 그때 서는 자리는 지금 자리(공중 경유)를 돌아올 자리로 잡습니다.
        /// </summary>
        internal bool TryBeginStand(EntityId id, Action cancelled, out ICombatUnitStand stand)
        {
            stand = null;
            if (!TryGetStandTarget(id, out ICombatUnitPresentationTarget target))
                return false;
            RequireNoPlacement(id);
            stand = target.BeginStand(cancelled, () => HoldsDeath(id));
            return true;
        }

        // 서는 자리를 빌릴 수 있는 조건 한 곳입니다(묻기·빌리기가 같이 씀). 양보할 수 있는 걸음 배치는 막지 않습니다(평가 C4-05).
        private bool TryGetStandTarget(EntityId id, out ICombatUnitPresentationTarget target) =>
            TryGetActive(id, out target) && CanYieldPlacement(id) && !target.HasStand && !target.HasPose;

        /// <summary>
        /// 그 유닛이 서는 자리를 빌렸으면 지금 서는 자리입니다(유닛 행동 설계 AA3). ID로 묻는 지금 몸 자리(접촉 몸·큐 자리)가 이 값을 먼저 봅니다.
        /// </summary>
        internal bool TryGetStandPose(EntityId id, out Pose pose)
        {
            if (_targets.TryGet(id, out var target) && target.TryGetStand(out Vector3 position, out Quaternion rotation))
            {
                pose = new Pose(position, rotation);
                return true;
            }
            pose = default;
            return false;
        }

        /// <summary>
        /// 켜진 표시의 크기 배율 대여를 시작합니다(포효 커지기, 설계 D6b). 자세와 따로라 커진 채로 공격 자세를 재생할 수 있습니다.
        /// 표시가 없거나 꺼져 있으면 false입니다.
        /// </summary>
        internal bool TryBeginScale(EntityId id, Action cancelled, out ICombatUnitScale scale)
        {
            scale = null;
            if (!TryGetActive(id, out var target))
                return false;
            scale = target.BeginScale(cancelled);
            return true;
        }

        /// <summary>
        /// 걸음 보간이 그 유닛의 위치·회전을 소유하기 시작합니다(이동 접촉 설계 M5 (a)). 표시가 없어도 시작합니다.
        /// 같은 유닛의 새 대여는 이전 대여를 대신합니다. 그 유닛에 자세 대여가 있으면 예외입니다(Q-R11 A).
        /// </summary>
        internal ICombatUnitPlacement BeginPlacement(EntityId id)
        {
            if (_targets.TryGet(id, out var target) && target.HasPose)
                throw new InvalidOperationException($"[CombatUnitPresentationWindow] 같은 유닛의 자세 대여와 걸음 배치는 함께 쓸 수 없습니다: {id}.");
            // 서는 자리를 빌린 유닛이 걸으면(대신 맞은 수호자가 같은 공격의 직접 대상이기도 해 밀리는 경우 등) 서는 자리를 그 자리에서 접고 걸음을 그대로 잇습니다
            // (유닛 행동 설계 AA8 안전장치, 계획 TP-19). 대신 맞은 접촉은 밀지 않는 것이 규칙이라(Q-AA5) 드문 경우로 보고 경고를 남깁니다.
            if (target != null && target.FoldStand() && _foldWarned.Add(id))
                Debug.LogWarning($"[CombatUnitPresentationWindow] 서는 자리를 빌린 유닛이 걸어 서는 자리를 접었습니다(유닛 행동 중 밀림): {id}.");
            var placement = new Placement(this, id);
            _placements[id] = placement;
            return placement;
        }

        /// <summary>
        /// 그 유닛의 지금 걸음 배치를 다른 대여에 양보할 수 있게 표시합니다(평가 C4-05). 도약 중간 경유에서 보간을 멈추고 배치만 쥔 레인이 부릅니다.
        /// 그 뒤 자세(<see cref="BeginAttackPose"/>·<see cref="TryBeginPose"/>)나 서는 자리(<see cref="TryBeginStand"/>)를 빌리면
        /// 예외·거절 대신 이 배치를 빼고 <paramref name="yielded"/>를 한 번 부릅니다. 되찾기는 레인이 합니다.
        /// 표시는 지금 배치 하나에 붙어, 배치가 끝나거나 새 배치로 바뀌면 사라집니다. 배치가 없으면 아무 일도 하지 않습니다.
        /// </summary>
        internal void MarkYieldable(EntityId id, Action yielded)
        {
            if (yielded == null) throw new ArgumentNullException(nameof(yielded));
            if (_placements.TryGetValue(id, out Placement placement))
                placement.MarkYieldable(yielded);
        }

        /// <summary>그 유닛의 걸음 배치에서 양보 표시를 지웁니다. 쥔 배치로 다시 걷기 시작하는 레인이 부릅니다(걷는 동안은 양보하지 않음, 결정 Q-R11 A).</summary>
        internal void ClearYieldable(EntityId id)
        {
            if (_placements.TryGetValue(id, out Placement placement))
                placement.ClearYieldable();
        }

        /// <summary>
        /// 연발 앞 탄의 접촉 피격(곁들임)이 다른 대여를 빼앗거나 예외 없이 자세(<paramref name="clip"/>이면 클립도)를 빌릴 수 있는지입니다(2026-11-11).
        /// 걸음 배치가 있으면 양보할 수 있어도 거짓입니다. 곁들임은 Core 사건이 아니라서 도약 경유를 빼앗지 않습니다.
        /// </summary>
        internal bool CanBeginContactHit(EntityId id, bool clip) =>
            TryGetActive(id, out var target) && !target.HasPose && (!clip || !target.HasClip) && !_placements.ContainsKey(id);

        /// <summary>중간 사건에 넘긴 도약 배치를 다른 자세·유닛 행동을 접지 않고 되찾을 수 있는지입니다.</summary>
        internal bool CanResumePlacement(EntityId id) =>
            !_targets.TryGet(id, out var target) || !target.HasPose && !target.HasStand;

        /// <summary>지금 표시 회전입니다. 표시가 없으면 false이고 그 유닛의 표시 회전을 지웁니다.</summary>
        internal bool TryGetRotation(EntityId id, out Quaternion rotation)
        {
            if (_targets.TryGet(id, out var target))
            {
                rotation = target.Rotation;
                return true;
            }
            rotation = Quaternion.identity;
            _facing.Remove(id);
            return false;
        }

        /// <summary>이미 있는 표시만 돌리고, 사실 회전 대신 그 회전을 유지합니다. 회전 때문에 표시를 새로 빌리지 않습니다.</summary>
        internal void SetFacing(EntityId id, Quaternion rotation)
        {
            if (!TryGetRotation(id, out _))
                return;
            _facing[id] = rotation;
            if (_targets.TryGet(id, out var target))
                target.ApplyFacing(rotation);
        }

        /// <summary>표시 회전을 지우고 사실 회전으로 되돌립니다. 표시 회전이 없으면 아무 일도 하지 않습니다.</summary>
        internal void ClearFacing(CharacterCombatFacts facts, FieldGridMapper mapper)
        {
            if (!_facing.Remove(facts.EntityId))
                return;
            if (_targets.TryGet(facts.EntityId, out var target))
                target.ApplyFacing(_targets.FactRotation(facts, mapper));
        }

        /// <summary>
        /// 사실 목록 유닛의 뒤 단계 꼬리를 모두 끊습니다(클립 칸 다시 짜기 설계 P8). 연출 취소가 부릅니다. 꼬리는 실행이 끝난 뒤에도 채널이 혼자 돌리므로
        /// 실행 취소(대여 반환)만으로는 끊기지 않습니다.
        /// </summary>
        internal void CutClipTails(IReadOnlyList<CharacterCombatFacts> facts)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            for (int i = 0; i < facts.Count; i++)
                if (_targets.TryGet(facts[i].EntityId, out var target))
                    target.CutClipTail();
        }

        /// <summary>모든 표시 회전을 지웁니다. Mapper가 있으면 사실 목록의 유닛은 사실 회전으로 되돌립니다.</summary>
        internal void ClearAllFacing(IReadOnlyList<CharacterCombatFacts> facts, FieldGridMapper mapper)
        {
            if (mapper != null)
                for (int i = 0; i < facts.Count; i++)
                    ClearFacing(facts[i], mapper);
            _facing.Clear();
        }

        /// <summary>이미 빌린 켜진 모델을 사망 사실 적용 뒤에도 유지합니다. 켜진 표시가 없거나 이미 유지 중이면 예외입니다.</summary>
        internal ICombatUnitDeathHold HoldDeath(EntityId id)
        {
            if (!TryGetActive(id, out var target) || _deathHolds.ContainsKey(id))
                throw new InvalidOperationException("[CombatUnitPresentationWindow] 같은 활성 대여에서 사망 모델을 한 번만 유지할 수 있습니다.");
            var hold = new DeathHold(this, id, target);
            _deathHolds.Add(id, hold);
            return hold;
        }

        /// <summary>
        /// 켜진 표시가 있고 아직 사망 유지 중이 아니라 <see cref="HoldDeath"/>를 부를 수 있는지입니다. 반응 없이 몸만 남기는 사망(터질 유닛, 자폭 깜빡임 설계 SD5)이 먼저 묻습니다.
        /// </summary>
        internal bool CanHoldDeath(EntityId id) => TryGetActive(id, out _) && !_deathHolds.ContainsKey(id);

        /// <summary>
        /// 이동 원본을 흐리게 보입니다(입력 미리보기). 앞의 이동 투명도는 먼저 끝냅니다. 켜진 표시가 없으면 예외입니다.
        /// 적용이 실패하면 이 층을 끝낸 뒤 예외를 다시 던집니다.
        /// </summary>
        internal ICombatUnitAppearance BeginMovementOpacity(EntityId id, float opacity)
        {
            EndMovementOpacity();
            if (!TryGetActive(id, out var target))
                throw new InvalidOperationException("[CombatUnitPresentationWindow] 이동 원본의 활성 표시가 없습니다.");
            var appearance = new Appearance(this, id);
            _movementOpacity = appearance;
            try
            {
                target.SetMovementOpacity(opacity);
            }
            catch (Exception error)
            {
                try
                {
                    EndMovementOpacity();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
            return appearance;
        }

        /// <summary>그 유닛의 켜진 표시가 피격 플래시 층을 쓸 수 있는지입니다(조각·조립 설계 BP9). 피격 조립이 플래시 항목을 넣을지 정할 때 씁니다.</summary>
        internal bool CanFlash(EntityId id) => TryGetActive(id, out var target) && target.SupportsHitFlash;

        /// <summary>
        /// 켜진 표시의 피격 플래시 층 대여를 시작합니다. 유닛마다 하나이고, 이동 반투명 층과는 표시가 합쳐 씁니다.
        /// 표시가 없거나 꺼졌거나 외형 층을 쓸 수 없으면 false입니다.
        /// </summary>
        internal bool TryBeginFlash(EntityId id, Action cancelled, out ICombatUnitFlash flash)
        {
            flash = null;
            if (!TryGetActive(id, out var target) || !target.SupportsHitFlash)
                return false;
            flash = target.BeginHitFlash(cancelled);
            return true;
        }

        /// <summary>
        /// 켜진 표시의 사건 클립 대여를 시작합니다(클립 설계 AC3). 유닛마다 하나이고, 표시가 꺼지거나 다시 대여되면 취소되고 <paramref name="cancelled"/>를 부릅니다.
        /// 표시가 없거나 꺼졌거나 클립 채널이 없으면(이벤트 컴포넌트 없음) false입니다.
        /// </summary>
        internal bool TryBeginClip(EntityId id, Action cancelled, out ICombatUnitClip clip)
        {
            clip = null;
            if (!TryGetActive(id, out var target) || !target.SupportsClip)
                return false;
            clip = target.BeginClip(cancelled);
            return true;
        }

        /// <summary>켜진 표시의 피격 동작입니다(반응 설정 읽기와 같은 조건, 클립 칸 다시 짜기 설계 CS1). 묶음이나 피격 칸이 없으면 false입니다.</summary>
        internal bool TryGetHitClip(EntityId id, out CombatClipMotion clip)
        {
            clip = null;
            if (!TryGetActive(id, out var target))
                return false;
            clip = _targets.AnimationSettingsOf(target.EntityKey)?.Hit;
            return clip != null;
        }

        /// <summary>표시 중인 유닛의 공격 클립입니다(공격 설정 읽기와 같은 조건, 계획 P4). 묶음이나 공격 칸이 없으면 false입니다.</summary>
        internal bool TryGetAttackClip(EntityId id, out CombatClipAttackData clip)
        {
            clip = null;
            if (!_targets.TryGet(id, out var target))
                return false;
            clip = _targets.AnimationSettingsOf(target.EntityKey)?.Attack;
            return clip != null;
        }

        /// <summary>
        /// 그 키의 공격 연출 값입니다(없으면 null). 표시가 없어도 읽습니다 — 전투 설치 때 투사체를 미리 만들 때 씁니다(GC 끊김 설계 GH6).
        /// 공격 규칙 DB는 조회하지 않습니다.
        /// </summary>
        internal CombatAttackPresentationData AttackSettingsOf(EntityKey key) => _targets.AttackSettingsOf(key);

        /// <summary>그 유닛의 표시 대상이 있는지입니다. 공격 표시 덮어쓰기로 코드 모션을 시작할 때 표시 없는 공격자를 거릅니다(평가 C10-12).</summary>
        internal bool HasTarget(EntityId id) => _targets.TryGet(id, out _);

        /// <summary>조기 공격이 기존 자세·클립·특성 행동을 빼앗지 않고 시작할 수 있는지입니다.</summary>
        internal bool CanBeginAttackClip(EntityId id)
        {
            if (!TryGetActive(id, out var target) || !target.SupportsClip)
                throw new InvalidOperationException($"공격 클립을 재생할 활성 표시가 없습니다: {id}.");
            return !target.HasClip && !target.HasPose && !target.HasStand && CanYieldPlacement(id);
        }

        /// <summary>이미 표시 중인 유닛의 공격 연출 값만 읽습니다. 공격 규칙 DB는 조회하지 않습니다.</summary>
        internal bool TryGetAttackPresentation(EntityId id, out CombatAttackPresentationData settings)
        {
            settings = null;
            if (!_targets.TryGet(id, out var target))
                return false;
            settings = _targets.AttackSettingsOf(target.EntityKey);
            return settings != null;
        }

        /// <summary>
        /// 지금 표시의 곡사 무기입니다. 표시가 없으면 false, 무기가 없으면 예외입니다(데이터 오류).
        /// 곡사인지와 연출 값은 부르는 쪽(곡사 전략, <c>CombatMortarStyle</c>)이 이미 압니다(View 3라운드 W8, 계획 Vf3.8).
        /// </summary>
        internal bool TryGetMortarWeapon(EntityId id, out CatusCannonWeaponView weapon)
        {
            weapon = null;
            if (!_targets.TryGet(id, out var target))
                return false;
            weapon = target.MortarWeapon;
            if (weapon == null)
                throw new InvalidOperationException($"[CombatUnitPresentationWindow] 곡사 무기 연결이 없습니다: {target.EntityKey}.");
            weapon.Validate();
            return true;
        }

        /// <summary>켜진 표시의 피격·사망 연출 값만 읽습니다. 자세는 조립 실행이 따로 빌립니다(<see cref="TryBeginPose"/>, 피격 구조 정리 설계 HC1).</summary>
        internal bool TryGetReactionSettings(EntityId id, out CombatUnitReactionPresentationData settings)
        {
            settings = null;
            if (!TryGetActive(id, out var target))
                return false;
            settings = _targets.ReactionSettingsOf(target.EntityKey);
            return settings != null;
        }

        /// <summary>
        /// 켜진 표시가 코드 피격 움직임(맞은 방향으로 밀렸다 돌아오며 젖힘, 넉백 때 밀리는 동안 젖힘)을 내는지입니다(2026-11-17 사용자 "끄고 켜는 옵션").
        /// 규칙 원본은 반응 값의 <see cref="CombatUnitReactionPresentationData.PlaysHitMotion"/>이고, 그 입력(반응 값·피격 동작 유무)을 모으는 곳은 여기 하나입니다
        /// (피격 뼈대 고르기와 넉백 젖힘이 함께 부름, 피격 구조 정리 계획 §6.7 ⑥). 표시나 반응 값이 없으면 false입니다.
        /// </summary>
        internal bool PlaysHitMotion(EntityId id) =>
            TryGetReactionSettings(id, out CombatUnitReactionPresentationData settings) && settings.PlaysHitMotion(TryGetHitClip(id, out _));

        /// <summary>사망 모델 유지가 아직 같은 대여를 붙잡고 있는지입니다. 사실 그리기가 묻습니다.</summary>
        internal bool HoldsDeath(EntityId id) => _deathHolds.TryGetValue(id, out var hold) && hold.IsCurrent;

        /// <summary>
        /// 사실 그리기가 그 유닛의 위치·회전을 쓰지 않아야 하는지입니다(B14 O19). 사망 모델 유지 중이거나 걸음 보간이 배치를 소유 중이거나
        /// 유닛 행동이 서는 자리를 빌렸으면 true입니다(서는 자리는 유닛 행동 계획 TP-18).
        /// </summary>
        internal bool KeepsPlacement(EntityId id) => HoldsDeath(id) || _placements.ContainsKey(id) ||
            (_targets.TryGet(id, out var target) && target.HasStand);

        /// <summary>사실 회전을 덮는 표시 회전입니다(B14 O19).</summary>
        internal bool TryGetFacing(EntityId id, out Quaternion facing) => _facing.TryGetValue(id, out facing);

        /// <summary>사실에서 사라진 유닛의 표시 회전을 지웁니다. 표시는 건드리지 않습니다.</summary>
        internal void ForgetFacing(EntityId id) => _facing.Remove(id);

        /// <summary>그 유닛이 이동 투명도를 가졌으면 끝냅니다. 사실에서 필드를 떠났거나 죽은 유닛에 씁니다.</summary>
        internal void EndMovementOpacityFor(EntityId id)
        {
            if (_movementOpacity != null && _movementOpacity.Id == id)
                EndMovementOpacity();
        }

        /// <summary>이동 투명도를 원래 외형으로 되돌립니다. 반복 호출해도 안전합니다.</summary>
        internal void EndMovementOpacity()
        {
            Appearance current = _movementOpacity;
            _movementOpacity = null;
            if (current != null && _targets.TryGet(current.Id, out var target))
                target.ClearMovementOpacity();
        }

        /// <summary>
        /// 표시가 풀로 돌아갔습니다. 그 유닛의 표시 회전과 이동 투명도 주인을 지웁니다. 반환 표시는 풀 정리가 외형을 되돌립니다.
        /// </summary>
        internal void OnReturned(EntityId id)
        {
            _facing.Remove(id);
            if (_movementOpacity != null && _movementOpacity.Id == id)
                _movementOpacity = null;
        }

        /// <summary>
        /// 모든 사망 유지·표시 회전·걸음 배치를 버립니다. 표시 집합을 비우기 전에 불러, 반환 콜백의 반응 취소가 같은 대여를 다시 반환하지 않게 합니다.
        /// </summary>
        internal void Reset()
        {
            _deathHolds.Clear();
            _facing.Clear();
            _placements.Clear();
            _foldWarned.Clear();
        }

        private bool TryGetActive(EntityId id, out ICombatUnitPresentationTarget target) =>
            _targets.TryGet(id, out target) && target.IsActive;

        // 자세·서는 자리를 빌리기 전에 그 유닛의 걸음 배치를 비웁니다. 양보 표시가 있는 배치(도약 중간 경유)는 빼고 레인에 알리고(평가 C4-05),
        // 걷는 중인 배치면 예외입니다(결정 Q-R11 A).
        private void RequireNoPlacement(EntityId id)
        {
            if (!_placements.TryGetValue(id, out Placement placement))
                return;
            if (!placement.IsYieldable)
                throw new InvalidOperationException($"[CombatUnitPresentationWindow] 같은 유닛의 자세 대여와 걸음 배치는 함께 쓸 수 없습니다: {id}.");
            _placements.Remove(id);
            placement.Yield();
        }

        // 그 유닛에 걸음 배치가 없거나, 있어도 양보할 수 있는지입니다. 묻기만 하고 배치는 빼지 않습니다.
        private bool CanYieldPlacement(EntityId id) =>
            !_placements.TryGetValue(id, out Placement placement) || placement.IsYieldable;

        private sealed class Placement : ICombatUnitPlacement
        {
            private readonly CombatUnitPresentationWindow _owner;
            private readonly EntityId _id;
            // 양보 콜백입니다(평가 C4-05). 도약 중간 경유에서 레인이 표시하고, 다시 걷기 시작하거나 양보하면 비웁니다.
            private Action _yielded;

            internal Placement(CombatUnitPresentationWindow owner, EntityId id)
            {
                _owner = owner;
                _id = id;
            }

            public bool IsCurrent => _owner._placements.TryGetValue(_id, out var current) && ReferenceEquals(current, this);

            internal bool IsYieldable => _yielded != null;

            internal void MarkYieldable(Action yielded) => _yielded = yielded;

            internal void ClearYieldable() => _yielded = null;

            // 창구가 이 배치를 뺀 뒤 부릅니다. 표시를 지우고 레인에 한 번 알립니다.
            internal void Yield()
            {
                Action yielded = _yielded;
                _yielded = null;
                yielded();
            }

            // 레인은 자기 유닛의 걸음만 보간하므로 사실의 ID는 이 대여의 ID와 같습니다.
            public void Interpolate(CharacterCombatFacts from, CharacterCombatFacts to, FieldGridMapper mapper, float progress, CombatStepStyle style)
            {
                if (!_owner._targets.TryGet(_id, out var target)) return;
                // 사실 위치(표면 높이 포함)에서 높이를 빼 가로만 직선으로 잇고, 높이는 걸음 모양이 정합니다(고지대 View 설계 HG-3, 걸음 모양 설계 ST3).
                // 걷기 모양이 같은 높이를 받으면 높이 0을 빼고 더하므로 옛 직선 보간과 같습니다.
                float fromHeight = mapper.SurfaceHeightOf(from.OccupiedCells, from.Anchor.Value);
                float toHeight = mapper.SurfaceHeightOf(to.OccupiedCells, to.Anchor.Value);
                Vector3 start = _owner._targets.FactPosition(from, mapper) - mapper.SurfaceOffset(fromHeight);
                Vector3 end = _owner._targets.FactPosition(to, mapper) - mapper.SurfaceOffset(toHeight);
                target.SetPlacementPosition(Vector3.Lerp(start, end, progress)
                    + mapper.SurfaceOffset(style.HeightAt(fromHeight, toHeight, progress)));
            }

            public void Restore()
            {
                if (IsCurrent)
                    _owner._placements.Remove(_id);
            }
        }

        private sealed class DeathHold : ICombatUnitDeathHold
        {
            private readonly CombatUnitPresentationWindow _owner;
            private readonly EntityId _id;
            private readonly ICombatUnitPresentationTarget _target;

            internal DeathHold(CombatUnitPresentationWindow owner, EntityId id, ICombatUnitPresentationTarget target)
            {
                _owner = owner;
                _id = id;
                _target = target;
            }

            public bool IsCurrent => _owner._deathHolds.TryGetValue(_id, out var current) && ReferenceEquals(current, this) &&
                _owner._targets.IsRented(_id, _target) && _target.EntityId == _id;

            public void Release()
            {
                if (!_owner._deathHolds.TryGetValue(_id, out var current) || !ReferenceEquals(current, this))
                    return;
                bool releaseRental = IsCurrent;
                _owner._deathHolds.Remove(_id);
                if (releaseRental)
                    _owner._targets.ReleaseRental(_id, _target);
            }
        }

        private sealed class Appearance : ICombatUnitAppearance
        {
            private readonly CombatUnitPresentationWindow _owner;

            internal Appearance(CombatUnitPresentationWindow owner, EntityId id)
            {
                _owner = owner;
                Id = id;
            }

            internal EntityId Id { get; }

            public bool IsCurrent => ReferenceEquals(_owner._movementOpacity, this);

            public void Restore()
            {
                if (IsCurrent)
                    _owner.EndMovementOpacity();
            }
        }
    }
}
