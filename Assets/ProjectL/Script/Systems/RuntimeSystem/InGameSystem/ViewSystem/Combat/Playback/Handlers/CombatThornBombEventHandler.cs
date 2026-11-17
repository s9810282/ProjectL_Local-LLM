using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// Core 사건 순서에 맞춰 부착과 폭발을 표시합니다. 폭발 연출은 가시 폭탄 TraitVisual 줄의 발동 줄(<see cref="TraitActivationKind.ThornBombDetonated"/>)
    /// 큐이고, 자리는 폭심지 칸입니다(선택 칸·영향 칸 모두 폭심지, 가시 폭탄 Phase 4 D3). 줄이 없으면 다른 특성 발동처럼 아무것도 내지 않습니다.
    /// 소리는 특성 발동 줄에 칸이 없어 아직 없습니다(소리 창구가 열리면 연결).
    /// </summary>
    internal sealed class CombatThornBombEventHandler : CombatEventHandler
    {
        private static readonly IReadOnlyList<CombatCuePoints.Body> NoBodies = Array.Empty<CombatCuePoints.Body>();

        private readonly ThornBombAttachmentPresenter _attachments;
        private readonly CombatActivationRows _rows;
        private readonly CombatMomentCuePlayback _cues;
        private readonly Func<FieldGridMapper> _mapper;

        internal CombatThornBombEventHandler(ThornBombAttachmentPresenter attachments, CombatActivationRows rows,
            CombatMomentCuePlayback cues, Func<FieldGridMapper> mapper)
        {
            _attachments = attachments ?? throw new ArgumentNullException(nameof(attachments));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _cues = cues ?? throw new ArgumentNullException(nameof(cues));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        internal override void CollectHighlight(CombatEvent e, List<CellCoord> cells)
        {
            if (e is CombatThornBombDetonatedEvent explosion)
                cells.Add(explosion.Center);
        }

        internal override void ApplyFacts(CombatEvent e)
        {
            switch (e)
            {
                case CombatThornBombAttachedEvent attached:
                    _attachments.Attach(attached.Bomb);
                    break;
                case CombatThornBombDetonatedEvent explosion:
                    _attachments.Remove(explosion.Bomb.BombId);
                    PlayExplosion(explosion);
                    break;
                case CombatThornBombDiscardedEvent discarded:
                    _attachments.Remove(discarded.Bomb.BombId);
                    break;
            }
        }

        private void PlayExplosion(CombatThornBombDetonatedEvent explosion)
        {
            CombatActivationRow row = _rows.Of(TraitKey.ThornBomb, TraitActivationKind.ThornBombDetonated);
            if (!row.Exists)
                return;
            Pose center = CombatCuePoints.CellPose(_mapper(), explosion.Center);
            _cues.PlayAccent(row.Cues, CombatCuePoints.ForActivation(explosion.Bomb.SourceId, null, NoBodies, new[] { center }, center));
        }
    }
}
