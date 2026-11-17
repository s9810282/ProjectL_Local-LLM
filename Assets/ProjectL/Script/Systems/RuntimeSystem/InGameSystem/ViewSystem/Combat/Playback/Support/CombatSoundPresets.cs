using System;
using System.Collections.Generic;
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
    /// 전투 소리 프리셋과 유닛 소리 줄의 답입니다. 값의 원본은 View DB입니다. 유닛·장애물 프리셋과 공격·피격·사망 소리 줄은 유닛 외형 표,
    /// 오브젝트·아이템 프리셋은 각 외형 표, 적 공용·배경음·추락·약한 지반 진입·붕괴 프리셋은 전투 연출 설정 표에 있습니다(소리 창구 2차 설계 ES1, 소리 프리셋 원본 설계 SP1~SP3).
    /// 어느 사건에 어느 칸을 내는지는 소리 표(<see cref="CombatEventSounds"/>)가 정하고, 이 객체는 그 칸에 넣을 번호만 답합니다.
    /// </summary>
    /// <remarks>
    /// 표시 표(<see cref="CombatDisplayTables"/>)가 한 번 만들고, 입력(배치 Drag·공격 조준)·표시의 배경음 정책(<see cref="CombatBgmPolicy"/>)·사건 소리가 같은 객체를 씁니다(계획 TP-26).
    /// 외형 표에 없는 유닛·오브젝트·아이템은 소리 없음(<see cref="None"/>)입니다.
    /// 토네이도 휩쓸림·착지 프리셋은 여기서 답하지 않습니다. 들어 올림 조립이 이미 가진 토네이도 연출 설정에서 바로 읽습니다(소리 창구 2차 계획 P2).
    /// </remarks>
    internal sealed class CombatSoundPresets
    {
        /// <summary>
        /// 프리셋 없음입니다. 받은 쪽이 <c>CombatSoundCue.Effect</c>로 내면 소리 없음,
        /// <see cref="CombatSoundCue.EffectKeepingPreset"/>으로 내면 프리셋 없이 냅니다(장애물·추락, 소리 프리셋 원본 설계 SP3). 값의 원본은 DB 수 규칙입니다.
        /// </summary>
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
        private readonly int[] _weakGroundEntry;
        private readonly Dictionary<EntityKey, CombatUnitSoundsData> _unitSounds = new();

        internal CombatSoundPresets(IUnitVisualTableQuery visuals, IObjectVisualTableQuery objects,
            IConsumableItemVisualTableQuery items, CombatPresentationSettingsData settings)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _objects = objects ?? throw new ArgumentNullException(nameof(objects));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            // 진입 사건마다 읽지 않게 이 객체를 만들 때 한 번 복사해 둡니다(소리 프리셋 원본 계획 TP-2). 이 객체는 View 서비스가 시작할 때(재생 시작 때) 한 번 만들어져,
            // 재생 중에 에셋의 진입 목록을 바꾸면 다음 재생부터 반영됩니다. 붕괴·추락 칸은 사건마다 읽습니다.
            _weakGroundEntry = settings.CopyWeakGroundEntrySoundPresets();
        }

        /// <summary>
        /// 유닛 하나의 선택·이동·취소 소리 프리셋입니다. 적은 외형과 상관없이 공용 프리셋 하나입니다(옛 규칙 그대로).
        /// 공격·피격·사망 소리는 외형의 소리 줄입니다(<see cref="UnitSounds"/>, 유닛 소리 설계 US3).
        /// </summary>
        internal int Unit(EntityKey key, Owner owner)
        {
            if (owner == Owner.Enemy)
                return _settings.EnemyUnitSoundPreset;
            return _visuals.TryGetUnitVisual(key, out UnitVisualData visual) ? visual.UnitSoundPreset : None;
        }

        /// <summary>
        /// 유닛 하나의 공격·피격·사망 소리 줄입니다(유닛 소리 설계 US1, 계획 TP-7). 외형 값은 읽을 때마다 새로 만들어져 사건마다 배열이 생기므로 키마다 처음 읽을 때 보관합니다.
        /// 보관은 이 객체 수명(View 서비스가 시작할 때 한 번)이라, 재생 중에 외형의 소리 줄을 바꾸면 다음 재생부터 반영됩니다(약한 지반 진입 목록과 같음).
        /// 외형 표에 없는 유닛은 줄이 없습니다.
        /// </summary>
        internal CombatUnitSoundsData UnitSounds(EntityKey key)
        {
            if (_unitSounds.TryGetValue(key, out CombatUnitSoundsData sounds))
                return sounds;
            sounds = _visuals.TryGetUnitVisual(key, out UnitVisualData visual) ? visual.Sounds : CombatUnitSoundsData.Empty;
            _unitSounds.Add(key, sounds);
            return sounds;
        }

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

        /// <summary>
        /// 남은 진입 횟수 하나의 약한 지반 진입 소리 프리셋입니다. 첫 원소가 남은 1이고, 남은 횟수가 목록보다 많으면 마지막 원소,
        /// 0 이하·빈 목록은 소리 없음(<see cref="None"/>)입니다(소리 프리셋 원본 설계 SP2). 이 규칙은 여기 한 곳에 있습니다.
        /// </summary>
        internal int WeakGroundEntry(int remaining)
        {
            if (remaining <= 0 || _weakGroundEntry.Length == 0)
                return None;
            return _weakGroundEntry[Math.Min(remaining, _weakGroundEntry.Length) - 1];
        }

        /// <summary>낭떠러지 추락 소리 프리셋입니다. -1이면 프리셋 없이 냅니다(받는 쪽이 <see cref="CombatSoundCue.EffectKeepingPreset"/>으로 냄, 소리 프리셋 원본 설계 SP3).</summary>
        internal int Fall() => _settings.FallSoundPreset;

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
