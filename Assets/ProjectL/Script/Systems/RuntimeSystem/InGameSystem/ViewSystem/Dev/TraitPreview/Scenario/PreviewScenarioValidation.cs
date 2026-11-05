#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    /// <summary>씬 상태를 바꾸기 전에 입력의 식별과 묶음 모양을 검사합니다.</summary>
    internal static class PreviewScenarioValidation
    {
        internal static void Require(TraitPreviewScenarioInput input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (input.version != 3)
                throw new ArgumentException($"지원하지 않는 조립 버전: {input.version}");
            Seconds(input.observationSeconds);
            Seconds(input.repeatInterval);
            if (input.units == null || input.units.Count == 0)
                throw new ArgumentException("초기 유닛이 없습니다.");
            var ids = new HashSet<string>();
            foreach (var unit in input.units)
            {
                if (unit == null || unit.unit == null)
                    throw new ArgumentException("초기 유닛 입력이 비었습니다.");
                Id(ids, unit.id, "유닛");
            }

            if (input.entries == null || input.entries.Count == 0)
                throw new ArgumentException("시연 순서가 비었습니다.");
            foreach (var entry in input.entries)
            {
                if (entry == null)
                    throw new ArgumentException("순서 항목이 비었습니다.");
                try
                {
                    Id(ids, entry.id, "묶음");
                    Defined(entry.kind);
                    Seconds(entry.seconds);
                    if (entry.blocks == null)
                        throw new ArgumentException("블록 목록이 없습니다.");
                    if (entry.kind != PreviewEntryKind.Packet)
                    {
                        if (entry.blocks.Count != 0)
                            throw new ArgumentException("대기는 묶음 사이에만 두며 블록을 포함하지 않습니다.");
                        continue;
                    }

                    if (entry.blocks.Count == 0)
                        throw new ArgumentException("묶음에 블록이 없습니다.");
                    int actions = 0;
                    for (int i = 0; i < entry.blocks.Count; i++)
                    {
                        var block = entry.blocks[i] ?? throw new ArgumentException($"블록 {i}이 비었습니다.");
                        Id(ids, block.id, "블록");
                        Defined(block.kind);
                        Defined(block.relation);
                        bool action = block.kind == PreviewBlockKind.Movement || block.kind == PreviewBlockKind.Attack || block.kind == PreviewBlockKind.ChildAttack;
                        if (action && (++actions > 1 || i != 0))
                            throw new ArgumentException("이동/공격은 묶음 맨 앞에 하나만 둡니다.");
                    }

                    bool attack = entry.blocks[0].kind == PreviewBlockKind.Attack || entry.blocks[0].kind == PreviewBlockKind.ChildAttack;
                    foreach (var block in entry.blocks)
                        if (!attack && (block.kind == PreviewBlockKind.Interception || block.relation == PreviewEventRelation.AttackHit))
                            throw new ArgumentException($"블록 '{block.name}'의 타격 후속/가로채기에 같은 묶음의 공격이 필요합니다.");
                }
                catch (Exception error)when (error is ArgumentException || error is InvalidOperationException)
                {
                    throw new ArgumentException($"묶음 '{entry.name}' ({entry.id}): {error.Message}", error);
                }
            }
        }

        internal static void Seconds(float value)
        {
            if (!float.IsFinite(value) || value < 0)
                throw new ArgumentException("시간은 유한한 0 이상 값이어야 합니다.");
        }

        internal static void Defined<T>(T value)
            where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new ArgumentException($"정의되지 않은 {typeof(T).Name}: {value}");
        }

        private static void Id(HashSet<string> ids, string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value) || !ids.Add(value))
                throw new ArgumentException($"{label} ID가 없거나 중복됩니다: {value}");
        }
    }
}
#endif
