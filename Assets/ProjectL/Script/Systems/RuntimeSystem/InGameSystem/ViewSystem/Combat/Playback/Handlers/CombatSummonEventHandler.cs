using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 소환 사건의 표시입니다(Presenter 분리 설계 S3, 보스 활성화 설계 D6b). 생성 칸 강조 → 생성 시점 사실만 보임 →
    /// 바닥 아래에서 돌며 올라옴 → 강조 끔. 등장 효과·사망은 뒤따르는 사건이 반영합니다.
    /// 순서는 소환 조립(<see cref="Compose"/>) 한 곳이 원본이고, 조각 실행 재생기가 진행합니다(조각·조립 설계 BP7).
    /// 유닛 표시가 없으면 자세를 빌리지 못해 등장 동작 없이, 틀이 강조를 보인 채 행동 간격만큼 머문 뒤 완료합니다.
    /// </summary>
    internal sealed class CombatSummonEventHandler : CombatEventHandler
    {
        private readonly CombatShownFacts _facts;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatAbilityMotionSettings _abilityMotion;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatPlaybackHighlight _highlight;

        internal CombatSummonEventHandler(CombatShownFacts facts, CombatPieceRunPlayer pieces, CombatAbilityMotionSettings abilityMotion,
            Func<FieldGridMapper> mapper, CombatPlaybackHighlight highlight)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _abilityMotion = abilityMotion ?? throw new ArgumentNullException(nameof(abilityMotion));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _highlight = highlight ?? throw new ArgumentNullException(nameof(highlight));
        }

        internal override void CollectHighlight(CombatEvent e, List<CellCoord> cells) =>
            CombatPlaybackHighlight.AddOccupiedCells(cells, ((CombatSummonEvent)e).Created);

        // 소환은 등장 효과 전 생성 시점 사실만 보인다. 등장 효과·사망은 뒤따르는 사건이 반영한다.
        internal override void ApplyFacts(CombatEvent e) => _facts.ApplyCharacter(((CombatSummonEvent)e).Created);

        /// <summary>소환 조립을 실행합니다. 끝나면 재생 강조를 끄고 완료합니다. 유닛 표시가 없으면 false입니다.</summary>
        internal override bool TryBeginAfterFacts(CombatEvent e, Action completed, int version)
        {
            var summoned = (CombatSummonEvent)e;
            CombatComposition composition = Compose(summoned.Created.EntityId, _abilityMotion, _mapper().WorldUp, _highlight);
            return _pieces.TryBegin(composition, completed, version);
        }

        /// <summary>
        /// 소환 조립입니다(조각·조립 설계 BP7). 행동 실행, 일반 완료, 자원은 새 유닛 자세입니다.
        /// 0 등장(기다림) → 1 강조 끄기(기다림). 둘 다 끝나면 완료하고, 전체 종료 때 자세를 되돌립니다.
        /// </summary>
        internal static CombatComposition Compose(EntityId created, CombatAbilityMotionSettings settings, Vector3 worldUp,
            CombatPlaybackHighlight highlight)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var composition = CombatComposition.ForAction(CombatPieceCompletion.General).Lease(created, CombatPieceChannel.Pose);
            var rise = new CombatRisePiece(created, settings.SummonRiseDelaySeconds, settings.SummonRiseSeconds, settings.SummonRiseDepth,
                settings.SummonRiseTurns, worldUp);
            int riseItem = composition.Add(rise, CombatPieceStart.AtStart(), waits: true);
            composition.Add(new CombatHighlightClearPiece(highlight), CombatPieceStart.After(riseItem), waits: true);
            return composition;
        }
    }
}
