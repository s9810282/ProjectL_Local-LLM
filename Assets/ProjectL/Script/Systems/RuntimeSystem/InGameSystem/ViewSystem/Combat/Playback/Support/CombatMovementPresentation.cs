using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 이동 표시입니다(Presenter 분리 설계 S6). 걸음을 유닛별 레인에 넘기고, 경로 끝 방향 복귀와 복귀 표시를 합니다.
    /// 시작 조건(Mapper 연결·남은 연출 대기)은 사건 Presenter가 확인한 뒤 부릅니다. 걸음 진행과 걸음 Gate는 레인(<see cref="CombatUnitMotionSet"/>)이 맡습니다.
    /// </summary>
    internal sealed class CombatMovementPresentation
    {
        private readonly CombatUnitMotionSet _motion;
        private readonly CombatEnemyPresentationPacing _pacing;
        private readonly CombatPlaybackHighlight _highlight;
        private readonly ICombatWaitSlot _waitSlot;
        private readonly CombatShownFacts _facts;
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatPositionSwapPresentation _positionSwap;

        /// <param name="waitSlot">걸음 전 간격을 거는 대기 칸입니다(연출 조립 설계 B12 보완 1). 레인이 진행 중이면 예외를 던지는 전제 확인은 대기 칸이 갖습니다.</param>
        /// <param name="presentation">유닛 표시 창구입니다. 표시 회전만 씁니다(연출 조립 설계 B13).</param>
        internal CombatMovementPresentation(CombatUnitMotionSet motion, CombatEnemyPresentationPacing pacing,
            CombatPlaybackHighlight highlight, ICombatWaitSlot waitSlot, CombatShownFacts facts,
            CombatUnitPresentationWindow presentation, Func<FieldGridMapper> mapper, CombatPositionSwapPresentation positionSwap = null)
        {
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            _pacing = pacing ?? throw new ArgumentNullException(nameof(pacing));
            _highlight = highlight ?? throw new ArgumentNullException(nameof(highlight));
            _waitSlot = waitSlot ?? throw new ArgumentNullException(nameof(waitSlot));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _positionSwap = positionSwap;
        }

        // 이 결과의 위치 교환 쌍 계획을 따라가는 세션입니다. 결과 준비 때 만들고 취소 때 비웁니다.
        private CombatPlacementBatchPlan.Session _placements;

        internal void Prepare(CombatResult result) => PreparePlacements(CombatPlacementBatchPlan.Build(result));

        /// <summary>결과 조립 지점에서 만든 같은 쌍 계획을 사용합니다.</summary>
        internal void PreparePlacements(CombatPlacementBatchPlan placements) =>
            _placements = (placements ?? throw new ArgumentNullException(nameof(placements))).Begin();
        internal void CancelPlacements()
        {
            _placements = null;
            _facts.ForgetHeldPlacements();
        }

        /// <summary>걸음 하나를 그 유닛의 레인에서 시작합니다. <paramref name="completed"/>는 걸음 사건 완료 통지입니다(동시 처리 설계 C6).</summary>
        /// <param name="leadHold">걸음 전에 더 기다릴 초입니다(공격 동작 앞당김, 클립 칸 다시 짜기 11-17 사용자 결정 A). 적 행동 간격에 더해집니다.</param>
        internal void BeginStep(CombatCharacterEvent step, Action completed, float leadHold = 0f)
        {
            if (_positionSwap?.TrySuppressStep(step) == true)
            {
                _highlight.Clear();
                completed();
                return;
            }
            if (_placements?.IsPartner(step) == true)
            {
                // 승객의 끝 사실은 쌍 시작에서 쥐었습니다. 발동 순간 위치 이동 행동이 가져가거나 사건 Presenter가 놓습니다(이펙트 끝 규칙 계획 TP-E7).
                // 걷기 레인을 만들지 않고 걸음 사건은 바로 끝냅니다.
                completed();
                return;
            }
            // 위치 교환: 이동자가 끝점까지 걷는 동안 승객은 옛 자리에 둡니다(차례로, 계획 TP-E8). 이동자 도착 때 함께 반영하지 않습니다.
            if (_placements != null && _placements.TryBegin(step, out var pair))
                _facts.HoldPlacement(pair.Partner.Final);
            CombatMovementKind kind = KindOf(step);
            // 스스로 하는 이동은 새 행동이라 앞 행동의 재생 강조를 지운다. 넉백 같은 밀림은 앞 포효의 결과라 유지한다.
            if (CombatMovementKinds.IsSelfDriven(kind))
                _highlight.Clear();
            // 걸음 전 간격은 자발적 경로의 첫 걸음(행동 시작)과 공격 동작 앞당김의 첫 선행 걸음에만 생깁니다. 그동안 다른 레인은 진행하지 않습니다(함께 돌 레인이 없을 때만 붙잡음).
            float gap = _pacing.BeforeStep(step) + Math.Max(0f, leadHold);
            if (gap > 0f) _waitSlot.StartGap(gap);
            _motion.BeginStep(step, kind, gap == 0f, _mapper(), completed);
        }

        // 걸음의 이동 종류는 여기서 묻습니다(걸음 모양 설계 ST1, 계획 P11). 판정 원본은 Core 이동 원인을 읽는 CombatMovementKinds.Of 하나이고
        // 적 행동 간격도 같은 판정을 부릅니다(특성 시스템 View 계획 R4·TP-7). 종류의 성질도 CombatMovementKinds가 정합니다(계획 §6 M1).
        private static CombatMovementKind KindOf(CombatCharacterEvent step) => CombatMovementKinds.Of(step);

        /// <summary>
        /// 경로 끝 방향 복귀입니다. 필드에 살아 있는 유닛만 복귀합니다. 스스로 하는 이동은 원래 방향으로 돌고,
        /// 밀린 경로는 젖힌 자세(표시 회전, 넉백 피격 연출 — 임시)가 남았을 때만 젖히는 시간 동안 폅니다(레인의 <c>BeginStraighten</c>).
        /// 접촉으로 사건 완료가 일찍 온 마지막 걸음은 아직 보간 중입니다(<paramref name="interpolating"/>). 방향 복귀를 보간 끝 뒤로 미루고 바로 완료합니다(이동 접촉 설계 M3).
        /// 이동자 사실은 보간 끝에 그리므로 그때는 걸음의 끝 사실로 판단합니다. 접촉 묶음이 없는 걸음은 보간 끝 뒤에 여기 옵니다.
        /// </summary>
        internal void EndMovement(CombatCharacterEvent lastStep, bool interpolating, Action completed)
        {
            if (_positionSwap?.SuppressesStep(lastStep) == true)
            {
                completed();
                return;
            }
            var facts = interpolating ? lastStep.Final : _facts.FindCharacter(lastStep.Final.EntityId);
            bool selfDriven = CombatMovementKinds.IsSelfDriven(KindOf(lastStep));
            if (facts == null || facts.Health.IsDead ||
                facts.Area != CharacterRuntimeArea.Field || !_presentation.TryGetRotation(facts.EntityId, out _) ||
                !selfDriven && !_presentation.TryGetFacing(facts.EntityId, out _))
            {
                completed();
                return;
            }
            if (interpolating)
            {
                _motion.DeferFacingRestore(lastStep);
                completed();
                return;
            }
            if (selfDriven)
                _motion.BeginFacingRestore(facts, completed, _mapper());
            else
                _motion.BeginStraighten(facts, completed, _mapper());
        }

        /// <summary>복귀한 유닛의 표시 회전을 지우고 복귀 사실을 반영해 다시 그립니다.</summary>
        internal void ShowReturn(CombatCharacterEvent returned)
        {
            if (_placements?.IsPartner(returned) == true) return;
            if (_placements != null && _placements.TryBegin(returned, out var pair))
            {
                _presentation.ClearFacing(pair.Partner.Final, _mapper());
                _facts.ApplyCharacter(pair.Partner.Final);
            }
            _presentation.ClearFacing(returned.Final, _mapper());
            _facts.ApplyCharacter(returned.Final);
            _facts.Render();
        }
    }
}
