using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 플래시 조각입니다(조각·조립 설계 BP9). 시작하자마자 회차 시간표(<see cref="CombatFlashPulses"/>)의 세기로 플래시 모습(몸 색·외곽선)을 쓰고,
    /// 전체 시간이 지나면 원래 모습으로 끝납니다. 피격 플래시는 한 회차(<see cref="CombatFlashPulses.Single"/>), 터지기 전 깜빡임은 여러 회차입니다(자폭 깜빡임 설계 SD3).
    /// 색은 ColorSystem 손잡이로만 바뀌고(표시의 외형 소유자, 섞는 규칙도 거기), 플래시 층 대여는 실행이 빌리고 되돌립니다(설계 Q-B6 A).
    /// </summary>
    internal sealed class CombatFlashPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly CombatFlashLook _look;
        private readonly CombatFlashPulses _pulses;
        private ICombatUnitFlash _flash;
        private float _elapsed;
        private bool _began;

        /// <param name="unit">플래시 채널을 빌린 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="look">세기 1일 때의 모습입니다.</param>
        /// <param name="pulses">회차 시간표입니다. 전체 시간이 0이면 시작 안에서 끝납니다(보이지 않음).</param>
        internal CombatFlashPiece(EntityId unit, CombatFlashLook look, CombatFlashPulses pulses)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("플래시할 유닛이 비어 있습니다.", nameof(unit));
            _unit = unit;
            _look = look;
            _pulses = pulses;
        }

        public bool IsEnded => _began && _elapsed >= _pulses.Duration;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _flash = context.Flash(_unit);
            _elapsed = 0f;
            _began = true;
            Apply();
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            Apply();
        }

        public void Cancel()
        {
        }

        // 지금 시각의 세기로 플래시 모습을 씁니다. 끝나면 세기 0(플래시 층 지움)입니다.
        private void Apply() => _flash.SetFlash(_look, _pulses.StrengthAt(_elapsed));
    }
}
