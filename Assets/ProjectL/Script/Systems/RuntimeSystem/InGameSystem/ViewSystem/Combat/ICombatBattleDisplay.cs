using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// CombatViewSystem이 전투 표시에 거는 계약입니다(계획 D.5·E). 언제 그릴지는 ViewSystem이, 어떻게 그릴지는 구현이 정합니다.
    /// 입력 해석에 필요한 집기(<see cref="ICombatPointerPicker"/>)와 입력 중 표시 의도(<see cref="ICombatInteractionFeedback"/>)도
    /// 같은 씬 연결이 제공합니다.
    /// </summary>
    /// <remarks>
    /// 구현: 씬 Presenter를 묶는 <see cref="CombatSceneDisplay"/>, 테스트 가짜. 예외는 호출자로 전파합니다.
    /// 포기 버튼·확인창(<see cref="ICombatAbandonDisplay"/>)과 개발 Layer 패널(<see cref="ICombatDevLayerDisplay"/>)은
    /// 각 컨트롤러가 그 계약만 받도록 나눠 둡니다(SOLID·SSOT 개선 설계 D2·D3).
    /// </remarks>
    internal interface ICombatBattleDisplay : ICombatPointerPicker, ICombatInteractionFeedback, ICombatMovementPresenter, ICombatPointerMapping, ICombatResultPresenter, ICombatPresentationControl,
        ICombatAbandonDisplay, ICombatDevLayerDisplay
    {
        /// <summary>전투 설치: 정적 필드를 구성하고 동적 값을 처음 적용합니다.</summary>
        void BindReturn(Action requested);
        void ShowCompletion(BattleCompletionChanges completion, Action confirmed);

        void Install(in CombatDisplayValues values);

        /// <summary>결과 처리 뒤 동적 값(유닛·구조물·손패·HUD)만 반영합니다.</summary>
        void Refresh(in CombatDisplayValues values);

        /// <summary>전투가 정리돼 전투 표시를 비웁니다. 씬 연결은 유지합니다.</summary>
        void Clear();

        /// <summary>씬 연결 해제: 빌린 View를 모두 돌려주고 참조를 놓습니다. 여러 번 불려도 안전해야 합니다.</summary>
        void Release();
    }
}
