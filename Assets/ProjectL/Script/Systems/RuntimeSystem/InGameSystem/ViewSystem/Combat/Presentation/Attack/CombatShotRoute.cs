using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>스킬 사건 51의 대상 하나와 그 접촉 칸입니다(Core <c>ImpactCells</c>, 전투 스킬 View 설계 SK3).</summary>
    internal readonly struct CombatShotRouteContact
    {
        internal CombatShotRouteContact(EntityId target, CellCoord cell)
        {
            Target = target;
            Cell = cell;
        }

        /// <summary>실제 수신자입니다.</summary>
        internal EntityId Target { get; }

        /// <summary>탄이 그 대상에 닿는 칸입니다(늘 경로 안).</summary>
        internal CellCoord Cell { get; }
    }

    /// <summary>
    /// 스킬 사건 51의 꺾인 탄 경로와 접촉 목록입니다(전투 스킬 View 설계 SK3, 계획 d.3). 경로 점은 첫 점(발사 자리) 다음에 경로 칸 가운데를 발사 높이로 올린 점이고,
    /// 대상마다 그 접촉 칸까지의 누적 길이 비율이 접촉 진행률입니다. 같은 대상의 접촉이 여럿이면 마지막 접촉이 그 대상을 풉니다(해제 규칙 원본 <see cref="CombatContactRelease"/>).
    /// </summary>
    /// <remarks>
    /// 코드 모션 직선(<c>CombatLinearAttackAnimation</c>)과 클립 출처 발사(<c>CombatLinearStyle.FireShot</c>)가 같은 값으로 쏩니다. 조우·관통·멈춤은 Core 값 그대로이고
    /// 여기서 계산하지 않습니다. 경로가 없는 사건(범위 폭발)에는 만들지 않습니다(<see cref="For"/>가 null).
    /// </remarks>
    internal sealed class CombatShotRoute
    {
        private readonly IReadOnlyList<CellCoord> _cells;
        private readonly IReadOnlyList<CombatShotRouteContact> _routeContacts;
        private Vector3[] _points = Array.Empty<Vector3>();
        private float[] _lengths = Array.Empty<float>();

        private CombatShotRoute(IReadOnlyList<CellCoord> cells, IReadOnlyList<CombatShotRouteContact> contacts)
        {
            _cells = cells;
            _routeContacts = contacts;
        }

        /// <summary>그 모양의 꺾인 경로입니다. 스킬 경로가 없으면 null입니다.</summary>
        internal static CombatShotRoute For(CombatAttackShape shape) =>
            shape?.SkillPath == null || shape.SkillPath.Count == 0 ? null : new CombatShotRoute(shape.SkillPath, shape.SkillContacts);

        /// <summary>만든 경로 점입니다(<see cref="Build"/> 뒤). 첫 점은 발사 자리입니다.</summary>
        internal IReadOnlyList<Vector3> Points => _points;

        /// <summary>접촉 목록입니다(진행률 오름차순, 하나 이상 — 대상이 없으면 끝의 대상 없는 접촉 하나).</summary>
        internal IReadOnlyList<CombatShotContact> Contacts { get; private set; } = Array.Empty<CombatShotContact>();

        /// <summary><see cref="Contacts"/>와 같은 차례의 접촉 자리(위치·방향)입니다. 접촉 이펙트가 씁니다.</summary>
        internal CombatProjectileContact[] ContactPoints { get; private set; } = Array.Empty<CombatProjectileContact>();

        /// <summary>첫 경로 칸을 발사 높이로 올린 점입니다. 코드 모션의 첫 구간 출발 오프셋(<see cref="CombatShotPath.LinearStart"/>)에 씁니다.</summary>
        internal Vector3 FirstTarget(FieldGridMapper mapper, Vector3 up, float height)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            return CombatShotPath.Raise(mapper.CellCenterToWorld(_cells[0]), up, height);
        }

        /// <summary>발사 자리 <paramref name="start"/>에서 경로 점·접촉을 만듭니다. 발사 때 한 번 부릅니다.</summary>
        /// <param name="facing">수평 길이가 없는 구간의 접촉 자리 회전입니다(그 공격의 방향).</param>
        internal void Build(Vector3 start, FieldGridMapper mapper, Vector3 up, float height, Quaternion facing)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            int count = _cells.Count + 1;
            _points = new Vector3[count];
            _points[0] = start;
            for (int i = 0; i < _cells.Count; i++)
                _points[i + 1] = CombatShotPath.Raise(mapper.CellCenterToWorld(_cells[i]), up, height);
            _lengths = new float[count];
            float total = CombatShotPath.FillPathLengths(_points, count, _lengths);

            var contacts = new List<CombatShotContact>(_routeContacts.Count);
            var ends = new List<int>(_routeContacts.Count);
            for (int i = 0; i < _routeContacts.Count; i++)
            {
                int end = IndexOf(_routeContacts[i].Cell) + 1;
                if (end <= 0)
                    end = count - 1;
                float progress = total > 0f ? _lengths[end] / total : 1f;
                Insert(contacts, ends, new CombatShotContact(_routeContacts[i].Target, progress), end);
            }
            if (contacts.Count == 0)
            {
                contacts.Add(new CombatShotContact(EntityId.Empty, 1f));
                ends.Add(count - 1);
            }
            var points = new CombatProjectileContact[contacts.Count];
            for (int i = 0; i < points.Length; i++)
                points[i] = CombatProjectileContact.Along(_points[ends[i] - 1], _points[ends[i]], 1f, up, facing);
            Contacts = contacts;
            ContactPoints = points;
        }

        private int IndexOf(CellCoord cell)
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] == cell)
                    return i;
            }
            return -1;
        }

        // 진행률 오름차순 삽입입니다. 같은 진행률은 먼저 들어온 것(Core 접촉 순번)이 앞입니다.
        private static void Insert(List<CombatShotContact> contacts, List<int> ends, CombatShotContact contact, int end)
        {
            int index = contacts.Count;
            while (index > 0 && contacts[index - 1].Progress > contact.Progress)
                index--;
            contacts.Insert(index, contact);
            ends.Insert(index, end);
        }
    }
}
