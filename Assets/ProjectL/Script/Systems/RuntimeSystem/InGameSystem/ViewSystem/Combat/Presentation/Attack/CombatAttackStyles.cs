using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 공격 연출 종류 → 전략 표입니다(View 3라운드 W8). 종류 하나에 전략 하나이고, 표를 만들 때 열거 값이 빠짐없이 한 번씩 있는지 검사합니다(설계 P20).
    /// 실패는 형식 초기화 예외 안쪽의 <see cref="InvalidOperationException"/>입니다(계획 TP-28).
    /// </summary>
    internal static class CombatAttackStyles
    {
        private static readonly CombatAttackStyle[] Styles = Build(
            new CombatLinearStyle(),
            new CombatMeleeStyle(),
            new CombatMortarStyle());

        /// <summary>그 종류의 전략입니다. 모르는 종류면 예외입니다(공격 표시 SO가 열거 값만 받으므로 운영에서는 오지 않음).</summary>
        internal static CombatAttackStyle Of(CombatAttackPresentationKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= Styles.Length || Styles[index] == null)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "[CombatAttackStyles] 모르는 공격 연출 종류입니다.");
            return Styles[index];
        }

        private static CombatAttackStyle[] Build(params CombatAttackStyle[] styles)
        {
            var values = (CombatAttackPresentationKind[])System.Enum.GetValues(typeof(CombatAttackPresentationKind));
            int max = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if ((int)values[i] < 0)
                    throw new InvalidOperationException($"[CombatAttackStyles] 음수 종류 값은 표에 넣을 수 없습니다: {values[i]}.");
                max = Math.Max(max, (int)values[i]);
            }

            var table = new CombatAttackStyle[max + 1];
            for (int i = 0; i < styles.Length; i++)
            {
                CombatAttackStyle style = styles[i] ?? throw new InvalidOperationException("[CombatAttackStyles] 빈 전략이 있습니다.");
                int index = (int)style.Kind;
                if (index < 0 || index > max || !System.Enum.IsDefined(typeof(CombatAttackPresentationKind), style.Kind))
                    throw new InvalidOperationException($"[CombatAttackStyles] 열거에 없는 종류의 전략입니다: {style.Kind}.");
                if (table[index] != null)
                    throw new InvalidOperationException($"[CombatAttackStyles] 같은 종류의 전략이 둘입니다: {style.Kind}.");
                table[index] = style;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (table[(int)values[i]] == null)
                    throw new InvalidOperationException($"[CombatAttackStyles] 전략이 없는 종류가 있습니다: {values[i]}.");
            }
            return table;
        }
    }
}
