using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // 상대 모양만 편집합니다. 확정 목록의 Undo/저장과 기믹 규칙은 호출자가 맡습니다(설계 §23).
    internal sealed class CombatStageSoGimmickShapeEditor
    {
        private static readonly string[] Tools = { "칠하기", "지우기" };
        private const double DefaultScale = 28;
        private int _tool, _controlId;
        private double _left, _bottom, _scale = DefaultScale;
        private bool _hasView, _fitRequested, _panning;
        private Rect _gestureRect;
        private List<Vector2Int> _stroke;
        private HashSet<Vector2Int> _cells, _before;
        private Vector2Int? _lastCell;
        private string _notice;

        internal bool IsInteracting => _stroke != null || _panning;

        internal void Cancel()
        {
            if (IsInteracting && GUIUtility.hotControl == _controlId) GUIUtility.hotControl = 0;
            _stroke = null; _cells = null; _before = null; _lastCell = null; _panning = false;
        }

        internal void ResetView()
        {
            Cancel(); _hasView = false; _notice = null;
        }

        internal bool Draw(IReadOnlyList<Vector2Int> source, bool editable, out List<Vector2Int> changed)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            changed = null;
            if (!editable && _stroke != null) Cancel();
            using (new EditorGUI.DisabledScope(!editable))
            {
                int tool = GUILayout.Toolbar(_tool, Tools);
                if (tool != _tool) { Cancel(); _tool = tool; }
            }
            bool normalSize = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전체 모양")) { Cancel(); _fitRequested = true; }
                if (GUILayout.Button("칸 크기")) { Cancel(); normalSize = true; }
            }
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(240), GUILayout.ExpandWidth(true));
            if (IsInteracting && (GUIUtility.hotControl != _controlId || (Event.current.type != EventType.Layout && rect != _gestureRect))) Cancel();
            _controlId = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive, rect);
            if (Event.current.type != EventType.Layout && rect.width > 1 && (!_hasView || _fitRequested)) Fit(source, rect);
            if (normalSize)
            {
                _left += rect.width / (2 * _scale) - rect.width / (2 * DefaultScale);
                _bottom += rect.height / (2 * _scale) - rect.height / (2 * DefaultScale);
                _scale = DefaultScale;
            }
            Handle(rect, source, editable, out changed);
            IReadOnlyList<Vector2Int> shown = _stroke ?? source;
            if (Event.current.type == EventType.Repaint) DrawCells(rect, shown, editable);
            string size = shown.Count == 0 ? "빈 모양" : $"가로 {(long)shown.Max(c => c.x) - shown.Min(c => c.x) + 1} · 세로 {(long)shown.Max(c => c.y) - shown.Min(c => c.y) + 1}";
            EditorGUILayout.LabelField($"{shown.Count}칸 · {size}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("휠: 확대/축소 · 가운데 버튼 또는 Alt+끌기: 보기 이동", EditorStyles.wordWrappedMiniLabel);
            string note = _notice ?? (shown.Count == 0 ? "빈 모양입니다. 사용 중인 기믹은 칸을 그려야 검사를 통과할 수 있습니다."
                : _scale < 8 ? "전체 모양을 보는 중입니다. 칠하려는 곳에 휠로 확대하세요." : "왼쪽 클릭/드래그로 모양을 편집합니다. Esc로 진행 중인 획을 취소합니다.");
            EditorGUILayout.HelpBox(note, _notice != null || shown.Count == 0 ? MessageType.Warning : MessageType.Info);
            return changed != null;
        }

        private void Fit(IReadOnlyList<Vector2Int> source, Rect rect)
        {
            double minX = source.Count > 0 ? source.Min(c => c.x) : 0;
            double minY = source.Count > 0 ? source.Min(c => c.y) : 0;
            double width = source.Count > 0 ? (double)source.Max(c => c.x) - minX + 1 : 1;
            double height = source.Count > 0 ? (double)source.Max(c => c.y) - minY + 1 : 1;
            _scale = Math.Min(DefaultScale, Math.Min(rect.width / (width + 2), rect.height / (height + 2)));
            _left = minX - (rect.width / _scale - width) / 2;
            _bottom = minY - (rect.height / _scale - height) / 2;
            _hasView = true; _fitRequested = false;
        }

        private Vector2Int? CellAt(Rect rect, Vector2 mouse)
        {
            if (!rect.Contains(mouse)) return null;
            double x = Math.Floor(_left + (mouse.x - rect.x) / _scale);
            double y = Math.Floor(_bottom + (rect.yMax - mouse.y) / _scale);
            if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue) return null;
            return new Vector2Int((int)x, (int)y);
        }

        private void Handle(Rect rect, IReadOnlyList<Vector2Int> source, bool editable, out List<Vector2Int> changed)
        {
            changed = null;
            Event e = Event.current;
            switch (e.GetTypeForControl(_controlId))
            {
                case EventType.ScrollWheel when rect.Contains(e.mousePosition):
                    if (!IsInteracting)
                    {
                        double x = _left + (e.mousePosition.x - rect.x) / _scale;
                        double y = _bottom + (rect.yMax - e.mousePosition.y) / _scale;
                        _scale = Math.Max(1e-8, Math.Min(64, _scale * Math.Pow(1.2, -e.delta.y)));
                        _left = x - (e.mousePosition.x - rect.x) / _scale;
                        _bottom = y - (rect.yMax - e.mousePosition.y) / _scale;
                    }
                    e.Use(); break;
                case EventType.MouseDown when rect.Contains(e.mousePosition):
                    bool pan = e.button == 2 || (e.button == 0 && e.alt);
                    Vector2Int? cell = CellAt(rect, e.mousePosition);
                    if (!pan && (e.button != 0 || !editable || _scale < 8 || !cell.HasValue)) return;
                    GUI.FocusControl(null);
                    GUIUtility.hotControl = _controlId;
                    _gestureRect = rect; _panning = pan; _notice = null;
                    if (!pan)
                    {
                        _before = new HashSet<Vector2Int>(source);
                        _cells = new HashSet<Vector2Int>(source);
                        _stroke = source.Distinct().ToList();
                        Paint(cell.Value); _lastCell = cell;
                    }
                    e.Use(); break;
                case EventType.MouseDrag when IsInteracting && GUIUtility.hotControl == _controlId:
                    if (_panning)
                    {
                        _left -= e.delta.x / _scale; _bottom += e.delta.y / _scale;
                    }
                    else
                    {
                        Vector2Int? next = CellAt(rect, e.mousePosition);
                        if (next.HasValue)
                        {
                            if (_lastCell.HasValue) PaintLine(_lastCell.Value, next.Value);
                            else Paint(next.Value);
                        }
                        // 밖을 지났다 들어오면 밖의 이동 경로를 격자 안으로 가로질러 잇지 않습니다.
                        _lastCell = next;
                    }
                    e.Use(); break;
                case EventType.MouseUp when IsInteracting && GUIUtility.hotControl == _controlId:
                    if (_stroke != null)
                    {
                        Vector2Int? last = CellAt(rect, e.mousePosition);
                        if (last.HasValue && _lastCell.HasValue) PaintLine(_lastCell.Value, last.Value);
                        changed = Finish(source);
                    }
                    Cancel(); e.Use(); break;
            }
        }

        private void Paint(Vector2Int cell)
        {
            if (_tool == 0)
            {
                if (_cells.Add(cell)) _stroke.Add(cell);
            }
            else if (_cells.Remove(cell)) _stroke.Remove(cell);
        }

        private void PaintLine(Vector2Int from, Vector2Int to)
        {
            long x = from.x, y = from.y, dx = Math.Abs((long)to.x - from.x), dy = -Math.Abs((long)to.y - from.y);
            int sx = from.x < to.x ? 1 : -1, sy = from.y < to.y ? 1 : -1;
            long error = dx + dy;
            while (true)
            {
                Paint(new Vector2Int((int)x, (int)y));
                if (x == to.x && y == to.y) break;
                long twice = 2 * error;
                if (twice >= dy) { error += dy; x += sx; }
                if (twice <= dx) { error += dx; y += sy; }
            }
        }

        private List<Vector2Int> Finish(IReadOnlyList<Vector2Int> source)
        {
            // 빈 칸 지우기·같은 칸 칠하기는 원본 중복/순서/원점을 바꾸지 않습니다.
            if (_before.SetEquals(_cells)) return null;
            if (_stroke.Count == 0) return new List<Vector2Int>();
            int minX = _stroke.Min(c => c.x), minY = _stroke.Min(c => c.y);
            if (_stroke.Any(c => (long)c.x - minX > int.MaxValue || (long)c.y - minY > int.MaxValue))
            {
                _notice = "모양의 상대 좌표가 정수 범위를 넘어 반영하지 않았습니다. 고급 좌표에서 범위를 줄여 주세요.";
                return null;
            }
            var normalized = _stroke.Select(c => new Vector2Int((int)((long)c.x - minX), (int)((long)c.y - minY))).ToList();
            if (normalized.SequenceEqual(source)) return null;
            // 데이터와 화면 원점을 함께 옮겨 정규화 전후 같은 자리에 그려집니다.
            _left -= minX; _bottom -= minY;
            return normalized;
        }

        private void DrawCells(Rect rect, IReadOnlyList<Vector2Int> cells, bool editable)
        {
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(.13f, .14f, .16f) : new Color(.88f, .89f, .9f));
            GUI.BeginClip(rect);
            try
            {
                Color grid = new Color(.5f, .5f, .5f, .3f);
                if (_scale >= 8)
                {
                    double firstX = (Math.Ceiling(_left) - _left) * _scale;
                    double firstY = (Math.Ceiling(_bottom) - _bottom) * _scale;
                    for (double x = firstX; x < rect.width; x += _scale) EditorGUI.DrawRect(new Rect((float)x, 0, 1, rect.height), grid);
                    for (double y = firstY; y < rect.height; y += _scale) EditorGUI.DrawRect(new Rect(0, rect.height - (float)y, rect.width, 1), grid);
                }
                Color fill = editable ? new Color(.27f, .62f, .69f, .85f) : new Color(.5f, .5f, .5f, .65f);
                foreach (Vector2Int cell in cells)
                {
                    double x = (cell.x - _left) * _scale;
                    double y = rect.height - (cell.y - _bottom + 1) * _scale;
                    if (x >= rect.width || y >= rect.height || x + _scale < 0 || y + _scale < 0) continue;
                    float inset = _scale >= 8 ? 1 : 0;
                    float size = (float)Math.Max(1, _scale - inset * 2);
                    EditorGUI.DrawRect(new Rect((float)x + inset, (float)y + inset, size, size), fill);
                }
            }
            finally { GUI.EndClip(); }
        }
    }
}
