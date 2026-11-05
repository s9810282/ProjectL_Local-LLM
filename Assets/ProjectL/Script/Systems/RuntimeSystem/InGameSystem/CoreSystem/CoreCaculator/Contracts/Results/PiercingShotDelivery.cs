using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>조준 정보와 별개인 관통 사격의 실제 이동 및 피격 순서다.</summary>
    public sealed class PiercingShotDelivery
    {
        public EffectOriginKey Origin { get; }
        public CellCoord OriginCell { get; }
        public CellCoord AimCell { get; }
        public CellCoord Step { get; }
        public ReadOnlyMemory<CellCoord> TraveledCells { get; }
        public ReadOnlyMemory<PiercingShotEncounter> Encounters { get; }
        public CellCoord Endpoint { get; }
        public PiercingShotStopReason StopReason { get; }

        public PiercingShotDelivery(EffectOriginKey origin, CellCoord originCell, CellCoord aimCell,
            CellCoord step, CellCoord[] traveledCells, PiercingShotEncounter[] encounters,
            CellCoord endpoint, PiercingShotStopReason stopReason)
        {
            if (!origin.IsValid) throw new ArgumentException("유효한 효과 출처가 필요합니다.", nameof(origin));
            if (step.X < -1 || step.X > 1 || step.Y < -1 || step.Y > 1 || step == CellCoord.Zero)
                throw new ArgumentOutOfRangeException(nameof(step));
            if (!Enum.IsDefined(typeof(PiercingShotStopReason), stopReason))
                throw new ArgumentOutOfRangeException(nameof(stopReason));
            if (traveledCells == null) throw new ArgumentNullException(nameof(traveledCells));
            if (encounters == null) throw new ArgumentNullException(nameof(encounters));
            if (originCell.X < 0 || originCell.Y < 0 || aimCell.X < 0 || aimCell.Y < 0 ||
                endpoint.X < 0 || endpoint.Y < 0)
                throw new ArgumentException("사격 셀 좌표는 음수일 수 없습니다.");

            // 뺄셈과 다음 셀 계산은 확장 정수로 수행해 좌표 오버플로를 방지한다.
            long dx = (long)aimCell.X - originCell.X;
            long dy = (long)aimCell.Y - originCell.Y;
            long distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (distance == 0 || dx != distance * step.X || dy != distance * step.Y)
                throw new ArgumentException("조준 셀은 진행 방향의 직선 위에 있어야 합니다.", nameof(aimCell));

            for (int i = 0; i < traveledCells.Length; i++)
            {
                CellCoord cell = traveledCells[i];
                if (cell.X < 0 || cell.Y < 0 ||
                    cell.X != originCell.X + ((long)i + 1) * step.X ||
                    cell.Y != originCell.Y + ((long)i + 1) * step.Y)
                    throw new ArgumentException("이동 셀은 출발점 다음부터 순서대로 연속되어야 합니다.", nameof(traveledCells));
            }

            var ids = new HashSet<EntityId>();
            long previousDistance = 0;
            for (int i = 0; i < encounters.Length; i++)
            {
                PiercingShotEncounter encounter = encounters[i];
                long encounterDistance = Math.Max(Math.Abs((long)encounter.Cell.X - originCell.X),
                    Math.Abs((long)encounter.Cell.Y - originCell.Y));
                if (encounter.TargetId.IsEmpty || !ids.Add(encounter.TargetId) || encounter.EncounterIndex != i ||
                    encounter.RemainingPercent < 1 || encounter.RemainingPercent > 100 ||
                    encounterDistance <= previousDistance || encounterDistance > traveledCells.Length ||
                    traveledCells[(int)encounterDistance - 1] != encounter.Cell)
                    throw new ArgumentException("피격은 중복 없이 이동 순서를 따라야 합니다.", nameof(encounters));
                previousDistance = encounterDistance;
            }

            if (traveledCells.Length == 0)
            {
                if (encounters.Length != 0 || endpoint != originCell || stopReason != PiercingShotStopReason.TargetBudget)
                    throw new ArgumentException("빈 경로는 출발점에서 소진된 관통 예산만 나타냅니다.");
            }
            else
            {
                CellCoord last = traveledCells[traveledCells.Length - 1];
                bool validEndpoint = stopReason == PiercingShotStopReason.Wall
                    ? endpoint.X == (long)last.X + step.X && endpoint.Y == (long)last.Y + step.Y
                    : endpoint == last;
                if (!validEndpoint)
                    throw new ArgumentException("종점이 이동 경로 및 종료 사유와 일치하지 않습니다.", nameof(endpoint));
                if (stopReason == PiercingShotStopReason.TargetBudget &&
                    (encounters.Length == 0 || encounters[encounters.Length - 1].Cell != endpoint))
                    throw new ArgumentException("소진된 관통 예산의 종점은 마지막 피격 셀이어야 합니다.", nameof(endpoint));
            }

            Origin = origin;
            OriginCell = originCell;
            AimCell = aimCell;
            Step = step;
            TraveledCells = (CellCoord[])traveledCells.Clone();
            Encounters = (PiercingShotEncounter[])encounters.Clone();
            Endpoint = endpoint;
            StopReason = stopReason;
        }

        // 결과와 프리뷰는 같은 대응 규칙을 사용하되 기존 피해 배열의 정렬은 강제하지 않는다.
        internal static void ValidateCorrespondence<T>(PiercingShotDelivery delivery, EntityId attackerId,
            EntityId primaryTargetId, CellCoord? primaryCell, ReadOnlyMemory<CellCoord> additionalCells,
            CellCoord[] affectedCells, T[] hits, Func<T, EntityId> targetId, Func<T, AttackTargetRole> role, Func<T, IReadOnlyList<AttackHitContactSnapshot>> contacts = null)
        {
            if (contacts != null && hits.Any(h => contacts(h) != null))
            {
                if (hits.Any(h => contacts(h) == null)) throw new ArgumentException("신규 접촉과 옛 피격을 혼합할 수 없습니다.", nameof(hits));
                ValidateCorrespondence(delivery, attackerId, primaryTargetId, primaryCell, additionalCells, affectedCells,
                    hits.SelectMany(contacts).ToArray(), c => c.OriginalTargetId, c => c.Role);
                return;
            }
            if (delivery == null)
            {
                foreach (T hit in hits)
                    if (role(hit) == AttackTargetRole.Effect)
                        throw new ArgumentException("효과 피격에는 관통 사격 기록이 필요합니다.", nameof(hits));
                return;
            }

            if (!primaryCell.HasValue || primaryCell.Value != delivery.AimCell || !additionalCells.IsEmpty)
                throw new ArgumentException("관통 기록은 추가 셀 없는 단일 조준과 일치해야 합니다.", nameof(delivery));
            if ((delivery.Origin.Kind == EffectOwnerKind.Trait || delivery.Origin.Kind == EffectOwnerKind.Category) &&
                delivery.Origin.OwnerInstanceId != attackerId.Value)
                throw new ArgumentException("유닛 효과의 출처는 공격자와 일치해야 합니다.", nameof(delivery));
            if (!affectedCells.AsSpan().SequenceEqual(delivery.TraveledCells.Span))
                throw new ArgumentException("영향 셀은 실제 이동 경로와 일치해야 합니다.", nameof(affectedCells));
            if (hits.Length != delivery.Encounters.Length)
                throw new ArgumentException("피해 목록과 관통 피격 수가 일치해야 합니다.", nameof(hits));

            var remainingIds = new HashSet<EntityId>();
            foreach (PiercingShotEncounter encounter in delivery.Encounters.Span)
                remainingIds.Add(encounter.TargetId);
            foreach (T hit in hits)
            {
                EntityId id = targetId(hit);
                AttackTargetRole expectedRole = id == primaryTargetId ? AttackTargetRole.Primary : AttackTargetRole.Effect;
                if (!remainingIds.Remove(id) || role(hit) != expectedRole)
                    throw new ArgumentException("피해 목록은 관통 피격과 선택 대상의 역할에 일치해야 합니다.", nameof(hits));
            }
        }
    }
}
