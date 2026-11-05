using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>원거리 클립의 집계·클립·번호별 발사를 조립합니다. 공통 방향/대여/가로채기는 기존 공격 조립이 맡습니다.</summary>
    internal static class CombatClipRangedAttack
    {
        // 입구의 인자는 CombatClipAttack.ComposeRanged가 검사합니다.
        internal static void Add(CombatComposition composition, EntityId attacker, CombatClipAttackData clip, CombatPieceStart start,
            Func<Vector3, Action, Action, ICombatShot> fire, Action release, out int played, out int hit,
            out IReadOnlyList<CombatProjectileCueTiming> projectileTimings)
        {
            var contacts = new CombatVolleyReleasePiece(clip.Shots.Count, release);
            // 같은 시작 조건의 항목은 목록 순서대로 시작합니다. 0초 클립 이벤트도 집계 준비 뒤에 냅니다.
            hit = composition.Add(contacts, start);
            played = composition.Add(new CombatClipPiece(attacker, clip.Clip), start);
            var timings = new CombatProjectileCueTiming[clip.Shots.Count];
            for (int i = 0; i < clip.Shots.Count; i++)
            {
                CombatClipShot shot = clip.Shots[i];
                CombatPieceStart flight = CombatPieceStart.After(played, SignalOf(shot.EventId));
                int fired = composition.Add(new CombatFirePiece(attacker, shot.Point, fire, contacts.ContactCallback(i)), flight);
                timings[i] = new CombatProjectileCueTiming(flight, CombatPieceStart.After(fired, CombatPieceSignal.Contact));
            }
            projectileTimings = Array.AsReadOnly(timings);
        }

        /// <summary>실제 존재하는 선택 이벤트만 큐에 연결합니다. 근접·피격 조립은 이 확장을 호출하지 않습니다.</summary>
        internal static void ConnectEventCues(CombatCueAnchors anchors, CombatClipData clip, int played)
        {
            for (int i = 1; i < clip.Timings.Events.Count; i++)
            {
                CombatClipEventId id = clip.Timings.Events[i].Id;
                anchors.Set(CombatPresentationCueRules.AnchorOf(id), played, SignalOf(id));
            }
        }

        /// <summary>DB 이벤트 식별자에서 재생 신호로의 대응입니다. 정수값 일치를 가정하지 않습니다.</summary>
        internal static CombatPieceSignal SignalOf(CombatClipEventId id) => id switch
        {
            CombatClipEventId.Event => CombatPieceSignal.ClipEvent,
            CombatClipEventId.Event2 => CombatPieceSignal.ClipEvent2,
            CombatClipEventId.Event3 => CombatPieceSignal.ClipEvent3,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "정의되지 않은 클립 이벤트입니다."),
        };
    }
}
