using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 전투 준비 게시와 정의 설치, 완료 후 전투 정리와 정의 해제를 한곳에서 관리한다.
    /// 진입 허용·승패·회복은 판단하지 않는다. 정의는 게시된 전투 ID와 함께 보관하며 DB를 다시 읽지 않는다.
    /// </summary>
    internal sealed class BattleLifetime : IBattleDefinitionRead, ISystemPostBindService
    {
        private ICombatStateBufferReadSink _combat;
        private IBattleBufferWriteSink _battle;
        private BattleInitialization _definitions;
        private long _battleInstanceId;

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _combat = buffers.CombatStateReadSink;
            _battle = buffers.BattleWriteSink;
        }

        /// <summary>선택과 준비된 전투를 함께 게시하고, 성공한 뒤에만 정의를 설치한다.</summary>
        public void Install(BattlePreparation preparation, BattleEntrySelection selection, BattleInitialization definitions)
        {
            if (preparation == null) throw new ArgumentNullException(nameof(preparation));
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            EnsureCanInstall();
            _battle.PrepareBattle(preparation, selection);
            Remember(definitions);
        }

        /// <summary>완료가 기록된 전투만 정리하고, Buffer 정리가 성공한 뒤 정의를 해제한다.</summary>
        public void EndCompleted()
        {
            EnsureBound();
            if (_combat.HasBattle && _combat.Completion == null)
                throw new InvalidOperationException("전투 완료 결과를 기록한 뒤 정리해야 합니다.");
            _battle.EndBattle();
            _definitions = null;
            _battleInstanceId = 0;
        }

        public BattleInitialization Require(long battleInstanceId)
        {
            if (_definitions == null)
                throw new InvalidOperationException("설치된 전투 정의가 없습니다.");
            if (battleInstanceId != _battleInstanceId)
                throw new InvalidOperationException("요청한 전투와 설치된 전투 정의가 다릅니다.");
            return _definitions;
        }

        private void EnsureCanInstall()
        {
            EnsureBound();
            if (_combat.HasBattle)
                throw new InvalidOperationException("이미 준비된 전투가 있습니다.");
        }

        private void Remember(BattleInitialization definitions)
        {
            _definitions = definitions;
            _battleInstanceId = _combat.BattleInstanceId;
        }

        private void EnsureBound()
        {
            if (_combat == null || _battle == null)
                throw new InvalidOperationException("전투 수명 관리는 Buffer 바인딩이 필요합니다.");
        }
    }
}
