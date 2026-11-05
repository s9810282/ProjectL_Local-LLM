using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>
    /// 하나의 UIId로 함께 열고 닫는 UI 인스턴스 그룹.
    /// 멤버의 UIId는 중복되어도 되며 UISystem에 개별 등록하지 않는다.
    /// </summary>
    public abstract class UIGroup : UIBase
    {
        [Tooltip("함께 열고 닫을 UI입니다. 배열 순서로 Push하며 마지막 요소부터 처리합니다.")]
        [SerializeField] private UIBase[] initialInstances = Array.Empty<UIBase>();

        private readonly Stack<UIBase> _instances = new();
        private bool _instancesInitialized;
        private bool _isOpen;
        private bool _hasDisplayState;
        private IUISystemService _uiSystemService;
        private IServiceModule _dataQuery;
        private ISystemServiceStoreReader _serviceResolver;

        /// <summary>파생 화면이 소유 멤버를 조회합니다. 등록 원본은 initialInstances와 내부 Stack 한 곳입니다.</summary>
        protected IEnumerable<UIBase> Instances
        {
            get
            {
                PrepareInstances();
                return _instances;
            }
        }

        public int InstanceCount
        {
            get
            {
                PrepareInstances();
                return _instances.Count;
            }
        }

        internal void PrepareInstances()
        {
            if (_instancesInitialized) return;
            _instancesInitialized = true;
            for (int i = 0; i < initialInstances.Length; i++)
                AddInstance(initialInstances[i]);
        }

        /// <summary>멤버를 Stack에 추가한다. 열린 그룹에 추가하면 즉시 함께 표시한다.</summary>
        public void AddInstance(UIBase instance)
        {
            PrepareInstances();
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (instance is GlobalUIBase)
                throw new InvalidOperationException("GlobalUI는 그룹 멤버가 아닌 독립 루트로 등록해야 합니다.");
            if (instance.OwnerGroup != null)
                throw new InvalidOperationException("UI 인스턴스는 하나의 그룹에 한 번만 추가할 수 있습니다.");
            for (UIGroup group = this; group != null; group = group.OwnerGroup)
                if (group == instance)
                    throw new InvalidOperationException("UIGroup은 자신 또는 조상 그룹을 멤버로 가질 수 없습니다.");

            if (instance is UIWidgetBase widget) widget.BindOwner(this);
            instance.OwnerGroup = this;
            _instances.Push(instance);
            if (_uiSystemService != null) instance.BindUISystemService(_uiSystemService);
            if (_dataQuery != null) instance.GetUIData(_dataQuery);
            if (_serviceResolver != null) instance.BindUIService(_serviceResolver);
            if (_isOpen) instance.Open();
            else if (_hasDisplayState) instance.Hide();
        }

        /// <summary>마지막 멤버를 숨기고 Stack에서 꺼낸다. 객체의 파괴나 풀 반환은 호출자가 맡는다.</summary>
        public UIBase PopInstance()
        {
            PrepareInstances();
            UIBase instance = _instances.Pop();
            if (instance != null)
            {
                instance.Hide();
                instance.OwnerGroup = null;
            }
            return instance;
        }

        /// <summary>동적 HUD처럼 임의 순서로 종료되는 멤버를 제거한다. 나머지 Stack 순서는 보존한다.</summary>
        public bool RemoveInstance(UIBase instance)
        {
            if (ReferenceEquals(instance, null)) throw new ArgumentNullException(nameof(instance));
            // 풀/씬 해제의 반복 제거는 정상이다.
            if (instance.OwnerGroup == null) return false;
            if (instance.OwnerGroup != this)
                throw new InvalidOperationException($"[UIGroup] '{instance.name}'은 '{name}'의 멤버가 아닙니다.");
            if (instance != null) instance.Hide();
            UIBase[] members = _instances.ToArray();
            _instances.Clear();
            for (int i = members.Length - 1; i >= 0; i--)
                if (!ReferenceEquals(members[i], instance)) _instances.Push(members[i]);
            instance.OwnerGroup = null;
            return true;
        }

        public override void BindUISystemService(IUISystemService service)
        {
            PrepareInstances();
            base.BindUISystemService(service);
            _uiSystemService = service;
            foreach (UIBase instance in _instances)
                if (instance != null) instance.BindUISystemService(service);
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            PrepareInstances();
            _dataQuery = query;
            foreach (UIBase instance in _instances)
                if (instance != null) instance.GetUIData(query);
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            PrepareInstances();
            _serviceResolver = resolver;
            foreach (UIBase instance in _instances)
                if (instance != null) instance.BindUIService(resolver);
        }

        public override void Open()
        {
            PrepareInstances();
            _isOpen = true;
            _hasDisplayState = true;
            base.Open();
            // Pop/ToArray 없이 순회하므로 멤버와 순서를 보존하고 임시 할당을 만들지 않는다.
            foreach (UIBase instance in _instances)
                if (instance != null) instance.Open();
        }

        public override void Hide()
        {
            PrepareInstances();
            _isOpen = false;
            _hasDisplayState = true;
            foreach (UIBase instance in _instances)
                if (instance != null) instance.Hide();
            base.Hide();
        }
    }
}
