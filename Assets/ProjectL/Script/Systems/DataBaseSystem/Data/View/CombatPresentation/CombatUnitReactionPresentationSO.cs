using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>여러 캐릭터가 공유하는 피격·사망 표현 입력입니다. 뼈대(반응 동작 값)와 칸마다의 연출 큐를 둡니다(범용 연출 구조 설계 G1·G2).</summary>
    [CreateAssetMenu(fileName = "CombatUnitReactionPresentation", menuName = "ProjectL/Database/View/Combat Unit Reaction Presentation")]
    internal sealed class CombatUnitReactionPresentationSO : ScriptableObject
    {
        [Header("피격")]
        [SerializeField, Min(0.01f)] private float hitDuration = 0.18f;
        [SerializeField, Min(0f)] private float hitCellFraction = 0.05f;
        [SerializeField, Range(0f, 60f)] private float hitAngle = 4f;
        [Tooltip("피격 클립이 있는 유닛도 코드 피격 움직임(위 값: 맞은 방향으로 밀렸다 돌아오며 젖힘, 넉백 때 밀리는 동안 젖힘)을 냅니다.\n"
            + "끄면(기본) 피격 클립이 있는 유닛은 클립만 움직입니다. 피격 시간·피격 칸 큐(플래시·이펙트·흔들림)·유닛 피격 소리 줄은 그대로입니다.\n"
            + "피격 클립이 없는 유닛은 이 칸과 상관없이 늘 냅니다. 사망 반응은 바뀌지 않습니다.")]
        [SerializeField] private bool hitMotionWithClip;
        [Tooltip("피격 칸 큐입니다. 플래시(빨강으로 바뀌었다 돌아옴, 시간 0이면 없음)·이펙트(자리 SubjectBody = 맞은 유닛 몸 중심, +Z가 맞은 방향)·카메라 흔들림을 둡니다. "
            + "피격 소리는 유닛 외형의 \"전투 소리\" 줄입니다. 시점 Event1~3은 피격 클립이 있는 유닛만 씁니다.")]
        [SerializeField] private CombatHitCueInput hitCues = new();
        [Header("사망")]
        [SerializeField, Min(0.01f)] private float deathDuration = 0.55f;
        [SerializeField, Min(0f)] private float deathRestDuration = 0.2f;
        [Tooltip("사망 칸 큐입니다. 이펙트(치명타, 자리 SubjectBody)·카메라 흔들림을 둡니다. 피격·사망 소리는 모든 유닛이 같이 쓰는 이 묶음이 아니라 유닛 외형의 \"전투 소리\" 줄입니다.")]
        [SerializeField] private CombatDeathCueInput deathCues = new();

        // 옛 칸 hitFlashColor·hitFlashDuration·hitEffect는 피격·사망 칸 큐로 옮겼다(범용 연출 구조 계획 Gd.3·Gd.9). 에셋 YAML의 옛 키는 Unity가 다시 저장할 때 지운다.

        /// <summary>
        /// 매번 새 불변 표시 값을 만듭니다. 값 조건의 원본은 값 객체 생성자이고(범용 연출 구조 계획 Ga.3), 여기서는 실패에 에셋 이름을 붙입니다.
        /// 값 인자 이름은 칸 이름과 같습니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">값이 규칙에 맞지 않을 때입니다(부트의 DB 생성이 멈춥니다).</exception>
        internal CombatUnitReactionPresentationData CreateData()
        {
            try
            {
                return new CombatUnitReactionPresentationData(hitDuration, hitCellFraction, hitAngle, deathDuration, deathRestDuration,
                    (hitCues ?? throw new ArgumentException("피격 큐 칸이 없습니다.", nameof(hitCues))).ToCues(),
                    (deathCues ?? throw new ArgumentException("사망 큐 칸이 없습니다.", nameof(deathCues))).ToCues(),
                    hitMotionWithClip);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(
                    $"[CombatUnitReactionPresentationSO] 반응 표현 값이 잘못되었습니다: {e.ParamName}. Asset: {name} — {e.Message}", e);
            }
        }
    }
}
