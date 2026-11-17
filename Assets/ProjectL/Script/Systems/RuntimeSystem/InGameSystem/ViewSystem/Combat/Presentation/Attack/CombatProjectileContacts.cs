using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 직선 탄 하나가 닿는 대상 하나입니다(관통·죽음의 손 View 설계 §3.1). 대상은 Core 사실이고, 진행률(탄 경로 0 = 출발 ~ 1 = 끝 위에서
    /// 그 대상 몸에 처음 닿는 지점)은 View가 잽니다. 대상이 비어 있으면 "대상 없는 끝"(남은 대상을 모두 해제할 순간)입니다.
    /// </summary>
    internal readonly struct CombatShotContact
    {
        internal CombatShotContact(EntityId target, float progress)
        {
            if (!float.IsFinite(progress))
                throw new ArgumentOutOfRangeException(nameof(progress), progress, "[CombatShotContact] 접촉 진행률이 유한하지 않습니다.");
            Target = target;
            Progress = Mathf.Clamp01(progress);
        }

        /// <summary>닿는 대상입니다. 비어 있으면 대상 없는 끝입니다.</summary>
        internal EntityId Target { get; }

        /// <summary>탄 경로 위 정규화 진행률(0~1)입니다.</summary>
        internal float Progress { get; }
    }

    /// <summary>
    /// 직선 탄의 접촉 목록과 관통 소실점의 규칙 한 곳입니다(관통·죽음의 손 View 설계 §3.1, 계획 TP-2·TP-4).
    /// 코드 모션 직선(<see cref="CombatLinearAttackAnimation"/>), 클립 출처 발사(<see cref="CombatLinearStyle.FireShot"/>),
    /// 궤적 미리보기(<c>CombatAttackTrajectory</c>)가 같은 함수를 씁니다. 조우·감쇠·멈춤은 계산하지 않습니다(Core 값 그대로).
    /// </summary>
    internal static class CombatProjectileContacts
    {
        /// <summary>
        /// 탄 하나의 접촉 목록을 진행률 오름차순으로 만듭니다(같으면 Core 순서). 관통이면 조우마다 하나(Core 순서),
        /// 아니면 접촉 대상(주 대상, 대신 맞기면 수호자) 하나, 둘 다 없으면 대상 없는 끝 하나(진행률 1)입니다. 목록은 늘 하나 이상입니다.
        /// </summary>
        /// <param name="piercingShot">Core 관통 기록입니다. 없으면 null입니다.</param>
        /// <param name="contactTarget">관통이 아닐 때 몸 접촉을 잴 대상입니다(<see cref="CombatAttackShape.ContactTargetId"/>). 없으면 비어 있습니다.</param>
        /// <param name="contactCell">관통이 아닐 때 접촉 대상의 Core 칸(몸과 경로가 닿지 않을 때 대신 투영)입니다.</param>
        /// <param name="progressOf">(대상, Core 칸)의 몸 접촉 진행률입니다. 몸 조회·대체 경고는 부르는 쪽이 맡습니다.</param>
        internal static IReadOnlyList<CombatShotContact> Collect(PiercingShotDelivery piercingShot, EntityId contactTarget,
            CellCoord? contactCell, Func<EntityId, CellCoord?, float> progressOf)
        {
            if (progressOf == null) throw new ArgumentNullException(nameof(progressOf));
            var contacts = new List<CombatShotContact>();
            if (piercingShot != null)
            {
                ReadOnlySpan<PiercingShotEncounter> encounters = piercingShot.Encounters.Span;
                for (int i = 0; i < encounters.Length; i++)
                    Insert(contacts, new CombatShotContact(encounters[i].TargetId, progressOf(encounters[i].TargetId, encounters[i].Cell)));
            }
            else if (!contactTarget.IsEmpty)
                contacts.Add(new CombatShotContact(contactTarget, progressOf(contactTarget, contactCell)));
            if (contacts.Count == 0)
                contacts.Add(new CombatShotContact(EntityId.Empty, 1f));
            return contacts;
        }

        /// <summary>
        /// 관통 탄이 사라지는 점(월드, 필드 바닥 높이)입니다(설계 D1·§3.1, 계획 TP-4). Endpoint 칸 가운데이고, 벽에서 멈췄으면 벽 칸 몸에 처음 닿는 곳입니다
        /// (궤적 막힘 선과 같은 접촉 기하). 발사 높이는 이 점에 더하지 않습니다(직선은 부르는 쪽이 <see cref="CombatShotPath.Raise"/>).
        /// </summary>
        /// <param name="origin">탄 경로의 출발 기준점(월드)입니다. 높이는 보지 않습니다.</param>
        internal static Vector3 VanishPoint(PiercingShotDelivery piercingShot, FieldGridMapper mapper, Vector3 origin)
        {
            if (piercingShot == null) throw new ArgumentNullException(nameof(piercingShot));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            Vector3 end = mapper.CellCenterToWorld(piercingShot.Endpoint);
            if (piercingShot.StopReason != PiercingShotStopReason.Wall)
                return end;

            // 출발점을 끝점 높이로 내려 바닥 위에서 자릅니다. 벽 몸과 닿지 않으면(기하 오차) 끝점 그대로입니다.
            Vector3 up = mapper.WorldUp.normalized;
            Vector3 flatOrigin = origin - up * Vector3.Dot(origin - end, up);
            Vector2 from = CombatBody.Flatten(mapper.WorldToLocal(flatOrigin));
            Vector2 to = CombatBody.Flatten(mapper.WorldToLocal(end));
            return CombatContactGeometry.TrySegmentContact(from, to, Vector2.zero, mapper.CellBody(piercingShot.Endpoint), out float time)
                ? Vector3.Lerp(flatOrigin, end, time)
                : end;
        }

        // 진행률 오름차순 삽입입니다. 같은 진행률은 먼저 들어온 것(Core 순서)이 앞입니다.
        private static void Insert(List<CombatShotContact> contacts, CombatShotContact contact)
        {
            int index = contacts.Count;
            while (index > 0 && contacts[index - 1].Progress > contact.Progress)
                index--;
            contacts.Insert(index, contact);
        }
    }
}
