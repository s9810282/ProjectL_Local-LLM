using System;
using System.Threading;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    internal sealed class RecoveryOperationPreparation
    {
        internal RecoveryStageService Owner { get; }
        internal object Scope { get; }
        internal RecoveryCommandData Command { get; }
        internal RecoveryOperationResult Result { get; }
        internal PreparedRecoveryMutation Mutation { get; }
        internal RecoveryStateProjection.Facts Facts { get; }
        internal bool Consumed { get; set; }

        internal RecoveryOperationPreparation(RecoveryStageService owner, object scope, RecoveryCommandData command,
            RecoveryOperationResult result, PreparedRecoveryMutation mutation, RecoveryStateProjection.Facts facts)
        {
            Owner = owner;
            Scope = scope;
            Command = command;
            Result = result;
            Mutation = mutation;
            Facts = facts;
        }
    }

    /// <summary>런타임 스레드의 동기 회복 호출만 허용하고 준비 중 재진입을 거절한다.</summary>
    internal sealed class RecoveryOperationGuard
    {
        private readonly int _threadId = Thread.CurrentThread.ManagedThreadId;
        private Scope _scope;
        internal object ActiveScope => _scope;

        internal IDisposable Enter()
        {
            if (Thread.CurrentThread.ManagedThreadId != _threadId || _scope != null)
                throw new InvalidOperationException("회복 처리의 병렬 호출 또는 재진입은 허용되지 않습니다.");
            var scope = new Scope(this);
            _scope = scope;
            return scope;
        }

        internal void RequireActive()
        {
            if (Thread.CurrentThread.ManagedThreadId != _threadId || _scope == null)
                throw new InvalidOperationException("회복 준비와 설치는 같은 동기 처리 범위 안에서 실행해야 합니다.");
        }

        private sealed class Scope : IDisposable
        {
            private RecoveryOperationGuard _owner;
            internal Scope(RecoveryOperationGuard owner) => _owner = owner;
            public void Dispose()
            {
                if (_owner == null) return;
                _owner._scope = null;
                _owner = null;
            }
        }
    }
}
