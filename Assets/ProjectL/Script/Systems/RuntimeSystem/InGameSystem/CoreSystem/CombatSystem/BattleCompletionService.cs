using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 포기 허용·중복 완료 판단, 포기 정산과 완료 게시, 완료 후 정리를 맡는다.
    /// 명령 실행이나 미리보기를 호출하지 않으며 이미 정산된 명령 결과를 다시 정산하지 않는다.
    /// </summary>
    internal sealed class BattleCompletionService : IBattleCompletionService, ISystemPostBindService, IExportSystemServices
    {
        private readonly BattleLifetime _lifetime;
        private readonly ArtifactLoadoutReader _artifacts;
        private readonly BattleProgressionPlanner _planner;
        private ICharacterBufferReadSink _characters;
        private ICombatStateBufferReadSink _combat;
        private IRunProgressBufferReadSink _run;
        private IBattleBufferWriteSink _battle;

        internal BattleCompletionService(BattleLifetime lifetime, ArtifactLoadoutReader artifacts, BattleProgressionPlanner planner)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _characters = buffers.CharacterReadSink;
            _combat = buffers.CombatStateReadSink;
            _run = buffers.RunProgressReadSink;
            _battle = buffers.BattleWriteSink;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IBattleCompletionService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public BattleAbandonResult AbandonBattle()
        {
            EnsureBound();
            if (_combat.Completion != null)
                return BattleAbandonResult.AlreadyCompleted;
            if (RunEnded) return BattleAbandonResult.RunEnded;
            if (!_combat.HasBattle) return BattleAbandonResult.NoActiveBattle;

            // DB 정의 오류는 완료 계산 전에 예외로 드러나며 아무것도 바꾸지 않는다.
            ArtifactLoadoutSnapshot loadout = _artifacts.Read();
            if (Complete(new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.VoluntaryAbandonment), loadout))
                return BattleAbandonResult.Abandoned;
            // 완료 판단 중 Run이 끝났거나 같은 완료가 이미 기록된 경우다.
            return RunEnded ? BattleAbandonResult.RunEnded : BattleAbandonResult.AlreadyCompleted;
        }

        public void EndBattle()
        {
            EnsureBound();
            _lifetime.EndCompleted();
        }

        /// <summary>현재 원본으로 완료 제안을 계산해 게시한다. Run 종료 거절이면 false다.</summary>
        private bool Complete(StageOutcomeSnapshot outcome, ArtifactLoadoutSnapshot loadout)
        {
            var main = ReadMain();
            if (!_characters.TryGetMainState(main.OwnedId.Value, out var persistent))
                throw new InvalidOperationException("전투 메인 지속 상태가 필요합니다.");
            var progress = _planner.Plan(outcome.Outcome);
            // Run 종료 거절이다. 그 밖의 거절은 planner가 상태 오류로 드러낸다.
            if (!progress.Accepted)
                return false;

            var changes = new BattleCompletionCalculator(loadout).Complete(main, persistent, outcome, progress);
            var characters = new List<CharacterStateChange>(changes.Characters.Characters);
            // 메인은 계산기가 정리했다. 나머지 참가자는 Buffer에서 읽고, 무엇을 비울지는 Core 정리 규칙에 맡긴다.
            foreach (var character in _characters.Characters)
            {
                if (character.EntityId == main.EntityId) continue;
                var before = character.ToFacts();
                var cleared = CombatTemporaryStateCleanup.Clear(before);
                if (!ReferenceEquals(before, cleared)) characters.Add(new CharacterStateChange(before, cleared));
            }
            // 이 완료 경로(포기 등)는 보유 기록을 바꾸지 않는다. 승리의 레벨 반영은 명령 게시 경로가 한다.
            // 변경을 버리지 않고 넘겨 두면 잘못 들어온 경우 Buffer가 거절해 드러난다.
            var finalChanges = new CharacterChanges(characters, changes.Characters.Souls,
                changes.Characters.Main, changes.Characters.Created, changes.Characters.Owned);
            return _battle.CompleteBattle(new BattleCompletionChanges(finalChanges, changes.Outcome, changes.Progression));
        }

        private CharacterCombatFacts ReadMain()
        {
            // 정산 대상 메인은 진입 때 포착한 참가 목록(Combat 상태)이 정한다. 역할로 메인을 찾지 않는다.
            var membership = _combat.Current.Membership
                ?? throw new InvalidOperationException("전투 참가 목록이 필요합니다.");
            return System.Linq.Enumerable.Single(_characters.Characters, c => c.ToFacts().OwnedId == membership.MainId).ToFacts();
        }

        private void EnsureBound()
        {
            if (_combat == null)
                throw new InvalidOperationException("전투 서비스는 Buffer 바인딩이 필요합니다.");
        }

        private bool RunEnded => _run.Outcome != RunOutcome.InProgress;
    }
}
