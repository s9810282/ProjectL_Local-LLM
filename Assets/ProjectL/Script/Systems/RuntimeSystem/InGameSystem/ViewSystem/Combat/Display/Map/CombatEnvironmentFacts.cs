using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 전투 환경 사실을 View가 어떻게 보는지의 공용 판단입니다(위험 범위 계획 P4). 오브젝트·지형·흔적 표시, 환경 설명, 위험 범위 찾기, 먼지 연막 호버가 함께 부릅니다.
    /// 맡는 판단(평가 C1-05, 작은 묶음 설계 SB3): 보임(<see cref="IsShown"/>), 분류 소속(<see cref="IsTerrain"/>·<see cref="IsObject"/>),
    /// Object 분류·형식(<see cref="TryGetObject"/>, 어긋나면 예외 하나), 장애물 프로필(<see cref="TryGetObstacle"/>)과 그 칸을 점유한 살아 있는 장애물(<see cref="TryGetObstacleAt"/>),
    /// 먼지 연막 흔적(<see cref="IsDustScreen"/>). 분류 정책이 바뀌면 여기 한 곳만 고칩니다.
    /// 상태를 갖지 않고 할당하지 않습니다(목록은 인덱스로 돎).
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

        /// <summary>
        /// 구조물이 살아 있고 <paramref name="cell"/>을 점유한 장애물이면 그 장애물 프로필을 찾습니다(환경 설명·위험 범위가 함께 씀).
        /// 판단 순서는 생존 → 점유 칸 → 분류·프로필(<see cref="TryGetObstacle"/>)이고, 분류 인자 검사도 그 함수가 합니다.
        /// </summary>
        internal static bool TryGetObstacleAt(EnvironmentSemanticBindings bindings, IReadOnlyList<ObstacleContentProfile> obstacles,
            StructureCombatFacts structure, CellCoord cell, out ObstacleContentProfile profile)
        {
            if (structure == null) throw new ArgumentNullException(nameof(structure));
            profile = null;
            if (structure.Health.IsDead || !Occupies(structure, cell)) return false;
            return TryGetObstacle(bindings, obstacles, structure, out profile);
        }

        /// <summary>원본이 Terrain으로 분류됐는지입니다(<see cref="EnvironmentSemanticBindings.TerrainSourceIds"/>). 원본 검사(사실의 Id)와 회전 검사(Source Id)가 함께 씁니다.</summary>
        internal static bool IsTerrain(EnvironmentSemanticBindings bindings, long sourceId)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            return Contains(bindings.TerrainSourceIds, sourceId);
        }

        /// <summary>원본이 Object로 분류됐는지입니다(<see cref="EnvironmentSemanticBindings.ObjectSourceIds"/>). 원본 검사(사실의 Id)와 회전 검사(Source Id)가 함께 씁니다.</summary>
        internal static bool IsObject(EnvironmentSemanticBindings bindings, long sourceId)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            return Contains(bindings.ObjectSourceIds, sourceId);
        }

        /// <summary>
        /// 원본이 Object로 분류됐으면 Object 사실로 돌려줍니다. 분류 밖이면 false입니다. 보임은 부르는 쪽이 <see cref="IsShown"/>으로 따로 봅니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">Object로 분류됐는데 원본 형식이 Object 사실이 아닐 때입니다(분류 계약 오류, 문구 하나 — 결정 Q-SB3 A).</exception>
        internal static bool TryGetObject(EnvironmentSemanticBindings bindings, MovementTileFact source, out ObjectSourceFact item)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            if (source == null) throw new ArgumentNullException(nameof(source));
            item = null;
            if (!Contains(bindings.ObjectSourceIds, source.Id)) return false;
            item = source as ObjectSourceFact
                ?? throw new InvalidOperationException($"[CombatEnvironmentFacts] Object 분류와 Buffer 원본 형식이 다릅니다: {source.Id}.");
            return true;
        }

        /// <summary>
        /// 원본이 먼지 연막 흔적(특성 흔적 + 먼지 연막 특성 키)인지입니다. 보임은 부르는 쪽이 <see cref="IsShown"/>으로 따로 봅니다(설계 P11).
        /// 형식 판정이라 null이면 false입니다(옛 형식 패턴과 같음).
        /// </summary>
        internal static bool IsDustScreen(MovementTileFact source, out TraitTrailFact trail)
        {
            trail = source as TraitTrailFact;
            if (trail != null && trail.Key == TraitKey.DustScreen) return true;
            trail = null;
            return false;
        }

        private static bool Contains(IReadOnlyList<long> ids, long id)
        {
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] == id) return true;
            return false;
        }

        private static bool Occupies(StructureCombatFacts structure, CellCoord cell)
        {
            IReadOnlyList<CellCoord> cells = structure.OccupiedCells;
            for (int i = 0; i < cells.Count; i++)
                if (cells[i] == cell) return true;
            return false;
        }
    }
}
