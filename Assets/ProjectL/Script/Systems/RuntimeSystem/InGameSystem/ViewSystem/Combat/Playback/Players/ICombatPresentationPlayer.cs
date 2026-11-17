using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 재생기 하나의 수명 계약입니다(연출 조립 설계 B12). 사건 Presenter는 재생기를 실행 목록(<see cref="CombatPresentationPlayers"/>)으로만 다루고,
    /// 각 재생기는 자기 부품의 시작 조건·프레임 진행·완료·정리 규칙을 답합니다.
    /// </summary>
    /// <remarks>
    /// 지켜야 할 뜻(설계 §5.2 L):
    /// <list type="bullet">
    /// <item><see cref="BeginFrame"/>은 프레임 시작 사실만 기록하고 진행하지 않습니다. 실패는 자기 <see cref="Tick"/>에서 던집니다.
    /// 지금 여기서 던지는 재생기는 없습니다(옛 반응 재생기의 반응 불가 — 설계 B14 O13 — 는 피격 구조 정리 설계 HC1로 조각 실행의 사라짐이 되어 그 Tick에서 던집니다).</item>
    /// <item><see cref="Tick"/>의 false는 "이 프레임의 뒤 재생기를 진행하지 않음"만 뜻합니다. 실패는 예외입니다.</item>
    /// <item><see cref="FindBlock"/>·<see cref="IsComplete"/>·<see cref="BlocksFinalFacts"/>는 부수효과 없는 읽기입니다.</item>
    /// </list>
    /// </remarks>
    internal interface ICombatPresentationPlayer
    {
        /// <summary>결과 하나의 재생을 준비합니다.</summary>
        void BeginResult(CombatResult result);

        /// <summary>이 재생기가 시작을 막는 이유입니다. 막지 않으면 <see cref="CombatStartBlock.None"/>입니다.</summary>
        CombatStartBlock FindBlock(in CombatStartQuery query);

        /// <summary>프레임 시작 사실을 기록합니다. 모든 재생기의 <see cref="BeginFrame"/>이 첫 <see cref="Tick"/>보다 먼저 불립니다.</summary>
        void BeginFrame(in CombatPlaybackFrame frame);

        /// <summary>한 프레임 진행합니다. false면 실행 목록이 이 프레임의 남은 재생기를 진행하지 않습니다.</summary>
        bool Tick(in CombatPlaybackFrame frame);

        /// <summary>결과 완료 판단에서 이 재생기가 끝났는지입니다.</summary>
        bool IsComplete { get; }

        /// <summary>최종 사실(결과 끝 Refresh)을 그리면 안 되는 상태인지입니다.</summary>
        bool BlocksFinalFacts { get; }

        /// <summary>진행을 버리고 자원을 되돌립니다. 완료를 알리지 않습니다. 재생 세대는 호출자가 먼저 올립니다.</summary>
        void Cancel();

        /// <summary>최종 해제입니다.</summary>
        void Release();
    }
}
