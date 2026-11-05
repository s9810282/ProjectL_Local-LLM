using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Descriptions.Traits;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>덱 카드와 전투 정보창이 공유하는 특성 표시 변환입니다. 선택·패널·DB 수명은 소유하지 않습니다.</summary>
    internal readonly struct UnitCardTraitContent
    {
        internal UnitCardTraitContent(TraitKey key, Sprite icon, string name, string description)
        {
            Key = key;
            Icon = icon;
            Name = name;
            Description = description;
        }

        internal TraitKey Key { get; }
        internal Sprite Icon { get; }
        internal string Name { get; }
        internal string Description { get; }

        internal static UnitCardTraitContent[] Create(ITraitVisualTableQuery visuals, ReadOnlySpan<TraitStatusSnapshot> traits,
            TraitSettingsSet settings)
        {
            if (traits.Length == 0) return Array.Empty<UnitCardTraitContent>();
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var content = new UnitCardTraitContent[traits.Length];
            for (int i = 0; i < traits.Length; i++)
            {
                TraitStatusSnapshot status = traits[i] ??
                    throw new InvalidOperationException($"[UnitCardTraitContent] 특성 상태 {i}번이 비어 있습니다.");
                content[i] = Read(visuals, status.Key, TraitDescriptionFormatter.ReadSettings(settings, status.Key), status);
            }
            return content;
        }

        internal static UnitCardTraitContent[] Create(ITraitVisualTableQuery visuals, OwnedCharacterStatsSnapshot source) =>
            Create(visuals, source != null ? source.Stats.TraitKeys.Span : ReadOnlySpan<TraitKey>.Empty, source);

        internal static UnitCardTraitContent[] Create(ITraitVisualTableQuery visuals, ReadOnlySpan<TraitKey> traits,
            OwnedCharacterStatsSnapshot source = null)
        {
            if (traits.Length == 0) return Array.Empty<UnitCardTraitContent>();
            IReadOnlyList<TraitAvailabilitySnapshot> traitAvailability = source?.TraitAvailability;
            if (traitAvailability != null && traitAvailability.Count != 0 && traitAvailability.Count != traits.Length)
                throw new InvalidOperationException("[UnitCardTraitContent] 같은 읽기의 특성 가용성 개수가 맞지 않습니다.");
            var content = new UnitCardTraitContent[traits.Length];
            for (int i = 0; i < traits.Length; i++)
            {
                var ownedAvailability = traitAvailability != null && traitAvailability.Count > 0 ? traitAvailability[i] : null;
                if (ownedAvailability != null && ownedAvailability.Key != traits[i])
                    throw new InvalidOperationException("[UnitCardTraitContent] 같은 읽기의 특성 가용성 순서가 다릅니다.");
                content[i] = Read(visuals, traits[i], TraitDescriptionFormatter.ReadSettings(source?.TraitSettings, traits[i]),
                    ownedAvailability: ownedAvailability);
            }
            return content;
        }

        private static UnitCardTraitContent Read(ITraitVisualTableQuery visuals, TraitKey key, TraitSpec descriptionData,
            TraitStatusSnapshot availability = null, TraitAvailabilitySnapshot ownedAvailability = null)
        {
            string unavailable = availability?.UnavailableReason ?? ownedAvailability?.UnavailableReason ?? string.Empty;
            string partial = availability?.PartialUnavailableReason ?? ownedAvailability?.PartialUnavailableReason ?? string.Empty;
            if (unavailable == "Retired")
                return new UnitCardTraitContent(key, null, "사용할 수 없는 특성", DescribeAvailability(unavailable));
            if (visuals == null || !visuals.TryGetTraitVisual(key, out var visual))
            {
                throw new InvalidOperationException($"[UnitCardTraitContent] TraitKey.{key}의 View 특성 표시 데이터가 필요합니다.");
            }
            string name = visual.DisplayName;
            string description = TraitDescriptionFormatter.Format(visual, descriptionData);
            if (unavailable.Length > 0)
            {
                name += " (현재 사용 불가)";
                description += "\n\n현재 사용 불가: " + DescribeAvailability(unavailable);
            }
            if (partial.Length > 0)
            {
                if (unavailable.Length == 0) name += " (일부 사용 불가)";
                description += "\n\n일부 사용 불가: " + DescribeAvailability(partial);
            }
            return new UnitCardTraitContent(key, visual.Icon, name, description);
        }

        private static string DescribeAvailability(string reason)
        {
            switch (reason)
            {
                case "DeferredUnitTurnTiming": return "유닛 턴에 의존하는 효과는 현재 진영 턴 규칙에서 지원하지 않습니다.";
                case "Retired": return "현재 규칙에서 제외된 특성입니다.";
                default: return "현재 전투 규칙에서 지원하지 않는 기능입니다.";
            }
        }
    }
}
