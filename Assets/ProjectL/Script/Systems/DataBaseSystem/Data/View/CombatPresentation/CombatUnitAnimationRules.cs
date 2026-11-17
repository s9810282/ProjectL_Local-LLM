using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 클립 규칙의 원본입니다(클립 설계 AC1·AC4·AC10, 원칙 검토 S1·S3, 클립 칸 다시 짜기 설계 CS8). 외형 표 한 행과 클립 묶음 SO는 이 함수들을 부르기만 합니다.
    /// 필수·선택 이벤트 읽기는 <see cref="CombatClipEvents.ReadTimings"/>, 시각 규칙은 <see cref="CombatClipTimings.FindTimeError"/>,
    /// 이벤트 목록 모양은 <see cref="CombatClipMotion.FindEventsError"/>, "컴포넌트 하나·Animator와 같은 GameObject" 규칙은 <see cref="CombatAnimationEvents.FindIn"/>가 원본입니다.
    /// </summary>
    internal static class CombatUnitAnimationRules
    {
        /// <summary>
        /// 유닛 하나의 클립 규칙입니다. DB 생성 때 외형 표 검사가 부릅니다.
        /// 이벤트 컴포넌트가 있으면(전투 재생이 그 Animator를 넘겨받는 유닛) 컨트롤러(Idle) 클립의 루트 곡선을 봅니다. 묶음이 없어도 봅니다.
        /// 클립 묶음이 있으면 컴포넌트가 있어야 하고, 공격 칸은 <see cref="CanUseAttackClip"/>인 공격 표시에만 쓸 수 있습니다.
        /// 이벤트 목록 줄 규칙은 칸·공격 종류마다 다릅니다(묶음은 여러 유닛이 공유해 종류를 모르므로 이 행 검사가 봄, 계획 P13, 설계 CS2):
        /// 원거리 = 지점은 있는 이벤트 지점·지연 0, 근접 = 같은 이벤트 한 번, 피격 = 지연 0·같은 이벤트 한 번. 근접·피격 줄의 지점은 읽지 않아 값을 보지 않습니다(11-17 사용자).
        /// 연출 큐의 유닛 조건(이벤트 지점 자리 = 공격 클립과 그 지점, 이벤트 시점 = 그 칸 본 단계 몸 클립에 표식)은 큐 규칙(<see cref="CombatPresentationCueRules.FindUnitError"/>)이,
        /// 유닛 소리 줄의 조건(시점·클립 표식·연출 묶음 유무)은 같은 규칙 클래스의 <see cref="CombatPresentationCueRules.FindUnitSoundError"/>가 봅니다(유닛 소리 계획 TP-3).
        /// </summary>
        /// <param name="unit">문구에 넣을 유닛 표시(예: 키)입니다.</param>
        /// <param name="reaction">그 유닛의 피격·사망 반응 값입니다. 없으면 null입니다. 피격 클립은 반응 묶음 위에서만 돕니다(범용 연출 구조 계획 Ga.5).</param>
        /// <param name="sounds">그 유닛의 공격·피격·사망 소리 줄입니다.</param>
        internal static void ValidateUnit(GameObject prefab, CombatUnitAnimationSO bundle, CombatAttackPresentationData attack,
            CombatUnitReactionPresentationData reaction, CombatUnitSoundsData sounds, string unit)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (sounds == null) throw new ArgumentNullException(nameof(sounds));
            CombatAnimationEvents events = CombatAnimationEvents.FindIn(prefab);
            CombatUnitAnimationData animation = bundle != null ? bundle.CreateData() : null;
            CombatEventPoints points = events != null ? events.CaptureEventPoints() : null;
            if (bundle != null)
            {
                if (events == null)
                    throw Invalid($"클립 묶음을 연결한 유닛의 프리팹에 {nameof(CombatAnimationEvents)}가 없습니다"
                        + $"(Animator가 있는 GameObject에 하나 붙입니다). {unit}, Prefab: {prefab.name}");
                // 피격 클립은 피격 반응 조립 안에서만 돕니다. 반응 묶음이 없으면 피격 사건이 연출 없이 지나가 클립이 조용히 안 나오므로 부트에서 막습니다
                // (범용 연출 구조 설계 §1 L1, 계획 Ga.5).
                if (animation.Hit != null && reaction == null)
                    throw Invalid("피격 클립은 피격·사망 반응 묶음(reactionPresentation)이 있는 유닛에만 넣을 수 있습니다. "
                        + $"{unit}, Prefab: {prefab.name}");
                if (animation.Attack != null && !CanUseAttackClip(attack))
                    throw Invalid("공격 클립은 공격 표시(근접·직선·곡사)가 있는 유닛에만 넣을 수 있습니다. "
                        + $"{unit}, 공격 표시: {(attack == null ? "없음" : attack.Kind.ToString())}");
                if (animation.Attack != null)
                    RequireAttackEvents(prefab, animation.Attack.Events, FiresProjectile(attack.Kind), points, unit);
                if (animation.Hit != null)
                    RequireTimingEvents(animation.Hit.Events, "피격", unit);
            }

            // 큐의 이벤트 지점 자리·이벤트 시점은 유닛마다 다른 조건이라 여기서 봅니다(범용 연출 구조 계획 P6·Gd.4). 지점이 없으면 이펙트가 잘못된 자리에
            // 조용히 나오므로 부트에서 막습니다(옛 이펙트 지점 필수 규칙 EF4를 넓힌 것, 결정 Q-E2 A).
            string cueError = CombatPresentationCueRules.FindUnitError(attack?.Cues, animation?.Attack?.Motion, points,
                reaction?.HitCues, animation?.Hit);
            if (cueError != null)
                throw Invalid($"{cueError}. {unit}, Prefab: {prefab.name}");
            string soundError = CombatPresentationCueRules.FindUnitSoundError(sounds, attack, animation?.Attack?.Motion, reaction != null, animation?.Hit);
            if (soundError != null)
                throw Invalid($"{soundError}. {unit}, Prefab: {prefab.name}");

            if (attack != null && points != null)
            {
                for (int i = 0; i < attack.Cues.Effects.Count; i++)
                {
                    CombatEffectCue cue = attack.Cues.Effects[i];
                    if (cue.Point != CombatCuePoint.ActorEventPoint) continue;
                    points.TryGet(cue.EventPoint, out Transform point);
                    RequireOwnedPoint(prefab, point, $"공격 이펙트 큐 {i}의 이벤트 지점 {cue.EventPoint}", unit);
                }
            }

            if (events == null)
                return;
            // Idle도 같은 그래프로 평가돼 루트를 되돌릴 수 없으므로 컨트롤러 클립을 봅니다(설계 AC9·AC10).
            RuntimeAnimatorController controller = events.Animator.runtimeAnimatorController;
            if (controller == null)
                return;
            AnimationClip[] idleClips = controller.animationClips;
            for (int i = 0; i < idleClips.Length; i++)
            {
                if (idleClips[i] != null)
                    RequireNoRootCurves(idleClips[i], $"{unit}, Prefab: {prefab.name}");
            }
        }

        // 공격 이벤트 줄 규칙입니다(설계 CS2). 원거리는 줄마다 한 발이라 이벤트 지점이 있어야 하고, 맞는 순간은 투사체가 정하므로 지연은 0입니다(설계 AC14).
        // 근접은 줄마다 맞는 순간이라 지점을 쓰지 않고, 같은 순간에 두 번 맞지 않게 같은 이벤트는 한 번입니다.
        private static void RequireAttackEvents(GameObject prefab, IReadOnlyList<CombatClipEventEntry> entries, bool ranged, CombatEventPoints points,
            string unit)
        {
            if (!ranged)
            {
                RequireTimingEvents(entries, "근접 공격", unit, allowDelay: true);
                return;
            }
            for (int i = 0; i < entries.Count; i++)
            {
                CombatClipEventEntry entry = entries[i];
                if (entry.Delay != 0f)
                    throw Invalid($"원거리 공격 이벤트 {i}번 줄({entry.EventId})의 지연은 0이어야 합니다(이벤트 순간에 바로 쏘고 맞는 순간은 투사체가 정함). "
                        + $"{unit}, 지연: {entry.Delay}");
                if (points == null || !points.TryGet(entry.Point, out Transform muzzle))
                    throw Invalid($"원거리 공격 이벤트 {i}번 줄({entry.EventId})의 발사 지점 {entry.Point}가 없습니다(이벤트 컴포넌트 Event Points). {unit}, Prefab: {prefab.name}");
                RequireOwnedPoint(prefab, muzzle, $"원거리 공격 이벤트 {i}번 줄({entry.EventId}) 발사 지점 {entry.Point}", unit);
            }
        }

        // 지점을 쓰지 않는 칸(근접 공격·피격)의 줄 규칙입니다. 지연은 근접만 받습니다. 지점은 읽지 않으므로 값을 보지 않습니다(11-17 사용자 "제약 없음", 설계 P3·P4 바뀜).
        private static void RequireTimingEvents(IReadOnlyList<CombatClipEventEntry> entries, string slot, string unit, bool allowDelay = false)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                CombatClipEventEntry entry = entries[i];
                if (!allowDelay && entry.Delay != 0f)
                    throw Invalid($"{slot} 이벤트 {i}번 줄({entry.EventId})의 지연은 0이어야 합니다(지연은 근접 공격의 맞는 순간에만 씀). {unit}");
                for (int j = 0; j < i; j++)
                {
                    if (entries[j].EventId == entry.EventId)
                        throw Invalid($"{slot} 이벤트 {i}번 줄이 {j}번 줄과 같은 이벤트({entry.EventId})입니다. {unit}");
                }
            }
        }

        /// <summary>
        /// 동작 단계 하나(몸 + 탑승물 + 부속)의 규칙을 보고 값으로 만듭니다(설계 CS1·CS5·CS6·CS8). 몸은 사건 클립 규칙(<see cref="RequireEventClip"/>)과 이벤트 셋,
        /// 탑승물·부속은 부속 규칙(<see cref="RequireCompanionClips"/>, 이벤트 없음·몸과 다른 뼈)을 따릅니다. 층 순서는 몸 · 탑승물 · 부속입니다(설계 P6).
        /// 본 단계 몸은 시작·기준 이벤트·끝이 필수(<see cref="CombatClipEvents.ReadTimings"/>)이고, 앞·뒤 단계 몸은 시작·끝만 두고 중간 이벤트를 찍지 않습니다
        /// (<see cref="CombatClipEvents.ReadStageTimings"/>, 설계 P7).
        /// </summary>
        /// <param name="main">본 단계인지입니다. 거짓이면 앞·뒤 단계입니다.</param>
        /// <exception cref="InvalidOperationException">규칙에 맞지 않을 때입니다. 문구에 클립 이름과 이유가 있습니다.</exception>
        internal static CombatClipData RequireStage(AnimationClip body, AnimationClip mount, IReadOnlyList<AnimationClip> companions, bool main = true)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));
            RequireEventClip(body);
            int count = (mount != null ? 1 : 0) + (companions?.Count ?? 0);
            var parts = new AnimationClip[count];
            int index = 0;
            if (mount != null) parts[index++] = mount;
            for (int i = 0; companions != null && i < companions.Count; i++)
                parts[index++] = companions[i];
            RequireCompanionClips(body, parts);
            CombatClipTimings timings = main ? CombatClipEvents.ReadTimings(body) : CombatClipEvents.ReadStageTimings(body);
            return new CombatClipData(body, timings, companions, mount);
        }

        private static void RequireOwnedPoint(GameObject prefab, Transform point, string field, string unit)
        {
            // 지점 존재는 바로 앞의 이벤트 줄/큐 조건 검사에서 확인했습니다. 여기서는 소속만 봅니다.
            if (point != prefab.transform && !point.IsChildOf(prefab.transform))
                throw Invalid($"{field}이 해당 프리팹에 속하지 않습니다: {point.name}. {unit}, Prefab: {prefab.name}");
        }

        /// <summary>
        /// 공격 칸을 쓸 수 있는 공격 표시인지입니다. 근접과 투사체를 쏘는 종류(<see cref="FiresProjectile"/>)입니다.
        /// </summary>
        internal static bool CanUseAttackClip(CombatAttackPresentationData attack) =>
            attack != null && (attack.Kind == CombatAttackPresentationKind.Melee || FiresProjectile(attack.Kind));

        /// <summary>
        /// 공격 클립이 이벤트 타이밍에 투사체를 쏘는 종류(직선·곡사)인지입니다(설계 5차 AC11·AC14, 계획 P10). 이 종류의 이벤트 줄은 발사 지점이 있어야 하고 지연은 0입니다.
        /// 종류는 공격 묶음의 투사체 부품에서 나오므로(범용 연출 구조 설계 G4), 이 판정은 곧 "투사체 부품이 있음"입니다.
        /// 원거리 판정은 이 함수 하나이고, DB 검사와 클립 공격 조립 분기가 함께 부릅니다.
        /// </summary>
        internal static bool FiresProjectile(CombatAttackPresentationKind kind) =>
            kind == CombatAttackPresentationKind.Linear || kind == CombatAttackPresentationKind.Mortar;

        /// <summary>
        /// 사건 클립(공격·피격 등) 하나의 규칙입니다. 길이는 유한한 양수, 반복(Loop Time) 금지, 루트 곡선 금지입니다.
        /// 필수·선택 이벤트는 따로 <see cref="CombatClipEvents.ReadTimings"/>가 봅니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">규칙에 맞지 않을 때입니다. 문구에 클립 이름과 이유가 있습니다.</exception>
        internal static void RequireEventClip(AnimationClip clip)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (!(clip.length > 0f) || float.IsInfinity(clip.length))
                throw Invalid($"클립 길이가 유한한 양수가 아닙니다: {clip.name}");
            if (clip.isLooping)
                throw Invalid($"반복 클립입니다(전투 사건 클립은 Loop Time을 끕니다): {clip.name}");
            RequireNoRootCurves(clip, null);
        }

        /// <summary>
        /// 주 클립에 딸린 부속 클립(무기 등)의 규칙입니다(설계 6차 AC18). 목록 모양(빈 칸·중복·주 클립과 같음, 원본 <see cref="CombatClipData.FindCompanionError"/>),
        /// 부속마다 사건 클립 규칙(<see cref="RequireEventClip"/>)과 <b>이벤트 없음</b>(시점의 원본은 주 클립 하나),
        /// 그리고 주 클립·다른 부속과 <b>같은 곡선을 움직이지 않음</b>(뒤 층이 앞 층의 동작을 지우지 않게)을 봅니다.
        /// 곡선 겹침은 루트 곡선 검사처럼 에디터에서만 봅니다(설계 I3). 목록이 null이거나 비면 아무것도 보지 않습니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">규칙에 맞지 않을 때입니다. 문구에 클립 이름과 이유가 있습니다.</exception>
        internal static void RequireCompanionClips(AnimationClip clip, IReadOnlyList<AnimationClip> companions)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (companions == null || companions.Count == 0)
                return;
            string error = CombatClipData.FindCompanionError(clip, companions);
            if (error != null)
                throw Invalid(error);
            for (int i = 0; i < companions.Count; i++)
            {
                AnimationClip companion = companions[i];
                RequireEventClip(companion);
                if (companion.events.Length > 0)
                    throw Invalid($"부속 클립에는 애니메이션 이벤트를 찍지 않습니다(시점은 주 클립 {clip.name}의 이벤트 셋이 정함): {companion.name}");
            }

            RequireNoSharedCurves(clip, companions);
        }

        /// <summary>주 클립과 부속 클립이 같은 곡선(경로·형식·속성)을 둘 이상이 움직이면 실패합니다. 에디터에서만 봅니다.</summary>
        private static void RequireNoSharedCurves(AnimationClip clip, IReadOnlyList<AnimationClip> companions)
        {
#if UNITY_EDITOR
            var owners = new Dictionary<EditorCurveBinding, AnimationClip>();
            AddCurves(owners, clip);
            for (int i = 0; i < companions.Count; i++)
                AddCurves(owners, companions[i]);
#endif
        }

