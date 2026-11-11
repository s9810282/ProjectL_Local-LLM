using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 마지막 결과 끝 그리기에 쓴 캐릭터 목록·턴·국면입니다(씬 표시 나누기 설계 DS3). 재생 중 보인 사실(<c>CombatShownFacts</c>)과 다른,
    /// "완료 표시가 본 목록"이라는 사실입니다.
    /// </summary>
    /// <remarks>
    /// 쓰는 곳은 표시 하나입니다. 결과 끝 그리기가 기록하고 선택 비우기가 비웁니다. 읽는 곳은 선택 표시(행동 커서)와
    /// 공격 피드백(호버 범위 원점)입니다. 연결마다 Factory가 하나 만들어 같은 것을 넘깁니다(계획 TP-12).
    /// </remarks>
    internal sealed class CombatDisplaySnapshot
    {
        /// <summary>마지막 결과 끝 그리기의 캐릭터 목록입니다. 비웠으면 빈 목록입니다.</summary>
        internal IReadOnlyList<CharacterData> Characters { get; private set; } = Array.Empty<CharacterData>();

        /// <summary>마지막 결과 끝 그리기의 턴입니다. 비웠으면 null입니다.</summary>
        internal CombatTurnFacts Turn { get; private set; }

        /// <summary>마지막 결과 끝 그리기의 전투 국면입니다. 비워도 지난 값이 남습니다(턴이 null이면 읽지 않음).</summary>
        internal CombatFlowPhase Phase { get; private set; }

        /// <summary>결과 끝 그리기 한 번의 목록·턴·국면을 기록합니다. 목록은 <see cref="Copy"/>로 고정한 것을 넘깁니다.</summary>
        internal void Record(IReadOnlyList<CharacterData> characters, CombatTurnFacts turn, CombatFlowPhase phase)
        {
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Turn = turn;
            Phase = phase;
        }

        /// <summary>목록과 턴을 비웁니다. 국면은 그대로 둡니다(턴이 null이면 읽지 않음).</summary>
        internal void Clear()
        {
            Characters = Array.Empty<CharacterData>();
            Turn = null;
        }

        /// <summary>가변 Buffer 목록의 불변 개체 참조를 완료 표시 한 번에 고정합니다(계획 TP-6).</summary>
        internal static IReadOnlyList<CharacterData> Copy(IReadOnlyList<CharacterData> characters)
        {
            if (characters.Count == 0)
                return Array.Empty<CharacterData>();
            var snapshot = new CharacterData[characters.Count];
            for (int i = 0; i < snapshot.Length; i++)
                snapshot[i] = characters[i];
            return Array.AsReadOnly(snapshot);
        }
    }
}
