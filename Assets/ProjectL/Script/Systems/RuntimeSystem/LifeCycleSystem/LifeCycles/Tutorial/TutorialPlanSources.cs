using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial
{
    /// <summary>
    /// TutorialPlan과 Job이 읽고 쓰는 곳의 묶음입니다(튜토리얼 설계 §9.1). 다른 Plan처럼 Buffer와 실행 창구만 담고,
    /// 전체 BufferQuery를 Job에 들고 다니지 않습니다. 상태는 갖지 않으며 struct라 매 판단마다 만들어도 할당이 없습니다.
    /// 단계 순서와 실습 통과 조건은 상태 Store의 단계 규칙에서 읽습니다(구현 계획 §12.3).
    /// </summary>
    internal readonly struct TutorialPlanSources
    {
        internal TutorialPlanSources(
            ITutorialBufferStore store,
            ITutorialStateBufferReadSink state,
            ICombatStateBufferReadSink combatState,
            ICombatResultHistoryReadSink combatResults,
            ICombatViewBufferReadSink combatInput,
            ITutorialViewExecution execution,
            ICombatProgressView progress,
            ICharacterBufferReadSink characters)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            State = state ?? throw new ArgumentNullException(nameof(state));
            CombatState = combatState ?? throw new ArgumentNullException(nameof(combatState));
            CombatResults = combatResults ?? throw new ArgumentNullException(nameof(combatResults));
            CombatInput = combatInput ?? throw new ArgumentNullException(nameof(combatInput));
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
        }

        /// <summary>다른 Plan과 같은 재료(Buffer 묶음·실행 창구)에서 필요한 것만 꺼냅니다.</summary>
        internal static TutorialPlanSources From(IBufferQuery buffers, ITutorialViewExecution execution,
            ICombatProgressView progress)
        {
            if (buffers == null)
                throw new ArgumentNullException(nameof(buffers));

            return new TutorialPlanSources(buffers.TutorialStore, buffers.TutorialStateReadSink,
                buffers.CombatStateReadSink, buffers.CombatResultHistoryReadSink, buffers.CombatViewReadSink,
                execution, progress, buffers.CharacterReadSink);
        }

        /// <summary>요청 큐·결과·세션·단계 규칙의 원본입니다. 바꾸는 것은 Job만 합니다.</summary>
        internal ITutorialBufferStore Store { get; }

        internal ITutorialStateBufferReadSink State { get; }

        internal ICombatStateBufferReadSink CombatState { get; }

        internal ICombatResultHistoryReadSink CombatResults { get; }

        internal ICombatViewBufferReadSink CombatInput { get; }

        /// <summary>실행 소유자의 내부 사실과 실행 창구입니다.</summary>
        internal ITutorialViewExecution Execution { get; }

        /// <summary>설명 시작에 필요한 기존 전투 View의 진행 사실만 읽습니다.</summary>
        internal ICombatProgressView Progress { get; }

        /// <summary>실제 피격 대상의 소유 진영을 읽습니다. 사망한 대상도 같은 전투의 기록으로 확인합니다.</summary>
        internal ICharacterBufferReadSink Characters { get; }
    }
}
