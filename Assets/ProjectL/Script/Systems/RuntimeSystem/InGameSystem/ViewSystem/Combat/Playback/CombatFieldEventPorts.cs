using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback
{
    /// <summary>
    /// 사건 Presenter가 필드 표시(가시·오브젝트·토네이도·낙석·하붑(모래폭풍)·지형 바뀜·특성 흔적·필드 외형 연출·필드 강조)에 닿는 창구입니다(View 3라운드 W9-3, 결정 Q-V27 A).
    /// 화약통·지뢰 폭발은 이 창구가 아니라 외형의 순간 큐로 냅니다(이펙트 창구 확장 설계 EC9).
    /// 창구는 모두 필수입니다.
    /// 운영 표시와 특성 확인 표시 모두 표시 Factory의 <c>CombatSceneDisplayFactory.CreateFieldPorts</c> 한 함수로 필드 표시들의 메서드를 넘깁니다(평가 C4-02).
    /// </summary>
    internal sealed class CombatFieldEventPorts
    {
        /// <param name="consumeThorn">가시 하나를 소비해 칸에서 지웁니다.</param>
        /// <param name="presentHighlight">재생 강조 층에 칸을 둡니다(빈 목록 = 지움).</param>
        /// <param name="thornBody">가시 칸의 걸음 접촉 몸입니다(이동 접촉 설계 M2).</param>
        /// <param name="objectBody">오브젝트 원본의 걸음 접촉 몸입니다(이동 접촉 설계 M2).</param>
        /// <param name="hideObject">발동한 오브젝트 원본을 사건 시점에 숨깁니다(이동 접촉 설계 M6).</param>
        /// <param name="showTerrainChange">종류가 바뀐 지형 원본(약한 지반 붕괴)의 칸 외형을 사건 시점에 바꿉니다(약한 지반 설계 WG6).</param>
        /// <param name="showTrail">특성 흔적 원본(땅 다지기·먼지 연막)을 사건 시점에 놓거나 바꾸거나 지웁니다(특성 시스템 View 설계 TS7).</param>
        /// <param name="tickAppearance">필드 외형 연출(토네이도 생성·축소, 바닥 교체 무너짐)을 진행합니다(타일 교체 설계 TR8).</param>
        /// <param name="appearancePlaying">필드 외형 연출이 하나라도 진행 중인지입니다. 결과 끝 사실 반영이 기다립니다.</param>
        /// <param name="appearanceBlocksStart">다음 사건 시작을 막는 필드 외형 연출이 있는지입니다(무너짐은 떨리는 동안만).</param>
        /// <param name="cancelAppearance">필드 외형 연출을 멈춥니다.</param>
        /// <param name="tornado">움직이는 토네이도 표시입니다(토네이도 View 계획 TP-3).</param>
        /// <param name="rockfall">낙석 표시입니다(낙석 View 계획 RP-4).</param>
        /// <param name="haboob">하붑(모래폭풍) 구름 표시입니다(하붑 View 계획 HP-6).</param>
        internal CombatFieldEventPorts(
            Action<CellCoord> consumeThorn,
            Action<IReadOnlyList<CellCoord>> presentHighlight,
            Func<CellCoord, FieldGridMapper, CombatBody> thornBody,
            Func<ObjectSourceFact, FieldGridMapper, CombatBody> objectBody,
            Action<long> hideObject,
            Action<MovementTileFact, FieldGridMapper> showTerrainChange,
            Action<TraitTrailFact, FieldGridMapper> showTrail,
            Action<float> tickAppearance, Func<bool> appearancePlaying, Func<bool> appearanceBlocksStart, Action cancelAppearance,
            ICombatTornadoField tornado,
            ICombatRockfallField rockfall,
            ICombatHaboobField haboob)
        {
            ConsumeThorn = consumeThorn ?? throw new ArgumentNullException(nameof(consumeThorn));
            PresentHighlight = presentHighlight ?? throw new ArgumentNullException(nameof(presentHighlight));
            ThornBody = thornBody ?? throw new ArgumentNullException(nameof(thornBody));
            ObjectBody = objectBody ?? throw new ArgumentNullException(nameof(objectBody));
            HideObject = hideObject ?? throw new ArgumentNullException(nameof(hideObject));
            ShowTerrainChange = showTerrainChange ?? throw new ArgumentNullException(nameof(showTerrainChange));
            ShowTrail = showTrail ?? throw new ArgumentNullException(nameof(showTrail));
            TickAppearance = tickAppearance ?? throw new ArgumentNullException(nameof(tickAppearance));
            AppearancePlaying = appearancePlaying ?? throw new ArgumentNullException(nameof(appearancePlaying));
            AppearanceBlocksStart = appearanceBlocksStart ?? throw new ArgumentNullException(nameof(appearanceBlocksStart));
            CancelAppearance = cancelAppearance ?? throw new ArgumentNullException(nameof(cancelAppearance));
            Tornado = tornado ?? throw new ArgumentNullException(nameof(tornado));
            Rockfall = rockfall ?? throw new ArgumentNullException(nameof(rockfall));
            Haboob = haboob ?? throw new ArgumentNullException(nameof(haboob));
        }

        internal Action<CellCoord> ConsumeThorn { get; }
        internal Action<IReadOnlyList<CellCoord>> PresentHighlight { get; }
        internal Func<CellCoord, FieldGridMapper, CombatBody> ThornBody { get; }
        internal Func<ObjectSourceFact, FieldGridMapper, CombatBody> ObjectBody { get; }
        internal Action<long> HideObject { get; }
        internal Action<MovementTileFact, FieldGridMapper> ShowTerrainChange { get; }
        internal Action<TraitTrailFact, FieldGridMapper> ShowTrail { get; }
        internal Action<float> TickAppearance { get; }
        internal Func<bool> AppearancePlaying { get; }
        internal Func<bool> AppearanceBlocksStart { get; }
        internal Action CancelAppearance { get; }
        internal ICombatTornadoField Tornado { get; }
        internal ICombatRockfallField Rockfall { get; }
        internal ICombatHaboobField Haboob { get; }
    }
}
