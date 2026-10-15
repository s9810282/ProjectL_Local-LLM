using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 재생 수명 검증용 가짜 연출입니다. 즉시 완료·지연 통지·재진입·예외를 테스트가 정합니다.
    /// 자원 해제 계약(잔여 자원·해제 순서)은 검증하지 않습니다(View 계획 §8 X3).
    /// </summary>
    internal sealed class FakeCombatPresentation : ICombatPresentation
    {
        internal readonly List<CombatResult> Played = new();
        internal readonly List<Action<CombatPresentationOutcome>> Callbacks = new();

        /// <summary>true면 Play 안에서 바로 완료를 알립니다.</summary>
        internal bool CompleteImmediately;

        /// <summary>Play 안에서 통지 전에 실행합니다(오류 보고·Detach 재진입 검증).</summary>
        internal Action OnPlay;
        internal Action OnTick;

        /// <summary>true면 Play가 통지 뒤 예외를 던집니다.</summary>
        internal bool ThrowAfterNotify;
        internal bool ThrowOnDetach;

        internal int CancelCount { get; private set; }

        public void Detach() { if (ThrowOnDetach) throw new InvalidOperationException("연출 해제 실패"); }
        public void Attach(ICombatBattleDisplay display) { }
        public void Tick(float deltaTime) { OnTick?.Invoke(); }
        public void Play(CombatResult result, Action<CombatPresentationOutcome> onFinished)
        {
            Played.Add(result);
            Callbacks.Add(onFinished);
            OnPlay?.Invoke();

            if (CompleteImmediately)
                onFinished(CombatPresentationOutcome.Completed);
            if (ThrowAfterNotify)
                throw new InvalidOperationException("연출 시작 실패(테스트)");
        }

        public void Cancel()
        {
            CancelCount++;
        }

        /// <summary>마지막으로 시작한 재생에 통지를 보냅니다.</summary>
        internal void Finish(CombatPresentationOutcome outcome)
        {
            Callbacks[Callbacks.Count - 1](outcome);
        }
    }
}
