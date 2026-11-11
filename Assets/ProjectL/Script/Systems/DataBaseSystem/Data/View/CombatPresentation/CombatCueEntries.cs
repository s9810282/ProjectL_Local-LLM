using System;
using Cinemachine;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    // 연출 큐(범용 연출 구조 설계 G2: 이펙트·흔들림·소리 시점·플래시, 투사체·피격 소리 설계 PS4: 프리셋 소리)의 입력 칸과 불변 값입니다.
    // 큐 종류마다 칸이 달라 종류별 형식을 둡니다(다형 직렬화를 쓰지 않음).
    // 기다림 여부는 큐 종류가 정합니다: 플래시만 사건 완료를 기다리고, 이펙트·흔들림·소리는 기다리지 않습니다.
    // 수 조건의 원본은 불변 값의 생성자이고, 사건·자리 조합은 큐 규칙(CombatPresentationCueRules)이 봅니다.

    /// <summary>이펙트 큐 입력입니다.</summary>
    [Serializable]
    internal sealed class CombatEffectCueEntry
    {
        [Tooltip("낼 이펙트입니다. Render DB 이펙트 표에 등록돼 있어야 합니다.")]
        [SerializeField] private EffectType effect = EffectType.Empty;
        [SerializeField] private CombatCueAnchor anchor = CombatCueAnchor.Start;
        [Tooltip("기준 시점 뒤 이펙트까지의 초입니다. 접촉(Contact) 시점은 0만 됩니다.")]
        [SerializeField, Min(0f)] private float delay;
        [SerializeField] private CombatCuePoint point = CombatCuePoint.SubjectBody;
        [Tooltip("ActorMuzzle·ActorEffectPoint에서 사용할 지점 번호입니다. 기본은 0이며 이벤트 번호와 별개입니다. 다른 자리는 0만 됩니다.")]
        [SerializeField, Min(0)] private int actorPoint;
        [Tooltip("켜면 낸 자리에 남깁니다. 끄면(기본) 몸 자리(ActorBody·SubjectBody) 이펙트가 그 유닛의 위치를 따라갑니다(위치만, 회전은 낼 때 그대로). "
            + "따라가던 유닛 표시가 사라지면 이펙트도 바로 멈춥니다. 다른 자리는 늘 낸 자리에 남습니다.")]
        [SerializeField] private bool stayInPlace;
        [Tooltip("자리 기준 위치 보정입니다. x = 오른쪽, y = 위, z = 그 자리의 앞이고, 앞이 무엇인지는 자리마다 다릅니다(자리 설명 참고). "
            + "월드 단위이며 필드 배율을 따르지 않습니다. 0이면 자리 그대로입니다.")]
        [SerializeField] private Vector3 positionOffset;
        [Tooltip("이펙트를 놓는 유닛이 Rider일 때 Position Offset에 더할 월드 Y 보정입니다. 양수면 위로 올립니다. ActorBody·ActorMuzzle·ActorEffectPoint는 행위자, SubjectBody는 각 대상의 병과를 따릅니다. EventCell·SelectedCell·Contact에는 적용하지 않습니다.")]
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
            CombatEffectEnd end = CombatEffectEnd.Natural, float endSeconds = 0f, int actorPoint = 0, float riderYOffset = 0f)
        {
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
            this.actorPoint = actorPoint;
        }

        internal CombatEffectCue ToCue() => new(effect, anchor, delay, point, positionOffset, rotationOffset, stayInPlace, end, endSeconds, actorPoint, riderYOffset);
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
        [SerializeField, Min(0f)] private float delay;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatShakeCueEntry()
        {
        }

        internal CombatShakeCueEntry(float strength, float seconds, CinemachineImpulseDefinition.ImpulseShapes shape, CombatCueAnchor anchor,
            float delay)
        {
            this.strength = strength;
            this.seconds = seconds;
            this.shape = shape;
            this.anchor = anchor;
            this.delay = delay;
        }

        internal CombatShakeCue ToCue() => new(strength, seconds, shape, anchor, delay);
    }

    /// <summary>
    /// 사건 소리 시점 입력입니다(계획 P15). 무엇을 낼지는 사건 소리 표가 정하고, 이 칸은 언제 낼지만 정합니다. 공격·사망 칸에 늘 있습니다.
    /// 낼지는 지금처럼 소리 프리셋(-1 = 없음)이 정합니다.
    /// </summary>
    [Serializable]
    internal sealed class CombatSoundTimingCueEntry
    {
        [SerializeField] private CombatCueAnchor anchor = CombatCueAnchor.Start;
        [SerializeField, Min(0f)] private float delay;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatSoundTimingCueEntry()
        {
        }

        internal CombatSoundTimingCueEntry(CombatCueAnchor anchor, float delay = 0f)
        {
            this.anchor = anchor;
            this.delay = delay;
        }

        internal CombatSoundTimingCue ToCue() => new(anchor, delay);
    }

    /// <summary>
    /// 프리셋을 가진 소리 큐 입력입니다(투사체·피격 소리 설계 PS4, 결정 Q-PS2·Q-PS3). 이펙트 큐와 같은 칸(시점·지연·자리)에 무엇을 낼지(프리셋)를 직접 듭니다.
    /// 지금은 피격 칸에서만 받습니다. 공격·사망 소리는 시점만 정하고 프리셋은 유닛 외형이 갖습니다(<see cref="CombatSoundTimingCueEntry"/>).
    /// </summary>
    [Serializable]
    internal sealed class CombatPresetSoundCueEntry
    {
        [Tooltip("낼 소리의 프리셋 번호입니다(효과음 슬롯 이벤트의 변종). 0 이상이어야 합니다. 소리를 끄려면 이 항목을 지웁니다.")]
        [SerializeField, Min(0)] private int preset;
        [SerializeField] private CombatCueAnchor anchor = CombatCueAnchor.Start;
        [Tooltip("기준 시점 뒤 소리까지의 초입니다. 길게 주면 그만큼 그 유닛의 다음 사건이 늦어집니다(연출이 소리까지 기다림).")]
        [SerializeField, Min(0f)] private float delay;
        [Tooltip("소리 자리입니다. 지금 효과음은 위치 없이 나므로 이 값은 소리에 쓰이지 않습니다(3D 소리를 붙일 때 씀). 피격 칸은 주체 몸(SubjectBody)만 됩니다.")]
        [SerializeField] private CombatCuePoint point = CombatCuePoint.SubjectBody;

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatPresetSoundCueEntry()
        {
        }

        internal CombatPresetSoundCueEntry(int preset, CombatCueAnchor anchor, float delay, CombatCuePoint point)
        {
            this.preset = preset;
            this.anchor = anchor;
            this.delay = delay;
            this.point = point;
        }

        internal CombatPresetSoundCue ToCue() => new(preset, anchor, delay, point);
    }

    /// <summary>피격 플래시 입력입니다(계획 P16). 피격 칸에 하나이고, 시간 0이면 없습니다. 시점은 시작(Start)·기준 순간(Cue)만 됩니다.</summary>
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
            CombatEffectEnd end = CombatEffectEnd.Natural, float endSeconds = 0f, int actorPoint = 0, float riderYOffset = 0f)
        {
            if (!CombatPresentationEffectRules.IsPlayable(effect))
                throw new ArgumentOutOfRangeException(nameof(effect), effect, CombatPresentationEffectRules.PlayableRule);
            CombatPresentationCueRules.RequireDefined(anchor, point);
            CombatPresentationValueRules.RequireNonNegative(delay, nameof(delay));
            CombatPresentationValueRules.RequireFinite(riderYOffset, nameof(riderYOffset));
            if (actorPoint < 0)
                throw new ArgumentOutOfRangeException(nameof(actorPoint), actorPoint, "공격 지점 번호는 0 이상이어야 합니다.");
            if (actorPoint != 0 && point is not (CombatCuePoint.ActorMuzzle or CombatCuePoint.ActorEffectPoint))
                throw new ArgumentException("공격 지점 번호는 ActorMuzzle·ActorEffectPoint에서만 지정합니다.", nameof(actorPoint));
            if (!System.Enum.IsDefined(typeof(CombatEffectEnd), end))
                throw new ArgumentOutOfRangeException(nameof(end), end, "정의된 이펙트 끝이어야 합니다.");
            // 시간 끝만 초를 씁니다. 다른 끝의 초는 뜻이 없어 보관하지 않습니다(입력 칸에 남아 있어도 무시).
            if (end == CombatEffectEnd.Seconds)
                CombatPresentationValueRules.RequirePositive(endSeconds, nameof(endSeconds));
            Effect = effect;
            Anchor = anchor;
            Delay = delay;
            Point = point;
            ActorPoint = actorPoint;
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

        /// <summary>행위자의 총구/이펙트 지점 번호입니다. 다른 자리에서는 0입니다.</summary>
        internal int ActorPoint { get; }

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
        internal CombatShakeCue(float strength, float seconds, CinemachineImpulseDefinition.ImpulseShapes shape, CombatCueAnchor anchor, float delay)
        {
            CombatPresentationValueRules.RequireNonNegative(strength, nameof(strength));
            CombatPresentationCueRules.RequireDefined(anchor);
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
        internal float Delay { get; }

        /// <summary>흔들림이 있는지입니다(세기가 0보다 큼).</summary>
        internal bool IsOn => Strength > 0f;
    }

    /// <summary>사건 소리 시점 값입니다. 기다리지 않습니다.</summary>
    internal sealed class CombatSoundTimingCue
    {
        internal CombatSoundTimingCue(CombatCueAnchor anchor, float delay)
        {
            CombatPresentationCueRules.RequireDefined(anchor);
            CombatPresentationValueRules.RequireNonNegative(delay, nameof(delay));
            Anchor = anchor;
            Delay = delay;
        }

        internal CombatCueAnchor Anchor { get; }
        internal float Delay { get; }
    }

    /// <summary>프리셋을 가진 소리 큐 값입니다. 기다리지 않습니다. 자리는 지금 소리 위치에 쓰이지 않습니다(투사체·피격 소리 설계 PS4, 계획 P3).</summary>
    internal sealed class CombatPresetSoundCue
    {
        /// <exception cref="ArgumentOutOfRangeException">값이 규칙에 맞지 않을 때입니다.</exception>
        internal CombatPresetSoundCue(int preset, CombatCueAnchor anchor, float delay, CombatCuePoint point)
        {
            // -1(소리 없음) 항목은 뜻이 없어 받지 않습니다. 소리를 끄려면 항목을 지웁니다(계획 P2).
            CombatPresentationValueRules.RequireSoundPreset(preset, nameof(preset), allowNone: false);
            CombatPresentationCueRules.RequireDefined(anchor, point);
            CombatPresentationValueRules.RequireNonNegative(delay, nameof(delay));
            Preset = preset;
            Anchor = anchor;
            Delay = delay;
            Point = point;
        }

        internal int Preset { get; }
        internal CombatCueAnchor Anchor { get; }
        internal float Delay { get; }
        internal CombatCuePoint Point { get; }
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
