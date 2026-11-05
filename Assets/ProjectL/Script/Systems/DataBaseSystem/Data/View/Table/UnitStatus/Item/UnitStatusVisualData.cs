using System;
using System.Collections.Generic;
using System.Globalization;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.UnitStatus.Item
{
    /// <summary>
    /// 걸린 상태 하나(버프·디버프)의 외형입니다(특성 발동 연출 설계 TA9). HUD의 표시 이름·아이콘·구분·설명 틀과,
    /// 그 상태가 켜져 있는 동안 몸에 붙는 유지 이펙트(이펙트 끝 규칙 설계 EL2)를 가집니다. DB를 만들 때 SO가 한 번 만들고 표가 들고 있는 읽기 전용 값입니다.
    /// </summary>
    /// <remarks>
    /// 설명 틀의 <c>{0}</c>·<c>{1}</c> 칸에는 그 상태의 Core 값(감소율·남은 턴 등)이 차례로 들어갑니다. 어느 칸에 어느 값이 들어가는지는
    /// 전투 HUD 상태 줄 표가 상태 종류마다 정합니다(값을 읽는 쪽이 원본). 이 값은 틀 모양만 가집니다.
    /// </remarks>
    internal sealed class UnitStatusVisualData : IData
    {
        internal UnitStatusVisualData(StatusEffectKey key, string displayName, Sprite icon,
            string descriptionTemplate, CombatHeldEffect whileEffect = null, IReadOnlyList<TraitActivationKind> restartOn = null)
        {
            Key = key;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? key.ToString() : displayName;
            Icon = icon;
            DescriptionTemplate = descriptionTemplate ?? string.Empty;
            WhileEffect = whileEffect;
            RestartOn = restartOn ?? Array.Empty<TraitActivationKind>();
        }

        public StatusEffectKey Key { get; }
        public string DisplayName { get; }
        /// <summary>비어 있으면 null이고, HUD는 기본 아이콘을 씁니다.</summary>
        public Sprite Icon { get; }
        public StatusEffectClassification Classification => StatusEffectCatalog.ClassificationOf(Key);
        /// <summary>설명 틀입니다. 비어 있으면 설명은 표시 이름 하나입니다.</summary>
        public string DescriptionTemplate { get; }

        /// <summary>
        /// 이 상태가 유닛에 보이는 동안(HUD 아이콘과 같은 규칙) 그 유닛 몸에 붙는 유지 이펙트입니다(이펙트 끝 규칙 설계 EL2). 없으면 null입니다.
        /// 같은 상태가 출처 여럿으로 걸려도 하나만 보입니다(HUD 아이콘 하나와 같음).
        /// </summary>
        internal CombatHeldEffect WhileEffect { get; }

        /// <summary>
        /// 이 발동 종류가 그 유닛에 오면 유지 이펙트를 거두고 다시 냅니다(설계 EL2, 결정 Q-EL3 A — 예: 보호막의 <c>ShieldReset</c>). 유지 이펙트가 없으면 비어 있습니다.
        /// </summary>
        internal IReadOnlyList<TraitActivationKind> RestartOn { get; }

        /// <summary>
        /// HUD 설명 문구입니다. 틀에 값을 차례로 넣고 "표시 이름: 설명"으로 만듭니다. 틀이 비면 표시 이름만입니다.
        /// 틀이 받을 수 없는 값 수면 <see cref="FormatException"/>입니다(미리 <see cref="AcceptsValueCount"/>로 확인).
        /// </summary>
        internal string Describe(object[] values)
        {
            if (DescriptionTemplate.Length == 0)
                return DisplayName;
            return $"{DisplayName}: {DescribeBody(values)}";
        }

        /// <summary>タイトルと別の欄に表示する、Core の値を埋めた説明本文です。</summary>
        internal string DescribeBody(object[] values) =>
            string.Format(CultureInfo.InvariantCulture, DescriptionTemplate, values ?? Array.Empty<object>());

        /// <summary>값 <paramref name="count"/>개로 설명을 만들 수 있는지입니다(칸 번호가 그 수 안이고 중괄호 짝이 맞는지).</summary>
        internal bool AcceptsValueCount(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            var values = new object[count];
            for (int i = 0; i < count; i++)
                values[i] = 0;
            try
            {
                Describe(values);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        internal void Validate()
        {
            if (Key == StatusEffectKey.None || !System.Enum.IsDefined(typeof(StatusEffectKey), Key))
                throw new InvalidOperationException($"[UnitStatusVisualData] 효과 키가 유효하지 않습니다: {Key}");
            _ = Classification;
            // 다시 내기는 유지 이펙트가 있을 때만 뜻이 있습니다. 종류는 정의된 값이고 겹치지 않아야 합니다.
            if (RestartOn.Count > 0 && WhileEffect == null)
                throw new InvalidOperationException($"[UnitStatusVisualData] 유지 이펙트가 없는데 다시 내기 발동을 적었습니다: {Key}");
            var kinds = new HashSet<TraitActivationKind>();
            foreach (TraitActivationKind kind in RestartOn)
            {
                if (kind == TraitActivationKind.None || !System.Enum.IsDefined(typeof(TraitActivationKind), kind))
                    throw new InvalidOperationException($"[UnitStatusVisualData] 다시 내기 발동 종류가 비었거나 정의되지 않았습니다: {Key}, {kind}");
                if (!kinds.Add(kind))
                    throw new InvalidOperationException($"[UnitStatusVisualData] 같은 다시 내기 발동 종류를 두 번 적었습니다: {Key}, {kind}");
            }
        }
    }
}
