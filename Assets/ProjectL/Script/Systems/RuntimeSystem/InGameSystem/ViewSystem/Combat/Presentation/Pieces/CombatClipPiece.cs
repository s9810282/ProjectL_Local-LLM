using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 사건 클립 조각입니다(클립 설계 AC3·AC6, 계획 T17). 실행이 빌린 클립 대여로 클립 하나를 재생하고(<see cref="CombatClipPlayback"/>),
    /// 시작과 존재하는 중간 이벤트마다 해당 번호의 신호를 냅니다. 선택 번호가 없으면 콜백도 만들지 않습니다.
    /// 끝 이벤트에서 끝나고, 끝 신호는 실행이 냅니다.
    /// </summary>
    /// <remarks>
    /// 클립 대여는 실행이 실행 끝에 되돌립니다(조각·조립 설계 BP4). 그래서 같은 실행의 다른 항목이 클립 끝보다 오래 가면,
    /// 그동안 모델은 클립 끝 프레임 자세로 남았다가 실행이 끝날 때 바닥(Idle)으로 돌아갑니다.
    /// </remarks>
    internal sealed class CombatClipPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly CombatClipData _clip;
        private CombatClipPlayback _playback;

        /// <param name="unit">클립 채널을 빌린 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        internal CombatClipPiece(EntityId unit, CombatClipData clip)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("클립을 재생할 유닛이 비어 있습니다.", nameof(unit));
            _unit = unit;
            _clip = clip ?? throw new ArgumentNullException(nameof(clip));
        }

        public bool IsEnded => _playback != null && _playback.IsComplete;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            Action event2 = _clip.Timings.TryGetEventTime(CombatClipEventId.Event2, out _)
                ? context.SignalCallback(CombatPieceSignal.ClipEvent2) : null;
            Action event3 = _clip.Timings.TryGetEventTime(CombatClipEventId.Event3, out _)
                ? context.SignalCallback(CombatPieceSignal.ClipEvent3) : null;
            _playback = new CombatClipPlayback(context.Clip(_unit), _clip,
                context.SignalCallback(CombatPieceSignal.ClipStart), context.SignalCallback(CombatPieceSignal.ClipEvent), null, event2, event3);
            _playback.Begin();
        }

        public void Tick(float deltaTime) => _playback.Tick(deltaTime);

        public void Cancel() => _playback?.Cancel();
    }
}
