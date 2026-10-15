using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 결과 하나를 연출합니다(설계 §9·§9.2, W18). 자기 임시 자원을 정리한 뒤 Unity 메인 스레드에서 완료 또는 실패를 한 번 알립니다.
    /// </summary>
    /// <remarks>
    /// Core 상태를 화면에 적용하거나 Buffer를 확인하지 않습니다(표시 반영과 확인은 CombatViewSystem).
    /// Play·Cancel의 동기 예외는 호출자에게 전파합니다. 구현: 순차 사건 어댑터(<see cref="OrderedCombatPresentation"/>), 테스트 가짜.
    /// </remarks>
    internal interface ICombatPresentation
    {
        /// <summary>씬 표시에 연결합니다. 진행 중 연출이 있으면 먼저 취소합니다.</summary>
        void Attach(ICombatBattleDisplay display);

        /// <summary>진행 중 연출을 한 프레임만큼 진행합니다.</summary>
        void Tick(float deltaTime);

        /// <summary>진행 중 연출을 취소하고 표시 참조를 놓습니다.</summary>
        void Detach();

        /// <summary>결과 하나를 재생합니다. 한 번에 하나만 재생합니다.</summary>
        void Play(CombatResult result, Action<CombatPresentationOutcome> onFinished);

        /// <summary>진행 중 연출과 자기 임시 자원을 정리합니다. 멱등적이며 완료로 보고하지 않습니다.</summary>
        void Cancel();
    }
}
