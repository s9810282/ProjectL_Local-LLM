using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 정적 지형 원본 바뀜 사건(<see cref="CombatMovementSourceEvent"/>)의 갈래 판정 한 곳입니다(이펙트 창구 확장 계획 P11, 소리 창구 2차 검토 L4).
    /// Core는 같은 사건 형식을 두 뜻으로 냅니다. 종류가 바뀐 원본(약한 지반 → 낭떠러지 등)과, 종류가 그대로인 진입 기록(약한 지반 진입 수, 약한 지반 설계 WG1)입니다.
    /// 사실 처리기(칸 외형 바꾸기)·동시 묶음(진입 기록을 앞 이동 묶음에 붙이기)·사건 형식 표의 순간 칸(붕괴 소리·큐)이 이 판정을 부릅니다.
    /// </summary>
    internal static class CombatTerrainChanges
    {
        /// <summary>정적 지형 원본의 종류가 바뀐 사건이면 바뀌기 전·뒤 사실을 줍니다. 칸 외형을 바꿀 사건입니다.</summary>
        internal static bool TryGetKindChange(CombatEvent e, out StaticTerrainFact before, out StaticTerrainFact after)
        {
            if (e is CombatMovementSourceEvent change && change.Expected is StaticTerrainFact expected &&
                change.Final is StaticTerrainFact final && expected.Kind != final.Kind)
            {
                before = expected;
                after = final;
                return true;
            }
            before = null;
            after = null;
            return false;
        }

        /// <summary>정적 지형 원본의 종류가 그대로인 사건(진입 기록)인지입니다. 칸 외형은 같고, 바로 앞 이동의 기록입니다.</summary>
        internal static bool IsSameKindEntry(CombatEvent e) =>
            e is CombatMovementSourceEvent change && change.Expected is StaticTerrainFact before &&
            change.Final is StaticTerrainFact after && before.Kind == after.Kind;

        /// <summary>약한 지반이 무너져 낭떠러지가 된 사건이면 그 칸을 줍니다. 붕괴 소리·큐는 이 갈래에만 냅니다(다른 종류 바뀜에는 내지 않음).</summary>
        internal static bool IsWeakGroundCollapse(CombatEvent e, out CellCoord cell)
        {
            if (TryGetKindChange(e, out StaticTerrainFact before, out StaticTerrainFact after) &&
                before.Kind == StaticTerrainKind.WeakGround && after.Kind == StaticTerrainKind.Cliff)
            {
                cell = after.Cell;
                return true;
            }
            cell = default;
            return false;
        }
    }
}
