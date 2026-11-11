using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// 보스가 지금 포효하면 어떻게 되는지의 예고 조회 결과다(포효 예고 설계 RF2). 포효 카운터는 보지 않고, 실제 포효와 같은 계산을
    /// 플레이어 시점 공개 미리보기로 돌린 답이다. 언제 보일지(카운터 조건)는 묻는 쪽이 정한다.
    /// </summary>
    /// <remarks>
    /// 범위 칸의 원본은 실제 포효 사건(<c>CombatRoarEvent.AreaCells</c>)과 같다. 대상이 있으면 공격 미리보기의 영향 칸이고,
    /// 대상이 없으면 반응 계산이 돌려준 빈 범위 칸이다(계획 TP-7). 넉백·대상 점유 칸은 <see cref="Attack"/>에 있다.
    /// </remarks>
    public readonly struct RoarForecastResult
    {
        public bool Succeeded { get; }
        public AttackFailureReason FailureReason { get; }
        public EntityId BossId { get; }
        /// <summary>포효 범위 칸이다. 실패면 비어 있다.</summary>
        public ReadOnlyMemory<CellCoord> AreaCells { get; }
        /// <summary>범위 안에 대상이 있을 때의 공격 미리보기다(넉백 기록·대상 점유 칸 포함). 대상이 없거나 실패면 null이다.</summary>
        public AttackPreview? Attack { get; }

        // 범위 칸은 이미 불변인 메모리만 받습니다(공격 미리보기의 영향 칸, 또는 팩터리가 한 번 복사한 배열). 여기서 다시 복사하지 않습니다.
        private RoarForecastResult(bool succeeded, AttackFailureReason failureReason, EntityId bossId, ReadOnlyMemory<CellCoord> areaCells,
            AttackPreview? attack)
        {
            if (bossId.IsEmpty)
                throw new ArgumentException("보스 아이디는 Empty일 수 없습니다.", nameof(bossId));

            Succeeded = succeeded;
            FailureReason = failureReason;
            BossId = bossId;
            AreaCells = areaCells;
            Attack = attack;
        }

        /// <summary>대상이 있는 예고다. 범위 칸은 공격 미리보기의 영향 칸을 그대로 가리킨다(같은 사실 한 벌).</summary>
        public static RoarForecastResult WithTargets(EntityId bossId, AttackPreview attack)
        {
            if (!attack.CanAttack || attack.AttackerId != bossId)
                throw new ArgumentException("포효 예고의 공격 미리보기는 그 보스의 성공 결과여야 합니다.", nameof(attack));
            return new RoarForecastResult(true, AttackFailureReason.None, bossId, attack.AffectedCells, attack);
        }

        /// <summary>범위 안에 대상이 없는 예고다. 범위 칸은 표시용으로 남긴다. 받은 배열은 한 번 복사한다.</summary>
        public static RoarForecastResult WithoutTargets(EntityId bossId, IReadOnlyList<CellCoord> areaCells)
        {
            if (areaCells == null)
                throw new ArgumentNullException(nameof(areaCells));
            var copied = new CellCoord[areaCells.Count];
            for (int i = 0; i < copied.Length; i++)
                copied[i] = areaCells[i];
            return new RoarForecastResult(true, AttackFailureReason.None, bossId, copied, null);
        }

        /// <summary>지금은 포효할 수 없다(전투 끝·밤 전투 아님·유닛 조건).</summary>
        public static RoarForecastResult Failure(AttackFailureReason failureReason, EntityId bossId)
        {
            if (failureReason == AttackFailureReason.None)
                throw new ArgumentException("실패 결과에는 실패 이유가 필요합니다.", nameof(failureReason));
            return new RoarForecastResult(false, failureReason, bossId, ReadOnlyMemory<CellCoord>.Empty, null);
        }
    }
}
