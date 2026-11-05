using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 처리하지 않은 전투 명령 하나를 Core 처리 서비스에 넘깁니다.
    /// 어떤 명령을 처리할지는 서비스가 처리 번호 기준으로 고르며, Job은 명령 내용을 들고 있지 않습니다.
    /// </summary>
    internal struct CombatCommandJob : IJobService
    {
        private readonly ICombatCommandService _commands;
        private readonly ICombatProgressReader _progress;
        private readonly int _expectedNumber;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        /// <summary>명령 N을 처리하고 같은 실행 안에서 처리 번호가 N 이상이 됐는지 확인합니다(재구성 설계 J3, K4).</summary>
        internal CombatCommandJob(ICombatCommandService commands, int priority, ICombatProgressReader progress,
            int expectedNumber)
        {
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (expectedNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(expectedNumber));
            _expectedNumber = expectedNumber;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            // false도 정상 반환이다(K14). 처리했는지는 반환값이 아니라 처리 번호로 본다.
            _commands.ProcessNextCommand();
            int handled = _progress.Read().HandledCommandNumber;
            if (handled < _expectedNumber)
                throw new InvalidOperationException(
                    $"[CombatCommandJob] 명령 {_expectedNumber}의 처리가 기록되지 않았습니다(처리 번호 {handled}).");
            IsCompleted = true;
        }
    }
}
