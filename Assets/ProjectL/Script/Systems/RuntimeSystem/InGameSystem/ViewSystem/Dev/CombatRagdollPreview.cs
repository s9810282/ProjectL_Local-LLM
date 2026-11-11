using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>미리보기 프리팹에서 같은 랙돌을 재생 시작과 함께 실행합니다.</summary>
    public sealed class CombatRagdollPreview : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<CombatUnitRagdoll>().Begin(-transform.forward, Vector3.up, 0f);
        }
    }
}
