using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 전투 환경 사실을 View가 어떻게 보는지의 공용 판단입니다(위험 범위 계획 P4). 오브젝트·지형·흔적 표시, 환경 설명, 위험 범위 찾기, 먼지 연막 호버가 함께 부릅니다.
    /// 상태를 갖지 않고 할당하지 않습니다.
    /// </summary>
    internal static class CombatEnvironmentFacts
    {
        /// <summary>플레이어에게 알려진 원본인지입니다. 모르는 원본은 그리지도 설명하지도 않습니다.</summary>
        internal static bool IsKnownToPlayer(MovementTileFact source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            // 포인터마다 불릴 수 있어 인터페이스 열거자를 만들지 않습니다.
            IReadOnlyList<Owner> knownTo = source.KnownTo;
            for (int i = 0; i < knownTo.Count; i++)
                if (knownTo[i] == Owner.Player) return true;
            return false;
        }

        /// <summary>
        /// 원본을 보여 줄지입니다: 켜짐 · 플레이어 공개. 표시(오브젝트·지형·흔적)와 입력(환경 설명·위험 범위·먼지 연막 호버)이 이 판단 하나를 씁니다(평가 C1-05).
        /// 분류(지형·오브젝트)와 외형 유무는 부르는 쪽이 따로 봅니다.
        /// </summary>
        internal static bool IsShown(MovementTileFact source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Active && IsKnownToPlayer(source);
        }

        /// <summary>구조물이 장애물로 분류됐으면 그 장애물 프로필을 찾습니다. 분류 밖이거나 프로필이 없으면 false입니다.</summary>
        internal static bool TryGetObstacle(EnvironmentSemanticBindings bindings, IReadOnlyList<ObstacleContentProfile> obstacles,
            StructureCombatFacts structure, out ObstacleContentProfile profile)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
            if (structure == null) throw new ArgumentNullException(nameof(structure));

            profile = null;
            bool classified = false;
            IReadOnlyList<EntityId> ids = bindings.ObstacleEntityIds;
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == structure.EntityId) { classified = true; break; }
            if (!classified) return false;
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].EntityId != structure.EntityId) continue;
                profile = obstacles[i];
                return true;
            }
            return false;
        }
    }
}
