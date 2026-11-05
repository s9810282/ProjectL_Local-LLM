using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 공개 이동 투영이 실제로 선택한 프로필을 개체별로 기록한다.</summary>
    internal sealed class CombatProfileReadSet
    {
        private readonly Dictionary<EntityId, CombatAttackProfile> _entries = new();

        internal IReadOnlyDictionary<EntityId, CombatAttackProfile> Entries { get; }

        internal CombatProfileReadSet()
        {
            Entries = new ReadOnlyDictionary<EntityId, CombatAttackProfile>(_entries);
        }

        internal void Record(EntityId entityId, CombatAttackProfile profile)
        {
            if (entityId.IsEmpty)
            {
                throw new ArgumentOutOfRangeException(nameof(entityId));
            }

            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (_entries.TryGetValue(entityId, out var recorded))
            {
                if (!ReferenceEquals(recorded, profile))
                {
                    throw new InvalidOperationException($"한 이동 투영에서 개체 {entityId.Value}의 프로필 선택이 달라졌습니다.");
                }

                return;
            }

            _entries.Add(entityId, profile);
        }
    }
}
