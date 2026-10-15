using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Framework.Utility;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner
{
    /// <summary>Plan이 Job을 등록하고 Flow가 실행 상태를 확인하는 공용 Runner입니다.</summary>
    #region 서비스

    public interface IJobRunner : IServiceModule
    {
        bool IsIdle { get; }

        void Register(IJobService job);
        void RegisterRange(IReadOnlyList<IJobService> jobs);
        void Clear();
    }

    /// <summary>
    /// IJobService를 우선순위 순서로 실행합니다.
    /// 완료되지 않은 Job은 다음 Tick에도 현재 Job으로 유지됩니다.
    /// </summary>
    #endregion

    #region 시스템 모듈

    internal sealed class JobRunnerSystem :
        IJobRunner,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly JobQueue<IJobService> _queue = new(
            JobPriorityComparer.Instance,
            capacity: 16);

        private IJobService _currentJob;

        public bool IsIdle => _currentJob == null && _queue.IsEmpty;

        public void Register(IJobService job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            _queue.Enqueue(job);
        }

        public void RegisterRange(IReadOnlyList<IJobService> jobs)
        {
            if (jobs == null)
                throw new ArgumentNullException(nameof(jobs));

            // 잘못된 목록이 큐에 일부만 들어가지 않도록 먼저 검증합니다.
            for (int i = 0; i < jobs.Count; i++)
                if (jobs[i] == null)
                    throw new ArgumentException("Job list contains null.", nameof(jobs));

            for (int i = 0; i < jobs.Count; i++)
                _queue.Enqueue(jobs[i]);
        }

        public void Clear()
        {
            _queue.Clear();
            _currentJob = null;
        }

        public void Tick(float deltaTime)
        {
            const int immediateJobLimit = 32;

            for (int i = 0; i < immediateJobLimit; i++)
            {
                if (_currentJob == null && !_queue.TryDequeue(out _currentJob))
                    return;

                _currentJob.Execute();

                if (!_currentJob.IsCompleted)
                    return;

                _currentJob = null;
            }

            // 한 Tick의 실행 예산을 소진하면 남은 Job은 다음 Tick에 실행합니다.
        }

        public void Shutdown()
        {
            Clear();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<IJobRunner>(this);
            sink.AddModule<ISystemModule>(this);
        }

        private sealed class JobPriorityComparer : IComparer<IJobService>
        {
            internal static readonly JobPriorityComparer Instance = new();

            public int Compare(IJobService left, IJobService right)
            {
                return left.Priority.CompareTo(right.Priority);
            }
        }
    }

    #endregion
}
