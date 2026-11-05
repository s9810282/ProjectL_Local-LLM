using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans
{
    /// <summary>
    /// 전투 진행 한 걸음을 판단하고 그 Job을 만드는 데 필요한 것만 담습니다(개선 설계 §5.1).
    /// Plan이 등록 때 두 그룹에서 한 번 꺼내 만들며, <see cref="Jobs.CombatNextJob"/>과 <see cref="CombatPlan.BuildStep"/>은 그룹을 받지 않습니다.
    /// </summary>
    /// <remarks>
    /// 상태는 갖지 않습니다. 사실은 쓸 때마다 <see cref="Progress"/>로 새로 읽고, 현재 진입 요청도 쓸 때 <see cref="Entry"/>에서 읽습니다.
    /// 사실 읽기만 늘 필요합니다. 나머지는 그 재료가 필요한 단계를 만들 때 읽고, 없으면 그때 멈춥니다(그룹에서 꺼내 쓰던 때와 같은 시점).
    /// </remarks>
    internal sealed class CombatStepContext
    {
        private readonly ICombatProgressView _view;
        private readonly ICombatEntryService _entry;
        private readonly ILifeCycleManager _manager;
        private readonly ICombatCommandService _commands;
        private readonly ICombatPreviewService _preview;
        private readonly ICombatAdvanceService _advance;
        private readonly IBattleCompletionService _battleCompletion;
        private readonly IChapterProgressionCoordinator _chapters;

        internal CombatStepContext(ICombatProgressReader progress, ICombatProgressView view, ICombatEntryService entry,
            ILifeCycleManager manager, ICombatCommandService commands, ICombatPreviewService preview,
            ICombatAdvanceService advance, IBattleCompletionService battleCompletion,
            IChapterProgressionCoordinator chapters)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _view = view;
            _entry = entry;
            _manager = manager;
            _commands = commands;
            _preview = preview;
            _advance = advance;
            _battleCompletion = battleCompletion;
            _chapters = chapters;
        }

        internal ICombatProgressReader Progress { get; }

        /// <summary>골격이 보는 View 계약입니다(제출 정지·완료 표시).</summary>
        internal ICombatProgressView View => _view ?? throw Missing("전투 View가 조립되지 않았습니다.");

        /// <summary>현재 진입 요청을 읽습니다. 완료 표시 Job이 대상 확인에 씁니다.</summary>
        internal ICombatEntryService Entry => _entry ?? throw Missing("전투 진입 서비스가 없습니다.");

        internal ILifeCycleManager Manager => _manager ?? throw Missing("LifeCycle 전환 서비스가 없습니다.");

        internal ICombatCommandService Commands => _commands ?? throw Missing("전투 명령 처리 서비스가 없습니다.");

        internal ICombatPreviewService Preview => _preview ?? throw Missing("전투 미리보기 서비스가 없습니다.");

        internal ICombatAdvanceService Advance => _advance ?? throw Missing("전투 자동 진행 서비스가 없습니다.");

        /// <summary>포기와 정리(EndBattle)입니다.</summary>
        internal IBattleCompletionService BattleCompletion =>
            _battleCompletion ?? throw Missing("전투 완료 서비스가 없습니다.");

        /// <summary>보스 승리의 챕터 완료입니다.</summary>
        internal IChapterProgressionCoordinator Chapters => _chapters ?? throw Missing("챕터 진행 서비스가 없습니다.");

        /// <summary>두 그룹에서 걸음 재료를 한 번 꺼냅니다. 사실 읽기가 없으면 등록 자리에서 멈춥니다.</summary>
        internal static CombatStepContext From(IInGameSystemGroupService service, ILifeCycleSystemGroupService lifeCycles)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            ICombatProgressReader progress = lifeCycles?.CombatProgress
                ?? throw Missing("전투 진행 사실을 읽을 LifeCycle 그룹이 없습니다.");
            IInGameCoreSystemGroupService core = service.CoreGroup;
            return new CombatStepContext(progress, service.ViewGroup?.Combat?.Progress, lifeCycles.CombatEntry,
                lifeCycles.LifeCycleManager, core?.CombatCommands, core?.CombatPreview, core?.CombatAdvance,
                core?.BattleCompletion, core?.ChapterProgression);
        }

        private static InvalidOperationException Missing(string what) => new($"CombatPlan: {what}");
    }
}
