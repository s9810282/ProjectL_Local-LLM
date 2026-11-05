using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 걸음 배치 대여 하나입니다(연출 조립 설계 B13, 이동 접촉 설계 M5 (a)). 대여 중에는 사실 그리기가 그 유닛의 위치·회전을 쓰지 않습니다.
    /// 같은 유닛의 새 대여가 이 대여를 대신하면 이 대여의 <see cref="Restore"/>는 아무 일도 하지 않습니다.
    /// </summary>
    internal interface ICombatUnitPlacement
    {
        /// <summary>이 대여가 아직 그 유닛의 배치를 소유하는지입니다.</summary>
        bool IsCurrent { get; }

        /// <summary>
        /// 두 사실의 그리기 위치 사이를 진행률만큼 보간해 씁니다. 가로는 직선이고, 높이는 두 칸 표면 높이를 걸음 모양 <paramref name="style"/>에 묻습니다
        /// (걸음 모양 설계 ST3, 높이 식 원본은 <see cref="CombatStepHeight"/>). 그 유닛의 표시가 없으면 아무 일도 하지 않습니다.
        /// </summary>
        /// <param name="style">이 걸음의 모양입니다. null이면 안 됩니다(레인이 걸음을 시작할 때 모양 표에서 고른 값을 넘깁니다).</param>
        void Interpolate(CharacterCombatFacts from, CharacterCombatFacts to, FieldGridMapper mapper, float progress, CombatStepStyle style);

        /// <summary>배치 소유를 끝냅니다. 여러 번 불러도 한 번만 끝냅니다.</summary>
        void Restore();
    }
}
