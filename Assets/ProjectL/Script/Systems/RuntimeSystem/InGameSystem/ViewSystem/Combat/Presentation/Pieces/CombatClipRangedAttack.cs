using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 원거리 클립의 집계·클립·이벤트 줄별 발사를 조립합니다(클립 칸 다시 짜기 설계 CS2 — 줄마다 한 발, 지점 = 발사 지점). 공통 방향/대여/가로채기는 기존 공격 조립이 맡습니다.
    /// </summary>
    internal static class CombatClipRangedAttack
    {
        // 입구의 인자는 CombatClipAttack.ComposeRanged가 검사합니다.
        internal static void Add(CombatComposition composition, EntityId attacker, CombatClipAttackData clip, CombatPieceStart start,
            Func<Vector3, Action<EntityId, bool>, Action, ICombatShot> fire, Action<EntityId> releaseTarget, Action release,
            Vector3? launchAimPoint, out int played, out int hit, out IReadOnlyList<CombatProjectileCueTiming> projectileTimings,
            Func<bool> canRelease = null, Action<int> contactFeedback = null)
        {
            IReadOnlyList<CombatClipEventEntry> shots = clip.Events;
            var contacts = new CombatVolleyReleasePiece(shots.Count, releaseTarget, release, canRelease, contactFeedback);
            // 같은 시작 조건의 항목은 목록 순서대로 시작합니다. 0초 클립 이벤트도 집계 준비 뒤에 냅니다.
            hit = composition.Add(contacts, start);
            played = composition.Add(new CombatClipPiece(attacker, clip.Motion, WeaponAim(clip, launchAimPoint)), start);
            var timings = new CombatProjectileCueTiming[shots.Count];
            for (int i = 0; i < shots.Count; i++)
            {
                CombatClipEventEntry shot = shots[i];
                CombatPieceStart flight = CombatPieceStart.After(played, CombatClipEventSignals.SignalOf(shot.EventId));
                int fired = composition.Add(new CombatFirePiece(attacker, shot.Point, fire, contacts.ContactCallback(i)), flight);
                timings[i] = new CombatProjectileCueTiming(flight, CombatPieceStart.After(fired, CombatPieceSignal.Contact));
            }
            projectileTimings = Array.AsReadOnly(timings);
        }

        private static CombatWeaponAim? WeaponAim(CombatClipAttackData clip, Vector3? launchAimPoint)
        {
            if (!launchAimPoint.HasValue)
                return null;

            float firstShot = clip.Clip.EndTime;
            float lastShot = 0f;
            int firstPoint = 0;
            foreach (CombatClipEventEntry shot in clip.Events)
            {
                if (!clip.Clip.Timings.TryGetEventTime(shot.EventId, out float time))
                    throw new InvalidOperationException($"[CombatClipRangedAttack] 발사 이벤트 시각이 없습니다: {shot.EventId}");
                if (time < firstShot)
                {
                    firstShot = time;
                    firstPoint = shot.Point;
                }
                lastShot = Mathf.Max(lastShot, time);
            }
            return new CombatWeaponAim(launchAimPoint.Value, firstShot, lastShot, clip.Clip.EndTime, firstPoint);
        }
    }
}