#if UNITY_EDITOR
        private static void AddCurves(Dictionary<EditorCurveBinding, AnimationClip> owners, AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                EditorCurveBinding binding = bindings[i];
                if (owners.TryGetValue(binding, out AnimationClip owner))
                    throw Invalid($"부속 클립 {clip.name}이(가) {owner.name}과(와) 같은 곡선({binding.path}.{binding.propertyName})을 움직입니다. "
                        + "부속 클립은 주 클립·다른 부속과 다른 뼈만 움직여야 합니다");
                owners.Add(binding, clip);
            }
        }
#endif

        /// <summary>
        /// 클립이 Animator GameObject 자신의 Transform을 움직이면(경로가 빈 Transform 곡선, 루트 곡선) 실패합니다(설계 AC1·AC10, 결정 Q-AC10 A).
        /// 전투 클립은 Unity 게임 시간 그래프로 평가돼 평가 직후 루트를 되돌릴 수 없으므로, 모델 자식(아마추어·뼈대)만 움직여야 합니다.
        /// 곡선 목록은 에디터에서만 읽을 수 있어 에디터 밖(플레이어 빌드)에서는 이 검사만 하지 않습니다(설계 I3).
        /// </summary>
        /// <param name="owner">문구에 넣을 사용처입니다. 없으면 null입니다.</param>
        internal static void RequireNoRootCurves(AnimationClip clip, string owner)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
#if UNITY_EDITOR
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                if (string.IsNullOrEmpty(bindings[i].path) && bindings[i].type == typeof(Transform))
                    throw Invalid($"클립이 Animator GameObject 자신의 Transform을 움직입니다(루트 곡선 {bindings[i].propertyName}). "
                        + $"전투 클립은 모델 자식만 움직여야 합니다. 클립: {clip.name}" + (owner == null ? string.Empty : $", 사용처: {owner}"));
            }
#endif
        }

        private static InvalidOperationException Invalid(string reason)
            => new($"[CombatUnitAnimationRules] 전투 클립 규칙에 맞지 않습니다: {reason}");
    }
}
