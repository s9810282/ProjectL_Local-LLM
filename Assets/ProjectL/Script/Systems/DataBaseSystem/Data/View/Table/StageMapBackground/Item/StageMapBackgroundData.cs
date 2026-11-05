using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMapBackground.Item
{
    /// <summary>챕터별 배경 프리팹의 읽기 전용 설정입니다. 소품 배치는 프리팹이 소유합니다.</summary>
    internal sealed class StageMapBackgroundData : IData
    {
        public int ChapterKey { get; }
        public GameObject BackgroundPrefab { get; }

        internal StageMapBackgroundData(int chapterKey, GameObject backgroundPrefab)
        {
            ChapterKey = chapterKey;
            BackgroundPrefab = backgroundPrefab;
        }

        /// <summary>배경 선택 키와 원본 프리팹의 루트 구성을 검사합니다.</summary>
        internal void Validate()
        {
            if (ChapterKey < 1)
            {
                throw new InvalidOperationException($"[StageMapBackgroundData] 챕터 {ChapterKey}: ChapterKey는 양수여야 합니다.");
            }

            string owner = $"[StageMapBackgroundData] 챕터 {ChapterKey} {nameof(BackgroundPrefab)}";
            ViewPrefabRules.RequirePrefabAsset(BackgroundPrefab, owner);
            ViewPrefabRules.RequireActiveRoot(BackgroundPrefab, owner);
            ViewPrefabRules.RequireIdentityRoot(BackgroundPrefab, owner);
        }
    }
}
