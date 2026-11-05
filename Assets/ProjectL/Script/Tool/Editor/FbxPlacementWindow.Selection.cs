using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        private bool _selectionPressed;
        private bool _selectionMarquee;
        private Vector2 _selectionStart;
        private Vector2 _selectionEnd;
        private Transform[] _selectionBefore;
        private FbxPlacementSelection.Mode _selectionMode;

        private void DrawWorkspaceSelection()
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID("FbxWorkspaceSelection".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.Layout && !e.alt && Tools.current != UnityEditor.Tool.View)
                HandleUtility.AddDefaultControl(id);
            if (e.type == EventType.Repaint && _selectionPressed && _selectionMarquee)
            {
                Rect rect = SelectionRect(_selectionStart, _selectionEnd);
                Handles.BeginGUI();
                EditorGUI.DrawRect(rect, new Color(.2f, .55f, 1, .15f));
                Color edge = new Color(.3f, .7f, 1);
                EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, 1), edge);
                EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - 1, rect.width, 1), edge);
                EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, 1, rect.height), edge);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.yMin, 1, rect.height), edge);
                Handles.EndGUI();
            }
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt &&
                Tools.current != UnityEditor.Tool.View && !Tools.viewToolActive &&
                GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id)
            {
                _selectionPressed = true;
                _selectionMarquee = false;
                _selectionStart = _selectionEnd = e.mousePosition;
                _selectionBefore = Selection.transforms;
                _selectionMode = (e.control || e.command) ? FbxPlacementSelection.Mode.Toggle :
                    e.shift ? FbxPlacementSelection.Mode.Add : FbxPlacementSelection.Mode.Replace;
                GUIUtility.hotControl = id;
                e.Use();
            }
            if (!_selectionPressed || GUIUtility.hotControl != id) return;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                ReleaseSelectionDrag();
                e.Use();
            }
            else if (e.type == EventType.MouseDrag)
            {
                _selectionEnd = e.mousePosition;
                _selectionMarquee |= (_selectionEnd - _selectionStart).sqrMagnitude > 16;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                _selectionEnd = e.mousePosition;
                List<Transform> hits;
                if (_selectionMarquee)
                {
                    Vector2 a = HandleUtility.GUIPointToScreenPixelCoordinate(_selectionStart);
                    Vector2 b = HandleUtility.GUIPointToScreenPixelCoordinate(_selectionEnd);
                    Rect viewport = camera.pixelRect;
                    a = new Vector2((a.x - viewport.x) / viewport.width, (a.y - viewport.y) / viewport.height);
                    b = new Vector2((b.x - viewport.x) / viewport.width, (b.y - viewport.y) / viewport.height);
                    hits = FbxPlacementSelection.FindInRect(workspaceRoot, camera, SelectionRect(a, b));
                }
                else
                {
                    hits = new List<Transform>();
                    Ray ray = HandleUtility.GUIPointToWorldRay(_selectionEnd);
                    float nearest = float.PositiveInfinity;
                    Transform picked = null;
                    foreach (Renderer renderer in workspaceRoot.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled || !renderer.bounds.IntersectRay(ray, out float distance) || distance >= nearest) continue;
                        Transform placed = FbxPlacementSelection.GetPlacedObject(workspaceRoot, renderer.transform);
                        if (placed == null) continue;
                        nearest = distance;
                        picked = placed;
                    }
                    if (picked != null) hits.Add(picked);
                }
                SelectMany(FbxPlacementSelection.Combine(workspaceRoot, _selectionBefore, hits, _selectionMode));
                ReleaseSelectionDrag();
                e.Use();
            }
        }

        private void ReleaseSelectionDrag()
        {
            _selectionPressed = false;
            _selectionMarquee = false;
            GUIUtility.hotControl = 0;
            Repaint();
        }

        private static Rect SelectionRect(Vector2 a, Vector2 b) => Rect.MinMaxRect(
            Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));

        private void SelectMany(List<Transform> targets)
        {
            UnityEngine.Object[] objects = new UnityEngine.Object[targets.Count];
            for (int i = 0; i < targets.Count; i++)
            {
                objects[i] = targets[i].gameObject;
                Transform parent = targets[i].parent;
                if (parent != null) _expanded.Add(parent.GetInstanceID());
            }
            selected = targets.Count > 0 ? targets[0] : null;
            Selection.objects = objects;
            Repaint();
        }

        private List<Transform> GetWorkspaceSelection()
        {
            List<Transform> targets = new List<Transform>();
            foreach (Transform target in Selection.transforms)
                if (Owns(target)) targets.Add(target);
            return targets;
        }

        private void FrameSelection()
        {
            List<Transform> targets = GetWorkspaceSelection();
            if (targets.Count == 0) return;
            Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
            foreach (Transform target in targets)
            {
                Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) bounds.Encapsulate(target.position);
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            }
            Frame(bounds, false);
        }

        private void DeleteSelection()
        {
            List<Transform> targets = GetWorkspaceSelection();
            foreach (Transform target in targets) if (target == workspaceRoot.transform) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            foreach (Transform target in targets) Undo.DestroyObjectImmediate(target.gameObject);
            Undo.CollapseUndoOperations(group);
            Select(null);
            Changed();
        }
    }
}
