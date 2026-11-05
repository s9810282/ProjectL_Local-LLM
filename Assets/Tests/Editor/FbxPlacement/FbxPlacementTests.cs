using NUnit.Framework;
using Cinemachine;
using Cinemachine.PostFX;
using PlacementTool = ProjectL.Tool.Editor.FbxPlacement;
using Workspace = ProjectL.Tool.Editor.FbxPlacementWorkspace;
using Surface = ProjectL.Tool.Editor.FbxPlacementSurface;
using Layers = ProjectL.Tool.Editor.FbxPlacementLayers;
using BoxSelection = ProjectL.Tool.Editor.FbxPlacementSelection;
using MapLights = ProjectL.Tool.Editor.FbxPlacementLights;
using MapCameras = ProjectL.Tool.Editor.FbxPlacementCameras;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ProjectL.FbxPlacement.Editor.Tests
{
    public sealed class FbxPlacementTests
    {
        private const string ModelPath = "Assets/ProjectL/Resources/Map/Environment/BackgroundSceneTest/Models/LAND17.fbx";
        private Scene _testScene;
        private int _initialRootCount;
        private GameObject _model;
        private Transform _parent;
        private GameObject _workspace;
        private GameObject _reopened;
        private string _assetFolder;
        private Mesh _surfaceMesh;

        [SetUp]
        public void SetUp()
        {
            _model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Assert.That(_model, Is.Not.Null, "검증에 사용하는 FBX가 없습니다.");
            // Test Runner가 준비한 씬에서 검증 전용 부모 아래에만 생성합니다.
            _testScene = SceneManager.GetActiveScene();
            _initialRootCount = _testScene.rootCount;
            _parent = new GameObject("FBX 배치 검증 전용 부모").transform;
            _parent.SetPositionAndRotation(new Vector3(3, 4, 5), Quaternion.Euler(0, 35, 0));
            _parent.localScale = new Vector3(2, 3, 4);
        }

        [TearDown]
        public void TearDown()
        {
            Workspace.Close(_reopened);
            Workspace.Close(_workspace);
            if (!string.IsNullOrEmpty(_assetFolder)) AssetDatabase.DeleteAsset(_assetFolder);
            if (_parent != null)
                Object.DestroyImmediate(_parent.gameObject);
            if (_surfaceMesh != null) Object.DestroyImmediate(_surfaceMesh);
        }

        [Test]
        public void Lights_CreateInChosenLayer_UndoRedoAndIsolation()
        {
            _workspace = Workspace.CreateRoot();
            _workspace.transform.position = new Vector3(4, 0, 2);
            Transform layer = Layers.Create(_workspace, "조명");
            layer.localPosition = Vector3.right * 7;
            Vector3 position = new Vector3(1, 4, 3);
            Light light = MapLights.Add(_workspace, LightType.Spot, position, layer);
            Assert.That(light.gameObject.scene, Is.EqualTo(_workspace.scene));
            Assert.That(light.transform.parent, Is.EqualTo(layer));
            Assert.That(Vector3.Distance(light.transform.position, _workspace.transform.TransformPoint(position)), Is.LessThan(.001f));
            Assert.That(light.lightmapBakeType, Is.EqualTo(LightmapBakeType.Realtime));
            Assert.That(light.cullingMask, Is.EqualTo(~0));
            Assert.That(MapLights.GetLights(_workspace), Has.Length.EqualTo(1));
            Assert.That(_testScene.rootCount, Is.EqualTo(_initialRootCount + 1));
            Undo.PerformUndo();
            Assert.That(MapLights.GetLights(_workspace), Is.Empty);
            Undo.PerformRedo();
            Assert.That(MapLights.GetLights(_workspace), Has.Length.EqualTo(1));
            Assert.That(layer.childCount, Is.EqualTo(1));
        }

        [Test]
        public void CameraPreview_ShowsSelectedCameraRendersWorkspaceAndRestoresRenderTarget()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("카메라 미리보기 검증에는 그래픽 장치가 필요합니다.");

            ProjectL.Tool.Editor.FbxPlacementWindow window = null;
            Material material = null;
            RenderTexture previousTarget = RenderTexture.active;
            RenderTexture sentinel = new RenderTexture(16, 16, 0);
            Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                window = ScriptableObject.CreateInstance<ProjectL.Tool.Editor.FbxPlacementWindow>();
                window.Show();
                var serialized = new SerializedObject(window);
                GameObject root = (GameObject)serialized.FindProperty("workspaceRoot").objectReferenceValue;
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                SceneManager.MoveGameObjectToScene(cube, root.scene);
                cube.transform.SetParent(root.transform, false);
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                Assert.That(shader, Is.Not.Null);
                material = new Material(shader);
                material.SetColor("_BaseColor", Color.magenta);
                cube.GetComponent<Renderer>().sharedMaterial = material;
                CinemachineVirtualCamera camera = MapCameras.Add(root, new Vector3(0, 0, -5), Quaternion.identity);

                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                System.Type type = typeof(ProjectL.Tool.Editor.FbxPlacementWindow);
                type.GetMethod("BuildSidebar", flags).Invoke(window, null);
                type.GetMethod("Select", flags).Invoke(window, new object[] { camera.transform });
                sentinel.Create();
                RenderTexture.active = sentinel;
                type.GetMethod("UpdateVirtualCameraPreview", flags).Invoke(window, null);

                Assert.That(RenderTexture.active, Is.SameAs(sentinel), "렌더 뒤 기존 출력 타깃을 복원해야 합니다.");
                VisualElement panel = window.rootVisualElement.Q("fbx-virtual-camera-preview");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                RenderTexture texture = panel.Q<Image>().image as RenderTexture;
                Assert.That(texture, Is.Not.Null, "선택한 카메라의 화면이 패널에 연결되어야 합니다.");
                RenderTexture.active = texture;
                pixel.ReadPixels(new Rect(texture.width / 2, texture.height / 2, 1, 1), 0, 0);
                pixel.Apply();
                Color color = pixel.GetPixel(0, 0);
                Assert.That(color.r, Is.GreaterThan(.6f), "가상 씬의 자홍색 큐브가 화면 중앙에 보여야 합니다.");
                Assert.That(color.b, Is.GreaterThan(.6f));
                Assert.That(color.g, Is.LessThan(.4f));
                Assert.That(root.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true), Is.Empty);
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (window != null)
                {
                    typeof(EditorWindow).GetProperty(nameof(EditorWindow.hasUnsavedChanges)).SetValue(window, false);
                    window.Close();
                }
                if (material != null) Object.DestroyImmediate(material);
                Object.DestroyImmediate(pixel);
                Object.DestroyImmediate(sentinel);
            }
        }

        [Test]
        public void Cameras_CreateInWorkspaceAndPrefabRoundtripPreservesCinemachineSettings()
        {
            _workspace = Workspace.CreateRoot();
            _workspace.transform.position = new Vector3(4, 0, 2);
            CinemachineVirtualCamera camera = MapCameras.Add(_workspace, new Vector3(1, 5, -3), Quaternion.Euler(40, 25, 0));
            LensSettings lens = camera.m_Lens;
            lens.Orthographic = false;
            lens.FieldOfView = 52;
            camera.m_Lens = lens;
            camera.Priority = 23;

            Assert.That(camera.transform.parent, Is.EqualTo(_workspace.transform));
            Assert.That(camera.transform.position, Is.EqualTo(_workspace.transform.TransformPoint(new Vector3(1, 5, -3))));
            Assert.That(camera.GetComponent<CinemachineImpulseListener>().m_ChannelMask, Is.EqualTo(1));
            Assert.That(camera.GetComponent<CinemachineVolumeSettings>(), Is.Not.Null);
            Assert.That(MapCameras.GetCameras(_workspace), Has.Length.EqualTo(1));

            _assetFolder = "Assets/Tests/FbxCameraRoundtrip_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Tests", System.IO.Path.GetFileName(_assetFolder));
            GameObject prefab = Workspace.SavePrefab(_workspace, _assetFolder + "/Map.prefab");
            _reopened = Workspace.OpenPrefab(prefab);
            CinemachineVirtualCamera[] cameras = MapCameras.GetCameras(_reopened);

            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].m_Lens.Orthographic, Is.False);
            Assert.That(cameras[0].m_Lens.FieldOfView, Is.EqualTo(52));
            Assert.That(cameras[0].Priority, Is.EqualTo(23));
            Assert.That(cameras[0].GetComponent<CinemachineImpulseListener>().m_ChannelMask, Is.EqualTo(1));
        }

        [TestCase(LightType.Directional)]
        [TestCase(LightType.Point)]
        [TestCase(LightType.Spot)]
        [TestCase(LightType.Rectangle)]
        [TestCase(LightType.Disc)]
        public void Lights_PrefabRoundtripPreservesUnityComponentsAndSettings(LightType type)
        {
            _workspace = Workspace.CreateRoot();
            Light light = MapLights.Add(_workspace, type, new Vector3(1, 3, 2));
            light.color = Color.red;
            light.intensity = 2.5f;
            light.range = 12;
            light.enabled = false;
            bool area = MapLights.IsArea(type);
            Assert.That(light.lightmapBakeType, Is.EqualTo(area ? LightmapBakeType.Baked : LightmapBakeType.Realtime));
            _assetFolder = "Assets/Tests/FbxLightRoundtrip_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets/Tests", System.IO.Path.GetFileName(_assetFolder));
            GameObject prefab = Workspace.SavePrefab(_workspace, _assetFolder + "/Map.prefab");
            _reopened = Workspace.OpenPrefab(prefab);
            Light[] lights = MapLights.GetLights(_reopened);
            Assert.That(lights, Has.Length.EqualTo(1));
            Assert.That(_reopened.transform.childCount, Is.EqualTo(1), "편집용 카메라나 조명을 저장하면 안 됩니다.");
            Assert.That(lights[0].type, Is.EqualTo(type));
            Assert.That(lights[0].color, Is.EqualTo(Color.red));
            Assert.That(lights[0].intensity, Is.EqualTo(2.5f));
            Assert.That(lights[0].range, Is.EqualTo(12));
            Assert.That(lights[0].enabled, Is.False);
            Assert.That(lights[0].transform.localPosition, Is.EqualTo(new Vector3(1, 3, 2)));
        }

        [Test]
        public void Lights_RejectForeignRootLayerAndInvalidInputBeforeCreating()
        {
            _workspace = Workspace.CreateRoot();
            Assert.Throws<System.ArgumentException>(() => MapLights.Add(_parent.gameObject, LightType.Point, Vector3.zero));
            Assert.Throws<System.ArgumentException>(() => MapLights.Add(_workspace, LightType.Point, Vector3.zero, _parent));
            Assert.Throws<System.ArgumentException>(() => MapLights.Add(_workspace, (LightType)999, Vector3.zero));
            Assert.Throws<System.ArgumentException>(() => MapLights.Add(_workspace, LightType.Point, new Vector3(float.NaN, 0, 0)));
            Assert.That(_workspace.transform.childCount, Is.Zero);
        }

        [Test]
        public void Lights_RectangleSelectionUsesLightPositionAndSkipsInactive()
        {
            _workspace = Workspace.CreateRoot();
            Camera camera = CreateSelectionCamera(true);
            Light left = MapLights.Add(_workspace, LightType.Point, Vector3.left * 2);
            left.enabled = false;
            MapLights.Add(_workspace, LightType.Directional, Vector3.right * 2);
            Light inactive = MapLights.Add(_workspace, LightType.Spot, Vector3.left * 2 + Vector3.up);
            inactive.gameObject.SetActive(false);
            CollectionAssert.AreEquivalent(new[] { left.transform },
                BoxSelection.FindInRect(_workspace, camera, new Rect(.05f, .3f, .4f, .4f)));
        }

        [Test]
        public void Selection_RectangleUsesCameraVolumeAndSkipsHiddenAndClippedObjects()
        {
            _workspace = Workspace.CreateRoot();
            Camera camera = CreateSelectionCamera(true);
            GameObject left = CreateSurfaceCube(new Vector3(-2, 0, 0));
            CreateSurfaceCube(new Vector3(2, 0, 0));
            CreateSurfaceCube(new Vector3(-2, 0, -20));
            CreateSurfaceCube(new Vector3(-2, 0, 150));
            GameObject hidden = CreateSurfaceCube(new Vector3(-2, 1, 0));
            hidden.GetComponent<Renderer>().enabled = false;
            GameObject inactive = CreateSurfaceCube(new Vector3(-2, -1, 0));
            inactive.SetActive(false);
            var hits = BoxSelection.FindInRect(_workspace, camera, new Rect(.05f, .3f, .4f, .4f));
            CollectionAssert.AreEquivalent(new[] { left.transform }, hits);
            CollectionAssert.AreEquivalent(hits,
                BoxSelection.FindInRect(_workspace, camera, new Rect(.45f, .7f, -.4f, -.4f)),
                "반대 방향으로 드래그해도 같은 영역을 선택해야 합니다.");
            Assert.That(BoxSelection.FindInRect(_workspace, camera, new Rect(0, 0, 0, 1)), Is.Empty);
        }

        [Test]
        public void Selection_PerspectiveFindsWholePlacedRootOnceAcrossLayerAndMeshes()
        {
            _workspace = Workspace.CreateRoot();
            Camera camera = CreateSelectionCamera(false);
            Transform layer = Layers.Create(_workspace, "프랍");
            GameObject placed = new GameObject("여러 메시 FBX");
            SceneManager.MoveGameObjectToScene(placed, _workspace.scene);
            placed.transform.SetParent(layer, false);
            GameObject first = CreateSurfaceCube(new Vector3(-2, 0, 0));
            first.transform.SetParent(placed.transform, true);
            GameObject second = CreateSurfaceCube(new Vector3(-2, 1, 0));
            second.transform.SetParent(placed.transform, true);
            CreateSurfaceCube(new Vector3(2, 0, 0));
            var hits = BoxSelection.FindInRect(_workspace, camera, new Rect(.15f, .3f, .3f, .4f));
            CollectionAssert.AreEqual(new[] { placed.transform }, hits);
            Assert.That(BoxSelection.GetPlacedObject(_workspace, first.transform), Is.EqualTo(placed.transform));
            Assert.That(BoxSelection.GetPlacedObject(_workspace, layer), Is.Null);
            Assert.That(BoxSelection.GetPlacedObject(_workspace, _parent), Is.Null);
        }

        [Test]
        public void Selection_CombinesAddedToggledAndReplacedObjectsWithoutForeignSelection()
        {
            _workspace = Workspace.CreateRoot();
            GameObject first = CreateSurfaceCube(Vector3.left);
            GameObject second = CreateSurfaceCube(Vector3.right);
            Transform[] previous = { first.transform, _parent, _workspace.transform };
            Transform[] hits = { first.transform, first.transform, second.transform };
            CollectionAssert.AreEquivalent(new[] { first.transform, second.transform },
                BoxSelection.Combine(_workspace, previous, hits, BoxSelection.Mode.Add));
            CollectionAssert.AreEqual(new[] { second.transform },
                BoxSelection.Combine(_workspace, previous, hits, BoxSelection.Mode.Toggle));
            Assert.That(BoxSelection.Combine(_workspace, previous, hits, BoxSelection.Mode.Remove), Is.Empty);
            Assert.That(BoxSelection.Combine(_workspace, previous, new Transform[0], BoxSelection.Mode.Replace), Is.Empty);
        }

        [Test]
        public void Selection_RectangleCanIncludeObjectCrossingNearClipPlane()
        {
            _workspace = Workspace.CreateRoot();
            Camera camera = CreateSelectionCamera(true);
            GameObject crossing = CreateSurfaceCube(new Vector3(0, 0, -9.8f));
            CollectionAssert.AreEqual(new[] { crossing.transform },
                BoxSelection.FindInRect(_workspace, camera, new Rect(0, 0, 1, 1)));
        }

        private Camera CreateSelectionCamera(bool orthographic)
        {
            GameObject go = new GameObject("선택 검증 카메라", typeof(Camera));
            SceneManager.MoveGameObjectToScene(go, _workspace.scene);
            go.transform.position = new Vector3(0, 0, -10);
            Camera camera = go.GetComponent<Camera>();
            camera.orthographic = orthographic;
            camera.orthographicSize = 5;
            camera.aspect = 1;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100;
            return camera;
        }

        [Test]
        public void Layers_NewFbxAndSurfacePlacementUseChosenParent()
        {
            _workspace = Workspace.CreateRoot();
            Transform layer = Layers.Create(_workspace, "프랍");
            layer.SetPositionAndRotation(new Vector3(3, 4, 5), Quaternion.Euler(0, 35, 0));
            layer.localScale = Vector3.one * 2;
            Vector3 position = new Vector3(2, 3, 4);
            GameObject placed = Workspace.AddFbx(_workspace, _model, position, layer);
            Assert.That(placed.transform.parent, Is.EqualTo(layer));
            Assert.That(Vector3.Distance(placed.transform.position, _workspace.transform.TransformPoint(position)), Is.LessThan(.001f));
            GameObject seated = Workspace.AddFbxOnSurface(_workspace, _model, new Vector3(2, 6, 4), Vector3.up, false, .1f, layer);
            Assert.That(seated.transform.parent, Is.EqualTo(layer));
            float bottom = float.PositiveInfinity;
            foreach (Renderer renderer in seated.GetComponentsInChildren<Renderer>()) bottom = Mathf.Min(bottom, renderer.bounds.min.y);
            Assert.That(bottom, Is.EqualTo(6.1f).Within(.001f));
        }

        [Test]
        public void Layers_SurfaceQueryFiltersGroupsAndKeepsDefaultSeparate()
        {
            _workspace = Workspace.CreateRoot();
            Transform ground = Layers.Create(_workspace, "바닥");
            Transform props = Layers.Create(_workspace, "프랍");
            GameObject low = CreateSurfaceCube(Vector3.zero);
            low.transform.SetParent(ground, true);
            GameObject high = CreateSurfaceCube(new Vector3(0, 3, 0));
            high.transform.SetParent(props, true);
            Ray ray = new Ray(new Vector3(0, 10, 0), Vector3.down);
            Assert.That(Surface.TryRaycast(_workspace, ray, out RaycastHit hit), Is.True);
            Assert.That(hit.point.y, Is.EqualTo(3.5f).Within(.001f));
            Assert.That(Surface.TryRaycast(_workspace, ray, out hit, layer: ground), Is.True);
            Assert.That(hit.point.y, Is.EqualTo(.5f).Within(.001f));
            Assert.That(Surface.TryRaycast(_workspace, ray, out _, layer: _workspace.transform), Is.False);
            GameObject defaultCube = CreateSurfaceCube(new Vector3(0, 6, 0));
            Assert.That(Surface.TryRaycast(_workspace, ray, out hit, layer: _workspace.transform), Is.True);
            Assert.That(hit.point.y, Is.EqualTo(6.5f).Within(.001f));
            high.SetActive(false);
            Assert.That(Surface.TryRaycast(_workspace, ray, out _, layer: props), Is.False);
            Assert.That(Layers.GetContainingLayer(_workspace, defaultCube.transform), Is.EqualTo(_workspace.transform));
        }

        [Test]
        public void Layers_MoveMeshChildMovesWholeFbxWithUndoAndWorldPosePreserved()
        {
            _workspace = Workspace.CreateRoot();
            Transform first = Layers.Create(_workspace, "첫 레이어");
            Transform second = Layers.Create(_workspace, "둘째 레이어");
            second.SetPositionAndRotation(new Vector3(5, 2, 8), Quaternion.Euler(0, 45, 0));
            second.localScale = Vector3.one * 2;
            GameObject placed = Workspace.AddFbx(_workspace, _model, new Vector3(2, 3, 4), first);
            Vector3 position = placed.transform.position;
            Quaternion rotation = placed.transform.rotation;
            Vector3 scale = placed.transform.lossyScale;
            Undo.IncrementCurrentGroup();
            Transform moved = Layers.MoveObject(_workspace, placed.GetComponentInChildren<MeshFilter>().transform, second);
            Undo.FlushUndoRecordObjects();
            Undo.IncrementCurrentGroup();
            Assert.That(moved, Is.EqualTo(placed.transform));
            Assert.That(moved.parent, Is.EqualTo(second));
            Assert.That(Vector3.Distance(moved.position, position), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(moved.rotation, rotation), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(moved.lossyScale, scale), Is.LessThan(.001f));
            Undo.PerformUndo();
            Assert.That(placed.transform.parent, Is.EqualTo(first));
            Undo.PerformRedo();
            Assert.That(placed.transform.parent, Is.EqualTo(second));
        }

        [Test]
        public void Layers_PrefabRoundTripKeepsGroupsAndMembership()
        {
            string folderName = "__FbxPlacementTest_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _assetFolder = "Assets/" + folderName;
            _workspace = Workspace.CreateRoot();
            Transform layer = Layers.Create(_workspace, "배경");
            Layers.Create(_workspace, "빈 레이어");
            Workspace.AddFbx(_workspace, _model, new Vector3(2, 3, 4), layer);
            GameObject prefab = Workspace.SavePrefab(_workspace, _assetFolder + "/Map.prefab");
            _reopened = Workspace.OpenPrefab(prefab);
            var restored = Layers.GetLayers(_reopened);
            Assert.That(restored.Count, Is.EqualTo(2));
            Assert.That(Layers.GetName(restored[0]), Is.EqualTo("배경"));
            Assert.That(restored[0].childCount, Is.EqualTo(1));
            Assert.That(restored[1].childCount, Is.Zero);
            Assert.That(restored[0].GetChild(0).localPosition, Is.EqualTo(new Vector3(2, 3, 4)));
        }

        [Test]
        public void Layers_RejectDuplicateNamesForeignParentsAndMovingGroups()
        {
            _workspace = Workspace.CreateRoot();
            Transform layer = Layers.Create(_workspace, "바닥");
            Assert.Throws<System.ArgumentException>(() => Layers.Create(_workspace, " 바닥 "));
            Assert.Throws<System.ArgumentException>(() => Layers.Create(_workspace, " "));
            Assert.Throws<System.ArgumentException>(() => Workspace.AddFbx(_workspace, _model, Vector3.zero, _parent));
            Assert.Throws<System.ArgumentException>(() => Layers.MoveObject(_workspace, layer, null));
            Layers.Rename(_workspace, layer, "지형");
            Assert.That(Layers.GetName(layer), Is.EqualTo("지형"));
            Assert.That(_workspace.transform.childCount, Is.EqualTo(1));
            Assert.That(layer.childCount, Is.Zero);
        }

        [Test]
        public void Surface_HitsActualTriangleWithoutColliderOrReadableMesh()
        {
            _workspace = Workspace.CreateRoot();
            _surfaceMesh = new Mesh();
            _surfaceMesh.vertices = new[] { new Vector3(-1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1) };
            _surfaceMesh.triangles = new[] { 0, 1, 2 };
            _surfaceMesh.RecalculateNormals();
            _surfaceMesh.RecalculateBounds();
            _surfaceMesh.UploadMeshData(true);
            GameObject surface = new GameObject("표면", typeof(MeshFilter), typeof(MeshRenderer));
            SceneManager.MoveGameObjectToScene(surface, _workspace.scene);
            surface.transform.SetParent(_workspace.transform, false);
            surface.GetComponent<MeshFilter>().sharedMesh = _surfaceMesh;
            surface.transform.SetPositionAndRotation(new Vector3(3, 2, 4), Quaternion.Euler(25, 30, 10));
            Vector3 normal = surface.transform.up;
            Vector3 point = surface.transform.TransformPoint(new Vector3(-.5f, 0, -.5f));
            Assert.That(_surfaceMesh.isReadable, Is.False);
            Assert.That(surface.GetComponents<Collider>(), Is.Empty);
            Assert.That(Surface.TryRaycast(_workspace, new Ray(point + normal * 5, -normal), out RaycastHit hit), Is.True);
            Assert.That(Vector3.Distance(hit.point, point), Is.LessThan(.001f));
            Assert.That(Vector3.Angle(hit.normal, normal), Is.LessThan(.01f));
            Vector3 empty = surface.transform.TransformPoint(new Vector3(.75f, 0, .75f));
            Assert.That(Surface.TryRaycast(_workspace, new Ray(empty + normal * 5, -normal), out _), Is.False,
                "외곽 상자 안이더라도 삼각형이 없는 곳은 배치 표면이 아닙니다.");
        }

        [Test]
        public void Surface_ChoosesNearestVisibleWorkspaceMeshAndCanIgnoreSubtree()
        {
            _workspace = Workspace.CreateRoot();
            GameObject bottom = CreateSurfaceCube(new Vector3(0, 0, 0));
            GameObject top = CreateSurfaceCube(new Vector3(0, 3, 0));
            Ray ray = new Ray(new Vector3(0, 10, 0), Vector3.down);
            Assert.That(Surface.TryRaycast(_workspace, ray, out RaycastHit hit), Is.True);
            Assert.That(hit.point.y, Is.EqualTo(3.5f).Within(.001f));
            Assert.That(Surface.TryRaycast(_workspace, ray, out hit, top.transform), Is.True);
            Assert.That(hit.point.y, Is.EqualTo(.5f).Within(.001f));
            top.GetComponent<Renderer>().enabled = false;
            Assert.That(Surface.TryRaycast(_workspace, ray, out hit), Is.True);
            Assert.That(hit.point.y, Is.EqualTo(.5f).Within(.001f));
            bottom.SetActive(false);
            GameObject outside = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outside.transform.SetParent(_parent, false);
            outside.transform.position = new Vector3(0, 5, 0);
            Assert.That(Surface.TryRaycast(_workspace, ray, out _), Is.False);
        }

        [Test]
        public void Surface_SeatsOffsetChildOnSlopeWithGapAndPreservesScale()
        {
            _workspace = Workspace.CreateRoot();
            GameObject container = new GameObject("중앙에서 벗어난 피벗");
            SceneManager.MoveGameObjectToScene(container, _workspace.scene);
            container.transform.SetParent(_workspace.transform, false);
            container.transform.localScale = new Vector3(2, 3, 4);
            container.transform.rotation = Quaternion.Euler(0, 35, 0);
            GameObject child = CreateSurfaceCube(Vector3.zero);
            child.transform.SetParent(container.transform, false);
            child.transform.localPosition = new Vector3(0, 2, 0);
            Vector3 normal = Quaternion.Euler(0, 0, 30) * Vector3.up;
            Vector3 point = new Vector3(4, 5, 6);
            Surface.Seat(container.transform, point, normal, true, .2f);
            Assert.That(Vector3.Angle(container.transform.up, normal), Is.LessThan(.01f));
            Vector3 bottomCenter = child.transform.TransformPoint(Vector3.down * .5f);
            Assert.That(Vector3.Dot(bottomCenter - point, normal), Is.EqualTo(.2f).Within(.001f));
            Assert.That(container.transform.localScale, Is.EqualTo(new Vector3(2, 3, 4)));
        }

        [Test]
        public void Surface_AddFbxUndoRedoPreservesSeatedPoseAndRootTransform()
        {
            _workspace = Workspace.CreateRoot();
            _workspace.transform.SetPositionAndRotation(new Vector3(5, 2, 7), Quaternion.Euler(0, 25, 0));
            _workspace.transform.localScale = Vector3.one * 2;
            Vector3 point = new Vector3(1, 4, 2);
            GameObject instance = Workspace.AddFbxOnSurface(_workspace, _model, point, Vector3.up, false, .1f);
            Vector3 expected = instance.transform.position;
            Bounds bounds = instance.GetComponentInChildren<Renderer>().bounds;
            Assert.That(bounds.min.y, Is.EqualTo(point.y + .1f).Within(.001f));
            Undo.PerformUndo();
            Assert.That(_workspace.transform.childCount, Is.Zero);
            Undo.PerformRedo();
            Assert.That(_workspace.transform.childCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(_workspace.transform.GetChild(0).position, expected), Is.LessThan(.001f));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(_workspace.transform.GetChild(0)), Is.False);
        }

        [Test]
        public void Surface_RejectsInvalidNormalBeforeAddingFbx()
        {
            _workspace = Workspace.CreateRoot();
            Assert.Throws<System.ArgumentException>(() =>
                Workspace.AddFbxOnSurface(_workspace, _model, Vector3.zero, Vector3.zero, true, 0));
            Assert.That(_workspace.transform.childCount, Is.Zero);
        }

        [Test]
        public void Surface_SeatsActualAsymmetricMeshInsteadOfEmptyBoundsCorner()
        {
            _workspace = Workspace.CreateRoot();
            Vector3[] vertices = { new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0) };
            _surfaceMesh = new Mesh { vertices = vertices, triangles = new[] { 0, 1, 2 } };
            _surfaceMesh.RecalculateBounds();
            _surfaceMesh.UploadMeshData(true);
            GameObject model = new GameObject("비대칭 모델", typeof(MeshFilter), typeof(MeshRenderer));
            SceneManager.MoveGameObjectToScene(model, _workspace.scene);
            model.transform.SetParent(_workspace.transform, false);
            model.GetComponent<MeshFilter>().sharedMesh = _surfaceMesh;
            model.transform.rotation = Quaternion.Euler(0, 0, 45);
            Vector3 point = new Vector3(4, 2, 7);
            Surface.Seat(model.transform, point, Vector3.up, false, 0);
            float bottom = float.PositiveInfinity;
            foreach (Vector3 vertex in vertices) bottom = Mathf.Min(bottom, model.transform.TransformPoint(vertex).y);
            Assert.That(bottom, Is.EqualTo(point.y).Within(.001f), "빈 바운딩 박스 모서리 때문에 메시가 뜨면 안 됩니다.");
            Bounds bounds = model.GetComponent<Renderer>().bounds;
            Assert.That(bounds.center.x, Is.EqualTo(point.x).Within(.001f));
            Assert.That(bounds.center.z, Is.EqualTo(point.z).Within(.001f));
        }

        [Test]
        public void Surface_UprightPlacementKeepsFootprintAtCursorOnSlope()
        {
            _workspace = Workspace.CreateRoot();
            GameObject model = CreateSurfaceCube(Vector3.zero);
            model.transform.rotation = Quaternion.Euler(0, 35, 0);
            model.transform.localScale = new Vector3(2, 3, 4);
            Quaternion rotation = model.transform.rotation;
            Vector3 normal = new Vector3(.6f, .8f, 0);
            Vector3 point = new Vector3(8, 3, 4);
            Surface.Seat(model.transform, point, normal, false, .2f);
            Assert.That(model.GetComponent<Renderer>().bounds.center.x, Is.EqualTo(point.x).Within(.001f));
            Assert.That(model.GetComponent<Renderer>().bounds.center.z, Is.EqualTo(point.z).Within(.001f));
            Assert.That(Quaternion.Angle(model.transform.rotation, rotation), Is.LessThan(.001f));
            float clearance = float.PositiveInfinity;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        clearance = Mathf.Min(clearance, Vector3.Dot(normal,
                            model.transform.TransformPoint(new Vector3(x, y, z) * .5f) - point));
            Assert.That(clearance, Is.EqualTo(.2f).Within(.001f));
        }

        private GameObject CreateSurfaceCube(Vector3 position)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(cube, _workspace.scene);
            cube.transform.SetParent(_workspace.transform, false);
            cube.transform.position = position;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube;
        }

        [Test]
        public void Workspace_AddsUnpackedFbxWithoutChangingActiveSceneOrItsRoots()
        {
            int rootsBefore = _testScene.rootCount;
            bool dirtyBefore = _testScene.isDirty;
            _workspace = Workspace.CreateRoot();
            GameObject placed = Workspace.AddFbx(_workspace, _model, new Vector3(3, 0, 5));
            Assert.That(EditorSceneManager.IsPreviewScene(placed.scene), Is.True);
            Assert.That(placed.transform.parent, Is.EqualTo(_workspace.transform));
            Assert.That(placed.transform.localPosition, Is.EqualTo(new Vector3(3, 0, 5)));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(placed), Is.False);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_testScene));
            Assert.That(_testScene.rootCount, Is.EqualTo(rootsBefore));
            Assert.That(_testScene.isDirty, Is.EqualTo(dirtyBefore));
            Assert.That(placed.GetComponentInChildren<MeshFilter>().sharedMesh,
                Is.SameAs(_model.GetComponentInChildren<MeshFilter>().sharedMesh));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PrefabPlacement_UnpacksNestedHierarchyAndPreservesSourceThroughUndoAndMapSave(bool variant)
        {
            GameObject prefab = CreatePlacementPrefab(variant);
            string sourcePath = AssetDatabase.GetAssetPath(prefab);
            byte[] original = System.IO.File.ReadAllBytes(sourcePath);
            _workspace = Workspace.CreateRoot();
            Transform layer = Layers.Create(_workspace, "프리팹");
            layer.SetPositionAndRotation(new Vector3(3, 1, 7), Quaternion.Euler(0, 35, 0));
            layer.localScale = Vector3.one * 2;
            Vector3 position = new Vector3(2, 3, 4);
            int rootsBefore = _testScene.rootCount;
            bool dirtyBefore = _testScene.isDirty;

            GameObject placed = Workspace.AddAsset(_workspace, prefab, position, layer);

            Assert.That(placed.scene, Is.EqualTo(_workspace.scene));
            Assert.That(placed.transform.parent, Is.SameAs(layer));
            Assert.That(Vector3.Distance(placed.transform.position, _workspace.transform.TransformPoint(position)), Is.LessThan(.001f));
            Assert.That(placed.transform.localScale, Is.EqualTo(prefab.transform.localScale));
            Assert.That(Quaternion.Angle(placed.transform.localRotation, prefab.transform.localRotation), Is.LessThan(.001f));
            Assert.That(placed.GetComponentInChildren<BoxCollider>(), Is.Not.Null);
            Assert.That(placed.GetComponent<Light>().intensity, Is.EqualTo(prefab.GetComponent<Light>().intensity));
            Assert.That(placed.GetComponentInChildren<MeshFilter>().sharedMesh, Is.SameAs(prefab.GetComponentInChildren<MeshFilter>().sharedMesh));
            AssertUnpacked(placed);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_testScene));
            Assert.That(_testScene.rootCount, Is.EqualTo(rootsBefore));
            Assert.That(_testScene.isDirty, Is.EqualTo(dirtyBefore));

            Undo.PerformUndo();
            Assert.That(layer.childCount, Is.Zero);
            Undo.PerformRedo();
            Assert.That(layer.childCount, Is.EqualTo(1));
            GameObject restored = layer.GetChild(0).gameObject;
            AssertUnpacked(restored);
            Assert.That(Vector3.Distance(restored.transform.position, position), Is.LessThan(.001f));
            restored.GetComponent<Light>().intensity = 9;
            restored.transform.GetChild(0).localPosition = Vector3.one * 5;
            Workspace.SavePrefab(_workspace, _assetFolder + "/Map.prefab");
            _reopened = Workspace.OpenPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(_assetFolder + "/Map.prefab"));
            GameObject reopened = _reopened.GetComponentInChildren<Light>().gameObject;
            Assert.That(reopened.GetComponent<Light>().intensity, Is.EqualTo(9));
            Assert.That(reopened.transform.GetChild(0).localPosition, Is.EqualTo(Vector3.one * 5));
            AssertUnpacked(reopened);
            CollectionAssert.AreEqual(original, System.IO.File.ReadAllBytes(sourcePath));
            Assert.That(prefab.GetComponent<Light>().intensity, Is.EqualTo(variant ? 3 : 2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PrefabSurfacePlacement_SeatsNestedMeshAndUndoRedoKeepsPose(bool variant)
        {
            GameObject prefab = CreatePlacementPrefab(variant);
            _workspace = Workspace.CreateRoot();
            Transform layer = Layers.Create(_workspace, "지형");
            Vector3 point = new Vector3(4, 6, 8);
            GameObject placed = Workspace.AddAssetOnSurface(_workspace, prefab, point, Vector3.up, false, .2f, layer);
            Assert.That(placed.GetComponentInChildren<Renderer>().bounds.min.y, Is.EqualTo(6.2f).Within(.001f));
            Assert.That(placed.transform.parent, Is.SameAs(layer));
            Vector3 expected = placed.transform.position;
            Undo.PerformUndo();
            Assert.That(layer.childCount, Is.Zero);
            Undo.PerformRedo();
            Assert.That(Vector3.Distance(layer.GetChild(0).position, expected), Is.LessThan(.001f));
            AssertUnpacked(layer.GetChild(0).gameObject);
        }

        [Test]
        public void PrefabCatalog_ListsRegularAndVariantAndAddsAnIndependentVolumeProfile()
        {
            GameObject variant = CreatePlacementPrefab(true);
            var window = ScriptableObject.CreateInstance<ProjectL.Tool.Editor.FbxPlacementWindow>();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            System.Type type = window.GetType();
            var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>().postExposure.Override(-2);
            AssetDatabase.CreateAsset(profile, _assetFolder + "/Mood.asset");
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            GameObject source = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(variant));
            try
            {
                source.AddComponent<UnityEngine.Rendering.Volume>().sharedProfile = profile;
                PrefabUtility.SaveAsPrefabAsset(source, AssetDatabase.GetAssetPath(variant));
            }
            finally { PrefabUtility.UnloadPrefabContents(source); }
            try
            {
                type.GetMethod("SetCatalogFolder", flags).Invoke(window, new object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(_assetFolder) });
                var paths = (System.Collections.Generic.List<string>)type.GetField("_catalogPaths", flags).GetValue(window);
                CollectionAssert.AreEquivalent(new[] { _assetFolder + "/Nested.prefab", _assetFolder + "/Base.prefab", _assetFolder + "/Variant.prefab" }, paths);
                variant = AssetDatabase.LoadAssetAtPath<GameObject>(_assetFolder + "/Variant.prefab");
                Assert.That(PlacementTool.GetPlacementAsset(variant.transform.GetChild(0).gameObject), Is.SameAs(variant));
                type.GetMethod("Add", flags).Invoke(window, new object[] { variant, Vector3.zero });
                var root = (GameObject)type.GetField("workspaceRoot", flags).GetValue(window);
                var volume = root.GetComponentInChildren<UnityEngine.Rendering.Volume>();
                Assert.That(volume.sharedProfile, Is.Not.SameAs(profile));
                volume.sharedProfile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments colors);
                colors.postExposure.Override(4);
                profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments original);
                Assert.That(original.postExposure.value, Is.EqualTo(-2));
            }
            finally
            {
                typeof(EditorWindow).GetProperty(nameof(EditorWindow.hasUnsavedChanges)).SetValue(window, false);
                window.Close();
            }
        }

        [Test]
        public void AssetPlacement_RejectsSceneObjectsForeignParentsAndInvalidSurfacesBeforeCreating()
        {
            GameObject prefab = CreatePlacementPrefab(false);
            _workspace = Workspace.CreateRoot();
            Assert.Throws<System.ArgumentException>(() => PlacementTool.GetModelAsset(prefab));
            Assert.Throws<System.ArgumentException>(() => Workspace.AddAsset(_workspace, _parent.gameObject, Vector3.zero));
            Assert.Throws<System.ArgumentException>(() => Workspace.AddAsset(_workspace, prefab, Vector3.zero, _parent));
            Assert.Throws<System.ArgumentException>(() => Workspace.AddAssetOnSurface(_workspace, prefab, Vector3.zero, Vector3.zero, false, 0));
            Assert.That(_workspace.transform.childCount, Is.Zero);
        }

        private GameObject CreatePlacementPrefab(bool variant)
        {
            string folderName = "__PrefabPlacementTest_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _assetFolder = "Assets/" + folderName;
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(_parent, false);
            GameObject nested = PrefabUtility.SaveAsPrefabAsset(cube, _assetFolder + "/Nested.prefab");
            Object.DestroyImmediate(cube);
            GameObject source = new GameObject("배치 프리팹", typeof(Light));
            source.transform.SetParent(_parent, false);
            try
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(nested, _testScene);
                child.transform.SetParent(source.transform, false);
                child.transform.localPosition = Vector3.up * 2;
                source.transform.localRotation = Quaternion.Euler(0, 15, 0);
                source.transform.localScale = new Vector3(2, 3, 4);
                source.GetComponent<Light>().intensity = 2;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, _assetFolder + "/Base.prefab");
                if (!variant) return prefab;
                Object.DestroyImmediate(source);
                source = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _testScene);
                source.transform.SetParent(_parent, false);
                source.GetComponent<Light>().intensity = 3;
                PrefabUtility.RecordPrefabInstancePropertyModifications(source.GetComponent<Light>());
                GameObject result = PrefabUtility.SaveAsPrefabAsset(source, _assetFolder + "/Variant.prefab");
                Assert.That(PrefabUtility.GetPrefabAssetType(result), Is.EqualTo(PrefabAssetType.Variant));
                return result;
            }
            finally { Object.DestroyImmediate(source); }
        }

        private static void AssertUnpacked(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(child.gameObject), Is.False);
        }

        [Test]
        public void Workspace_UndoRedoWorksInsidePreviewScene()
        {
            _workspace = Workspace.CreateRoot();
            Workspace.AddFbx(_workspace, _model, new Vector3(2, 0, 4));
            Undo.PerformUndo();
            Assert.That(_workspace.transform.childCount, Is.Zero);
            Undo.PerformRedo();
            Assert.That(_workspace.transform.childCount, Is.EqualTo(1));
            Assert.That(_workspace.transform.GetChild(0).localPosition, Is.EqualTo(new Vector3(2, 0, 4)));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(_workspace.transform.GetChild(0)), Is.False);
        }

        [Test]
        public void Workspace_PrefabRoundTripPreservesLayoutAndSharedMesh()
        {
            string folderName = "__FbxPlacementTest_" + System.Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            _assetFolder = "Assets/" + folderName;
            _workspace = Workspace.CreateRoot();
            GameObject placed = Workspace.AddFbx(_workspace, _model, new Vector3(2, 3, 4));
            placed.transform.localRotation = Quaternion.Euler(0, 60, 0);
            placed.transform.localScale *= 2;
            // 작업 화면의 보조 카메라가 맵 프리팹에 섞이지 않는지 확인합니다.
            GameObject camera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera));
            SceneManager.MoveGameObjectToScene(camera, _workspace.scene);
            GameObject prefab = Workspace.SavePrefab(_workspace, _assetFolder + "/Map.prefab");
            Assert.That(EditorUtility.IsPersistent(prefab), Is.True);
            Assert.That(prefab.GetComponentsInChildren<Camera>(true), Is.Empty);
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(_workspace), Is.False);
            _reopened = Workspace.OpenPrefab(prefab);
            Transform restored = _reopened.transform.GetChild(0);
            Assert.That(restored.localPosition, Is.EqualTo(placed.transform.localPosition));
            Assert.That(Quaternion.Angle(restored.localRotation, placed.transform.localRotation), Is.LessThan(.001f));
            Assert.That(restored.localScale, Is.EqualTo(placed.transform.localScale));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(restored), Is.False);
            Assert.That(restored.GetComponentInChildren<MeshFilter>().sharedMesh,
                Is.SameAs(_model.GetComponentInChildren<MeshFilter>().sharedMesh));
            restored.localPosition = Vector3.zero;
            Assert.That(prefab.transform.GetChild(0).localPosition, Is.EqualTo(new Vector3(2, 3, 4)));
        }

        [Test]
        public void Workspace_RejectsRegularSceneRootAndInvalidSavePath()
        {
            Assert.Throws<System.ArgumentException>(() => Workspace.AddFbx(_parent.gameObject, _model, Vector3.zero));
            _workspace = Workspace.CreateRoot();
            Assert.Throws<System.ArgumentException>(() => Workspace.SavePrefab(_workspace, "Library/Map.prefab"));
            Assert.Throws<System.ArgumentException>(() => Workspace.SavePrefab(_workspace, "Assets/../Map.prefab"));
            Assert.That(_workspace.transform.childCount, Is.Zero);
        }

        [Test]
        public void Place_PreservesSourceAndMeshReferences_AndUnpacksHierarchy()
        {
            Vector3 sourceScale = _model.transform.localScale;
            Quaternion sourceRotation = _model.transform.localRotation;
            Vector3 offset = new Vector3(1, 2, 3);
            GameObject placed = PlacementTool.Place(_model, _parent, offset, new Vector3(0, 45, 0), Vector3.one * 2);

            Assert.That(placed.scene, Is.EqualTo(_testScene));
            Assert.That(placed.transform.parent, Is.EqualTo(_parent));
            Assert.That(placed.transform.localPosition, Is.EqualTo(offset));
            Assert.That(placed.transform.localScale, Is.EqualTo(sourceScale * 2));
            Assert.That(Quaternion.Angle(placed.transform.localRotation, Quaternion.Euler(0, 45, 0) * sourceRotation), Is.LessThan(.001f));
            foreach (Transform child in placed.GetComponentsInChildren<Transform>(true))
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(child.gameObject), Is.False);

            MeshFilter[] originals = _model.GetComponentsInChildren<MeshFilter>(true);
            MeshFilter[] copies = placed.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(copies.Length, Is.EqualTo(originals.Length).And.GreaterThan(0));
            for (int i = 0; i < originals.Length; i++)
                Assert.That(copies[i].sharedMesh, Is.SameAs(originals[i].sharedMesh));
            Renderer[] sourceRenderers = _model.GetComponentsInChildren<Renderer>(true);
            Renderer[] copyRenderers = placed.GetComponentsInChildren<Renderer>(true);
            Assert.That(copyRenderers.Length, Is.EqualTo(sourceRenderers.Length));
            for (int i = 0; i < sourceRenderers.Length; i++)
                CollectionAssert.AreEqual(sourceRenderers[i].sharedMaterials, copyRenderers[i].sharedMaterials);
            Assert.That(_model.transform.localScale, Is.EqualTo(sourceScale));
            Assert.That(_model.transform.localRotation, Is.EqualTo(sourceRotation));
        }

        [Test]
        public void Place_OneUndoRemovesInstance_AndRedoRestoresUnpackedTransform()
        {
            Vector3 position = new Vector3(7, 8, 9);
            PlacementTool.Place(_model, _parent, position, Vector3.zero, Vector3.one);
            Assert.That(_parent.childCount, Is.EqualTo(1));
            Undo.PerformUndo();
            Assert.That(_parent.childCount, Is.Zero);
            Undo.PerformRedo();
            Assert.That(_parent.childCount, Is.EqualTo(1));
            Transform restored = _parent.GetChild(0);
            Assert.That(restored.localPosition, Is.EqualTo(position));
            Assert.That(restored.localScale, Is.EqualTo(_model.transform.localScale));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(restored.gameObject), Is.False);
        }

        [Test]
        public void Place_RejectsSceneObjectBeforeCreatingAnything()
        {
            Assert.Throws<System.ArgumentException>(() =>
                PlacementTool.Place(_parent.gameObject, null, Vector3.zero, Vector3.zero, Vector3.one));
            Assert.That(_testScene.rootCount, Is.EqualTo(_initialRootCount + 1));
            Assert.That(_parent.childCount, Is.Zero);
        }

        [Test]
        public void Place_RejectsZeroOrNonFiniteTransformBeforeCreatingAnything()
        {
            Assert.Throws<System.ArgumentException>(() =>
                PlacementTool.Place(_model, _parent, Vector3.zero, Vector3.zero, Vector3.zero));
            Assert.Throws<System.ArgumentException>(() =>
                PlacementTool.Place(_model, _parent, new Vector3(float.NaN, 0, 0), Vector3.zero, Vector3.one));
            Assert.That(_parent.childCount, Is.Zero);
        }
    }
}
