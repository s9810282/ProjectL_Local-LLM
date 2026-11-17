using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.RunStart
{
    /// <summary>
    /// 기본 로드아웃(메인은 지정하면 그 메인)으로 새 Run을 한 번 시작하는 유일한 Job입니다. 로비 출발과 Stage 첫 진입이 같이 씁니다(검사 후속 정리 설계 Q-R7, 계획 TP-R8).
    /// 남은 전투 막기·맵 입력 비우기·호출·성공한 결과의 로스터 설치 확인을 여기서 하고, 결과를 받는 쪽은 <see cref="IRunStartOutcome"/>입니다.
    /// 메인 키는 만들 때 받은 값(로비는 제출한 요청에 실린 키)만 쓰고, 실행 순간에 화면 선택을 다시 읽지 않습니다(로비 메인 선택 설계 LM5).
    /// </summary>
    internal sealed class RunStartJob : IJobService
    {
        /// <summary>새 Run의 시작 로드아웃 키입니다. Run 설정 원본(DT)이 생기기 전까지 쓰는 기본값입니다.</summary>
        internal const int DefaultLoadoutKey = 1;

        private readonly IRunStartService _runStart;
        private readonly IRunStartOutcome _outcome;
        /// <summary>null이면 로드아웃 기본 메인입니다. 값이 있으면 <see cref="EntityKey.None"/>이라도 그대로 넘기고 Core가 판정합니다.</summary>
        private readonly EntityKey? _mainCharacterKey;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly IStageViewBufferWriteSink _stageInput;
        private readonly ICharacterBufferReadSink _roster;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal RunStartJob(IRunStartService runStart, IRunStartOutcome outcome, EntityKey? mainCharacterKey,
            ICombatStateBufferReadSink combat, IStageViewBufferWriteSink stageInput, ICharacterBufferReadSink roster, int priority)
        {
            _runStart = runStart ?? throw new ArgumentNullException(nameof(runStart));
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            _mainCharacterKey = mainCharacterKey;
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
                    RosterAcquireResult result = _mainCharacterKey.HasValue
                        ? _runStart.StartNewRun(DefaultLoadoutKey, _mainCharacterKey.Value)
                        : _runStart.StartNewRun(DefaultLoadoutKey);
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

        /// <summary>
        /// 이 호출의 계약만 봅니다. 돌려준 로스터와 메인 지속 상태가 Buffer에 그대로 게시돼 있어야 합니다.
        /// 메인을 지정한 호출이면 설치된 메인이 그 키여야 합니다. 지정하지 않은 호출은 키를 비교하지 않습니다(계획 TP-5).
        /// </summary>
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
                    if (_mainCharacterKey.HasValue && installed.CharacterKey != _mainCharacterKey.Value)
                        throw new InvalidOperationException(
                            $"[RunStartJob] 설치된 메인({installed.CharacterKey})이 지정한 메인({_mainCharacterKey.Value})과 다릅니다.");
                    mainCount++;
                }
            }
            if (mainCount != 1)
                throw new InvalidOperationException("[RunStartJob] 이번 새 Run의 메인 캐릭터가 없습니다.");
        }
    }
}
