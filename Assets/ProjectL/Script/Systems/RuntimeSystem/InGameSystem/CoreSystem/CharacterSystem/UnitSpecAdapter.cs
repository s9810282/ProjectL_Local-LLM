using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    /// <summary>
    /// DB 유닛·병과 정의를 Core Contracts 명세로 바꾼다. 전투 정의와 보유 능력치 조회가 같이 쓰며
    /// 스테이지·필드·적·환경 정보를 요구하지 않는다. 상태 생성·저장은 담당하지 않는다.
    /// </summary>
    internal static class UnitSpecAdapter
    {
        internal static UnitSpec ReadUnit(UnitData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Validate();
            var traits = new List<TraitSpec>();
            foreach (var trait in data.Traits)
            {
                traits.Add(TraitSpecAdapter.Read(trait));
            }
            return new UnitSpec(data.Key, data.Role, data.FootprintWidth, data.FootprintHeight,
                FootprintCells(data.FootprintWidth, data.FootprintHeight), data.MaxHealth, MoveMethod.LiftAndPlace, data.AttackRange, data.Category,
                data.AttackPower, data.PhysicalDefense,
                data.Priority, traits, data.OwnerEligibility, data.Occlusion, behavior: data.Behavior,
                killReward: data.KillReward);
        }

        /// <summary>
        /// 유닛 정의와 그 유닛이 참조하는 DB 지정 방식으로 Core 공격 프로필을 만든다. 전투 정의와 보유 능력치 조회가 같이 쓴다.
        /// 공격하지 않는 유닛(지정 방식 키 0)은 거절한다(정의 조회와 공격 가능 여부 분리는 후속 과제).
        /// 지정 방식 값의 해석과 특성·지정 방식 결합은 Core 계약(TargetingMethodSpec·CombatAttackProfile)이 검사한다.
        /// </summary>
        internal static CombatAttackProfile ReadProfile(IInGameQuery query, UnitSpec unit)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            UnitData data = query.UnitTableQuery.GetUnit(unit.Key);
            if (data.TargetingMethodKey == 0)
                throw new InvalidOperationException(
                    $"공격하지 않는 유닛(지정 방식 키 0)은 아직 공격 프로필을 만들 수 없습니다: {unit.Key}.");
            var method = query.TargetingMethodTableQuery.GetTargetingMethod(data.TargetingMethodKey);
            try
            {
                return new CombatAttackProfile(unit, ReadMethod(method));
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"DB 공격 지정 방식을 Core 공격 프로필로 바꿀 수 없습니다: {unit.Key}, Method {data.TargetingMethodKey}.", exception);
            }
        }

        /// <summary>DB 공격 지정 방식을 Core 지정 방식 명세로 바꾼다. 필드 대응의 유일한 정의다.</summary>
        internal static TargetingMethodSpec ReadMethod(TargetingMethodData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Validate();
            return new TargetingMethodSpec(data.Shape, data.ChoiceMode, data.Splash, data.SplashPercent, data.ChoiceAngle, data.Width);
        }

        /// <summary>병과 설정으로 Core 병과 명세 전체(필수 4종 + Boss)와 각 병과의 레벨당 상승치를 만든다.</summary>
        internal static CategorySpec[] ReadCategories(CategoryConfigData config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate();
            return new[]
            {
                new CategorySpec(CategoryKey.Main,Array.Empty<CategoryRuleSpec>(),                    config.MainMovementAllowance,config.MainLevelGrowth),
                new CategorySpec(CategoryKey.Rider,Array.Empty<CategoryRuleSpec>(),                    config.RiderMovementAllowance,config.RiderLevelGrowth),
                new CategorySpec(CategoryKey.Heavy,Array.Empty<CategoryRuleSpec>(),                    config.HeavyMovementAllowance,config.HeavyLevelGrowth),
                new CategorySpec(CategoryKey.Cannon,new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() },                    config.CannonMovementAllowance,config.CannonLevelGrowth),
                new CategorySpec(CategoryKey.Boss,Array.Empty<CategoryRuleSpec>(),                    config.BossMovementAllowance,config.BossLevelGrowth)
            };
        }

        /// <summary>너비·높이 직사각형의 로컬 칸 목록이다. 유닛과 구조물 정의가 같이 쓴다.</summary>
        internal static CellCoord[] FootprintCells(int width, int height)
        {
            var cells = new CellCoord[checked(width * height)];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) cells[y * width + x] = new CellCoord(x, y);
            return cells;
        }
    }
}
