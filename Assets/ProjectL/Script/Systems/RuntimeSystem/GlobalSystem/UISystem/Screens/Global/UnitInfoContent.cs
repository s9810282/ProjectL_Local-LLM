using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>조회한 사실을 상세 위젯에 전달하는 표시값입니다. 규칙 계산이나 선택 상태는 갖지 않습니다.</summary>
    internal sealed class UnitInfoContent
    {
        internal UnitInfoContent(string category, string name, Sprite portrait, int? level, int? health,
            int? attack, int? defense, int? moveDistance, int? attackDistance, UnitCardTraitContent[] abilities)
        {
            Category = category;
            Name = name;
            Portrait = portrait;
            Level = level;
            Health = health;
            Attack = attack;
            Defense = defense;
            MoveDistance = moveDistance;
            AttackDistance = attackDistance;
            Abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
        }

        internal string Category { get; }
        internal string Name { get; }
        internal Sprite Portrait { get; }
        internal int? Level { get; }
        internal int? Health { get; }
        internal int? Attack { get; }
        internal int? Defense { get; }
        internal int? MoveDistance { get; }
        internal int? AttackDistance { get; }
        internal UnitCardTraitContent[] Abilities { get; }
    }
}
