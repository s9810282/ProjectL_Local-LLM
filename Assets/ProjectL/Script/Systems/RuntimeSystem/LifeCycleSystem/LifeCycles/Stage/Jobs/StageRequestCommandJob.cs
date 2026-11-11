using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>
    /// 씬과 독립적인 요청 소유자(회복·증강)의 요청 하나를 실행 순간에 예약하고 그 번호만 Core에 처리시키며,
    /// 예외 때도 소유자에게 종료를 통지하는 Job의 공통 몸통입니다(스테이지 선택 리팩토링 설계 §2.7 C-12).
    /// 예약을 Plan이 아니라 여기서 해, 예약과 실행이 떨어지지 않습니다(검사 후속 정리 계획 TP-R9).
    /// </summary>
    internal abstract class StageRequestCommandJob : IJobService
    {
        private bool _started;

        public int Priority => 3;
        public bool IsCompleted { get; private set; }

        /// <summary>통지가 실패했을 때 원래 실행 오류의 <see cref="Exception.Data"/>에 남기는 열쇠입니다.</summary>
        protected abstract string NotificationErrorKey { get; }

        public void Execute()
        {
            if (_started) return;
            _started = true;
            if (!TryReserveAndBegin(out int number))
            {
                IsCompleted = true;
                return;
            }
            bool processed = false;
            Exception error = null;
            try
            {
                processed = Process(number);
            }
            catch (Exception original)
            {
                error = original;
                throw;
            }
            finally
            {
                IsCompleted = true;
                try
                {
                    NotifyFinished(number, processed, error);
                }
                catch (Exception notificationError) when (error != null)
                {
                    error.Data[NotificationErrorKey] = notificationError;
                }
            }
        }

        /// <summary>소유자에게 예약을 받고 그 번호의 실행 시작을 알립니다. 둘 다 되면 참입니다.</summary>
        protected abstract bool TryReserveAndBegin(out int number);

        /// <summary>그 번호 하나를 Core에 처리시킵니다.</summary>
        protected abstract bool Process(int number);

        protected abstract void NotifyFinished(int number, bool processed, Exception error);
    }
}
