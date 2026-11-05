using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 걷기 모양입니다(걸음 모양 설계 ST2). 지금까지의 걸음 그대로입니다. 같은 높이는 평평하고 높이가 다른 칸 사이에서만 뛰며,
    /// 이동자 몸이 직선으로 지나며 처음 닿는 순간 접촉합니다(이동 접촉 설계 M2). 어느 이동 종류가 이 모양을 쓰는지는 <see cref="CombatStepStyles.From"/>가 정합니다.
    /// </summary>
    internal sealed class CombatWalkStepStyle : CombatStepStyle
    {
        private readonly float _seconds;
        private readonly float _climbHop;

        /// <param name="seconds">걸음 하나의 시간입니다(설정 <c>movementStepSeconds</c>).</param>
        /// <param name="climbHop">높이가 다른 칸 사이에서 높은 칸보다 더 올라가는 높이입니다(설정 <c>stepClimbHop</c>).</param>
        internal CombatWalkStepStyle(float seconds, float climbHop)
        {
            CombatPresentationSettingsData.ValidateStepSeconds(seconds, nameof(seconds));
            CombatPresentationSettingsData.ValidateStepClimbHop(climbHop, nameof(climbHop));
            _seconds = seconds;
            _climbHop = climbHop;
        }

        internal override float Seconds => _seconds;

        internal override float HeightAt(float fromHeight, float toHeight, float progress) =>
            CombatStepHeight.At(fromHeight, toHeight, progress, _climbHop);

        // 몸이 지나며 처음 닿는 순간이라 원본마다 다릅니다.
        internal override bool TryGetFixedContact(out float progress)
        {
            progress = 0f;
            return false;
        }

        internal override float ContactProgress(CombatBody moverStart, Vector2 moverEndCenter, CombatBody source) =>
            CombatStepContacts.ContactProgress(moverStart, moverEndCenter, source);
    }
}
