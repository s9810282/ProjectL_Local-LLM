using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 쓰지 않습니다. 옛 Ballista 코드 모션 길(장전 화살·활시위·재장전 표시)을 걷은 뒤 프리팹 컴포넌트라 남깁니다(View 3라운드 V3-e, Q-V24 A).
    /// 참조 없는 에셋 <c>EnemyUnit_Ballista.prefab</c>이 이 컴포넌트를 가져, 지우면 Missing Script가 생깁니다.
    /// 직렬화 칸 둘은 그 프리팹 값이 그대로 읽히게 둡니다(계획 TP-25).
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    public sealed class BallistaWeaponView : MonoBehaviour
    {
        // 프리팹 값을 보존하려는 칸이라 코드에서 읽지 않습니다.
#pragma warning disable CS0169, CS0649
        [SerializeField] private Transform bolt;
        [SerializeField] private Transform bowString;
#pragma warning restore CS0169, CS0649
    }
}
