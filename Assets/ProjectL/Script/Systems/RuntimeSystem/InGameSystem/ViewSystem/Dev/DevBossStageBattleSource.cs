#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.Runtime;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>
    /// 개발 진입 전용 전투 정의 소스입니다. 요청한 노드의 스테이지 대신 현재 맵의 보스 전투 스테이지 정의를
    /// 운영과 같은 DB 소스(<see cref="DatabaseBattleInitializationSource"/>)로 읽습니다.
    /// </summary>
    /// <remarks>
    /// Core는 현재 노드와 이어진 노드로만 진입을 허락하므로(NotConnected), 보스 노드로 바로 갈 수 없습니다.
    /// 그래서 이어진 노드로 진입하되 전투 정의만 보스 스테이지로 바꿉니다(보스 활성화 계획 §6.4, 사용자 결정).
    /// 맵 진행은 진입한 노드로 처리되므로 보스 승리 뒤의 챕터 완료·Run 종료 흐름은 확인하지 못합니다.
    /// 보스 스테이지는 Buffer에 게시된 현재 맵 노드 중 전투 스테이지이면서 맵 역할이 Boss인 것 하나로 고릅니다(키·이름으로 고르지 않음).
    /// 프레임워크가 이 소스를 모듈로 묶지 않으므로, 서비스 바인딩은 조립한 쪽(GameBootstrap)이 <see cref="Bind"/>로 한 번 합니다.
    /// </remarks>
    internal sealed class DevBossStageBattleSource : IBattleInitializationSource
    {
        private readonly DatabaseBattleInitializationSource _database = new();
        private ICurrentProgressBufferReadSink _current;
        private IStageProgressBufferReadSink _stages;

        /// <summary>런타임 서비스 바인딩 뒤 한 번 부릅니다. 운영 DB 소스의 바인딩도 함께 합니다.</summary>
        internal void Bind(ISystemServiceStoreReader services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            _database.PostBindService(services);
            IBufferQuery buffers = services.GetService(BufferSystemKey.Buffer);
            _current = buffers.CurrentProgressReadSink;
            _stages = buffers.StageProgressReadSink;
        }

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            if (_current == null || _stages == null)
                throw new InvalidOperationException("[DevBossStageBattleSource] 서비스 바인딩(Bind) 전에는 전투 정의를 읽을 수 없습니다.");

            StageKey boss = ResolveBossStage();
            Debug.Log($"[DevBossStageBattleSource] 개발 진입: 요청 스테이지 {stageKey.Value} 대신 보스 스테이지 {boss.Value}의 정의를 읽습니다.");
            return _database.Read(database, chapterKey, boss);
        }

        private StageKey ResolveBossStage()
        {
            int mapKey = _current.CurrentMapKey;
            int stageKey = 0;
            int count = 0;
            foreach (StageProgressData node in _stages.GetStages(mapKey))
            {
                if (node.StageType != StageType.Combat || node.MapRole != StageMapRole.Boss)
                    continue;
                stageKey = node.StageKey;
                count++;
            }

            if (count != 1)
                throw new InvalidOperationException(
                    $"[DevBossStageBattleSource] 맵 {mapKey}의 보스 전투 노드가 하나가 아닙니다(수: {count}).");
            return new StageKey(stageKey);
        }
    }
}
#endif
