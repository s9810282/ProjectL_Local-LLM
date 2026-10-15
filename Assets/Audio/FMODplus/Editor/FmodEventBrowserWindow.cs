#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// FMOD 이벤트 카탈로그를 폴더 계층 형태로 보여주는 브라우저
/// 일반 브라우저 모드와 선택 모드 지원.

public class FmodEventBrowserWindow : EditorWindow
{
    private static Action<FmodScannedEventEntry> _onEventPicked;
    private static FmodEventCatalog _requestedCatalog;
    private static string _requestedTitle;
    private static string _currentSelectedPath;

    private FmodEventCatalog _catalog;
    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private string _searchText = "";
    private FmodScannedEventEntry _selectedEvent;

    private readonly Dictionary<string, bool> _folderFoldouts = new();

    [MenuItem("FMOD/Plus/Event Browser")]
    public static void OpenWindow()
    {
        FmodEventBrowserWindow window = GetWindow<FmodEventBrowserWindow>("FMOD Event Browser");
        _onEventPicked = null;
        _requestedCatalog = null;
        _requestedTitle = null;
        _currentSelectedPath = null;
        window.minSize = new Vector2(520, 320);
    }

    public static void OpenPicker(
        FmodEventCatalog catalog,
        Action<FmodScannedEventEntry> onPicked,
        string title = "Select FMOD Event",
        string currentSelectedPath = null)
    {
        FmodEventBrowserWindow window = GetWindow<FmodEventBrowserWindow>(true, title, true);

        _requestedCatalog = catalog;
        _onEventPicked = onPicked;
        _requestedTitle = title;
        _currentSelectedPath = currentSelectedPath;

        window._catalog = catalog;
        window._selectedEvent = null;
        window.TryRestoreSelectedEventFromPath(currentSelectedPath);
        window.minSize = new Vector2(520, 320);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_catalog == null)
        {
            EditorGUILayout.HelpBox(
                "카탈로그를 선택한 뒤 FmodEventCatalog asset을 지정하십시오.",
                MessageType.Info
            );
            return;
        }

        EditorGUILayout.Space();

        bool narrowMode = position.width < 700f;

