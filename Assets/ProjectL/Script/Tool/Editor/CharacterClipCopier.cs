using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// 캐릭터 FBX의 테이크를 <c>.anim</c>으로 복사하고, 클립마다 전투 클립 이벤트 셋(시작·이벤트 타이밍·끝)을 찍습니다(2026-10-29).
    /// 기본 시각은 시작 = 0초, 이벤트 타이밍 = 길이의 절반, 끝 = 길이입니다. 시각은 애니메이션 창에서 조정합니다.
    /// 원본 FBX와 출력 폴더는 메뉴 하나에 한 줄씩입니다.
    /// </summary>
    /// <remarks>
    /// 이벤트 함수 이름의 원본은 <c>CombatAnimationEvents</c>(Systems)입니다. 이 어셈블리는 Systems를 참조하지 않아 이름을 문자열로 적었고,
    /// 틀리면 DB 생성 때 <c>CombatClipEvents.Read</c>가 실패합니다. 시각 규칙은 <c>CombatClipData.FindTimeError</c>가 검사합니다.
    /// 다시 돌리면 같은 <c>.anim</c>을 덮어써 GUID가 유지되고, 곡선·클립 설정은 FBX에서 다시 옮기되 이미 찍힌 이벤트는 그대로 둡니다(조정한 시각 보존).
    /// </remarks>
    internal static class CharacterClipCopier
    {
        private const string MenuRoot = "ProjectL/Animation/클립 복사 (이벤트 셋)/";

        // CombatAnimationEvents의 이벤트 함수 이름입니다(원본은 그 클래스).
        private const string ClipStart = "ClipStart";
        private const string ClipEvent = "ClipEvent";
        private const string ClipEnd = "ClipEnd";

        [MenuItem(MenuRoot + "EnemyCannon")]
        private static void RunEnemyCannon()
            => Run("Assets/ProjectL/Resources/Characters/New/Enemy/EnemyCannon.fbx",
                "Assets/ProjectL/Prefabs/Characters/New/Enemy", "AnimationClips");

        [MenuItem(MenuRoot + "ShotGun")]
        private static void RunShotGun()
            => Run("Assets/ProjectL/Resources/Characters/New/Playerable/ShotGun.fbx",
                "Assets/ProjectL/Prefabs/Characters/New/Playerable", "AniamtionsClips");

        private static void Run(string sourcePath, string outputParent, string outputName)
        {
            var sources = new List<AnimationClip>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(sourcePath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    sources.Add(clip);
            }
            if (sources.Count == 0)
                throw new InvalidOperationException($"[CharacterClipCopier] 클립이 없습니다: {sourcePath}");

            string outputFolder = outputParent + "/" + outputName;
            if (!AssetDatabase.IsValidFolder(outputFolder))
                AssetDatabase.CreateFolder(outputParent, outputName);

            var report = new StringBuilder();
            foreach (AnimationClip source in sources)
            {
                AnimationClip copy = Copy(source, outputFolder, out bool keptEvents);
                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(copy);
                report.AppendLine($"  {source.name} → {AssetDatabase.GetAssetPath(copy)} (길이 {copy.length:0.###}초, "
                    + $"{(keptEvents ? "기존 이벤트 유지" : "이벤트 새로 찍음")}: {Describe(events)})");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CharacterClipCopier] {sourcePath}의 클립 {sources.Count}개를 복사했습니다.\n{report}");
        }

        private static AnimationClip Copy(AnimationClip source, string outputFolder, out bool keptEvents)
        {
            string path = $"{outputFolder}/{source.name.Replace('|', '_')}.anim";
            AnimationClip copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool created = copy == null;
            AnimationEvent[] existingEvents = created ? Array.Empty<AnimationEvent>() : AnimationUtility.GetAnimationEvents(copy);
            if (created)
                copy = new AnimationClip();
            else
                copy.ClearCurves();
            copy.frameRate = source.frameRate;

            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
            var curves = new AnimationCurve[bindings.Length];
            for (int i = 0; i < bindings.Length; i++)
                curves[i] = AnimationUtility.GetEditorCurve(source, bindings[i]);
            // 회전 곡선은 성분을 하나씩 넣으면 사원수 보정이 끼므로 한 번에 넣습니다.
            AnimationUtility.SetEditorCurves(copy, bindings, curves);

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
                AnimationUtility.SetObjectReferenceCurve(copy, binding, AnimationUtility.GetObjectReferenceCurve(source, binding));

            AnimationUtility.SetAnimationClipSettings(copy, AnimationUtility.GetAnimationClipSettings(source));
            copy.EnsureQuaternionContinuity();

            keptEvents = existingEvents.Length > 0;
            AnimationUtility.SetAnimationEvents(copy, keptEvents ? existingEvents : DefaultEvents(copy.length));

            if (created)
                AssetDatabase.CreateAsset(copy, path);
            else
                EditorUtility.SetDirty(copy);
            return copy;
        }

        // 시작·중간·끝에 하나씩 찍습니다. 조정은 애니메이션 창에서 합니다.
        private static AnimationEvent[] DefaultEvents(float length)
        {
            return new[]
            {
                new AnimationEvent { functionName = ClipStart, time = 0f },
                new AnimationEvent { functionName = ClipEvent, time = length * 0.5f },
                new AnimationEvent { functionName = ClipEnd, time = length },
            };
        }

        private static string Describe(AnimationEvent[] events)
        {
            var text = new StringBuilder();
            foreach (AnimationEvent item in events)
            {
                if (text.Length > 0)
                    text.Append(", ");
                text.Append($"{item.functionName} {item.time:0.###}");
            }
            return text.ToString();
        }
    }
}
