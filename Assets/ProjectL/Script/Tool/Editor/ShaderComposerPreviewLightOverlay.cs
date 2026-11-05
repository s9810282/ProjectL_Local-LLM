using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // 실제 프리뷰 Light의 값을 읽어 이미지 위에만 표시한다. 입력과 씬 객체는 소유하지 않는다.
    internal sealed class ShaderComposerPreviewLightOverlay
    {
        private const int CircleSegments = 32;
        private static readonly Vector2[] UnitCircle = CreateCircle();
        private readonly Vector3[] _line = new Vector3[2];
        private readonly GUIContent[] _labels = new GUIContent[ShaderComposerPreviewSettings.MaximumLights];
        private readonly LightType[] _labelTypes = new LightType[ShaderComposerPreviewSettings.MaximumLights];
        private readonly bool[] _labelEnabled = new bool[ShaderComposerPreviewSettings.MaximumLights];
        private readonly bool[] _labelOutside = new bool[ShaderComposerPreviewSettings.MaximumLights];
        private GUIStyle _labelStyle;
        private Camera _camera;
        private Rect _rect;

        internal void Draw(Rect rect, Camera camera, Bounds bounds, IReadOnlyList<Light> lights, bool showRanges)
        {
            _camera = camera;
            _rect = new Rect(0, 0, rect.width, rect.height);
            if (_labelStyle == null)
                _labelStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
            Color savedColor = Handles.color;
            Matrix4x4 savedMatrix = Handles.matrix;
            GUI.BeginGroup(rect);
            Handles.BeginGUI();
            try
            {
                Handles.matrix = Matrix4x4.identity;
                float radius = Mathf.Max(0.001f, bounds.extents.magnitude);
                for (int i = 0; i < Mathf.Min(lights.Count, _labels.Length); i++)
                {
                    Light light = lights[i];
                    if (light == null) continue;
                    Color.RGBToHSV(light.color, out float hue, out float saturation, out float value);
                    Color tint = light.enabled ? Color.HSVToRGB(hue, saturation, Mathf.Max(0.7f, value)) : Color.gray;
                    Handles.color = new Color(tint.r, tint.g, tint.b, light.enabled ? 0.65f : 0.3f);
                    Vector3 forward = light.transform.forward;
                    Vector3 position = light.transform.position;
                    if (light.type == LightType.Directional)
                    {
                        position = bounds.center - forward * radius * 1.65f;
                        Arrow(position, forward, radius * 0.75f);
                    }
                    else
                    {
                        if (light.type == LightType.Spot) Arrow(position, forward, radius * 0.55f);
                        if (showRanges)
                        {
                            if (light.type == LightType.Point)
                            {
                                Circle(position, Vector3.right, Vector3.up, light.range);
                                Circle(position, Vector3.right, Vector3.forward, light.range);
                                Circle(position, Vector3.up, Vector3.forward, light.range);
                            }
                            else if (light.type == LightType.Spot)
                            {
                                Cone(light, light.spotAngle, true);
                                Cone(light, light.innerSpotAngle, false);
                            }
                        }
                    }
                    Handles.color = tint;
                    Marker(position, i, light);
                }
            }
            finally
            {
                Handles.color = savedColor;
                Handles.matrix = savedMatrix;
                Handles.EndGUI();
                GUI.EndGroup();
            }
        }

        private static Vector2[] CreateCircle()
        {
            var points = new Vector2[CircleSegments + 1];
            for (int i = 0; i <= CircleSegments; i++)
            {
                float angle = i * (Mathf.PI * 2 / CircleSegments);
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            return points;
        }

        private void Arrow(Vector3 origin, Vector3 direction, float length)
        {
            Vector3 end = origin + direction * length;
            Vector3 side = Vector3.Cross(direction, _camera.transform.forward);
            if (side.sqrMagnitude < 0.001f) side = Vector3.Cross(direction, _camera.transform.up);
            side = side.normalized * length * 0.18f;
            Line(origin, end);
            Line(end, end - direction * length * 0.3f + side);
            Line(end, end - direction * length * 0.3f - side);
        }

        private void Cone(Light light, float angle, bool spokes)
        {
            if (angle <= 0) return;
            // 실측 범위는 광원 중심으로부터의 거리다. 경계도 같은 거리의 구면 위에 둔다.
            float halfAngle = angle * Mathf.Deg2Rad * 0.5f;
            float radius = Mathf.Sin(halfAngle) * light.range;
            Vector3 origin = light.transform.position;
            Vector3 center = origin + light.transform.forward * (Mathf.Cos(halfAngle) * light.range);
            Vector3 right = light.transform.right;
            Vector3 up = light.transform.up;
            Circle(center, right, up, radius);
            if (!spokes) return;
            Line(origin, center + right * radius);
            Line(origin, center - right * radius);
            Line(origin, center + up * radius);
            Line(origin, center - up * radius);
        }

        private void Circle(Vector3 center, Vector3 right, Vector3 up, float radius)
        {
            Vector3 previous = center + right * radius;
            for (int i = 1; i <= CircleSegments; i++)
            {
                Vector3 next = center + (right * UnitCircle[i].x + up * UnitCircle[i].y) * radius;
                Line(previous, next);
                previous = next;
            }
        }

        private void Line(Vector3 from, Vector3 to)
        {
            // 카메라 뒤쪽 좌표의 투영 반전과 near plane 부근의 폭주를 막는다.
            Vector3 viewFrom = _camera.transform.InverseTransformPoint(from);
            Vector3 viewTo = _camera.transform.InverseTransformPoint(to);
            float near = _camera.nearClipPlane + 0.00001f;
            if (viewFrom.z < near && viewTo.z < near) return;
            if (viewFrom.z < near)
                from = Vector3.Lerp(from, to, (near - viewFrom.z) / (viewTo.z - viewFrom.z));
            else if (viewTo.z < near)
                to = Vector3.Lerp(from, to, (viewFrom.z - near) / (viewFrom.z - viewTo.z));
            Vector3 first = Project(from);
            Vector3 last = Project(to);
            Vector3 delta = last - first;
            float enter = 0;
            float exit = 1;
            if (!Clip(-delta.x, first.x, ref enter, ref exit) ||
                !Clip(delta.x, _rect.width - first.x, ref enter, ref exit) ||
                !Clip(-delta.y, first.y, ref enter, ref exit) ||
                !Clip(delta.y, _rect.height - first.y, ref enter, ref exit)) return;
            _line[0] = first + delta * enter;
            _line[1] = first + delta * exit;
            Handles.DrawAAPolyLine(1.5f, _line);
        }

        private static bool Clip(float direction, float distance, ref float enter, ref float exit)
        {
            if (Mathf.Abs(direction) < 0.000001f) return distance >= 0;
            float t = distance / direction;
            if (direction < 0) enter = Mathf.Max(enter, t);
            else exit = Mathf.Min(exit, t);
            return enter <= exit;
        }

        private Vector3 Project(Vector3 world)
        {
            Vector3 point = _camera.WorldToViewportPoint(world);
            return new Vector3(point.x * _rect.width, (1 - point.y) * _rect.height, 0);
        }

        private void Marker(Vector3 world, int index, Light light)
        {
            Vector3 viewport = _camera.WorldToViewportPoint(world);
            Vector3 point = Project(world);
            bool outside = viewport.z <= _camera.nearClipPlane || !_rect.Contains(point);
            if (viewport.z <= _camera.nearClipPlane)
            {
                point.x = _rect.width - point.x;
                point.y = _rect.height - point.y;
            }
            point.x = Mathf.Clamp(point.x, 12, _rect.width - 12);
            point.y = Mathf.Clamp(point.y, 12, _rect.height - 12);
            _line[0] = point + Vector3.left * 5; _line[1] = point + Vector3.right * 5;
            Handles.DrawAAPolyLine(2.5f, _line);
            _line[0] = point + Vector3.up * 5; _line[1] = point + Vector3.down * 5;
            Handles.DrawAAPolyLine(2.5f, _line);
            if (_labels[index] == null || _labelTypes[index] != light.type || _labelEnabled[index] != light.enabled || _labelOutside[index] != outside)
            {
                if (_labels[index] == null) _labels[index] = new GUIContent();
                _labels[index].text = (index + 1) + " · " + light.type + (light.enabled ? "" : " (꺼짐)") + (outside ? " / 화면 밖" : "");
                _labelTypes[index] = light.type;
                _labelEnabled[index] = light.enabled;
                _labelOutside[index] = outside;
            }
            Vector2 size = _labelStyle.CalcSize(_labels[index]) + new Vector2(8, 4);
            var labelRect = new Rect(Mathf.Clamp(point.x + 9, 0, Mathf.Max(0, _rect.width - size.x)),
                Mathf.Clamp(point.y + 7, 0, Mathf.Max(0, _rect.height - size.y)), size.x, size.y);
            EditorGUI.DrawRect(labelRect, new Color(0, 0, 0, 0.7f));
            GUI.Label(labelRect, _labels[index], _labelStyle);
        }
    }
}