        if (narrowMode)
        {
            DrawLeftPanelLayout(GUILayout.MinHeight(220));
            EditorGUILayout.Space(6);
            DrawRightPanelLayout(GUILayout.MinHeight(220));
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanelLayout(GUILayout.MinWidth(260), GUILayout.Width(position.width * 0.45f));
            DrawRightPanelLayout(GUILayout.MinWidth(260), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        _catalog = (FmodEventCatalog)EditorGUILayout.ObjectField(
            "Catalog",
            _catalog,
            typeof(FmodEventCatalog),
            false
        );

        _searchText = EditorGUILayout.TextField("Search", _searchText);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Use Selected Catalog"))
        {
            if (Selection.activeObject is FmodEventCatalog selectedCatalog)
            {
                _catalog = selectedCatalog;
                _selectedEvent = null;
            }
        }

        if (GUILayout.Button("Clear Selection"))
        {
            _selectedEvent = null;
        }

        EditorGUILayout.EndHorizontal();

        if (_onEventPicked != null)
        {
            string currentInfo = string.IsNullOrWhiteSpace(_currentSelectedPath)
                ? "현재 선택된 이벤트 없음"
                : $"현재 선택: {_currentSelectedPath}";

            EditorGUILayout.HelpBox(
                "현재 이벤트 선택 모드입니다. 왼쪽에서 이벤트를 클릭하여 적용하십시오.\n" + currentInfo,
                MessageType.Info
            );
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawLeftPanelLayout(params GUILayoutOption[] options)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, options);
        EditorGUILayout.LabelField("Hierarchy", EditorStyles.boldLabel);

        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

        FolderNode root = BuildTree(_catalog.events, _searchText);

        foreach (var childFolder in root.children.Values)
        {
            DrawFolderNodeRecursive(childFolder, 0);
        }

        foreach (var evt in root.events)
        {
            DrawEventButton(evt, 0);
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPanelLayout(params GUILayoutOption[] options)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, options);
        EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);

        _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

        if (_selectedEvent == null)
        {
            EditorGUILayout.HelpBox("왼쪽에서 이벤트를 선택해주십시오.", MessageType.Info);
        }
        else
        {
            DrawSelectableInfo("Display Name", _selectedEvent.displayName);
            DrawSelectableInfo("Event Name", _selectedEvent.eventName);
            DrawSelectableInfo("Folder", _selectedEvent.folderPath);
            DrawSelectableInfo("Path", _selectedEvent.path);
            DrawSelectableInfo("GUID", _selectedEvent.guidString);
            DrawSelectableInfo("Source Bank", _selectedEvent.sourceBankPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Event Info", EditorStyles.boldLabel);
            EditorGUILayout.Toggle("One Shot", _selectedEvent.isOneShot);
            EditorGUILayout.Toggle("Snapshot", _selectedEvent.isSnapshot);
            EditorGUILayout.Toggle("3D", _selectedEvent.is3D);
            EditorGUILayout.Toggle("Has Sustain Point", _selectedEvent.hasSustainPoint);
            EditorGUILayout.IntField("Length (ms)", _selectedEvent.lengthMs);
            EditorGUILayout.FloatField("Min Distance", _selectedEvent.minDistance);
            EditorGUILayout.FloatField("Max Distance", _selectedEvent.maxDistance);

            EditorGUILayout.Space();

            if (GUILayout.Button("Copy Path"))
            {
                EditorGUIUtility.systemCopyBuffer = _selectedEvent.path;
            }

            if (GUILayout.Button("Copy GUID"))
            {
                EditorGUIUtility.systemCopyBuffer = _selectedEvent.guidString;
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectableInfo(string label, string value)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(
            string.IsNullOrEmpty(value) ? "-" : value,
            EditorStyles.textField,
            GUILayout.Height(EditorGUIUtility.singleLineHeight * 2.2f)
        );
        EditorGUILayout.Space(2);
    }

    private void DrawFolderNodeRecursive(FolderNode node, int indentLevel)
    {
        string key = node.fullPath;

        if (!_folderFoldouts.ContainsKey(key))
            _folderFoldouts[key] = true;

        EditorGUI.indentLevel = indentLevel;
        _folderFoldouts[key] = EditorGUILayout.Foldout(
            _folderFoldouts[key],
            $"{node.name} ({node.TotalEventCountRecursive})",
            true
        );

        if (_folderFoldouts[key])
        {
            foreach (var evt in node.events)
            {
                DrawEventButton(evt, indentLevel + 1);
            }

            foreach (var child in node.children.Values)
            {
                DrawFolderNodeRecursive(child, indentLevel + 1);
            }
        }

        EditorGUI.indentLevel = 0;
    }

    private void DrawEventButton(FmodScannedEventEntry evt, int indentLevel)
    {
        bool isSelected = _selectedEvent == evt;

        GUIStyle style = isSelected
            ? new GUIStyle(EditorStyles.miniButtonMid) { alignment = TextAnchor.MiddleLeft }
            : new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleLeft };

        float indentWidth = 18f * indentLevel;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(indentWidth);

        if (GUILayout.Button($"＊ {evt.eventName}", style, GUILayout.MinHeight(20)))
        {
            _selectedEvent = evt;

            if (_onEventPicked != null)
            {
                _onEventPicked.Invoke(evt);

                _onEventPicked = null;
                _requestedCatalog = null;
                _requestedTitle = null;
                _currentSelectedPath = null;

                Close();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private FolderNode BuildTree(List<FmodScannedEventEntry> events, string searchText)
    {
        FolderNode root = new FolderNode("ROOT", "");

        string lowerSearch = string.IsNullOrWhiteSpace(searchText)
            ? ""
            : searchText.ToLowerInvariant();

        foreach (var evt in events)
        {
            if (_catalog != null && _catalog.UsesBankFilter && !_catalog.AllowsEvent(evt))
                continue;

            if (!MatchesSearch(evt, lowerSearch))
                continue;

            AddEventToTree(root, evt);
        }

        return root;
    }

    private bool MatchesSearch(FmodScannedEventEntry evt, string lowerSearch)
    {
        if (string.IsNullOrEmpty(lowerSearch))
            return true;

        return (evt.path != null && evt.path.ToLowerInvariant().Contains(lowerSearch))
            || (evt.folderPath != null && evt.folderPath.ToLowerInvariant().Contains(lowerSearch))
            || (evt.eventName != null && evt.eventName.ToLowerInvariant().Contains(lowerSearch));
    }

    private void AddEventToTree(FolderNode root, FmodScannedEventEntry evt)
    {
        if (string.IsNullOrWhiteSpace(evt.folderPath))
        {
            root.events.Add(evt);
            return;
        }

        string[] parts = evt.folderPath.Split('/');
        FolderNode current = root;
        string currentPath = "";

        foreach (string part in parts)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

            if (!current.children.TryGetValue(part, out FolderNode child))
            {
                child = new FolderNode(part, currentPath);
                current.children.Add(part, child);
            }

            current = child;
        }

        current.events.Add(evt);
    }

    private void TryRestoreSelectedEventFromPath(string eventPath)
    {
        if (_catalog == null || string.IsNullOrWhiteSpace(eventPath))
        {
            _selectedEvent = null;
            return;
        }

        FmodScannedEventEntry entry = _catalog.FindByPath(eventPath);
        _selectedEvent = entry != null && _catalog.AllowsEvent(entry)
            ? entry
            : null;
    }

    private class FolderNode
    {
        public string name;
        public string fullPath;
        public Dictionary<string, FolderNode> children = new();
        public List<FmodScannedEventEntry> events = new();

        public FolderNode(string name, string fullPath)
        {
            this.name = name;
            this.fullPath = fullPath;
        }

        public int TotalEventCountRecursive
        {
            get
            {
                int count = events.Count;
                foreach (var child in children.Values)
                    count += child.TotalEventCountRecursive;
                return count;
            }
        }
    }
}
#endif
