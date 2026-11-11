using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

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
        private readonly CombatWeaponAim? _weaponAim;
        private readonly float _playbackSpeed;
        private CombatClipPlayback _playback;
        // 중간 이벤트 순번(Timings.Events의 칸)마다 받은 신호 입구입니다. Begin 때 한 번 만듭니다.
        private Action[] _eventSignals;

        /// <param name="unit">클립 채널을 빌린 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        internal CombatClipPiece(EntityId unit, CombatClipData clip, CombatWeaponAim? weaponAim = null, float playbackSpeed = 1f)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("클립을 재생할 유닛이 비어 있습니다.", nameof(unit));
            _unit = unit;
            _clip = clip ?? throw new ArgumentNullException(nameof(clip));
            CombatPresentationValueRules.RequirePositive(playbackSpeed, nameof(playbackSpeed));
            _playbackSpeed = playbackSpeed;
            _weaponAim = weaponAim;
        }

        public bool IsEnded => _playback != null && _playback.IsComplete;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            // 있는 이벤트의 신호 입구만 받습니다(입구는 항목·신호마다 한 번이고, 받은 신호만 낼 신호로 적힘). 0초 이벤트는 재생 Begin 안에서 바로 나오므로
            // 그 전에 받습니다(작은 묶음 설계 SB2). 번호 → 신호는 표 하나(CombatClipEventSignals)라 번호를 더해도 이 조각은 고치지 않습니다.
            IReadOnlyList<CombatClipTimings.EventTiming> events = _clip.Timings.Events;
            _eventSignals = new Action[events.Count];
            for (int i = 0; i < events.Count; i++)
                _eventSignals[i] = context.SignalCallback(CombatClipEventSignals.SignalOf(events[i].Id));
            ICombatUnitClip lease = context.Clip(_unit);
            if (_weaponAim.HasValue && lease is ICombatWeaponPitchClip aim)
                aim.AimWeapon(_weaponAim.Value);
            _playback = new CombatClipPlayback(lease, _clip,
                context.SignalCallback(CombatPieceSignal.ClipStart), EventReached, null, _playbackSpeed);
            _playback.Begin();
        }

        public void Tick(float deltaTime) => _playback.Tick(deltaTime);

        public void Cancel() => _playback?.Cancel();

        private void EventReached(int index) => _eventSignals[index]();
    }
}
