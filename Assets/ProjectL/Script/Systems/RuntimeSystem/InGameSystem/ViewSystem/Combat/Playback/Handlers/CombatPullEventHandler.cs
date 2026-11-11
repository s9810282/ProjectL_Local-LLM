using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>Core의 실제 끌어오기 경로를 기존 유닛 이동 레인에서 끝낸 뒤 후속 사건을 허용합니다.</summary>
    internal sealed class CombatPullEventHandler : CombatEventHandler
    {
        private readonly CombatUnitMotionSet _motion;
        private readonly CombatShownFacts _facts;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatPreAttackPresentationStarts _preAttackStarts;

        internal CombatPullEventHandler(CombatUnitMotionSet motion, CombatShownFacts facts, Func<FieldGridMapper> mapper,
            CombatPreAttackPresentationStarts preAttackStarts = null)
        {
            _preAttackStarts = preAttackStarts;
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version)
        {
            var movement = ((CombatPullEvent)e).Movement;
            CharacterCombatFacts target = _facts.FindCharacter(movement.TargetId);
            if (target == null)
                throw new InvalidOperationException("끌어오기 대상의 표시 사실이 없습니다.");
            _motion.BeginPull(movement, target, _mapper(), () =>
            {
                _facts.RetainPullArrival(movement.TargetId);
                completed?.Invoke();
            }, _preAttackStarts?.CapturePullStart((CombatPullEvent)e, version));
            return true;
        }
    }
}
