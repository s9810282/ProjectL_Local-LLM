using System;
using System.Collections.Generic;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 연출 큐가 붙는 사건 칸입니다(범용 연출 구조 설계 G3). 공격은 전달 종류(근접·원거리)에 따라 고를 수 있는 시점·자리가 다릅니다.
    /// 5 이후는 사실 사건·토네이도·특성 발동·낙석·구조물 피격 칸이고 이펙트·흔들림만 받습니다(이펙트 창구 확장 설계 EC3, 계획 P4). 구조물 피격(StructureHit)은 흔들림 없이 지연 0 이펙트만 받습니다. 이 사건들의 소리는 사건 소리 표가 냅니다.
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
        /// 칸(EventCell, 칸마다)·선택 칸(SelectedCell, 한 번)입니다. 추가 공격의 공격 동작 큐는 이 칸이 아니라 그 줄이 쓰는 공격 표시의 칸 규칙을 따릅니다.
        /// </summary>
        TraitActivation = 12,

        /// <summary>낙석 경고가 시작되는 순간입니다(낙석 View 계획 RP-3·RP-7). 시점은 Start만이고, 사건 칸 자리는 터널 자리입니다.</summary>
        RockfallWarning = 13,

        /// <summary>돌 하나가 칸에 닿는 순간입니다(낙석 View 계획 RP-3). 시점은 End(낙하 조각 끝)만이고, 자리는 그 칸입니다.</summary>
        RockfallImpact = 14,

        /// <summary>일반 Structure의 피격 순간입니다.</summary>
        StructureHit = 15,

        /// <summary>
        /// 플레이어 스킬 영역 사건(투사체 경로가 없는 사건 51)의 칸마다 내는 큐입니다(전투 스킬 View 설계 Q-SK4). 맞는 순간(Hit)만이고 자리는 그 칸입니다.
        /// </summary>
        PlayerSkillCell = 16,
    }

    /// <summary>
    /// 연출 큐 규칙의 원본입니다(범용 연출 구조 설계 G3, 계획 Gd.2). 사건마다 고를 수 있는 기준 시점·자리, 접촉 시점의 제약(계획 P5),
    /// 소리·플래시의 자리(계획 P15·P16)를 한 곳에 둡니다. 공격·반응 묶음과 사실 사건 칸의 값이 만들어질 때(DB 생성) 이 규칙을 지나고,
    /// 유닛마다 다른 조건(공격 클립·지점·피격 클립)은 외형 표 검사가 <see cref="FindUnitError"/>로 봅니다.
    /// </summary>
    /// <remarks>
    /// 기준 시점 표: 공격·피격·사망은 시작·끝, 클립 이벤트 시점(Event1~3, 번호 표 <see cref="CombatClipEventIds"/>)은 공격 둘과 피격이고, 그 칸 클립의 이벤트 목록에
    /// 등록된 번호만 됩니다(유닛 검사 <see cref="FindUnitError"/>, 클립 칸 다시 짜기 설계 CS3). 코드 모션 공격의 Event1은 타격·발사 순간입니다.
    /// 접촉(Contact)은 공격 둘이고, 어느 접촉에 낼지(<see cref="CombatContactFilter"/>)는 큐 칸이 고릅니다: Every = 접촉마다(이펙트만·접촉점·지연 0),
    /// FirstHit = 공격당 한 번 첫 실제 피격(옛 맞음 Hit, 설계 CS10). 플레이어 스킬 칸은 Contact·FirstHit만입니다.
    /// 사실 사건 칸 여섯은 시작(사건 순간)만, 토네이도는 시작(휩쓸림)·끝(착지)입니다(이펙트 창구 확장 설계 EC3). 특성 발동은 시작(발동 순간)만이고,
    /// 자리는 발동 유닛 몸·대상 몸·칸입니다(특성 발동 연출 설계 TA2). 낙석은 경고가 시작, 돌이 닿는 순간이 끝이고 자리는 사건 칸입니다(낙석 View 계획 RP-3).
    /// 구조물 피격(StructureHit)은 시작만이고 자리는 주체 몸이며, 흔들림을 둘 수 없고 이펙트 지연은 0이어야 합니다.
    /// 접촉의 Every에는 이펙트만 두고 지연은 0이며, 자리는 접촉점입니다(계획 P5). 자리는 공격이 행위자 쪽, 피격·사망·아이템·토네이도·구조물 피격이 주체 몸,
    /// 나머지 사실 사건이 사건 칸입니다. 큐에는 소리가 없습니다(공격·피격·사망 소리는 유닛 외형의 소리 줄, 유닛 소리 설계 US2).
    /// 토네이도 칸의 큐는 지연이 0입니다(<see cref="TornadoNoDelayRule"/>).
    /// 플레이어 스킬 영역 칸(PlayerSkillCell)은 접촉의 FirstHit(맞는 순간)만이고 자리는 그 칸이며, 흔들림은 둘 수 없습니다 — 사건 하나에 한 번만 내도록 공격 묶음 큐에 둡니다(전투 스킬 View 설계 Q-SK4).
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

        /// <summary>
        /// 그 칸에서 그 기준 시점(접촉이면 고르기 포함)을 고를 수 있는지입니다. 클립 이벤트 시점은 그 칸 클립에 등록됐는지를 유닛 검사가 더 봅니다.
        /// </summary>
        internal static bool AllowsAnchor(CombatCueEvent kind, CombatCueAnchor anchor, CombatContactFilter filter) => kind switch
        {
            CombatCueEvent.MeleeAttack or CombatCueEvent.RangedAttack => anchor is CombatCueAnchor.Start or CombatCueAnchor.Contact or CombatCueAnchor.End
                || CombatClipEventIds.IsEventAnchor(anchor),
            CombatCueEvent.Hit => anchor is CombatCueAnchor.Start or CombatCueAnchor.End || CombatClipEventIds.IsEventAnchor(anchor),
            CombatCueEvent.Death => anchor is CombatCueAnchor.Start or CombatCueAnchor.End,
            CombatCueEvent.ObjectActivated or CombatCueEvent.ObstacleHit or CombatCueEvent.ObstacleDestroyed or CombatCueEvent.Fall
                or CombatCueEvent.WeakGroundCollapse or CombatCueEvent.ItemActivated or CombatCueEvent.TraitActivation
                or CombatCueEvent.RockfallWarning or CombatCueEvent.StructureHit => anchor == CombatCueAnchor.Start,
            CombatCueEvent.Tornado => anchor is CombatCueAnchor.Start or CombatCueAnchor.End,
            CombatCueEvent.RockfallImpact => anchor == CombatCueAnchor.End,
            CombatCueEvent.PlayerSkillCell => IsFirstHit(anchor, filter),
            _ => false,
        };

        /// <summary>접촉마다(앞 접촉 포함) 내는 시점인지입니다. 조립 항목이 아니라 접촉 입구가 바로 냅니다.</summary>
        internal static bool IsEveryContact(CombatCueAnchor anchor, CombatContactFilter filter) =>
            anchor == CombatCueAnchor.Contact && filter == CombatContactFilter.Every;

        /// <summary>공격당 한 번 첫 실제 피격 접촉(옛 맞음 Hit)에 내는 시점인지입니다. 조립 항목(해제 신호 뒤)이라 지연을 받습니다.</summary>
        internal static bool IsFirstHit(CombatCueAnchor anchor, CombatContactFilter filter) =>
            anchor == CombatCueAnchor.Contact && filter == CombatContactFilter.FirstHit;

        /// <summary>
        /// 큐 값 생성자가 고르기를 보관하기 전에 부릅니다. 접촉 시점이면 정의된 값이어야 하고, 접촉이 아니면 뜻이 없어 <see cref="CombatContactFilter.Every"/>로 둡니다.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">접촉 시점인데 정의되지 않은 고르기일 때입니다.</exception>
        internal static CombatContactFilter RequireFilter(CombatCueAnchor anchor, CombatContactFilter filter)
        {
            if (anchor != CombatCueAnchor.Contact)
                return CombatContactFilter.Every;
            if (!System.Enum.IsDefined(typeof(CombatContactFilter), filter))
                throw new ArgumentOutOfRangeException(nameof(filter), filter, "접촉 시점은 정의된 고르기(Every·FirstHit)여야 합니다.");
            return filter;
        }

        /// <summary>그 칸·시점에서 이펙트 자리를 고를 수 있는지입니다. 접촉의 Every와 접촉점은 서로만 맞습니다.</summary>
        internal static bool AllowsPoint(CombatCueEvent kind, CombatCueAnchor anchor, CombatContactFilter filter, CombatCuePoint point)
        {
            bool every = IsEveryContact(anchor, filter);
            if (every || point == CombatCuePoint.Contact)
                return every && point == CombatCuePoint.Contact;
            return kind switch
            {
                CombatCueEvent.MeleeAttack or CombatCueEvent.RangedAttack => point is CombatCuePoint.ActorEventPoint or CombatCuePoint.ActorBody,
                CombatCueEvent.Hit or CombatCueEvent.Death or CombatCueEvent.ItemActivated or CombatCueEvent.Tornado or CombatCueEvent.StructureHit =>
                    point == CombatCuePoint.SubjectBody,
                CombatCueEvent.ObjectActivated or CombatCueEvent.ObstacleHit or CombatCueEvent.ObstacleDestroyed or CombatCueEvent.Fall
                    or CombatCueEvent.WeakGroundCollapse or CombatCueEvent.RockfallWarning or CombatCueEvent.RockfallImpact
                    or CombatCueEvent.PlayerSkillCell => point == CombatCuePoint.EventCell,
                CombatCueEvent.TraitActivation => point is CombatCuePoint.ActorBody or CombatCuePoint.SubjectBody or CombatCuePoint.EventCell
                    or CombatCuePoint.SelectedCell,
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
            // 칸마다 내는 큐라 흔들림을 두면 칸 수만큼 흔들립니다. 흔들림은 공격 묶음 큐에 둡니다(전투 스킬 View 설계 Q-SK4, SK-a 검토 M).
            if (kind == CombatCueEvent.PlayerSkillCell && cues.Shakes.Count > 0)
                return "PlayerSkillCell 칸에는 흔들림을 둘 수 없습니다(칸마다 흔들리므로 공격 묶음 큐에 두세요)";
            for (int i = 0; i < cues.Effects.Count; i++)
            {
                CombatEffectCue effect = cues.Effects[i];
                if (kind == CombatCueEvent.StructureHit && effect.Delay != 0f)
                    return "StructureHit 이펙트 지연은 0이어야 합니다";
                if (!AllowsAnchor(kind, effect.Anchor, effect.ContactFilter))
                    return $"이펙트 큐 {i}: {kind} 칸은 시점 {NameOf(effect.Anchor, effect.ContactFilter)}를 쓸 수 없습니다";
                if (!AllowsPoint(kind, effect.Anchor, effect.ContactFilter, effect.Point))
                    return $"이펙트 큐 {i}: {kind} 칸·시점 {NameOf(effect.Anchor, effect.ContactFilter)}는 자리 {effect.Point}를 쓸 수 없습니다";
                if (IsEveryContact(effect.Anchor, effect.ContactFilter) && effect.Delay != 0f)
                    return $"이펙트 큐 {i}: 접촉(Contact)의 Every는 지연이 0이어야 합니다(닿을 때마다 그 순간에 냄)";
                if (kind == CombatCueEvent.Tornado && effect.Delay != 0f)
                    return $"이펙트 큐 {i}: {TornadoNoDelayRule}";
                // 행동 끝은 유닛 행동이 있는 특성 발동 줄에서만 끝이 옵니다(이펙트 끝 규칙 계획 TP-E2). 행동이 있는지는 줄 검사가 봅니다.
                if (effect.End == CombatEffectEnd.UnitAction && kind != CombatCueEvent.TraitActivation)
                    return $"이펙트 큐 {i}: 끝 UnitAction(행동 끝)은 특성 발동 줄의 큐에서만 고를 수 있습니다";
            }
            for (int i = 0; i < cues.Shakes.Count; i++)
            {
                CombatShakeCue shake = cues.Shakes[i];
                // 접촉의 Every에는 이펙트만 둡니다. 공격당 한 번의 흔들림은 접촉의 FirstHit에 둡니다.
                if (IsEveryContact(shake.Anchor, shake.ContactFilter) || !AllowsAnchor(kind, shake.Anchor, shake.ContactFilter))
                    return $"흔들림 큐 {i}: {kind} 칸은 시점 {NameOf(shake.Anchor, shake.ContactFilter)}를 쓸 수 없습니다";
                if (kind == CombatCueEvent.Tornado && cues.Shakes[i].Delay != 0f)
                    return $"흔들림 큐 {i}: {TornadoNoDelayRule}";
            }
            // 소리는 큐에 없습니다. 공격·피격·사망 소리는 유닛 외형의 소리 줄(검사는 FindUnitSoundError), 사실 사건·토네이도의 소리는 사건 소리 표가 냅니다
            // (같은 사실의 원본을 둘로 두지 않음, 이펙트 창구 확장 설계 EC3, 유닛 소리 설계 US2).
            if (cues.Flash != null)
            {
                if (kind != CombatCueEvent.Hit)
                    return $"{kind} 칸에는 플래시를 둘 수 없습니다(피격만)";
                // 플래시는 시작·기준 순간만 받습니다(계획 P16).
                if (cues.Flash.Anchor != CombatCueAnchor.Start && cues.Flash.Anchor != CombatCueAnchor.Event1)
                    return $"플래시: 시점은 Start·Event1만 됩니다({cues.Flash.Anchor})";
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
        /// 유닛 하나의 큐 조건입니다(계획 P6·Gd.4, 클립 칸 다시 짜기 설계 CS3·CS4). 이벤트 지점 자리(<see cref="CombatCuePoint.ActorEventPoint"/>)는 공격 클립이 있고
        /// 프리팹에 그 번호의 지점이 있어야 하며, 클립 이벤트 시점(Event1~3)은 그 칸 본 단계 몸 클립에 그 표식이 있어야 합니다(코드 모션 공격은 Event1만, 이벤트 줄 목록과는 상관없음 — 11-17 사용자). 맞으면 null입니다.
        /// </summary>
        /// <param name="attack">공격 칸 큐입니다. 공격 표시가 없으면 null입니다.</param>
        /// <param name="attackMotion">공격 칸 클립입니다. 공격 클립이 없으면(코드 모션) null입니다.</param>
        /// <param name="points">이벤트 지점입니다. 이벤트 컴포넌트가 없으면 null입니다.</param>
        /// <param name="hit">피격 칸 큐입니다. 반응 묶음이 없으면 null입니다.</param>
        /// <param name="hitMotion">피격 칸 클립입니다. 피격 클립이 없으면 null입니다.</param>
        internal static string FindUnitError(CombatEventCues attack, CombatClipMotion attackMotion, CombatEventPoints points,
            CombatEventCues hit, CombatClipMotion hitMotion)
        {
            if (attack != null)
            {
                for (int i = 0; i < attack.Effects.Count; i++)
                {
                    CombatEffectCue cue = attack.Effects[i];
                    if (cue.Point == CombatCuePoint.ActorEventPoint &&
                        (attackMotion == null || points == null || !points.TryGet(cue.EventPoint, out _)))
                        return $"공격 이펙트 큐 {i}의 이벤트 지점 {cue.EventPoint}가 없습니다. 공격 클립과 이벤트 컴포넌트의 Event Points {cue.EventPoint}번 칸을 지정해야 합니다";
                }
                string error = FindEventAnchorError(attack, attackMotion, codeMotionAllowsFirst: true, "공격");
                if (error != null) return error;
            }
            if (hit != null)
            {
                string error = FindEventAnchorError(hit, hitMotion, codeMotionAllowsFirst: false, "피격");
                if (error != null) return error;
            }
            return null;
        }

        // 큐가 쓰는 클립 이벤트 시점마다 그 칸 본 단계 몸 클립에 그 표식이 있는지 봅니다.
        private static string FindEventAnchorError(CombatEventCues cues, CombatClipMotion motion, bool codeMotionAllowsFirst, string slot)
        {
            IReadOnlyList<CombatClipEventId> ids = CombatClipEventIds.All;
            for (int i = 0; i < ids.Count; i++)
            {
                if (!UsesAnchor(cues, CombatClipEventIds.AnchorOf(ids[i]))) continue;
                string error = FindClipEventError(ids[i], motion, codeMotionAllowsFirst, $"{slot} 칸의 큐", slot);
                if (error != null) return error;
            }
            return null;
        }

        /// <summary>
        /// 유닛 소리 줄의 유닛 조건입니다(유닛 소리 계획 TP-3). 맞으면 null입니다.
        /// 공격 줄 = 그 유닛 공격 표시 종류의 시점(접촉 Every 제외), 피격 줄 = 피격 칸 시점, 사망 줄 = 사망 칸 시점이고, Event1~3은 그 칸 본 단계 몸 클립에 표식이 있어야 합니다(큐와 같은 검사).
        /// 줄이 쓰이지 않는 조합은 조용히 안 나지 않게 막습니다: 공격 표시가 없는 공격 줄·반응 묶음이 없는 사망 줄은 사건 순간에만 나서 Start · 지연 0만 받고,
        /// 반응 묶음이 없는 피격 줄은 피격 조립이 없어 받지 않습니다(계획 검토 R6).
        /// </summary>
        /// <param name="attack">그 유닛의 공격 표시입니다. 없으면 null입니다.</param>
        /// <param name="attackMotion">공격 칸 클립입니다. 없으면(코드 모션) null입니다.</param>
        /// <param name="hasReaction">피격·사망 반응 묶음이 있는지입니다.</param>
        /// <param name="hitMotion">피격 칸 클립입니다. 없으면 null입니다.</param>
        internal static string FindUnitSoundError(CombatUnitSoundsData sounds, CombatAttackPresentationData attack, CombatClipMotion attackMotion,
            bool hasReaction, CombatClipMotion hitMotion)
        {
            if (sounds == null) throw new ArgumentNullException(nameof(sounds));
            for (int i = 0; i < sounds.Attack.Count; i++)
            {
                CombatUnitSoundCue row = sounds.Attack[i];
                string user = $"공격 소리 {i}번 줄";
                string error = attack == null
                    ? FindMomentOnlyError(row, user, "공격 표시(attackPresentation)가 없어 사건 순간에만 납니다")
                    : FindSoundAnchorError(row, AttackEventOf(attack.Kind), attackMotion, codeMotionAllowsFirst: true, user, "공격");
                if (error != null) return error;
            }
            for (int i = 0; i < sounds.Hit.Count; i++)
            {
                string user = $"피격 소리 {i}번 줄";
                if (!hasReaction)
                    return $"{user} — 반응 묶음(reactionPresentation)이 없는 유닛은 피격 조립이 없어 피격 소리가 나지 않습니다";
                string error = FindSoundAnchorError(sounds.Hit[i], CombatCueEvent.Hit, hitMotion, codeMotionAllowsFirst: false, user, "피격");
                if (error != null) return error;
            }
            for (int i = 0; i < sounds.Death.Count; i++)
            {
                CombatUnitSoundCue row = sounds.Death[i];
                string user = $"사망 소리 {i}번 줄";
                // 사망 칸은 클립 이벤트 시점을 받지 않아(AllowsAnchor) 클립 표식을 볼 일이 없습니다.
                string error = hasReaction
                    ? FindSoundAnchorError(row, CombatCueEvent.Death, null, codeMotionAllowsFirst: false, user, "사망")
                    : FindMomentOnlyError(row, user, "반응 묶음(reactionPresentation)이 없어 사건 순간에만 납니다");
                if (error != null) return error;
            }
            return null;
        }

        /// <summary>
        /// 공격 소리 줄을 그 공격의 표시 종류·클립에 맞춥니다(유닛 소리 계획 TP-4, 결정 Q-US3 A). 그 공격이 받는 시점이면 줄 그대로, 아니면 Event1(타격·발사 순간, 지연 그대로)로 바꿉니다.
        /// 스킬·특성 발동 줄이 공격 표시를 덮어쓰면 시전자 줄이 그 종류에 맞지 않을 수 있습니다. 자기 공격 표시는 외형 검사를 지나 늘 그대로입니다.
        /// 허용 판정은 외형 검사(<see cref="FindUnitSoundError"/>)와 같은 함수입니다. 바뀐 줄이 없으면 받은 목록을 그대로 돌려줍니다(할당 없음).
        /// </summary>
        /// <param name="kind">그 공격의 큐 칸입니다(<see cref="AttackEventOf"/>).</param>
        /// <param name="motion">그 공격의 클립 동작입니다. 코드 모션이면 null입니다.</param>
        internal static IReadOnlyList<CombatUnitSoundCue> AttackSoundsFor(IReadOnlyList<CombatUnitSoundCue> rows, CombatCueEvent kind, CombatClipMotion motion)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (kind != CombatCueEvent.MeleeAttack && kind != CombatCueEvent.RangedAttack)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "공격 칸이어야 합니다.");
            CombatUnitSoundCue[] fitted = null;
            for (int i = 0; i < rows.Count; i++)
            {
                CombatUnitSoundCue row = rows[i];
                if (FindSoundAnchorError(row, kind, motion, codeMotionAllowsFirst: true, string.Empty, string.Empty) == null)
                {
                    if (fitted != null) fitted[i] = row;
                    continue;
                }
                if (fitted == null)
                {
                    fitted = new CombatUnitSoundCue[rows.Count];
                    for (int j = 0; j < i; j++)
                        fitted[j] = rows[j];
                }
                fitted[i] = new CombatUnitSoundCue(row.Sound, CombatCueAnchor.Event1, CombatContactFilter.Every, row.Delay);
            }
            return fitted ?? rows;
        }

        // 소리 줄 하나가 그 칸에서 고를 수 있는 시점이고, 클립 이벤트 시점이면 그 표식이 있는지 봅니다. 접촉마다(Every)는 조립 항목이 아니라 소리를 받지 않습니다(옛 소리 시점과 같음).
        private static string FindSoundAnchorError(CombatUnitSoundCue row, CombatCueEvent kind, CombatClipMotion motion, bool codeMotionAllowsFirst,
            string user, string slot)
        {
            if (IsEveryContact(row.Anchor, row.ContactFilter) || !AllowsAnchor(kind, row.Anchor, row.ContactFilter))
                return $"{user} — {slot} 소리는 시점 {NameOf(row.Anchor, row.ContactFilter)}를 쓸 수 없습니다";
            IReadOnlyList<CombatClipEventId> ids = CombatClipEventIds.All;
            for (int i = 0; i < ids.Count; i++)
            {
                if (CombatClipEventIds.AnchorOf(ids[i]) == row.Anchor)
                    return FindClipEventError(ids[i], motion, codeMotionAllowsFirst, user, slot);
            }
            return null;
        }

        // 연출 없이 사건 순간에만 나는 줄은 시점·지연이 쓰이지 않아 Start · 지연 0만 받습니다.
        private static string FindMomentOnlyError(CombatUnitSoundCue row, string user, string reason) =>
            row.Anchor == CombatCueAnchor.Start && row.Delay == 0f
                ? null
                : $"{user} — {reason}. 시점은 Start, 지연은 0만 됩니다(지금 {NameOf(row.Anchor, row.ContactFilter)}, {row.Delay}초)";

        // 클립 이벤트 시점 하나를 그 칸 본 단계 몸 클립의 표식으로 봅니다(큐·유닛 소리 줄이 같이 씀). 클립이 없는 칸은 코드 모션 공격의 Event1(타격·발사 순간)만 됩니다.
        private static string FindClipEventError(CombatClipEventId id, CombatClipMotion motion, bool codeMotionAllowsFirst, string user, string slot)
        {
            CombatCueAnchor anchor = CombatClipEventIds.AnchorOf(id);
            if (motion == null)
                return codeMotionAllowsFirst && id == CombatClipEventIds.Primary
                    ? null
                    : $"{user} — 시점 {anchor}는 {slot} 클립이 있고 본 단계 몸 클립에 그 이벤트 표식이 있는 유닛만 됩니다";
            return motion.HasEvent(id)
                ? null
                : $"{user} — 시점 {anchor}의 표식({CombatClipEventIds.FunctionNameOf(id)})이 {slot} 본 단계 몸 클립에 없습니다. 클립: {motion.Main.Clip.name}";
        }

        /// <summary>
        /// 유닛마다 표식을 확인해야 하는 이벤트 시점(기준 이벤트 Event1 다음 번호)을 쓰는지입니다. Event1 표식은 본 단계 몸 클립에 늘 있어(클립 규칙) 어느 유닛이든 됩니다.
        /// </summary>
        internal static bool UsesAdditionalEvents(CombatEventCues cues)
        {
            IReadOnlyList<CombatClipEventId> ids = CombatClipEventIds.All;
            for (int i = 1; i < ids.Count; i++)
                if (UsesAnchor(cues, CombatClipEventIds.AnchorOf(ids[i]))) return true;
            return false;
        }

        /// <summary>
        /// 이펙트 큐가 이벤트 지점 자리를 하나라도 쓰는지입니다. 이 자리는 유닛마다 다른 조건(<see cref="FindUnitError"/>)이라,
        /// 어느 유닛이 쓸지 모르는 값(특성 추가 공격의 공격 표시 덮어쓰기)은 이 자리를 쓸 수 없습니다(특성 발동 연출 묶음 끝 검토 M2).
        /// </summary>
        internal static bool UsesActorPoint(CombatEventCues cues)
        {
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            for (int i = 0; i < cues.Effects.Count; i++)
                if (cues.Effects[i].Point == CombatCuePoint.ActorEventPoint)
                    return true;
            return false;
        }

        /// <summary>오류 문구용 시점 이름입니다. 접촉이면 고르기를 붙입니다.</summary>
        private static string NameOf(CombatCueAnchor anchor, CombatContactFilter filter) =>
            anchor == CombatCueAnchor.Contact ? $"{anchor}({filter})" : anchor.ToString();

        /// <summary>기준 시점 열거가 정의된 값인지 봅니다(큐 값 생성자용).</summary>
        /// <exception cref="ArgumentOutOfRangeException">정의되지 않은 값일 때입니다.</exception>
        internal static void RequireDefined(CombatCueAnchor anchor)
        {
            if (!System.Enum.IsDefined(typeof(CombatCueAnchor), anchor))
                throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "정의된 기준 시점이어야 합니다.");
        }

        /// <summary>기준 시점·자리 열거가 정의된 값인지 봅니다(이펙트 큐 값 생성자용).</summary>
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
            // 세기 0 흔들림은 런타임 조립이 건너뛰므로(CombatCueComposer) 사용으로 세지 않습니다(평가 C10-21).
            for (int i = 0; i < cues.Shakes.Count; i++)
                if (cues.Shakes[i].IsOn && cues.Shakes[i].Anchor == anchor) return true;
            return cues.Flash != null && cues.Flash.IsOn && cues.Flash.Anchor == anchor;
        }
    }
}
