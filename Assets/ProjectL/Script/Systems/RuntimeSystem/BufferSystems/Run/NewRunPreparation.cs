using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Run
{
    /// <summary>
    /// 새 Run 시작 제안이다. Core가 시작 로드아웃으로 정한 로스터와 시작 Run 재화를 담는다.
    /// Buffer는 이전 Run을 비운 뒤 이 값을 한 번에 설치한다. 로스터 규칙(메인 1·서브 수·자격)은 Core가 이미 판정했다.
    /// </summary>
    public sealed class NewRunPreparation
    {
        public IReadOnlyList<RosterAcquireRequest> Roster { get; }
        public MainCharacterPersistentState InitialMain { get; }
        public int StartingRunCurrency { get; }
        /// <summary>이번 Run의 스테이지 첫 손패 수(메인 포함).</summary>
        public int StartingHandSize { get; }

        public NewRunPreparation(IReadOnlyList<RosterAcquireRequest> roster, MainCharacterPersistentState initialMain,
            int startingRunCurrency, int startingHandSize)
        {
            if (startingHandSize < 1) throw new ArgumentOutOfRangeException(nameof(startingHandSize));
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (initialMain == null) throw new ArgumentNullException(nameof(initialMain));
            if (startingRunCurrency < 0) throw new ArgumentOutOfRangeException(nameof(startingRunCurrency));
            var copy = new RosterAcquireRequest[roster.Count];
            int mains = 0;
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = roster[i] ?? throw new ArgumentException("로스터 요청에 null을 넣을 수 없습니다.", nameof(roster));
                if (copy[i].Role == CharacterOwnershipRole.Main) mains++;
            }
            // 새 Run은 메인 캐릭터 하나로 시작한다는 저장 형식만 확인한다.
            if (mains != 1) throw new ArgumentException("새 Run 로스터에는 메인 캐릭터가 정확히 하나 필요합니다.", nameof(roster));

            Roster = Array.AsReadOnly(copy);
            InitialMain = initialMain;
            StartingRunCurrency = startingRunCurrency;
            StartingHandSize = startingHandSize;
        }
    }
}
