using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>공격·반응이 배타적으로 소유하는 한 번의 유닛 표시 대여입니다.</summary>
    /// <remarks>다음 ID·대여를 이전 RestorePose가 수정할 수 없습니다. HUD의 기본 자세도 보존합니다.</remarks>
    internal interface ICombatUnitPose
    {
        bool IsAvailable { get; }
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        void ApplyPose(Vector3 position, Quaternion rotation);
        /// <summary>사망 중 디졸브 양을 씁니다. 지원하지 않는 표시는 아무 일도 하지 않습니다.</summary>
        void SetDeathDissolve(float amount);
        /// <summary>취소·풀 반환 때 사망 디졸브를 원래 값으로 되돌립니다.</summary>
        void ClearDeathDissolve();
        void RestorePose();
    }

    /// <summary>사망 반응에서 자세 대여가 끝날 때까지 모델에 랙돌을 적용합니다.</summary>
    internal interface ICombatUnitRagdollPose
    {
        bool TryBeginDeathRagdoll(Vector3 push, Vector3 groundUp, float groundWidth);
    }
}
