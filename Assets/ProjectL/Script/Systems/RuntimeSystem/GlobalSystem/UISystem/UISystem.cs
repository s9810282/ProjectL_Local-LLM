using ProjectL.Systems.ServiceKeys.Runtime;
﻿using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;

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

    /// <summary>씬 바인딩 뒤 생성되는 화면의 등록·바인딩·해제를 한 수명으로 연결한다.</summary>
    public interface IUIRuntimeRegistry
    {
        void RegisterRuntimeUI(UIBase ui);
        void UnregisterRuntimeUI(UIBase ui);
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
        IUIRuntimeFaultHandler,

        IUIRegistry, IUIRuntimeRegistry
    {
        private readonly RuntimeFaultBoundary _faults;

        public UISystem() : this(null)
        {
        }

        internal UISystem(RuntimeFaultBoundary faults)
        {
            _faults = faults;
        }

        bool IUIRuntimeFaultHandler.IsFaulted => _faults?.IsFaulted ?? false;
        void IUIRuntimeFaultHandler.Report(Exception error) => _faults?.Report(error);

        private readonly Dictionary<UIId, IUIService> _uis = new();
        private readonly Dictionary<UIId, GlobalUIBase> _globalUIs = new();
        internal IReadOnlyDictionary<UIId, GlobalUIBase> GlobalUIs => _globalUIs;
        private bool _hasBoundScene;
        // 열린 상태와 뒤로가기 순서를 분리해 Parallel UI의 여러 경로를 지원한다.
        private readonly List<UIId> _uiHistory = new();
        private UIId _historyFallback = UIId.None;
        private readonly Dictionary<UIId, UIId> _parents = new();
        private HashSet<UIId> _openUIs = new();
        private bool _hierarchyValidated;

        private UIBase[] _sceneUIs = Array.Empty<UIBase>();

        private IServiceModule _systemDataQuery;
        private ISystemServiceStoreReader _systemServiceResolver;
        private IAudioSystemService _audio;
        private readonly HashSet<UIAudioScope> _audioScopes = new();

        private bool _isInitialized;
        private bool _isServiceBound;
        private readonly HashSet<UIId> _runtimeUIIds = new();

        #region SystemModule

        public void BindScene()
        {
            _historyFallback = UIId.None;
            // 씬을 바꾸면 이전 씬의 호버 설명은 즉시 정리합니다.
            if (_isInitialized && _uis.ContainsKey(UIId.Hover)) CloseUI(UIId.Hover);
            BindUIAudio();
            _uis.Clear();
            _uiHistory.Clear();
            _parents.Clear();
            _runtimeUIIds.Clear();
            _openUIs.RemoveWhere(id => !_globalUIs.ContainsKey(id));
            _hierarchyValidated = false;

            foreach (var pair in _globalUIs)
                if (pair.Value != null) _uis.Add(pair.Key, pair.Value);

            UIBase[] foundUIs = UnityEngine.Object.FindObjectsByType<UIBase>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var sceneUIs = new List<UIBase>();

            // 검색 순서와 무관하게 그룹 소유권을 먼저 확정한다. 멤버는 그룹만 바인딩한다.
            for (int i = 0; i < foundUIs.Length; i++)
                if (foundUIs[i] is UIGroup group) group.PrepareInstances();

            for (int i = 0; i < foundUIs.Length; i++)
            {
                UIBase uiBase = foundUIs[i];

                if (uiBase == null)
                    continue;
                if (!uiBase.RegisterWithScene && !uiBase.IsRuntimeRegistered)
                    continue;
                if (uiBase is UIWidgetBase widget)
                {
                    widget.PrepareOwner();
                    continue;
                }
                if (uiBase.OwnerGroup != null)
                    continue;
                // GlobalUI는 최초 씬에서만 수집하고 이후 씬 바인딩에서 제외한다.
                if (_hasBoundScene && uiBase is GlobalUIBase)
                    continue;

                sceneUIs.Add(uiBase);
                uiBase.ExportUIService(this);
                // 같은 씬 재바인딩에서도 동적 화면의 해제 계약은 보존한다.
                if (uiBase.IsRuntimeRegistered) _runtimeUIIds.Add(uiBase.ID);
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
            // UI만 조립한 구성에서도 기존 입력·표시를 유지한다. 오디오는 Global이 있을 때 연결한다.
            try
            {
                _audio = systemServiceResolver.GetService(SystemGroupsService.Global).AudioServiceCommand;
            }
            catch (KeyNotFoundException)
            {
                _audio = null;
            }
            BindUIAudio();

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
            _historyFallback = UIId.None;
            // 표시 소유자가 이후 GameObject를 파괴하더라도 동적 UI의 입력·렌더 콜백은 먼저 정리한다.
            var runtimeIds = new UIId[_runtimeUIIds.Count];
            _runtimeUIIds.CopyTo(runtimeIds);
            foreach (UIId id in runtimeIds)
            {
                if (!_uis.TryGetValue(id, out IUIService service) || !(service is UIBase view) || IsDestroyed(view)) continue;
                view.Hide();
                view.IsRuntimeRegistered = false;
            }
            if (_isInitialized && _isServiceBound)
            {
                if (_uis.ContainsKey(UIId.Hover)) CloseUI(UIId.Hover);
                if (_uis.ContainsKey(UIId.Notice)) CloseUI(UIId.Notice);
            }
            foreach (var scope in _audioScopes)
                if (scope != null) scope.Bind(null);
            _audioScopes.Clear();
            _audio = null;
            _sceneUIs = Array.Empty<UIBase>();
            _uis.Clear();
            _globalUIs.Clear();
            _hasBoundScene = false;
            _uiHistory.Clear();
            _parents.Clear();
            _runtimeUIIds.Clear();
            _openUIs.Clear();
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

        private void BindUIAudio()
        {
            if (_audio == null) return;
            _audioScopes.RemoveWhere(scope => scope == null);
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var root = canvas.transform.root.gameObject;
                var scope = root.GetComponent<UIAudioScope>();
                if (scope == null) scope = root.AddComponent<UIAudioScope>();
                if (_audioScopes.Add(scope)) scope.Bind(_audio);
            }
        }

        public void OpenGlobalUI()
        {
            EnsureInitialized();

            foreach (var pair in _globalUIs)
                if (pair.Value != null && pair.Value.OpenWithGlobalUI) OpenUI(pair.Key, true);
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
                throw new InvalidOperationException($"[{nameof(UISystem)}] 등록되지 않은 UI입니다. UIId: {id}");
            }

            if (IsDestroyed(targetUI))
            {
                _uis.Remove(id);
                _openUIs.Remove(id);
                RemoveFromHistory(id);
                throw new InvalidOperationException($"[{nameof(UISystem)}] 파괴된 UI를 열 수 없습니다. UIId: {id}");
            }

            if (IsOpen(id) && _uiHistory.Contains(id))
            {
                targetUI.Open();
                return;
            }
            if (!IsParallel(targetUI)) CloseUnrelatedHistory(id);
            RemoveFromHistory(id);

            targetUI.Open();
            _openUIs.Add(id);
            _uiHistory.Add(id);
            if (targetUI is UIBase view && view.RestoreWhenHistoryEmpty) _historyFallback = id;
        }

        public void OpenUI(UIId id, bool isFixable)
        {
            if (!isFixable)
            {
                throw new ArgumentException($"[{nameof(UISystem)}] 고정 표시가 아니면 OpenUI(UIId) 오버로드를 사용해야 합니다. UIId: {id}", nameof(isFixable));
            }
            EnsureInitialized();

            if (_parents.TryGetValue(id, out UIId parentId))
            {
                OpenChildUI(parentId, id);
                return;
            }

            if (!_uis.TryGetValue(id, out IUIService targetUI))
            {
                throw new InvalidOperationException($"[{nameof(UISystem)}] 등록되지 않은 UI입니다. UIId: {id}");
            }

            if (IsDestroyed(targetUI))
            {
                _uis.Remove(id);
                _openUIs.Remove(id);
                RemoveFromHistory(id);
                throw new InvalidOperationException($"[{nameof(UISystem)}] 파괴된 UI를 열 수 없습니다. UIId: {id}");
            }

            RemoveFromHistory(id);
            targetUI.Open();
            _openUIs.Add(id);
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

            if (!IsParallel(child)) CloseUnrelatedHistory(childId);
            child.Open();
            _openUIs.Add(childId);
            _uiHistory.Add(childId);
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
                throw new InvalidOperationException($"[{nameof(UISystem)}] 닫으려는 UI가 등록되어 있지 않습니다. UIId: {id}");
            }

            bool wasInHistory = _uiHistory.Contains(id);
            // 기본 화면의 명시적 종료는 복귀 예약도 해제합니다. Exclusive 교체는 내부 종료를 사용합니다.
            if (_historyFallback == id) _historyFallback = UIId.None;
            CloseChildrenInternal(id);
            if (!IsDestroyed(targetUI)) targetUI.Hide();
            else _uis.Remove(id);
            _openUIs.Remove(id);
            RemoveFromHistory(id);
            if (wasInHistory && _uiHistory.Count == 0 && _historyFallback != UIId.None)
                OpenUI(_historyFallback);
        }

        public void BackCloseUI()
        {
            EnsureInitialized();

            if (_uiHistory.Count == 0)
                return;

            CloseUI(_uiHistory[_uiHistory.Count - 1]);
        }

        public void HideAllUI()
        {
            EnsureInitialized();

            _historyFallback = UIId.None;
            HideAllUIInternal(clearHistory: true);
        }

        #endregion

        #region Registry

        public void RegisterRuntimeUI(UIBase ui)
        {
            if (ui == null) throw new ArgumentNullException(nameof(ui));
            EnsureInitialized();
            if (ui is UIWidgetBase || ui is GlobalUIBase || ui.OwnerGroup != null)
                throw new InvalidOperationException($"[{nameof(UISystem)}] 동적 등록은 독립 씬 화면만 지원합니다. UIId: {ui.ID}, 객체: {ui.name}");
            if (ui.ParentID != UIId.None &&
                (!_uis.TryGetValue(ui.ParentID, out IUIService parent) || IsDestroyed(parent)))
                throw new InvalidOperationException($"[{nameof(UISystem)}] 동적 화면의 부모가 등록되지 않았습니다. {ui.ParentID} -> {ui.ID}");
            AddUI(ui.ID, ui, ui.ParentID);
            try
            {
                ValidateHierarchy();
                ui.BindUISystemService(this);
                if (_systemDataQuery != null) ui.GetUIData(_systemDataQuery);
                ui.BindUIService(_systemServiceResolver);
                ui.Hide();
                ui.IsRuntimeRegistered = true;
                _runtimeUIIds.Add(ui.ID);
            }
            catch
            {
                _uis.Remove(ui.ID);
                _parents.Remove(ui.ID);
                _hierarchyValidated = false;
                throw;
            }
        }

        public void UnregisterRuntimeUI(UIBase ui)
        {
            if (ReferenceEquals(ui, null)) throw new ArgumentNullException(nameof(ui));
            // 씬 재바인딩·반복 해제 뒤 이미 등록이 사라진 것은 정상적인 소유 자원 정리다.
            if (!_uis.TryGetValue(ui.ID, out IUIService registered) || !ReferenceEquals(registered, ui)) return;
            if (!_runtimeUIIds.Contains(ui.ID))
                throw new InvalidOperationException($"[{nameof(UISystem)}] 정적 화면은 동적 해제로 제거할 수 없습니다. UIId: {ui.ID}");
            foreach (var pair in _parents)
                if (pair.Value == ui.ID)
                    throw new InvalidOperationException($"[{nameof(UISystem)}] 자식 화면을 먼저 해제해야 합니다. {ui.ID} -> {pair.Key}");
            CloseRegisteredUIInternal(ui.ID);
            _runtimeUIIds.Remove(ui.ID);
            ui.IsRuntimeRegistered = false;
            _uis.Remove(ui.ID);
            _parents.Remove(ui.ID);
            _hierarchyValidated = false;
        }

        public void AddUI(UIId id, IUIService ui, UIId parentId = UIId.None)
        {
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));
            if (ui is UIBase member && member.OwnerGroup != null)
                throw new InvalidOperationException($"[{nameof(UISystem)}] 그룹 멤버는 UIId로 개별 등록할 수 없습니다. UIId: {id}");
            if (ui is UIWidgetBase)
                throw new InvalidOperationException($"[{nameof(UISystem)}] 소유 화면의 위젯은 독립 등록할 수 없습니다. UIId: {id}");

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
            UIId fallback = UIId.None;
            foreach (KeyValuePair<UIId, IUIService> pair in _uis)
            {
                if (!(pair.Value is UIBase view) || !view.RestoreWhenHistoryEmpty) continue;
                if (view is GlobalUIBase || view.Policy != UIOpenPolicy.Exclusive || _parents.ContainsKey(pair.Key))
                    throw new InvalidOperationException($"[{nameof(UISystem)}] 복귀 화면은 씬의 Exclusive 루트 UI여야 합니다. UIId: {pair.Key}");
                if (fallback != UIId.None)
                    throw new InvalidOperationException($"[{nameof(UISystem)}] 복귀 화면은 씬에 하나만 등록할 수 있습니다. UIId: {fallback}, {pair.Key}");
                fallback = pair.Key;
            }
            // 등록 시 용량을 확보해 열기·닫기 도중 관리 컬렉션이 확장되지 않게 한다.
            if (_uiHistory.Capacity < _uis.Count) _uiHistory.Capacity = _uis.Count;
            // Unity Mono는 삭제 슬롯이 남은 HashSet의 EnsureCapacity 확장에서 예외가 납니다.
            // 0은 현재 용량 조회만 수행합니다. 확장이 필요하면 새 집합에 열린 ID를 보존합니다.
            if (_openUIs.EnsureCapacity(0) < _uis.Count)
            {
                var openUIs = new HashSet<UIId>(_uis.Count);
                foreach (UIId id in _openUIs) openUIs.Add(id);
                _openUIs = openUIs;
            }
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
            while (true)
            {
                if (!_uis.TryGetValue(id, out IUIService ui) || IsDestroyed(ui)
                    || !_openUIs.Contains(id))
                    return false;
                if (!_parents.TryGetValue(id, out UIId parent)) return true;
                id = parent;
            }
        }

        private void CloseChildrenInternal(UIId parentId)
        {
            // Parallel로 열린 모든 가지를 자손부터 닫는다. 등록 관계는 순회 중 변경하지 않는다.
            foreach (KeyValuePair<UIId, UIId> pair in _parents)
            {
                if (pair.Value != parentId || !_openUIs.Contains(pair.Key)) continue;
                CloseRegisteredUIInternal(pair.Key);
            }
        }

        private void CloseRegisteredUIInternal(UIId id)
        {
            CloseChildrenInternal(id);
            if (_uis.TryGetValue(id, out IUIService ui) && !IsDestroyed(ui)) ui.Hide();
            _openUIs.Remove(id);
            RemoveFromHistory(id);
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
            _openUIs.Clear();

            if (clearHistory)
            {
                _uiHistory.Clear();
            }
        }

        private void CloseUnrelatedHistory(UIId targetId)
        {
            // 기존 Exclusive 전환 경로는 유지하되, Parallel UI와 새 UI의 조상은 보존한다.
            // Exclusive 조상을 닫는 경우에는 그 아래 Parallel 자손도 함께 닫는다.
            for (int i = _uiHistory.Count - 1; i >= 0; i--)
            {
                // 유지 루트를 일반 이력으로 전환하면 자손이 부모보다 앞에 있을 수도 있다.
                if (i >= _uiHistory.Count) continue;
                UIId currentId = _uiHistory[i];
                if (IsAncestorOrSelf(currentId, targetId)) continue;
                if (_uis.TryGetValue(currentId, out IUIService ui) && IsParallel(ui)) continue;
                CloseRegisteredUIInternal(currentId);
            }
        }

        private bool IsAncestorOrSelf(UIId ancestorId, UIId id)
        {
            while (true)
            {
                if (id == ancestorId) return true;
                if (!_parents.TryGetValue(id, out id)) return false;
            }
        }

        private void RemoveFromHistory(UIId id)
        {
            _uiHistory.Remove(id);
        }

        private static bool IsParallel(IUIService ui) =>
            ui is UIBase view && view.Policy == UIOpenPolicy.Parallel;

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
