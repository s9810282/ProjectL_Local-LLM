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
        internal UnitCardTraitContent(TraitKey key, TraitOrigin origin, Sprite icon, string name, string description)
        {
            Key = key;
            Origin = origin;
            Icon = icon;
            Name = name;
            Description = description;
        }

        internal TraitKey Key { get; }
        internal TraitOrigin Origin { get; }
        internal Sprite Icon { get; }
        internal string Name { get; }
        internal string Description { get; }

        internal static UnitCardTraitContent[] Create(ITraitVisualTableQuery visuals, ReadOnlySpan<TraitStatusSnapshot> traits,
            TraitSettingsSet settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            var display = settings.Display;
            if (traits.Length != display.Count)
                throw new InvalidOperationException("[UnitCardTraitContent] 전투 특성 상태와 표시 목록의 개수가 다릅니다.");
            if (traits.Length == 0) return Array.Empty<UnitCardTraitContent>();
            var statusByKey = new Dictionary<TraitKey, TraitStatusSnapshot>(traits.Length);
            foreach (TraitStatusSnapshot status in traits)
            {
                if (status == null || !statusByKey.TryAdd(status.Key, status))
                    throw new InvalidOperationException("[UnitCardTraitContent] 전투 특성 상태에 빈 항목 또는 중복 키가 있습니다.");
            }
            var content = new UnitCardTraitContent[display.Count];
            for (int i = 0; i < content.Length; i++)
            {
                TraitDisplayEntry entry = display[i];
                if (!statusByKey.TryGetValue(entry.Key, out var status))
                    throw new InvalidOperationException($"[UnitCardTraitContent] 표시 특성 {entry.Key}의 전투 상태가 없습니다.");
                content[i] = Read(visuals, entry.Key, entry.Origin, TraitDescriptionFormatter.ReadSettings(settings, entry.Key), status);
            }
            return content;
        }

        internal static UnitCardTraitContent[] Create(ITraitVisualTableQuery visuals, OwnedCharacterStatsSnapshot source) =>
            Create(visuals, source != null ? source.Stats.TraitKeys.Span : ReadOnlySpan<TraitKey>.Empty, source);

        internal static UnitCardTraitContent[] Create(ITraitVisualTableQuery visuals, ReadOnlySpan<TraitKey> traits,
            OwnedCharacterStatsSnapshot source = null)
        {
            if (traits.Length == 0) return Array.Empty<UnitCardTraitContent>();
            // 보유 스냅샷 없이 기본 키만 넘기는 호출에는 분류된 획득 이력이 없습니다.
            if (source == null)
            {
                var unitTraits = new UnitCardTraitContent[traits.Length];
                for (int i = 0; i < traits.Length; i++)
                    unitTraits[i] = Read(visuals, traits[i], TraitOrigin.Unit, null);
                return unitTraits;
            }
            var settings = source.TraitSettings;
            var display = settings.Display;
            if (display.Count != traits.Length)
                throw new InvalidOperationException("[UnitCardTraitContent] 보유 특성 키와 표시 목록의 개수가 다릅니다.");
            var traitAvailability = source.TraitAvailability;
            if (traitAvailability.Count != 0 && traitAvailability.Count != traits.Length)
                throw new InvalidOperationException("[UnitCardTraitContent] 같은 읽기의 특성 가용성 개수가 맞지 않습니다.");
            var indexByKey = new Dictionary<TraitKey, int>(traits.Length);
            for (int i = 0; i < traits.Length; i++)
            {
                if (!indexByKey.TryAdd(traits[i], i))
                    throw new InvalidOperationException($"[UnitCardTraitContent] 보유 특성 키 {traits[i]}가 중복됩니다.");
            }
            var content = new UnitCardTraitContent[display.Count];
            for (int i = 0; i < content.Length; i++)
            {
                TraitDisplayEntry entry = display[i];
                if (!indexByKey.TryGetValue(entry.Key, out int index))
                    throw new InvalidOperationException($"[UnitCardTraitContent] 표시 특성 {entry.Key}가 보유 특성 키에 없습니다.");
                var ownedAvailability = traitAvailability.Count > 0 ? traitAvailability[index] : null;
                if (ownedAvailability != null && ownedAvailability.Key != entry.Key)
                    throw new InvalidOperationException("[UnitCardTraitContent] 같은 읽기의 특성 가용성 순서가 다릅니다.");
                content[i] = Read(visuals, entry.Key, entry.Origin, TraitDescriptionFormatter.ReadSettings(settings, entry.Key),
                    ownedAvailability: ownedAvailability);
            }
            return content;
        }

        private static UnitCardTraitContent Read(ITraitVisualTableQuery visuals, TraitKey key, TraitOrigin origin, TraitSpec descriptionData,
            TraitStatusSnapshot availability = null, TraitAvailabilitySnapshot ownedAvailability = null)
        {
            string unavailable = availability?.UnavailableReason ?? ownedAvailability?.UnavailableReason ?? string.Empty;
            string partial = availability?.PartialUnavailableReason ?? ownedAvailability?.PartialUnavailableReason ?? string.Empty;
            if (unavailable == "Retired")
                return new UnitCardTraitContent(key, origin, null, "사용할 수 없는 특성", DescribeAvailability(unavailable));
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
            return new UnitCardTraitContent(key, origin, visual.Icon, name, description);
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
