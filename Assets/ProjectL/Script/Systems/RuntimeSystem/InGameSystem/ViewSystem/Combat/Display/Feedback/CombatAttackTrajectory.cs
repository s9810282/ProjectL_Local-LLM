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
    /// 이 클래스가 원본인 규칙은 둘입니다. "어느 증거가 궤적을 끊나(Direct만)"와 "가능 응답에서 어느 대상에 선을 긋나(<see cref="IsLineTarget"/>,
    /// 궤적 대상별 선 설계 PT2)"입니다. "어느 외형이 궤적이 없나(근접)"·출발점·곡선 여부·대상마다 긋는지는 종류의 전략이 답합니다(View 3라운드 W8).
    /// 언제 보일지는 공격 조준이, 어떻게 그릴지(점선 규칙·메시)는 캔버스가 정합니다.
    /// 막힘 여부와 벽 칸은 Core 증거를 그대로 쓰고 그 칸을 경로에 투영할 뿐, 벽 모양을 다시 계산하지 않습니다.
    /// 경로는 필드 셀 루트 로컬 좌표로 만들고, 선마다의 버퍼(직선 2점·곡사 33점)는 처음 필요할 때 한 번 만들어 다시 씁니다.
    /// 마지막과 같은 입력이면 캔버스를 부르지 않습니다.
    /// </remarks>
    internal sealed class CombatAttackTrajectory
    {
        /// <summary>곡사 경로 표본 구간 수입니다. 곡선을 같은 길이로 다시 나눌 때의 정밀도일 뿐 화면의 점 간격과는 상관없습니다.</summary>
        internal const int ArcSegments = 32;

        private readonly ICombatTrajectoryCanvas _canvas;
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly ICombatUnitPlacementReader _placements;
        private readonly List<Vector3[]> _straights = new();
        private readonly List<Vector3[]> _arcs = new();
        private readonly List<CombatBody> _walls = new();
        // 이번에 그을 선의 목표점(월드, 바닥)과 이미 선을 받은 대상입니다. 매번 비우고 다시 씁니다.
        private readonly List<Vector3> _targets = new();
        private readonly HashSet<EntityId> _lineTargets = new();
        private readonly List<Line> _lines = new();
        private readonly List<FieldTrajectoryPath> _paths = new();
        // 스킬 꺾인 선의 점 버퍼입니다. 점 수가 늘 때만 다시 만듭니다(전투 스킬 View 계획 TP-7).
        private Vector3[] _pathPoints = Array.Empty<Vector3>();

        private bool _shown;
        private Drawn _last;
        private readonly List<Line> _lastLines = new();

        internal CombatAttackTrajectory(ICombatTrajectoryCanvas canvas, CombatUnitPresentationWindow presentation,
            ICombatUnitPlacementReader placements)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _placements = placements ?? throw new ArgumentNullException(nameof(placements));
        }

        /// <summary>
        /// 공격 가능 응답의 궤적입니다. 가능 응답은 끊지 않습니다.
        /// 대상마다 긋는 종류(직선)는 <see cref="IsLineTarget"/> 대상마다 한 줄이고(궤적 대상별 선 설계 PT2), 그런 대상이 없거나
        /// 대상마다 긋지 않는 종류는 실제 공격과 같은 목표점(<see cref="CombatAttackStyle.TryTarget"/>)으로 한 줄입니다(곡사 = 주 칸,
        /// 직선 = 주 칸 또는 방향 앞 가장 먼 영향 칸). 근접 외형·표시 없는 공격자·주 칸 없는 곡사는 지웁니다.
        /// </summary>
        internal void Show(EntityId attackerId, in AttackPreview preview)
        {
            FieldGridMapper mapper = _canvas.Mapper;
            // 이동만 하는 끌어오기에는 직접 공격 궤적이 없습니다. 목표 칸은 실제 타격 칸(끌어오기면 도착 칸)입니다.
            if (CombatAttackTarget.IsMovementOnly(preview) || mapper == null || !TryResolveShot(attackerId, mapper, out Shot shot))
            {
                Clear();
                return;
            }

            _targets.Clear();
            // 관통 탄은 조우 대상마다 선을 긋지 않고, 실제 탄처럼 사라지는 점까지 한 줄입니다(관통·죽음의 손 View 설계 §3.2).
            if (shot.Style.DrawsPerTarget && preview.PiercingShot == null)
                CollectLineTargets(preview, shot, mapper);
            if (_targets.Count == 0)
            {
                // 선 받을 대상이 없으면(빈 부채꼴 등) 지금처럼 실제 탄의 목표점 하나입니다(설계 PT3).
                if (!TryShotTarget(preview, shot, mapper, out Vector3 target))
                {
                    Clear();
                    return;
                }
                _targets.Add(target);
            }

            _walls.Clear();
            Draw(attackerId, shot, mapper, blocked: false);
        }

        /// <summary>
        /// 막힌 칸의 궤적입니다. Direct 증거의 벽 칸에서 끊고 막힘으로 그립니다. Direct 증거가 없으면(착탄 뒤 파급만 막힘) 지웁니다.
        /// 끝은 첫 Direct 증거의 접촉 칸이고(방향 조준이 막혔을 때도 실제로 막힌 대상 쪽, 구현 평가 P-V6), 증거에 없으면 받은 칸입니다.
        /// 막힌 응답에는 대상 목록이 없어 늘 한 줄입니다(설계 PT4).
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

            _targets.Clear();
            _targets.Add(mapper.CellCenterToWorld(end));
            Draw(attackerId, shot, mapper, blocked: true);
        }

        /// <summary>
        /// 시전자에서 칸들을 차례로 잇는 꺾인 직선 하나입니다(전투 스킬 View 설계 SK2, 계획 TP-7). 스킬 선택 선(시전자 → 고른 점)과 예보의 실제 경로 선이 씁니다.
        /// 첫 구간은 직선 발사와 같은 기하(발사 높이에서 조금 앞)이고, 다음 점은 칸 가운데를 발사 높이로 올린 점입니다. 막힘으로 끊지 않습니다(Core 경로는 실제로 지난 칸).
        /// 곡사·근접처럼 꺾인 직선이 아닌 연출, 표시 없는 시전자, 빈 칸 목록이면 지웁니다.
        /// </summary>
        internal void ShowPath(EntityId casterId, IReadOnlyList<CellCoord> cells, CombatAttackPresentationData settings)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            FieldGridMapper mapper = _canvas.Mapper;
            CombatAttackStyle style = CombatAttackStyles.Of(settings.Kind);
            if (mapper == null || cells.Count == 0 || style.DrawsArc
                || !style.TryResolveTrajectoryOrigin(casterId, settings, _presentation, _placements, mapper, out Vector3 origin, out _))
            {
                Clear();
                return;
            }

            Vector3 up = mapper.WorldUp;
            float height = settings.ProjectileHeight;
            int count = cells.Count + 1;
            if (_pathPoints.Length < count)
                _pathPoints = new Vector3[count];
            style.Segment(origin, mapper.CellCenterToWorld(cells[0]), up, height, out Vector3 from, out Vector3 firstTo);
            _pathPoints[0] = mapper.WorldToLocal(from);
            _pathPoints[1] = mapper.WorldToLocal(firstTo);
            for (int i = 1; i < cells.Count; i++)
                _pathPoints[i + 1] = mapper.WorldToLocal(CombatShotPath.Raise(mapper.CellCenterToWorld(cells[i]), up, height));

            _paths.Clear();
            _paths.Add(new FieldTrajectoryPath(_pathPoints, count));
            _canvas.ShowAttackTrajectories(_paths, blocked: false);
            // 공격 궤적의 같은 입력 기억과는 다른 그림이라, 다음 공격 궤적은 같은 값이어도 다시 그립니다.
            _shown = false;
        }

        /// <summary>궤적을 지웁니다. 여러 번 불려도 안전합니다.</summary>
        internal void Clear()
        {
            _shown = false;
            _canvas.ClearAttackTrajectory();
        }

        /// <summary>
        /// 가능 응답에서 선을 받는 대상입니다(궤적 대상별 선 설계 PT2): 공격 전달 접촉이 있고(<c>IsDirectHit</c>) 역할이 주 대상
        /// (<see cref="AttackTargetRole.Primary"/>)이나 직접 지정 칸 대상(<see cref="AttackTargetRole.Designated"/>, 샷건 부채꼴)인 대상입니다.
        /// 스플래시·특성 경로·접촉 없는 보조 피해 대상은 선이 없습니다. 스플래시도 접촉이 있어 <c>IsDirectHit</c>만으로는 거르지 못해 역할을 함께 봅니다.
        /// 역할·접촉은 Core 값 그대로이며 View에서 다시 계산하지 않습니다.
        /// </summary>
        internal static bool IsLineTarget(in AttackTargetPreview target) =>
            target.IsDirectHit && (target.Role == AttackTargetRole.Primary || target.Role == AttackTargetRole.Designated);

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

        /// <summary>
        /// 선을 받는 대상마다 목표점(월드, 바닥)을 모읍니다(설계 PT2). 주 대상은 실제 탄과 같은 목표점(주 칸)이고,
        /// 직접 지정 칸 대상은 그 대상이 서 있는 위치입니다. 같은 대상은 한 번만, 주 대상 선도 한 번만(끝이 늘 주 칸이라), 순서는 응답 순서입니다.
        /// </summary>
        private void CollectLineTargets(in AttackPreview preview, in Shot shot, FieldGridMapper mapper)
        {
            _lineTargets.Clear();
            bool primaryDrawn = false;
            ReadOnlySpan<AttackTargetPreview> targets = preview.Targets.Span;
            for (int i = 0; i < targets.Length; i++)
            {
                ref readonly AttackTargetPreview target = ref targets[i];
                if (!IsLineTarget(target) || !_lineTargets.Add(target.TargetId))
                    continue;

                Vector3 point;
                bool found;
                if (target.Role == AttackTargetRole.Primary)
                {
                    if (primaryDrawn)
                        continue;
                    primaryDrawn = true;
                    found = TryShotTarget(preview, shot, mapper, out point);
                }
                else
                    found = TryStandingPoint(target, mapper, out point);
                if (found)
                    _targets.Add(point);
            }
        }

        /// <summary>
        /// 실제 공격과 같은 목표점입니다(구현 평가 P-V1). 목표 칸은 실제 타격 칸(끌어오기면 도착 칸)이고, 관통이면 탄이 사라지는 점입니다
        /// (관통·죽음의 손 View 설계 §3.2, 실제 탄과 같은 함수 <see cref="CombatAttackStyle.TryShotTarget"/>).
        /// </summary>
        private static bool TryShotTarget(in AttackPreview preview, in Shot shot, FieldGridMapper mapper, out Vector3 target) =>
            shot.Style.TryShotTarget(CombatAttackTarget.DeliveryCell(preview), preview.AttackDirection, preview.AffectedCells.Span,
                preview.PiercingShot, mapper, shot.Origin, out target);

        /// <summary>
        /// 대상이 서 있는 위치(월드)입니다. 표시 위치를 읽지 못하는 대상(구조물·장애물·꺼진 표시)은 차지한 칸 중심들의 평균입니다(설계 P3).
        /// 차지한 칸도 없으면 false입니다.
        /// </summary>
        private bool TryStandingPoint(in AttackTargetPreview target, FieldGridMapper mapper, out Vector3 point)
        {
            if (_placements.TryGetPlacedPosition(target.TargetId, out point))
                return true;

            ReadOnlySpan<CellCoord> cells = target.TargetOccupiedCells.Span;
            if (cells.Length == 0)
                return false;

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < cells.Length; i++)
                sum += mapper.CellCenterToWorld(cells[i]);
            point = sum / cells.Length;
            return true;
        }

        /// <summary>
        /// 모은 목표점마다 출발·끝을 정하고, 막힘이면 끊어 로컬 경로로 그립니다. 마지막과 같은 입력이면 건너뜁니다.
        /// 출발점은 모든 선이 같은 공격자 기준점이고, 발사 높이·출발 당기기는 선마다 전략이 정합니다.
        /// </summary>
        private void Draw(EntityId attackerId, in Shot shot, FieldGridMapper mapper, bool blocked)
        {
            _lines.Clear();
            for (int i = 0; i < _targets.Count; i++)
            {
                shot.Style.Segment(shot.Origin, _targets[i], mapper.WorldUp, shot.ProjectileHeight, out Vector3 worldFrom, out Vector3 worldTo);
                Vector3 from = mapper.WorldToLocal(worldFrom);
                Vector3 to = mapper.WorldToLocal(worldTo);
                float cut = blocked ? CutProgress(CombatBody.Flatten(from), CombatBody.Flatten(to), _walls) : 1f;
                _lines.Add(new Line(from, to, cut));
            }

            bool arc = shot.Style.DrawsArc;
            var drawn = new Drawn(mapper, attackerId, arc, shot.ArcHeight, blocked);
            if (_shown && drawn.Equals(_last) && SameLines())
                return;

            _paths.Clear();
            for (int i = 0; i < _lines.Count; i++)
            {
                Vector3[] buffer = BufferFor(arc, i);
                Line line = _lines[i];
                int count = FillPath(arc, line.From, line.To, shot.ArcHeight, line.Cut, buffer);
                _paths.Add(new FieldTrajectoryPath(buffer, count));
            }
            _canvas.ShowAttackTrajectories(_paths, blocked);

            _last = drawn;
            // AddRange는 임시 배열을 만들 수 있어 하나씩 옮깁니다(호버마다 할당 없음, 설계 PT6).
            _lastLines.Clear();
            for (int i = 0; i < _lines.Count; i++)
                _lastLines.Add(_lines[i]);
            _shown = true;
        }

        /// <summary>이번 선 목록이 마지막으로 그린 선 목록과 같은지입니다(개수·순서·선마다 값).</summary>
        private bool SameLines()
        {
            if (_lines.Count != _lastLines.Count)
                return false;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (!_lines[i].Equals(_lastLines[i]))
                    return false;
            }
            return true;
        }

        /// <summary>선 i번째의 경로 버퍼입니다. 선마다 처음 필요할 때 한 번 만듭니다(설계 PT6).</summary>
        private Vector3[] BufferFor(bool arc, int index)
        {
            List<Vector3[]> buffers = arc ? _arcs : _straights;
            while (buffers.Count <= index)
                buffers.Add(new Vector3[arc ? ArcSegments + 1 : 2]);
            return buffers[index];
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

        /// <summary>선 하나의 필드 로컬 시작·끝과 끊는 진행률입니다.</summary>
        private readonly struct Line : IEquatable<Line>
        {
            internal Line(Vector3 from, Vector3 to, float cut)
            {
                From = from;
                To = to;
                Cut = cut;
            }

            internal Vector3 From { get; }
            internal Vector3 To { get; }
            internal float Cut { get; }

            public bool Equals(Line other) => From.Equals(other.From) && To.Equals(other.To) && Cut.Equals(other.Cut);

            public override bool Equals(object obj) => obj is Line other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(From, To, Cut);
        }

        /// <summary>
        /// 마지막으로 그린 입력 중 선 목록 밖의 값입니다. 선 목록(<see cref="SameLines"/>)과 함께 같으면 캔버스를 다시 부르지 않습니다(성능 설계 §3.3).
        /// 필드 좌표 변환도 넣어, 필드가 다시 구성되면(캔버스가 밖에서 비워짐) 같은 경로라도 다시 그립니다(구현 평가 P-V5).
        /// </summary>
        private readonly struct Drawn : IEquatable<Drawn>
        {
            private readonly FieldGridMapper _mapper;
            private readonly EntityId _attackerId;
            private readonly bool _arc;
            private readonly float _arcHeight;
            private readonly bool _blocked;

            internal Drawn(FieldGridMapper mapper, EntityId attackerId, bool arc, float arcHeight, bool blocked)
            {
                _mapper = mapper;
                _attackerId = attackerId;
                _arc = arc;
                _arcHeight = arcHeight;
                _blocked = blocked;
            }

            public bool Equals(Drawn other) =>
                ReferenceEquals(_mapper, other._mapper) &&
                _attackerId == other._attackerId && _arc == other._arc && _blocked == other._blocked &&
                _arcHeight.Equals(other._arcHeight);

            public override bool Equals(object obj) => obj is Drawn other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_attackerId, _arc, _arcHeight, _blocked);
        }
    }
}
