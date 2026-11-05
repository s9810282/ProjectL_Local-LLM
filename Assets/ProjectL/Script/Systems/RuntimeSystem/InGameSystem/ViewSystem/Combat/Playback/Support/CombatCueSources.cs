using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.ConsumableItem.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Object.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 사건 순간의 연출 큐 값의 답입니다(이펙트 창구 확장 설계 EC5·EC7, 계획 P9). 값의 원본은 View DB입니다. 오브젝트·장애물·아이템 큐는 각 외형 표,
    /// 추락·약한 지반 붕괴 큐는 전투 연출 설정 표에 있습니다. 어느 사건이 어느 순간인지는 사건 형식 표의 순간 칸이 정하고, 이 객체는 그 순간의 큐만 답합니다.
    /// 특성 발동 큐는 특성 외형 표의 발동 연출 줄에 있고(특성 발동 연출 설계 TA2), (특성 키, 발동 종류)로 그 줄을 답합니다(<see cref = "TryGetActivation"/>).
    /// </summary>
    /// <remarks>
    /// 표시 표(<see cref = "CombatDisplayTables"/>)가 한 번 만듭니다. 값 쪽 보관 방식은 표마다 다릅니다 — 오브젝트 외형은 DB 생성 때 검사한 묶음을 들고 있고, 유닛·아이템 외형과 설정 표는 읽을 때마다 직렬화 칸에서 새로 만듭니다(묶음 끝 검토 L5). 그래서 보관은 쓰는 쪽(순간 큐 내는 곳)이 연결마다 한 번 합니다(계획 P6).
    /// 소리 프리셋 답(<see cref = "CombatSoundPresets"/>)과 같은 표를 읽지만 합치지 않습니다. 소리 번호와 이펙트 큐는 다른 값이라 바뀌는 까닭이 다릅니다(설계 §7.4).
    /// 외형 표에 없는 오브젝트·장애물·아이템과 큐 칸이 없는 순간(오브젝트 생성, 결정 Q-EC5 A)은 빈 묶음입니다.
    /// </remarks>
    internal sealed class CombatCueSources
    {
        private static readonly CombatEventCues NoCues = new(null, null, null, null);
        private readonly IUnitVisualTableQuery _units;
        private readonly IObjectVisualTableQuery _objects;
        private readonly IConsumableItemVisualTableQuery _items;
        private readonly ITraitVisualTableQuery _traits;
        private readonly CombatPresentationSettingsData _settings;
        /// <param name="statuses">상태 외형 표입니다. 상태 유지 이펙트의 출처가 읽습니다(이펙트 끝 규칙 계획 TP-E15).</param>
        internal CombatCueSources(IUnitVisualTableQuery units, IObjectVisualTableQuery objects, IConsumableItemVisualTableQuery items, ITraitVisualTableQuery traits, CombatPresentationSettingsData settings,
            IUnitStatusVisualTableQuery statuses)
        {
            UnitStatuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
            _units = units ?? throw new ArgumentNullException(nameof(units));
            _objects = objects ?? throw new ArgumentNullException(nameof(objects));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _traits = traits ?? throw new ArgumentNullException(nameof(traits));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>그 순간의 큐 묶음입니다. 큐가 없으면 빈 묶음(<see cref = "CombatEventCues.IsEmpty"/>)입니다.</summary>
        internal CombatEventCues For(in CombatEventMoment moment) => moment.Kind switch
        {
            CombatMomentKind.ObjectActivated => _objects.TryGetObjectVisual(moment.ObjectKind, out ObjectVisualData objectVisual) ? objectVisual.ActivationCues : NoCues,
            CombatMomentKind.ObjectCreated => NoCues,
            CombatMomentKind.ObstacleHit => TryGetObstacle(moment.ObstacleKind, out UnitVisualData hitObstacle) ? hitObstacle.ObstacleHitCues : NoCues,
            CombatMomentKind.ObstacleDestroyed => TryGetObstacle(moment.ObstacleKind, out UnitVisualData destroyedObstacle) ? destroyedObstacle.ObstacleDestroyedCues : NoCues,
            CombatMomentKind.Fall => _settings.FallCues,
            CombatMomentKind.WeakGroundCollapse => _settings.WeakGroundCollapseCues,
            CombatMomentKind.ItemActivated => _items.TryGet(moment.ItemKey, out ConsumableItemVisualData itemVisual) ? itemVisual.ActivationCues : NoCues,
            _ => throw new ArgumentOutOfRangeException(nameof(moment), moment.Kind, "모르는 사건 순간 종류입니다."),
        };
        internal CombatEventCues StructureHit(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityKey key) =>
            _units.GetUnitVisual(key).StructureHitCues;
        /// <summary>
        /// 특성 발동 하나의 연출 줄입니다(큐와 추가 공격 줄의 공격 표시 덮어쓰기, 설계 TA2). 특성 외형이 없거나 그 발동 종류의 줄이 없으면 false입니다.
        /// 줄의 값(큐·공격 표시)은 읽을 때마다 새로 만들므로 보관은 쓰는 쪽이 합니다(계획 P6과 같은 규칙).
        /// </summary>
        internal bool TryGetActivation(TraitKey trait, TraitActivationKind kind, out TraitActivationVisual activation) =>
            TryGetActivation(trait, kind, CombatActivationDirection.Any, out activation);

        /// <summary>(특성, 발동 종류, 방향)과 꼭 맞는 연출 줄입니다(이펙트 끝 규칙 설계 EL7). 방향 줄이 없을 때 Any로 가는 것은 발동 줄 보관이 정합니다.</summary>
        internal bool TryGetActivation(TraitKey trait, TraitActivationKind kind, CombatActivationDirection direction, out TraitActivationVisual activation)
        {
            activation = null;
            return _traits.TryGetTraitVisual(trait, out TraitVisualData visual) && visual != null && visual.TryGetActivation(kind, direction, out activation);
        }

        /// <summary>상태 외형 표입니다. 상태 유지 이펙트(상태 외형 칸 <c>whileEffect</c>)의 원본입니다(이펙트 끝 규칙 설계 EL2).</summary>
        internal IUnitStatusVisualTableQuery UnitStatuses { get; }

        /// <summary>특성 외형 표입니다. 특성 상시 이펙트(특성 외형 칸 <c>alwaysEffects</c>)의 출처가 읽습니다(이펙트 끝 규칙 설계 EL3).</summary>
        internal ITraitVisualTableQuery Traits => _traits;

        internal string TraitName(TraitKey trait) => _traits.TryGetTraitVisual(trait, out TraitVisualData visual) ? visual.DisplayName : trait.ToString();
        /// <summary>활성 줄에서 실제로 선택한 기본/Override 프리팹을 중복 없이 반환합니다.</summary>
        internal System.Collections.Generic.IReadOnlyList<UnityEngine.GameObject> BodyTextPrefabs(UnityEngine.GameObject defaultPrefab)
        {
            var selected = new System.Collections.Generic.List<UnityEngine.GameObject>();
            // 방향 줄(결속 오를 때·내릴 때)도 모두 훑습니다(이펙트 끝 규칙 설계 EL7).
            foreach (TraitVisualData visual in _traits.GetAllTraitVisuals())
                foreach (TraitActivationVisual row in visual.Activations)
                    if (row.BodyText is CombatBodyTextData text)
                    {
                        UnityEngine.GameObject prefab = text.PrefabOverride != null ? text.PrefabOverride : defaultPrefab;
                        if (prefab == null)
                            throw new InvalidOperationException($"[CombatBodyText] {visual.Key}/{row.Kind}/{row.Direction}: Prefab Override 또는 공통 Body Text Prefab 참조가 필요합니다.");
                        if (!selected.Contains(prefab))
                            selected.Add(prefab);
                    }

            return selected.AsReadOnly();
        }

        // 장애물 외형은 유닛 외형 표에 있습니다(소리 창구 2차 결정 Q-ES3과 같은 자리). 키는 Core 규칙 하나로 구합니다.
        private bool TryGetObstacle(ObstacleContentKind kind, out UnitVisualData visual) => _units.TryGetUnitVisual(ObstacleContentProfile.KeyFor(kind), out visual);
    }
}
