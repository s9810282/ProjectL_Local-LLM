using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 유닛의 전투 사건 클립 묶음 입력입니다(클립 설계 AC1). 외형 표(<c>UnitVisualData</c>)가 참조하고 여러 유닛이 공유할 수 있습니다.
    /// 클립마다 시작·첫 이벤트·끝을 하나씩 찍고, 중간 시점 2·3은 필요할 때만 더합니다. 클립은 모델 자식의 Transform만 움직입니다.
    /// 칸마다 주 클립(몸, 이벤트 셋) 하나와 같은 시각으로 같이 도는 부속 클립(무기 등, 이벤트 없음)을 둡니다(설계 6차 AC16).
    /// </summary>
    [CreateAssetMenu(fileName = "CombatUnitAnimation", menuName = "ProjectL/Database/View/Combat Unit Animation")]
    internal sealed class CombatUnitAnimationSO : ScriptableObject
    {
        [Header("공격")]
        [Tooltip("공격 클립(주 클립, 몸)입니다. ClipStart·ClipEvent·ClipEnd를 하나씩 찍고 ClipEvent2·3은 필요할 때만 더합니다. 비워 두면 공격은 코드 모션입니다. 넣을 수 있는 공격 표시 종류는 외형 표 검사(CombatUnitAnimationRules)가 정합니다.")]
        [SerializeField] private AnimationClip attackClip;
        [Tooltip("공격 클립과 같은 시각으로 같이 재생할 부속 클립(무기 등)입니다. 이벤트를 찍지 않고, 공격 클립과 다른 뼈만 움직입니다. 비워도 됩니다.")]
        [SerializeField] private AnimationClip[] attackCompanionClips = Array.Empty<AnimationClip>();
        [Tooltip("이벤트 타이밍 뒤 대상이 맞기까지의 초. 피격자의 피격 모션이 이때 시작합니다. 원거리(직선·곡사)는 0으로 둡니다(맞는 순간은 투사체가 정함, 규칙은 CombatUnitAnimationRules)")]
        [SerializeField, Min(0f)] private float hitDelay;
        [Tooltip("선택 발사 목록입니다. 비우면 ClipEvent에서 기본 지점 0으로 한 발을 쏩니다. 넣으면 이 목록만 사용하며 원거리 공격 클립에서만 허용합니다.")]
        [SerializeField] private CombatClipShotEntry[] shots = Array.Empty<CombatClipShotEntry>();
        // 옛 칸 soundDelay·attackEffect·effectDelay는 공격 묶음의 큐(소리 시점·이펙트)로 옮겼다(범용 연출 구조 계획 Gd.3). YAML의 옛 키는 다시 저장할 때 지운다.
        [Header("피격")]
        [Tooltip("피격 클립(주 클립, 몸)입니다. 비워 두면 피격은 코드 반응만 씁니다.")]
        [SerializeField] private AnimationClip hitClip;
        [Tooltip("피격 클립과 같은 시각으로 같이 재생할 부속 클립(무기 등)입니다. 이벤트를 찍지 않고, 피격 클립과 다른 뼈만 움직입니다. 비워도 됩니다.")]
        [SerializeField] private AnimationClip[] hitCompanionClips = Array.Empty<AnimationClip>();

        /// <summary>입력을 검사하고 매번 새 불변 값을 만듭니다(DB 규칙 P2). 피격 선딜레이는 공격 클립이 있을 때만 봅니다.</summary>
        internal CombatUnitAnimationData CreateData()
        {
            if (attackClip == null && shots != null && shots.Length > 0)
                throw Invalid("발사 목록(shots)은 주 공격 클립(attackClip)이 있어야 합니다");
            if (attackClip == null && hitClip == null && !HasAny(attackCompanionClips) && !HasAny(hitCompanionClips))
                throw Invalid("공격·피격 클립이 모두 비어 있습니다(묶음을 연결하지 않으면 코드 모션입니다)");
            RequireMainClip(attackClip, attackCompanionClips, nameof(attackClip), nameof(attackCompanionClips));
            RequireMainClip(hitClip, hitCompanionClips, nameof(hitClip), nameof(hitCompanionClips));
            CombatClipAttackData attack = null;
            if (attackClip != null)
            {
                if (!CombatClipAttackData.IsValidDelay(hitDelay))
                    throw Invalid($"{nameof(hitDelay)} — {CombatClipAttackData.DelayRule}");
                try
                {
                    attack = new CombatClipAttackData(ReadClip(attackClip, attackCompanionClips, nameof(attackClip)), hitDelay, ReadShots());
                }
                catch (ArgumentException error)
                {
                    throw Invalid($"{nameof(shots)} — {error.Message}", error);
                }
            }

            CombatClipData hit = hitClip != null ? ReadClip(hitClip, hitCompanionClips, nameof(hitClip)) : null;
            return new CombatUnitAnimationData(attack, hit);
        }

        private CombatClipShot[] ReadShots()
        {
            if (shots == null || shots.Length == 0) return Array.Empty<CombatClipShot>();
            var values = new CombatClipShot[shots.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (shots[i] == null) throw Invalid($"{nameof(shots)}의 {i}번 칸이 비어 있습니다");
                values[i] = shots[i].ToData();
            }
            return values;
        }

        /// <summary>
        /// 클립 규칙(길이·반복·루트 곡선, <see cref="CombatUnitAnimationRules.RequireEventClip"/>), 부속 규칙(<see cref="CombatUnitAnimationRules.RequireCompanionClips"/>)과
        /// 이벤트 셋을 보고 값으로 만듭니다.
        /// </summary>
        private CombatClipData ReadClip(AnimationClip clip, AnimationClip[] companions, string field)
        {
            CombatClipTimings timings;
            try
            {
                CombatUnitAnimationRules.RequireEventClip(clip);
                CombatUnitAnimationRules.RequireCompanionClips(clip, companions);
                timings = CombatClipEvents.ReadTimings(clip);
            }
            catch (InvalidOperationException error)
            {
                throw Invalid($"{field} — {error.Message}", error);
            }

            return new CombatClipData(clip, timings, companions);
        }

        /// <summary>주 클립 없이 부속만 있는 칸은 시점의 원본이 없어 거절합니다(계획 P15).</summary>
        private void RequireMainClip(AnimationClip clip, AnimationClip[] companions, string clipField, string companionField)
        {
            if (clip == null && HasAny(companions))
                throw Invalid($"{companionField} — 주 클립({clipField}) 없이 부속 클립만 있습니다(부속은 주 클립의 시각으로만 돕니다)");
        }

        private static bool HasAny(AnimationClip[] clips) => clips != null && clips.Length > 0;

        private InvalidOperationException Invalid(string reason, Exception inner = null)
            => new($"[CombatUnitAnimationSO] 클립 묶음 값이 잘못되었습니다: {reason}, Asset: {name}", inner);
    }
}
