using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 연출 큐가 붙는 사건 칸입니다(범용 연출 구조 설계 G3). 공격은 전달 종류(근접·원거리)에 따라 고를 수 있는 시점·자리가 다릅니다.
    /// 5 이후는 사실 사건·토네이도 칸이고 이펙트·흔들림만 받습니다(이펙트 창구 확장 설계 EC3, 계획 P4). 이 사건들의 소리는 사건 소리 표가 냅니다.
    /// </summary>
    internal enum CombatCueEvent
    {
        MeleeAttack = 1,
        RangedAttack = 2,
        Hit = 3,
        Death = 4,

        /// <summary>오브젝트 발동(지뢰)·습득(깃발·보안관 배지) 순간입니다. 자리는 오브젝트 칸입니다.</summary>
        ObjectActivated = 5,

        /// <summary>장애물(화약통·보급품)이 피해를 받은 순간입니다. 자리는 장애물 칸입니다.</summary>
        ObstacleHit = 6,

        /// <summary>장애물이 파괴된 순간입니다. 자리는 장애물 칸입니다.</summary>
        ObstacleDestroyed = 7,

        /// <summary>유닛·구조물이 낭떠러지로 떨어지기 시작하는 순간입니다. 자리는 떨어진 칸입니다.</summary>
        Fall = 8,

        /// <summary>약한 지반이 무너져 낭떠러지가 된 순간입니다. 자리는 무너진 칸입니다.</summary>
        WeakGroundCollapse = 9,

        /// <summary>아이템이 쓰인 순간입니다(중복 효과 포함). 자리는 소지자 몸입니다.</summary>
        ItemActivated = 10,

        /// <summary>토네이도에 휩쓸린 순간(Start)과 내려앉은 순간(End)입니다. 자리는 그 시점의 유닛 몸입니다.</summary>
        Tornado = 11,

        /// <summary>
        /// 특성이 발동한 순간입니다(특성 발동 연출 설계 TA2, 계획 Tb.3). 시점은 Start만이고, 이펙트 자리는 발동 유닛 몸(ActorBody)·대상 몸(SubjectBody, 대상마다)·
        /// 칸(EventCell, 칸마다)입니다. 추가 공격의 공격 동작 큐는 이 칸이 아니라 그 줄이 쓰는 공격 표시의 칸 규칙을 따릅니다.
        /// </summary>
        TraitActivation = 12,

        /// <summary>낙석 경고가 시작되는 순간입니다(낙석 View 계획 RP-3·RP-7). 시점은 Start만이고, 사건 칸 자리는 터널 자리입니다.</summary>
        RockfallWarning = 13,

        /// <summary>돌 하나가 칸에 닿는 순간입니다(낙석 View 계획 RP-3). 시점은 End(낙하 조각 끝)만이고, 자리는 그 칸입니다.</summary>
        RockfallImpact = 14,

        /// <summary>일반 Structure의 피격 순간입니다.</summary>
        StructureHit = 15,
    }

    /// <summary>
    /// 연출 큐 규칙의 원본입니다(범용 연출 구조 설계 G3, 계획 Gd.2). 사건마다 고를 수 있는 기준 시점·자리, 접촉 시점의 제약(계획 P5),
    /// 소리·플래시의 자리(계획 P15·P16)를 한 곳에 둡니다. 공격·반응 묶음과 사실 사건 칸의 값이 만들어질 때(DB 생성) 이 규칙을 지나고,
    /// 유닛마다 다른 조건(공격 클립·지점·피격 클립)은 외형 표 검사가 <see cref="FindUnitError"/>로 봅니다.
    /// </summary>
    /// <remarks>
    /// 기준 시점 표: 공격·피격·사망은 시작·끝, Cue는 공격 둘과 피격(피격 클립이 있을 때만), Cue2·3는 해당 이벤트가 있는 원거리 클립만입니다.
    /// 맞음(Hit)은 공격 둘, 접촉(Contact)은 원거리 공격만입니다.
    /// 사실 사건 칸 여섯은 시작(사건 순간)만, 토네이도는 시작(휩쓸림)·끝(착지)입니다(이펙트 창구 확장 설계 EC3). 특성 발동은 시작(발동 순간)만이고,
    /// 자리는 발동 유닛 몸·대상 몸·칸입니다(특성 발동 연출 설계 TA2). 낙석은 경고가 시작, 돌이 닿는 순간이 끝이고 자리는 사건 칸입니다(낙석 View 계획 RP-3).
    /// 접촉 시점에는 이펙트만 두고 지연은 0이며, 자리는 접촉점입니다(계획 P5). 자리는 공격이 행위자 쪽, 피격·사망·아이템·토네이도가 주체 몸,
    /// 나머지 사실 사건이 사건 칸입니다. 소리 시점은 공격·사망 칸에만 있습니다.
    /// 토네이도 칸의 큐는 지연이 0입니다(<see cref="TornadoNoDelayRule"/>).
    /// </remarks>
    internal static class CombatPresentationCueRules
    {
        /// <summary>
        /// 토네이도 칸 지연 규칙 문구입니다(이펙트 창구 확장 묶음 끝 검토 M1). 들어 올림 조립은 들어 올림이 끝나면 완료를 알리고,
        /// 완료 콜백이 착지 칸을 그립니다. 그때 지연된 큐가 남아 있으면 실행이 자세 대여를 쥐고 있어 그리기가 위치를 쓰지 않고,
        /// 큐가 끝나 대여가 돌아갈 때 유닛이 휩쓸리기 전 칸으로 되돌아갑니다.
        /// </summary>
        internal const string TornadoNoDelayRule =
            "토네이도 칸의 지연은 0이어야 합니다(착지 칸을 들어 올림이 끝나는 순간 그려서, 그 뒤에 남는 큐가 있으면 유닛이 휩쓸리기 전 칸으로 되돌아가 보입니다)";

        /// <summary>공격 표시 종류의 큐 칸입니다. 근접 = 투사체 없음, 나머지 = 원거리입니다.</summary>
        internal static CombatCueEvent AttackEventOf(CombatAttackPresentationKind kind) =>
            kind == CombatAttackPresentationKind.Melee ? CombatCueEvent.MeleeAttack : CombatCueEvent.RangedAttack;

        /// <summary>그 칸에서 그 기준 시점을 고를 수 있는지입니다. 피격의 기준 순간은 피격 클립이 있어야 하므로 유닛 검사가 더 봅니다.</summary>
        internal static bool AllowsAnchor(CombatCueEvent kind, CombatCueAnchor anchor) => kind switch
        {
            CombatCueEvent.MeleeAttack => anchor is CombatCueAnchor.Start or CombatCueAnchor.Cue or CombatCueAnchor.Hit or CombatCueAnchor.End,
            CombatCueEvent.RangedAttack => anchor is CombatCueAnchor.Start or CombatCueAnchor.Cue or CombatCueAnchor.Hit or CombatCueAnchor.Contact
                or CombatCueAnchor.End or CombatCueAnchor.Cue2 or CombatCueAnchor.Cue3,
            CombatCueEvent.Hit => anchor is CombatCueAnchor.Start or CombatCueAnchor.Cue or CombatCueAnchor.End,
            CombatCueEvent.Death => anchor is CombatCueAnchor.Start or CombatCueAnchor.End,
            CombatCueEvent.ObjectActivated or CombatCueEvent.ObstacleHit or CombatCueEvent.ObstacleDestroyed or CombatCueEvent.Fall
                or CombatCueEvent.WeakGroundCollapse or CombatCueEvent.ItemActivated or CombatCueEvent.TraitActivation
                or CombatCueEvent.RockfallWarning or CombatCueEvent.StructureHit => anchor == CombatCueAnchor.Start,
            CombatCueEvent.Tornado => anchor is CombatCueAnchor.Start or CombatCueAnchor.End,
            CombatCueEvent.RockfallImpact => anchor == CombatCueAnchor.End,
            _ => false,
        };

        /// <summary>그 칸·시점에서 이펙트 자리를 고를 수 있는지입니다. 접촉 시점과 접촉점은 서로만 맞습니다.</summary>
        internal static bool AllowsPoint(CombatCueEvent kind, CombatCueAnchor anchor, CombatCuePoint point)
        {
            if (anchor == CombatCueAnchor.Contact || point == CombatCuePoint.Contact)
                return anchor == CombatCueAnchor.Contact && point == CombatCuePoint.Contact;
            return kind switch
            {
                CombatCueEvent.MeleeAttack => point is CombatCuePoint.ActorEffectPoint or CombatCuePoint.ActorBody,
                CombatCueEvent.RangedAttack => point is CombatCuePoint.ActorEffectPoint or CombatCuePoint.ActorMuzzle or CombatCuePoint.ActorBody,
                CombatCueEvent.Hit or CombatCueEvent.Death or CombatCueEvent.ItemActivated or CombatCueEvent.Tornado or CombatCueEvent.StructureHit =>
                    point == CombatCuePoint.SubjectBody,
                CombatCueEvent.ObjectActivated or CombatCueEvent.ObstacleHit or CombatCueEvent.ObstacleDestroyed or CombatCueEvent.Fall
                    or CombatCueEvent.WeakGroundCollapse or CombatCueEvent.RockfallWarning or CombatCueEvent.RockfallImpact => point == CombatCuePoint.EventCell,
                CombatCueEvent.TraitActivation => point is CombatCuePoint.ActorBody or CombatCuePoint.SubjectBody or CombatCuePoint.EventCell,
                _ => false,
            };
        }

        /// <summary>큐 묶음이 그 칸의 규칙에 맞지 않는 첫 이유입니다. 맞으면 null입니다.</summary>
        internal static string FindError(CombatEventCues cues, CombatCueEvent kind)
        {
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            if (!System.Enum.IsDefined(typeof(CombatCueEvent), kind))
                return $"정의되지 않은 큐 칸입니다: {kind}";
            if (kind == CombatCueEvent.StructureHit && cues.Shakes.Count > 0)
                return "StructureHit 칸에는 흔들림을 둘 수 없습니다";
            for (int i = 0; i < cues.Effects.Count; i++)
            {
                CombatEffectCue effect = cues.Effects[i];
                if (kind == CombatCueEvent.StructureHit && effect.Delay != 0f)
                    return "StructureHit 이펙트 지연은 0이어야 합니다";
                if (!AllowsAnchor(kind, effect.Anchor))
                    return $"이펙트 큐 {i}: {kind} 칸은 시점 {effect.Anchor}를 쓸 수 없습니다";
                if (!AllowsPoint(kind, effect.Anchor, effect.Point))
                    return $"이펙트 큐 {i}: {kind} 칸·시점 {effect.Anchor}는 자리 {effect.Point}를 쓸 수 없습니다";
                if (effect.Anchor == CombatCueAnchor.Contact && effect.Delay != 0f)
                    return $"이펙트 큐 {i}: 접촉(Contact) 시점의 지연은 0이어야 합니다(닿을 때마다 그 순간에 냄)";
                if (kind == CombatCueEvent.Tornado && effect.Delay != 0f)
                    return $"이펙트 큐 {i}: {TornadoNoDelayRule}";
                // 행동 끝은 유닛 행동이 있는 특성 발동 줄에서만 끝이 옵니다(이펙트 끝 규칙 계획 TP-E2). 행동이 있는지는 줄 검사가 봅니다.
                if (effect.End == CombatEffectEnd.UnitAction && kind != CombatCueEvent.TraitActivation)
                    return $"이펙트 큐 {i}: 끝 UnitAction(행동 끝)은 특성 발동 줄의 큐에서만 고를 수 있습니다";
            }
            for (int i = 0; i < cues.Shakes.Count; i++)
            {
                CombatCueAnchor anchor = cues.Shakes[i].Anchor;
                // 접촉 시점에는 이펙트만 둡니다. 피해 후속을 여는 공격당 한 번의 흔들림은 Hit에 둡니다.
                if (anchor == CombatCueAnchor.Contact || !AllowsAnchor(kind, anchor))
                    return $"흔들림 큐 {i}: {kind} 칸은 시점 {anchor}를 쓸 수 없습니다";
                if (kind == CombatCueEvent.Tornado && cues.Shakes[i].Delay != 0f)
                    return $"흔들림 큐 {i}: {TornadoNoDelayRule}";
            }
            // 소리 시점은 공격·사망 칸에만 있습니다. 피격은 프리셋 소리 목록을 쓰고, 사실 사건·토네이도의 소리는 사건 소리 표가 냅니다
            // (같은 사실의 원본을 둘로 두지 않음, 이펙트 창구 확장 설계 EC3).
            bool needsSound = kind is CombatCueEvent.MeleeAttack or CombatCueEvent.RangedAttack or CombatCueEvent.Death;
            if (needsSound != (cues.SoundTimings.Count > 0))
                return needsSound ? $"{kind} 칸에는 소리 시점이 있어야 합니다" : $"{kind} 칸에는 소리 시점을 둘 수 없습니다";
            if (kind == CombatCueEvent.Death && cues.SoundTimings.Count != 1)
                return "사망 칸의 사건 소리 시점은 하나만 지정합니다";
            for (int i = 0; i < cues.SoundTimings.Count; i++)
            {
                CombatCueAnchor anchor = cues.SoundTimings[i].Anchor;
                if (anchor == CombatCueAnchor.Contact || !AllowsAnchor(kind, anchor))
                    return $"소리 시점 {i}: {kind} 칸은 시점 {anchor}를 쓸 수 없습니다";
            }
            // 프리셋을 가진 소리 큐는 피격 칸에서만 받습니다(투사체·피격 소리 설계 PS4, 계획 P7). 공격·사망 칸은 위의 소리 시점만 씁니다.
            if (cues.PresetSounds.Count > 0 && kind != CombatCueEvent.Hit)
                return $"{kind} 칸에는 소리 큐 목록을 둘 수 없습니다(피격만, 공격·사망은 소리 시점만)";
            for (int i = 0; i < cues.PresetSounds.Count; i++)
            {
                // 시점·자리 표는 이펙트 큐와 같습니다(결정 Q-PS3). 자리는 지금 소리 위치에 쓰이지 않지만 같은 조건으로 봅니다(계획 P3).
                CombatPresetSoundCue sound = cues.PresetSounds[i];
                if (!AllowsAnchor(kind, sound.Anchor))
                    return $"소리 큐 {i}: {kind} 칸은 시점 {sound.Anchor}를 쓸 수 없습니다";
                if (!AllowsPoint(kind, sound.Anchor, sound.Point))
                    return $"소리 큐 {i}: {kind} 칸·시점 {sound.Anchor}는 자리 {sound.Point}를 쓸 수 없습니다";
            }
            if (cues.Flash != null)
            {
                if (kind != CombatCueEvent.Hit)
                    return $"{kind} 칸에는 플래시를 둘 수 없습니다(피격만)";
                // 플래시는 시작·기준 순간만 받습니다(계획 P16).
                if (cues.Flash.Anchor != CombatCueAnchor.Start && cues.Flash.Anchor != CombatCueAnchor.Cue)
                    return $"플래시: 시점은 Start·Cue만 됩니다({cues.Flash.Anchor})";
            }
            return null;
        }

        /// <param name="context">어느 값의 큐인지 문구 앞에 붙일 말입니다(예: "[ObjectVisualData] Kind: Mine"). 없으면 붙이지 않습니다.</param>
        /// <exception cref="ArgumentException">규칙에 맞지 않을 때입니다. 문구에 이유가 있습니다.</exception>
        internal static void Require(CombatEventCues cues, CombatCueEvent kind, string context = null)
        {
            string error = FindError(cues, kind);
            if (error != null)
                throw new ArgumentException(string.IsNullOrEmpty(context) ? error : $"{context}: {error}", nameof(cues));
        }

        /// <summary>
        /// 유닛 하나의 큐 조건입니다(계획 P6·Gd.4). 행위자 지점 자리(이펙트 지점·발사 지점)는 공격 클립이 있고 프리팹에 그 지점이 있어야 하며,
        /// 피격의 기준 순간(Cue)은 피격 클립이 있어야 합니다. 맞으면 null입니다.
        /// </summary>
        /// <param name="attack">공격 칸 큐입니다. 공격 표시가 없으면 null입니다.</param>
        /// <param name="hit">피격 칸 큐입니다. 반응 묶음이 없으면 null입니다.</param>
        internal static string FindUnitError(CombatEventCues attack, CombatClipData attackClip, CombatAttackPoints points,
            CombatEventCues hit, bool hasHitClip)
        {
            if (attack != null)
            {
                for (int i = 0; i < attack.Effects.Count; i++)
                {
                    CombatEffectCue cue = attack.Effects[i];
                    if (cue.Point == CombatCuePoint.ActorEffectPoint &&
                        (attackClip == null || points == null || !points.TryGetEffectPoint(cue.ActorPoint, out _)))
                        return $"공격 이펙트 큐 {i}의 이펙트 지점 {cue.ActorPoint}가 없습니다. 공격 클립과 해당 effectPoint를 지정해야 합니다";
                    if (cue.Point == CombatCuePoint.ActorMuzzle &&
                        (attackClip == null || points == null || !points.TryGetMuzzle(cue.ActorPoint, out _)))
                        return $"공격 이펙트 큐 {i}의 총구 지점 {cue.ActorPoint}가 없습니다. 공격 클립과 해당 muzzle을 지정해야 합니다";
                }
                string error = FindEventError(attack, attackClip, CombatClipEventId.Event2)
                    ?? FindEventError(attack, attackClip, CombatClipEventId.Event3);
                if (error != null) return error;
            }
            if (hit != null && !hasHitClip && UsesAnchor(hit, CombatCueAnchor.Cue))
                return "피격 칸의 큐가 기준 순간(Cue)을 씁니다. 피격 클립이 있는 유닛만 됩니다";
            return null;
        }

        /// <summary>클립 이벤트에서 큐 시점으로의 대응입니다. 런타임 연결과 DB 존재 검사가 함께 사용합니다.</summary>
        internal static CombatCueAnchor AnchorOf(CombatClipEventId id) => id switch
        {
            CombatClipEventId.Event => CombatCueAnchor.Cue,
            CombatClipEventId.Event2 => CombatCueAnchor.Cue2,
            CombatClipEventId.Event3 => CombatCueAnchor.Cue3,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "정의되지 않은 클립 이벤트입니다."),
        };

        /// <summary>유닛마다 클립 존재를 확인해야 하는 선택 시점을 쓰는지입니다.</summary>
        internal static bool UsesAdditionalEvents(CombatEventCues cues) =>
            UsesAnchor(cues, AnchorOf(CombatClipEventId.Event2)) || UsesAnchor(cues, AnchorOf(CombatClipEventId.Event3));

        private static string FindEventError(CombatEventCues cues, CombatClipData clip, CombatClipEventId id)
        {
            CombatCueAnchor anchor = AnchorOf(id);
            if (!UsesAnchor(cues, anchor)) return null;
            return clip == null || !clip.Timings.TryGetEventTime(id, out _)
                ? $"공격 큐가 {anchor}를 쓰지만 해당 공격 클립 이벤트 {id}가 없습니다" : null;
        }

        /// <summary>
        /// 이펙트 큐가 행위자 지점 자리(이펙트 지점·발사 지점)를 하나라도 쓰는지입니다. 이 자리는 유닛마다 다른 조건(<see cref="FindUnitError"/>)이라,
        /// 어느 유닛이 쓸지 모르는 값(특성 추가 공격의 공격 표시 덮어쓰기)은 이 자리를 쓸 수 없습니다(특성 발동 연출 묶음 끝 검토 M2).
        /// </summary>
        internal static bool UsesActorPoint(CombatEventCues cues)
        {
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            for (int i = 0; i < cues.Effects.Count; i++)
            {
                CombatCuePoint point = cues.Effects[i].Point;
                if (point == CombatCuePoint.ActorEffectPoint || point == CombatCuePoint.ActorMuzzle)
                    return true;
            }
            return false;
        }

        /// <summary>기준 시점 열거가 정의된 값인지 봅니다(큐 값 생성자용).</summary>
        /// <exception cref="ArgumentOutOfRangeException">정의되지 않은 값일 때입니다.</exception>
        internal static void RequireDefined(CombatCueAnchor anchor)
        {
            if (!System.Enum.IsDefined(typeof(CombatCueAnchor), anchor))
                throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "정의된 기준 시점이어야 합니다.");
        }

        /// <summary>기준 시점·자리 열거가 정의된 값인지 봅니다(이펙트·프리셋 소리 큐 값 생성자용).</summary>
        /// <exception cref="ArgumentOutOfRangeException">정의되지 않은 값일 때입니다.</exception>
        internal static void RequireDefined(CombatCueAnchor anchor, CombatCuePoint point)
        {
            RequireDefined(anchor);
            if (!System.Enum.IsDefined(typeof(CombatCuePoint), point))
                throw new ArgumentOutOfRangeException(nameof(point), point, "정의된 이펙트 자리여야 합니다.");
        }

        private static bool UsesAnchor(CombatEventCues cues, CombatCueAnchor anchor)
        {
            for (int i = 0; i < cues.Effects.Count; i++)
                if (cues.Effects[i].Anchor == anchor) return true;
            for (int i = 0; i < cues.Shakes.Count; i++)
                if (cues.Shakes[i].Anchor == anchor) return true;
            for (int i = 0; i < cues.PresetSounds.Count; i++)
                if (cues.PresetSounds[i].Anchor == anchor) return true;
            for (int i = 0; i < cues.SoundTimings.Count; i++)
                if (cues.SoundTimings[i].Anchor == anchor) return true;
            return cues.Flash != null && cues.Flash.IsOn && cues.Flash.Anchor == anchor;
        }
    }
}
