using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 뛰기 모양입니다(걸음 모양 설계 ST2·ST5). 칸마다 한 번 늘 호로 뜁니다(꼭대기 = 두 칸 중 높은 칸 표면 + 뛰기 높이).
    /// 몸이 공중을 지나므로 접촉은 착지 순간(진행률 1)입니다. 그래서 걸음 사건 완료(첫 접촉 묶음 해제)도 착지 순간입니다.
    /// </summary>
    internal sealed class CombatHopStepStyle : CombatStepStyle
    {
        // 착지 = 보간 끝입니다.
        private const float Landing = 1f;

        private readonly float _seconds;
        private readonly float _height;

        /// <param name="seconds">뛰기 한 번의 시간입니다(설정 <c>hopSeconds</c>).</param>
        /// <param name="height">꼭대기가 두 칸 중 높은 칸 표면보다 더 올라가는 높이입니다(설정 <c>hopHeight</c>).</param>
        internal CombatHopStepStyle(float seconds, float height)
        {
            CombatPresentationSettingsData.ValidateStepSeconds(seconds, nameof(seconds));
            CombatPresentationSettingsData.ValidateHopHeight(height, nameof(height));
            _seconds = seconds;
            _height = height;
        }

        internal override float Seconds => _seconds;

        internal override float HeightAt(float fromHeight, float toHeight, float progress) =>
            CombatStepHeight.Arc(fromHeight, toHeight, progress, _height);

        // 공중을 지나는 동안은 닿지 않습니다. 착지에 모든 묶음이 해제됩니다(설계 ST5). 몸을 구할 필요가 없습니다.
        internal override bool TryGetFixedContact(out float progress)
        {
            progress = Landing;
            return true;
        }

        internal override float ContactProgress(CombatBody moverStart, Vector2 moverEndCenter, CombatBody source) => Landing;
    }
}
