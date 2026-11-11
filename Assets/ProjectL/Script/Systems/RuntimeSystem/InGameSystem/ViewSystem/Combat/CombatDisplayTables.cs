using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Haboob.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.ThornField.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 전투 씬 표시(<see cref="CombatSceneDisplay"/>)가 쓰는 View DB 조회 묶음입니다(구조 리팩토링 설계 D4).
    /// </summary>
    /// <remarks>
    /// <see cref="CombatViewSystem"/>은 이 묶음만 보관하고 표시 구현의 개별 표를 알지 않습니다.
    /// <c>CombatSceneDisplayFactory</c>가 개별 표를 필요한 협력 객체에 전달합니다. 값은 View DB가 원본이며 여기서는 참조만 들고 있습니다.
    /// </remarks>
    internal sealed class CombatDisplayTables
    {
        /// <summary>모든 표를 받습니다. 부르는 곳은 View DB에서 고르는 <see cref="From"/> 하나입니다.</summary>
        internal CombatDisplayTables(
            StageFieldViewData field,
            RailwayVisualData railway,
            ThornFieldVisualData thornField,
            RockfallVisualData rockfall,
            HaboobVisualData haboob,
            IUnitVisualTableQuery units,
            ITraitVisualTableQuery traits,
            IStageViewPropTableQuery stageProps,
            IObjectVisualTableQuery objects,
            ITerrainVisualTableQuery terrains,
            IStageEnvironmentVisualTableQuery environments,
            CombatPresentationSettingsData presentationSettings,
            IConsumableItemVisualTableQuery items,
            ICategoryVisualTableQuery categories,
            IUnitStatusVisualTableQuery statuses)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            Railway = railway ?? throw new ArgumentNullException(nameof(railway));
            ThornField = thornField ?? throw new ArgumentNullException(nameof(thornField));
            Rockfall = rockfall ?? throw new ArgumentNullException(nameof(rockfall));
            Haboob = haboob ?? throw new ArgumentNullException(nameof(haboob));
            Units =units ?? throw new ArgumentNullException(nameof(units));
            // 특성 외형은 유닛 HUD 표시에 필요한 필수 의존성이다(구조 개선 VR5). 기본값 null로 숨기지 않는다.
            Traits = traits ?? throw new ArgumentNullException(nameof(traits));
            StageProps = stageProps ?? throw new ArgumentNullException(nameof(stageProps));
            Objects = objects ?? throw new ArgumentNullException(nameof(objects));
            Terrains = terrains ?? throw new ArgumentNullException(nameof(terrains));
            Environments = environments ?? throw new ArgumentNullException(nameof(environments));
            PresentationSettings = presentationSettings ?? throw new ArgumentNullException(nameof(presentationSettings));
            // 아이템 외형은 가방 칸과 유닛 HUD 아이콘에 필요한 필수 의존성이다(가방 설계 P3).
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Categories = categories ?? throw new ArgumentNullException(nameof(categories));
            // 걸린 상태(버프·디버프) HUD 칸의 줄 표다. 만들 때 줄마다 외형·설명 틀을 확인한다(특성 발동 연출 설계 TA9).
            UnitStatuses = ProjectL.Systems.Factory.Runtime.InGame.View.CombatUnitStatusFactory.Create(
                statuses ?? throw new ArgumentNullException(nameof(statuses)), Traits, Rockfall);
            // 소리 프리셋은 외형 표·설정 표에서 고른다. 입력과 표시가 같은 객체를 받는다(View 3라운드 W9-2, 계획 TP-26).
            SoundPresets = new CombatSoundPresets(Units, Objects, Items, PresentationSettings);
            // 사건 순간의 연출 큐도 같은 외형 표·설정 표에서 읽는다. 소리 프리셋과는 다른 값이라 따로 둔다(이펙트 창구 확장 설계 §7.4).
            // 특성 발동 큐는 특성 외형 표의 발동 연출 줄에서 읽는다(특성 발동 연출 설계 TA2).
            CueSources = new CombatCueSources(Units, Objects, Items, Traits, PresentationSettings, statuses);
        }

        /// <summary>필드 외형 설정(칸 프리팹·표시 모드·유닛 크기 등)입니다.</summary>
        internal StageFieldViewData Field { get; }

        /// <summary>레일·열차 외형 설정입니다.</summary>
        internal RailwayVisualData Railway { get; }

        /// <summary>가시밭 칸 외형 설정입니다. 가시밭 칸 외형의 원본은 이 표 한 곳입니다(구조 리팩토링 설계 D12).</summary>
        internal ThornFieldVisualData ThornField { get; }

        /// <summary>낙석 표현 설정입니다(돌·느낌표 프리팹, 낙하 값, 큐, 소리, 호버 설명).</summary>
        internal RockfallVisualData Rockfall { get; }

        /// <summary>하붑 표현 설정입니다(구름 이펙트, 소리, 호버 설명).</summary>
        internal HaboobVisualData Haboob { get; }

        internal IUnitVisualTableQuery Units { get; }

        internal ITraitVisualTableQuery Traits { get; }
        internal ICategoryVisualTableQuery Categories { get; }

        internal IStageViewPropTableQuery StageProps { get; }

        internal IObjectVisualTableQuery Objects { get; }

        internal ITerrainVisualTableQuery Terrains { get; }

        /// <summary>스테이지별 환경 배치 회전입니다(Object·Terrain 외형이 함께 씁니다).</summary>
        internal IStageEnvironmentVisualTableQuery Environments { get; }

        /// <summary>전투 연출의 전역 시간·크기 값입니다. 원본은 View DB 설정 표 한 곳입니다(연출 조립 설계 §9 I-5).</summary>
        internal CombatPresentationSettingsData PresentationSettings { get; }

        /// <summary>소모 아이템의 아이콘·이름·설명입니다. 키는 InGame 아이템 키와 같습니다(가방 설계 P3).</summary>
        internal IConsumableItemVisualTableQuery Items { get; }

        /// <summary>전투 소리 프리셋 번호의 답입니다. 원본은 유닛·오브젝트·아이템 외형 표와 전투 연출 설정 표입니다(View 3라운드 W9-2, 소리 창구 2차).</summary>
        internal CombatSoundPresets SoundPresets { get; }

        /// <summary>사건 순간의 연출 큐 값의 답입니다. 원본은 오브젝트·유닛(장애물)·아이템 외형 표와 전투 연출 설정 표입니다(이펙트 창구 확장 설계 EC5).</summary>
        internal CombatCueSources CueSources { get; }

        /// <summary>유닛에 걸린 상태(버프·디버프)를 HUD 상태 칸으로 바꾸는 줄 표입니다. 원본은 상태 외형 표(View DB)입니다(특성 발동 연출 설계 TA9).</summary>
        internal CombatUnitStatuses UnitStatuses { get; }

        /// <summary>View DB에서 전투 표시에 필요한 표를 모읍니다. 하나라도 없으면 DB 구성 오류로 던집니다.</summary>
        internal static CombatDisplayTables From(IViewQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            return new CombatDisplayTables(
                query.StageFieldViewTableQuery.Config
                    ?? throw new InvalidOperationException("[CombatDisplayTables] StageFieldView 설정이 없습니다."),
                query.RailwayVisualTableQuery.Config
                    ?? throw new InvalidOperationException("[CombatDisplayTables] RailwayVisual 설정이 없습니다."),
                query.ThornFieldVisualTableQuery.Config
                    ?? throw new InvalidOperationException("[CombatDisplayTables] ThornFieldVisual 설정이 없습니다."),
                query.RockfallVisualTableQuery.Config
                    ?? throw new InvalidOperationException("[CombatDisplayTables] RockfallVisual 설정이 없습니다."),
                query.HaboobVisualTableQuery.Config
                    ?? throw new InvalidOperationException("[CombatDisplayTables] HaboobVisual 설정이 없습니다."),
                query.UnitVisualTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] UnitVisual 표가 없습니다."),
                query.TraitVisualTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] TraitVisual 표가 없습니다."),
                query.StageViewPropTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] StageViewProp 표가 없습니다."),
                query.ObjectVisualTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] ObjectVisual 표가 없습니다."),
                query.TerrainVisualTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] TerrainVisual 표가 없습니다."),
                query.StageEnvironmentVisualTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] StageEnvironmentVisual 표가 없습니다."),
                query.CombatPresentationSettingsTableQuery?.Config
                    ?? throw new InvalidOperationException("[CombatDisplayTables] CombatPresentationSettings 설정이 없습니다."),
                query.ConsumableItemVisualTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] ConsumableItemVisual 표가 없습니다."),
                query.CategoryVisualTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] CategoryVisual 표가 없습니다."),
                query.UnitStatusVisualTableQuery
                    ?? throw new InvalidOperationException("[CombatDisplayTables] UnitStatusVisual 표가 없습니다."));
        }
    }
}
