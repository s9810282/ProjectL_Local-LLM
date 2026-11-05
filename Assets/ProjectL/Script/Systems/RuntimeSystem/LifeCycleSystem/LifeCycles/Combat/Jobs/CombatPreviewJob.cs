using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 현재 미리보기 요청 하나를 Core 처리 서비스에 넘깁니다.
    /// 요청은 서비스가 실행 시점에 읽으며, 미리보기는 게임 상태를 바꾸지 않습니다.
    /// </summary>
    internal struct CombatPreviewJob : IJobService
    {
        private readonly ICombatPreviewService _preview;
        private readonly ICombatProgressReader _progress;
        private readonly int _requestNumber;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 미리보기 요청을 처리하고 같은 실행 안에서 응답 번호가 요청 번호 이상인지 확인합니다(재구성 설계 J3).
        /// 판단한 Job(<see cref="CombatNextJob"/>) 안에서 바로 이어 실행되므로 그 사이 View가 요청을 바꾸지 않습니다(개선 설계 §5.2).
        /// </summary>
        internal CombatPreviewJob(ICombatPreviewService preview, int priority, ICombatProgressReader progress,
            int requestNumber)
        {
            _preview = preview ?? throw new ArgumentNullException(nameof(preview));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (requestNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(requestNumber));
            _requestNumber = requestNumber;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            // false도 정상 반환이다. 응답했는지는 반환값이 아니라 응답 번호로 본다.
            _preview.ProcessLatestPreview();
            int answered = _progress.Read().AnsweredPreviewNumber;
            if (answered < _requestNumber)
                throw new InvalidOperationException(
                    $"[CombatPreviewJob] 미리보기 요청 {_requestNumber}에 대한 응답이 기록되지 않았습니다(응답 번호 {answered}).");
            IsCompleted = true;
        }
    }
}
