#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>
    /// 개발 진입 전용 전투 정의 소스입니다. 요청한 노드의 스테이지 대신 지정한 스테이지 키의 전투 정의를
    /// 운영과 같은 DB 소스(<see cref="DatabaseBattleInitializationSource"/>)로 읽습니다.
    /// </summary>
    /// <remarks>
    /// 챕터 맵은 Run마다 새로 만들어지고 노드마다 일반 스테이지를 무작위로 뽑으므로, 특정 일반 스테이지의 노드로 바로 갈 수 없습니다.
    /// 그래서 이어진 노드로 진입하되 전투 정의만 지정 스테이지로 바꿉니다(토네이도 기획 정합 계획 G3.0, 보스 스테이지 소스와 같은 방식).
    /// 맵 진행은 진입한 노드로 처리됩니다. 지정 키가 현재 챕터의 전투 스테이지가 아니면 운영 DB 소스의 검사가 거절하고,
    /// 서비스 바인딩 전 조회도 운영 DB 소스가 거절합니다.
    /// 프레임워크가 이 소스를 모듈로 묶지 않으므로, 서비스 바인딩은 조립한 쪽(GameBootstrap)이 <see cref="Bind"/>로 한 번 합니다.
    /// </remarks>
    internal sealed class DevSelectedStageBattleSource : IBattleInitializationSource
    {
        private readonly DatabaseBattleInitializationSource _database = new();
        private readonly StageKey _selected;

        /// <param name="stageKey">읽을 전투 스테이지 키입니다. 1 이상이어야 합니다.</param>
        internal DevSelectedStageBattleSource(int stageKey)
        {
            if (stageKey < 1)
                throw new ArgumentOutOfRangeException(nameof(stageKey), stageKey,
                    "[DevSelectedStageBattleSource] 지정 스테이지 키는 1 이상이어야 합니다. " +
                    "Play From Boot 설정(또는 Boot 씬 GameBootstrap)의 Dev Stage Key에 전투 스테이지 키를 넣으세요.");
            _selected = new StageKey(stageKey);
        }

        /// <summary>런타임 서비스 바인딩 뒤 한 번 부릅니다. 운영 DB 소스의 바인딩을 합니다.</summary>
        internal void Bind(ISystemServiceStoreReader services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            _database.PostBindService(services);
        }

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            Debug.Log($"[DevSelectedStageBattleSource] 개발 진입: 요청 스테이지 {stageKey.Value} 대신 지정 스테이지 {_selected.Value}의 정의를 읽습니다.");
            return _database.Read(database, chapterKey, _selected);
        }
    }
}
#endif
