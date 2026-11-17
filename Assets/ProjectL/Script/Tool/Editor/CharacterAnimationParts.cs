using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>FBX 복사본의 몸·부속 클립이 같은 곡선을 덮어쓰지 않도록 정지 곡선 중복을 정리합니다.</summary>
    internal static class CharacterAnimationParts
    {
        private const float Tolerance = 0.0001f;
        private const string EnemyRiderRoot = "Horse_Bone/뼈대.005/CTRL_Body/BaseHumanBone";

        internal static void Separate(AnimationClip main, List<AnimationClip> companions)
        {
            if (companions.Count == 0) return;
            var clips = new List<AnimationClip> { main };
            clips.AddRange(companions);
            var bindings = new Dictionary<EditorCurveBinding, List<int>>();
            var removals = new List<EditorCurveBinding>[clips.Count];
            for (int i = 0; i < clips.Count; i++)
            {
                removals[i] = new List<EditorCurveBinding>();
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clips[i]))
                {
                    if (!bindings.TryGetValue(binding, out List<int> owners))
                    {
                        owners = new List<int>();
                        bindings.Add(binding, owners);
                    }
                    owners.Add(i);
                }
            }
            foreach (KeyValuePair<EditorCurveBinding, List<int>> entry in bindings)
            {
                if (entry.Value.Count < 2) continue;
                // 기수와 말이 같은 아마추어에 들어 있어 말 테이크에도 기수 곡선이 구워져 있습니다.
                // 이 모델은 BaseHumanBone 하위가 기수, 그 밖의 Horse_Bone 하위가 말입니다.
                if (main.name.StartsWith("Enemy_Horse_Shovel_", StringComparison.Ordinal)
                    && companions.Count == 1 && companions[0].name.StartsWith("Enemy_Horse_", StringComparison.Ordinal))
                {
                    if (entry.Key.path != "Horse_Bone" && !entry.Key.path.StartsWith("Horse_Bone/", StringComparison.Ordinal))
                        throw new InvalidOperationException($"[CharacterAnimationParts] 기수·말의 예상 경로가 아닙니다: {entry.Key.path}");
                    bool rider = entry.Key.path == EnemyRiderRoot || entry.Key.path.StartsWith(EnemyRiderRoot + "/", StringComparison.Ordinal);
                    foreach (int index in entry.Value)
                        if (index != (rider ? 0 : 1)) removals[index].Add(entry.Key);
                    continue;
                }
                int owner = entry.Value[0];
                bool hasMotion = false;
                foreach (int index in entry.Value)
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clips[index], entry.Key);
                    if (!Moves(curve)) continue;
                    if (hasMotion && !Equivalent(AnimationUtility.GetEditorCurve(clips[owner], entry.Key), curve))
                        throw new InvalidOperationException($"[CharacterAnimationParts] 서로 다른 동작이 같은 곡선을 움직입니다: {clips[owner].name}, {clips[index].name}, {entry.Key.path}.{entry.Key.propertyName}");
                    owner = index;
                    hasMotion = true;
                }
                foreach (int index in entry.Value)
                    if (index != owner) removals[index].Add(entry.Key);
            }
            KeepTransformComponentsTogether(clips, bindings, removals);
            for (int i = 0; i < clips.Count; i++)
            {
                EditorCurveBinding[] removed = removals[i].ToArray();
                AnimationUtility.SetEditorCurves(clips[i], removed, new AnimationCurve[removed.Length]);
                if (i > 0) AnimationUtility.SetAnimationEvents(clips[i], Array.Empty<AnimationEvent>());
                EditorUtility.SetDirty(clips[i]);
            }
        }

        // 사원수·벡터의 성분을 서로 다른 클립으로 찢으면 보간 결과가 달라지므로 한 소유자에게 함께 둡니다.
        private static void KeepTransformComponentsTogether(List<AnimationClip> clips,
            Dictionary<EditorCurveBinding, List<int>> bindings, List<EditorCurveBinding>[] removals)
        {
            var groups = new Dictionary<EditorCurveBinding, List<EditorCurveBinding>>();
            foreach (EditorCurveBinding binding in bindings.Keys)
            {
                if (binding.type != typeof(Transform)) continue;
                int dot = binding.propertyName.LastIndexOf('.');
                if (dot < 0) continue;
                EditorCurveBinding group = binding;
                group.propertyName = binding.propertyName.Substring(0, dot);
                if (!groups.TryGetValue(group, out List<EditorCurveBinding> components))
                {
                    components = new List<EditorCurveBinding>();
                    groups.Add(group, components);
                }
                components.Add(binding);
            }
            foreach (List<EditorCurveBinding> components in groups.Values)
            {
                int owner = -1;
                foreach (EditorCurveBinding binding in components)
                {
                    foreach (int index in bindings[binding])
                    {
                        if (removals[index].Contains(binding)
                            || !Moves(AnimationUtility.GetEditorCurve(clips[index], binding))) continue;
                        if (owner >= 0 && owner != index)
                        {
                            foreach (EditorCurveBinding other in components)
                            {
                                AnimationCurve left = AnimationUtility.GetEditorCurve(clips[owner], other);
                                AnimationCurve right = AnimationUtility.GetEditorCurve(clips[index], other);
                                if (left == null || right == null || !Equivalent(left, right))
                                    throw new InvalidOperationException($"[CharacterAnimationParts] 변환 성분의 소유가 충돌합니다: {clips[owner].name}, {clips[index].name}, {binding.path}.{binding.propertyName}");
                            }
                        }
                        owner = index;
                    }
                }
                if (owner < 0)
                {
                    EditorCurveBinding first = components[0];
                    foreach (int index in bindings[first])
                        if (!removals[index].Contains(first)) { owner = index; break; }
                }
                if (owner < 0) throw new InvalidOperationException("[CharacterAnimationParts] 변환 곡선의 소유자가 없습니다.");
                foreach (EditorCurveBinding binding in components)
                    foreach (int index in bindings[binding])
                        if (index == owner) removals[index].Remove(binding);
                        else if (!removals[index].Contains(binding)) removals[index].Add(binding);
            }
        }

        private static bool Moves(AnimationCurve curve)
        {
            Keyframe[] keys = curve.keys;
            if (keys.Length == 0) return false;
            foreach (Keyframe key in keys)
                if (Mathf.Abs(key.value - keys[0].value) > Tolerance
                    || (!float.IsInfinity(key.inTangent) && Mathf.Abs(key.inTangent) > Tolerance)
                    || (!float.IsInfinity(key.outTangent) && Mathf.Abs(key.outTangent) > Tolerance))
                    return true;
            return false;
        }

        private static bool Equivalent(AnimationCurve left, AnimationCurve right)
        {
            Keyframe[] a = left.keys;
            Keyframe[] b = right.keys;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (!Near(a[i].time, b[i].time) || !Near(a[i].value, b[i].value)
                    || !Near(a[i].inTangent, b[i].inTangent) || !Near(a[i].outTangent, b[i].outTangent))
                    return false;
            return true;
        }

        private static bool Near(float left, float right)
            => left == right || Mathf.Abs(left - right) <= Tolerance;
    }
}
