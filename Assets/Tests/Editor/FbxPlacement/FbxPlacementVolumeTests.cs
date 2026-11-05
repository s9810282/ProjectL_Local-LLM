using System;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.PostFX;
using NUnit.Framework;
using ProjectL.Tool.Editor;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectL.FbxPlacement.Editor.Tests
{
    public sealed class FbxPlacementVolumeTests
    {
        private GameObject _root;
        private GameObject _second;
        private readonly List<VolumeProfile> _profiles = new List<VolumeProfile>();
        private string _folder;

        [SetUp]
        public void SetUp() => _root = FbxPlacementWorkspace.CreateRoot();

        [TearDown]
        public void TearDown()
        {
            FbxPlacementWorkspace.Close(_second);
            FbxPlacementWorkspace.Close(_root);
            foreach (VolumeProfile profile in _profiles) FbxPlacementVolumes.DestroyProfile(profile);
            _profiles.Clear();
            if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
        }

        private Volume Add(GameObject root, Transform layer = null)
        {
            Volume volume = FbxPlacementVolumes.Add(root, layer);
            _profiles.Add(volume.sharedProfile);
            return volume;
        }

        [Test]
        public void Add_UsesChosenLayer_AndUndoRedoKeepsProfile()
        {
            Scene active = SceneManager.GetActiveScene();
            int roots = active.rootCount;
            Transform layer = FbxPlacementLayers.Create(_root, "무드");
            Volume volume = Add(_root, layer);
            Assert.That(volume.isGlobal, Is.True);
            Assert.That(volume.transform.parent, Is.SameAs(layer));
            Assert.That(volume.gameObject.scene, Is.EqualTo(_root.scene));
            Assert.That(volume.sharedProfile.components, Has.Count.EqualTo(5));
            VolumeProfile profile = volume.sharedProfile;
            Undo.PerformUndo();
            Assert.That(_root.GetComponentsInChildren<Volume>(), Is.Empty);
            Undo.PerformRedo();
            Assert.That(_root.GetComponentInChildren<Volume>().sharedProfile, Is.SameAs(profile));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.rootCount, Is.EqualTo(roots));
        }

        [Test]
        public void ProfileCopy_DoesNotShareComponentsOrParametersWithSource()
        {
            VolumeProfile source = Add(_root).sharedProfile;
            source.TryGet(out ColorAdjustments original);
            original.postExposure.Override(-1);
            VolumeProfile copy = FbxPlacementVolumes.CopyProfile(source);
            _profiles.Add(copy);
            copy.TryGet(out ColorAdjustments edited);
            edited.postExposure.Override(2);
            Assert.That(edited, Is.Not.SameAs(original));
            Assert.That(edited.postExposure, Is.Not.SameAs(original.postExposure));
            Assert.That(original.postExposure.value, Is.EqualTo(-1));
        }

        [Test]
        public void Import_CopiesExistingVolumeIntoWorkspaceWithoutChangingSourceProfile()
        {
            GameObject sourceObject = new GameObject("기존 Global Volume", typeof(Volume));
            Volume source = sourceObject.GetComponent<Volume>();
            VolumeProfile sourceProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            sourceProfile.Add<ColorAdjustments>().postExposure.Override(-2f);
            source.isGlobal = true;
            source.priority = 7;
            source.weight = .65f;
            source.sharedProfile = sourceProfile;
            try
            {
                Volume imported = FbxPlacementVolumes.Import(_root, source);
                _profiles.Add(imported.sharedProfile);

                Assert.That(imported.gameObject.scene, Is.EqualTo(_root.scene));
                Assert.That(imported.isGlobal, Is.True);
                Assert.That(imported.priority, Is.EqualTo(7));
                Assert.That(imported.weight, Is.EqualTo(.65f));
                Assert.That(imported.sharedProfile, Is.Not.SameAs(sourceProfile));
                imported.sharedProfile.TryGet(out ColorAdjustments copied);
                copied.postExposure.Override(3f);
                sourceProfile.TryGet(out ColorAdjustments original);
                Assert.That(original.postExposure.value, Is.EqualTo(-2f));
            }
            finally
            {
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(sourceProfile);
            }
        }

        [Test]
        public void MapSave_PersistsProfileAndOverrides_AndRuntimeAssemblyKeepsThem()
        {
            Volume volume = Add(_root);
            volume.priority = 12;
            volume.weight = .6f;
            volume.sharedProfile.TryGet(out ColorAdjustments colors);
            colors.postExposure.Override(-1.25f);
            volume.sharedProfile.TryGet(out Tonemapping tone);
            tone.mode.Override(TonemappingMode.ACES);
            _folder = "Assets/Tests/FbxVolume_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Tests", System.IO.Path.GetFileName(_folder));
            MapDefinitionSO map = FbxPlacementMapAssets.Save(_root, _folder + "/Map.prefab");
            Volume saved = map.Prefab.GetComponentInChildren<Volume>();
            Assert.That(EditorUtility.IsPersistent(saved.sharedProfile), Is.True);
            Assert.That(EditorUtility.IsPersistent(volume.sharedProfile), Is.False, "작업용 프로필은 별도로 유지합니다.");
            foreach (VolumeComponent component in saved.sharedProfile.components)
                Assert.That(AssetDatabase.GetAssetPath(component), Is.EqualTo(AssetDatabase.GetAssetPath(saved.sharedProfile)));
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(saved.sharedProfile), ImportAssetOptions.ForceUpdate);
            _second = FbxPlacementMapAssets.Open(map);
            Volume reopened = _second.GetComponentInChildren<Volume>();
            reopened.sharedProfile.TryGet(out ColorAdjustments restored);
            Assert.That(restored.postExposure.overrideState, Is.True);
            Assert.That(restored.postExposure.value, Is.EqualTo(-1.25f));
            GameObject instance = MapAssembler.Instantiate(map, _root.transform);
            Volume assembled = instance.GetComponentInChildren<Volume>();
            assembled.sharedProfile.TryGet(out Tonemapping restoredTone);
            Assert.That(restoredTone.mode.value, Is.EqualTo(TonemappingMode.ACES));
            Assert.That(assembled.weight, Is.EqualTo(.6f));
            Assert.That(assembled.priority, Is.EqualTo(12));
        }

        [Test]
        public void MapSave_PersistsVirtualCameraEmbeddedVolumeProfileWithoutVolumeGameObject()
        {
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profiles.Add(profile);
            profile.Add<ColorAdjustments>();
            profile.TryGet(out ColorAdjustments colors);
            colors.postExposure.Override(-.75f);
            CinemachineVirtualCamera camera = FbxPlacementCameras.Add(_root, new Vector3(2, 5, -4), Quaternion.Euler(35, 15, 0));
            CinemachineVolumeSettings settings = camera.GetComponent<CinemachineVolumeSettings>();
            settings.m_Profile = profile;
            _folder = "Assets/Tests/FbxCameraVolume_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Tests", System.IO.Path.GetFileName(_folder));

            MapDefinitionSO map = FbxPlacementMapAssets.Save(_root, _folder + "/Map.prefab");
            CinemachineVolumeSettings savedSettings = map.Prefab.GetComponentInChildren<CinemachineVolumeSettings>();

            Assert.That(savedSettings, Is.Not.Null);
            Assert.That(EditorUtility.IsPersistent(savedSettings.m_Profile), Is.True);
            Assert.That(map.Prefab.GetComponentsInChildren<Volume>(true), Is.Empty);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(map.Prefab), ImportAssetOptions.ForceUpdate);
            _second = FbxPlacementMapAssets.Open(map);
            CinemachineVolumeSettings reopenedSettings = _second.GetComponentInChildren<CinemachineVolumeSettings>();
            Assert.That(reopenedSettings.m_Profile, Is.SameAs(savedSettings.m_Profile));
        }

        [Test]
        public void VolumeEvaluation_FiltersOtherScenesByWorkspaceCamera()
        {
            if (!VolumeManager.instance.isInitialized) Assert.Ignore("URP 볼륨 시스템 초기화 후 실행합니다.");
            Volume volume = Add(_root);
            volume.sharedProfile.TryGet(out ColorAdjustments own);
            own.postExposure.Override(-1.5f);
            _second = FbxPlacementWorkspace.CreateRoot();
            Volume foreign = Add(_second);
            foreign.priority = 10000;
            foreign.sharedProfile.TryGet(out ColorAdjustments other);
            other.postExposure.Override(7);
            GameObject cameraObject = EditorUtility.CreateGameObjectWithHideFlags("볼륨 검증 카메라", HideFlags.HideAndDontSave, typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, _root.scene);
            cameraObject.transform.SetParent(_root.transform, false);
            cameraObject.GetComponent<Camera>().scene = _root.scene;
            VolumeStack stack = VolumeManager.instance.CreateStack();
            try
            {
                VolumeManager.instance.Update(stack, cameraObject.transform, ~0);
                Assert.That(stack.GetComponent<ColorAdjustments>().postExposure.value, Is.EqualTo(-1.5f));
            }
            finally { VolumeManager.instance.DestroyStack(stack); }
        }
    }
}
