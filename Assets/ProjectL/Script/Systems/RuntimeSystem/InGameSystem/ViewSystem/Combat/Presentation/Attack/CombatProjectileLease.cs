using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>발사체 한 번의 대여 수명입니다. 반환된 토큰으로 다음 대여를 회수할 수 없습니다.</summary>
    internal sealed class CombatProjectileLease
    {
        private GameObject _instance;

        internal CombatProjectileLease(GameObject instance)
        {
            _instance = instance;
        }

        /// <summary>활성 대여의 표시 인스턴스입니다. 반환·취소 이후에는 null입니다.</summary>
        internal GameObject Instance => _instance;
        internal bool IsActive => _instance != null;

        internal void Invalidate()
        {
            _instance = null;
        }
    }
}
