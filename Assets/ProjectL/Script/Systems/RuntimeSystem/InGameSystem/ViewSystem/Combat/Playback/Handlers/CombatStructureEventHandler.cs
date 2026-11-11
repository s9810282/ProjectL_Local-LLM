using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>기존 Gate 해제 뒤 Structure의 피격 피드백과 사실 반영을 수행합니다.</summary>
    internal sealed class CombatStructureEventHandler : CombatEventHandler
    {
        private readonly CombatStructureHitFeedback _feedback;
        private readonly Action<StructureCombatFacts> _apply;
        private readonly Func<CombatEvent, bool> _isSplit;
        internal CombatStructureEventHandler(Func<EntityId, Pose?> body, Action<EntityKey, EntityId, Pose> cue,
            Action<Vector3, int> damage, Action<StructureCombatFacts> apply)
            : this(new CombatStructureHitFeedback(body, cue, damage), apply)
        {
        }

        /// <param name="isSplit">연발 연결이 없으면 전체 숫자를 표시하는 기존 단독 처리 경로입니다.</param>
        internal CombatStructureEventHandler(CombatStructureHitFeedback feedback, Action<StructureCombatFacts> apply,
            Func<CombatEvent, bool> isSplit = null)
        {
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            _isSplit = isSplit;
        }

        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version) =>
            TryBeginBeforeFacts(e, e, completed, version);

        internal override bool TryBeginBeforeFacts(CombatEvent e, CombatEvent original, Action completed, int version)
        {
            _feedback.ShowHit((CombatStructureEvent)e, _isSplit?.Invoke(original) != true);
            return false;
        }
        internal override void ApplyFacts(CombatEvent e) => _apply(((CombatStructureEvent)e).Final);
    }
}
