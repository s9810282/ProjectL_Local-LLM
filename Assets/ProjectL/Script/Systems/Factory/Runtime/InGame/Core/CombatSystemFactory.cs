using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.Factory.Runtime.InGame.Core
{
    /// <summary>
    /// 전투 역할 서비스와 공유 협력 객체를 만들어 전달받은 sink에 한 번씩 등록한다.
    /// 하위 Builder를 만들거나 Build하지 않으며, 외부 Stage 명령 조립에 필요한 진입 계약만 돌려준다.
    /// </summary>
    internal static class CombatSystemFactory
    {
        internal static IBattleEntryService Register(ISystemWriteSink sink, IBattleInitializationSource source,
            ICharacterRosterService characters, IChapterProgressionCoordinator progression, bool usePreparedEnemyAdvance = false)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (progression == null) throw new ArgumentNullException(nameof(progression));
            if (source == null)
            {
                var databaseSource = new DatabaseBattleInitializationSource();
                sink.AddModule<ISystemModule>(databaseSource);
                source = databaseSource;
            }
            var movementRules = CombatMovementRules.Validated(new IMovementTileRule[] {
                new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule() });

            // 공유 협력 객체는 여기서 한 번만 만들고, 각자 자기 바인딩만 하도록 모듈로 등록한다.
            var lifetime = new BattleLifetime();
            sink.AddModule<ISystemModule>(lifetime);
            var artifacts = new ArtifactLoadoutReader();
            sink.AddModule<ISystemModule>(artifacts);
            var assembler = new CombatInputAssembler(lifetime, artifacts);
            sink.AddModule<ISystemModule>(assembler);
            var planner = new BattleProgressionPlanner(progression);
            sink.AddModule<ISystemModule>(planner);
            var publisher = new CombatTransitionPublisher(planner);
            sink.AddModule<ISystemModule>(publisher);

            var entry = new BattleEntryService(source, characters, progression, lifetime);
            entry.ExportServices(sink);
            new BattleCompletionService(lifetime, artifacts, planner).ExportServices(sink);
            new CombatCommandService(assembler, publisher, movementRules).ExportServices(sink);
            // 성능·Flow 인수 전에는 기본 자동 진행을 유지한다. 진단 조립에서만 명시적으로 준비 경로를 선택한다.
            new CombatAdvanceService(assembler, publisher, movementRules, usePreparation: usePreparedEnemyAdvance).ExportServices(sink);
            new CombatPreviewService(assembler, movementRules).ExportServices(sink);
            // 첫 동작 준비는 게시 협력자 없이 조립기·이동 규칙만 받는다(첫 동작 Core 준비 계획 FU-a).
            new CombatFirstUsePreparationService(assembler, movementRules).ExportServices(sink);
            return entry;
        }
    }
}
