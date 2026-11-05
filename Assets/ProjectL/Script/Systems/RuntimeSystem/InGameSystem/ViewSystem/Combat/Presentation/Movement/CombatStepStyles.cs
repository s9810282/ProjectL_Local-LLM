using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 이동 종류 → 걸음 모양 표입니다(걸음 모양 설계 ST2, 계획 P6). 종류 하나에 모양 하나이고, 만들 때 열거의 모든 값에 모양이 있는지 검사합니다
    /// (빠지면 전투 표시 조립에서 실패합니다). 값이 설정 표에서 오므로 공격 종류 표(<c>CombatAttackStyles</c>)와 달리 인스턴스입니다.
    /// </summary>
    /// <remarks>새 이동 종류는 열거 값 하나 + 성질 한 줄(<see cref="CombatMovementKinds"/>) + 모양 클래스 하나 + 이 표 한 줄입니다. 걸음 레인·배치 대여·접촉 게이트·이동 표시는 바뀌지 않습니다(설계 ST6).</remarks>
    internal sealed class CombatStepStyles
    {
        private static readonly CombatMovementKind[] Kinds = (CombatMovementKind[])System.Enum.GetValues(typeof(CombatMovementKind));

        private readonly CombatStepStyle[] _styles;

        /// <param name="voluntary">자발 이동의 모양입니다(설정이 고름).</param>
        /// <param name="forced">밀림의 모양입니다.</param>
        internal CombatStepStyles(CombatStepStyle voluntary, CombatStepStyle forced)
        {
            _styles = new CombatStepStyle[TableSize()];
            _styles[(int)CombatMovementKind.Voluntary] = voluntary ?? throw new ArgumentNullException(nameof(voluntary));
            _styles[(int)CombatMovementKind.Forced] = forced ?? throw new ArgumentNullException(nameof(forced));
            for (int i = 0; i < Kinds.Length; i++)
            {
                if (_styles[(int)Kinds[i]] == null)
                    throw new InvalidOperationException($"[CombatStepStyles] 모양이 없는 이동 종류가 있습니다: {Kinds[i]}.");
            }
        }

        /// <summary>
        /// 사건 Presenter 값 묶음으로 만듭니다. 밀림은 늘 걷기이고(설계 Q-ST2), 걷기 하나를 밀림과(모양이 Walk면) 자발 이동이 함께 씁니다.
        /// 이 규칙과 걸음 모양(Walk·Hop) → 모양 클래스 대응은 여기 한 곳입니다(계획 P5).
        /// </summary>
        internal static CombatStepStyles From(CombatEventPresentationValues values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var walk = new CombatWalkStepStyle(values.StepSeconds, values.StepClimbHop);
            CombatStepLookSettings look = values.StepLook;
            CombatStepStyle voluntary = look.Look switch
            {
                CombatStepLook.Walk => walk,
                CombatStepLook.Hop => new CombatHopStepStyle(look.HopSeconds, look.HopHeight),
                _ => throw new ArgumentOutOfRangeException(nameof(values), look.Look, "[CombatStepStyles] 모르는 걸음 모양입니다."),
            };
            return new CombatStepStyles(voluntary, walk);
        }

        /// <summary>그 이동 종류의 모양입니다. 걸음을 시작할 때 한 번 부릅니다. 모르는 종류면 예외입니다.</summary>
        internal CombatStepStyle For(CombatMovementKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= _styles.Length || _styles[index] == null)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "[CombatStepStyles] 모르는 이동 종류입니다.");
            return _styles[index];
        }

        private static int TableSize()
        {
            int max = 0;
            for (int i = 0; i < Kinds.Length; i++)
            {
                if ((int)Kinds[i] < 0)
                    throw new InvalidOperationException($"[CombatStepStyles] 음수 종류 값은 표에 넣을 수 없습니다: {Kinds[i]}.");
                max = Math.Max(max, (int)Kinds[i]);
            }
            return max + 1;
        }
    }
}
