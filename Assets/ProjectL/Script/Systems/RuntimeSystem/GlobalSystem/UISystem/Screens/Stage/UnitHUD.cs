using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems
{
    /// <summary>유닛 하나의 표시. 전투 수치와 예상 결과는 호출자가 계산해서 전달한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UnitHUD : MonoBehaviour, IUIInputReceiver
    {
        [Header("Health")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Color playerHealthColor = Color.green;
        [SerializeField] private Color enemyHealthColor = Color.red;
        [Header("Turn Order (Count)")]
        [Tooltip("Timeline UI 작업 중에는 비활성화한다. 기존 Count 표시를 복원할 때 활성화한다.")]
        [SerializeField] private bool showTurnOrder = false;
        [SerializeField] private TMP_Text turnOrderText;
        [SerializeField] private Image turnOrderImage;

        [Header("Action Cost")]
        [SerializeField] private Transform actionCostRoot;
        [Tooltip("코스트 하나를 나타내는 Image 프리팹. MaxCost만큼 생성하고 재사용한다.")]
        [SerializeField] private Image actionCostPrefab;
        [Tooltip("Image 사이의 수평 간격(UI 단위).")]
        [SerializeField, Min(0f)] private float actionCostSpacing = 10f;
        [SerializeField] private Color playerCostColor = new(0f, 0.45f, 1f, 1f);
        [SerializeField] private Color enemyCostColor = new(1f, 0.5f, 0f, 1f);
        [SerializeField] private Color spentCostColor = Color.gray;
        [SerializeField] private float spentCostRotation = 45f;
        [Tooltip("소모/회복 시 회전 전환 시간(초). 클수록 느리며, 0이면 즉시 전환한다.")]
        [SerializeField, Min(0f)] private float spentCostRotationDuration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float previewAlpha = 0.4f;
        [Tooltip("미확정/예상 소모 코스트 알파가 반투명→원본→반투명으로 왕복하는 횟수(초당).")]
        [SerializeField, Min(0.01f)] private float previewPulseSpeed = 1f;

        [Header("Buff / Debuff")]
        [SerializeField] private Transform statusRoot;
        [Tooltip("버프/디버프 하나를 나타내는 Image 프리팹. 입력용 Button과 UIInputBinding은 자동 연결한다.")]
        [SerializeField] private Image statusIconPrefab;
        [SerializeField, Min(0f)] private float statusIconSpacing = 10f;
        [SerializeField] private GameObject descriptionPanel;
        [SerializeField] private TMP_Text descriptionText;

        private readonly List<CostSlot> _costSlots = new();
        private readonly List<StatusSlot> _statusSlots = new();
        private readonly List<UnitHUDStatus> _statuses = new();
        private int _currentHealth, _maxHealth;
        private int? _previewHealth;
        private int _currentCost, _maxCost, _pendingMoveCost, _previewCost;
        private int _hoveredStatus = -1, _pinnedStatus = -1;
        private Owner _owner = Owner.Player;
        private EntityId _entityId;

        private sealed class CostSlot
        {
            public Image Image;
            public Quaternion OriginalRotation;
            public float CurrentRotation;
            public float TargetRotation;
            public bool RotationInitialized;
        }

        private sealed class StatusSlot
        {
            public Button Button;
            public Image Icon;
            public UIInputBinding Input;
            public Selectable.Transition Transition;
        }

        private void Awake()
        {
            // HP/텍스트/바탕은 전투 포인터를 가리지 않는다. 설명 가능한 아이콘만 나중에 입력을 연다.
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            ClearDescription();
            SetTurnOrder(false, null);
        }

        /// <summary>
        /// View가 CharacterBuffer에서 읽은 유닛을 전달한다. Owner/HP/Cost는 같은 Buffer 조회값에서 반영한다.
        /// 다른 유닛에 재사용할 때는 이전 아이콘 선택을 정리한다. 버프 표시 데이터는 이 호출 뒤 SetStatuses로 전달한다.
        /// </summary>
        public void SetUnit(CharacterData unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            SetUnit(unit.ToFacts());
        }

        internal void SetUnit(CharacterCombatFacts unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (_entityId != unit.EntityId) Clear();
            _entityId = unit.EntityId;
            _owner = unit.Owner;
            SetUnitExtrasVisible(true);
            SetHealth(unit.Health.CurrentHealth, unit.Health.MaxHealth);
            SetActionCost(unit.Stats.CurrentActionCost, unit.Stats.MaxActionCost, unit.PendingMoveActionCost);
        }

        /// <summary>건물에는 체력바와 체력 숫자만 표시한다.</summary>
        internal void SetStructure(StructureCombatFacts structure)
        {
            if (structure == null) throw new ArgumentNullException(nameof(structure));
            if (_entityId != structure.EntityId) Clear();
            _entityId = structure.EntityId;
            _owner = structure.Owner;
            SetUnitExtrasVisible(false);
            SetHealth(structure.Health.CurrentHealth, structure.Health.MaxHealth);
        }

        private void SetUnitExtrasVisible(bool visible)
        {
            if (actionCostRoot != null) actionCostRoot.gameObject.SetActive(visible);
            if (statusRoot != null) statusRoot.gameObject.SetActive(visible);
            if (turnOrderText != null) turnOrderText.gameObject.SetActive(visible && showTurnOrder);
            if (turnOrderImage != null) turnOrderImage.gameObject.SetActive(visible && showTurnOrder);
        }

        /// <summary>현재 체력 표시. 예상 체력은 별도의 명시적 호출로만 활성화한다.</summary>
        public void SetHealth(int current, int maximum)
        {
            _maxHealth = Mathf.Max(0, maximum);
            _currentHealth = Mathf.Clamp(current, 0, _maxHealth);
            _previewHealth = null;
            RefreshHealth();
        }

        /// <summary>향후 Core 예상 결과 연결용. HUD는 피해량을 계산하지 않는다.</summary>
        public void SetHealthPreview(int? projectedHealth)
        {
            _previewHealth = projectedHealth;
            RefreshHealth();
        }

        private void RefreshHealth()
        {
            int shown = Mathf.Clamp(_previewHealth ?? _currentHealth, 0, _maxHealth);
            if (healthText != null) healthText.SetText("{0} / {1}", shown, _maxHealth);
            if (healthSlider == null) return;
            if (healthSlider.fillRect != null && healthSlider.fillRect.TryGetComponent(out Image fillImage))
                fillImage.color = _owner == Owner.Enemy ? enemyHealthColor : playerHealthColor;
            healthSlider.interactable = false;
            healthSlider.wholeNumbers = false;
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.SetValueWithoutNotify(_maxHealth > 0 ? (float)shown / _maxHealth : 0f);
        }

        /// <summary>
        /// current는 Core의 CurrentActionCost(미확정 이동 비용이 이미 차감된 값).
        /// pendingMoveCost만큼의 Image를 진영 색으로 복원해 반투명으로 표시한다.
        /// </summary>
        public void SetActionCost(int current, int maximum, int pendingMoveCost = 0)
        {
            _maxCost = Mathf.Max(0, maximum);
            _currentCost = Mathf.Clamp(current, 0, _maxCost);
            _pendingMoveCost = Mathf.Clamp(pendingMoveCost, 0, _maxCost - _currentCost);
            _previewCost = 0;
            RefreshActionCost();
        }

        /// <summary>Core가 견적한 추가 소모량. 기존 미확정 이동 비용을 포함하지 않는다.</summary>
        public void SetActionCostPreview(int additionalCost)
        {
            _previewCost = Mathf.Clamp(additionalCost, 0, _currentCost);
            RefreshActionCost();
        }

        /// <summary>유닛 진영을 명시적으로 전달한다. 체력바와 잔여/미확정 코스트 색에 반영한다.</summary>
        public void SetOwner(Owner unitOwner)
        {
            if (unitOwner != Owner.Player && unitOwner != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(unitOwner));
            _owner = unitOwner;
            RefreshHealth();
            RefreshActionCost();
        }

        private void RefreshActionCost()
        {
            if (actionCostRoot == null || actionCostPrefab == null)
                return;
            while (_costSlots.Count < _maxCost)
            {
                Image image = Instantiate(actionCostPrefab, actionCostRoot, false);
                image.raycastTarget = false;
                RectTransform rect = image.rectTransform;
                Vector2 size = rect.rect.size;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                _costSlots.Add(new CostSlot { Image = image, OriginalRotation = rect.localRotation });
            }

            int remaining = _currentCost + _pendingMoveCost;
            int opaque = _currentCost - _previewCost;
            Color availableColor = _owner == Owner.Enemy ? enemyCostColor : playerCostColor;
            float nextX = 0f;
            for (int i = 0; i < _costSlots.Count; i++)
            {
                CostSlot slot = _costSlots[i];
                slot.Image.gameObject.SetActive(i < _maxCost);
                RectTransform rect = slot.Image.rectTransform;
                float width = rect.rect.width * Mathf.Abs(rect.localScale.x);
                rect.anchoredPosition = new Vector2(nextX + width * 0.5f, 0f);
                nextX += width + Mathf.Max(0f, actionCostSpacing);
                bool spent = i >= remaining;
                Color color = spent ? spentCostColor : availableColor;
                if (!spent && i >= opaque) color.a *= previewAlpha;
                slot.Image.color = color;
                slot.TargetRotation = spent ? spentCostRotation : 0f;
                if (i >= _maxCost) slot.RotationInitialized = false;
                else if (!slot.RotationInitialized || spentCostRotationDuration <= 0f)
                {
                    slot.CurrentRotation = slot.TargetRotation;
                    slot.RotationInitialized = true;
                }
                rect.localRotation = slot.OriginalRotation *
                    Quaternion.Euler(0f, 0f, slot.CurrentRotation);
            }
        }

        private void LateUpdate()
        {
            AnimateCostRotation(Time.unscaledDeltaTime);
            AnimateCostPreview(Time.unscaledTime);
        }

        internal void AnimateCostRotation(float unscaledDeltaTime)
        {
            float step = spentCostRotationDuration > 0f
                ? Mathf.Abs(spentCostRotation) / spentCostRotationDuration * Mathf.Max(0f, unscaledDeltaTime)
                : float.PositiveInfinity;
            int end = Mathf.Min(_maxCost, _costSlots.Count);
            for (int i = 0; i < end; i++)
            {
                CostSlot slot = _costSlots[i];
                if (slot.CurrentRotation == slot.TargetRotation) continue;
                slot.CurrentRotation = Mathf.MoveTowards(slot.CurrentRotation, slot.TargetRotation, step);
                // 기준 회전에 오프셋을 적용해 반복 갱신에서도 회전이 누적되지 않는다.
                slot.Image.rectTransform.localRotation = slot.OriginalRotation *
                    Quaternion.Euler(0f, 0f, slot.CurrentRotation);
            }
        }

        /// <summary>표시 애니메이션만 갱신한다. 반복된 견적 호출에도 위상이 초기화되지 않는다.</summary>
        internal void AnimateCostPreview(float unscaledTime)
        {
            if (_pendingMoveCost == 0 && _previewCost == 0) return;
            float phase = unscaledTime * Mathf.Max(0.01f, previewPulseSpeed) * Mathf.PI * 2f;
            float alphaMultiplier = Mathf.Lerp(Mathf.Clamp01(previewAlpha), 1f, (1f - Mathf.Cos(phase)) * 0.5f);
            float baseAlpha = (_owner == Owner.Enemy ? enemyCostColor : playerCostColor).a;
            int end = Mathf.Min(_currentCost + _pendingMoveCost, Mathf.Min(_maxCost, _costSlots.Count));
            for (int i = _currentCost - _previewCost; i < end; i++)
            {
                Image image = _costSlots[i].Image;
                Color color = image.color;
                color.a = baseAlpha * alphaMultiplier;
                image.color = color;
            }
        }

        /// <summary>Core Timeline에서 정한 다음 행동 순서(1부터). 현재 행위자는 N, 순서 없음은 빈칸.</summary>
        public void SetTurnOrder(bool isActiveActor, int? predictedOrder)
        {
            if (!showTurnOrder)
            {
                if (turnOrderText != null) turnOrderText.gameObject.SetActive(false);
                if (turnOrderImage != null) turnOrderImage.gameObject.SetActive(false);
                return;
            }
            if (turnOrderText == null) return;
            if (isActiveActor) turnOrderText.SetText("N");
            else if (predictedOrder.HasValue && predictedOrder.Value > 0)
                turnOrderText.SetText("{0}", predictedOrder.Value);
            else turnOrderText.SetText(string.Empty);
        }

        /// <summary>View가 준비한 버프/디버프 아이콘과 설명. 순서 변경 시에도 Key로 선택을 유지한다.</summary>
        public void SetStatuses(IReadOnlyList<UnitHUDStatus> statuses)
        {
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            string pinnedKey = IsStatusValid(_pinnedStatus) ? _statuses[_pinnedStatus].Key : null;
            bool sameKeys = statuses.Count == _statuses.Count;
            for (int i = 0; i < statuses.Count; i++)
            {
                if (string.IsNullOrEmpty(statuses[i].Key))
                    throw new ArgumentException("Status Key가 필요합니다.", nameof(statuses));
                for (int j = 0; j < i; j++)
                    if (statuses[j].Key == statuses[i].Key)
                        throw new ArgumentException("Status Key는 유닛 내에서 고유해야 합니다.", nameof(statuses));
                if (sameKeys && statuses[i].Key != _statuses[i].Key) sameKeys = false;
            }
            if (!sameKeys)
            {
                for (int i = 0; i < _statusSlots.Count; i++) _statusSlots[i].Input.OnHoverExit();
                _hoveredStatus = -1;
            }
            _statuses.Clear();
            _pinnedStatus = -1;
            for (int i = 0; i < statuses.Count; i++)
            {
                _statuses.Add(statuses[i]);
                if (statuses[i].Key == pinnedKey) _pinnedStatus = i;
            }
            if (statusRoot != null && statusIconPrefab != null)
            {
                while (_statusSlots.Count < statuses.Count)
                {
                    Image icon = Instantiate(statusIconPrefab, statusRoot, false);
                    Button button = icon.GetComponent<Button>();
                    if (button == null) button = icon.gameObject.AddComponent<Button>();
                    if (button.targetGraphic == null) button.targetGraphic = icon;
                    icon.raycastTarget = false;
                    RectTransform rect = icon.rectTransform;
                    Vector2 size = rect.rect.size;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = size;
                    _statusSlots.Add(new StatusSlot
                    {
                        Button = button, Icon = icon, Transition = button.transition,
                        Input = UIInputBinding.Attach(button, this, _statusSlots.Count)
                    });
                }
                float nextX = 0f;
                for (int i = 0; i < _statusSlots.Count; i++)
                {
                    StatusSlot slot = _statusSlots[i];
                    if (i < statuses.Count)
                    {
                        RectTransform rect = slot.Icon.rectTransform;
                        float width = rect.rect.width * Mathf.Abs(rect.localScale.x);
                        rect.anchoredPosition = new Vector2(nextX + width * 0.5f, 0f);
                        nextX += width + Mathf.Max(0f, statusIconSpacing);
                        slot.Icon.sprite = statuses[i].Icon != null ? statuses[i].Icon : statusIconPrefab.sprite;
                        // Description 미구현/참조 누락이면 아이콘 표시만 한다.
                        bool canDescribe = CanDescribe(i);
                        if (!canDescribe) slot.Input.OnHoverExit();
                        slot.Button.transition = canDescribe ? slot.Transition : Selectable.Transition.None;
                        slot.Button.interactable = canDescribe;
                        slot.Icon.raycastTarget = canDescribe;
                    }
                    slot.Button.gameObject.SetActive(i < statuses.Count);
                }
            }
            RefreshDescription();
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!isActiveAndEnabled || !CanDescribe(slot)) return;
            _pinnedStatus = _pinnedStatus == slot ? -1 : slot;
            // 같은 아이콘을 다시 클릭하면 hover 중이어도 닫는다.
            if (_pinnedStatus < 0) _hoveredStatus = -1;
            RefreshDescription();
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
            if (!isActiveAndEnabled || !CanDescribe(slot)) return;
            _hoveredStatus = slot;
            RefreshDescription();
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
            if (_hoveredStatus == slot) _hoveredStatus = -1;
            RefreshDescription();
        }

        private bool IsStatusValid(int slot) => slot >= 0 && slot < _statuses.Count;
        private bool CanDescribe(int slot) => descriptionPanel != null && descriptionText != null &&
            IsStatusValid(slot) && !string.IsNullOrWhiteSpace(_statuses[slot].Description);

        private void RefreshDescription()
        {
            int slot = IsStatusValid(_pinnedStatus) ? _pinnedStatus : _hoveredStatus;
            bool show = isActiveAndEnabled && CanDescribe(slot);
            if (descriptionText != null)
                descriptionText.SetText(show ? _statuses[slot].Description : string.Empty);
            if (descriptionPanel != null) descriptionPanel.SetActive(show);
        }

        private void ClearDescription()
        {
            _hoveredStatus = _pinnedStatus = -1;
            RefreshDescription();
        }

        /// <summary>Hover 해제, 확정/취소/모드 변경 뒤 호출한다.</summary>
        public void ClearPreview()
        {
            _previewHealth = null;
            _previewCost = 0;
            RefreshHealth();
            RefreshActionCost();
        }

        /// <summary>다른 유닛에 재사용하거나 풀 반환 전에 호출한다.</summary>
        public void Clear()
        {
            _entityId = EntityId.Empty;
            _owner = Owner.Player;
            for (int i = 0; i < _statusSlots.Count; i++)
            {
                _statusSlots[i].Input.OnHoverExit();
                _statusSlots[i].Button.gameObject.SetActive(false);
            }
            _statuses.Clear();
            ClearDescription();
            SetHealth(0, 0);
            SetActionCost(0, 0);
            SetTurnOrder(false, null);
        }

        private void OnDisable()
        {
            for (int i = 0; i < _statusSlots.Count; i++) _statusSlots[i].Input.OnHoverExit();
            ClearDescription();
            ClearPreview();
        }
    }

    /// <summary>Core 상태와 View DB를 조합해 호출자가 만든 표시 데이터. HUD는 효과 규칙을 해석하지 않는다.</summary>
    public readonly struct UnitHUDStatus
    {
        public string Key { get; }
        public Sprite Icon { get; }
        public string Description { get; }

        public UnitHUDStatus(string key, Sprite icon, string description)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Status Key가 필요합니다.", nameof(key));
            Key = key;
            Icon = icon;
            Description = description ?? string.Empty;
        }
    }
}
