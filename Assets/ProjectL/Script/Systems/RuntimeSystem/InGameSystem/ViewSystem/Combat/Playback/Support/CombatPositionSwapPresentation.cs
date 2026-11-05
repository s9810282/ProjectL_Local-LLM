using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>위치 교환 전용 표시의 결과 대응과 대기 조립을 소유합니다. 배치 사실·실행 세대·실행 수명을 복제하지 않습니다.</summary>
    internal sealed class CombatPositionSwapPresentation
    {
        private readonly CombatActivationRows _rows;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatShownFacts _facts;
        private readonly CombatTraitFeedbackPlayback _feedback;
        private readonly Func<int> _generation;
        private readonly Dictionary<CombatCharacterEvent, CombatPlacementBatchPlan.PositionSwap> _steps = new();
        private readonly Dictionary<(CombatEffectsEvent Effects, int Index), CombatPlacementBatchPlan.PositionSwap> _activations = new();
        private CombatPositionSwapVisualData _visual;
        private CombatEffectActionGroup _activeEffects;

        internal CombatPositionSwapPresentation(CombatActivationRows rows, CombatPieceRunPlayer pieces,
            CombatShownFacts facts, CombatTraitFeedbackPlayback feedback, Func<int> generation)
        {
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        internal void Prepare(CombatPlacementBatchPlan placements)
        {
            if (placements == null) throw new ArgumentNullException(nameof(placements));
            Clear();
            _visual = _rows.Of(TraitKey.PositionSwap, TraitActivationKind.MovementChanged).PositionSwap;
            if (_visual == null)
                return;
            foreach (CombatPlacementBatchPlan.PositionSwap swap in placements.PositionSwaps())
            {
                _activations.Add((swap.Effects, swap.ActivationIndex), swap);
                foreach (CombatCharacterEvent step in swap.ActorSteps)
                    _steps.Add(step, swap);
                _steps.Add(swap.Pair.Partner, swap);
            }
        }

        private bool TryGetStep(CombatCharacterEvent step, out CombatPlacementBatchPlan.PositionSwap swap) => _steps.TryGetValue(step, out swap);

        private bool TryGetActivation(CombatEffectsEvent effects, int index, out CombatPlacementBatchPlan.PositionSwap swap) =>
            _activations.TryGetValue((effects, index), out swap);

        /// <summary>원래 걸음 사건은 완료하되 레인은 만들지 않습니다. 첫 걸음에서 두 배치를 함께 보류합니다.</summary>
        internal bool TrySuppressStep(CombatCharacterEvent step)
        {
            if (!TryGetStep(step, out var swap))
                return false;
            if (ReferenceEquals(step, swap.ActorSteps[0]))
                _facts.ClaimPositionSwap(swap.Pair);
            return true;
        }

        internal bool SuppressesStep(CombatCharacterEvent step) => _steps.ContainsKey(step);

        /// <summary>해당 발동의 피드백을 한 번 내고 대기합니다. 기존 상태 유지 이펙트는 이 묶음에 들어오지 않습니다.</summary>
        internal bool TryBegin(CombatEffectsEvent effects, int index, Action completed, int version)
        {
            if (!TryGetActivation(effects, index, out var swap))
                return false;
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (_activeEffects != null)
                throw new InvalidOperationException("[CombatPositionSwap] 이전 위치 교환이 아직 끝나지 않았습니다.");
            var group = new CombatEffectActionGroup(recallImmediately: true);
            Action close = group.Open();
            group.Seal();
            _activeEffects = group;
            try
            {
                _feedback.Start(effects, index, group);
                if (version != _generation())
                    return true;
                BeginDelay(() =>
                {
                    List<Exception> errors = null;
                    CleanupErrors.Try(() => _facts.ReleasePositionSwap(swap.Pair), ref errors);
                    if (ReferenceEquals(_activeEffects, group))
                        _activeEffects = null;
                    CleanupErrors.Try(close, ref errors);
                    CleanupErrors.ThrowIfAny(errors, "[CombatPositionSwap] 동시 배치·이펙트 종료에 실패했습니다.");
                    if (version == _generation())
                        completed();
                }, version);
            }
            catch (Exception error)
            {
                List<Exception> errors = null;
                CleanupErrors.Try(() => _facts.DiscardPositionSwap(swap.Pair), ref errors);
                if (ReferenceEquals(_activeEffects, group))
                    _activeEffects = null;
                CleanupErrors.Try(group.EndImmediately, ref errors);
                if (errors != null)
                    throw new AggregateException(error, new AggregateException(errors));
                throw;
            }
            return true;
        }

        /// <summary>기존 행동 실행으로 기다립니다. 완료 1회·취소·세대 검사는 기존 조각 실행기가 담당합니다.</summary>
        private void BeginDelay(Action completed, int version)
        {
            var composition = CombatComposition.ForAction(CombatPieceCompletion.General);
            composition.Add(new CombatPositionSwapDelayPiece(_visual), CombatPieceStart.AtStart(), waits: true);
            if (!_pieces.TryBegin(composition, completed, version))
                throw new InvalidOperationException("[CombatPositionSwap] 위치 교환 대기를 시작하지 못했습니다.");
        }

        /// <summary>결과 대응과 이 발동의 이펙트를 비웁니다. 진행 중인 조각 실행의 취소는 기존 실행 목록이 담당합니다.</summary>
        internal void Clear()
        {
            CombatEffectActionGroup group = _activeEffects;
            _activeEffects = null;
            _steps.Clear();
            _activations.Clear();
            _visual = null;
            group?.EndImmediately();
        }
    }
}
