using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// Core 처리 서비스에 전투 준비를 한 번 요청합니다.
    /// 준비 재료의 출처와 전투 상태 설치는 Core가 맡습니다.
    /// </summary>
    internal struct CombatPrepareJob : IJobService
    {
        private readonly IInGameSystemGroupService _service;
        private readonly CombatEntryRequest _entry;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatPrepareJob(IInGameSystemGroupService service, int priority)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _entry = null;
            Priority = priority;
            IsCompleted = false;
        }

        /// <summary>새 첫/다음 전투 진입은 명시적 대상을 사용합니다. 두 인자 생성자는 확정 선택의 호환용입니다.</summary>
        internal CombatPrepareJob(IInGameSystemGroupService service, int priority, CombatEntryRequest entry)
            : this(service, priority)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (_service == null)
                throw new InvalidOperationException("CombatPrepareJob requires an in-game service.");

            if (_entry != null)
            {
                try
                {
                    if (_entry.TryStart())
                        _entry.PublishResult(_service.CoreGroup.Combat.EnterBattle(_entry.ChapterKey, _entry.NodeId));
                    _entry.CompleteJob();
                }
                catch
                {
                    _entry.Fail();
                    throw;
                }
            }
            else
            {
                // 이미 확정된 선택을 쓰는 기존 호출자만 유지한다. 새 LifeCycle 진입은 이 경로를 쓰지 않는다.
                _service.CoreGroup.Combat.PrepareBattle();
            }
            IsCompleted = true;
        }
    }
}
