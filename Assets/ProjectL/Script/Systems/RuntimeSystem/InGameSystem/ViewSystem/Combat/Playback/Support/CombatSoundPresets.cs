using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.ConsumableItem.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Object.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 전투 소리 프리셋과 공격 슬롯 번호의 답입니다. 값의 원본은 View DB입니다. 유닛·장애물 프리셋과 공격 슬롯은 유닛 외형 표,
    /// 오브젝트·아이템 프리셋은 각 외형 표, 적 공용·배경음·추락·약한 지반 붕괴 프리셋은 전투 연출 설정 표에 있습니다(소리 창구 2차 설계 ES1).
    /// 어느 사건에 어느 칸을 내는지는 소리 표(<see cref="CombatEventSounds"/>)가 정하고, 이 객체는 그 칸에 넣을 번호만 답합니다.
    /// </summary>
    /// <remarks>
    /// 표시 표(<see cref="CombatDisplayTables"/>)가 한 번 만들고, 입력(배치 Drag·공격 조준)·표시의 배경음 정책(<see cref="CombatBgmPolicy"/>)·사건 소리가 같은 객체를 씁니다(계획 TP-26).
    /// 외형 표에 없는 유닛·오브젝트·아이템은 소리 없음(<see cref="None"/>)입니다.
    /// 토네이도 휩쓸림·착지 프리셋은 여기서 답하지 않습니다. 들어 올림 조립이 이미 가진 토네이도 연출 설정에서 바로 읽습니다(소리 창구 2차 계획 P2).
    /// </remarks>
    internal sealed class CombatSoundPresets
    {
        /// <summary>소리 없음입니다. 이 번호를 받은 쪽은 소리를 내지 않습니다. 값의 원본은 DB 수 규칙입니다.</summary>
        internal const int None = CombatPresentationValueRules.NoSoundPreset;

        /// <summary>
        /// 소리를 내는 번호인지입니다(0 이상). 사건 소리 단서와 입력 소리가 이 판정 하나를 씁니다.
        /// 판정의 원본은 DB 수 규칙 <see cref="CombatPresentationValueRules.IsSoundPreset"/>입니다(투사체·피격 소리 묶음 끝 검토 중간 2).
        /// </summary>
        internal static bool IsSound(int preset) => CombatPresentationValueRules.IsSoundPreset(preset);

        private readonly IUnitVisualTableQuery _visuals;
        private readonly IObjectVisualTableQuery _objects;
        private readonly IConsumableItemVisualTableQuery _items;
        private readonly CombatPresentationSettingsData _settings;

        internal CombatSoundPresets(IUnitVisualTableQuery visuals, IObjectVisualTableQuery objects,
            IConsumableItemVisualTableQuery items, CombatPresentationSettingsData settings)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _objects = objects ?? throw new ArgumentNullException(nameof(objects));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>유닛 하나의 선택·이동·취소·사망 소리 프리셋입니다. 적은 외형과 상관없이 공용 프리셋 하나입니다(옛 규칙 그대로).</summary>
        internal int Unit(EntityKey key, Owner owner)
        {
            if (owner == Owner.Enemy)
                return _settings.EnemyUnitSoundPreset;
            return _visuals.TryGetUnitVisual(key, out UnitVisualData visual) ? visual.UnitSoundPreset : None;
        }

        /// <summary>UnitAttack 플레이어의 공격 슬롯 번호입니다. 포효도 포효한 유닛의 이 값입니다.</summary>
        internal int AttackSlot(EntityKey key) =>
            _visuals.TryGetUnitVisual(key, out UnitVisualData visual) ? visual.AttackSoundSlotIndex : None;

        /// <summary>
        /// 장애물 종류 하나의 피해·파괴 소리 프리셋입니다(소리 창구 2차 설계 Q-ES3). 장애물 외형은 유닛 외형 표에 있어 그 유닛 소리 번호를 다시 씁니다.
        /// 장애물은 적 유닛이 아니므로 적 공용 프리셋 규칙은 쓰지 않습니다(계획 P6).
        /// </summary>
        internal int Obstacle(ObstacleContentKind kind) =>
            _visuals.TryGetUnitVisual(ObstacleContentProfile.KeyFor(kind), out UnitVisualData visual) ? visual.UnitSoundPreset : None;

        /// <summary>오브젝트 종류 하나의 발동·습득·생성 소리 프리셋입니다(소리 창구 2차 설계 Q-ES2″). 유닛처럼 슬롯이 달라도 이 번호 하나입니다.</summary>
        internal int Object(ObjectContentKind kind) =>
            _objects.TryGetObjectVisual(kind, out ObjectVisualData visual) ? visual.ObjectSoundPreset : None;

        /// <summary>아이템 하나의 발동(소모) 소리 프리셋입니다(소리 창구 2차 설계 ES1).</summary>
        internal int Item(int itemKey) =>
            _items.TryGet(itemKey, out ConsumableItemVisualData visual) ? visual.ItemSoundPreset : None;

        /// <summary>약한 지반 붕괴 소리 프리셋입니다(소리 창구 2차 설계 Q-ES5′).</summary>
        internal int WeakGroundCollapse() => _settings.WeakGroundCollapseSoundPreset;

        /// <summary>국면의 배경음 프리셋입니다(구조 원칙 평가 R2 (a)). 사건 소리와 표시의 배경음 정책(<see cref="CombatBgmPolicy"/>, 국면 배경음·완료 뒤 배경음)이 부릅니다.</summary>
        internal int Bgm(CombatBgmPhase phase) => phase switch
        {
            CombatBgmPhase.Night => _settings.NightBgmPreset,
            CombatBgmPhase.Day => _settings.DayBgmPreset,
            CombatBgmPhase.AfterCompletion => _settings.AfterCompletionBgmPreset,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "모르는 배경음 국면입니다."),
        };
    }
}
