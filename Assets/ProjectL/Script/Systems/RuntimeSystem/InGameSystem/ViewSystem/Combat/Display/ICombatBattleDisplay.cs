using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// CombatViewSystem이 전투 표시에 거는 계약입니다(계획 D.5·E). 언제 그릴지는 ViewSystem이, 어떻게 그릴지는 구현이 정합니다.
    /// 입력 해석에 필요한 집기(<see cref="ICombatPointerPicker"/>)와 입력 중 표시 의도(<see cref="ICombatInteractionFeedback"/>)도
    /// 같은 씬 연결이 제공합니다.
    /// </summary>
    /// <remarks>
    /// 구현: 씬 Presenter를 묶는 <see cref="CombatSceneDisplay"/>, 테스트 가짜. 예외는 호출자로 전파합니다.
    /// 행동 버튼(<see cref="ICombatActionDisplay"/>)과 UI 가림(<see cref="ICombatPointerOcclusion"/>)은
    /// 쓰는 쪽이 그 계약만 받도록 나눠 둡니다(행동 버튼 설계 B1·B5).
    /// 순차 연출이 쓰는 재생 계약은 상속하지 않고 <see cref="EventPlayback"/>으로 넘깁니다(연출 조립 설계 §9 I-4, 결정 Q-R16 A).
    /// 형식 검사로 꺼내 쓰는 계약은 없습니다(구조 리팩토링 설계 D6).
    /// </remarks>
    internal interface ICombatBattleDisplay : ICombatPointerPicker, ICombatInteractionFeedback, ICombatMovementFeedback, ICombatPointerMapping,
        ICombatCursorDevice, ICombatActionDisplay, ICombatPointerOcclusion, ICombatItemDragFeedback,
        ICombatUnitRangeFeedback, ICombatAttackTrajectoryFeedback, ICombatKnockbackPreviewFeedback, ICombatAttackModeTargets
    {
        /// <summary>
        /// 순차 연출이 쓰는 재생 계약입니다. 이 씬 연결이 소유하고, 해제 뒤 사건 시작은 재생 계약이 예외로 거절합니다(리팩토링 설계 D9).
        /// </summary>
        ICombatEventPlayback EventPlayback { get; }

        /// <summary>
        /// 플레이어 카메라 조작(이동·줌) 시점입니다(카메라 조작 설계 CC1). 이 씬 연결이 소유하고 해제 때 카메라를 처음으로 되돌립니다.
        /// 맵 카메라가 없으면 조작이 없는 시점입니다(null이 아님).
        /// </summary>
        ICombatCameraControl CameraControl { get; }

        /// <summary>
        /// 결과 화면을 엽니다. 확인을 누르면 <paramref name="confirm"/>을 부르고, 참이면 완료 뒤 배경음으로 바꿉니다(View 2라운드 W6-5).
        /// 확인을 받아도 되는지(같은 연결·같은 완료·한 번만)는 <paramref name="confirm"/>이 판단합니다.
        /// </summary>
        void ShowCompletion(BattleCompletionChanges completion, Func<bool> confirm);

        /// <summary>전투 설치: 정적 필드를 구성하고 동적 값을 처음 적용합니다.</summary>
        void Install(in CombatDisplayValues values);

        /// <summary>결과 처리 뒤 동적 값(유닛·구조물·HUD)만 반영합니다.</summary>
        void Refresh(in CombatDisplayValues values);

        /// <summary>전투가 정리돼 전투 표시를 비웁니다. 씬 연결은 유지합니다.</summary>
        void Clear();

        /// <summary>씬 연결 해제: 빌린 View를 모두 돌려주고 참조를 놓습니다. 여러 번 불려도 안전해야 합니다.</summary>
        void Release();

        /// <summary>
        /// 유닛 HUD 아이템 아이콘 우클릭을 보낼 해제 입구를 겁니다(가방 설계 P6). 입력이 표시보다 늦게 만들어지므로 조정자가 입력을 만든 뒤 걸고,
        /// 입력을 정리할 때 null로 풉니다. 해제 뒤에는 무시합니다.
        /// </summary>
        void BindItemUnequip(Action<EntityId> unequip);
    }
}
