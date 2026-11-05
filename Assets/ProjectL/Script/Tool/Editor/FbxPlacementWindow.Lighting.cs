using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        private const string LightDragKey = "ProjectL.FbxPlacement.LightType";
        [SerializeField] private bool showLightCatalog;
        [SerializeField] private bool useWorkspaceLighting = true;
        [SerializeField] private int lightTypeIndex;
        [SerializeField] private Vector3 lightPlacementPosition = new Vector3(0, 3, 0);
        [SerializeField] private float lightDropHeight = 3;
        private Vector2 _lightCatalogScroll;
        private int _dragLightIndex = -1;
        private Vector2 _lightDragStart;

        private void DrawLightCatalog()
        {
            using (new GUILayout.AreaScope(new Rect(8, 6, CatalogWidth - 16, CatalogContentHeight - 12)))
            using (var scroll = new EditorGUILayout.ScrollViewScope(_lightCatalogScroll))
            {
                _lightCatalogScroll = scroll.scrollPosition;
                GUILayout.Label("Unity 라이트", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("더블클릭 추가 · 씬으로 드래그 배치", EditorStyles.wordWrappedMiniLabel);
                int id = GUIUtility.GetControlID("FbxLightDrag".GetHashCode(), FocusType.Passive);
                for (int i = 0; i < FbxPlacementLights.Types.Length; i++) DrawLightCatalogRow(i, id);
                HandleLightCatalogDrag(id);
                GUILayout.Space(6);
                EditorGUILayout.LabelField("추가 위치 (맵 기준)", EditorStyles.miniLabel);
                lightPlacementPosition = EditorGUILayout.Vector3Field(GUIContent.none, lightPlacementPosition);
                lightDropHeight = Mathf.Max(0, EditorGUILayout.FloatField(new GUIContent("드래그 높이", "표면에서 위쪽으로 띄울 거리"), lightDropHeight));
                if (GUILayout.Button("선택 종류 추가", GUILayout.Height(25)))
                    Run(() => AddLight(FbxPlacementLights.Types[lightTypeIndex], lightPlacementPosition));
                EditorGUILayout.HelpBox("Area는 베이크 전용입니다. 이 작업실의 실시간 미리보기에서는 빛이 나오지 않습니다.", MessageType.Info);
                GUILayout.Space(8);
                Light[] lights = FbxPlacementLights.GetLights(workspaceRoot);
                GUILayout.Label("맵의 라이트 · " + lights.Length + "개", EditorStyles.boldLabel);
                if (lights.Length == 0) EditorGUILayout.LabelField("Directional부터 추가해 보세요.", EditorStyles.wordWrappedMiniLabel);
                foreach (Light light in lights)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool enabled = EditorGUILayout.Toggle(light.enabled, GUILayout.Width(18));
                        if (enabled != light.enabled)
                        {
                            Undo.RecordObject(light, "맵 라이트 켜기/끄기");
                            light.enabled = enabled;
                            Changed();
                        }
                        if (GUILayout.Toggle(Selection.Contains(light.gameObject),
                            new GUIContent(light.name, FbxPlacementLights.GetName(light.type)), "Button") != Selection.Contains(light.gameObject))
                            Select(light.transform);
                        if (GUILayout.Button("보기", GUILayout.Width(34))) { Select(light.transform); Frame(light.transform); }
                    }
                    EditorGUILayout.LabelField(FbxPlacementLights.GetName(light.type) + " · " + light.lightmapBakeType,
                        EditorStyles.miniLabel);
                }
            }
        }

        private void DrawLightCatalogRow(int index, int id)
        {
            Rect rect = GUILayoutUtility.GetRect(10, 42, GUILayout.ExpandWidth(true));
            LightType type = FbxPlacementLights.Types[index];
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, lightTypeIndex == index ? new Color(.17f, .32f, .47f) : new Color(.22f, .22f, .22f));
            GUI.Label(new Rect(rect.x + 5, rect.y + 3, 28, 32), EditorGUIUtility.IconContent("Light Icon"));
            GUI.Label(new Rect(rect.x + 36, rect.y + 3, rect.width - 40, 20), FbxPlacementLights.GetName(type));
            GUI.Label(new Rect(rect.x + 36, rect.y + 22, rect.width - 40, 18),
                FbxPlacementLights.IsArea(type) ? "베이크 전용" : "실시간 조명", EditorStyles.miniLabel);
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition)) return;
            lightTypeIndex = index;
            if (e.clickCount == 2)
            {
                GUIUtility.hotControl = 0;
                _dragLightIndex = -1;
                Run(() => AddLight(type, lightPlacementPosition));
            }
            else
            {
                _dragLightIndex = index;
                _lightDragStart = e.mousePosition;
                GUIUtility.hotControl = id;
            }
            e.Use();
            Repaint();
        }

        private void HandleLightCatalogDrag(int id)
        {
            Event e = Event.current;
            if (GUIUtility.hotControl != id) return;
            if (e.type == EventType.MouseDrag && _dragLightIndex >= 0 && (e.mousePosition - _lightDragStart).sqrMagnitude > 36)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new UnityEngine.Object[0];
                DragAndDrop.SetGenericData(LightDragKey, FbxPlacementLights.Types[_dragLightIndex]);
                DragAndDrop.StartDrag("Unity Light");
                GUIUtility.hotControl = 0;
                _dragLightIndex = -1;
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                _dragLightIndex = -1;
                e.Use();
            }
        }

        private bool HandleLightDrop(Event e)
        {
            if (DragAndDrop.GetGenericData(LightDragKey) is not LightType type) return false;
            bool valid = TryGetDropTarget(HandleUtility.GUIPointToWorldRay(e.mousePosition), out _dropPoint, out _dropNormal);
            _showDropTarget = valid;
            DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            if (e.type == EventType.DragPerform)
            {
                if (valid)
                {
                    DragAndDrop.AcceptDrag();
                    Vector3 position = workspaceRoot.transform.InverseTransformPoint(_dropPoint + Vector3.up * lightDropHeight);
                    Run(() => AddLight(type, position));
                }
                DragAndDrop.SetGenericData(LightDragKey, null);
                _showDropTarget = false;
            }
            e.Use();
            Repaint();
            return true;
        }

        private void AddLight(LightType type, Vector3 position)
        {
            Light light = FbxPlacementLights.Add(workspaceRoot, type, position, PlacementParent);
            _expanded.Add(PlacementParent.GetInstanceID());
            Select(light.transform);
            useWorkspaceLighting = sceneLighting = true;
            Tools.current = UnityEditor.Tool.Move;
            Changed();
            _notice = null;
        }

        private void DrawLightSettings()
        {
            Light light = selected.GetComponent<Light>();
            if (light == null) return;
            GUILayout.Space(8);
            GUILayout.Label("Unity Light 설정", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("종류", FbxPlacementLights.GetName(light.type));
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle("라이트 켜기", light.enabled);
            Color color = EditorGUILayout.ColorField("색상", light.color);
            float intensity = Mathf.Max(0, EditorGUILayout.FloatField("밝기", light.intensity));
            bool area = FbxPlacementLights.IsArea(light.type);
            LightmapBakeType mode = area ? LightmapBakeType.Baked :
                (LightmapBakeType)EditorGUILayout.EnumPopup("Mode", light.lightmapBakeType);
            float range = light.range;
            if (light.type != LightType.Directional) range = Mathf.Max(.01f, EditorGUILayout.FloatField("범위", range));
            float angle = light.spotAngle, inner = light.innerSpotAngle;
            if (light.type == LightType.Spot)
            {
                angle = EditorGUILayout.Slider("바깥 각도", angle, 1, 179);
                inner = EditorGUILayout.Slider("안쪽 각도", inner, 0, angle);
            }
            Vector2 size = light.areaSize;
            if (area) size = EditorGUILayout.Vector2Field("Area 크기", size);
            LightShadows shadows = (LightShadows)EditorGUILayout.EnumPopup("그림자", light.shadows);
            float strength = EditorGUILayout.Slider("그림자 강도", light.shadowStrength, 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(light, "맵 라이트 설정");
                light.enabled = enabled;
                light.color = color;
                light.intensity = intensity;
                light.lightmapBakeType = mode;
                light.range = range;
                light.spotAngle = angle;
                light.innerSpotAngle = inner;
                light.areaSize = new Vector2(Mathf.Max(.01f, size.x), Mathf.Max(.01f, size.y));
                light.shadows = shadows;
                light.shadowStrength = strength;
                Changed();
            }
            if (mode == LightmapBakeType.Baked)
                EditorGUILayout.HelpBox("Baked 라이트는 베이크 후 적용됩니다. 실시간으로 확인하려면 Realtime 또는 Mixed를 사용하세요.", MessageType.Info);
            if (!useWorkspaceLighting)
                EditorGUILayout.HelpBox("상단의 '씬 조명'을 켜면 배치한 라이트가 반영됩니다.", MessageType.Info);
        }

        private void DrawWorkspaceLights()
        {
            foreach (Light light in FbxPlacementLights.GetLights(workspaceRoot))
            {
                if (!light.gameObject.activeInHierarchy) continue;
                float size = HandleUtility.GetHandleSize(light.transform.position) * .08f;
                using (new Handles.DrawingScope(light.enabled ? new Color(1, .85f, .25f) : Color.gray))
                {
                    if (Handles.Button(light.transform.position, Quaternion.identity, size, size * 1.5f, Handles.SphereHandleCap))
                        Select(light.transform);
                    Handles.Label(light.transform.position + Vector3.up * size * 2, new GUIContent(light.name));
                    if (!Selection.Contains(light.gameObject)) continue;
                    if (light.type == LightType.Directional)
                        Handles.ArrowHandleCap(0, light.transform.position, light.transform.rotation, size * 8, EventType.Repaint);
                    else if (light.type == LightType.Point)
                    {
                        Handles.DrawWireDisc(light.transform.position, Vector3.up, light.range);
                        Handles.DrawWireDisc(light.transform.position, Vector3.right, light.range);
                    }
                    else if (light.type == LightType.Spot)
                    {
                        Vector3 end = light.transform.position + light.transform.forward * light.range;
                        float radius = Mathf.Tan(light.spotAngle * Mathf.Deg2Rad * .5f) * light.range;
                        Handles.DrawWireDisc(end, light.transform.forward, radius);
                        Handles.DrawLine(light.transform.position, end + light.transform.right * radius);
                        Handles.DrawLine(light.transform.position, end - light.transform.right * radius);
                    }
                }
            }
        }
    }
}
