using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem
{
    #region 서비스

    public interface IUISystemService :
        IServiceModule
    {
        void CloseUI(UIId id);

        void OpenUI(UIId id);

        void OpenUI(UIId id, bool isFixable);

        void OpenGlobalUI();

        void OpenChildUI(UIId parentId, UIId childId);

        void CloseChildUI(UIId parentId);

        void BackCloseUI();

        void HideAllUI();
    }

    public interface IUIRegistry
    {
        void AddUI(UIId id, IUIService ui, UIId parentId = UIId.None);
    }

    #endregion

    #region 시스템 모듈

    internal sealed class UISystem :
        ISystemBindScene,
        ISystemInitData<IServiceModule>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices,
        IUISystemService,

        IUIRegistry
    {
        private readonly Dictionary<UIId, IUIService> _uis = new();
        private readonly Dictionary<UIId, GlobalUIBase> _globalUIs = new();
        private bool _hasBoundScene;
        private readonly Stack<UIId> _uiHistory = new();
        private readonly Dictionary<UIId, UIId> _parents = new();
        // History에 없는 유지 UI도 자식 UI의 부모가 될 수 있다.
        private readonly HashSet<UIId> _openRoots = new();
        private bool _hierarchyValidated;

        private UIBase[] _sceneUIs = Array.Empty<UIBase>();

        private IServiceModule _systemDataQuery;
        private ISystemServiceStoreReader _systemServiceResolver;

        private bool _isInitialized;
        private bool _isServiceBound;

        #region SystemModule

        public void BindScene()
        {
            _uis.Clear();
            _uiHistory.Clear();
            _parents.Clear();
            _openRoots.RemoveWhere(id => !_globalUIs.ContainsKey(id));
            _hierarchyValidated = false;

            foreach (var pair in _globalUIs)
                if (pair.Value != null) _uis.Add(pair.Key, pair.Value);

            UIBase[] foundUIs = UnityEngine.Object.FindObjectsByType<UIBase>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var sceneUIs = new List<UIBase>();

            for (int i = 0; i < foundUIs.Length; i++)
            {
                UIBase uiBase = foundUIs[i];

                if (uiBase == null)
                    continue;
                // GlobalUI는 최초 씬에서만 수집하고 이후 씬 바인딩에서 제외한다.
                if (_hasBoundScene && uiBase is GlobalUIBase)
                    continue;

                sceneUIs.Add(uiBase);
                uiBase.ExportUIService(this);
                uiBase.BindUISystemService(this);

                if (_systemDataQuery != null)
                {
                    uiBase.GetUIData(_systemDataQuery);
                }

                if (_systemServiceResolver != null)
                {
                    uiBase.BindUIService(_systemServiceResolver);
                }
            }
            _sceneUIs = sceneUIs.ToArray();
            ValidateHierarchy();
            _hasBoundScene = true;
            if (_isInitialized) HideClosedRegisteredUI();
        }

        public void InitData(IServiceModule systemDataQuery)
        {
            if (systemDataQuery == null)
            {
                throw new ArgumentNullException(
                    nameof(systemDataQuery));
            }

            _systemDataQuery = systemDataQuery;

            for (int i = 0; i < _sceneUIs.Length; i++)
            {
                IUIService uiBase = _sceneUIs[i];

                if (uiBase == null)
                    continue;

                uiBase.GetUIData(systemDataQuery);
            }
        }

        public void PostBindService(
            ISystemServiceStoreReader systemServiceResolver)
        {
            if (systemServiceResolver == null)
            {
                throw new ArgumentNullException(
                    nameof(systemServiceResolver));
            }

            _systemServiceResolver = systemServiceResolver;

            for (int i = 0; i < _sceneUIs.Length; i++)
            {
                IUIService uiBase = _sceneUIs[i];

                if (uiBase == null)
                    continue;

                uiBase.BindUIService(systemServiceResolver);
            }

            _isServiceBound = true;
        }

        public void Initialize()
        {
            ValidateHierarchy();
            HideClosedRegisteredUI();
            _isInitialized = true;
        }

        public void Shutdown()
        {
            _sceneUIs = Array.Empty<UIBase>();
            _uis.Clear();
            _globalUIs.Clear();
            _hasBoundScene = false;
            _uiHistory.Clear();
            _parents.Clear();
            _openRoots.Clear();
            _hierarchyValidated = false;
            _systemDataQuery = null;
            _systemServiceResolver = null;
            _isInitialized = false;
            _isServiceBound = false;
        }

        public void ExportServices(
            ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<IUISystemService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        #region SystemService

        public void OpenGlobalUI()
        {
            EnsureInitialized();

            foreach (var pair in _globalUIs)
                if (pair.Value != null) OpenUI(pair.Key, true);
        }

        public void OpenUI(UIId id)
        {
            EnsureInitialized();

            if (_globalUIs.ContainsKey(id))
            {
                OpenUI(id, true);
                return;
            }

            if (_parents.TryGetValue(id, out UIId parentId))
            {
                OpenChildUI(parentId, id);
                return;
            }

            if (!_uis.TryGetValue(id, out IUIService targetUI))
            {
                Debug.LogError(
                    $"[{nameof(UISystem)}] 등록되지 않은 UI입니다. UIId: {id}");

                return;
            }

            if (IsDestroyed(targetUI))
            {
                _uis.Remove(id);
                _openRoots.Remove(id);
                RemoveFromHistory(id);
                Debug.LogWarning(
                    $"[{nameof(UISystem)}] 파괴된 UI를 열 수 없습니다. UIId: {id}");
                return;
            }

            if (IsOpen(id) && _uiHistory.Contains(id))
            {
                targetUI.Open();
                return;
            }
            CloseUnrelatedHistory(id);
            RemoveFromHistory(id);

            targetUI.Open();
            _openRoots.Add(id);
            _uiHistory.Push(id);
        }

        public void OpenUI(UIId id, bool isFixable)
        {
            if (!isFixable)
            {
                Debug.LogWarning(
                    $"[{nameof(UISystem)}] 잘못된 호출 함수입니다. Fixable UI가 아닌 경우 OpenUI(UIId id) 오버로드를 사용하십시오. UIId : {id}");
            }
            EnsureInitialized();

            if (_parents.TryGetValue(id, out UIId parentId))
            {
                OpenChildUI(parentId, id);
                return;
            }

            if (!_uis.TryGetValue(id, out IUIService targetUI))
            {
                Debug.LogError(
                    $"[{nameof(UISystem)}] 등록되지 않은 UI입니다. UIId: {id}");

                return;
            }

            if (IsDestroyed(targetUI))
            {
                _uis.Remove(id);
                _openRoots.Remove(id);
                RemoveFromHistory(id);
                Debug.LogWarning(
                    $"[{nameof(UISystem)}] 파괴된 UI를 열 수 없습니다. UIId: {id}");
                return;
            }

            RemoveFromHistory(id);
            targetUI.Open();
            _openRoots.Add(id);
        }

        public void OpenChildUI(UIId parentId, UIId childId)
        {
            EnsureInitialized();
            if (!_uis.TryGetValue(parentId, out IUIService parent) || IsDestroyed(parent)
                || !_uis.TryGetValue(childId, out IUIService child) || IsDestroyed(child))
                throw new InvalidOperationException($"[{nameof(UISystem)}] 부모와 자식 UI가 등록되어 있어야 하며 파괴되지 않아야 합니다. {parentId} -> {childId}");
            if (!_parents.TryGetValue(childId, out UIId registeredParent) || registeredParent != parentId)
                throw new InvalidOperationException($"[{nameof(UISystem)}] 등록된 부모와 일치하지 않습니다. {parentId} -> {childId}");
            if (!IsOpen(parentId))
                throw new InvalidOperationException($"[{nameof(UISystem)}] 부모 UI가 열려 있지 않습니다. UIId: {parentId}");

            if (IsOpen(childId))
            {
                child.Open();
                return;
            }

            CloseChildrenInternal(parentId);
            CloseUnrelatedHistory(childId);
            child.Open();
            _uiHistory.Push(childId);
        }

        public void CloseChildUI(UIId parentId)
        {
            EnsureInitialized();
            if (!_uis.ContainsKey(parentId))
                throw new InvalidOperationException($"[{nameof(UISystem)}] 등록되지 않은 부모 UI입니다. UIId: {parentId}");
            CloseChildrenInternal(parentId);
        }

        public void CloseUI(UIId id)
        {
            EnsureInitialized();

            if (!_uis.TryGetValue(id, out IUIService targetUI))
            {
                Debug.LogWarning(
                    $"[{nameof(UISystem)}] 닫으려는 UI가 등록되어 있지 않습니다. UIId: {id}");

                return;
            }

            CloseChildrenInternal(id);
            if (!IsDestroyed(targetUI)) targetUI.Hide();
            else _uis.Remove(id);
            _openRoots.Remove(id);
            RemoveFromHistory(id);
        }

        public void BackCloseUI()
        {
            EnsureInitialized();

            if (!_uiHistory.TryPeek(out UIId currentId))
                return;

            CloseUI(currentId);
        }

        public void HideAllUI()
        {
            EnsureInitialized();

            HideAllUIInternal(clearHistory: true);
        }

        #endregion

        #region Registry

        public void AddUI(UIId id, IUIService ui, UIId parentId = UIId.None)
        {
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));

            if (id == UIId.None)
            {
                throw new InvalidOperationException(
                    $"[{nameof(UISystem)}] {nameof(UIId.None)}은 UI 등록에 사용할 수 없습니다. " +
                    $"타입: {ui.GetType().Name}");
            }

            if (id == parentId)
                throw new InvalidOperationException($"[{nameof(UISystem)}] UI는 자신의 부모가 될 수 없습니다. UIId: {id}");

            if (ui is GlobalUIBase && parentId != UIId.None)
                throw new InvalidOperationException($"[{nameof(UISystem)}] 유지 UI는 루트로 등록해야 합니다. UIId: {id}");

            if (_uis.TryAdd(id, ui))
            {
                if (!_hasBoundScene && ui is GlobalUIBase globalUI)
                {
                    _globalUIs.Add(id, globalUI);
                    if (Application.isPlaying)
                        UnityEngine.Object.DontDestroyOnLoad(globalUI.transform.root.gameObject);
                }
                if (parentId != UIId.None) _parents.Add(id, parentId);
                _hierarchyValidated = false;
                return;
            }

            throw new InvalidOperationException(
                $"[{nameof(UISystem)}] 동일한 UIId가 중복 등록되었습니다. " +
                $"UIId: {id}, 기존 타입: {_uis[id].GetType().Name}, " +
                $"신규 타입: {ui.GetType().Name}");
        }

        #endregion

        #region Internal

        private void ValidateHierarchy()
        {
            if (_hierarchyValidated) return;
            // 씬 검색 순서는 보장되지 않으므로 모든 UI 등록 이후 검증한다.
            foreach (KeyValuePair<UIId, UIId> pair in _parents)
            {
                UIId current = pair.Key;
                int remaining = _uis.Count;
                while (_parents.TryGetValue(current, out UIId parent))
                {
                    if (!_uis.ContainsKey(parent))
                        throw new InvalidOperationException($"[{nameof(UISystem)}] 부모 UI가 등록되지 않았습니다. {current} -> {parent}");
                    if (--remaining <= 0)
                        throw new InvalidOperationException($"[{nameof(UISystem)}] UI 부모 관계가 순환합니다. UIId: {pair.Key}");
                    current = parent;
                }
            }
            _hierarchyValidated = true;
        }

        private void HideClosedRegisteredUI()
        {
            // 최초 초기화에서는 모든 UI를 숨기고, 재바인딩에서는 열린 유지 UI를 보존한다.
            foreach (KeyValuePair<UIId, IUIService> pair in _uis)
            {
                if (!IsDestroyed(pair.Value) && !IsOpen(pair.Key))
                    pair.Value.Hide();
            }
        }

        private bool IsOpen(UIId id)
        {
            while (_parents.TryGetValue(id, out UIId parent))
            {
                if (!_uis.TryGetValue(id, out IUIService ui) || IsDestroyed(ui)
                    || !_uiHistory.Contains(id))
                    return false;
                id = parent;
            }
            return _openRoots.Contains(id) && _uis.TryGetValue(id, out IUIService root) && !IsDestroyed(root);
        }

        private void CloseChildrenInternal(UIId parentId)
        {
            if (!_uiHistory.TryPeek(out UIId currentId)) return;

            // History는 한 경로다. 최상단의 부모를 따라가 요청한 부모의 경로인지 확인한다.
            while (_parents.TryGetValue(currentId, out currentId))
            {
                if (currentId != parentId) continue;
                CloseHistoryAbove(parentId);
                return;
            }
        }

        private void HideAllUIInternal(bool clearHistory)
        {
            foreach (UIId child in _parents.Keys)
            {
                if (!IsOpen(child) && _uis.TryGetValue(child, out IUIService ui) && !IsDestroyed(ui))
                    ui.Hide();
            }
            foreach (KeyValuePair<UIId, IUIService> pair in _uis)
            {
                if (_parents.ContainsKey(pair.Key)) continue;
                CloseChildrenInternal(pair.Key);
                if (IsDestroyed(pair.Value))
                    continue;

                pair.Value.Hide();
            }
            _openRoots.Clear();

            if (clearHistory)
            {
                _uiHistory.Clear();
            }
        }

        private void CloseUnrelatedHistory(UIId targetId)
        {
            // 새 UI의 부모부터 올라가 History에 있는 가장 가까운 조상까지만 남긴다.
            UIId keepId = UIId.None;
            while (_parents.TryGetValue(targetId, out targetId))
            {
                if (!_uiHistory.Contains(targetId)) continue;
                keepId = targetId;
                break;
            }
            CloseHistoryAbove(keepId);
        }

        private void CloseHistoryAbove(UIId keepId)
        {
            // 유지 루트는 History에 없으므로 Pop 대상이 되지 않는다.
            while (_uiHistory.TryPeek(out UIId currentId) && currentId != keepId)
            {
                _uiHistory.Pop();
                _openRoots.Remove(currentId);
                if (_uis.TryGetValue(currentId, out IUIService ui) && !IsDestroyed(ui))
                    ui.Hide();
            }
        }

        private void RemoveFromHistory(UIId id)
        {
            if (_uiHistory.Count == 0)
                return;

            Stack<UIId> temporaryHistory = new(
                _uiHistory.Count);

            while (_uiHistory.TryPop(out UIId historyId))
            {
                if (!EqualityComparer<UIId>.Default.Equals(historyId, id))
                {
                    temporaryHistory.Push(historyId);
                }
            }

            while (temporaryHistory.TryPop(out UIId historyId))
            {
                _uiHistory.Push(historyId);
            }
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    $"[{nameof(UISystem)}] Initialize 이전에는 UI를 조작할 수 없습니다.");
            }

            if (!_isServiceBound)
            {
                throw new InvalidOperationException(
                    $"[{nameof(UISystem)}] UI 서비스 바인딩이 완료되지 않았습니다.");
            }
            ValidateHierarchy();
        }

        private static bool IsDestroyed(IUIService ui)
        {
            return ui is UnityEngine.Object unityObject && unityObject == null;
        }

        #endregion
    }

    #endregion
}
