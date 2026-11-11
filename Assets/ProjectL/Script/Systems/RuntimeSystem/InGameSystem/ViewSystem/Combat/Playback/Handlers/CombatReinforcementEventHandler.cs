using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>적 본부 증원을 생성 사실로 보인 뒤 기존 소환 등장 액션을 재사용합니다. 후속 효과와 사망은 기존 사건 처리기가 맡습니다.</summary>
    internal sealed class CombatReinforcementEventHandler : CombatEventHandler
    {
        private readonly CombatShownFacts _facts;
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatAbilityMotionSettings _abilityMotion;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatPlaybackHighlight _highlight;

        internal CombatReinforcementEventHandler(CombatShownFacts facts, CombatPieceRunPlayer pieces,
            CombatAbilityMotionSettings abilityMotion, Func<FieldGridMapper> mapper, CombatPlaybackHighlight highlight)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _abilityMotion = abilityMotion ?? throw new ArgumentNullException(nameof(abilityMotion));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _highlight = highlight ?? throw new ArgumentNullException(nameof(highlight));
        }

        internal override void CollectHighlight(CombatEvent e, List<CellCoord> cells) =>
            CombatPlaybackHighlight.AddOccupiedCells(cells, ((CombatReinforcementEvent)e).Created);

        internal override void ApplyFacts(CombatEvent e) =>
            _facts.ApplyCharacter(((CombatReinforcementEvent)e).Created);

        internal override bool TryBeginAfterFacts(CombatEvent e, Action completed, int version)
        {
            var created = ((CombatReinforcementEvent)e).Created;
            var composition = CombatSummonEventHandler.Compose(created.EntityId, _abilityMotion, _mapper().WorldUp, _highlight);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            composition.ProfilingEvent = e;
#endif
            return _pieces.TryBegin(composition, completed, version);
        }
    }
}
