using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class CharacterStateChange
    {
        public CharacterCombatFacts Expected { get; }
        public CharacterCombatFacts Final { get; }

        public CharacterStateChange(CharacterCombatFacts expected, CharacterCombatFacts final)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));

            if (expected.EntityId != final.EntityId)
                throw new ArgumentException("캐릭터 변경은 전투 개체 ID를 바꿀 수 없습니다.", nameof(final));
        }
    }

    public sealed class MainCharacterPersistentChange
    {
        public MainCharacterPersistentState Expected { get; }
        public MainCharacterPersistentState Final { get; }

        public MainCharacterPersistentChange(MainCharacterPersistentState expected,
            MainCharacterPersistentState final)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
        }
    }

    /// <summary>한 명령에서 검증 후 함께 반영할 캐릭터 변경 제안이다.</summary>
    public sealed class CharacterChanges
    {
        public IReadOnlyList<CharacterStateChange> Characters { get; }
        public IReadOnlyList<SoulCountChange> Souls { get; }
        public MainCharacterPersistentChange Main { get; }
        public IReadOnlyList<CharacterCombatFacts> Created { get; }

        public CharacterChanges(IReadOnlyList<CharacterStateChange> characters,
            IReadOnlyList<SoulCountChange> souls,
            MainCharacterPersistentChange main = null, IReadOnlyList<CharacterCombatFacts> created = null)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (souls == null)
                throw new ArgumentNullException(nameof(souls));

            var characterCopy = new CharacterStateChange[characters.Count];
            for (int i = 0; i < characterCopy.Length; i++)
            {
                CharacterStateChange change = characters[i];
                if (change == null)
                    throw new ArgumentException("캐릭터 변경에는 null을 넣을 수 없습니다.", nameof(characters));

                characterCopy[i] = change;
            }

            var soulCopy = new SoulCountChange[souls.Count];
            for (int i = 0; i < soulCopy.Length; i++)
            {
                SoulCountChange change = souls[i];
                if (change == null)
                    throw new ArgumentException("영혼 변경에는 null을 넣을 수 없습니다.", nameof(souls));

                soulCopy[i] = change;
            }

            Characters = Array.AsReadOnly(characterCopy);
            Souls = Array.AsReadOnly(soulCopy);
            Main = main;
            Created = Array.AsReadOnly((created ?? Array.Empty<CharacterCombatFacts>()).ToArray());
            if (Created.Any(c => c == null)) throw new ArgumentException("생성할 캐릭터는 null일 수 없습니다.");
        }
    }
}
