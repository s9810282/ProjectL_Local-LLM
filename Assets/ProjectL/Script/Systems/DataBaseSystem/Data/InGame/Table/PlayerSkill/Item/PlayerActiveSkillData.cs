using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.PlayerSkill.Item
{
    internal sealed class PlayerActiveSkillData : IData
    {
        internal PlayerSkillDefinition Definition { get; }

        internal PlayerActiveSkillData(PlayerSkillDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }
}
