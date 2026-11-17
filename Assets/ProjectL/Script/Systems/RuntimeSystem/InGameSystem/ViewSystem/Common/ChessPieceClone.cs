using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 로비 캐릭터 말과 스테이지 맵 현재 위치 말이 함께 쓰는 체스 말 복제본 표시 정책입니다.
    /// 원본 프리팹은 전투와 공유하므로 복제본에만 적용합니다.
    /// </summary>
    internal static class ChessPieceClone
    {
        /// <summary>복제본 자식에 줄 레이어입니다(Unity Default).</summary>
        internal const int DefaultLayer = 0;

        /// <summary>
        /// 복제본 루트 자세를 원점·배율 1로 두고(보정은 호출자의 부모가 맡음), 자식 레이어를 <see cref="DefaultLayer"/>로 바꾸고,
        /// Collider를 끄고, Animator의 루트 모션·이벤트와 전투 사망 부품을 끕니다.
        /// </summary>
        internal static void Prepare(GameObject instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = DefaultLayer;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
            {
                animator.applyRootMotion = false;
                animator.fireEvents = false;
            }
            foreach (CombatUnitRagdoll ragdoll in instance.GetComponentsInChildren<CombatUnitRagdoll>(true))
                ragdoll.enabled = false;
        }
    }
}
