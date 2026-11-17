using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 특성 발동 사건(<see cref="CombatEffectsEvent"/>)의 표시입니다(특성 발동 연출 설계 TA1·TA3·TA6). 사건의 특성 발동 목록을 차례로 보고,
    /// 발동마다 특성 외형 표의 발동 연출 줄(특성 키, 발동 종류)의 큐를 곁들임 실행으로 냅니다. 줄이 없는 발동은 큐가 없습니다.
    /// 추가 공격 시작 발동(난사·2연발·연쇄 폭발·행운의 탄환, Core 사실에 추가 공격 결과가 있음)이면 발동 유닛이 공격 연출을 칩니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 큐만 있는 줄은 사건을 막지 않습니다(결정 Q-TA4 A). 그래서 친 공격이 없으면 false를 돌려줘 틀이 사실 반영·완료로 이어 갑니다(계획 P6).
    /// 한 사건의 발동이 여럿이면 목록 순서대로 내고, 큐는 기다리지 않으니 사실상 함께 나옵니다(결정 Q-TA6 A).
    /// 줄 값(큐·공격 표시·유닛 행동)은 발동 줄 보관(<see cref="CombatActivationRows"/>, 대신 맞기 앞 항목과 같은 것)이 연결 동안 보관하고, 큐 자리(발동 유닛 몸·대상 몸·영향 칸)도 여기서 보인 사실로 만들어 곁들임 실행 입구에 넘깁니다(묶음 끝 검토 L3).
    /// </para>
    /// <para>
    /// 추가 공격은 발동 사건을 출처로 공격 시작(<see cref="CombatAttackStart"/>)을 부르고 true를 돌려줍니다. 칸·방향은 발동 사실에서 옮기고(<see cref="CombatAttackShape.ForChildAttack"/>),
    /// 사건 완료는 보통 공격처럼 첫 유효 해제입니다. 뒤따르는 피해는 그 공격 그룹으로 이 출처에 맺혀 맞는 순간에 시작합니다(특성 시스템 View 계획 R1·TP-2).
    /// 판정은 형식 표의 <see cref="CombatEventTypeTable.IsChildAttackStart"/> 하나이고, 연출 줄이 없어도 칩니다(결정 Q-TS1 A). 같은 판정이 이 사건을 출처를 여는 사건으로 만들어,
    /// 이전 공격 출처가 끝나고 모든 유닛 연출이 쉰 뒤에 시작합니다(결정 Q-TD1 A, 검토 M1·L5).
    /// 공격 표시는 줄의 덮어쓰기, 없으면 발동 유닛의 일반 공격 표시입니다(결정 Q-TA3 B). 친 공격의 공격 소리는 아직 없습니다(소리 표가 특성 발동 출처에 유닛 소리 줄을 고르지 않음, 계획 P5 — 유닛 소리 계획 TP-7).
    /// </para>
    /// <para>
    /// Core는 추가 공격 시작을 발동 하나짜리 사건으로 냅니다(<c>AttackResultFactory.AttachChild</c>). 발동이 둘 이상인 사건에 추가 공격 시작이 있으면 계약 오류로 던집니다(계획 TP-6). 이 검사와 추가 공격 방향 검사는 큐·행동을 내기 전에 합니다(묶음 끝 검토 L6).
    /// 발동 유닛이 보인 사실에 없으면(몸이 사라짐) 공격 연출 없이 사건을 이어 갑니다. 직군 발동(Categories)은 보이지 않습니다.
    /// 반영할 사실이 없어 <see cref="CombatEventHandler.ApplyFacts"/>는 그대로 둡니다.
    /// </para>
    /// <para>
    /// 줄에 유닛 행동이 있으면 행동 유닛마다 유닛 실행 하나(나가기·돌기 → 머물기 → 돌아오기)를 엽니다(유닛 행동 설계 AA7, 계획 Tf5.1).
    /// 사건은 기다리지 않고 바로 완료하며(일반 완료, 완료 통지 없음 — 계획 TP-23·설계 P11), 행동 유닛 자신의 다음 사건·다음 행동은 유닛 실행 규칙대로 그 실행 끝까지 기다립니다.
    /// 행동 유닛이 다른 유닛 실행 중이거나 서는 자리를 빌릴 수 없으면 이번 행동만 건너뛰고 경고를 한 번 남깁니다(결정 Q-AA6 A, 큐는 그대로 냄).
    /// 행동 유닛이 도약 중간 경유에 있으면 서는 자리를 빌릴 때 창구가 그 걸음 배치를 양보받습니다(평가 C4-05). 여기서 따로 내려놓지 않습니다.
    /// 추가 공격이면서 행동 값이 있는 줄은 공격만 하고 행동은 경고 한 번 뒤 건너뜁니다(설계 P9). 지켜진 유닛 앞(GuardFront)은 대신 맞기에만 뜻이 있어
    /// 경고 한 번 뒤 제자리(Stay)로 합니다(설계 P10). 가로채기 발동은 공격 시작이 공격 조립 안에서 다룹니다(설계 AA6).
    /// </para>
    /// </remarks>
    internal sealed class CombatTraitEventHandler : CombatEventHandler
    {
        private readonly CombatAttackStart _attacks;
        private readonly CombatShownFacts _facts;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly float _hopHeight;
        // 발동 피드백입니다(큐·몸 글자·줄 고르기). 운영과 특성 연출 확인 도구 모두 넘겨서 필수입니다(이펙트 끝 규칙 계획 TP-E28).
        private readonly CombatTraitFeedbackPlayback _feedback;
        // 터질 유닛의 몸 유지입니다. 자폭 발동이 시작하면 큐보다 먼저 놓아, 몸이 폭발과 같은 순간에 사라집니다(자폭 깜빡임 설계 SD5).
        private readonly CombatSelfDestructDeaths _selfDestruct;
        private readonly CombatPositionSwapPresentation _positionSwap;
        // 유닛 행동을 건너뛰거나 고쳐 쓴 줄을 경고한 기록입니다. 사유마다 따로 두고 연결 동안 한 번씩만 남깁니다(묶음 끝 검토 L10).
        private readonly HashSet<(TraitKey Trait, TraitActivationKind Kind)> _childActionWarned = new();
        private readonly HashSet<(TraitKey Trait, TraitActivationKind Kind)> _guardFrontWarned = new();
        private readonly HashSet<(TraitKey Trait, TraitActivationKind Kind)> _actionBusyWarned = new();

        /// <param name="pieces">조각 실행 재생기입니다. 유닛 행동 실행을 엽니다(유닛 행동 계획 Tf5.1).</param>
        /// <param name="hopHeight">유닛 행동 뛰기 높이입니다(걸음 모양 설정의 뛰기 높이, 계획 TP-21).</param>
        /// <param name="feedback">발동 피드백입니다. 줄(방향까지 본 줄)·큐·몸 글자를 맡습니다.</param>
        /// <param name="selfDestruct">터질 유닛의 몸 유지입니다(자폭 깜빡임 설계 SD5, 필수).</param>
        internal CombatTraitEventHandler(CombatAttackStart attacks, CombatShownFacts facts, CombatPieceRunPlayer pieces,
            float hopHeight, CombatTraitFeedbackPlayback feedback, CombatSelfDestructDeaths selfDestruct,
            CombatPositionSwapPresentation positionSwap = null)
        {
            _attacks = attacks ?? throw new ArgumentNullException(nameof(attacks));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            // 값 조건은 걸음 모양 설정이 이미 검사했습니다(CombatStepLookSettings).
            _hopHeight = hopHeight;
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _selfDestruct = selfDestruct ?? throw new ArgumentNullException(nameof(selfDestruct));
            _positionSwap = positionSwap;
        }

        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version)
        {
            var effects = (CombatEffectsEvent)e;
            IReadOnlyList<TraitActivationSnapshot> traits = effects.Traits;
            // 계약 검사(추가 공격 시작은 발동 하나·방향 있음)는 큐·행동을 내기 전에 합니다(묶음 끝 검토 L6). 어기면 아무것도 내지 않고 멈춥니다.
            TraitActivationSnapshot child = ChildAttackOf(traits);
            // 발동 유닛이 보인 사실에 없으면(몸이 사라짐, 인계 §3) 추가 공격은 치지 않습니다(계획 TP-6).
            CombatAttackShape childShape = child != null && _facts.FindCharacter(child.SourceUnitId) != null
                ? CombatAttackShape.ForChildAttack(effects, child)
                : null;
            CombatAttackPresentationData childPresentation = null;
            bool positionSwapBegun = false;
            // 발동 유닛·대상의 몸 자리는 사실 반영 전 보인 사실로 구합니다(순간 큐와 같은 시점).
            for (int i = 0; i < traits.Count; i++)
            {
                // 자폭 발동이면 깜빡이던 몸을 여기서 내립니다. 큐(폭발)보다 먼저라 몸이 사라지는 프레임에 폭발이 납니다(자폭 깜빡임 설계 SD5).
                _selfDestruct.Detonate(traits[i]);
                if (_positionSwap?.TryBegin(effects, i, completed, version) == true)
                {
                    positionSwapBegun = true;
                    continue;
                }
                TraitActivationSnapshot trait = traits[i];
                // 피드백과 같은 줄입니다(결속 오를 때·내릴 때처럼 방향까지 본 줄, 이펙트 끝 규칙 계획 Ee.2).
                CombatActivationRow row = _feedback.RowOf(effects, i);
                bool isChild = ReferenceEquals(trait, child);
                if (isChild)
                    childPresentation = row.Presentation;
                // 행동 끝 큐가 있으면 이 발동의 행동 묶음을 만듭니다. 큐를 낸 뒤 행동을 열고, 다 연 뒤 닫습니다(계획 TP-E3).
                CombatEffectActionGroup group = row.Exists && row.Cues.EndsWithAction ? new CombatEffectActionGroup() : null;
                _feedback.Start(effects, i, group);
                // 큐 자리를 정한 뒤 행동을 엽니다. 큐는 행동 유닛이 움직이기 전 자리에 납니다.
                if (row.Exists)
                    for (int actionIndex = 0; actionIndex < row.Actions.Count; actionIndex++)
                        BeginUnitActions(trait, row.Actions[actionIndex], isChild, version, group);
                group?.Seal();
            }
            // 추가 공격 시작 발동의 공격 연출입니다(특성 시스템 View 계획 Tb.5). 공격 표시는 줄의 덮어쓰기, 없으면 발동 유닛의 일반 공격 표시입니다.
            return positionSwapBegun || childShape != null && _attacks.TryBegin(childShape, childPresentation, completed, version);
        }

        /// <summary>후속 특성 행동이 같은 유닛의 채널을 사용할 수 있는지입니다. 조기 공격이 그 행동을 생략시키지 않도록 먼저 확인합니다.</summary>
        internal bool MayUseUnitAction(CombatEffectsEvent effects, EntityId actor)
        {
            for (int i = 0; i < effects.Traits.Count; i++)
            {
                TraitActivationSnapshot trait = effects.Traits[i];
                if (trait.ChildAttack != null || CombatAttackShape.IsInterception(trait)) continue;
                CombatActivationRow row = _feedback.RowOf(effects, i);
                if (!row.Exists) continue;
                bool isTarget = false;
                for (int j = 0; j < trait.TargetIds.Count; j++)
                    isTarget |= trait.TargetIds[j] == actor;
                if (row.PositionSwap != null && (trait.SourceUnitId == actor || isTarget)) return true;
                for (int j = 0; j < row.Actions.Count; j++)
                    if (row.Actions[j].Actor == CombatUnitActionActor.Source ? trait.SourceUnitId == actor : isTarget)
                        return true;
            }
            return false;
        }

        // 추가 공격 시작 발동입니다. 없으면 null입니다. Core는 추가 공격 시작을 발동 하나짜리 사건으로 내므로(AttackResultFactory.AttachChild), 아니면 계약 오류입니다(계획 TP-6).
        private static TraitActivationSnapshot ChildAttackOf(IReadOnlyList<TraitActivationSnapshot> traits)
        {
            TraitActivationSnapshot child = null;
            for (int i = 0; i < traits.Count && child == null; i++)
                if (CombatEventTypeTable.IsChildAttackStart(traits[i]))
                    child = traits[i];
            if (child != null && traits.Count != 1)
                throw new InvalidOperationException(
                    $"[CombatTraitEventHandler] 추가 공격 시작 사건에는 발동이 하나뿐이어야 합니다(Core 계약, AttackResultFactory.AttachChild). 발동 수: {traits.Count}");
            return child;
        }

        // 사건 목록 안 발동의 유닛 행동입니다(유닛 행동 설계 AA7). 행동 유닛 = 발동 유닛(Source) 또는 대상마다(Targets, 설계 P1)이고,
        // 대상(보기·대상 쪽)은 Source면 발동의 대상, Targets면 발동 유닛입니다(계획 TP-29).
        private void BeginUnitActions(TraitActivationSnapshot trait, CombatUnitActionData action, bool childAttack, int version, CombatEffectActionGroup group)
        {
            // 가로채기(대신 맞기)는 공격 결과 안 반응이라 대신 맞기 앞 항목이 공격 조립 안에서 냅니다(설계 AA6). 사건 목록에 와도 여기서는 하지 않습니다.
            if (CombatAttackShape.IsInterception(trait))
                return;
            (TraitKey, TraitActivationKind) key = (trait.TraitKey, trait.TraitActivationKind);
            if (childAttack)
            {
                // 추가 공격은 공격 동작이 자세를 잡으므로 행동을 건너뜁니다(설계 P9).
                if (_childActionWarned.Add(key))
                    Debug.LogWarning($"[CombatTraitEventHandler] 추가 공격 발동 줄에 유닛 행동이 있습니다. 공격만 하고 행동은 건너뜁니다: {trait.TraitKey} {trait.TraitActivationKind}");
                return;
            }
            CombatUnitActionPlace place = action.Place;
            if (place == CombatUnitActionPlace.Relocate)
            {
                BeginRelocations(trait, key, action, version, group);
                return;
            }
            if (place == CombatUnitActionPlace.GuardFront || place == CombatUnitActionPlace.AwayFromAttacker)
            {
                // 지켜진 유닛 앞은 가로채기에만 기준점(지켜진 유닛·공격자)이 있습니다(설계 P10).
                if (_guardFrontWarned.Add(key))
                    Debug.LogWarning($"[CombatTraitEventHandler] {place}는 대신 맞기에만 기준점이 있습니다. 제자리(Stay)로 합니다: {trait.TraitKey} {trait.TraitActivationKind}");
                place = CombatUnitActionPlace.Stay;
            }
            if (action.Actor == CombatUnitActionActor.Source)
            {
                BeginUnitAction(trait, key, action, place, trait.SourceUnitId, TargetOf(trait), version, group);
                return;
            }
            for (int i = 0; i < trait.TargetIds.Count; i++)
                BeginUnitAction(trait, key, action, place, trait.TargetIds[i], trait.SourceUnitId, version, group);
        }

        // 위치 이동 행동입니다(이펙트 끝 규칙 설계 EL11). 행동 유닛 = 발동 유닛(Source) 또는 대상마다(Targets)이고,
        // 그중 미뤄 둔 배치가 있는 유닛(위치 교환 승객)만 합니다. 없으면(이동자) 경고 없이 건너뜁니다(계획 TP-E6).
        private void BeginRelocations(TraitActivationSnapshot trait, (TraitKey, TraitActivationKind) key, CombatUnitActionData action, int version,
            CombatEffectActionGroup group)
        {
            if (action.Actor == CombatUnitActionActor.Source)
            {
                BeginRelocation(trait, key, action, trait.SourceUnitId, version, group);
                return;
            }
            for (int i = 0; i < trait.TargetIds.Count; i++)
                BeginRelocation(trait, key, action, trait.TargetIds[i], version, group);
        }

        // 미뤄 둔 배치를 가져가 그 사실 자리로 한 번 옮깁니다. 행동이 끝나면 가져간 사실을 반영하고 행동 묶음을 닫습니다(계획 TP-E7 ②).
        // 시작하지 못하면 가져간 것을 돌려놓아 사건 Presenter의 놓기가 바로 반영합니다(순간 이동, 계획 Ef.6).
        private void BeginRelocation(TraitActivationSnapshot trait, (TraitKey, TraitActivationKind) key, CombatUnitActionData action, EntityId actor,
            int version, CombatEffectActionGroup group)
        {
            if (actor.IsEmpty || !_facts.TryClaimPlacement(actor, out CharacterCombatFacts destination))
                return;
            if (_facts.StandingPlaceOf(actor) != null && !_pieces.HasUnitRun(actor))
            {
                Action close = group?.Open();
                CombatComposition relocate = CombatUnitActionComposer.ComposeRelocate(actor, action, _facts.StandingPoseOf(destination), _hopHeight);
                if (_pieces.TryBegin(relocate, () =>
                    {
                        _facts.ReleaseClaimedPlacement(actor);
                        close?.Invoke();
                    }, version))
                {
                    return;
                }
                close?.Invoke();
            }
            _facts.UnclaimPlacement(actor);
            if (_actionBusyWarned.Add(key))
                Debug.LogWarning($"[CombatTraitEventHandler] 위치 이동 유닛이 다른 연출 중이라 바로 새 자리에 놓습니다: {trait.TraitKey} {trait.TraitActivationKind} {actor}");
        }

        // 행동 유닛 하나의 유닛 실행을 엽니다. 보인 사실의 필드 위에 없으면(몸이 사라짐) 하지 않습니다.
        // 그 유닛의 유닛 실행이 이미 있거나 서는 자리를 빌릴 수 없으면(실행이 시작하지 않음) 이번 행동만 건너뜁니다(결정 Q-AA6 A).
        private void BeginUnitAction(TraitActivationSnapshot trait, (TraitKey, TraitActivationKind) key, CombatUnitActionData action,
            CombatUnitActionPlace place, EntityId actor, EntityId target, int version, CombatEffectActionGroup group)
        {
            if (actor.IsEmpty || _facts.StandingPlaceOf(actor) == null)
                return;
            if (!_pieces.HasUnitRun(actor))
            {
                // 행동 끝 이펙트는 이 행동이 끝날 때 묶음이 셉니다(계획 TP-E3). 시작하지 못하면 바로 닫습니다.
                Action close = group?.Open();
                if (_pieces.TryBegin(CombatUnitActionComposer.ComposeMoment(actor, action, place, target, _facts.StandingPlaceOf, _hopHeight), close, version))
                {
                    return;
                }
                close?.Invoke();
            }
            if (_actionBusyWarned.Add(key))
                Debug.LogWarning($"[CombatTraitEventHandler] 행동 유닛이 다른 연출 중이라 이번 유닛 행동을 건너뜁니다(큐는 냄): {trait.TraitKey} {trait.TraitActivationKind} {actor}");
        }

        // 발동 유닛이 행동할 때의 대상입니다. 대상이 여럿이면 첫 대상, 없으면 비어 있습니다(그때 대상 쪽은 제자리, 대상 보기는 지금 방향).
        private static EntityId TargetOf(TraitActivationSnapshot trait) => trait.TargetIds.Count > 0 ? trait.TargetIds[0] : EntityId.Empty;
    }
}
