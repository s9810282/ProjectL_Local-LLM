#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>
    /// 개발 진입 전용입니다. 전투 스테이지 입력 SO를 참조하는 스테이지 루트를 DB 카탈로그에서 찾아 그 루트의 순수 Data를 돌려줍니다.
    /// </summary>
    /// <remarks>
    /// 전투 스테이지 SO에는 키가 없고 키·맵·역할은 그것을 참조하는 루트가 가집니다. GetData는 매번 새 Data를 만들므로
    /// 어떤 SO인지는 SO 참조로만 맞출 수 있습니다(개발 진입 지정 스테이지 배치 설계 DS2).
    /// 보스 등 역할 검사는 하지 않습니다. 놓을 수 있는 스테이지인지는 Core 그래프 생성이 판정합니다(설계 DS4).
    /// </remarks>
    internal static class DevStageRootLookup
    {
        /// <summary>
        /// <paramref name="stage"/>를 참조하는 스테이지 루트를 찾습니다. 없거나 둘 이상이면 예외로 시작을 멈춥니다.
        /// </summary>
        internal static StageRootData Find(DatabaseCatalogSO catalog, CombatStageData stage)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (stage == null) throw new ArgumentNullException(nameof(stage));

            var matches = new List<StageRootDataSO>();
            foreach (DatabaseGroup group in catalog.GetAllData().OfType<DatabaseGroup>())
                foreach (StageRootDataTableSO table in group.GetAllData().OfType<StageRootDataTableSO>())
                    foreach (StageRootDataSO root in table.GetAllData().OfType<StageRootDataSO>())
                        if (ReferenceEquals(root.StageDataInput, stage))
                            matches.Add(root);

            if (matches.Count == 0)
                throw new InvalidOperationException(
                    $"[DevStageRootLookup] DB 카탈로그의 스테이지 루트 표에 등록되지 않은 CombatStageData입니다: {stage.name}");
            if (matches.Count > 1)
                throw new InvalidOperationException(
                    $"[DevStageRootLookup] 여러 스테이지 루트가 같은 CombatStageData({stage.name})를 참조합니다: " +
                    string.Join(", ", matches.Select(root => $"{root.name}({((StageRootData)root.GetData()).StageKey})")));
            return (StageRootData)matches[0].GetData();
        }
    }
}
#endif
