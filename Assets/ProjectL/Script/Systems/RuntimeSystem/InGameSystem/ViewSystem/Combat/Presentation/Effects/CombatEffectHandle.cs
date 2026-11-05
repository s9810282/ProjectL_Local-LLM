using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 창구가 낸 이펙트 하나의 손잡이입니다(이펙트 끝 규칙 설계 EL5). 거두기와 옮기기만 합니다. 받침(<see cref="CombatEffectFollower"/>)은 다시 쓰이므로
    /// 받침의 사용 번호를 함께 들고, 그 이펙트가 이미 끝나 받침이 다른 이펙트를 받았으면 아무것도 하지 않습니다.
    /// </summary>
    /// <remarks>값 형식이라 낼 때 할당이 없습니다. 기본값은 빈 손잡이이고 거두기는 아무것도 하지 않습니다.</remarks>
    internal readonly struct CombatEffectHandle
    {
        private readonly CombatEffectFollower _follower;
        private readonly int _use;

        internal CombatEffectHandle(CombatEffectFollower follower, int use)
        {
            _follower = follower;
            _use = use;
        }

        /// <summary>그 이펙트가 아직 보이는지입니다(거두기·끝·멈춤 전).</summary>
        internal bool IsLive => _follower != null && _follower.IsLive(_use);

        /// <summary>그 이펙트를 거둡니다(링 버퍼면 바로 지우고, 아니면 방출만 멈춤). 이미 끝났으면 아무것도 하지 않습니다.</summary>
        internal void Recall()
        {
            if (_follower != null)
                _follower.Recall(_use);
        }

        /// <summary>이 사용 번호의 남은 입자까지 바로 지웁니다. 반환·재사용된 다른 이펙트는 건드리지 않습니다.</summary>
        internal void RecallImmediately()
        {
            if (_follower != null)
                _follower.RecallImmediately(_use);
        }

        /// <summary>그 이펙트의 받침을 월드 위치로 옮깁니다(하붑 View 계획 HP-23). 이미 끝났거나 받침이 다른 이펙트를 받았으면 아무것도 하지 않습니다.</summary>
        internal void MoveTo(Vector3 position)
        {
            if (_follower != null)
                _follower.MoveTo(_use, position);
        }
    }
}
