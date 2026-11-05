using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item
{
    /// <summary>플레이어 시작 덱의 유닛 하나와 수량입니다.</summary>
    public readonly struct PlayerLoadoutEntry
    {
        public EntityKey UnitKey { get; }
        public int Count { get; }

        public PlayerLoadoutEntry(EntityKey unitKey, int count)
        {
            UnitKey = unitKey;
            Count = count;
        }
    }

    /// <summary>
    /// 플레이어 시작 구성입니다. Core가 받는 순수 C# 데이터이며 PlayerLoadoutDataSO가 생성합니다.
    /// </summary>
    /// <remarks>
    /// 새 Run 시작(StartNewRun)이 메인 캐릭터와 시작 서브로 로스터를 만들고, 시작 Run 재화를 지급하고, 첫 손패 수를 Run에 기록합니다.
    /// 셔플 시드는 스테이지마다 다르게 섞이도록 스테이지 TimelineSeed를 쓰므로 여기에 두지 않습니다.
    /// </remarks>
    public sealed class PlayerLoadoutData : IData
    {
        public IReadOnlyList<int> InitialDeckGrantIndices { get; }
        public int LoadoutKey { get; }
        public EntityKey MainCharacterKey { get; }
        public IReadOnlyList<PlayerLoadoutEntry> Entries { get; }
        /// <summary>각 스테이지 첫 손패 수(메인 포함). 첫 손패에만 적용되며 덱에서 뽑는 서브 수는 이 값 - 1입니다.</summary>
        public int StartingHandSize { get; }
        /// <summary>새 Run 시작 때 지급하는 Run 재화(상점·회복용)입니다. 전투의 스테이지 골드와 별개입니다.</summary>
        public int StartingRunCurrency { get; }

        public PlayerLoadoutData(
            int loadoutKey,
            EntityKey mainCharacterKey,
            PlayerLoadoutEntry[] entries,
            int startingHandSize,
            int startingRunCurrency = 0, int[] initialDeckGrantIndices = null)
        {
            InitialDeckGrantIndices = Array.AsReadOnly((int[])(initialDeckGrantIndices?.Clone() ?? Array.Empty<int>()));
            LoadoutKey = loadoutKey;
            MainCharacterKey = mainCharacterKey;
            Entries = Array.AsReadOnly((PlayerLoadoutEntry[])(entries?.Clone() ?? Array.Empty<PlayerLoadoutEntry>()));
            StartingHandSize = startingHandSize;
            StartingRunCurrency = startingRunCurrency;
        }

        public void Validate()
        {
            if (LoadoutKey < 1)
                throw new InvalidOperationException("[PlayerLoadoutData] LoadoutKey must be positive.");
            if (MainCharacterKey == EntityKey.None)
                throw new InvalidOperationException($"[PlayerLoadoutData] Main character is missing. LoadoutKey: {LoadoutKey}");
            if (StartingHandSize < 1)
                throw new InvalidOperationException($"[PlayerLoadoutData] StartingHandSize must be at least 1 (main character). LoadoutKey: {LoadoutKey}");
            if (StartingRunCurrency < 0)
                throw new InvalidOperationException($"[PlayerLoadoutData] StartingRunCurrency must not be negative. LoadoutKey: {LoadoutKey}");

            var keys = new HashSet<EntityKey>();
            foreach (PlayerLoadoutEntry entry in Entries)
            {
                if (entry.UnitKey == EntityKey.None)
                    throw new InvalidOperationException($"[PlayerLoadoutData] Entry unit is missing. LoadoutKey: {LoadoutKey}");
                if (entry.Count < 1)
                    throw new InvalidOperationException($"[PlayerLoadoutData] Entry count must be at least 1. LoadoutKey: {LoadoutKey}");
                // Core RunCharacterDeckSpec과 같이 같은 유닛은 한 줄로 적습니다.
                if (!keys.Add(entry.UnitKey))
                    throw new InvalidOperationException($"[PlayerLoadoutData] Duplicate entry unit {entry.UnitKey}. LoadoutKey: {LoadoutKey}");
            }
        }

        /// <summary>
        /// 초기 덱 선택 번호의 모양만 검사합니다(E2 인계 §3). 번호는 펼친 지급 순서(메인 먼저, 그다음 항목을 Count만큼)를 가리킵니다.
        /// DB 표 생성 때만 부릅니다. 새 Run 시작의 <see cref="Validate"/>에 넣으면 Core가 돌려주는 구체 덱 사유보다 예외가 먼저 납니다.
        /// 덱 규칙(메인 하나·최대 장수·메인 0번)은 Core <c>DeckRules</c>가 원본이라 여기서 다시 검사하지 않습니다.
        /// </summary>
        public void ValidateInitialDeckIndices()
        {
            if (InitialDeckGrantIndices.Count == 0)
                throw new InvalidOperationException($"[PlayerLoadoutData] InitialDeckGrantIndices must not be empty. LoadoutKey: {LoadoutKey}");

            int grantCount = 1;
            foreach (PlayerLoadoutEntry entry in Entries)
                grantCount += entry.Count;

            var seen = new HashSet<int>();
            foreach (int index in InitialDeckGrantIndices)
            {
                if (index < 0 || index >= grantCount)
                    throw new InvalidOperationException(
                        $"[PlayerLoadoutData] Initial deck index {index} is outside the expanded grant range [0, {grantCount}). LoadoutKey: {LoadoutKey}");
                if (!seen.Add(index))
                    throw new InvalidOperationException($"[PlayerLoadoutData] Duplicate initial deck index {index}. LoadoutKey: {LoadoutKey}");
            }
        }
    }
}
