using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 동작 단계 하나의 입력입니다(클립 칸 다시 짜기 설계 CS1). 몸(주 클립) + 탑승물(말, 설계 CS5) + 부속(무기, 올가미 포함)이 같은 시각으로 돕니다.
    /// </summary>
    [Serializable]
    internal sealed class CombatClipStageInput
    {
        [Tooltip("몸 클립(주 클립)입니다. ClipStart·ClipEnd를 하나씩 찍고, 본 단계면 ClipEvent(·ClipEvent2·3)도 찍습니다(앞·뒤 단계에는 ClipEvent류를 찍지 않음). "
            + "본 단계를 비우면 이 칸은 코드 모션·코드 반응이고, 앞·뒤 단계를 비우면 그 단계가 없습니다.")]
        [SerializeField] private AnimationClip body;
        [Tooltip("탑승물(말) 클립입니다. 몸과 같은 시각으로 돌고 이벤트를 찍지 않으며, 몸·부속과 다른 뼈만 움직입니다. 탑승물이 없으면 비웁니다.")]
        [SerializeField] private AnimationClip mount;
        [Tooltip("부속 클립(무기·올가미 등)입니다. 몸과 같은 시각으로 돌고 이벤트를 찍지 않으며, 몸·탑승물·다른 부속과 다른 뼈만 움직입니다. 비워도 됩니다.")]
        [SerializeField] private AnimationClip[] companions = Array.Empty<AnimationClip>();

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatClipStageInput()
        {
        }

        internal AnimationClip Body => body;

        /// <summary>몸·탑승물·부속 중 하나라도 넣었는지입니다(비운 단계인지).</summary>
        internal bool HasAny => body != null || mount != null || (companions != null && companions.Length > 0);

        /// <summary>
        /// 단계 클립 규칙(<see cref="CombatUnitAnimationRules.RequireStage"/>)을 보고 값으로 만듭니다. 몸이 비어 있으면 null입니다.
        /// </summary>
        /// <param name="field">문구에 넣을 칸 이름입니다.</param>
        /// <param name="main">본 단계인지입니다. 거짓이면 앞·뒤 단계라 몸 클립에 중간 이벤트를 찍지 않습니다(설계 P7).</param>
        /// <exception cref="InvalidOperationException">규칙에 맞지 않을 때입니다(문구에 칸 이름과 이유).</exception>
        internal CombatClipData ToData(string field, bool main = true)
        {
            if (body == null)
            {
                if (mount != null || (companions != null && companions.Length > 0))
                    throw new InvalidOperationException($"{field} — 몸 클립 없이 탑승물·부속 클립만 있습니다(탑승물·부속은 몸 클립의 시각으로만 돕니다)");
                return null;
            }
            try
            {
                return CombatUnitAnimationRules.RequireStage(body, mount, companions, main);
            }
            catch (InvalidOperationException error)
            {
                throw new InvalidOperationException($"{field} — {error.Message}", error);
            }
        }
    }

    /// <summary>
    /// 동작 칸(공격·피격) 하나의 입력입니다(클립 칸 다시 짜기 설계 CS1·CS2·CS6·CS7). 앞 단계(공격만) + 본 단계 + 뒤 단계 + 이벤트 목록 + 재생 배율 + 전환 블렌드 셋입니다.
    /// </summary>
    [Serializable]
    internal sealed class CombatClipMotionInput
    {
        [Tooltip("앞 단계입니다(공격 칸만). 본 단계 앞에 늘 재생하는 사전 동작입니다(예: 카우보이 PullBack). 몸 클립에는 ClipStart·ClipEnd만 찍습니다. 비우면 없습니다.")]
        [SerializeField] private CombatClipStageInput pre = new();
        [Tooltip("본 단계입니다. 클립 이벤트(맞는 순간·발사·큐 시점)는 이 단계의 몸 클립에만 찍습니다. 큐의 Start·Event1~3·End도 이 단계 클립의 이름표입니다.")]
        [SerializeField] private CombatClipStageInput main = new();
        [Tooltip("뒤 단계입니다. 본 단계가 끝나 사건이 완료된 뒤 채널이 혼자 이어 재생하고, 그 유닛의 다음 클립·취소·풀 반환·래그돌이 오면 끊습니다. "
            + "몸 클립에는 ClipStart·ClipEnd만 찍습니다. 비우면 없습니다.")]
        [SerializeField] private CombatClipStageInput post = new();
        [Tooltip("이벤트 줄 목록입니다(번호, 지점, 지연). [Event1, 0, 0]은 적지 않아도 늘 맨 앞에 있습니다(공격 = 공격 애니메이션 임팩트 타이밍, 피격 = 피격 타이밍) — 대부분의 유닛은 비워 둡니다. "
            + "Event1의 지점·지연을 바꿀 때만 Event1 줄을 적습니다. "
            + "원거리 공격 = 줄마다 한 발(두 발 이상·다른 발사 지점일 때 Event2·3 줄을 더함), 근접 공격 = 줄마다 맞는 순간(여러 번 맞거나 지연이 필요할 때, 앞 줄은 앞 접촉 피격, 마지막 접촉이 실제 해제), "
            + "피격 = 타이밍 표시(맞기 전 예열 동작 등을 위한 자리). "
            + "큐의 Event1~3 시점은 이 목록과 상관없이 본 단계 몸 클립에 그 표식(ClipEvent~ClipEvent3)이 있으면 쓸 수 있습니다.")]
        [SerializeField] private CombatClipEventEntryInput[] events = Array.Empty<CombatClipEventEntryInput>();
        [Tooltip("이 칸의 모든 단계와 클립 이벤트의 재생 배율입니다(옛 Attack Speed). 1은 기본, 2는 두 배, 0.5는 절반 속도입니다. 유한한 양수여야 합니다. "
            + "투사체 속도·이펙트 자체 속도·초 단위 지연·블렌드 초는 바꾸지 않습니다.")]
        [SerializeField, Min(0.01f)] private float animSpeed = 1f;
        [Tooltip("Idle → 이 동작으로 들어올 때의 블렌드입니다.")]
        [SerializeField] private CombatClipBlendInput blendIn = new();
        [Tooltip("이 동작의 단계 → 다음 단계로 넘어갈 때의 블렌드입니다(앞·뒤 단계가 있을 때).")]
        [SerializeField] private CombatClipBlendInput blendBetween = new();
        [Tooltip("이 동작 → Idle로 돌아갈 때의 블렌드입니다.")]
        [SerializeField] private CombatClipBlendInput blendOut = new();

        // Unity 직렬화가 쓰는 기본 생성자입니다.
        public CombatClipMotionInput()
        {
        }

        /// <summary>본 단계 몸 클립이 있는지입니다(칸이 비었는지).</summary>
        internal bool HasBody => main != null && main.Body != null;

        /// <summary>값으로 만듭니다. 본 단계 몸 클립이 비면 null입니다(코드 모션·코드 반응).</summary>
        /// <param name="field">문구에 넣을 칸 이름입니다.</param>
        /// <param name="allowsPre">앞 단계를 받는 칸인지입니다. 공격 칸만 참입니다(결정 Q-A6 A).</param>
        /// <exception cref="InvalidOperationException">규칙에 맞지 않을 때입니다(문구에 칸 이름과 이유).</exception>
        internal CombatClipMotion ToData(string field, bool allowsPre)
        {
            CombatClipData body = (main ?? throw new InvalidOperationException($"{field} — 본 단계 칸이 없습니다")).ToData($"{field}.main");
            if (!allowsPre && pre != null && pre.HasAny)
                throw new InvalidOperationException($"{field}.pre — 앞 단계는 공격 칸만 둘 수 있습니다(피격 칸은 뒤 단계만)");
            if (body == null)
            {
                if ((pre != null && pre.HasAny) || (post != null && post.HasAny))
                    throw new InvalidOperationException($"{field} — 본 단계 몸 클립 없이 앞·뒤 단계만 있습니다");
                return null;
            }
            CombatClipData before = pre?.ToData($"{field}.pre", main: false);
            CombatClipData after = post?.ToData($"{field}.post", main: false);
            try
            {
                var entries = new CombatClipEventEntry[events?.Length ?? 0];
                for (int i = 0; i < entries.Length; i++)
                    entries[i] = (events[i] ?? throw new ArgumentException($"이벤트 목록 {i}번 칸이 비어 있습니다")).ToData();
                return new CombatClipMotion(body, entries, animSpeed,
                    (blendIn ?? new CombatClipBlendInput()).ToData(),
                    (blendBetween ?? new CombatClipBlendInput()).ToData(),
                    (blendOut ?? new CombatClipBlendInput()).ToData(),
                    before, after);
            }
            catch (ArgumentException error)
            {
                throw new InvalidOperationException($"{field} — {error.Message}", error);
            }
        }
    }

    /// <summary>
    /// 동작 칸(공격·피격) 하나의 값입니다(클립 칸 다시 짜기 설계 CS1·CS6). 앞 단계(있으면)·본 단계·뒤 단계(있으면), 이벤트 줄 목록, 재생 배율, 전환 블렌드 셋입니다.
    /// 이벤트 줄 목록의 [Event1, 0, 0](<see cref="DefaultEvents"/>)은 적지 않아도 맨 앞에 들어가고(11-17 사용자), 규칙의 원본은 <see cref="FindEventsError"/>, 칸·공격 종류마다 다른 줄 규칙은 외형 표 검사(<see cref="CombatUnitAnimationRules"/>)가 봅니다.
    /// 클립 이벤트와 큐 시점(Start·Event1~3·End)은 본 단계 클립의 이름표이고(큐는 표식만 있으면 됨, <see cref="HasEvent"/>), 앞·뒤 단계에는 중간 이벤트가 없습니다(설계 P7).
    /// </summary>
    internal sealed class CombatClipMotion
    {
        /// <param name="pre">앞 단계입니다. 없으면 null입니다. 중간 이벤트가 없어야 합니다.</param>
        /// <param name="post">뒤 단계입니다. 없으면 null입니다. 중간 이벤트가 없어야 합니다.</param>
        /// <exception cref="ArgumentException">이벤트 목록·재생 배율·앞뒤 단계가 규칙에 맞지 않을 때입니다.</exception>
        /// <param name="events">이벤트 줄 목록입니다. null이거나 비면 기본 한 줄(<see cref="DefaultEvents"/>)이고, Event1 줄이 없으면 기본 줄을 맨 앞에 붙입니다(<see cref="WithPrimary"/>).</param>
        internal CombatClipMotion(CombatClipData main, IReadOnlyList<CombatClipEventEntry> events, float animSpeed = 1f,
            CombatClipBlend blendIn = default, CombatClipBlend blendBetween = default, CombatClipBlend blendOut = default,
            CombatClipData pre = null, CombatClipData post = null)
        {
            Main = main ?? throw new ArgumentNullException(nameof(main));
            events = WithPrimary(events);
            string error = FindEventsError(main, events);
            if (error != null) throw new ArgumentException(error, nameof(events));
            if (!CombatPresentationValueRules.IsPositive(animSpeed))
                throw new ArgumentException($"재생 배율(animSpeed) — {CombatPresentationValueRules.PositiveRule}", nameof(animSpeed));
            if (pre != null && pre.Timings.Events.Count > 0)
                throw new ArgumentException($"앞 단계 몸 클립에 중간 이벤트가 있습니다(본 단계에만 찍음). 클립: {pre.Clip.name}", nameof(pre));
            if (post != null && post.Timings.Events.Count > 0)
                throw new ArgumentException($"뒤 단계 몸 클립에 중간 이벤트가 있습니다(본 단계에만 찍음). 클립: {post.Clip.name}", nameof(post));
            Pre = pre;
            Post = post;
            var copy = new CombatClipEventEntry[events.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = events[i];
            Events = Array.AsReadOnly(copy);
            AnimSpeed = animSpeed;
            BlendIn = blendIn;
            BlendBetween = blendBetween;
            BlendOut = blendOut;
        }

        /// <summary>앞 단계입니다(공격 칸만, 결정 Q-A6 A). 없으면 null입니다. 본 단계 앞에 늘 재생합니다.</summary>
        internal CombatClipData Pre { get; }

        /// <summary>본 단계입니다. 클립 이벤트 시각의 원본입니다.</summary>
        internal CombatClipData Main { get; }

        /// <summary>뒤 단계입니다. 없으면 null입니다. 본 단계가 끝난 뒤 채널이 혼자 재생합니다(결정 Q-A7 A, 설계 P8).</summary>
        internal CombatClipData Post { get; }

        /// <summary>이벤트 줄 목록입니다. 하나 이상이고 Event1 줄이 있습니다(입력에 Event1 줄이 없으면 <see cref="DefaultEvents"/>의 줄이 맨 앞).</summary>
        internal IReadOnlyList<CombatClipEventEntry> Events { get; }

        /// <summary>
        /// 기본 줄입니다: 기준 이벤트(Event1) · 지점 0 · 지연 0. 공격 칸 = 공격 애니메이션 임팩트 타이밍, 피격 칸 = 피격 타이밍입니다(11-17 사용자).
        /// 대부분의 유닛이 이 한 줄이라 에셋에 적지 않고, Event1 줄을 적지 않은 목록에는 늘 이 줄이 맨 앞에 들어갑니다.
        /// </summary>
        internal static IReadOnlyList<CombatClipEventEntry> DefaultEvents { get; } =
            Array.AsReadOnly(new[] { new CombatClipEventEntry(CombatClipEventIds.Primary, 0, 0f) });

        /// <summary>
        /// 기준 이벤트 줄을 채운 목록입니다: 비면 기본 한 줄, Event1 줄이 없으면 기본 줄을 맨 앞에 붙이고, 있으면 그대로입니다(11-17 사용자 — 에셋에서 [Event1, 0, 0]을 지움).
        /// Event1의 지점·지연을 바꿀 때만 Event1 줄을 적습니다.
        /// </summary>
        private static IReadOnlyList<CombatClipEventEntry> WithPrimary(IReadOnlyList<CombatClipEventEntry> events)
        {
            if (events == null || events.Count == 0)
                return DefaultEvents;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].EventId == CombatClipEventIds.Primary)
                    return events;
            }
            var filled = new CombatClipEventEntry[events.Count + 1];
            filled[0] = DefaultEvents[0];
            for (int i = 0; i < events.Count; i++)
                filled[i + 1] = events[i];
            return filled;
        }

        /// <summary>이 칸의 재생 배율입니다(옛 공격 attackSpeed). 블렌드 초에는 걸지 않습니다.</summary>
        internal float AnimSpeed { get; }

        /// <summary>Idle → 첫 단계(앞 단계가 있으면 앞 단계, 없으면 본 단계)로 들어올 때의 블렌드입니다(설계 CS7).</summary>
        internal CombatClipBlend BlendIn { get; }

        /// <summary>단계 → 다음 단계(앞 → 본, 본 → 뒤)로 넘어갈 때의 블렌드입니다(설계 CS7).</summary>
        internal CombatClipBlend BlendBetween { get; }

        /// <summary>
        /// 마지막 단계 → Idle로 돌아갈 때의 블렌드입니다(설계 CS7). 뒤 단계가 있으면 뒤 단계가 끝날 때, 없으면 본 단계가 끝날 때 섞기 시작합니다(CS-c 뒤 단계 규칙, 11-17 사용자).
        /// 꺼져 있으면 지금처럼 뒤 단계 끝 또는 대여 반환 때 바로 돌아갑니다.
        /// </summary>
        internal CombatClipBlend BlendOut { get; }

        /// <summary>
        /// 본 단계 몸 클립에 그 번호의 이벤트 표식이 있는지입니다. 큐의 Event1~3 시점은 이벤트 줄 목록이 아니라 이 표식으로 정합니다(11-17 사용자 — 설계 CS3의 "등록" 조건을 대신함).
        /// 본 단계 클립은 기준 이벤트(Event1) 표식이 늘 있습니다(<see cref="CombatClipEvents.ReadTimings"/>).
        /// </summary>
        internal bool HasEvent(CombatClipEventId id) => Main.Timings.TryGetEventTime(id, out _);

        /// <summary>
        /// 이벤트 줄 목록 규칙의 원본입니다(생성자가 <see cref="WithPrimary"/>로 Event1 줄을 채운 뒤 봄): 하나 이상, Event1 줄이 있음(기준 이벤트), 줄마다 주 클립에 그 이름표가 있고 끝보다 앞, 같은 (번호, 지점)은 한 번.
        /// 맞으면 null입니다.
        /// </summary>
        internal static string FindEventsError(CombatClipData main, IReadOnlyList<CombatClipEventEntry> events)
        {
            if (main == null) throw new ArgumentNullException(nameof(main));
            if (events == null || events.Count == 0)
                return "이벤트 목록이 비어 있습니다";
            bool hasPrimary = false;
            for (int i = 0; i < events.Count; i++)
            {
                CombatClipEventEntry entry = events[i];
                hasPrimary |= entry.EventId == CombatClipEventIds.Primary;
                if (!main.Timings.TryGetEventTime(entry.EventId, out float time))
                    return $"이벤트 목록 {i}번 줄의 이벤트 {entry.EventId}가 주 클립에 없습니다. 클립: {main.Clip.name}";
                if (time >= main.EndTime)
                    return $"이벤트 목록 {i}번 줄의 이벤트 {entry.EventId}는 끝보다 앞서야 합니다. 클립: {main.Clip.name}";
                for (int j = 0; j < i; j++)
                {
                    if (events[j].EventId == entry.EventId && events[j].Point == entry.Point)
                        return $"이벤트 목록 {i}번 줄이 {j}번 줄과 같은 이벤트·지점입니다: {entry.EventId}, 지점 {entry.Point}";
                }
            }
            return hasPrimary ? null : $"이벤트 목록에 기준 이벤트({CombatClipEventIds.Primary}) 줄이 없습니다";
        }
    }
}
