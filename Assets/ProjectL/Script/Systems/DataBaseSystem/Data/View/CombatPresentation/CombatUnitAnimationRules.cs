using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 클립 규칙의 원본입니다(클립 설계 AC1·AC4·AC10, 원칙 검토 S1·S3). 외형 표 한 행과 클립 묶음 SO는 이 함수들을 부르기만 합니다.
    /// 필수·선택 이벤트 읽기는 <see cref="CombatClipEvents.ReadTimings"/>, 시각 규칙은 <see cref="CombatClipTimings.FindTimeError"/>,
    /// "컴포넌트 하나·Animator와 같은 GameObject" 규칙은 <see cref="CombatAnimationEvents.FindIn"/>가 원본입니다.
    /// </summary>
    internal static class CombatUnitAnimationRules
    {
        /// <summary>
        /// 유닛 하나의 클립 규칙입니다. DB 생성 때 외형 표 검사가 부릅니다.
        /// 이벤트 컴포넌트가 있으면(전투 재생이 그 Animator를 넘겨받는 유닛) 컨트롤러(Idle) 클립의 루트 곡선을 봅니다. 묶음이 없어도 봅니다.
        /// 클립 묶음이 있으면 컴포넌트가 있어야 하고, 공격 칸은 <see cref="CanUseAttackClip"/>인 공격 표시에만 쓸 수 있습니다.
        /// 원거리(<see cref="FiresProjectile"/>) 공격 칸이면 발사 지점이 있어야 하고 피격 선딜레이는 0입니다(묶음은 여러 유닛이 공유해 종류를 모르므로 이 행 검사가 봄, 계획 P13).
        /// 연출 큐의 유닛 조건(행위자 지점 자리 = 공격 클립과 그 지점, 피격 기준 순간 = 피격 클립)은 큐 규칙(<see cref="CombatPresentationCueRules.FindUnitError"/>)이 봅니다
        /// (옛 "공격 칸 이펙트면 이펙트 지점 필수", 이펙트 설계 EF4를 넓힌 것).
        /// </summary>
        /// <param name="unit">문구에 넣을 유닛 표시(예: 키)입니다.</param>
        /// <param name="reaction">그 유닛의 피격·사망 반응 값입니다. 없으면 null입니다. 피격 클립은 반응 묶음 위에서만 돕니다(범용 연출 구조 계획 Ga.5).</param>
        internal static void ValidateUnit(GameObject prefab, CombatUnitAnimationSO bundle, CombatAttackPresentationData attack,
            CombatUnitReactionPresentationData reaction, string unit)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            CombatAnimationEvents events = CombatAnimationEvents.FindIn(prefab);
            CombatUnitAnimationData animation = bundle != null ? bundle.CreateData() : null;
            CombatAttackPoints points = events != null ? events.CaptureAttackPoints() : null;
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
                if (animation.Attack != null && animation.Attack.HasExplicitShots && !FiresProjectile(attack.Kind))
                    throw Invalid($"명시 발사 목록은 원거리 공격 클립에만 사용할 수 있습니다. {unit}, Prefab: {prefab.name}");
                if (animation.Attack != null && FiresProjectile(attack.Kind))
                {
                    IReadOnlyList<CombatClipShot> shots = animation.Attack.Shots;
                    for (int i = 0; i < shots.Count; i++)
                    {
                        if (!points.TryGetMuzzle(shots[i].Point, out Transform muzzle))
                            throw Invalid($"발사 {i}({shots[i].EventId})의 총구가 없습니다. 지점 {shots[i].Point}, {unit}, Prefab: {prefab.name}");
                        RequireOwnedPoint(prefab, muzzle, $"발사 {i}({shots[i].EventId}) 총구, 지점 {shots[i].Point}", unit);
                    }
                    // 원거리는 이벤트 타이밍에 바로 쏘고 맞는 순간은 투사체가 정합니다(설계 AC14). 칸 하나에 뜻이 둘이 되지 않게 0만 받습니다.
                    if (animation.Attack.HitDelay != 0f)
                        throw Invalid("원거리 공격 클립은 이벤트 타이밍에 바로 쏘므로 피격 선딜레이를 0으로 둡니다(맞는 순간은 투사체가 정함). "
                            + $"{unit}, 피격 선딜레이: {animation.Attack.HitDelay}");
                }
            }

            // 큐의 행위자 지점 자리·피격 기준 순간은 유닛마다 다른 조건이라 여기서 봅니다(범용 연출 구조 계획 P6·Gd.4). 지점이 없으면 이펙트가 잘못된 자리에
            // 조용히 나오므로 부트에서 막습니다(옛 이펙트 지점 필수 규칙 EF4를 넓힌 것, 결정 Q-E2 A). 포구는 이펙트 지점을 대신하지 않습니다.
            string cueError = CombatPresentationCueRules.FindUnitError(attack?.Cues, animation?.Attack?.Clip, points,
                reaction?.HitCues, animation?.Hit != null);
            if (cueError != null)
                throw Invalid($"{cueError}. {unit}, Prefab: {prefab.name}");

            if (attack != null && events != null)
            {
                for (int i = 0; i < attack.Cues.Effects.Count; i++)
                {
                    CombatEffectCue cue = attack.Cues.Effects[i];
                    if (cue.Point == CombatCuePoint.ActorMuzzle)
                    {
                        points.TryGetMuzzle(cue.ActorPoint, out Transform muzzle);
                        RequireOwnedPoint(prefab, muzzle, $"공격 이펙트 큐 {i}의 총구 지점 {cue.ActorPoint}", unit);
                    }
                    else if (cue.Point == CombatCuePoint.ActorEffectPoint)
                    {
                        points.TryGetEffectPoint(cue.ActorPoint, out Transform effectPoint);
                        RequireOwnedPoint(prefab, effectPoint, $"공격 이펙트 큐 {i}의 이펙트 지점 {cue.ActorPoint}", unit);
                    }
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

        private static void RequireOwnedPoint(GameObject prefab, Transform point, string field, string unit)
        {
            // 지점 존재는 바로 앞의 발사/큐 조건 검사에서 확인했습니다. 여기서는 소속만 봅니다.
            if (point != prefab.transform && !point.IsChildOf(prefab.transform))
                throw Invalid($"{field}이 해당 프리팹에 속하지 않습니다: {point.name}. {unit}, Prefab: {prefab.name}");
        }

        /// <summary>
        /// 공격 칸을 쓸 수 있는 공격 표시인지입니다. 근접과 투사체를 쏘는 종류(<see cref="FiresProjectile"/>)입니다.
        /// </summary>
        internal static bool CanUseAttackClip(CombatAttackPresentationData attack) =>
            attack != null && (attack.Kind == CombatAttackPresentationKind.Melee || FiresProjectile(attack.Kind));

        /// <summary>
        /// 공격 클립이 이벤트 타이밍에 투사체를 쏘는 종류(직선·곡사)인지입니다(설계 5차 AC11·AC14, 계획 P10). 이 종류는 포구가 있어야 하고 피격 선딜레이는 0입니다.
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
