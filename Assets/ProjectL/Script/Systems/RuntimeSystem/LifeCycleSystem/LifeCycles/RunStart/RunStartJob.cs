using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.RunStart
{
    /// <summary>
    /// 기본 로드아웃으로 새 Run을 한 번 시작하는 유일한 Job입니다. 로비 출발과 Stage 첫 진입이 같이 씁니다(검사 후속 정리 설계 Q-R7, 계획 TP-R8).
    /// 남은 전투 막기·맵 입력 비우기·호출·성공한 결과의 로스터 설치 확인을 여기서 하고, 결과를 받는 쪽은 <see cref="IRunStartOutcome"/>입니다.
    /// </summary>
    internal sealed class RunStartJob : IJobService
    {
        /// <summary>새 Run의 시작 로드아웃 키입니다. Run 설정 원본(DT)이 생기기 전까지 쓰는 기본값입니다.</summary>
        internal const int DefaultLoadoutKey = 1;

        private readonly IRunStartService _runStart;
        private readonly IRunStartOutcome _outcome;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly IStageViewBufferWriteSink _stageInput;
        private readonly ICharacterBufferReadSink _roster;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal RunStartJob(IRunStartService runStart, IRunStartOutcome outcome, ICombatStateBufferReadSink combat,
            IStageViewBufferWriteSink stageInput, ICharacterBufferReadSink roster, int priority)
        {
            _runStart = runStart ?? throw new ArgumentNullException(nameof(runStart));
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _stageInput = stageInput ?? throw new ArgumentNullException(nameof(stageInput));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            Priority = priority;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;
            try
            {
                if (_outcome.TryStart())
                {
                    // 새 Run은 전투를 비울 수 있으므로 남은 전투를 실행 계층에서 먼저 막습니다.
                    if (_combat.HasBattle)
                        throw new InvalidOperationException("[RunStartJob] 이전 전투가 남아 있어 새 Run을 시작할 수 없습니다.");
                    // 이전 Run의 맵 입력을 취소합니다. 발급 번호와 이미 게시한 결과는 유지합니다.
                    _stageInput.Clear();
                    RosterAcquireResult result = _runStart.StartNewRun(DefaultLoadoutKey);
                    _outcome.Publish(result);
                    if (result.Succeeded)
                        RequireInstalledRoster(result);
                }
                // 취소된 요청은 Core를 부르지 않고 그대로 끝냅니다.
                IsCompleted = true;
            }
            catch
            {
                _outcome.Fail();
                throw;
            }
        }

        /// <summary>이 호출의 계약만 봅니다. 돌려준 로스터와 메인 지속 상태가 Buffer에 그대로 게시돼 있어야 합니다.</summary>
        private void RequireInstalledRoster(RosterAcquireResult result)
        {
            if (result.Acquired.Count == 0 || _roster.Owned.Count != result.Acquired.Count)
                throw new InvalidOperationException("[RunStartJob] 성공한 새 Run 호출의 실제 로스터가 게시되지 않았습니다.");
            int mainCount = 0;
            foreach (var acquired in result.Acquired)
            {
                if (!_roster.TryGetOwned(acquired.Id, out var installed) || installed.CharacterKey != acquired.CharacterKey ||
                    installed.Role != acquired.Role || installed.Level != acquired.Level)
                    throw new InvalidOperationException("[RunStartJob] 실제 로스터가 이번 새 Run 결과와 다릅니다.");
                if (installed.Role == CharacterOwnershipRole.Main)
                {
                    if (!_roster.TryGetMainState(installed.Id, out _))
                        throw new InvalidOperationException("[RunStartJob] 게시된 메인 지속 상태가 없습니다.");
                    mainCount++;
                }
            }
            if (mainCount != 1)
                throw new InvalidOperationException("[RunStartJob] 이번 새 Run의 메인 캐릭터가 없습니다.");
        }
    }
}
