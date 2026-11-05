using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // 임포트된 FBX의 메시와 변환만 독립 미리보기로 가져온다.
    internal sealed class ShaderComposerObjectPreview : IDisposable
    {
        private sealed class Part
        {
            internal Mesh Mesh;
            internal Matrix4x4 Matrix;
        }

        private readonly PreviewRenderUtility _preview = new PreviewRenderUtility();
        private readonly List<Part> _parts = new List<Part>();
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private readonly List<Light> _lights = new List<Light>();
        private readonly ShaderComposerPreviewLightOverlay _lightOverlay = new ShaderComposerPreviewLightOverlay();
        private readonly ShaderComposerPreviewPostProcessing _postProcessing = new ShaderComposerPreviewPostProcessing();
        private UnityEngine.Object _sourceAsset;
        private bool _sourceInitialized;
        private Bounds _bounds;
        private string _error;
        private float _yaw = -30;
        private float _pitch = 15;
        private float _zoom = 1;

        internal ShaderComposerObjectPreview()
        {
            _preview.camera.fieldOfView = 30;
            _preview.camera.allowHDR = true;
            _preview.camera.clearFlags = CameraClearFlags.SolidColor;
            _preview.camera.backgroundColor = new Color(0.17f, 0.17f, 0.17f);
            // PreviewRenderUtility.Render가 기본 조명을 다시 켜므로 강도도 0으로 고정한다.
            foreach (Light light in _preview.lights)
            {
                light.enabled = false;
                light.intensity = 0;
                light.color = Color.black;
            }
        }

        internal static bool TryGetFbxModel(UnityEngine.Object asset, out GameObject model)
        {
            model = null;
            if (asset == null || !EditorUtility.IsPersistent(asset)) return false;
            string path = AssetDatabase.GetAssetPath(asset);
            if (!string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase) ||
                !(AssetImporter.GetAtPath(path) is ModelImporter)) return false;
            model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return model != null;
        }

        internal void SetSource(UnityEngine.Object asset, bool refresh = false)
        {
            if (_sourceInitialized && _sourceAsset == asset && !refresh) return;
            _sourceAsset = asset;
            _sourceInitialized = true;
            GameObject source = null;
            bool valid = asset == null || TryGetFbxModel(asset, out source);
            ReleaseMeshes();
            _error = null;
            _yaw = -30;
            _pitch = 15;
            _zoom = 1;
            if (!valid)
            {
                _error = "Project에 임포트된 FBX 모델 파일을 넣으세요.";
                return;
            }
            try
            {
                if (source == null)
                {
                    Mesh sphere = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                    if (sphere == null) throw new InvalidOperationException("기본 구 메시를 찾지 못했습니다.");
                    Add(sphere, Matrix4x4.identity);
                    return;
                }
                Matrix4x4 origin = Matrix4x4.Translate(-source.transform.position);
                HashSet<Renderer> excluded = LowerLods(source);
                foreach (MeshRenderer renderer in source.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!Visible(renderer, source.transform) || excluded.Contains(renderer)) continue;
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                        Add(filter.sharedMesh, origin * renderer.localToWorldMatrix);
                }
                foreach (SkinnedMeshRenderer renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!Visible(renderer, source.transform) || excluded.Contains(renderer) || renderer.sharedMesh == null) continue;
                    var mesh = new Mesh { name = "Shader Composer Pose", hideFlags = HideFlags.HideAndDontSave };
                    _ownedMeshes.Add(mesh);
                    renderer.BakeMesh(mesh);
                    mesh.RecalculateBounds();
                    Add(mesh, origin * renderer.localToWorldMatrix);
                }
                if (_parts.Count == 0) _error = "이 FBX에는 미리볼 메시가 없습니다.";
            }
            catch (Exception exception)
            {
                ReleaseMeshes();
                _error = "FBX를 미리보기로 가져오지 못했습니다: " + exception.Message;
            }
        }

        private static bool Visible(Renderer renderer, Transform root)
        {
            if (!renderer.enabled) return false;
            for (Transform node = renderer.transform; node != null && node != root; node = node.parent)
                if (!node.gameObject.activeSelf) return false;
            return true;
        }

        private static HashSet<Renderer> LowerLods(GameObject source)
        {
            var result = new HashSet<Renderer>();
            foreach (LODGroup group in source.GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                    foreach (Renderer renderer in lods[i].renderers)
                        if (renderer != null) result.Add(renderer);
                if (lods.Length > 0)
                    foreach (Renderer renderer in lods[0].renderers) result.Remove(renderer);
            }
            return result;
        }

        private void Add(Mesh mesh, Matrix4x4 matrix)
        {
            if (mesh.subMeshCount == 0) return;
            Bounds bounds = mesh.bounds;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)));
                        if (_parts.Count == 0 && x == -1 && y == -1 && z == -1) _bounds = new Bounds(corner, Vector3.zero);
                        else _bounds.Encapsulate(corner);
                    }
                }
            }
            _parts.Add(new Part { Mesh = mesh, Matrix = matrix });
        }

        private void ApplySettings(ShaderComposerPreviewSettings settings)
        {
            while (_lights.Count > settings.lights.Count)
            {
                int last = _lights.Count - 1;
                UnityEngine.Object.DestroyImmediate(_lights[last].gameObject);
                _lights.RemoveAt(last);
            }
            while (_lights.Count < settings.lights.Count)
            {
                var source = EditorUtility.CreateGameObjectWithHideFlags("Shader Composer Preview Light", HideFlags.HideAndDontSave, typeof(Light));
                _preview.AddSingleGO(source);
                _lights.Add(source.GetComponent<Light>());
            }
            float radius = Mathf.Max(0.001f, _bounds.extents.magnitude);
            for (int i = 0; i < _lights.Count; i++)
            {
                ShaderComposerPreviewLight data = settings.lights[i];
                Light light = _lights[i];
                light.enabled = data.enabled;
                light.type = data.type;
                light.color = data.color;
                light.intensity = Mathf.Max(0, data.intensity);
                light.range = Mathf.Max(0.01f, data.range) * radius;
                light.spotAngle = Mathf.Clamp(data.spotAngle, 1, 179);
                light.innerSpotAngle = Mathf.Clamp(data.innerSpotAngle, 0, light.spotAngle);
                light.shadows = LightShadows.None;
                light.transform.SetPositionAndRotation(_bounds.center + data.position * radius, Quaternion.Euler(data.rotation));
            }
            _preview.camera.backgroundColor = settings.background;
            _preview.ambientColor = settings.ambient;
        }

        internal bool Draw(Rect rect, Material material, ShaderComposerPreviewSettings settings)
        {
            bool moved = Navigate(rect);
            if (!string.IsNullOrEmpty(_error))
            {
                GUI.Label(rect, _error, EditorStyles.wordWrappedLabel);
                return moved;
            }
            if (material == null || _parts.Count == 0)
            {
                GUI.Label(rect, "조립 결과를 준비하고 있습니다.", EditorStyles.centeredGreyMiniLabel);
                return moved;
            }
            if (Event.current.type != EventType.Repaint) return moved;
            try
            {
                Texture image = Render(rect, material, settings);
                if (image != null) GUI.DrawTexture(rect, image, ScaleMode.StretchToFill, false);
                if (image != null && settings.showLightOverlay)
                    _lightOverlay.Draw(rect, _preview.camera, _bounds, _lights, settings.showLightRanges);
            }
            catch (Exception exception)
            {
                _error = "미리보기를 렌더링하지 못했습니다: " + exception.Message;
            }
            return moved;
        }

        private Texture Render(Rect rect, Material material, ShaderComposerPreviewSettings settings)
        {
            ApplySettings(settings);
            float aspect = Mathf.Max(0.1f, rect.width / Mathf.Max(1, rect.height));
            float vertical = _preview.camera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float angle = Mathf.Min(vertical, Mathf.Atan(Mathf.Tan(vertical) * aspect));
            float radius = Mathf.Max(0.001f, _bounds.extents.magnitude);
            float distance = radius / Mathf.Sin(angle) * 1.15f * _zoom;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            _preview.camera.transform.SetPositionAndRotation(_bounds.center - rotation * Vector3.forward * distance, rotation);
            _preview.camera.aspect = aspect;
            _preview.camera.nearClipPlane = Mathf.Max(0.00001f, radius * 0.01f);
            _preview.camera.farClipPlane = distance + radius * 4;
            _preview.BeginPreview(rect, GUIStyle.none);
            Texture image = null;
            try
            {
                foreach (Part part in _parts)
                    for (int subMesh = 0; subMesh < part.Mesh.subMeshCount; subMesh++)
                        _preview.DrawMesh(part.Mesh, part.Matrix, material, subMesh);
                // 직접 계산한 종횡비·화각을 오버레이 투영과 동일하게 유지한다.
                if (!_postProcessing.Render(_preview, settings)) _preview.Render(true, false);
            }
            finally
            {
                image = _preview.EndPreview();
            }
            return image;
        }

        private bool Navigate(Rect rect)
        {
            Event input = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Passive);
            switch (input.GetTypeForControl(control))
            {
                case EventType.MouseDown:
                    if (rect.Contains(input.mousePosition) && (input.button == 0 || input.button == 1))
                    {
                        GUIUtility.hotControl = control;
                        input.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == control)
                    {
                        _yaw += input.delta.x * 0.4f;
                        _pitch = Mathf.Clamp(_pitch + input.delta.y * 0.4f, -85, 85);
                        input.Use();
                        return true;
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == control)
                    {
                        GUIUtility.hotControl = 0;
                        input.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    if (rect.Contains(input.mousePosition))
                    {
                        _zoom = Mathf.Clamp(_zoom * Mathf.Exp(input.delta.y * 0.05f), 0.35f, 5);
                        input.Use();
                        return true;
                    }
                    break;
            }
            return false;
        }

        private void ReleaseMeshes()
        {
            _parts.Clear();
            foreach (Mesh mesh in _ownedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
            _ownedMeshes.Clear();
        }

        public void Dispose()
        {
            ReleaseMeshes();
            _lights.Clear();
            _postProcessing.Dispose();
            _preview.Cleanup();
        }
    }
}
