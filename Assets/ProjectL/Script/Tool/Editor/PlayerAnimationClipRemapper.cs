using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// 아군 애니메이션 임시 처리 도구입니다(2026-10-20, 민형님 재내보내기 전까지).
    /// <c>Animation.fbx</c>의 테이크는 캐릭터 뼈대를 <c>Barllista/뼈대/BaseModel/…</c> 아래에서 움직이는데, 아군 모델 FBX는 같은 뼈대가
    /// <c>아마츄어/…</c> 아래에 있어 곡선이 하나도 붙지 않습니다(모델 요청 메모 R1). 그래서 테이크를 <c>.anim</c>으로 복사하면서 경로 앞부분을 바꾸고,
    /// 아군 클립 묶음 SO와 Idle 컨트롤러가 가리키던 FBX 클립을 복사본으로 바꿉니다.
    /// </summary>
    /// <remarks>
    /// 경로 규칙은 <see cref="TryRemap"/> 하나입니다. 이벤트·클립 설정은 FBX 가져오기 설정 그대로 옮기되, 이름이 Idle로 끝나는 테이크만 Loop Time을 켭니다.
    /// 다시 돌리면 같은 <c>.anim</c>을 덮어써 GUID가 유지되므로, FBX 이벤트를 고친 뒤 다시 돌려도 참조가 끊기지 않습니다.
    /// 재내보내기로 모델과 클립 계층이 맞으면 이 도구와 복사본은 필요 없습니다(참조를 FBX 클립으로 되돌릴 것).
    /// </remarks>
    internal static class PlayerAnimationClipRemapper
    {
        private const string SourcePath = "Assets/ProjectL/Resources/Animations/Characters/Player/Animation.fbx";
        private const string OutputParent = "Assets/ProjectL/Prefabs/Characters/Colored/Playerable";
        private const string OutputName = "AnimationClip";
        private const string OutputFolder = OutputParent + "/" + OutputName;

        // FBX 클립 참조를 복사본으로 바꿀 곳입니다(아군 클립 묶음 SO, 아군 Idle 컨트롤러).
        private static readonly string[] ReferenceFolders =
        {
            "Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentation/AnimationClip/Playerable",
            "Assets/ProjectL/Prefabs/Characters/Colored/Playerable/AnimationControlelr",
        };

        // 캐릭터 아마추어 오브젝트 자체입니다. 값이 발리스타 뼈 기준이라 아군 모델의 아마츄어에 옮기면 모델이 어긋나므로 뺍니다.
        private const string CharacterArmature = "Barllista/뼈대/BaseModel";
        private const string ModelArmature = "아마츄어";
        // 발리스타 무기 리그입니다. Ballista.prefab은 Animation.fbx를 "Animation"이라는 자식으로 품고 있어 그 아래로 옮깁니다.
        private const string WeaponRig = "Barllista";
        private const string WeaponRigInBallistaPrefab = "Animation/Barllista";
        // 이름이 이것으로 끝나는 테이크는 Idle 컨트롤러용이라 복사본의 Loop Time을 켭니다.
        private const string IdleSuffix = "Idle";

        [MenuItem("ProjectL/Animation/아군 클립 경로 변환 (임시)")]
        private static void Run()
        {
            var sources = new List<AnimationClip>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(SourcePath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    sources.Add(clip);
            }
            if (sources.Count == 0)
                throw new InvalidOperationException($"[PlayerAnimationClipRemapper] 클립이 없습니다: {SourcePath}");

            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder(OutputParent, OutputName);

            var copies = new Dictionary<int, AnimationClip>();
            var report = new StringBuilder();
            foreach (AnimationClip source in sources)
            {
                AnimationClip copy = Copy(source, out int kept, out int dropped, out int events);
                copies.Add(source.GetInstanceID(), copy);
                report.AppendLine($"  {source.name} → {AssetDatabase.GetAssetPath(copy)} (곡선 {kept}, 뺀 곡선 {dropped}, 이벤트 {events})");
            }

            int replaced = Repoint(copies, report);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerAnimationClipRemapper] 클립 {copies.Count}개를 복사하고 참조 {replaced}곳을 바꿨습니다.\n{report}");
        }

        /// <summary>
        /// FBX 경로를 아군 프리팹 경로로 바꿉니다. 캐릭터 아마추어 자체 곡선이면 false(뺌)입니다.
        /// 캐릭터 뼈대는 <c>아마츄어/…</c>로, 발리스타 무기 리그는 <c>Animation/Barllista/…</c>로 옮기고 나머지는 그대로 둡니다.
        /// </summary>
        internal static bool TryRemap(string path, out string remapped)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path == CharacterArmature)
            {
                remapped = null;
                return false;
            }
            if (path.StartsWith(CharacterArmature + "/", StringComparison.Ordinal))
            {
                remapped = ModelArmature + path.Substring(CharacterArmature.Length);
                return true;
            }
            if (path == WeaponRig || path.StartsWith(WeaponRig + "/", StringComparison.Ordinal))
            {
                remapped = WeaponRigInBallistaPrefab + path.Substring(WeaponRig.Length);
                return true;
            }
            remapped = path;
            return true;
        }

        private static AnimationClip Copy(AnimationClip source, out int kept, out int dropped, out int events)
        {
            string path = $"{OutputFolder}/{source.name.Replace('|', '_')}.anim";
            AnimationClip copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool created = copy == null;
            if (created)
                copy = new AnimationClip();
            else
                copy.ClearCurves();
            copy.frameRate = source.frameRate;

            kept = 0;
            dropped = 0;
            var bindings = new List<EditorCurveBinding>();
            var curves = new List<AnimationCurve>();
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                if (!TryRemap(binding.path, out string remapped))
                {
                    dropped++;
                    continue;
                }
                EditorCurveBinding moved = binding;
                moved.path = remapped;
                bindings.Add(moved);
                curves.Add(AnimationUtility.GetEditorCurve(source, binding));
                kept++;
            }
            // 회전 곡선은 성분을 하나씩 넣으면 사원수 보정이 끼므로 한 번에 넣습니다.
            AnimationUtility.SetEditorCurves(copy, bindings.ToArray(), curves.ToArray());

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                if (!TryRemap(binding.path, out string remapped))
                {
                    dropped++;
                    continue;
                }
                EditorCurveBinding moved = binding;
                moved.path = remapped;
                AnimationUtility.SetObjectReferenceCurve(copy, moved, AnimationUtility.GetObjectReferenceCurve(source, binding));
                kept++;
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            // FBX 가져오기 설정에서 Idle 테이크의 Loop Time이 꺼져 있어 한 번만 돌고 멈추므로 복사본은 반복합니다.
            // 사건 클립(공격·피격)은 반복 금지 규칙이 있어 이름이 Idle로 끝나는 테이크만 켭니다.
            if (source.name.EndsWith(IdleSuffix, StringComparison.Ordinal))
                settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            AnimationEvent[] clipEvents = AnimationUtility.GetAnimationEvents(source);
            AnimationUtility.SetAnimationEvents(copy, clipEvents);
            events = clipEvents.Length;
            copy.EnsureQuaternionContinuity();

            if (created)
                AssetDatabase.CreateAsset(copy, path);
            else
                EditorUtility.SetDirty(copy);
            return copy;
        }

        // 참조 폴더의 모든 에셋(컨트롤러의 상태·블렌드 트리 포함)에서 FBX 클립 참조를 복사본으로 바꿉니다.
        private static int Repoint(IReadOnlyDictionary<int, AnimationClip> copies, StringBuilder report)
        {
            int replaced = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Object", ReferenceFolders))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(assetPath))
                    continue;
                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (asset == null)
                        continue;
                    var serialized = new SerializedObject(asset);
                    SerializedProperty property = serialized.GetIterator();
                    bool changed = false;
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference)
                            continue;
                        if (property.objectReferenceValue is AnimationClip clip
                            && copies.TryGetValue(clip.GetInstanceID(), out AnimationClip copy))
                        {
                            report.AppendLine($"  참조: {assetPath} ({asset.name}.{property.propertyPath}) {clip.name} → {copy.name}");
                            property.objectReferenceValue = copy;
                            changed = true;
                            replaced++;
                        }
                    }
                    if (!changed)
                        continue;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                }
            }
            return replaced;
        }
    }
}
