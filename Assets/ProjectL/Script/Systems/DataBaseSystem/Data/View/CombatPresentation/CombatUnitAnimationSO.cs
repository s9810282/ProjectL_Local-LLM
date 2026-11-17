using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 유닛의 전투 사건 클립 묶음 입력입니다(클립 설계 AC1, 클립 칸 다시 짜기 설계 CS1). 외형 표(<c>UnitVisualData</c>)가 참조하고 여러 유닛이 공유할 수 있습니다.
    /// 칸(공격·피격)마다 동작 하나 = 앞 단계(공격만) + 본 단계(몸 + 탑승물 + 부속) + 뒤 단계 + 이벤트 줄 목록([Event1, 0, 0]은 적지 않아도 맨 앞) + 재생 배율 + 전환 블렌드입니다.
    /// 클립마다 시작·끝을 하나씩 찍고, 본 단계 몸 클립에만 첫 이벤트와 필요할 때 중간 시점 2·3을 더합니다. 클립은 모델 자식의 Transform만 움직입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatUnitAnimation", menuName = "ProjectL/Database/View/Combat Unit Animation")]
    internal sealed class CombatUnitAnimationSO : ScriptableObject
    {
        [Header("공격 — Events 기본 [Event1, 0, 0] = 공격 애니메이션 임팩트 타이밍")]
        [Tooltip("공격 동작입니다. 본 단계 몸 클립을 비우면 공격은 코드 모션입니다. 넣을 수 있는 공격 표시 종류는 외형 표 검사(CombatUnitAnimationRules)가 정합니다. "
            + "이벤트 목록([Event1, 0, 0]은 적지 않아도 맨 앞에 있음): 원거리 = 줄마다 한 발(지점 = 발사 지점, 지연 0), 근접 = 줄마다 맞는 순간(지연 = 이벤트 뒤 맞기까지의 초, 지점은 쓰지 않음).")]
        [SerializeField] private CombatClipMotionInput attack = new();
        [Tooltip("공격 동작(앞 단계가 있으면 앞 단계부터)을 언제 시작하는지입니다. AfterLead = 선행 구간(이 공격 결과의 발동·휩쓸기처럼 공격 사건 앞에 오는 사건)이 끝나고 공격 사건이 시작할 때(기본), "
            + "WithLead = 선행 구간 사건 중 연출이 처음 시작되는 순간 + 지연. 선행 구간이 없는 공격은 둘 다 바로 시작합니다.")]
        [SerializeField] private CombatAttackMotionStart motionStart;
        [Tooltip("WithLead일 때 선행 연출 시작 기준의 공격 동작 시작 초입니다. 0 = 동시 시작, 양수 = 그만큼 늦게, "
            + "음수 = 그만큼 먼저 시작하고 선행 구간의 첫 사건 연출(발동 글자·끌려옴·휩쓸기 걸음 등)과 그 뒤 사건이 그 초만큼 기다립니다(재생 전체가 그만큼 길어짐).")]
        [SerializeField] private float motionStartDelay;

        [Header("피격 — Events 기본 [Event1, 0, 0] = 피격 타이밍")]
        [Tooltip("피격 동작입니다. 본 단계 몸 클립을 비우면 피격은 코드 반응만 씁니다. 앞 단계는 두지 않습니다. 이벤트 목록은 타이밍 표시 자리입니다([Event1, 0, 0]은 적지 않아도 맨 앞에 있음, 지연 0, 지점은 쓰지 않음 — 맞기 전 예열 동작 등을 위해 남김). 큐 시점은 클립 표식만 있으면 됩니다.")]
        [SerializeField] private CombatClipMotionInput hit = new();

        // 옛 시작 모드 칸입니다(0 기본 / 1 지정 Kind / 2 휩쓸기). 시작 기준으로 옮기는 도구(CombatClipSlotMigration 단계 3)만 읽고 DB는 읽지 않습니다(계획 TP-3).
        // 도구가 옮긴 뒤 0으로 비우고, 다음 Phase에 지웁니다.
        [SerializeField, HideInInspector] private int attackAnimationStartMode;
        [SerializeField, HideInInspector] private float attackAnimationStartDelaySeconds;

        /// <summary>입력을 검사하고 매번 새 불변 값을 만듭니다(DB 규칙 P2).</summary>
        internal CombatUnitAnimationData CreateData()
        {
            RequireMigrated();
            string startError = CombatClipAttackData.FindStartError(motionStart, motionStartDelay);
            if (startError != null)
                throw Invalid(startError);
            CombatClipMotion attackMotion;
            CombatClipMotion hitMotion;
            try
            {
                attackMotion = (attack ?? throw new InvalidOperationException("공격 칸이 없습니다")).ToData(nameof(attack), allowsPre: true);
                hitMotion = (hit ?? throw new InvalidOperationException("피격 칸이 없습니다")).ToData(nameof(hit), allowsPre: false);
            }
            catch (InvalidOperationException error)
            {
                throw Invalid(error.Message, error);
            }
            if (motionStart == CombatAttackMotionStart.WithLead && attackMotion == null)
                throw Invalid("motionStart — WithLead는 공격 본 단계 몸 클립이 있어야 합니다");
            if (attackMotion == null && hitMotion == null)
                throw Invalid("공격·피격 클립이 모두 비어 있습니다(묶음을 연결하지 않으면 코드 모션입니다)");
            return new CombatUnitAnimationData(
                attackMotion != null ? new CombatClipAttackData(attackMotion, motionStart, motionStartDelay) : null, hitMotion);
        }

        // 옛 시작 모드 칸에만 값이 남아 있으면(옮기기 도구 단계 3을 아직 돌리지 않음) 조용히 기본 기준으로 두지 않고 멈춥니다(설계 CS9).
        private void RequireMigrated()
        {
            if (attackAnimationStartMode != 0)
                throw Invalid($"옛 시작 모드 칸(attackAnimationStartMode = {attackAnimationStartMode})에 값이 남아 있습니다. "
                    + "메뉴 ProjectL > Combat > 클립 칸 옮기기 (3) 시작 기준을 실행하세요");
        }

        private InvalidOperationException Invalid(string reason, Exception inner = null)
            => new($"[CombatUnitAnimationSO] 클립 묶음 값이 잘못되었습니다: {reason}, Asset: {name}", inner);
    }
}
