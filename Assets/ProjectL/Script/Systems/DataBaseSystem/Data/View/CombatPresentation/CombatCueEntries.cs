using System;
using Cinemachine;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>큐 입력 칸 여럿이 같이 쓰는 툴팁 문구입니다.</summary>
    internal static class CombatCueTooltips
    {
        internal const string ContactFilter =
            "접촉(Contact) 시점에서 어느 접촉에 낼지입니다. Every = 접촉마다(앞 접촉 포함, 이펙트만·자리 Contact·지연 0), "
            + "FirstHit = 공격당 한 번, 첫 실제 피격 접촉(옛 Hit). 접촉이 아닌 시점에서는 보지 않습니다.";
    }

    /// <summary>
    /// 이벤트 지점 자리(<see cref="CombatCuePoint.ActorEventPoint"/>)의 회전입니다(클립 칸 다시 짜기 설계 CS4, 결정 Q-A4).
    /// 0(<see cref="None"/>)은 "아직 고르지 않음"이고 이벤트 지점 자리에서는 검사가 거절합니다. 옮기기 도구는 이 값으로 옮기지 않은 옛 큐를 알아봅니다(계획 TP-6).
    /// </summary>
    internal enum CombatEventPointRotation
    {
        /// <summary>아직 고르지 않음입니다. 이벤트 지점 자리에서는 쓸 수 없습니다.</summary>
        None = 0,

        /// <summary>이벤트 지점 Transform의 회전입니다(옛 ActorEffectPoint).</summary>
        Point = 1,

        /// <summary>행위자 자세 방향입니다. 지점은 위치만 씁니다(옛 ActorMuzzle).</summary>
        Actor = 2,
    }

    // 연출 큐(범용 연출 구조 설계 G2: 이펙트·흔들림·플래시)의 입력 칸과 불변 값입니다. 소리는 큐가 아니라 유닛 외형의 소리 줄(CombatUnitSounds.cs)입니다(유닛 소리 설계 US2).
    // 큐 종류마다 칸이 달라 종류별 형식을 둡니다(다형 직렬화를 쓰지 않음).
    // 기다림 여부는 큐 종류가 정합니다: 플래시만 사건 완료를 기다리고, 이펙트·흔들림은 기다리지 않습니다.
    // 수 조건의 원본은 불변 값의 생성자이고, 사건·자리 조합은 큐 규칙(CombatPresentationCueRules)이 봅니다.

    /// <summary>이펙트 큐 입력입니다.</summary>
    [Serializable]
    internal sealed class CombatEffectCueEntry
    {
        [Tooltip("낼 이펙트입니다. Render DB 이펙트 표에 등록돼 있어야 합니다.")]
        [SerializeField] private EffectType effect = EffectType.Empty;
        [SerializeField] private CombatCueAnchor anchor = CombatCueAnchor.Start;
        [Tooltip(CombatCueTooltips.ContactFilter)]
        [SerializeField] private CombatContactFilter contactFilter = CombatContactFilter.Every;
        [Tooltip("기준 시점 뒤 이펙트까지의 초입니다. 접촉(Contact)의 Every는 0만 됩니다.")]
        [SerializeField, Min(0f)] private float delay;
        [SerializeField] private CombatCuePoint point = CombatCuePoint.SubjectBody;
        [Tooltip("ActorEventPoint에서 쓸 이벤트 지점 번호(CombatAnimationEvents의 Event Points 목록 칸)입니다. 기본은 0이며 클립 이벤트 번호와 별개입니다. 다른 자리는 0만 됩니다.")]
        [FormerlySerializedAs("actorPoint")]
        [SerializeField, Min(0)] private int eventPoint;
        [Tooltip("ActorEventPoint의 회전입니다. Point = 이벤트 지점 Transform의 회전, Actor = 행위자 자세 방향(지점은 위치만 씀). 다른 자리에서는 보지 않습니다.")]
        [SerializeField] private CombatEventPointRotation eventPointRotation = CombatEventPointRotation.None;
        [Tooltip("켜면 낸 자리에 남깁니다. 끄면(기본) 몸 자리(ActorBody·SubjectBody) 이펙트가 그 유닛의 위치를 따라갑니다(위치만, 회전은 낼 때 그대로). "
            + "따라가던 유닛 표시가 사라지면 이펙트도 바로 멈춥니다. 다른 자리는 늘 낸 자리에 남습니다.")]
        [SerializeField] private bool stayInPlace;
        [Tooltip("자리 기준 위치 보정입니다. x = 오른쪽, y = 위, z = 그 자리의 앞이고, 앞이 무엇인지는 자리마다 다릅니다(자리 설명 참고). "
            + "월드 단위이며 필드 배율을 따르지 않습니다. 0이면 자리 그대로입니다.")]
        [SerializeField] private Vector3 positionOffset;
        [Tooltip("이펙트를 놓는 유닛이 Rider일 때 Position Offset에 더할 월드 Y 보정입니다. 양수면 위로 올립니다. ActorBody·ActorEventPoint는 행위자, SubjectBody는 각 대상의 병과를 따릅니다. EventCell·SelectedCell·Contact에는 적용하지 않습니다.")]
        [SerializeField] private float riderYOffset;
        [Tooltip("자리 회전 뒤에 더하는 회전입니다(오일러 각, 도). 예: 위로 뿜는 이펙트를 바닥에 눕히려면 (-90, 0, 0). 0이면 자리 회전 그대로입니다.")]
        [SerializeField] private Vector3 rotationOffset;
        [Tooltip("언제 끝나나입니다(이펙트 끝 규칙 설계 EL1). Natural(기본) = 프리팹이 스스로 멈출 때, Seconds = 낸 뒤 End Seconds가 지나면 거둠, "
            + "UnitAction = 그 발동이 연 유닛 행동이 모두 끝나면 거둠(유닛 행동이 있는 특성 발동 줄에서만). "
            + "상태가 걸려 있는 동안·보유하는 동안 보이는 이펙트는 여기가 아니라 상태 외형·특성 상시 목록에 넣습니다.")]
        [SerializeField] private CombatEffectEnd end = CombatEffectEnd.Natural;
        [Tooltip("End가 Seconds일 때 낸 뒤 거둘 때까지의 초입니다(0보다 커야 함). 다른 끝에서는 쓰지 않습니다.")]
        [SerializeField, Min(0f)] private float endSeconds;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatEffectCueEntry()
        {
        }

        internal CombatEffectCueEntry(EffectType effect, CombatCueAnchor anchor, float delay, CombatCuePoint point,
            Vector3 positionOffset = default, Vector3 rotationOffset = default, bool stayInPlace = false,
            CombatEffectEnd end = CombatEffectEnd.Natural, float endSeconds = 0f, int eventPoint = 0, float riderYOffset = 0f,
            CombatContactFilter contactFilter = CombatContactFilter.Every, CombatEventPointRotation eventPointRotation = CombatEventPointRotation.Point)
        {
            this.contactFilter = contactFilter;
            this.eventPointRotation = eventPointRotation;
            this.effect = effect;
            this.anchor = anchor;
            this.delay = delay;
            this.point = point;
            this.positionOffset = positionOffset;
            this.riderYOffset = riderYOffset;
            this.rotationOffset = rotationOffset;
            this.stayInPlace = stayInPlace;
            this.end = end;
            this.endSeconds = endSeconds;
            this.eventPoint = eventPoint;
        }

        internal CombatEffectCue ToCue() => new(effect, anchor, delay, point, positionOffset, rotationOffset, stayInPlace, end, endSeconds, eventPoint,
            riderYOffset, contactFilter, eventPointRotation);
    }

    /// <summary>카메라 흔들림 큐 입력입니다. 값은 Framework 카메라 효과(<c>CameraShakeData</c>)를 만들 칸만 둡니다(설계 G2).</summary>
    [Serializable]
    internal sealed class CombatShakeCueEntry
    {
        [Tooltip("흔들림 세기입니다(아래 방향 속도의 크기). 0이면 흔들림이 없습니다.")]
        [SerializeField, Min(0f)] private float strength;
        [Tooltip("흔들림 시간(초)입니다. 조건은 카메라 효과가 봅니다.")]
        [SerializeField] private float seconds = .15f;
        [Tooltip("흔들림 모양입니다. 조건은 카메라 효과가 봅니다.")]
        [SerializeField] private CinemachineImpulseDefinition.ImpulseShapes shape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        [SerializeField] private CombatCueAnchor anchor = CombatCueAnchor.Start;
        [Tooltip(CombatCueTooltips.ContactFilter)]
        [SerializeField] private CombatContactFilter contactFilter = CombatContactFilter.Every;
        [SerializeField, Min(0f)] private float delay;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatShakeCueEntry()
        {
        }

        internal CombatShakeCueEntry(float strength, float seconds, CinemachineImpulseDefinition.ImpulseShapes shape, CombatCueAnchor anchor,
            float delay, CombatContactFilter contactFilter = CombatContactFilter.Every)
        {
            this.strength = strength;
            this.seconds = seconds;
            this.shape = shape;
            this.anchor = anchor;
            this.delay = delay;
            this.contactFilter = contactFilter;
        }

        internal CombatShakeCue ToCue() => new(strength, seconds, shape, anchor, delay, contactFilter);
    }

    /// <summary>피격 플래시 입력입니다(계획 P16). 피격 칸에 하나이고, 시간 0이면 없습니다. 시점은 시작(Start)·Event1만 됩니다.</summary>
    [Serializable]
    internal sealed class CombatFlashCueEntry
    {
        [Tooltip("모델이 이 색으로 바뀌었다가 원래 색으로 돌아옵니다(ColorSystem 손잡이). 알파는 쓰지 않습니다(모델의 원래 알파를 따름).")]
        [SerializeField] private Color color = Color.red;
        [Tooltip("피격 동안 모델 외곽선(Cartoon 재질 Outline 패스)이 이 색이 됐다가 몸 색과 같이 원래 외곽선으로 돌아옵니다. 외곽선은 조명을 받지 않아 몸 색보다 진하게 보이고, HDR 밝기가 1보다 크면 블룸으로 빛납니다. 알파는 쓰지 않습니다.")]
        [SerializeField, ColorUsage(false, true)] private Color outlineColor = Color.red;
        [Tooltip("피격 동안의 외곽선 폭(월드 단위)입니다. 0이면 외곽선은 바꾸지 않습니다(몸 색만). 재질 기본 폭은 0.015~0.02입니다.")]
        [SerializeField, Min(0f)] private float outlineWidth;
        [Tooltip("원래 색으로 돌아오는 시간(초)입니다. 0이면 플래시가 없습니다. 피격 반응보다 길면 그 유닛의 다음 사건이 늦어집니다.")]
        [SerializeField, Min(0f)] private float seconds;
        [SerializeField] private CombatCueAnchor anchor = CombatCueAnchor.Start;
        [SerializeField, Min(0f)] private float delay;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatFlashCueEntry()
        {
        }

        internal CombatFlashCueEntry(Color color, float seconds, CombatCueAnchor anchor = CombatCueAnchor.Start, float delay = 0f,
            Color outlineColor = default, float outlineWidth = 0f)
        {
            this.color = color;
            this.outlineColor = outlineColor;
            this.outlineWidth = outlineWidth;
            this.seconds = seconds;
            this.anchor = anchor;
            this.delay = delay;
        }

        internal CombatFlashCue ToCue() => new(new CombatFlashLook(color, outlineColor, outlineWidth), seconds, anchor, delay);
    }

    /// <summary>
    /// 이펙트 큐 값입니다. 기다리지 않습니다. 위치·회전 보정은 자리 기준 값이고(이펙트 창구 확장 설계 EC1, 계획 P1), 어떤 값이든 뜻이 있어 검사하지 않습니다.
    /// 보정을 자리에 적용하는 규칙은 View의 큐 자리 해석 한 곳이 맡습니다(계획 P2).
    /// </summary>
    internal sealed class CombatEffectCue
    {
        /// <exception cref="ArgumentOutOfRangeException">값이 규칙에 맞지 않을 때입니다.</exception>
        internal CombatEffectCue(EffectType effect, CombatCueAnchor anchor, float delay, CombatCuePoint point,
            Vector3 positionOffset = default, Vector3 rotationOffset = default, bool stayInPlace = false,
            CombatEffectEnd end = CombatEffectEnd.Natural, float endSeconds = 0f, int eventPoint = 0, float riderYOffset = 0f,
            CombatContactFilter contactFilter = CombatContactFilter.Every, CombatEventPointRotation eventPointRotation = CombatEventPointRotation.Point)
        {
            if (!CombatPresentationEffectRules.IsPlayable(effect))
                throw new ArgumentOutOfRangeException(nameof(effect), effect, CombatPresentationEffectRules.PlayableRule);
            CombatPresentationCueRules.RequireDefined(anchor, point);
            ContactFilter = CombatPresentationCueRules.RequireFilter(anchor, contactFilter);
            CombatPresentationValueRules.RequireNonNegative(delay, nameof(delay));
            CombatPresentationValueRules.RequireFinite(riderYOffset, nameof(riderYOffset));
            if (eventPoint < 0)
                throw new ArgumentOutOfRangeException(nameof(eventPoint), eventPoint, "이벤트 지점 번호는 0 이상이어야 합니다.");
            if (eventPoint != 0 && point != CombatCuePoint.ActorEventPoint)
                throw new ArgumentException("이벤트 지점 번호는 ActorEventPoint에서만 지정합니다.", nameof(eventPoint));
            if (point == CombatCuePoint.ActorEventPoint &&
                eventPointRotation is not (CombatEventPointRotation.Point or CombatEventPointRotation.Actor))
                throw new ArgumentOutOfRangeException(nameof(eventPointRotation), eventPointRotation,
                    "이벤트 지점 자리는 회전(Point·Actor)을 골라야 합니다(옛 큐면 메뉴 ProjectL > Combat > 클립 칸 옮기기 (1)을 실행하세요).");
            // 회전 칸은 ActorEventPoint에서만 뜻이 있습니다. 다른 자리의 값은 보관하지 않습니다(입력 칸에 남아 있어도 무시).
            EventPointRotation = point == CombatCuePoint.ActorEventPoint ? eventPointRotation : CombatEventPointRotation.Point;
            if (!System.Enum.IsDefined(typeof(CombatEffectEnd), end))
                throw new ArgumentOutOfRangeException(nameof(end), end, "정의된 이펙트 끝이어야 합니다.");
            // 시간 끝만 초를 씁니다. 다른 끝의 초는 뜻이 없어 보관하지 않습니다(입력 칸에 남아 있어도 무시).
            if (end == CombatEffectEnd.Seconds)
                CombatPresentationValueRules.RequirePositive(endSeconds, nameof(endSeconds));
            Effect = effect;
            Anchor = anchor;
            Delay = delay;
            Point = point;
            EventPoint = eventPoint;
            PositionOffset = positionOffset;
            RiderYOffset = riderYOffset;
            RotationOffset = Quaternion.Euler(rotationOffset);
            StaysInPlace = stayInPlace;
            End = end;
            EndSeconds = end == CombatEffectEnd.Seconds ? endSeconds : 0f;
        }

        internal EffectType Effect { get; }
        internal CombatCueAnchor Anchor { get; }
        internal float Delay { get; }
        internal CombatCuePoint Point { get; }

        /// <summary>어느 접촉에 낼지입니다. 접촉 시점이 아니면 <see cref="CombatContactFilter.Every"/>(보지 않음)입니다.</summary>
        internal CombatContactFilter ContactFilter { get; }

        /// <summary>행위자의 이벤트 지점 번호입니다(<see cref="CombatCuePoint.ActorEventPoint"/>). 다른 자리에서는 0입니다.</summary>
        internal int EventPoint { get; }

        /// <summary>이벤트 지점 자리의 회전입니다. 다른 자리에서는 <see cref="CombatEventPointRotation.Point"/>(보지 않음)입니다.</summary>
        internal CombatEventPointRotation EventPointRotation { get; }

        /// <summary>낸 자리에 남기는지입니다(입력 칸 <c>stayInPlace</c>). 거짓이면 몸 자리 이펙트는 유닛을 따라갑니다(<see cref="FollowsUnit"/>).</summary>
        internal bool StaysInPlace { get; }

        /// <summary>
        /// 낸 뒤 그 유닛의 위치를 따라가는지입니다(이펙트 따라가기 설계 FE1). 몸 자리(행위자 몸·주체 몸)이고 남기기가 꺼져 있을 때만 참이고,
        /// 다른 자리(지점·칸·접촉)는 늘 낸 자리에 남습니다. 이 판단의 원본은 이 속성 하나입니다.
        /// </summary>
        internal bool FollowsUnit => !StaysInPlace && Point is CombatCuePoint.ActorBody or CombatCuePoint.SubjectBody;

        /// <summary>자리 기준 위치 보정입니다(x 오른쪽·y 위·z 자리의 앞, 월드 단위).</summary>
        internal Vector3 PositionOffset { get; }

        /// <summary>이펙트를 놓는 유닛이 Rider일 때 더할 월드 Y 보정입니다. 유닛이 없는 칸·접촉 자리는 적용하지 않습니다.</summary>
        internal float RiderYOffset { get; }

        /// <summary>자리 회전 뒤에 더하는 회전입니다. 오일러 각 입력을 값을 만들 때 한 번 바꿉니다.</summary>
        internal Quaternion RotationOffset { get; }

        /// <summary>언제 끝나나입니다(이펙트 끝 규칙 설계 EL1). 창구의 놓기 값으로 바꾸는 곳은 View의 한 함수입니다(계획 TP-E26).</summary>
        internal CombatEffectEnd End { get; }

        /// <summary>시간 끝(<see cref="CombatEffectEnd.Seconds"/>)의 초입니다. 다른 끝이면 0입니다.</summary>
        internal float EndSeconds { get; }
    }

    /// <summary>카메라 흔들림 큐 값입니다. 기다리지 않습니다. 세기 0이면 흔들림이 없습니다.</summary>
    internal sealed class CombatShakeCue
    {
        /// <exception cref="ArgumentOutOfRangeException">세기·지연·시점이 규칙에 맞지 않을 때입니다. 시간·모양 조건의 원본은 카메라 효과입니다(카메라 연출 조각 설계 K5 결정 A).</exception>
        internal CombatShakeCue(float strength, float seconds, CinemachineImpulseDefinition.ImpulseShapes shape, CombatCueAnchor anchor, float delay,
            CombatContactFilter contactFilter = CombatContactFilter.Every)
        {
            CombatPresentationValueRules.RequireNonNegative(strength, nameof(strength));
            CombatPresentationCueRules.RequireDefined(anchor);
            ContactFilter = CombatPresentationCueRules.RequireFilter(anchor, contactFilter);
            CombatPresentationValueRules.RequireNonNegative(delay, nameof(delay));
            Strength = strength;
            Seconds = seconds;
            Shape = shape;
            Anchor = anchor;
            Delay = delay;
        }

        internal float Strength { get; }
        internal float Seconds { get; }
        internal CinemachineImpulseDefinition.ImpulseShapes Shape { get; }
        internal CombatCueAnchor Anchor { get; }
        internal CombatContactFilter ContactFilter { get; }
        internal float Delay { get; }

        /// <summary>흔들림이 있는지입니다(세기가 0보다 큼).</summary>
        internal bool IsOn => Strength > 0f;
    }

    /// <summary>
    /// 피격 플래시가 세기 1일 때의 모습입니다(몸 색·외곽선 색·외곽선 폭). 원래 모습과 세기만큼 섞는 규칙은 유닛 외형 소유자 한 곳이 갖습니다(조각·조립 설계 BP9 보충).
    /// 시점 값은 갖지 않아, 외형 대여는 언제 그리는지 모른 채 모습만 받습니다.
    /// </summary>
    internal readonly struct CombatFlashLook
    {
        internal CombatFlashLook(Color body, Color outline, float outlineWidth)
        {
            CombatPresentationValueRules.RequireNonNegative(outlineWidth, nameof(outlineWidth));
            Body = body;
            Outline = outline;
            OutlineWidth = outlineWidth;
        }

        internal Color Body { get; }
        internal Color Outline { get; }
        internal float OutlineWidth { get; }

        /// <summary>외곽선을 바꾸는지입니다(폭이 0보다 큼). 아니면 몸 색만 바뀝니다.</summary>
        internal bool HasOutline => OutlineWidth > 0f;
    }

    /// <summary>피격 플래시 값입니다. 사건 완료를 기다립니다. 시간 0이면 없습니다.</summary>
    internal sealed class CombatFlashCue
    {
        internal CombatFlashCue(CombatFlashLook look, float seconds, CombatCueAnchor anchor, float delay)
        {
            CombatPresentationValueRules.RequireNonNegative(seconds, nameof(seconds));
            CombatPresentationCueRules.RequireDefined(anchor);
            CombatPresentationValueRules.RequireNonNegative(delay, nameof(delay));
            Look = look;
            Seconds = seconds;
            Anchor = anchor;
            Delay = delay;
        }

        internal CombatFlashLook Look { get; }
        internal float Seconds { get; }
        internal CombatCueAnchor Anchor { get; }
        internal float Delay { get; }

        /// <summary>플래시가 있는지입니다(시간이 0보다 큼).</summary>
        internal bool IsOn => Seconds > 0f;
    }
}
