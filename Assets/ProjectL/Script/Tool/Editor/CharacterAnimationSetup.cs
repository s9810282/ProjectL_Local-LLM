using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>캐릭터별 FBX 클립을 복사하고 프리팹의 기본·부속 Idle을 연결하는 에디터 조립 도구입니다.</summary>
    internal static class CharacterAnimationSetup
    {
        private const string SourceRoot = "Assets/ProjectL/Resources/Characters/New";
        private const string PrefabRoot = "Assets/ProjectL/Prefabs/Characters/New";
        private const string IdleLayerPrefix = "ClipIdle_";
        private const string ReportPath = "Temp/character-animation-setup-report.json";

        // FBX 안의 몸 Idle 이름입니다. 나머지 Idle은 말·무기 등 부속 레이어로 연결합니다.
        private static readonly Dictionary<string, string> MainIdleNames = new Dictionary<string, string>
        {
            { "Enemy/EnemyCannon", "Enemy_Cannon_Idle" },
            { "Enemy/EnemyHeavy", "EnemyHeavy_Idle" },
            { "Enemy/EnemyMiner", "Enemy_Miner_Idle" },
            { "Enemy/EnemyShabow", "Enemy_Horse_Shovel_Idle" },
            { "Enemy/Stage1Boss", "Stage1Boss_Idle" },
            { "Playerable/Akimbo", "Akimbo_Idle" },
            { "Playerable/Ballistar", "Ballista_Sniper_Idle" },
            { "Playerable/Boomber", "Horse_Player_Boomber_Idle" },
            { "Playerable/CactusCannon", "CactusCannon_Idle" },
            { "Playerable/CowBoy", "CowBoy_Idle" },
            { "Playerable/Gravedigger", "Gravedigger_Idle" },
            { "Playerable/ShotGun", "ShotGun_Idle" },
            { "Playerable/Sniper", "Sniper_Idle" },
        };

        [Serializable]
        private sealed class CharacterResult
        {
            public string character;
            public string source;
            public string prefab;
            public string controller;
            public string[] clips;
            public string[] idleClips;
            public string status;
            public string error;
        }

        [Serializable]
        private sealed class SetupReport
        {
            public List<CharacterResult> characters = new List<CharacterResult>();
        }

        /// <summary>메뉴와 배치 실행의 공용 진입점입니다. 결과는 캐릭터별로 저장합니다.</summary>
        [MenuItem("ProjectL/Animation/New 캐릭터 전체 클립 복사·Animator 연결")]
        public static void CopyAllAndBind()
        {
            var report = new SetupReport();
            int failures = 0;
            string[] sources = Directory.GetFiles(SourceRoot, "*.fbx", SearchOption.AllDirectories);
            Array.Sort(sources, StringComparer.Ordinal);
            if (sources.Length == 0)
                throw new InvalidOperationException($"[CharacterAnimationSetup] FBX가 없습니다: {SourceRoot}");

            foreach (string file in sources)
            {
                string source = file.Replace('\\', '/');
                string relative = Path.GetDirectoryName(source).Replace('\\', '/').Substring(SourceRoot.Length + 1);
                var result = new CharacterResult
                {
                    character = relative,
                    source = source,
                    prefab = $"{PrefabRoot}/{relative}/{Path.GetFileNameWithoutExtension(source)}.prefab",
                };
                report.characters.Add(result);
                try
                {
                    SetupCharacter(result);
                }
                catch (Exception exception)
                {
                    failures++;
                    result.status = "failed";
                    result.error = exception.ToString();
                    Debug.LogException(exception);
                }
            }

            AssetDatabase.SaveAssets();
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            if (failures > 0)
            {
                var failure = new InvalidOperationException($"[CharacterAnimationSetup] {failures}개 캐릭터 연결 실패. 결과: {ReportPath}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw failure;
            }
            Debug.Log($"[CharacterAnimationSetup] 완료: {report.characters.Count}개 FBX. 결과: {ReportPath}");
        }

        /// <summary>사용자가 모델 교체를 승인한 두 캐릭터만 새 모델로 갱신하고 연결합니다.</summary>
        [MenuItem("ProjectL/Animation/EnemyShabow·Boomber 모델 갱신·Animator 연결")]
        public static void RepairMountedAndBind()
        {
            if (!File.Exists(ReportPath))
                throw new InvalidOperationException($"[CharacterAnimationSetup] 먼저 전체 클립을 복사해야 합니다: {ReportPath}");
            SetupReport report = JsonUtility.FromJson<SetupReport>(File.ReadAllText(ReportPath));
            int failures = 0;
            foreach (string character in new[] { "Enemy/EnemyShabow", "Playerable/Boomber" })
            {
                CharacterResult result = report.characters.Find(item => item.character == character);
                if (result == null)
                    throw new InvalidOperationException($"[CharacterAnimationSetup] 기존 결과가 없습니다: {character}");
                try
                {
                    CharacterPrefabModelRepair.ReplaceVisualChildren(result.prefab, result.source);
                    SetupCharacter(result);
                    result.error = null;
                }
                catch (Exception exception)
                {
                    failures++;
                    result.status = "failed";
                    result.error = exception.ToString();
                    Debug.LogException(exception);
                }
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            if (failures > 0)
                throw new InvalidOperationException($"[CharacterAnimationSetup] 모델 갱신·연결 실패 {failures}개. 결과: {ReportPath}");
            Debug.Log($"[CharacterAnimationSetup] EnemyShabow·Boomber 모델 갱신·연결 완료. 결과: {ReportPath}");
        }

        private static void SetupCharacter(CharacterResult result)
        {
            var sources = new List<AnimationClip>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(result.source))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    sources.Add(clip);
            sources.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            if (sources.Count == 0)
            {
                if (result.character != "Enemy/EnemyRecover")
                    throw new InvalidOperationException($"[CharacterAnimationSetup] FBX에 클립이 없습니다: {result.source}");
                result.clips = Array.Empty<string>();
                result.idleClips = Array.Empty<string>();
                result.status = "no-animation-in-source";
                return;
            }
            if (!MainIdleNames.TryGetValue(result.character, out string mainIdleName))
                throw new InvalidOperationException($"[CharacterAnimationSetup] 기본 Idle 정의가 없습니다: {result.character}");

            string output = Path.GetDirectoryName(result.prefab).Replace('\\', '/') + "/Animation";
            if (!AssetDatabase.IsValidFolder(output))
                throw new InvalidOperationException($"[CharacterAnimationSetup] Animation 폴더가 없습니다: {output}");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(result.prefab) == null)
                throw new InvalidOperationException($"[CharacterAnimationSetup] 프리팹이 없습니다: {result.prefab}");

            var copies = new List<AnimationClip>();
            var idles = new List<AnimationClip>();
            AnimationClip mainIdle = null;
            foreach (AnimationClip source in sources)
            {
                bool isIdle = source.name.EndsWith("_Idle", StringComparison.Ordinal);
                AnimationClip copy = CharacterClipCopier.Copy(source, output, out _, !isIdle);
                if (isIdle)
                {
                    var settings = AnimationUtility.GetAnimationClipSettings(copy);
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(copy, settings);
                    idles.Add(copy);
                    if (source.name == mainIdleName || source.name.EndsWith("|" + mainIdleName, StringComparison.Ordinal))
                        mainIdle = copy;
                }
                EditorUtility.SetDirty(copy);
                copies.Add(copy);
            }
            string mainPrefix = mainIdleName.Substring(0, mainIdleName.Length - "_Idle".Length);
            foreach (string action in new[] { "Idle", "Attack", "Hit", "PullBack" })
            {
                AnimationClip primary = copies.Find(clip => clip.name == mainPrefix + "_" + action);
                if (primary == null) continue;
                List<AnimationClip> companions = copies.FindAll(clip => clip != primary
                    && clip.name.EndsWith("_" + action, StringComparison.Ordinal));
                CharacterAnimationParts.Separate(primary, companions);
            }
            result.clips = copies.ConvertAll(AssetDatabase.GetAssetPath).ToArray();
            result.idleClips = idles.ConvertAll(AssetDatabase.GetAssetPath).ToArray();
            if (mainIdle == null)
                throw new InvalidOperationException($"[CharacterAnimationSetup] 기본 Idle을 찾지 못했습니다: {result.source}, {mainIdleName}");

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            bool usesOpenStage = stage != null && stage.assetPath == result.prefab;
            GameObject root = usesOpenStage ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(result.prefab);
            try
            {
                Animator[] animators = root.GetComponentsInChildren<Animator>(true);
                if (animators.Length > 1)
                    throw new InvalidOperationException($"[CharacterAnimationSetup] Animator가 여러 개입니다: {result.prefab}, {animators.Length}개");
                Animator animator = animators.Length == 1 ? animators[0] : root.AddComponent<Animator>();
                ValidateBindings(animator, idles, result.prefab);
                string ownerFolder = Path.GetDirectoryName(result.prefab).Replace('\\', '/') + "/";
                AnimatorController controller;
                if (animator.runtimeAnimatorController == null)
                {
                    string destination = output + "/" + Path.GetFileNameWithoutExtension(result.prefab) + ".controller";
                    controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(destination);
                    if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(destination);
                }
                else
                {
                    controller = animator.runtimeAnimatorController as AnimatorController;
                    if (controller == null || !AssetDatabase.GetAssetPath(controller).StartsWith(ownerFolder, StringComparison.Ordinal))
                        throw new InvalidOperationException($"[CharacterAnimationSetup] 캐릭터 폴더 밖의 Controller입니다: {result.prefab}");
                }
                if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
                SetIdle(controller.layers[0].stateMachine, mainIdle);
                foreach (AnimationClip idle in idles)
                {
                    if (idle == mainIdle) continue;
                    string layerName = IdleLayerPrefix + idle.name;
                    int index = Array.FindIndex(controller.layers, layer => layer.name == layerName);
                    if (index < 0)
                    {
                        controller.AddLayer(layerName);
                        index = controller.layers.Length - 1;
                    }
                    AnimatorControllerLayer[] layers = controller.layers;
                    layers[index].defaultWeight = 1f;
                    SetIdle(layers[index].stateMachine, idle);
                    controller.layers = layers;
                }
                EditorUtility.SetDirty(controller);
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(root, result.prefab, out bool success);
                if (!success)
                    throw new InvalidOperationException($"[CharacterAnimationSetup] 프리팹 저장 실패: {result.prefab}");
                result.controller = AssetDatabase.GetAssetPath(controller);
                result.status = "copied-and-bound";
            }
            finally
            {
                if (!usesOpenStage) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>이미 전체 목록을 작성한 뒤 캐릭터 하나만 다시 복사하고 연결합니다.</summary>
        [MenuItem("ProjectL/Animation/EnemyShabow 클립 갱신·Animator 연결")]
        public static void RefreshEnemyShabow()
            => CopyCharacterAndBind("Enemy/EnemyShabow");

        internal static void CopyCharacterAndBind(string character)
        {
            SetupReport report = File.Exists(ReportPath)
                ? JsonUtility.FromJson<SetupReport>(File.ReadAllText(ReportPath)) : new SetupReport();
            CharacterResult result = report.characters.Find(item => item.character == character);
            if (result == null)
            {
                string name = Path.GetFileName(character);
                result = new CharacterResult
                {
                    character = character,
                    source = $"{SourceRoot}/{character}/{name}.fbx",
                    prefab = $"{PrefabRoot}/{character}/{name}.prefab",
                };
                report.characters.Add(result);
            }
            SetupCharacter(result);
            result.error = null;
            AssetDatabase.SaveAssets();
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            Debug.Log($"[CharacterAnimationSetup] {character} 클립·Animator 연결 완료.");
        }

        private static void SetIdle(AnimatorStateMachine machine, AnimationClip clip)
        {
            if (machine == null)
                throw new InvalidOperationException($"[CharacterAnimationSetup] StateMachine이 없습니다: {clip.name}");
            AnimatorState state = machine.defaultState;
            if (state == null) state = machine.AddState(clip.name);
            state.motion = clip;
            state.name = clip.name;
            machine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(machine);
        }

        private static void ValidateBindings(Animator animator, List<AnimationClip> clips, string prefab)
        {
            foreach (AnimationClip clip in clips)
            {
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (binding.path.Length == 0 || animator.transform.Find(binding.path) != null) continue;
                    throw new InvalidOperationException($"[CharacterAnimationSetup] Idle 뼈대 경로가 프리팹에 없습니다: {prefab}, 클립 {clip.name}, 경로 {binding.path}");
                }
            }
        }
    }
}
