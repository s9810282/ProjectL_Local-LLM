using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 실행 소유자가 튜토리얼 표시를 맡기는 좁은 계약입니다(튜토리얼 설계 §10). 표시는 끝났다는 사실만 알리고 단계를 바꾸지 않습니다.
    /// 구현은 Combat 씬의 튜토리얼 화면을 쓰는 <see cref="TutorialViewUIDisplay"/>입니다.
    /// </summary>
    internal interface ITutorialViewDisplay
    {
        /// <summary>Combat 화면에 연결합니다. 씬의 튜토리얼 화면을 찾고 창 활성 알림을 구독합니다. 없으면 예외입니다.</summary>
        void Attach();

        /// <summary>화면 연결을 끊고 창 활성 알림을 해제합니다. 열린 표시는 먼저 <see cref="Close"/>로 닫습니다. 연결이 없으면 아무것도 하지 않습니다.</summary>
        void Detach();

        /// <summary>표시를 시작하고, 끝나면 <paramref name="completed"/>를 한 번 부릅니다. 진행 중인 앞 표시는 이 표시가 대신합니다.</summary>
        void Present(TutorialViewDisplayRequest request, Action completed);

        /// <summary>현재 화면 좌표만 갱신합니다. 표시 완료·진행·타이머를 다시 시작하지 않습니다.</summary>
        void RefreshLayout();

        /// <summary>입력 제한 때문에 막힌 조작이 있었다는 안내를 잠시 띄웁니다. 완료를 알리지 않습니다.</summary>
        void ShowBlockedInputNotice(TutorialDefinitionData definition, int stepKey);

        /// <summary>모든 표시와 화면 막을 닫습니다. 열린 것이 없거나 연결이 없으면 아무것도 하지 않습니다. 실패는 예외입니다.</summary>
        void Close();
    }

    /// <summary>표시 하나의 내용입니다. 문구·보일 대상·덮개는 정의의 그 단계에서 읽습니다(설계 §15.11).</summary>
    internal readonly struct TutorialViewDisplayRequest
    {
        internal TutorialViewDisplayRequest(TutorialDefinitionData definition, int stepKey, TutorialViewPresentationKind kind)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (stepKey <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepKey), stepKey, "0보다 커야 합니다.");
            if (kind == TutorialViewPresentationKind.None)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "표시 종류가 필요합니다.");

            StepKey = stepKey;
            Kind = kind;
        }

        internal TutorialDefinitionData Definition { get; }
        internal int StepKey { get; }
        internal TutorialViewPresentationKind Kind { get; }
    }
}
