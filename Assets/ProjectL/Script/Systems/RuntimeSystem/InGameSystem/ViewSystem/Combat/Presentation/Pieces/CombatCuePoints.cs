using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 이펙트 큐 자리 해석의 한 곳입니다(범용 연출 구조 설계 G6, 계획 Gd.5). 자리 → 위치·회전 규칙과, 큐의 위치·회전 보정을 자리에 적용하는 규칙
    /// (<see cref="Apply"/>, 이펙트 창구 확장 설계 EC1·계획 P2)을 모읍니다. 옛 세 규칙(클립 이펙트 지점, 피격 이펙트의 몸 중심·맞은 방향, 투사체 접촉점)이 여기로 왔습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 행위자 지점 자리(이펙트 지점·발사 지점)는 그 실행이 빌린 클립 채널에서 시작 순간에 읽습니다. 그래서 공격 클립이 있는 유닛만 쓰고(계획 P6),
    /// 보정도 그 조각이 시작 순간에 적용합니다. 정해진 자리(몸·칸)는 여기서 보정해 조각에 넘깁니다. 자리마다 보정을 적용하는 곳은 하나입니다(계획 P3).
    /// </para>
    /// <para>
    /// 몸 자리(행위자·주체)와 칸 자리는 처리기·내는 곳이 조립을 만들 때 구해 넘기고, 찾지 못했으면 그 큐의 항목을 넣지 않습니다(옛 피격 이펙트와 같음).
    /// 주체 몸은 시점별로 받습니다. 끝(End) 시점 큐는 끝 자리를 씁니다(토네이도: 휩쓸리기 전·내려앉은 뒤, 계획 P10). 피격·사망은 두 자리가 같습니다.
    /// </para>
    /// <para>
    /// 주체 몸과 칸 자리는 여럿일 수 있습니다(특성 발동 연출 설계 TA4). 그 자리를 고른 이펙트 큐는 자리마다 항목 하나씩입니다(<see cref="CountOf"/>).
    /// 지금 여럿을 넘기는 곳은 특성 발동뿐이고, 다른 사건은 하나 또는 없음입니다.
    /// </para>
    /// <para>
    /// 몸 자리는 그 몸의 유닛과 함께 받습니다(<see cref="Body"/>). 남기기가 꺼진 몸 자리 큐(<see cref="CombatEffectCue.FollowsUnit"/>)는 그 유닛의 위치를 따라가는
    /// 조각이 됩니다(이펙트 따라가기 설계 FE1). 낼 자리는 지금처럼 이 값의 몸 자리이고, 따라가기는 낸 뒤의 움직임만 더합니다.
    /// </para>
    /// </remarks>
    internal readonly struct CombatCuePoints
    {
        private static readonly Pose[] NoPoses = Array.Empty<Pose>();
        private static readonly Body[] NoBodies = Array.Empty<Body>();

        /// <summary>
        /// 몸 자리 하나입니다. <see cref="Unit"/>은 그 몸의 유닛이고, 남기기가 꺼진 몸 자리 이펙트가 따라갈 대상입니다(이펙트 따라가기 설계 FE1).
        /// </summary>
        internal readonly struct Body
        {
            internal Body(EntityId unit, Pose pose)
            {
                Unit = unit;
                Pose = pose;
            }

            internal EntityId Unit { get; }
            internal Pose Pose { get; }
        }

        private readonly EntityId _actor;
        private readonly Pose? _actorBody;
        private readonly IReadOnlyList<Body> _subjectBodies;
        private readonly IReadOnlyList<Body> _subjectEndBodies;
        private readonly IReadOnlyList<Pose> _cells;

        private CombatCuePoints(EntityId actor, Pose? actorBody, IReadOnlyList<Body> subjectBodies, IReadOnlyList<Body> subjectEndBodies,
            IReadOnlyList<Pose> cells)
        {
            _actor = actor;
            _actorBody = actorBody;
            _subjectBodies = subjectBodies;
            _subjectEndBodies = subjectEndBodies;
            _cells = cells;
        }

        /// <summary>공격 칸의 자리입니다. <paramref name="actorBody"/>는 행위자 몸 상자 중심·모델 방향이고, 찾지 못했거나 쓰지 않으면 null입니다.</summary>
        internal static CombatCuePoints ForActor(EntityId actor, Pose? actorBody = null) => new(actor, actorBody, null, null, null);

        /// <summary>
        /// 피격·사망·아이템 칸의 자리입니다. <paramref name="subjectBody"/>는 주체 몸 상자 중심과 그 칸의 방향이고, 찾지 못했으면 null입니다.
        /// <paramref name="subject"/>는 그 몸의 유닛이고, 따라가는 이펙트가 그 유닛을 따라갑니다.
        /// </summary>
        internal static CombatCuePoints ForSubject(EntityId subject, Pose? subjectBody) => ForSubject(subject, subjectBody, subjectBody);

        /// <summary>시점마다 몸 자리가 다른 주체의 자리입니다(토네이도). 끝(End) 시점 큐는 <paramref name="end"/>, 그 밖은 <paramref name="start"/>를 씁니다.</summary>
        internal static CombatCuePoints ForSubject(EntityId subject, Pose? start, Pose? end) =>
            new(EntityId.Empty, null, One(subject, start), One(subject, end), null);

        /// <summary>사건 칸의 자리입니다(<see cref="CombatCuePoint.EventCell"/>). 칸 표면 가운데·필드 방향이고(<see cref="CellPose"/>), 칸이 없으면 null입니다.</summary>
        internal static CombatCuePoints ForCell(Pose? cell) => new(EntityId.Empty, null, null, null, One(cell));

        /// <summary>
        /// 칸 자리의 위치·회전 규칙 한 곳입니다(이펙트 창구 확장 설계 EC2, 결정 Q-EC6 A). 칸 표면 가운데(고지대면 그 높이)와 필드 방향(R0)입니다.
        /// 순간 큐와 특성 발동 큐가 함께 부릅니다(특성 시스템 View 계획 Ta.5).
        /// </summary>
        /// <exception cref="InvalidOperationException">필드 연결 전(<paramref name="mapper"/> null)이거나 칸이 필드 밖일 때입니다.</exception>
        internal static Pose CellPose(FieldGridMapper mapper, CellCoord cell)
        {
            if (mapper == null)
                throw new InvalidOperationException("[CombatCuePoints] 필드 연결 전에는 칸 자리를 정할 수 없습니다.");
            if (!mapper.IsInside(cell))
                throw new InvalidOperationException($"[CombatCuePoints] 칸 자리가 필드 밖입니다: {cell}");
            return new Pose(mapper.CellSurfaceToWorld(cell), mapper.RotationToWorld(Rotation4.R0));
        }

        /// <summary>
        /// 특성 발동 칸의 자리입니다(특성 발동 연출 설계 TA4). 발동 유닛 몸 하나, 대상 몸 여럿, 칸 여럿입니다. 찾지 못한 몸·칸은 목록에 넣지 않고 넘깁니다.
        /// 특성 발동 큐는 시작 시점만 받아(큐 규칙) 끝 자리는 시작 자리와 같습니다.
        /// </summary>
        internal static CombatCuePoints ForActivation(EntityId activator, Pose? activatorBody, IReadOnlyList<Body> subjectBodies,
            IReadOnlyList<Pose> cells) => new(activator, activatorBody, subjectBodies, subjectBodies, cells);

        /// <summary>
        /// 특성 발동 사실 하나의 큐 자리입니다(특성 발동 연출 설계 TA4). 발동 유닛 몸, 대상(<c>TargetIds</c>)마다의 몸, 영향 칸(<c>TargetCells</c>)마다의 칸 자리입니다.
        /// 특성 처리기(사건 목록 안 발동)와 공격 시작(가로채기 반응 줄, 유닛 행동 계획 Tf4.5)이 함께 부르는 한 곳입니다. 칸 자리 규칙은 <see cref="CellPose"/>입니다.
        /// </summary>
        /// <param name="bodyOf">대상 하나의 지금 몸 자리입니다(보인 사실의 서 있는 몸). 찾지 못하면 null이고, 그 몸은 목록에 넣지 않습니다.</param>
        /// <param name="mapper">필드 좌표 변환입니다. 칸이 있을 때만 읽습니다.</param>
        internal static CombatCuePoints ForActivation(TraitActivationSnapshot trait, Func<EntityId, Pose?> bodyOf, Func<FieldGridMapper> mapper)
        {
            if (trait == null) throw new ArgumentNullException(nameof(trait));
            if (bodyOf == null) throw new ArgumentNullException(nameof(bodyOf));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            var targets = new List<Body>(trait.TargetIds.Count);
            for (int i = 0; i < trait.TargetIds.Count; i++)
            {
                Pose? body = bodyOf(trait.TargetIds[i]);
                if (body.HasValue)
                    targets.Add(new Body(trait.TargetIds[i], body.Value));
            }
            IReadOnlyList<Pose> cells = Array.Empty<Pose>();
            if (trait.TargetCells.Count > 0)
            {
                FieldGridMapper field = mapper();
                var poses = new Pose[trait.TargetCells.Count];
                for (int i = 0; i < poses.Length; i++)
                    poses[i] = CellPose(field, trait.TargetCells[i]);
                cells = poses;
            }
            return ForActivation(trait.SourceUnitId, bodyOf(trait.SourceUnitId), targets, cells);
        }

        /// <summary>투사체 접촉 자리입니다. 회전 = 진행 방향(접촉 값이 정함). 보정은 부르는 쪽이 <see cref="Apply"/>로 적용합니다.</summary>
        internal static Pose Contact(CombatProjectileContact contact) => new(contact.Position, contact.Rotation);

        /// <summary>
        /// 큐의 위치·회전 보정을 자리에 적용합니다(이펙트 창구 확장 설계 EC1). 위치 = 자리 위치 + 자리 회전 × 위치 보정(월드 단위),
        /// 회전 = 자리 회전 × 회전 보정입니다. 추가 월드 보정은 자리 회전과 무관하게 더합니다. 보정이 0이면 자리 그대로입니다(계획 P2).
        /// </summary>
        internal static Pose Apply(Pose point, CombatEffectCue cue, Vector3 additionalWorldOffset = default)
        {
            if (cue == null) throw new ArgumentNullException(nameof(cue));
            return Apply(point, cue.PositionOffset, cue.RotationOffset, additionalWorldOffset);
        }

        /// <summary>
        /// 자리 기준 보정을 자리에 적용하는 규칙의 원본입니다. 위치는 자리 회전으로 돌린 보정과 추가 월드 보정만큼 옮기고, 회전은 자리 회전 뒤에 보정을 더합니다.
        /// 큐와 오래 보이는 이펙트(상태 유지·특성 상시, 이펙트 끝 규칙 계획 TP-E11)가 함께 씁니다.
        /// </summary>
        internal static Pose Apply(Pose point, Vector3 positionOffset, Quaternion rotationOffset, Vector3 additionalWorldOffset = default) =>
            new(point.position + point.rotation * positionOffset + additionalWorldOffset, point.rotation * rotationOffset);

        /// <summary>
        /// 그 이펙트 큐가 낼 항목 수입니다. 행위자 지점 자리는 하나(시작 순간에 읽음), 몸·칸 자리는 찾은 자리 수이고, 찾지 못했으면 0(항목 없음)입니다.
        /// 접촉 자리는 조립 항목이 아니라 예외입니다(접촉 입구가 냄).
        /// </summary>
        internal int CountOf(CombatEffectCue cue)
        {
            if (cue == null) throw new ArgumentNullException(nameof(cue));
            return cue.Point switch
            {
                CombatCuePoint.ActorEffectPoint or CombatCuePoint.ActorMuzzle => 1,
                CombatCuePoint.ActorBody => _actorBody.HasValue ? 1 : 0,
                CombatCuePoint.SubjectBody => SubjectBodiesOf(cue).Count,
                CombatCuePoint.EventCell => Cells.Count,
                CombatCuePoint.Contact => throw new InvalidOperationException("[CombatCuePoints] 접촉 자리 이펙트는 공격 재생의 접촉 입구가 냅니다."),
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue.Point, "정의되지 않은 이펙트 자리입니다."),
            };
        }

        /// <summary>그 이펙트 큐의 <paramref name="index"/>번째 자리 조각입니다. 인덱스는 0부터 <see cref="CountOf"/> 앞까지입니다.</summary>
        /// <param name="group">행동 끝 큐의 발동 묶음입니다(특성 발동 큐만, 없으면 결과 끝에 거둠 — 이펙트 끝 규칙 계획 TP-E3·TP-E4).</param>
        internal CombatEffectPiece CreatePiece(CombatEffectWindow effects, CombatEffectCue cue, int index, CombatEffectActionGroup group = null)
        {
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            int count = CountOf(cue);
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"이펙트 자리 수({count}) 밖입니다.");
            return cue.Point switch
            {
                CombatCuePoint.ActorEffectPoint => CombatEffectPiece.AtClipPoint(_actor, effects, cue),
                CombatCuePoint.ActorMuzzle => CombatEffectPiece.AtClipMuzzle(_actor, effects, cue),
                CombatCuePoint.ActorBody => At(effects, cue, _actor, _actorBody.Value, group),
                CombatCuePoint.SubjectBody => At(effects, cue, SubjectBodiesOf(cue)[index].Unit, SubjectBodiesOf(cue)[index].Pose, group),
                _ => At(effects, cue, EntityId.Empty, Cells[index], group),
            };
        }

        // 끝(End) 시점 큐는 끝 자리, 그 밖은 시작 자리입니다.
        private IReadOnlyList<Body> SubjectBodiesOf(CombatEffectCue cue) =>
            (cue.Anchor == CombatCueAnchor.End ? _subjectEndBodies : _subjectBodies) ?? NoBodies;

        // 비운 목록(기본값 구조체 포함)은 자리 없음입니다.
        private IReadOnlyList<Pose> Cells => _cells ?? NoPoses;

        private static IReadOnlyList<Pose> One(Pose? point) => point.HasValue ? new[] { point.Value } : NoPoses;

        private static IReadOnlyList<Body> One(EntityId unit, Pose? point) => point.HasValue ? new[] { new Body(unit, point.Value) } : NoBodies;

        // 정해진 자리의 조각입니다. 보정은 여기서 한 번 적용하고, 조각은 받은 자리를 그대로 씁니다(계획 P3).
        // 따라갈지·끝은 큐의 놓기 값이 정합니다(몸 자리·남기기 꺼짐이면 그 유닛을 따라감, 이펙트 따라가기 설계 FE1·이펙트 끝 규칙 설계 EL5).
        private static CombatEffectPiece At(CombatEffectWindow effects, CombatEffectCue cue, EntityId unit, Pose point, CombatEffectActionGroup group) =>
            CombatEffectPiece.At(effects, cue, Apply(point, cue, effects.AdditionalWorldOffsetOf(unit, cue.RiderYOffset)), unit, group);
    }
}
