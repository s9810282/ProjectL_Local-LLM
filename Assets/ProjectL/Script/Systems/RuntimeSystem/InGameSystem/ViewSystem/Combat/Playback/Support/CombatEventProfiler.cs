#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>View가 이미 받은 사건과 표시 사실로만 분류합니다. 현재 턴이나 Core 조회를 사용하지 않습니다.</summary>
    internal sealed class CombatEventProfiler
    {
        private static readonly Dictionary<Type, CombatPlaybackProfile.ByOwner> _events = CreateEvents();
        private static readonly Dictionary<CombatEventKind, CombatPlaybackProfile.ByOwner> _characters = CreateCharacters();
        private static readonly CombatPlaybackProfile.ByOwner _unknown = new("Unknown");
        private static readonly CombatPlaybackProfile.ByOwner _unit = new("UnitRun");
        private static readonly CombatPlaybackProfile.ByOwner _accent = new("Accent");
        private readonly Func<EntityId, CharacterCombatFacts> _findCharacter;

        internal CombatEventProfiler(Func<EntityId, CharacterCombatFacts> findCharacter)
        {
            _findCharacter = findCharacter;
        }

        internal static CombatPlaybackProfile Unknown => _unknown.Unknown;

        internal CombatPlaybackProfile Of(CombatEvent item)
        {
            if (item == null) return Unknown;
            if (item is CombatCharacterEvent character)
            {
                var profiles = _characters.TryGetValue(character.Kind, out var found) ? found : _unknown;
                return profiles.Of((character.Expected ?? character.Final)?.Owner);
            }
            var set = _events.TryGetValue(item.GetType(), out var registered) ? registered : _unknown;
            // 추가 공격은 대상이 아니라 출처 공격자의 진영입니다.
            EntityId actor = CombatEventTypeTable.ActorOf(item);
            if (CombatEventTypeTable.TryGetAttackSource(item, out var source))
                actor = source.AttackerId;
            if (actor.IsEmpty && CombatEventTypeTable.TryGetSubject(item, out EntityId subject))
                actor = subject;
            if (actor.IsEmpty) return set.System;
            if (item is CombatSummonEvent summoned && actor == summoned.Created.EntityId)
                return set.Of(summoned.Created.Owner);
            if (item is CombatReinforcementEvent reinforcement && actor == reinforcement.Created.EntityId)
                return set.Of(reinforcement.Created.Owner);
            return set.Of(_findCharacter?.Invoke(actor)?.Owner);
        }

        internal CombatPlaybackProfile Of(CombatComposition composition)
        {
            var source = composition.ProfilingEvent ?? composition.Completion.Source;
            if (source != null) return Of(source);
            if (composition.ProfilingEventType != null && _events.TryGetValue(composition.ProfilingEventType, out var set))
                return set.Of(_findCharacter?.Invoke(composition.Subject)?.Owner);
            return composition.Subject.IsEmpty ? _accent.System : _unit.Of(_findCharacter?.Invoke(composition.Subject)?.Owner);
        }

        internal CombatPlaybackProfile AttackOf(EntityId actor) =>
            _events[typeof(CombatAttackEvent)].Of(_findCharacter?.Invoke(actor)?.Owner);

        private static Dictionary<Type, CombatPlaybackProfile.ByOwner> CreateEvents()
        {
            var result = new Dictionary<Type, CombatPlaybackProfile.ByOwner>();
            foreach (Type type in CombatEventTypeTable.EventTypes)
            {
                string name = type.Name;
                if (name.StartsWith("Combat", StringComparison.Ordinal)) name = name.Substring(6);
                if (name.EndsWith("Event", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 5);
                result.Add(type, new CombatPlaybackProfile.ByOwner(name));
            }
            return result;
        }

        private static Dictionary<CombatEventKind, CombatPlaybackProfile.ByOwner> CreateCharacters()
        {
            var result = new Dictionary<CombatEventKind, CombatPlaybackProfile.ByOwner>();
            foreach (CombatEventKind kind in Enum.GetValues(typeof(CombatEventKind)))
                result[kind] = new CombatPlaybackProfile.ByOwner("Character." + kind);
            return result;
        }
    }
}
#endif
