using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageCommandSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// Core 처리 서비스에 전투 준비를 한 번 요청합니다.
    /// 준비 재료의 출처와 전투 상태 설치는 Core가 맡습니다.
    /// </summary>
    internal struct CombatPrepareJob : IJobService
    {
        private readonly IBattleEntryService _battleEntry;
        private readonly IStageCommandProcessingService _mapCommands;
        private readonly CombatEntryRequest _entry;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 진입 요청의 대상으로 전투를 준비합니다. 결과는 요청 객체에 남깁니다(골격 설계 §6.3 K20).
        /// 진입 종류에 필요한 Core 서비스 하나만 요구합니다. 직행은 전투 진입, 맵 진입은 맵 명령 처리입니다(골격 점검 설계 K-5 e).
        /// </summary>
        internal CombatPrepareJob(IBattleEntryService battleEntry, IStageCommandProcessingService mapCommands, int priority,
            CombatEntryRequest entry)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            if (entry.Kind == CombatEntryKind.Direct && battleEntry == null)
                throw new ArgumentNullException(nameof(battleEntry));
            if (entry.Kind == CombatEntryKind.Map && mapCommands == null)
                throw new ArgumentNullException(nameof(mapCommands));
            _battleEntry = battleEntry;
            _mapCommands = mapCommands;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            try
            {
                // 취소된 요청은 Core를 부르지 않고 배출만 한다.
                if (_entry.TryStart())
                {
                    if (_entry.Kind == CombatEntryKind.Direct)
                    {
                        _entry.PublishResult(_battleEntry.EnterBattle(_entry.ChapterKey, _entry.NodeId));
                    }
                    else if (_mapCommands.TryProcessNextBattleCommand(_entry.MapCommandNumber, out var result))
                    {
                        // 동기 Core 거래가 완료된 결과는 호출 중 입력이 지워져도 보존합니다.
                        _entry.PublishResult(result);
                    }
                    else
                    {
                        _entry.CancelUnprocessed();
                    }
                }
                _entry.CompleteJob();
            }
            catch
            {
                _entry.Fail();
                throw;
            }
            IsCompleted = true;
        }
    }
}
