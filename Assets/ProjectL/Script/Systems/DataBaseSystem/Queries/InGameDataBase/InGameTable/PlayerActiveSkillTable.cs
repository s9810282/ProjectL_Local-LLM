using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.PlayerSkill.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable
{
    internal interface IPlayerActiveSkillTableQuery : IServiceModule
    {
        PlayerActiveSkillData GetPlayerSkill(PlayerSkillKey key);
        bool TryGetPlayerSkill(PlayerSkillKey key, out PlayerActiveSkillData data);
        bool TryGetPlayerSkillForMain(EntityKey main, out PlayerActiveSkillData data);
    }

    internal sealed class PlayerActiveSkillTable : IPlayerActiveSkillTableQuery, IExportSystemServices
    {
        private readonly Dictionary<PlayerSkillKey, PlayerActiveSkillData> _bySkill = new();
        private readonly Dictionary<EntityKey, PlayerActiveSkillData> _byMain = new();

        internal PlayerActiveSkillTable(IDataProvider<IDatabaseItem> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (var item in source.GetAllData())
            {
                if (item is not PlayerActiveSkillDataSO row || row.GetData() is not PlayerActiveSkillData data)
                    throw new InvalidOperationException("[PlayerActiveSkillTable] 스킬 정의 SO가 필요합니다.");

                var definition = data.Definition;
                if (!_bySkill.TryAdd(definition.Key, data))
                    throw new InvalidOperationException($"[PlayerActiveSkillTable] 중복 스킬: {definition.Key} ({row.name}).");
                if (!_byMain.TryAdd(definition.MainCharacterKey, data))
                    throw new InvalidOperationException($"[PlayerActiveSkillTable] 중복 메인: {definition.MainCharacterKey} ({row.name}).");
            }
        }

        public PlayerActiveSkillData GetPlayerSkill(PlayerSkillKey key) => TryGetPlayerSkill(key, out var data) ? data :
            throw new KeyNotFoundException($"[PlayerActiveSkillTable] 스킬이 없습니다: {key}.");

        public bool TryGetPlayerSkill(PlayerSkillKey key, out PlayerActiveSkillData data) => _bySkill.TryGetValue(key, out data);

        public bool TryGetPlayerSkillForMain(EntityKey main, out PlayerActiveSkillData data) => _byMain.TryGetValue(main, out data);

        internal static PlayerSkillDefinition FindForMain(IInGameQuery query, EntityKey main) =>
            (query as IHasPlayerActiveSkillTable)?.PlayerActiveSkills is { } table &&
            table.TryGetPlayerSkillForMain(main, out var data) ? data.Definition : null;

        /// <summary>스킬 SO의 참조와 별개로 실제 카탈로그에서 사용하는 메인 정의를 확인한다.</summary>
        internal void ValidateOwners(IUnitTableQuery units)
        {
            foreach (var row in _bySkill.Values)
            {
                var definition = row.Definition;
                var key = definition.MainCharacterKey;
                string source = $"[PlayerActiveSkillTable] Skill={definition.Key}, Main={key}";
                if (!units.TryGetUnit(key, out var unit) || unit.Role != UnitRole.MainCharacter ||
                    unit.OwnerEligibility != UnitOwnerEligibility.PlayerOnly || unit.Category != CategoryKey.Main)
                    throw new InvalidOperationException($"{source}: 유닛 카탈로그에 일치하는 플레이어 메인 정의가 필요합니다.");
                definition.ValidateTraits(unit.Traits.Select(t => t.TraitKey));
                if (definition.Key == PlayerSkillKey.MagicBullet && unit.Traits.Count(t => t.TraitKey == TraitKey.PiercingFalloff) != 1)
                    throw new InvalidOperationException($"{source}: 관통 감쇠 설정이 하나 필요합니다.");
            }
        }

        public void ExportServices(ISystemWriteSink sink) => sink.AddService<IPlayerActiveSkillTableQuery>(this);
    }
}
