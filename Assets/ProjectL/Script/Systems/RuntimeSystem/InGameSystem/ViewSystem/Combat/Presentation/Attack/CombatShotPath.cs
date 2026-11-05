using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 실제 발사와 궤적 미리보기가 함께 쓰는 발사 기하입니다(공격 궤적 설계 TJ3).
    /// 곡선 식·곡사 높이·발사 높이·직선 출발 오프셋의 원본은 이곳 한 곳입니다. 사거리·피격·차단은 판정하지 않습니다.
    /// 외형별 목표점 선택은 공격 종류 전략(<see cref="CombatAttackStyle.TryTarget"/>)으로 옮겼습니다(View 3라운드 W8). 여기에는 종류와 무관한 기하만 있습니다.
    /// </summary>
    internal static class CombatShotPath
    {
        // 원본 0.2 출발 오프셋입니다. 가까운 목표를 넘어서지 않는 표시 거리로만 씁니다.
        private const float LinearStartOffset = .2f;

        /// <summary>
        /// <paramref name="from"/> → <paramref name="to"/> 곡선 위 진행률 <paramref name="t"/>의 점입니다. 높이가 0이면 직선입니다.
        /// 연산 순서는 옛 발사체 풀과 같아 같은 값을 냅니다.
        /// </summary>
        /// <param name="up">곡사 변위 방향입니다. 호출자가 정규화해 넘깁니다.</param>
        /// <param name="height">곡선 꼭대기 높이(월드 길이)입니다.</param>
        internal static Vector3 ArcPoint(Vector3 from, Vector3 to, Vector3 up, float height, float t)
        {
            Vector3 position = Vector3.Lerp(from, to, t);
            position += up * (height * (4f * t * (1f - t)));
            return position;
        }

        /// <summary>ArcPoint와 같은 진행률의 접선입니다. 프레임 간 이동량이나 완급 변화에 의존하지 않습니다.</summary>
        internal static Vector3 ArcTangent(Vector3 from, Vector3 to, Vector3 up, float height, float t) =>
            to - from + up * (height * (4f * (1f - 2f * t)));

        /// <summary>곡사 높이(월드 길이) = 필드 셀 폭 × 외형의 곡사 높이(셀 단위)입니다.</summary>
        internal static float ArcHeight(FieldGridMapper mapper, CombatAttackPresentationData settings)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            return mapper.WorldCellWidth * settings.ArcHeightInCells;
        }

        /// <summary><paramref name="up"/> 방향으로 <paramref name="height"/>만큼 올린 점입니다(발사 높이).</summary>
        internal static Vector3 Raise(Vector3 point, Vector3 up, float height) => point + up * height;

        /// <summary>
        /// 직선 발사의 출발점입니다. 올린 출발점에서 올린 종점 쪽으로 최대 0.2만큼 옮기되 종점을 넘지 않습니다.
        /// 두 점이 사실상 같으면 출발점 그대로입니다.
        /// </summary>
        internal static Vector3 LinearStart(Vector3 raisedFrom, Vector3 raisedTo)
        {
            float distance = Vector3.Distance(raisedFrom, raisedTo);
            if (distance > .000001f) raisedFrom += (raisedTo - raisedFrom) / distance * Mathf.Min(LinearStartOffset, distance);
            return raisedFrom;
        }
    }
}
