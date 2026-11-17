using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 사건 클립 조각입니다(클립 설계 AC3·AC6, 계획 T17, 클립 칸 다시 짜기 설계 CS6). 실행이 빌린 클립 대여로 동작 하나를 재생합니다:
    /// 앞 단계(있으면) → 본 단계(<see cref="CombatClipPlayback"/>) → 본 단계 끝에 뒤 단계(있으면)를 채널 꼬리로 넘김.
    /// 신호는 본 단계의 이름표만 냅니다: 시작(<see cref="CombatPieceSignal.ClipStart"/>)과 존재하는 중간 이벤트마다 해당 번호의 신호(설계 P7). 선택 번호가 없으면 콜백도 만들지 않습니다.
    /// 본 단계 끝 이벤트에서 끝나고, 끝 신호는 실행이 냅니다.
    /// </summary>
    /// <remarks>
    /// <para>클립 대여는 실행이 실행 끝에 되돌립니다(조각·조립 설계 BP4). 그래서 같은 실행의 다른 항목이 클립 끝보다 오래 가면,
    /// 그동안 모델은 본 단계 끝 프레임 자세로 남았다가 실행이 끝날 때 바닥(Idle)으로 돌아갑니다. 뒤 단계나 Out 블렌드가 있으면 본 단계 끝에 채널로 넘겨,
    /// 실행이 끝나기를 기다리지 않고 뒤 단계 → Idle로 이어집니다(실행이 끝나도 이어짐).</para>
    /// <para>
    /// 블렌드(설계 CS7): 첫 단계는 In, 앞 → 본 단계는 Between으로 같은 대여에서 섞어 갈아 끼우고, 섞기 시간은 이 조각의 Tick 시간(실제 초, 재생 배율 없음)이 넣습니다.
    /// 클립 시각·이벤트는 섞는 동안에도 0초부터 흐르므로 사건 완료 시점은 그대로입니다. 앞 단계 끝을 넘긴 시간은 본 단계와 새 섞기가 바로 이어 받습니다.
    /// 본 단계 끝에 섞기가 남아 있으면 그 섞기도 채널에 넘겨 끝까지 진행합니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatClipPiece : ICombatPresentationPiece
    {
        private readonly EntityId _unit;
        private readonly CombatClipMotion _motion;
        private readonly CombatWeaponAim? _weaponAim;
        private ICombatUnitClip _lease;
        private CombatClipPlayback _pre;
        private CombatClipPlayback _main;
        private Action _startSignal;
        private bool _cancelled;
        private bool _tailHanded;
        // 본 단계 중간 이벤트 순번(Timings.Events의 칸)마다 받은 신호 입구입니다. Begin 때 한 번 만듭니다.
        private Action[] _eventSignals;

        /// <param name="unit">클립 채널을 빌린 유닛입니다. 조립이 그 채널을 적어야 합니다.</param>
        /// <param name="motion">재생할 동작입니다. 재생 배율은 동작 칸의 <see cref="CombatClipMotion.AnimSpeed"/>입니다.</param>
        internal CombatClipPiece(EntityId unit, CombatClipMotion motion, CombatWeaponAim? weaponAim = null)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("클립을 재생할 유닛이 비어 있습니다.", nameof(unit));
            _unit = unit;
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            _weaponAim = weaponAim;
        }

        public bool IsEnded => _main != null && _main.IsComplete;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            // 있는 이벤트의 신호 입구만 받습니다(입구는 항목·신호마다 한 번이고, 받은 신호만 낼 신호로 적힘). 0초 이벤트는 재생 Begin 안에서 바로 나오므로
            // 그 전에 받습니다(작은 묶음 설계 SB2). 번호 → 신호는 표 하나(CombatClipEventSignals)라 번호를 더해도 이 조각은 고치지 않습니다.
            IReadOnlyList<CombatClipTimings.EventTiming> events = _motion.Main.Timings.Events;
            _eventSignals = new Action[events.Count];
            for (int i = 0; i < events.Count; i++)
                _eventSignals[i] = context.SignalCallback(CombatClipEventSignals.SignalOf(events[i].Id));
            _startSignal = context.SignalCallback(CombatPieceSignal.ClipStart);
            _lease = context.Clip(_unit);
            if (_motion.Pre == null)
            {
                BeginMain(0f);
                return;
            }
            // 앞 단계는 신호를 내지 않습니다. 큐 시점은 본 단계 이름표입니다(설계 P7).
            _pre = new CombatClipPlayback(_lease, _motion.Pre, null, null, null, _motion.AnimSpeed, _motion.BlendIn);
            _pre.Begin();
            if (_pre.IsComplete)
                BeginMain(_pre.Overflow);
        }

        public void Tick(float deltaTime)
        {
            if (_cancelled) return;
            // 섞기 시간은 클립 시각과 같은 시계(이 Tick)로 넣습니다(설계 CS7 시간). 재생 배율은 걸지 않습니다(설계 P12).
            _lease.AdvanceBlend(deltaTime);
            if (_main == null)
            {
                _pre.Tick(deltaTime);
                if (!_cancelled && _pre.IsComplete)
                    BeginMain(_pre.Overflow);
                return;
            }
            _main.Tick(deltaTime);
            HandTailIfEnded();
        }

        public void Cancel()
        {
            _cancelled = true;
            _pre?.Cancel();
            _main?.Cancel();
        }

        private void BeginMain(float carried)
        {
            // 첫 단계면 Idle에서 In, 앞 단계 뒤면 Between으로 섞습니다(설계 CS7).
            _main = new CombatClipPlayback(_lease, _motion.Main, _startSignal, EventReached, null, _motion.AnimSpeed,
                _motion.Pre == null ? _motion.BlendIn : _motion.BlendBetween);
            _main.Begin();
            if (_cancelled) return;
            // 조준은 본 단계를 꽂은 뒤에 겁니다. 앞 단계에서 갈아 끼울 때 채널이 조준을 풀기 때문입니다(조준 곡선은 본 단계 시각 기준).
            if (_weaponAim.HasValue && _lease is ICombatWeaponPitchClip aim)
                aim.AimWeapon(_weaponAim.Value);
            if (carried > 0f)
            {
                _lease.AdvanceBlend(carried);
                _main.Tick(carried);
            }
            HandTailIfEnded();
        }

        // 본 단계가 끝난 순간 나머지(뒤 단계·Out 블렌드·남은 섞기)를 채널에 넘깁니다(설계 CS6·CS7, 결정 Q-A7 A). 사건 완료는 본 단계 끝 그대로입니다.
        // 넘길 것이 없으면 지금처럼 본 단계 끝 자세로 남았다가 대여 반환 때 바로 돌아갑니다.
        private void HandTailIfEnded()
        {
            if (_cancelled || _tailHanded || !_main.IsComplete || !_lease.IsAvailable)
                return;
            if (_motion.Post == null && !_motion.BlendOut.Enabled && !_lease.IsBlending)
                return;
            _tailHanded = true;
            _lease.PlayTail(_motion.Post, _motion.AnimSpeed, _motion.BlendBetween, _motion.BlendOut);
        }

        private void EventReached(int index) => _eventSignals[index]();
    }
}
