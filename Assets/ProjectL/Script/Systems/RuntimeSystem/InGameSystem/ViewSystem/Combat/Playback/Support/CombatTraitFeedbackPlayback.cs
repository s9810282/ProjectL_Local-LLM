using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>상태 Render 뒤의 발동 피드백과 일반 발동 텍스트를 기존 조각 실행으로 보냅니다.</summary>
    internal sealed class CombatTraitFeedbackPlayback
    {
        private readonly CombatActivationRows _rows;
        private readonly CombatMomentCuePlayback _cues;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatBodyTextWindow _text;
        private readonly CombatShownFacts _facts;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly Func<int> _generation;
        private readonly HashSet<string> _warned = new();
        private readonly CombatTraitFeedbackSession _session = new();
        private readonly CombatTraitStateEvidenceRegistry _evidence;
        // 오래 보이는 이펙트 부품입니다. 발동 시작을 알려 상태 외형의 다시 내기 발동이면 그 유지 이펙트를 다시 내게 합니다(이펙트 끝 규칙 계획 TP-E13).
        private readonly CombatPersistentEffects _persistent;
        private readonly CombatPreAttackPresentationStarts _preAttackStarts;
        internal CombatTraitFeedbackPlayback(CombatActivationRows rows, CombatMomentCuePlayback cues, CombatPieceRunPlayer pieces, CombatBodyTextWindow text, CombatShownFacts facts, Func<FieldGridMapper> mapper, Func<int> generation, CombatTraitStateEvidenceRegistry evidence, CombatPersistentEffects persistent,
            CombatPreAttackPresentationStarts preAttackStarts = null)
        {
            _preAttackStarts = preAttackStarts;
            _persistent = persistent ?? throw new ArgumentNullException(nameof(persistent));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _cues = cues ?? throw new ArgumentNullException(nameof(cues));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _text = text;
            _evidence = evidence;
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        internal void Begin(CombatResult result)
        {
            var plan = CombatTraitFeedbackPlan.Build(result, _rows, _evidence);
            _session.Begin(plan);
            foreach (string issue in plan.Issues)
                if (_warned.Add(issue))
                    Debug.LogWarning("[CombatTraitFeedback] " + issue);
        }

        internal void Clear()
        {
            _session.Clear();
        }

        /// <summary>
        /// 발동 하나의 시작 피드백(발동 순간 큐·몸 글자)입니다. <paramref name="actionGroup"/>은 그 발동의 행동 끝 이펙트 묶음이고(이펙트 끝 규칙 계획 TP-E3),
        /// 끝 <c>UnitAction</c> 큐를 그 묶음에 맡깁니다. 행동이 없는 발동은 null입니다.
        /// </summary>
        internal void Start(CombatEffectsEvent effects, int index, CombatEffectActionGroup actionGroup = null)
        {
            TraitActivationSnapshot trait = effects.Traits[index];
            int generation = _generation();
            if (!_session.TryStart(effects, index, out var occurrence))
                return;
            // 이 발동 종류로 다시 내는 유지 이펙트(상태 외형 restartOn)를 거둡니다. 이 사건의 사실 그리기가 새로 냅니다(결정 Q-EL3 A). 줄이 없는 발동도 신호입니다.
            _persistent.NotifyActivation(trait);
            // 줄은 계획이 방향까지 보고 고른 것입니다(결속 오를 때·내릴 때, 이펙트 끝 규칙 설계 EL7).
            CombatActivationRow row = occurrence.Row;
            if (!row.Exists || row.Timing != CombatActivationCueTiming.ActivationStart)
                return;
            if (_cues.TryPlayAccent(row.Cues, CombatCuePoints.ForActivation(trait, _facts.StandingBodyOf, _mapper), actionGroup))
                _preAttackStarts?.TraitStarted(trait, generation);
            var composition = CombatComposition.ForAccent();
            if (generation != _generation())
                return;
            AddText(composition, row, trait, null, occurrence.Cells);
            Play(composition);
        }

        /// <summary>
        /// 그 발동에 쓸 연출 줄입니다. 이번 결과 계획에 있으면 방향까지 본 줄, 없으면 방향을 가리지 않는(Any) 줄입니다.
        /// 특성 처리기가 유닛 행동·추가 공격 표시를 읽을 때 피드백과 같은 줄을 쓰게 합니다(이펙트 끝 규칙 계획 Ee.2).
        /// </summary>
        internal CombatActivationRow RowOf(CombatEffectsEvent effects, int index)
        {
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            return _session.TryGetOccurrence(effects, index, out var occurrence) ? occurrence.Row : _rows.Of(effects.Traits[index]);
        }

        internal void AfterFacts(CombatEvent e)
        {
            if (e is not CombatTraitStateEvent state || !_session.TryConsumeState(state, out var bindings))
                return;
            int generation = _generation();
            foreach (var binding in bindings)
            {
                if (generation != _generation())
                    return;
                TraitActivationSnapshot trait = binding.Trait;
                CombatActivationRow row = binding.Occurrence.Row;
                var effects = new List<CombatEffectCue>();
                foreach (CombatEffectCue cue in row.Cues.Effects)
                    if (cue.Point == CombatCuePoint.SubjectBody || binding.Unit == trait.SourceUnitId)
                        effects.Add(cue);
                bool first = _session.TryBeginShake(binding.Occurrence);
                var cues = new CombatEventCues(effects, first ? row.Cues.Shakes : null, null, null);
                Pose? body = _facts.StandingBodyOf(binding.Unit);
                if (_cues.TryPlayAccent(cues, CombatCuePoints.ForActivation(trait.SourceUnitId, binding.Unit == trait.SourceUnitId ? body : null,
                    body.HasValue ? new[] { new CombatCuePoints.Body(binding.Unit, body.Value) } : Array.Empty<CombatCuePoints.Body>(), Array.Empty<Pose>())))
                    _preAttackStarts?.TraitStarted(trait, generation);
                var composition = CombatComposition.ForAccent();
                if (generation != _generation())
                    return;
                AddText(composition, row, trait, binding.Unit, binding.Occurrence.Cells);
                // 발동 유닛도 대상이면 자신의 상태 반영을 기다립니다. 대상이 아니면 첫 실제 상태 반영 뒤 한 번만 표시합니다.
                if (row.Text?.Target == CombatBodyTextTarget.All && !trait.TargetIds.Contains(trait.SourceUnitId) &&
                    _session.TryBeginSourceText(binding.Occurrence))
                    AddText(composition, row, trait, trait.SourceUnitId, binding.Occurrence.Cells);
                Play(composition);
            }
        }

        internal void AddText(CombatComposition composition, TraitActivationSnapshot trait, EntityId? only = null, int? cells = null) =>
            AddText(composition, _rows.Of(trait), trait, only, cells);

        /// <summary>특성 처리기가 유닛 행동 실행을 실제로 연 뒤 통지합니다. 실패·생략한 행동은 부르지 않습니다.</summary>
        internal void ActionStarted(TraitActivationSnapshot trait, int generation) =>
            _preAttackStarts?.TraitStarted(trait, generation);

        private void AddText(CombatComposition composition, CombatActivationRow row, TraitActivationSnapshot trait, EntityId? only, int? cells)
        {
            if (row.Text == null)
                return;
            if (_text == null)
                throw new InvalidOperationException("[CombatTraitFeedback] 텍스트 창구가 연결되지 않았습니다.");
            string label = row.Text.Format(row.Name, cells);
            Action started = _preAttackStarts?.CaptureTraitStart(trait, _generation());
            bool includesSource = row.Text.Target is CombatBodyTextTarget.Source or CombatBodyTextTarget.All;
            if (includesSource && (!only.HasValue || only == trait.SourceUnitId))
                composition.Add(new CombatBodyTextPiece(_text, trait.SourceUnitId, label, row.Text, started), CombatPieceStart.AtStart());
            if (row.Text.Target is CombatBodyTextTarget.Targets or CombatBodyTextTarget.All)
                foreach (EntityId target in trait.TargetIds)
                    if ((!includesSource || target != trait.SourceUnitId) && (!only.HasValue || only == target))
                        composition.Add(new CombatBodyTextPiece(_text, target, label, row.Text, started), CombatPieceStart.AtStart());
        }

        private void Play(CombatComposition composition)
        {
            if (!composition.IsEmpty && !_pieces.TryBegin(composition, null, _generation()))
                throw new InvalidOperationException("[CombatTraitFeedback] 텍스트 곁들임 실행을 시작하지 못했습니다.");
        }
    }
}
