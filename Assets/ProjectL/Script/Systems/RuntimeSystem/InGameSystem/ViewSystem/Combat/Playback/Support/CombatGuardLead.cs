using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 공격 결과 안 가로채기(대신 맞기)로 공격 조립의 앞 항목(<see cref="CombatAttackLead"/>)을 만드는 한 곳입니다(유닛 행동 설계 AA6, 묶음 끝 검토 M2에서 공격 시작에서 뺌).
    /// 공격 시작(<see cref="CombatAttackStart"/>)은 이것이 돌려준 앞 항목을 공격 조립 둘에 넘기기만 합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 수호자 = 가로채기의 보호자(희생은 Core가 보호자 = 발동 유닛을 검사, 설계 P1), 지켜진 유닛 = 보호 대상입니다. 가로채기마다 그 발동 줄(특성, 발동 종류)을
    /// 발동 줄 보관(<see cref="CombatActivationRows"/>)에서 찾아 반응 줄 큐와 행동을 모읍니다.
    /// </para>
    /// <para>
    /// 앞 항목은 조립을 만든 직후 반응 줄의 큐(실행 시작 = 나가기 시작, 설계 P6, 자리는 나가기 전)와 수호자 나가기를 넣고, 뼈대의 맞는 순간 뒤 대기 → 돌아오기를 붙입니다.
    /// 수호자 서는 자리를 빌릴 수 없으면 그 수호자만 빼고(설계 P7), 줄이 없거나 행동이 없으면 지금처럼 제자리에서 맞습니다.
    /// 행동 목록의 Source는 수호자, Targets는 지켜진 유닛입니다. 서로 다른 유닛의 나가기를 동시에 시작하고 실제 완료를 모두 기다립니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatGuardLead
    {
        private readonly CombatActivationRows _rows;
        private readonly CombatShownFacts _facts;
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly CombatEffectWindow _effects;
        private readonly CombatCameraWindow _camera;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly float _hopHeight;
        private readonly CombatTraitFeedbackPlayback _feedback;
        private readonly Func<ICombatSourceSettlement> _settlement;
        // 대신 맞기 줄에 넣은 위치 이동 행동을 경고한 기록입니다(연결 동안 줄마다 한 번).
        private readonly HashSet<(TraitKey Trait, TraitActivationKind Kind)> _relocateWarned = new();
        /// <param name="rows">발동 줄 보관입니다. 특성 처리기와 같은 것을 씁니다.</param>
        /// <param name="facts">보인 사실입니다. 큐 자리(몸)와 행동 자리의 기준점(서 있는 자리)을 여기서 구합니다.</param>
        /// <param name="presentation">유닛 표시 창구입니다. 수호자 서는 자리를 빌릴 수 있는지 묻습니다.</param>
        /// <param name="hopHeight">유닛 행동 뛰기 높이입니다(걸음 모양 설정의 뛰기 높이, 계획 TP-21).</param>
        /// <param name="settlement">순차 재생기의 후속 정리 질문입니다. 재생기가 붙은 뒤에야 있으므로 쓸 때 읽습니다(계획 TP-13). 수호자 돌아오기가 씁니다.</param>
        internal CombatGuardLead(CombatActivationRows rows, CombatShownFacts facts, CombatUnitPresentationWindow presentation,
            CombatEffectWindow effects, CombatCameraWindow camera, Func<FieldGridMapper> mapper, float hopHeight,
            Func<ICombatSourceSettlement> settlement, CombatTraitFeedbackPlayback feedback = null)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            // 값 조건은 걸음 모양 설정이 이미 검사했습니다(CombatStepLookSettings).
            _hopHeight = hopHeight;
            _feedback = feedback;
            _settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
        }

        /// <summary>
        /// 그 공격 모양의 앞 항목입니다. 발동 줄이 있는 가로채기가 없으면 null(조립이 지금과 같음)입니다.
        /// 공격 모양의 반응 목록은 가로채기만 담습니다(<see cref="CombatAttackShape.Reactions"/>).
        /// </summary>
        internal CombatAttackLead LeadOf(CombatAttackShape shape)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            IReadOnlyList<TraitActivationSnapshot> reactions = shape.Reactions;
            List<CombatUnitActionComposer.Guard> guards = null;
            bool hasText = false;
            List<(CombatEventCues Cues, TraitActivationSnapshot Trait)> cues = null;
            for (int i = 0; i < reactions.Count; i++)
            {
                TraitActivationSnapshot trait = reactions[i];
                CombatActivationRow row = _rows.Of(trait);
                if (!row.Exists) continue;
                hasText |= row.Text != null;
                if (!row.Cues.IsEmpty)
                    (cues ??= new List<(CombatEventCues, TraitActivationSnapshot)>()).Add((row.Cues, trait));
                AttackInterceptionSnapshot interception = trait.Interception;
                for (int actionIndex = 0; actionIndex < row.Actions.Count; actionIndex++)
                {
                    // 위치 이동은 미뤄 둔 배치가 있는 유닛만 하는 행동이라 대신 맞기에서는 뜻이 없습니다(이펙트 끝 규칙 계획 TP-E5). 경고 한 번 뒤 뺍니다.
                    if (row.Actions[actionIndex].Place == CombatUnitActionPlace.Relocate)
                    {
                        if (_relocateWarned.Add((trait.TraitKey, trait.TraitActivationKind)))
                            Debug.LogWarning($"[CombatGuardLead] 대신 맞기 줄의 위치 이동(Relocate) 행동은 하지 않습니다: {trait.TraitKey} {trait.TraitActivationKind}");
                        continue;
                    }
                    (guards ??= new List<CombatUnitActionComposer.Guard>()).Add(
                        new CombatUnitActionComposer.Guard(interception.GuardianId, interception.ProtectedId, row.Actions[actionIndex]));
                }
            }
            if (hasText && _feedback == null)
                throw new InvalidOperationException("[CombatGuardLead] 몸체 텍스트 피드백이 연결되지 않았습니다.");
            if (guards == null && cues == null && !hasText) return null;
            IReadOnlyList<CombatUnitActionComposer.Guard> added = Array.Empty<CombatUnitActionComposer.Guard>();
            return new CombatAttackLead(
                composition =>
                {
                    foreach (TraitActivationSnapshot trait in reactions) _feedback?.AddText(composition, trait);
                    // 큐 자리는 나가기 전(지금 서 있는 곳)입니다. 큐는 시작 시점만 받습니다(특성 발동 큐 규칙).
                    if (cues != null)
                        for (int i = 0; i < cues.Count; i++)
                            CombatCueComposer.Add(composition, new CombatCueAnchors(), cues[i].Cues,
                                CombatCuePoints.ForActivation(cues[i].Trait, _facts.StandingBodyOf, _mapper), new CombatCueSinks(_effects, _camera, null, null));
                    if (guards == null) return CombatPieceStart.AtStart();
                    return CombatUnitActionComposer.AddGuards(composition, guards, shape.AttackerId, _facts.StandingPlaceOf, _presentation.CanBeginStand,
                        _hopHeight, out added);
                },
                (composition, hitItem) =>
                {
                    if (added.Count == 0) return;
                    ICombatSourceSettlement settlement = _settlement() ??
                        throw new InvalidOperationException("[CombatGuardLead] 순차 재생기가 붙기 전에는 수호자 돌아오기를 만들 수 없습니다.");
                    CombatUnitActionComposer.AddReturns(composition, added, hitItem, shape.Source, settlement, _hopHeight);
                });
        }

    }
}
