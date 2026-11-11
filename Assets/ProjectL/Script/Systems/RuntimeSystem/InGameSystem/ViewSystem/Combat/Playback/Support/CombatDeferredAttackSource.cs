using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>조기 실행의 출처 수명을 원래 공격 사건과 합류할 때 엽니다. 실제 피해 해제는 기존 Gate가 소유합니다.</summary>
    internal sealed class CombatDeferredAttackSource : ICombatSourceLifetime
    {
        private readonly ICombatSourceLifetime _target;
        private readonly Func<int> _generation;
        private CombatEvent _source;
        private EntityId _actor;
        private CombatHitCompletion _policy;
        private int _version;
        private bool _opened;
        private bool _ended;
        private bool _cancelled;
        private Action _completed;

        internal CombatDeferredAttackSource(ICombatSourceLifetime target, Func<int> generation)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        internal bool CanRelease => _opened && !_cancelled && _version == _generation();

        public void BeginSource(CombatEvent source, EntityId actorId, CombatHitCompletion completion, Action completed, int version)
        {
            if (_source != null)
                throw new InvalidOperationException("조기 공격 출처는 한 번만 시작합니다.");
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _actor = actorId;
            _policy = completion;
            _version = version;
        }

        internal void Join(Action completed)
        {
            if (_source == null || _opened || _cancelled || _version != _generation())
                throw new InvalidOperationException("조기 공격 출처의 합류 수명이 잘못되었습니다.");
            _completed = completed ?? throw new ArgumentNullException(nameof(completed));
            _opened = true;
            _target.BeginSource(_source, _actor, _policy, Complete, _version);
            if (_ended && CanRelease)
                _target.EndSource(_source, _version);
        }

        public void ReleaseAll(CombatEvent source, int version)
        {
            if (Matches(source, version) && CanRelease)
                _target.ReleaseAll(source, version);
        }

        public void EndSource(CombatEvent source, int version)
        {
            if (!Matches(source, version) || _cancelled) return;
            _ended = true;
            if (CanRelease)
                _target.EndSource(source, version);
        }

        public void AbortSource(CombatEvent source)
        {
            if (!ReferenceEquals(source, _source)) return;
            _cancelled = true;
            if (_opened)
                _target.AbortSource(source);
            _completed = null;
        }

        private bool Matches(CombatEvent source, int version) =>
            ReferenceEquals(source, _source) && version == _version && version == _generation();

        private void Complete()
        {
            Action completed = _completed;
            _completed = null;
            if (!_cancelled && _version == _generation())
                completed?.Invoke();
        }
    }
}
