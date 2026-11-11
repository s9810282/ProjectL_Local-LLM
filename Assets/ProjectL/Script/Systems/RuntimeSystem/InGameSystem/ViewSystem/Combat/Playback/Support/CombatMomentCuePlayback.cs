using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 사실 사건의 순간 큐(이펙트·흔들림)를 내는 곳입니다(이펙트 창구 확장 설계 EC6·EC7, 계획 Eb.11). 사건 Presenter가 연결마다 하나 만들고,
    /// 사건 틀이 사건 소리 바로 뒤에서 한 줄로 부릅니다. 순간인지는 사건 형식 표의 순간 칸, 큐 값은 <see cref="CombatCueSources"/>가 답합니다.
    /// 특성 발동 큐도 같은 곁들임 실행 입구(<see cref="PlayAccent"/>)로 냅니다(특성 발동 연출 설계 TA3). 그쪽은 특성 처리기가 줄 값을 읽고 자리를 만들어 발동마다 부릅니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 큐는 곁들임 실행(<see cref="CombatPieceScope.Accent"/>)으로 냅니다. 사건을 막지 않고 사건 완료도 알리지 않아, 큐를 붙여도 사건 순서·시간이 같습니다.
    /// 다만 남은 지연 항목만큼 기차 사건과 결과 끝이 기다립니다(설계 §7 F4).
    /// </para>
    /// <para>
    /// 큐 값은 순간 열쇠마다 처음 읽을 때 보관하고 이 객체가 사는 동안(연결 하나) 다시 씁니다(계획 P6). SO를 에디터에서 고치면 다음 전투 진입부터 보입니다.
    /// 붙인 항목이 하나도 없으면(흔들림 수신기 없음·세기 0·자리 못 찾음) 시작하지 않습니다. 빈 조립은 실행할 수 없기 때문입니다(계획 P17, 설계 §7 F1).
    /// </para>
    /// </remarks>
    internal sealed class CombatMomentCuePlayback
    {
        private readonly Dictionary<EntityKey, CombatEventCues> _structureCues = new();

        /// <summary>Structure 사건이 해제된 순간의 몸 자리에서 큐를 냅니다.</summary>
        internal void PlayStructureHit(EntityKey key,
            EntityId id, Pose body)
        {
            if (!_structureCues.TryGetValue(key, out CombatEventCues cues))
            {
                cues = _sources.StructureHit(key);
                _structureCues.Add(key, cues);
            }
            if (!cues.IsEmpty) PlayAccent(cues, CombatCuePoints.ForSubject(id, body));
        }
        private readonly CombatCueSources _sources;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatShownFacts _facts;
        private readonly CombatEffectWindow _effects;
        private readonly CombatCameraWindow _camera;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly Func<int> _generation;
        private readonly Dictionary<(CombatMomentKind Kind, int Key), CombatEventCues> _cues = new();

        internal CombatMomentCuePlayback(CombatCueSources sources, CombatPieceRunPlayer pieces, CombatShownFacts facts,
            CombatEffectWindow effects, CombatCameraWindow camera, Func<FieldGridMapper> mapper, Func<int> generation)
        {
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        /// <summary>
        /// 사건이 순간이고 그 큐가 있으면 곁들임 실행을 시작합니다. 사실 반영 전에 부릅니다(주체 몸 자리를 지금 보인 사실로 구함).
        /// 순간이 아니거나 큐가 없거나 붙은 항목이 없으면 아무것도 하지 않습니다.
        /// </summary>
        internal void Play(CombatEvent e)
        {
            if (!CombatEventTypeTable.TryGetMoment(e, out CombatEventMoment moment))
                return;
            CombatEventCues cues = CuesOf(moment);
            if (cues.IsEmpty)
                return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            TryPlayAccent(cues, PointsOf(moment), profilingEvent: e);
#else
            PlayAccent(cues, PointsOf(moment));
#endif
        }

        /// <summary>
        /// 큐 묶음 하나를 주어진 자리로 곁들임 실행으로 냅니다. 순간 큐(<see cref="Play"/>)와 특성 발동 큐(특성 처리기, 특성 발동 연출 설계 TA3)가 함께 쓰는
        /// 한 곳입니다(묶음 끝 검토 L3 — 값을 읽고 자리를 만드는 것은 부르는 쪽). 큐 칸은 시작(사건 순간)만 받아 기준 시점 표에 이을 뼈대가 없습니다.
        /// 소리는 소리 표가 맡습니다(설계 EC3). 붙은 항목이 없으면 시작하지 않습니다.
        /// </summary>
        /// <param name="actionGroup">행동 끝 큐(<c>UnitAction</c>)를 거둘 발동 묶음입니다(특성 발동 큐만, 이펙트 끝 규칙 계획 TP-E3). 없으면 결과 끝에 거둡니다.</param>
        internal void PlayAccent(CombatEventCues cues, CombatCuePoints points, CombatEffectActionGroup actionGroup = null)
        {
            TryPlayAccent(cues, points, actionGroup);
        }

        /// <summary>실제 항목이 있는 큐 실행을 열었는지입니다. 기존 큐 내부 지연은 그대로 적용합니다.</summary>
        internal bool TryPlayAccent(CombatEventCues cues, CombatCuePoints points, CombatEffectActionGroup actionGroup = null
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , CombatEvent profilingEvent = null
#endif
        )
        {
            if (cues == null) throw new ArgumentNullException(nameof(cues));

            CombatComposition composition = CombatComposition.ForAccent();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            composition.ProfilingEvent = profilingEvent;
#endif
            CombatCueComposer.Add(composition, new CombatCueAnchors(), cues, points,
                new CombatCueSinks(_effects, _camera, null, null, actionGroup: actionGroup));
            if (composition.IsEmpty)
                return false;
            if (!_pieces.TryBegin(composition, null, _generation()))
                throw new InvalidOperationException("[CombatMomentCuePlayback] 빌릴 채널이 없는 곁들임 실행은 늘 시작해야 합니다.");
            return true;
        }

        private CombatEventCues CuesOf(in CombatEventMoment moment)
        {
            (CombatMomentKind, int) key = (moment.Kind, moment.Key);
            if (!_cues.TryGetValue(key, out CombatEventCues cues))
            {
                cues = _sources.For(moment);
                _cues.Add(key, cues);
            }
            return cues;
        }

        // 주체가 있는 순간(아이템)은 주체 몸 자리(보인 사실로 서 있는 몸, 설계 EC2 아이템 = 소지자), 그 밖은 사건 칸 자리입니다.
        // 고를 수 있는 자리는 큐 규칙이 칸마다 이미 막았습니다.
        private CombatCuePoints PointsOf(in CombatEventMoment moment) =>
            moment.Subject.IsEmpty
                ? CombatCuePoints.ForCell(CellPoseOf(moment.Cell))
                : CombatCuePoints.ForSubject(moment.Subject, _facts.StandingBodyOf(_facts.FindCharacter(moment.Subject)));

        // 사건 칸 자리입니다(규칙 원본 CombatCuePoints.CellPose). 칸이 없는 장애물 반응은 null(그 자리 이펙트 없음)입니다.
        private Pose? CellPoseOf(CellCoord? cell) => cell.HasValue ? CombatCuePoints.CellPose(_mapper(), cell.Value) : (Pose?)null;
    }
}
