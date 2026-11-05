using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Tool.Editor;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ProjectL.FbxPlacement.Editor.Tests
{
    public sealed class FbxPlacementBakingTests
    {
        private GameObject _root;
        private GameObject _second;
        private GameObject _instance;
        private Texture2D _texture;
        private Texture2D _otherTexture;
        private LightmapData[] _previousMaps;
        private LightmapsMode _previousMode;
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _previousMaps = LightmapSettings.lightmaps;
            _previousMode = LightmapSettings.lightmapsMode;
            _root = FbxPlacementWorkspace.CreateRoot();
            _texture = new Texture2D(2, 2);
            _otherTexture = new Texture2D(2, 2);
        }

        [TearDown]
        public void TearDown()
        {
            FbxPlacementBaker.Current?.Cancel();
            if (_instance != null) Object.DestroyImmediate(_instance);
            FbxPlacementWorkspace.Close(_second);
            FbxPlacementWorkspace.Close(_root);
            LightmapSettings.lightmaps = _previousMaps;
            LightmapSettings.lightmapsMode = _previousMode;
            if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
            if (_texture != null && !EditorUtility.IsPersistent(_texture)) Object.DestroyImmediate(_texture);
            if (_otherTexture != null && !EditorUtility.IsPersistent(_otherTexture)) Object.DestroyImmediate(_otherTexture);
        }

        private MeshRenderer AddCube(GameObject root)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(cube, root.scene);
            cube.transform.SetParent(root.transform, false);
            return cube.GetComponent<MeshRenderer>();
        }

        private MapBakedLighting Configure(GameObject root, MeshRenderer renderer, Texture2D texture)
        {
            var binding = root.AddComponent<MapBakedLighting>();
            binding.SetData(LightmapsMode.NonDirectional,
                new[] { new MapBakedLighting.Atlas { color = texture } },
                new[] { new MapBakedLighting.Surface { renderer = renderer, atlasIndex = 0, scaleOffset = new Vector4(.5f, .5f, .25f, 0) } },
                Array.Empty<MapBakedLighting.BakedLight>());
            binding.Apply();
            return binding;
        }

        [Test]
        public void MapAssets_SplitLightingIntoSo_ResolveDuplicateNamesAndAssembleNewInstances()
        {
            LightmapSettings.lightmaps = Array.Empty<LightmapData>();
            MeshRenderer first = AddCube(_root);
            MeshRenderer second = AddCube(_root);
            first.name = second.name = "같은 이름";
            var lightObject = new GameObject("Mixed Light");
            SceneManager.MoveGameObjectToScene(lightObject, _root.scene);
            lightObject.transform.SetParent(_root.transform, false);
            Light light = lightObject.AddComponent<Light>();
            _folder = "Assets/Tests/FbxMapSo_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Tests", System.IO.Path.GetFileName(_folder));
            AssetDatabase.CreateAsset(_texture, _folder + "/Lightmap.asset");
            var binding = _root.AddComponent<MapBakedLighting>();
            binding.SetData(LightmapsMode.NonDirectional,
                new[] { new MapBakedLighting.Atlas { color = _texture } },
                new[]
                {
                    new MapBakedLighting.Surface { renderer = first, atlasIndex = 0, scaleOffset = new Vector4(.5f, 1, 0, 0) },
                    new MapBakedLighting.Surface { renderer = second, atlasIndex = 0, scaleOffset = new Vector4(.5f, 1, .5f, 0) }
                }, new[]
                {
                    new MapBakedLighting.BakedLight
                    {
                        light = light, isBaked = true, bakeType = LightmapBakeType.Mixed,
                        mixedMode = MixedLightingMode.Shadowmask, maskChannel = 2, probeIndex = -1
                    }
                });
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] sceneRoots = activeScene.GetRootGameObjects();
            bool sceneWasDirty = activeScene.isDirty;
            MapDefinitionSO map = FbxPlacementMapAssets.Save(_root, _folder + "/Map.prefab");
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeScene));
            Assert.That(activeScene.GetRootGameObjects(), Is.EquivalentTo(sceneRoots));
            Assert.That(activeScene.isDirty, Is.EqualTo(sceneWasDirty));
            Assert.That(_root.scene.GetRootGameObjects(), Has.Length.EqualTo(1));
            Assert.That(AssetDatabase.GetAssetPath(map), Is.EqualTo(_folder + "/Map_Map.asset"));
            Assert.That(map.BakedLighting, Is.Not.Null);
            Assert.That(EditorUtility.IsPersistent(map.BakedLighting), Is.True);
            Assert.That(map.Prefab.GetComponent<MapBakedLighting>(), Is.Null, "배치 프리팹과 베이크 데이터는 분리합니다.");
            Assert.That(binding.Data, Is.SameAs(map.BakedLighting));
            Assert.That(FbxPlacementMapAssets.Save(_root, _folder + "/Map.prefab"), Is.SameAs(map));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(map.BakedLighting), ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(map), ImportAssetOptions.ForceUpdate);
            map = AssetDatabase.LoadAssetAtPath<MapDefinitionSO>(_folder + "/Map_Map.asset");
            _second = FbxPlacementMapAssets.Open(map);
            _instance = MapAssembler.Instantiate(map);
            MeshRenderer[] instanceSurfaces = _instance.GetComponentsInChildren<MeshRenderer>();
            Assert.That(instanceSurfaces[0].lightmapScaleOffset.z, Is.Zero);
            Assert.That(instanceSurfaces[1].lightmapScaleOffset.z, Is.EqualTo(.5f));
            Assert.That(instanceSurfaces[0].lightmapIndex, Is.EqualTo(instanceSurfaces[1].lightmapIndex));
            Assert.That(instanceSurfaces[0], Is.Not.SameAs(first));
            Assert.That(_instance.GetComponent<MapBakedLighting>().Data, Is.SameAs(map.BakedLighting));
            LightBakingOutput output = _instance.GetComponentInChildren<Light>().bakingOutput;
            Assert.That(output.isBaked, Is.True);
            Assert.That(output.lightmapBakeType, Is.EqualTo(LightmapBakeType.Mixed));
            Assert.That(output.mixedLightingMode, Is.EqualTo(MixedLightingMode.Shadowmask));
            Assert.That(output.occlusionMaskChannel, Is.EqualTo(2));
            _second.transform.GetChild(1).name = "계층 변경";
            int mapsBefore = LightmapSettings.lightmaps.Length;
            Assert.Throws<InvalidOperationException>(() => MapAssembler.Apply(_second, map.BakedLighting));
            Assert.That(LightmapSettings.lightmaps, Has.Length.EqualTo(mapsBefore));
        }

        [Test]
        public void MapAssets_CanSaveAndAssembleAnUnbakedMap()
        {
            AddCube(_root);
            _folder = "Assets/Tests/FbxUnbakedMap_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Tests", System.IO.Path.GetFileName(_folder));
            MapDefinitionSO map = FbxPlacementMapAssets.Save(_root, _folder + "/Map.prefab");
            Assert.That(map.BakedLighting, Is.Null);
            _instance = MapAssembler.Instantiate(map);
            Assert.That(_instance.GetComponentInChildren<MeshRenderer>(), Is.Not.Null);
            Assert.That(_instance.GetComponent<MapBakedLighting>(), Is.Null);
        }

        [Test]
        public void StaticSettings_ChildrenAreOptional_SelectionIsDeduplicated_AndUndoRestoresGi()
        {
            MeshRenderer renderer = AddCube(_root);
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.ReflectionProbeStatic);
            var selection = new[] { _root.transform, renderer.transform };
            var targets = FbxPlacementStaticSettings.GetTargets(selection, true);
            Assert.That(targets, Has.Count.EqualTo(2));
            var parentOnly = FbxPlacementStaticSettings.GetTargets(new[] { _root.transform }, false);
            FbxPlacementStaticSettings.SetFlag(parentOnly, StaticEditorFlags.OccluderStatic, true);
            Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject), Is.EqualTo(StaticEditorFlags.ReflectionProbeStatic));
            renderer.receiveGI = ReceiveGI.LightProbes;
            FbxPlacementStaticSettings.SetFlag(targets, StaticEditorFlags.ContributeGI, true);
            Assert.That(FbxPlacementBakeData.IsTarget(renderer), Is.True);
            Assert.That((GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ReflectionProbeStatic) != 0, Is.True);
            Undo.PerformUndo();
            Assert.That(renderer.receiveGI, Is.EqualTo(ReceiveGI.LightProbes));
            Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject), Is.EqualTo(StaticEditorFlags.ReflectionProbeStatic));
            Undo.PerformRedo();
            Assert.That(FbxPlacementBakeData.IsTarget(renderer), Is.True);
        }

        [Test]
        public void StaticSettings_AllFlagsKeepBakeCompatible_AndRejectInvalidTargetsBeforeEditing()
        {
            MeshRenderer renderer = AddCube(_root);
            var targets = new[] { renderer.gameObject };
            Assert.Throws<ArgumentException>(() => FbxPlacementStaticSettings.SetAll(new[] { renderer.gameObject, null }, true));
            Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject), Is.EqualTo((StaticEditorFlags)0));
            FbxPlacementStaticSettings.SetAll(targets, true);
            Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject),
                Is.EqualTo(FbxPlacementStaticSettings.AllFlags & ~StaticEditorFlags.BatchingStatic));
            Assert.That(FbxPlacementBakeData.IsTarget(renderer), Is.True);
            FbxPlacementStaticSettings.SetAll(targets, false);
            Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject), Is.EqualTo((StaticEditorFlags)0));
            Assert.That(renderer.receiveGI, Is.EqualTo(ReceiveGI.LightProbes));
            FbxPlacementStaticSettings.SetFlag(targets, StaticEditorFlags.BatchingStatic, true);
            Assert.That(GameObjectUtility.GetStaticEditorFlags(renderer.gameObject), Is.EqualTo(StaticEditorFlags.BatchingStatic));
        }

        [Test]
        public void PreviewLighting_UsesItsOwnSceneArray_WithoutReplacingNormalSceneMaps()
        {
            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
            LightmapSettings.lightmaps = new[] { new LightmapData { lightmapColor = _otherTexture } };
            MeshRenderer renderer = AddCube(_root);
            Configure(_root, renderer, _texture);
            Assert.That(renderer.lightmapIndex, Is.EqualTo(1));
            FbxPlacementPreviewLighting.Apply(_root);
            FbxPlacementPreviewLighting.Apply(_root);
            Assert.That(LightmapSettings.lightmaps, Has.Length.EqualTo(1));
            Assert.That(LightmapSettings.lightmaps[0].lightmapColor, Is.SameAs(_otherTexture));
            Assert.That(renderer.lightmapIndex, Is.Zero, "가상 씬은 자체 배열의 인덱스를 사용합니다.");
            Assert.That(renderer.lightmapScaleOffset, Is.EqualTo(new Vector4(.5f, .5f, .25f, 0)));
            Assert.That(Unsupported.SetOverrideLightingSettings(_root.scene), Is.True);
            try
            {
                Assert.That(LightmapSettings.lightmaps, Has.Length.EqualTo(1));
                Assert.That(LightmapSettings.lightmaps[0].lightmapColor, Is.SameAs(_texture));
                Assert.That(LightmapSettings.lightmapsMode, Is.EqualTo(LightmapsMode.NonDirectional));
            }
            finally { Unsupported.RestoreOverrideLightingSettings(); }
            Assert.That(LightmapSettings.lightmaps[0].lightmapColor, Is.SameAs(_otherTexture));
        }

        [Test]
        public void Binding_PreservesExistingSceneIndices_SharesTexturesAndReleasesOnlyOwnedEntries()
        {
            LightmapData original = new LightmapData { lightmapColor = _otherTexture };
            LightmapSettings.lightmaps = new[] { original };
            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
            MeshRenderer first = AddCube(_root);
            MapBakedLighting binding = Configure(_root, first, _texture);
            Assert.That(first.lightmapIndex, Is.EqualTo(1));
            Assert.That(first.lightmapScaleOffset, Is.EqualTo(new Vector4(.5f, .5f, .25f, 0)));
            binding.Apply();
            Assert.That(LightmapSettings.lightmaps, Has.Length.EqualTo(2), "같은 맵을 재적용해도 중복 등록하면 안 됩니다.");
            _second = FbxPlacementWorkspace.CreateRoot();
            MeshRenderer second = AddCube(_second);
            MapBakedLighting other = Configure(_second, second, _texture);
            Assert.That(second.lightmapIndex, Is.EqualTo(first.lightmapIndex));
            binding.Release();
            Assert.That(LightmapSettings.lightmaps[1].lightmapColor, Is.SameAs(_texture));
            Assert.That(first.lightmapIndex, Is.EqualTo(-1));
            other.Release();
            Assert.That(LightmapSettings.lightmaps, Has.Length.EqualTo(1));
            Assert.That(LightmapSettings.lightmaps[0].lightmapColor, Is.SameAs(_otherTexture));
        }

        [Test]
        public void Binding_RejectsDirectionalMismatchBeforeChangingSceneMaps()
        {
            LightmapSettings.lightmaps = new[] { new LightmapData { lightmapColor = _otherTexture, lightmapDir = _otherTexture } };
            LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
            MeshRenderer renderer = AddCube(_root);
            var binding = _root.AddComponent<MapBakedLighting>();
            binding.SetData(LightmapsMode.NonDirectional,
                new[] { new MapBakedLighting.Atlas { color = _texture } },
                new[] { new MapBakedLighting.Surface { renderer = renderer, atlasIndex = 0 } },
                Array.Empty<MapBakedLighting.BakedLight>());
            Assert.Throws<InvalidOperationException>(() => binding.Apply());
            Assert.That(LightmapSettings.lightmaps, Has.Length.EqualTo(1));
            Assert.That(LightmapSettings.lightmapsMode, Is.EqualTo(LightmapsMode.CombinedDirectional));
        }

        [Test]
        public void Targets_SetOnlyGiFlags_DisableStaticBatching_AndFingerprintTracksEdits()
        {
            MeshRenderer renderer = AddCube(_root);
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.BatchingStatic);
            FbxPlacementBakeData.SetTargets(_root, true);
            Assert.That(FbxPlacementBakeData.IsTarget(renderer), Is.True);
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
            Assert.That((flags & StaticEditorFlags.OccluderStatic) != 0, Is.True);
            Assert.That((flags & StaticEditorFlags.BatchingStatic) == 0, Is.True);
            string before = FbxPlacementBakeData.Fingerprint(_root);
            renderer.lightmapIndex = 2;
            Assert.That(FbxPlacementBakeData.Fingerprint(_root), Is.EqualTo(before), "씬 전환에 따른 임시 인덱스는 배치 변경이 아닙니다.");
            renderer.transform.position += Vector3.right;
            Assert.That(FbxPlacementBakeData.Fingerprint(_root), Is.Not.EqualTo(before));
            FbxPlacementBakeData.SetTargets(_root, false);
            Assert.That(FbxPlacementBakeData.GetTargets(_root), Is.Empty);
            Assert.That(renderer.receiveGI, Is.EqualTo(ReceiveGI.LightProbes));
        }

        [Test]
        public void Validation_RequiresTargetsUvsAndBakedOrMixedLight()
        {
            Assert.Throws<InvalidOperationException>(() => FbxPlacementBakeData.Validate(_root));
            MeshRenderer renderer = AddCube(_root);
            FbxPlacementBakeData.SetTargets(_root, true);
            Assert.Throws<InvalidOperationException>(() => FbxPlacementBakeData.Validate(_root));
            Light light = FbxPlacementLights.Add(_root, LightType.Directional, Vector3.up * 3);
            Assert.Throws<InvalidOperationException>(() => FbxPlacementBakeData.Validate(_root));
            light.lightmapBakeType = LightmapBakeType.Mixed;
            Assert.DoesNotThrow(() => FbxPlacementBakeData.Validate(_root));
            renderer.gameObject.SetActive(false);
            Assert.Throws<InvalidOperationException>(() => FbxPlacementBakeData.Validate(_root));
        }

        [Test]
        public void Capture_UsesAtlasLocalIndicesAndPreservesShadowmaskLightMetadata()
        {
            MeshRenderer source = AddCube(_root);
            FbxPlacementBakeData.SetTargets(_root, true);
            Light light = FbxPlacementLights.Add(_root, LightType.Directional, Vector3.up);
            _second = FbxPlacementWorkspace.CreateRoot();
            MeshRenderer baked = AddCube(_second);
            FbxPlacementBakeData.SetTargets(_second, true);
            baked.lightmapIndex = 1;
            baked.lightmapScaleOffset = new Vector4(.25f, .5f, .5f, .25f);
            Light bakedLight = FbxPlacementLights.Add(_second, LightType.Directional, Vector3.up);
            bakedLight.bakingOutput = new LightBakingOutput
            {
                isBaked = true, lightmapBakeType = LightmapBakeType.Mixed,
                mixedLightingMode = MixedLightingMode.Shadowmask, occlusionMaskChannel = 2, probeOcclusionLightIndex = 3
            };
            var data = FbxPlacementBakeData.Capture(_root, _second,
                new[] { new LightmapData(), new LightmapData { lightmapColor = _texture, shadowMask = _otherTexture } },
                LightmapsMode.NonDirectional);
            Assert.That(data.atlases, Has.Length.EqualTo(1));
            Assert.That(data.surfaces[0].renderer, Is.SameAs(source));
            Assert.That(data.surfaces[0].atlasIndex, Is.Zero);
            Assert.That(data.surfaces[0].scaleOffset, Is.EqualTo(baked.lightmapScaleOffset));
            LightmapSettings.lightmaps = Array.Empty<LightmapData>();
            data.Apply(_root);
            Assert.That(light.bakingOutput.occlusionMaskChannel, Is.EqualTo(2));
            Assert.That(light.bakingOutput.mixedLightingMode, Is.EqualTo(MixedLightingMode.Shadowmask));
            Assert.That(LightmapSettings.lightmaps[source.lightmapIndex].shadowMask, Is.SameAs(_otherTexture));
        }

        [Test]
        public void Prefab_ReopenRestoresTexturesRendererReferencesAndOffsetsAutomatically()
        {
            LightmapSettings.lightmaps = Array.Empty<LightmapData>();
            MeshRenderer renderer = AddCube(_root);
            _folder = "Assets/Tests/FbxBakeData_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Tests", System.IO.Path.GetFileName(_folder));
            AssetDatabase.CreateAsset(_texture, _folder + "/Lightmap.asset");
            Configure(_root, renderer, _texture);
            GameObject prefab = FbxPlacementWorkspace.SavePrefab(_root, _folder + "/Map.prefab");
            _second = FbxPlacementWorkspace.OpenPrefab(prefab);
            MeshRenderer reopened = _second.GetComponentInChildren<MeshRenderer>();
            Assert.That(reopened, Is.Not.SameAs(renderer));
            Assert.That(reopened.lightmapScaleOffset, Is.EqualTo(renderer.lightmapScaleOffset));
            Assert.That(AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[reopened.lightmapIndex].lightmapColor),
                Is.EqualTo(_folder + "/Lightmap.asset"));
            Assert.That(_second.GetComponent<MapBakedLighting>().SurfaceCount, Is.EqualTo(1));
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            MeshRenderer instantiated = _instance.GetComponentInChildren<MeshRenderer>();
            Assert.That(instantiated.lightmapIndex, Is.GreaterThanOrEqualTo(0), "일반 씬에 생성해도 자동 연결해야 합니다.");
            Assert.That(AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[instantiated.lightmapIndex].lightmapColor),
                Is.EqualTo(_folder + "/Lightmap.asset"));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Baker_RealBakeSavesPrefabAndRestoresOriginalScenes()
        {
            _folder = "Assets/Tests/FbxBakeIntegration_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Tests", System.IO.Path.GetFileName(_folder));
            // EditMode Runner가 만든 기본 씬만 테스트 폴더에 저장합니다.
            Scene active = SceneManager.GetActiveScene();
            GameObject[] defaultRoots = active.GetRootGameObjects();
            if (SceneManager.sceneCount == 1 && string.IsNullOrEmpty(active.path) && defaultRoots.Length == 2 &&
                Array.Exists(defaultRoots, item => item.name == "Main Camera" && item.GetComponent<Camera>() != null) &&
                Array.Exists(defaultRoots, item => item.name == "Directional Light" && item.GetComponent<Light>() != null))
                Assert.That(EditorSceneManager.SaveScene(active, _folder + "/TestBootstrap.unity"), Is.True);
            // 사용자에게 저장창을 띄우거나 저장 안 된 작업을 버리는 테스트는 실행하지 않습니다.
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    Assert.Ignore("저장되지 않은 일반 씬이 있어 실제 씬 전환 베이크 검증은 건너뜁니다.");
            if (Lightmapping.isRunning) Assert.Ignore("사용자의 베이크가 진행 중입니다.");
            var originalScenes = EditorSceneManager.GetSceneManagerSetup();
            MeshRenderer renderer = AddCube(_root);
            FbxPlacementBakeData.SetTargets(_root, true);
            Light light = FbxPlacementLights.Add(_root, LightType.Directional, Vector3.up * 3);
            light.lightmapBakeType = LightmapBakeType.Baked;
            string path = _folder + "/Map.prefab";
            FbxPlacementWorkspace.SavePrefab(_root, path);
            var settings = new FbxPlacementBakeSettings
            {
                resolution = 2, atlasSize = 256, directSamples = 8, indirectSamples = 8, environmentSamples = 8,
                lightmapper = LightingSettings.Lightmapper.ProgressiveGPU, directionalMode = LightmapsMode.CombinedDirectional
            };
            bool done = false, success = false;
            string message = "";
            Assert.That(FbxPlacementBaker.Start(_root, path, settings, (ok, text) => { success = ok; message = text; done = true; }), Is.True);
            double deadline = EditorApplication.timeSinceStartup + 150;
            while (!done && EditorApplication.timeSinceStartup < deadline) yield return null;
            if (!done) FbxPlacementBaker.Current?.Cancel();
            Assert.That(done && success, Is.True, message);
            Assert.That(FbxPlacementBaker.IsRunning, Is.False);
            Assert.That(EditorSceneManager.GetSceneManagerSetup().Length, Is.EqualTo(originalScenes.Length));
            for (int i = 0; i < originalScenes.Length; i++)
                Assert.That(EditorSceneManager.GetSceneManagerSetup()[i].path, Is.EqualTo(originalScenes[i].path));
            Assert.That(_root.GetComponent<MapBakedLighting>().AtlasCount, Is.GreaterThan(0));
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab.GetComponent<MapBakedLighting>(), Is.Null);
            MapDefinitionSO map = AssetDatabase.LoadAssetAtPath<MapDefinitionSO>(FbxPlacementMapAssets.DefinitionPath(path));
            Assert.That(map.BakedLighting.SurfaceCount, Is.EqualTo(1));
            _second = FbxPlacementMapAssets.Open(map);
            Assert.That(_second.GetComponentInChildren<MeshRenderer>().lightmapIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(renderer.lightmapScaleOffset.x, Is.GreaterThan(0));
            string firstMap = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapColor);
            done = false;
            success = true;
            Assert.That(FbxPlacementBaker.Start(_root, path, settings, (ok, text) => { success = ok; message = text; done = true; }), Is.True);
            FbxPlacementBaker.Current.Cancel();
            deadline = EditorApplication.timeSinceStartup + 10;
            while (!done && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.That(done, Is.True, "취소 후 씬 복원 콜백이 필요합니다.");
            Assert.That(success, Is.False);
            Assert.That(FbxPlacementBaker.IsRunning, Is.False);
            Assert.That(AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[renderer.lightmapIndex].lightmapColor), Is.EqualTo(firstMap));
            Assert.That(EditorSceneManager.GetSceneManagerSetup()[0].path, Is.EqualTo(originalScenes[0].path));
        }
    }
}
