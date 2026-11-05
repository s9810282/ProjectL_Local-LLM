using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>조립 목록과 선택 입력을 편집합니다. 사건 생성/재생은 담당하지 않습니다.</summary>
    internal sealed class PreviewScenarioEditor
    {
        private int _unit;
        private int _entry;
        private int _block;
        private PreviewBlockKind _addKind;
        internal void Draw(SerializedObject owner, TraitPreviewScenarioInput input)
        {
            if (input.version < 3)
            {
                EditorGUILayout.HelpBox("기존 조립 입력을 v3 상태 입력으로 이전한 뒤 편집하세요. 원본 프리셋은 변경하지 않습니다.", MessageType.Info);
                if (GUILayout.Button("v3 상태 입력으로 이전"))
                    Change(owner, () => JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(TraitPreviewScenarioImport.Upgrade(input)), input));
                return;
            }
            var data = owner.FindProperty("scenario");
            PreviewScenarioFields.Field(data, "stageViewKey", "Stage View Key");
            PreviewScenarioFields.Field(data, "repeat", "전체 반복");
            PreviewScenarioFields.Field(data, "pauseBetweenPackets", "묶음마다 멈춤 (시작 설정)");
            PreviewScenarioFields.Field(data, "observationSeconds", "전체 완료 뒤 관찰 시간");
            PreviewScenarioFields.Field(data, "repeatInterval", "전체 반복 간격");
            DrawUnits(owner, input, data.FindPropertyRelative("units"));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("시연 순서 — 공격·피격은 한 묶음", EditorStyles.boldLabel);
            for (int i = 0; i < input.entries.Count; i++)
            {
                int index = i;
                var entry = input.entries[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(_entry == i, $"{i + 1}. {entry.name} [{entry.kind}]", "Button") && _entry != i)
                    {
                        _entry = i;
                        _block = 0;
                    }

                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUILayout.Button("↑", GUILayout.Width(25)))
                            Change(owner, () =>
                            {
                                (input.entries[index - 1], input.entries[index]) = (input.entries[index], input.entries[index - 1]);
                                _entry = index - 1;
                            });
                    using (new EditorGUI.DisabledScope(i == input.entries.Count - 1))
                        if (GUILayout.Button("↓", GUILayout.Width(25)))
                            Change(owner, () =>
                            {
                                (input.entries[index + 1], input.entries[index]) = (input.entries[index], input.entries[index + 1]);
                                _entry = index + 1;
                            });
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                AddEntry(owner, input, PreviewEntryKind.Packet, "+ 묶음");
                AddEntry(owner, input, PreviewEntryKind.TimeWait, "+ 시간 대기");
                AddEntry(owner, input, PreviewEntryKind.ManualWait, "+ 수동 대기");
            }

            if (input.entries.Count == 0)
                return;
            _entry = Mathf.Clamp(_entry, 0, input.entries.Count - 1);
            var current = input.entries[_entry];
            var property = data.FindPropertyRelative("entries").GetArrayElementAtIndex(_entry);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PreviewScenarioFields.Field(property, "name", "묶음/대기 이름");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("이 항목 복제"))
                        Change(owner, () =>
                        {
                            var copy = Copy(current);
                            copy.id = NewId();
                            foreach (var block in copy.blocks)
                                block.id = NewId();
                            input.entries.Insert(_entry + 1, copy);
                            ++_entry;
                        });
                    if (GUILayout.Button("이 항목 제거"))
                        Change(owner, () => input.entries.RemoveAt(_entry));
                }

                if (current.kind == PreviewEntryKind.TimeWait)
                    PreviewScenarioFields.Field(property, "seconds", "대기 시간");
                else if (current.kind == PreviewEntryKind.ManualWait)
                    EditorGUILayout.LabelField("이 경계에서 다음 진행 버튼을 기다립니다.");
                else
                    DrawBlocks(owner, input, current, property.FindPropertyRelative("blocks"));
            }
        }

        private void DrawUnits(SerializedObject owner, TraitPreviewScenarioInput input, SerializedProperty units)
        {
            EditorGUILayout.LabelField("초기 유닛", EditorStyles.boldLabel);
            if (input.units.Count > 0)
            {
                _unit = Mathf.Clamp(_unit, 0, input.units.Count - 1);
                _unit = EditorGUILayout.Popup("편집 유닛", _unit, input.units.Select(u => u.name).ToArray());
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_unit == 0))
                        if (GUILayout.Button("유닛 순서 ↑"))
                            Change(owner, () =>
                            {
                                (input.units[_unit - 1], input.units[_unit]) = (input.units[_unit], input.units[_unit - 1]);
                                --_unit;
                            });
                    using (new EditorGUI.DisabledScope(_unit == input.units.Count - 1))
                        if (GUILayout.Button("유닛 순서 ↓"))
                            Change(owner, () =>
                            {
                                (input.units[_unit + 1], input.units[_unit]) = (input.units[_unit], input.units[_unit + 1]);
                                ++_unit;
                            });
                }

                var data = units.GetArrayElementAtIndex(_unit);
                PreviewScenarioFields.Field(data, "name", "편집 이름");
                var unit = data.FindPropertyRelative("unit");
                foreach (var field in new[]
                {
                    "key",
                    "enemy",
                    "cell",
                    "direction",
                    "health",
                    "maxHealth",
                    "attack",
                    "defense"
                }

                )
                    PreviewScenarioFields.Field(unit, field);
                foreach (string field in new[] { "actionUse", "distanceMoved", "ownsPositionSwap", "ownsReconSquad" })
                    PreviewScenarioFields.Field(data, field);
                if (data.FindPropertyRelative("ownsReconSquad").boolValue)
                {
                    PreviewScenarioFields.Field(data, "reconIncreasePercent", "정찰 증가율 (%)");
                    PreviewScenarioFields.Field(data, "reconRadius", "정찰 직교 반경");
                }
                PreviewScenarioFields.Field(data, "ownsAcceleration", "가속 보유 특성");
                PreviewScenarioFields.Field(data, "ownsCleanSweep", "깔끔한 뒷정리 보유 특성");
                PreviewScenarioFields.Field(data, "acceleration", "초기 가속 축적");
                PreviewScenarioFields.Field(data, "cohesionDefenseBonus", "초기 조건부 방어 기여");
                PreviewScenarioFields.Field(data, "ownsRegeneration", "재생 보유");
                if (input.units[_unit].ownsRegeneration)
                    foreach (string field in new[] { "healAmount", "healEveryOwnSideTurnEnds", "completedEligibleTurnEnds" })
                        PreviewScenarioFields.Field(data, field);
                var effects = data.FindPropertyRelative("effects");
                for (int i = 0; i < effects.arraySize; i++)
                {
                    int index = i;
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        PreviewScenarioFields.Effect(effects.GetArrayElementAtIndex(i), input);
                        if (GUILayout.Button("이 효과 제거")) Change(owner, () => input.units[_unit].effects.RemoveAt(index));
                    }
                }
                if (GUILayout.Button("초기 출처 효과 추가"))
                    Change(owner, () => input.units[_unit].effects.Add(new PreviewEffectSourceInput { sourceId = input.units[_unit].id }));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("유닛 추가"))
                    Change(owner, () =>
                    {
                        input.units.Add(new PreviewScenarioUnit { name = "유닛 " + input.units.Count });
                        _unit = input.units.Count - 1;
                    });
                using (new EditorGUI.DisabledScope(input.units.Count == 0))
                {
                    if (GUILayout.Button("유닛 복제"))
                        Change(owner, () =>
                        {
                            var copy = Copy(input.units[_unit]);
                            var old = copy.id;
                            copy.id = NewId();
                            copy.name += " 복사";
                            foreach (var grant in copy.effects)
                            {
                                grant.id = NewId();
                                if (grant.sourceId == old)
                                    grant.sourceId = copy.id;
                            }

                            input.units.Add(copy);
                            _unit = input.units.Count - 1;
                        });
                    if (GUILayout.Button("유닛 제거"))
                        Change(owner, () => input.units.RemoveAt(_unit));
                }
            }
        }

        private void DrawBlocks(SerializedObject owner, TraitPreviewScenarioInput input, PreviewScenarioEntry entry, SerializedProperty blocks)
        {
            for (int i = 0; i < entry.blocks.Count; i++)
            {
                int index = i;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Toggle(_block == i, $"{i + 1}. {entry.blocks[i].name} [{entry.blocks[i].kind}]", "Button"))
                        _block = i;
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUILayout.Button("↑", GUILayout.Width(25)))
                            Change(owner, () =>
                            {
                                (entry.blocks[index - 1], entry.blocks[index]) = (entry.blocks[index], entry.blocks[index - 1]);
                                _block = index - 1;
                            });
                    using (new EditorGUI.DisabledScope(i == entry.blocks.Count - 1))
                        if (GUILayout.Button("↓", GUILayout.Width(25)))
                            Change(owner, () =>
                            {
                                (entry.blocks[index + 1], entry.blocks[index]) = (entry.blocks[index], entry.blocks[index + 1]);
                                _block = index + 1;
                            });
                }
            }

            _addKind = (PreviewBlockKind)EditorGUILayout.EnumPopup("추가할 블록", _addKind);
            if (GUILayout.Button("블록 추가"))
                Change(owner, () =>
                {
                    entry.blocks.Add(Create(_addKind, input));
                    _block = entry.blocks.Count - 1;
                });
            if (entry.blocks.Count == 0)
                return;
            _block = Mathf.Clamp(_block, 0, entry.blocks.Count - 1);
            var block = entry.blocks[_block];
            var kind = (PreviewBlockKind)EditorGUILayout.EnumPopup("블록 종류 (변경 시 입력 초기화)", block.kind);
            if (kind != block.kind)
                Change(owner, () =>
                {
                    var replacement = Create(kind, input);
                    replacement.id = block.id;
                    entry.blocks[_block] = replacement;
                });
            PreviewScenarioBlockEditor.Draw(blocks.GetArrayElementAtIndex(_block), block.kind, input);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("블록 복제"))
                    Change(owner, () =>
                    {
                        var copy = Copy(block);
                        copy.id = NewId();
                        entry.blocks.Insert(_block + 1, copy);
                        ++_block;
                    });
                if (GUILayout.Button("블록 제거"))
                    Change(owner, () => entry.blocks.RemoveAt(_block));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bool previous = _entry > 0 && input.entries[_entry - 1].kind == PreviewEntryKind.Packet && !Action(block.kind);
                using (new EditorGUI.DisabledScope(!previous))
                    if (GUILayout.Button("앞 묶음에 붙이기"))
                        Change(owner, () =>
                        {
                            var target = input.entries[_entry - 1];
                            target.blocks.Add(block);
                            entry.blocks.RemoveAt(_block);
                            if (entry.blocks.Count == 0)
                                input.entries.RemoveAt(_entry);
                            --_entry;
                            _block = target.blocks.Count - 1;
                        });
                bool split = block.kind != PreviewBlockKind.Interception && block.relation == PreviewEventRelation.Recorded && !Action(block.kind);
                using (new EditorGUI.DisabledScope(!split))
                    if (GUILayout.Button("새 묶음으로 분리"))
                        Change(owner, () =>
                        {
                            entry.blocks.RemoveAt(_block);
                            var next = new PreviewScenarioEntry
                            {
                                name = block.name
                            };
                            next.blocks.Add(block);
                            input.entries.Insert(_entry + 1, next);
                            if (entry.blocks.Count == 0)
                                input.entries.RemoveAt(_entry);
                            else
                                ++_entry;
                            _block = 0;
                        });
            }
        }

        private void AddEntry(SerializedObject owner, TraitPreviewScenarioInput input, PreviewEntryKind kind, string label)
        {
            if (GUILayout.Button(label))
                Change(owner, () =>
                {
                    input.entries.Add(new PreviewScenarioEntry { kind = kind, name = label.Substring(2) });
                    _entry = input.entries.Count - 1;
                    _block = 0;
                });
        }

        private static PreviewScenarioBlock Create(PreviewBlockKind kind, TraitPreviewScenarioInput input)
        {
            string source = input.units.FirstOrDefault()?.id ?? "";
            var target = input.units.Skip(1).FirstOrDefault();
            var block = new PreviewScenarioBlock
            {
                kind = kind,
                name = kind.ToString()
            };
            block.movement.unitId = source;
            block.state.unitId = source;
            block.state.grant.sourceId = source;
            block.trait.sourceId = source;
            block.attack.attackerId = source;
            if (target != null)
            {
                block.attack.targetCell = target.unit.cell;
                block.attack.hits.Add(new PreviewHitInput { targetId = target.id, finalHealth = Math.Max(0, target.unit.health - 10) });
            }

            return block;
        }

        private static bool Action(PreviewBlockKind kind) => kind == PreviewBlockKind.Movement || kind == PreviewBlockKind.Attack || kind == PreviewBlockKind.ChildAttack;
        private static string NewId() => Guid.NewGuid().ToString("N");
        private static T Copy<T>(T input) => JsonUtility.FromJson<T>(JsonUtility.ToJson(input));
        private static void Change(SerializedObject owner, Action edit)
        {
            owner.ApplyModifiedProperties();
            Undo.RecordObject(owner.targetObject, "특성 시연 조립 편집");
            edit();
            EditorUtility.SetDirty(owner.targetObject);
            owner.Update();
            TraitActivationPreviewBridge.Host?.Stop();
            GUIUtility.ExitGUI();
        }
    }
}
