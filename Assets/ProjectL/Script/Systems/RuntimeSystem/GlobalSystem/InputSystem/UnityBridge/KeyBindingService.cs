using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityInput = UnityEngine.InputSystem.InputSystem;
using RebindingOperation = UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.UnityBridge
{
    /// <summary>Bridge 소유 복제 에셋만 변경하고 키 입력 대기의 자원을 해제합니다.</summary>
    internal sealed class KeyBindingService : IKeyBindingService, IDisposable
    {
        internal const string PreferencesKey = "ProjectL.Input.KeyBindings.v1";
        private readonly KeyBindingDefinition[] _definitions;
        private readonly string _preferencesKey;
        private readonly HashSet<string> _allowedPaths = new(StringComparer.OrdinalIgnoreCase);
        private Entry[] _entries;
        private string[] _snapshot;
        private RebindingOperation _operation;
        private IKeyBindingReceiver _receiver;
        private int _captureIndex;
        private string _candidatePath;

        [Serializable]
        private sealed class SavedState
        {
            public int version = 1;
            public SavedEntry[] entries;
        }

        [Serializable]
        private sealed class SavedEntry
        {
            public KeyBindingAction action;
            public string path;
        }

        private readonly struct Entry
        {
            internal InputAction Action { get; }
            internal int BindingIndex { get; }
            internal string Path => Action.bindings[BindingIndex].effectivePath;

            internal Entry(InputAction action, int bindingIndex)
            {
                Action = action;
                BindingIndex = bindingIndex;
            }
        }

        internal KeyBindingService(IReadOnlyList<KeyBindingDefinition> definitions, string preferencesKey = PreferencesKey)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            _definitions = new KeyBindingDefinition[definitions.Count];
            for (int i = 0; i < definitions.Count; i++) _definitions[i] = definitions[i];
            _preferencesKey = preferencesKey ?? throw new ArgumentNullException(nameof(preferencesKey));
        }

        internal void Initialize(InputActionAsset actions)
        {
            if (actions == null) throw new ArgumentNullException(nameof(actions));
            if (_entries != null) throw new InvalidOperationException("[KeyBindingService] 이미 초기화했습니다.");
            // 장치가 연결되지 않아도 저장 값을 검증하도록 Keyboard 레이아웃에서 후보를 만듭니다.
            var layout = UnityInput.LoadLayout("Keyboard");
            foreach (var control in layout.controls)
            {
                string name = control.name.ToString();
                if (control.layout.ToString() == "Key" && !IsForbidden(name))
                    _allowedPaths.Add("<Keyboard>/" + name);
            }

            var entries = new Entry[_definitions.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                KeyBindingDefinition definition = _definitions[i];
                for (int j = 0; j < i; j++)
                    if (_definitions[j].Action == definition.Action)
                        throw new InvalidOperationException("[KeyBindingService] 키 설정 대상이 중복됩니다.");
                InputAction action = actions.FindActionMap(definition.Map, true).FindAction(definition.Name, true);
                int index = -1;
                for (int j = 0; j < action.bindings.Count; j++)
                {
                    var binding = action.bindings[j];
                    if (binding.isComposite || binding.isPartOfComposite || !_allowedPaths.Contains(binding.path)) continue;
                    if (index != -1) throw new InvalidOperationException("[KeyBindingService] 키보드 바인딩은 1개여야 합니다.");
                    index = j;
                }
                if (index == -1) throw new InvalidOperationException($"[KeyBindingService] 키가 없습니다: {definition.Map}.{definition.Name}");
                entries[i] = new Entry(action, index);
            }
            _entries = entries;
            LoadSaved();
        }

        public bool HasChanges
        {
            get
            {
                if (_snapshot == null || _entries == null) return false;
                for (int i = 0; i < _entries.Length; i++)
                    if (!SamePath(_snapshot[i], _entries[i].Path)) return true;
                return false;
            }
        }

        public string GetKeyText(KeyBindingAction action)
        {
            Entry entry = _entries[Find(action)];
            return entry.Action.GetBindingDisplayString(entry.BindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        public void BeginEdit()
        {
            EnsureInitialized();
            if (_snapshot != null) throw new InvalidOperationException("[KeyBindingService] 이미 편집을 시작했습니다.");
            _snapshot = ReadPaths();
        }

        public void SaveEdit()
        {
            EnsureInitialized();
            CancelCapture();
            if (_snapshot == null) return;
            var saved = new SavedState { entries = new SavedEntry[_entries.Length] };
            for (int i = 0; i < _entries.Length; i++)
                saved.entries[i] = new SavedEntry { action = _definitions[i].Action, path = _entries[i].Path };
            PlayerPrefs.SetString(_preferencesKey, JsonUtility.ToJson(saved));
            PlayerPrefs.Save();
            _snapshot = null;
        }

        public void CancelEdit()
        {
            CancelCapture();
            if (_snapshot == null) return;
            ApplyPaths(_snapshot);
            _snapshot = null;
        }

        public void BeginCapture(KeyBindingAction action, IKeyBindingReceiver receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            int index = Find(action);
            if (_snapshot == null) throw new InvalidOperationException("[KeyBindingService] 편집을 시작해야 합니다.");
            CancelCapture();
            _captureIndex = index;
            _receiver = receiver;
            // 키 전용 임시 구독입니다. 원래 액션의 활성 상태와 별개로 검증 후 override합니다.
            _operation = new RebindingOperation()
                .WithExpectedControlType<KeyControl>()
                .WithControlsHavingToMatchPath("<Keyboard>/*")
                .WithMatchingEventsBeingSuppressed()
                .OnPotentialMatch(OnPotentialMatch)
                .OnApplyBinding(ApplyCandidate)
                .OnComplete(OnComplete);
            try { _operation.Start(); }
            catch { CancelCapture(); throw; }
        }

        public void CancelCapture()
        {
            RebindingOperation operation = _operation;
            _operation = null;
            _receiver = null;
            _candidatePath = null;
            operation?.Dispose();
        }

        private void OnPotentialMatch(RebindingOperation operation)
        {
            // ESC도 이 이벤트에서 소비한 뒤, 캡처를 끝내고 옵션 닫기 요청으로 전달합니다.
            for (int i = 0; i < operation.candidates.Count; i++)
            {
                if (operation.candidates[i] is KeyControl key && key.keyCode == Key.Escape)
                {
                    IKeyBindingReceiver receiver = _receiver;
                    CancelCapture();
                    receiver?.KeyBindingReceived(KeyBindingResult.CloseRequested);
                    return;
                }
            }
            KeyBindingResult result = KeyBindingResult.Success;
            string path = null;
            for (int i = 0; i < operation.candidates.Count; i++)
            {
                string candidate = "<Keyboard>/" + operation.candidates[i].name;
                if (!_allowedPaths.Contains(candidate))
                {
                    result = KeyBindingResult.Forbidden;
                    break;
                }
                if (i == 0) path = candidate;
            }
            if (result == KeyBindingResult.Success && path != null && HasConflict(_captureIndex, path))
                result = KeyBindingResult.Duplicate;
            if (result != KeyBindingResult.Success)
            {
                while (operation.candidates.Count > 0) operation.RemoveCandidate(operation.candidates[0]);
                _receiver.KeyBindingReceived(result);
                return;
            }
            if (path == null) return;
            _candidatePath = path;
            operation.Complete();
        }

        private void ApplyCandidate(RebindingOperation operation, string ignoredPath)
        {
            Entry entry = _entries[_captureIndex];
            entry.Action.ApplyBindingOverride(entry.BindingIndex, _candidatePath);
        }

        private void OnComplete(RebindingOperation operation)
        {
            IKeyBindingReceiver receiver = _receiver;
            CancelCapture();
            receiver?.KeyBindingReceived(KeyBindingResult.Success);
        }

        private void LoadSaved()
        {
            if (_entries.Length == 0 || !PlayerPrefs.HasKey(_preferencesKey)) return;
            try
            {
                var state = JsonUtility.FromJson<SavedState>(PlayerPrefs.GetString(_preferencesKey));
                if (state == null || state.version != 1 || state.entries == null || state.entries.Length != _entries.Length)
                    throw new InvalidOperationException("저장 형식이 맞지 않습니다.");
                var paths = new string[_entries.Length];
                foreach (SavedEntry saved in state.entries)
                {
                    if (saved == null) throw new InvalidOperationException("저장 항목이 없습니다.");
                    int index = Find(saved.action);
                    if (paths[index] != null || !_allowedPaths.Contains(saved.path))
                        throw new InvalidOperationException("저장 키가 잘못됐습니다.");
                    paths[index] = saved.path;
                }
                for (int i = 0; i < paths.Length; i++)
                    if (paths[i] == null || HasConflict(i, paths[i], paths))
                        throw new InvalidOperationException("저장 키가 중복됩니다.");
                ApplyPaths(paths);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                Debug.LogWarning($"[KeyBindingService] 저장 키를 읽을 수 없어 기본값을 사용합니다: {exception.Message}");
            }
        }

        private bool HasConflict(int index, string path, string[] paths = null)
        {
            for (int i = 0; i < _entries.Length; i++)
                if (i != index && _definitions[i].ConflictGroup == _definitions[index].ConflictGroup
                    && SamePath(paths == null ? _entries[i].Path : paths[i], path))
                    return true;
            return false;
        }

        private static bool SamePath(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        private static bool IsForbidden(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "tab":
                case "capslock":
                case "numlock":
                case "fn":
                case "leftmeta":
                case "rightmeta":
                case "leftctrl":
                case "rightctrl":
                case "leftalt":
                case "rightalt":
                case "escape":
                    return true;
                default:
                    return false;
            }
        }

        private int Find(KeyBindingAction action)
        {
            EnsureInitialized();
            for (int i = 0; i < _definitions.Length; i++)
                if (_definitions[i].Action == action) return i;
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        private void EnsureInitialized()
        {
            if (_entries == null) throw new InvalidOperationException("[KeyBindingService] 초기화가 필요합니다.");
        }

        private string[] ReadPaths()
        {
            var paths = new string[_entries.Length];
            for (int i = 0; i < paths.Length; i++) paths[i] = _entries[i].Path;
            return paths;
        }

        private void ApplyPaths(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
                _entries[i].Action.ApplyBindingOverride(_entries[i].BindingIndex, paths[i]);
        }

        public void Dispose()
        {
            CancelEdit();
            _entries = null;
            _allowedPaths.Clear();
        }
    }
}
