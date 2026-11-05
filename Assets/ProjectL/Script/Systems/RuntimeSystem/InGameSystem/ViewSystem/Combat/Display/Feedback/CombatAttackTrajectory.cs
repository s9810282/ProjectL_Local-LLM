using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>
    /// 공격 조준의 궤적 표시 의도를 경로로 바꿔 필드에 그립니다(공격 궤적 설계 TJ2·TJ4·TJ6, 계획 §2.6).
    /// 공격자 외형·무기·위치를 읽어 실제 발사와 같은 공격 종류 전략(<see cref="CombatAttackStyle"/>)으로 시작·끝을 정하고,
    /// 막힌 궤적이면 Core가 준 Direct 벽 칸에서 끊습니다.
    /// </summary>
    /// <remarks>
    /// "어느 증거가 궤적을 끊나(Direct만)"의 원본은 이 클래스이고, "어느 외형이 궤적이 없나(근접)"·출발점·곡선 여부는 종류의 전략이 답합니다(View 3라운드 W8).
    /// 언제 보일지는 공격 조준이, 어떻게 그릴지(점선 규칙·메시)는 캔버스가 정합니다.
    /// 막힘 여부와 벽 칸은 Core 증거를 그대로 쓰고 그 칸을 경로에 투영할 뿐, 벽 모양을 다시 계산하지 않습니다.
    /// 경로는 필드 셀 루트 로컬 좌표로 만들고 고정 버퍼(직선 2점·곡사 33점)를 다시 씁니다. 마지막과 같은 입력이면 캔버스를 부르지 않습니다.
    /// </remarks>
    internal sealed class CombatAttackTrajectory
    {
        /// <summary>곡사 경로 표본 구간 수입니다. 곡선을 같은 길이로 다시 나눌 때의 정밀도일 뿐 화면의 점 간격과는 상관없습니다.</summary>
        internal const int ArcSegments = 32;

        private readonly ICombatTrajectoryCanvas _canvas;
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly ICombatUnitPlacementReader _placements;
        private readonly Vector3[] _straight = new Vector3[2];
        private readonly Vector3[] _arc = new Vector3[ArcSegments + 1];
        private readonly List<CombatBody> _walls = new();

        private bool _shown;
        private Drawn _last;

        internal CombatAttackTrajectory(ICombatTrajectoryCanvas canvas, CombatUnitPresentationWindow presentation,
            ICombatUnitPlacementReader placements)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _placements = placements ?? throw new ArgumentNullException(nameof(placements));
        }

        /// <summary>
        /// 공격 가능 응답의 궤적입니다. 목표점은 실제 공격과 같은 함수(<see cref="CombatAttackStyle.TryTarget"/>)로 정합니다
        /// (곡사 = 주 칸, 직선 = 주 칸 또는 방향 앞 가장 먼 영향 칸). 가능 응답은 끊지 않습니다.
        /// 근접 외형·표시 없는 공격자·주 칸 없는 곡사는 지웁니다.
        /// </summary>
        internal void Show(EntityId attackerId, in AttackPreview preview)
        {
            FieldGridMapper mapper = _canvas.Mapper;
            // 이동만 하는 끌어오기에는 직접 공격 궤적이 없습니다. 목표 칸은 실제 타격 칸(끌어오기면 도착 칸)입니다.
            if (CombatAttackTarget.IsMovementOnly(preview) || mapper == null || !TryResolveShot(attackerId, mapper, out Shot shot)
                || !shot.Style.TryTarget(CombatAttackTarget.DeliveryCell(preview), preview.AttackDirection, preview.AffectedCells.Span,
                    mapper, shot.Origin, out Vector3 target))
            {
                Clear();
                return;
            }

            _walls.Clear();
            Draw(attackerId, shot, target, mapper, blocked: false);
        }

        /// <summary>
        /// 막힌 칸의 궤적입니다. Direct 증거의 벽 칸에서 끊고 막힘으로 그립니다. Direct 증거가 없으면(착탄 뒤 파급만 막힘) 지웁니다.
        /// 끝은 첫 Direct 증거의 접촉 칸이고(방향 조준이 막혔을 때도 실제로 막힌 대상 쪽, 구현 평가 P-V6), 증거에 없으면 받은 칸입니다.
        /// </summary>
        internal void ShowBlocked(EntityId attackerId, CellCoord target, IReadOnlyList<AttackBlockedContact> contacts)
        {
            if (contacts == null)
                throw new ArgumentNullException(nameof(contacts));

            FieldGridMapper mapper = _canvas.Mapper;
            if (mapper == null || !CollectDirectWalls(contacts, mapper, out CellCoord? contact)
                || !TryResolveShot(attackerId, mapper, out Shot shot))
            {
                Clear();
                return;
            }

            CellCoord end = contact ?? target;
            if (!mapper.IsInside(end))
            {
                Clear();
                return;
            }

            Draw(attackerId, shot, mapper.CellCenterToWorld(end), mapper, blocked: true);
        }

        /// <summary>궤적을 지웁니다. 여러 번 불려도 안전합니다.</summary>
        internal void Clear()
        {
            _shown = false;
            _canvas.ClearAttackTrajectory();
        }

        /// <summary>
        /// 막힌 경로를 끊는 진행률(0~1)입니다. 경로 바닥 투영이 벽 칸 몸에 처음 닿는 가장 이른 진행률이고,
        /// 하나도 닿지 않으면 첫 벽 칸 중심을 경로에 투영합니다(히트 타이밍 설계 H5의 대체 규칙과 같음). 벽이 없으면 1입니다.
        /// </summary>
        /// <param name="from">필드 로컬 바닥 좌표(<see cref="CombatBody.Flatten"/>)입니다.</param>
        internal static float CutProgress(Vector2 from, Vector2 to, IReadOnlyList<CombatBody> walls)
        {
            if (walls == null)
                throw new ArgumentNullException(nameof(walls));
            if (walls.Count == 0)
                return 1f;

            bool touched = false;
            float earliest = 1f;
            for (int i = 0; i < walls.Count; i++)
            {
                if (CombatContactGeometry.TrySegmentContact(from, to, Vector2.zero, walls[i], out float time) && (!touched || time < earliest))
                {
                    earliest = time;
                    touched = true;
                }
            }
            return touched ? earliest : CombatContactGeometry.ProjectOnSegment(from, to, walls[0].Center);
        }

        /// <summary>
        /// 필드 로컬 시작·끝과 끊는 진행률로 경로를 <paramref name="buffer"/>에 채우고 점 개수를 돌려줍니다.
        /// 곡사는 실제 포탄과 같은 곡선 식으로 <see cref="ArcSegments"/> + 1점, 직선은 2점입니다. 곡선의 바닥 투영은 진행률에 대해 직선이라 같은 진행률로 자릅니다.
        /// </summary>
        /// <param name="arcHeight">곡선 꼭대기 높이(필드 셀 루트는 월드 크기 1이라 월드 길이와 같음)입니다.</param>
        internal static int FillPath(bool arc, Vector3 from, Vector3 to, float arcHeight, float cut, Vector3[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            int count = arc ? ArcSegments + 1 : 2;
            if (buffer.Length < count)
                throw new ArgumentException("경로 버퍼가 작습니다.", nameof(buffer));

            cut = Mathf.Clamp01(cut);
            if (!arc)
            {
                buffer[0] = from;
                buffer[1] = Vector3.Lerp(from, to, cut);
                return count;
            }

            // 필드 셀 루트는 Y축 회전만 허용하므로 로컬 위쪽이 필드 위쪽입니다.
            for (int i = 0; i < count; i++)
                buffer[i] = CombatShotPath.ArcPoint(from, to, Vector3.up, arcHeight, cut * i / ArcSegments);
            return count;
        }

        /// <summary>목표점(월드, 바닥)에서 출발·끝을 정하고, 막힘이면 끊어 로컬 경로로 그립니다. 마지막과 같은 입력이면 건너뜁니다.</summary>
        private void Draw(EntityId attackerId, in Shot shot, Vector3 target, FieldGridMapper mapper, bool blocked)
        {
            shot.Style.Segment(shot.Origin, target, mapper.WorldUp, shot.ProjectileHeight, out Vector3 worldFrom, out Vector3 worldTo);
            Vector3 from = mapper.WorldToLocal(worldFrom);
            Vector3 to = mapper.WorldToLocal(worldTo);
            float cut = blocked ? CutProgress(CombatBody.Flatten(from), CombatBody.Flatten(to), _walls) : 1f;
            bool arc = shot.Style.DrawsArc;
            var drawn = new Drawn(mapper, attackerId, arc, from, to, shot.ArcHeight, cut, blocked);
            if (_shown && drawn.Equals(_last))
                return;

            Vector3[] buffer = arc ? _arc : _straight;
            int count = FillPath(arc, from, to, shot.ArcHeight, cut, buffer);
            _canvas.ShowAttackTrajectory(buffer, count, blocked);
            _last = drawn;
            _shown = true;
        }

        /// <summary>
        /// Direct 증거의 보드 안 벽 칸 몸을 모으고, 첫 Direct 증거의 접촉 칸을 줍니다. 벽이 하나라도 있으면 true입니다
        /// (Propagation은 착탄 뒤 파급이라 궤적을 끊지 않음).
        /// </summary>
        private bool CollectDirectWalls(IReadOnlyList<AttackBlockedContact> contacts, FieldGridMapper mapper, out CellCoord? firstContact)
        {
            _walls.Clear();
            firstContact = null;
            for (int i = 0; i < contacts.Count; i++)
            {
                AttackBlockedContact contact = contacts[i];
                if (contact == null || contact.Phase != AttackOcclusionPhase.Direct)
                    continue;
                firstContact ??= contact.Contact;
                for (int w = 0; w < contact.WallCells.Count; w++)
                {
                    CellCoord wall = contact.WallCells[w];
                    if (mapper.IsInside(wall))
                        _walls.Add(mapper.CellBody(wall));
                }
            }
            return _walls.Count > 0;
        }

        /// <summary>
        /// 공격자의 공격 종류 전략과 출발 기준점입니다. 근접 외형·표시 없는 공격자는 false(궤적 없음, 설계 TJ4)입니다.
        /// 출발 기준점은 종류의 전략이 답합니다(곡사 = 무기 총구(공격 클립이 있으면 유닛 위치), 직선 = 유닛 위치, View 3라운드 W8).
        /// </summary>
        private bool TryResolveShot(EntityId attackerId, FieldGridMapper mapper, out Shot shot)
        {
            shot = default;
            if (!_presentation.TryGetAttackPresentation(attackerId, out CombatAttackPresentationData settings))
                return false;

            CombatAttackStyle style = CombatAttackStyles.Of(settings.Kind);
            if (!style.TryResolveTrajectoryOrigin(attackerId, settings, _presentation, _placements, mapper, out Vector3 origin, out float arcHeight))
                return false;
            shot = new Shot(style, origin, settings.ProjectileHeight, arcHeight);
            return true;
        }

        /// <summary>공격 종류 전략·출발 기준점·발사 높이·곡사 높이입니다(월드).</summary>
        private readonly struct Shot
        {
            internal Shot(CombatAttackStyle style, Vector3 origin, float projectileHeight, float arcHeight)
            {
                Style = style;
                Origin = origin;
                ProjectileHeight = projectileHeight;
                ArcHeight = arcHeight;
            }

            internal CombatAttackStyle Style { get; }
            internal Vector3 Origin { get; }
            internal float ProjectileHeight { get; }
            internal float ArcHeight { get; }
        }

        /// <summary>
        /// 마지막으로 그린 입력입니다. 같으면 캔버스를 다시 부르지 않습니다(성능 설계 §3.3).
        /// 필드 좌표 변환도 넣어, 필드가 다시 구성되면(캔버스가 밖에서 비워짐) 같은 경로라도 다시 그립니다(구현 평가 P-V5).
        /// </summary>
        private readonly struct Drawn : IEquatable<Drawn>
        {
            private readonly FieldGridMapper _mapper;
            private readonly EntityId _attackerId;
            private readonly bool _arc;
            private readonly Vector3 _from;
            private readonly Vector3 _to;
            private readonly float _arcHeight;
            private readonly float _cut;
            private readonly bool _blocked;

            internal Drawn(FieldGridMapper mapper, EntityId attackerId, bool arc, Vector3 from, Vector3 to, float arcHeight, float cut,
                bool blocked)
            {
                _mapper = mapper;
                _attackerId = attackerId;
                _arc = arc;
                _from = from;
                _to = to;
                _arcHeight = arcHeight;
                _cut = cut;
                _blocked = blocked;
            }

            public bool Equals(Drawn other) =>
                ReferenceEquals(_mapper, other._mapper) &&
                _attackerId == other._attackerId && _arc == other._arc && _blocked == other._blocked &&
                _from.Equals(other._from) && _to.Equals(other._to) && _arcHeight.Equals(other._arcHeight) && _cut.Equals(other._cut);

            public override bool Equals(object obj) => obj is Drawn other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_attackerId, _arc, _from, _to, _cut, _blocked);
        }
    }
}
