using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// <see cref="CombatViewRefresh"/>가 한 번 관측한 변경 신호입니다(설계 §5.3).
    /// 계약 위반이면 <see cref="Violation"/>만 채우고 다른 신호는 비웁니다.
    /// </summary>
    /// <remarks>
    /// 전투 설치·정리는 신호로 관측하지 않습니다. 첫 그리기는 Attach가 하고(개선 설계 D13),
    /// 전투가 사라지거나 바뀌면 조정자가 Detach합니다(D9).
    /// </remarks>
    internal readonly struct CombatRefreshSignal
    {
        private CombatRefreshSignal(CombatResult unpresentedResult, bool previewAnswered, string violation)
        {
            UnpresentedResult = unpresentedResult;
            PreviewAnswered = previewAnswered;
            Violation = violation;
        }

        /// <summary>아직 확인하지 않은 결과 하나(S == P + 1)입니다. 없으면 null입니다.</summary>
        internal CombatResult UnpresentedResult { get; }

        /// <summary>미리보기 응답 번호가 올랐습니다.</summary>
        internal bool PreviewAnswered { get; }

        /// <summary>결과 게시 계약 위반 내용입니다. 정상이면 null입니다.</summary>
        internal string Violation { get; }

        internal static CombatRefreshSignal Observed(CombatResult unpresentedResult, bool previewAnswered)
        {
            return new CombatRefreshSignal(unpresentedResult, previewAnswered, null);
        }

        internal static CombatRefreshSignal Violated(string violation)
        {
            return new CombatRefreshSignal(null, false, violation);
        }
    }
}
