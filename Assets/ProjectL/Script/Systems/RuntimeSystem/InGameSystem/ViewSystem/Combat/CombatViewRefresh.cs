using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// Buffer에는 변경 알림이 없으므로 결과 순번과 미리보기 응답 번호를 비교해 변경 신호를 만듭니다(설계 §5.3, W4·W9).
    /// 그리지 않으며, 결과 처리 완료의 기준은 Buffer의 PresentedResultSequence(P)를 그대로 씁니다.
    /// </summary>
    /// <remarks>
    /// 보관하는 것은 확인한 미리보기 응답 번호뿐입니다. 같은 의미의 결과 완료 카운터를 만들지 않습니다.
    /// 전투 설치·정리는 관측하지 않습니다: 첫 그리기는 Attach가 하고(개선 설계 D13), 전투가 사라지면 조정자가 Detach합니다(D9).
    /// </remarks>
    internal sealed class CombatViewRefresh
    {
        private int _observedPreviewNumber;

        /// <summary>새 씬 연결 때 확인한 미리보기 응답 번호를 지웁니다.</summary>
        internal void Reset()
        {
            _observedPreviewNumber = 0;
        }

        /// <summary>
        /// 현재 값을 한 번 비교합니다. S = LatestResultSequence, P = PresentedResultSequence입니다.
        /// 정상 흐름에서 미확인 결과는 최대 하나이며, 유실·순번 불일치·건너뜀·역행은 위반으로 돌려줍니다.
        /// 위반이면 확인한 응답 번호를 바꾸지 않습니다.
        /// </summary>
        internal CombatRefreshSignal Observe(
            int latestSequence,
            int presentedSequence,
            CombatResult latestResult,
            int answeredPreviewNumber)
        {
            string violation = FindViolation(latestSequence, presentedSequence, latestResult);
            if (violation != null)
                return CombatRefreshSignal.Violated(violation);

            bool previewAnswered = answeredPreviewNumber > _observedPreviewNumber;
            if (previewAnswered)
                _observedPreviewNumber = answeredPreviewNumber;

            CombatResult unpresented = latestSequence == presentedSequence + 1 ? latestResult : null;
            return CombatRefreshSignal.Observed(unpresented, previewAnswered);
        }

        private static string FindViolation(int latestSequence, int presentedSequence, CombatResult latestResult)
        {
            if (latestResult != null && latestResult.Sequence != latestSequence)
                return $"최신 결과의 순번이 발행 순번과 다릅니다(결과 {latestResult.Sequence}, 발행 {latestSequence}).";

            if (latestSequence < presentedSequence)
                return $"발행 순번이 확인 순번보다 작습니다(발행 {latestSequence}, 확인 {presentedSequence}).";

            if (latestSequence - presentedSequence > 1)
                return $"확인하지 않은 결과를 건너뛰었습니다(발행 {latestSequence}, 확인 {presentedSequence}).";

            if (latestSequence > presentedSequence && latestResult == null)
                return $"확인하지 않은 결과 {latestSequence}가 없습니다(확인 {presentedSequence}).";

            // S == P(결과 없음 또는 이미 확인한 결과), S == P + 1(새 결과 하나)은 정상이다.
            return null;
        }
    }
}
