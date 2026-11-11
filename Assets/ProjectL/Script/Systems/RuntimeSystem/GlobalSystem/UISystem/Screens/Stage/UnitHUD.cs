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

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>유닛 하나의 표시. 전투 수치와 예상 결과는 호출자가 계산해서 전달한다.</summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems", sourceAssembly: "ProjectL.Systems", sourceClassName: "UnitHUD")]
    public sealed class UnitHUD : UIWidgetBase, IUIInputReceiver, IUISecondaryClickReceiver
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIUnitHUDClick = new("Combat.Action.UI.UnitHUD.Click");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIUnitHUDHoverEnter = new("Combat.Action.UI.UnitHUD.HoverEnter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIUnitHUDHoverExit = new("Combat.Action.UI.UnitHUD.HoverExit");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIUnitHUDSecondaryClick = new("Combat.Action.UI.UnitHUD.SecondaryClick");
#endif

        private bool _ownerVisible = true;
        internal bool OwnerVisible => _ownerVisible;
        internal void SetOwnerVisible(bool visible)
        {
            if (_ownerVisible == visible && gameObject.activeSelf == visible) return;
            _ownerVisible = visible;
            if (visible) Open();
            else Hide();
        }
        public override void Open()
        {
            if (_ownerVisible) base.Open();
            else base.Hide();
        }

        [Header("Position")]
        [Tooltip("Rider 유닛 HUD에 추가할 월드 Y 보정입니다. 양수면 위로 올립니다.")]
        [SerializeField] private float riderHudYOffset;

        /// <summary>화면 투영 전에 HUD 기준점에 더할 월드 위치 보정입니다.</summary>
        internal Vector3 AdditionalWorldOffset => _isRider ? Vector3.up * riderHudYOffset : Vector3.zero;

        [SerializeField] private UnitHUDInfoPanel infoPanel;
        internal EntityId EntityId => _entityId;

        [Header("Action State")]
        [SerializeField] private Image actionStateImage;
        [Tooltip("이동 가능(흰색) 또는 이동·공격 모두 불가(검정색)를 표시합니다.")]
        [SerializeField, InspectorName("Sprite 1")] private Sprite actionStateSprite1;
        [Tooltip("공격만 가능할 때 흰색으로 표시합니다.")]
        [SerializeField, InspectorName("Sprite 2")] private Sprite actionStateSprite2;

        [Header("Damage")]
        [SerializeField] private UnitDamageText damageText;
        internal UnitDamageText DamageTemplate => damageText != null ? damageText :
            throw new InvalidOperationException($"[UnitHUD] '{name}': damageText 템플릿이 필요합니다.");

        /// <summary>결과 재생 측에서 전달한 확정 피해를 표시한다.</summary>
        public void ShowDamage(int damage)
        {
            if (damageText != null) damageText.Show(damage);
        }


        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthSubFill;
        [Tooltip("HP/SubFill 뒤에 겹치는 보호막입니다. 왼쪽부터 현재 HP와 보호막의 합계까지 채웁니다.")]
        [SerializeField] private Image shieldFill;
        [Tooltip("Fill이 HP 1%를 줄이는 시간(초). 0이면 즉시 반영한다.")]
        [SerializeField, Min(0f)] private float fillSecondsPerPercent = 0.003f;
        [Tooltip("Fill 감소 완료 후 SubFill이 HP 1%를 줄이는 시간(초). 0이면 즉시 반영한다.")]
        [SerializeField, Min(0f)] private float subFillSecondsPerPercent = 0.01f;
        [SerializeField, InspectorName("Curr HP")] private TMP_Text healthText;
        [SerializeField, InspectorName("Max HP")] private TMP_Text maxHealthText;
        [SerializeField] private Color playerHealthColor = Color.green;
        [SerializeField] private Color enemyHealthColor = Color.red;
        [SerializeField] private Color obstacleHealthColor = Color.gray;
        [Tooltip("예상 피해 구간의 Alpha가 원본→최소→원본으로 왕복하는 시간(초). 클수록 느리다.")]
        [SerializeField, Min(0.05f)] private float healthPreviewPulseDuration = 1.2f;
        [Tooltip("예상/현재 체력 텍스트를 한 번 바꾸기까지 걸리는 시간(초). 기본값은 HP 페이드 편도 시간의 두 배다.")]
        [SerializeField, Min(0.05f)] private float healthPreviewTextInterval = 1.2f;
        [Tooltip("예상 피해 구간이 가장 흐려졌을 때의 Alpha 배율.")]
        [SerializeField, Range(0f, 1f)] private float healthPreviewMinAlpha = 0.15f;

        [Header("Item")]
        [Tooltip("HP바 왼쪽 위 고정 아이템입니다. 같은 HUD의 Mask/Status Root 밖에 둔 BuffIcon 인스턴스의 루트 Image를 연결합니다.")]
        [SerializeField] private Image itemIcon;
        private const int ItemSlot = int.MaxValue;
        private UnitHUDStatus? _itemStatus;
        private StatusSlot _itemSlot;
        private Sprite _itemFallbackIcon;

        [Header("Buff / Debuff")]
        [SerializeField] private Transform statusRoot;
        [Tooltip("버프/디버프 배경 Image 프리팹. 상태 Sprite는 자식 Icon의 Image에 표시하며, 입력용 Button·UIInputBinding·UISecondaryClickBinding은 배경에 자동 연결한다.")]
        [SerializeField] private Image statusIconPrefab;
        [Tooltip("상태 아이콘의 가로 간격입니다. 배치 폭은 Status Root의 RectTransform에서 읽습니다.")]
        [SerializeField, Min(0f)] private float statusIconSpacing = 10f;
        [Tooltip("여러 행으로 배치할 때 행 사이의 세로 간격입니다.")]
        [SerializeField, Min(0f)] private float statusIconRowSpacing = 4f;
        [SerializeField] private GameObject descriptionPanel;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject buffPanel;
        [SerializeField] private TMP_Text buffTitleText;
        [SerializeField] private TMP_Text buffDescriptionText;

        private bool _hasStatusLayout;
        private UnitHUDStatusLayout _statusLayout;
        private readonly List<StatusSlot> _statusSlots = new();
        private readonly List<UnitHUDStatus> _statuses = new();
        private readonly HealthFillAnimation _healthAnimation = new();
        private bool _hasHealth;
        private int _currentHealth, _maxHealth, _currentShield;
        private double HealthBarDenominator => Math.Max(_maxHealth, (double)_currentHealth + _currentShield);
        private int? _previewHealth;
        private float _healthPreviewElapsed;
        private float _healthPreviewTextElapsed;
        private Color _healthPreviewSubFillColor;
        private bool _healthPreviewColorCaptured;
        private Color _healthPreviewTextColor;
        private bool _healthPreviewTextColorCaptured;
        private int _hoveredStatus = -1, _pinnedStatus = -1;
        private Owner _owner = Owner.Player;
        private bool _isObstacle;
        private bool _isRider;
        private EntityId _entityId;
        // 상태 아이콘 우클릭을 받는 쪽입니다(가방 설계 P6). 없으면 우클릭은 아무것도 하지 않습니다.
        private Action<EntityId, string> _statusSecondary;

        private sealed class StatusSlot
        {
            public Button Button;
            public Image Root;
            public Image Icon;
            public UIInputBinding Input;
            public UISecondaryClickBinding Secondary;
            public Selectable.Transition Transition;
        }

        private void Awake()
        {
            // 데이터 바인딩 전에는 표시를 지운다. 필수 참조는 SetUnit에서 검증한다.
            // HP/텍스트/바탕은 전투 포인터를 가리지 않는다. 설명 가능하거나 우클릭 동작이 있는 아이콘만 나중에 입력을 연다(가방 설계 P6).
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            if (infoPanel != null) infoPanel.Prepare();
            ClearDescription();
            if (itemIcon != null) itemIcon.gameObject.SetActive(false);
            SetActionAvailability(null);
        }

        /// <summary>
        /// View가 CharacterBuffer에서 읽은 유닛을 전달한다. Owner/HP는 같은 Buffer 조회값에서 반영한다.
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
            ValidateReferences();
            if (_entityId != unit.EntityId) Clear();
            _entityId = unit.EntityId;
            _owner = unit.Owner;
            _isObstacle = false;
            _isRider = unit.Stats.Category.Key == CategoryKey.Rider;
            SetUnitExtrasVisible(true);
            SetHealth(unit.Health.CurrentHealth, unit.Health.MaxHealth, unit.TraitEffects.ActiveShieldTotal);
        }

        internal void SetInfo(string unitName, int attack, int shield)
        {
            ValidateReferences();
            infoPanel.SetUnit(unitName, attack, shield);
            infoPanel.CopyHealth(healthFill, healthSubFill, shieldFill);
        }

        internal void SetActionAvailability(CombatActionAvailability availability)
        {
            // 두 Sprite를 지정하기 전에는 표시를 숨깁니다. 바인딩 전 정리도 허용합니다.
            if (availability == null || actionStateSprite1 == null)
            {
                if (actionStateImage != null) actionStateImage.gameObject.SetActive(false);
                return;
            }

            bool attackOnly = availability.CanAttack && !availability.CanMove;
            actionStateImage.sprite = attackOnly ? actionStateSprite2 : actionStateSprite1;
            actionStateImage.color = availability.CanMove || availability.CanAttack ? Color.white : Color.black;
            actionStateImage.gameObject.SetActive(true);
        }

        /// <summary>드로우 전에는 전투 사실을 만들지 않고 기본 위젯만 준비합니다. 독립 정보창은 열지 않습니다.</summary>
        internal void PrepareResources()
        {
            ValidateReferences();
            Clear();
            SetUnitExtrasVisible(true);
        }

        /// <summary>로딩 준비 전용입니다. 닫힌 정보창·툴팁을 열거나 정상 배치에서 강제 갱신하지 않습니다.</summary>
        internal void PrepareActiveMeshes()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(false))
                text.ForceMeshUpdate();
        }

        internal void ValidateReferences()
        {
            if (buffPanel == null || buffTitleText == null || buffDescriptionText == null)
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': buffPanel/buffTitleText/buffDescriptionText 참조가 필요합니다.");
            if (statusRoot == null || !buffPanel.transform.IsChildOf(statusRoot) ||
                !buffTitleText.transform.IsChildOf(buffPanel.transform) || !buffDescriptionText.transform.IsChildOf(buffPanel.transform))
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': BuffPanel은 Status Root 아래, 제목과 설명은 BuffPanel 내부에 있어야 합니다.");
            if (infoPanel == null || healthFill == null || healthSubFill == null || shieldFill == null || healthText == null || maxHealthText == null)
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': infoPanel/healthFill/healthSubFill/shieldFill/healthText/maxHealthText 참조가 필요합니다.");
            if (!infoPanel.transform.IsChildOf(transform) || !healthFill.transform.IsChildOf(transform) ||
                !healthSubFill.transform.IsChildOf(transform) || !shieldFill.transform.IsChildOf(transform) || !healthText.transform.IsChildOf(infoPanel.transform) ||
                !maxHealthText.transform.IsChildOf(infoPanel.transform))
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': infoPanel/HPBar는 같은 HUD, healthText/maxHealthText는 해당 InfoPanel 내부에 있어야 합니다.");
            if (shieldFill == healthFill || shieldFill == healthSubFill || shieldFill.sprite == null ||
                shieldFill.type != Image.Type.Filled || shieldFill.fillMethod != Image.FillMethod.Horizontal || shieldFill.fillOrigin != 0 ||
                shieldFill.transform.parent != healthFill.transform.parent || shieldFill.transform.parent != healthSubFill.transform.parent ||
                shieldFill.transform.GetSiblingIndex() >= healthSubFill.transform.GetSiblingIndex() ||
                healthSubFill.transform.GetSiblingIndex() >= healthFill.transform.GetSiblingIndex())
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': shieldFill은 Sprite가 연결된 왼쪽 시작 가로 Filled Image이며 같은 부모의 Shield → SubFill → Fill 순서가 필요합니다.");
            if (actionStateImage == null || !actionStateImage.transform.IsChildOf(transform) ||
                actionStateImage.transform == transform || actionStateImage.transform.IsChildOf(infoPanel.transform.parent) ||
                actionStateImage.transform.IsChildOf(statusRoot) || statusRoot.IsChildOf(actionStateImage.transform) ||
                actionStateImage == healthFill || actionStateImage == healthSubFill || actionStateImage == shieldFill)
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': actionStateImage는 같은 HUD의 InfoPanelMask/Status Root 밖에 있는 별도 Image여야 합니다.");
            if ((actionStateSprite1 == null) != (actionStateSprite2 == null))
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': 행동 상태를 표시하려면 Sprite 1과 Sprite 2를 함께 지정해야 합니다.");
            if (statusRoot != null && (!statusRoot.IsChildOf(transform) || statusRoot == transform))
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': statusRoot는 같은 HUD 내부에 있어야 합니다.");
            if (itemIcon == null)
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': itemIcon 참조가 필요합니다.");
            if (!itemIcon.transform.IsChildOf(transform) || itemIcon.transform == transform ||
                itemIcon == actionStateImage || itemIcon.transform.IsChildOf(infoPanel.transform.parent) ||
                (statusRoot != null && (itemIcon.transform.IsChildOf(statusRoot) || statusRoot.IsChildOf(itemIcon.transform))) ||
                actionStateImage.transform.IsChildOf(itemIcon.transform) || itemIcon.transform.IsChildOf(actionStateImage.transform))
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': itemIcon은 같은 HUD의 Mask/Status Root 밖, actionStateImage와 별개의 고정 슬롯이어야 합니다.");
            RequireStatusIcon(itemIcon);
            infoPanel.Prepare();
        }

        /// <summary>건물·장애물의 기본 체력바를 표시합니다. 선택 정보는 별도 호출로 전달합니다.</summary>
        internal void SetStructure(StructureCombatFacts structure)
        {
            if (structure == null) throw new ArgumentNullException(nameof(structure));
            ValidateReferences();
            if (_entityId != structure.EntityId) Clear();
            _entityId = structure.EntityId;
            _owner = structure.Owner;
            _isRider = false;
            _isObstacle = ObstacleContentProfile.IsNativeObstacleKey(structure.Definition.Key);
            SetItem(null);
            SetUnitExtrasVisible(false);
            SetHealth(structure.Health.CurrentHealth, structure.Health.MaxHealth);
        }

        internal void SetStructureInfo(string displayName, int attackPower)
        {
            ValidateReferences();
            infoPanel.SetUnit(displayName, attackPower, null);
            infoPanel.CopyHealth(healthFill, healthSubFill, shieldFill);
        }

        private void SetUnitExtrasVisible(bool visible)
        {
            if (!visible) SetActionAvailability(null);
            if (itemIcon != null) itemIcon.gameObject.SetActive(visible && _itemStatus.HasValue);
            if (statusRoot != null) statusRoot.gameObject.SetActive(visible);
        }

        /// <summary>같은 사실의 현재 체력·최대 체력·유효 보호막을 함께 받습니다. 예상 체력은 별도 호출로만 활성화합니다.</summary>
        public void SetHealth(int current, int maximum, int shield = 0)
        {
            int nextMaximum = Mathf.Max(0, maximum);
            bool snap = !_hasHealth || _maxHealth != nextMaximum || !isActiveAndEnabled;
            double previousDenominator = HealthBarDenominator;
            // 실제 피해는 예상 표시가 아닌 직전 실제 HP에서 감소를 시작한다.
            if (_previewHealth.HasValue)
                _healthAnimation.Snap(HealthBarAmount(_currentHealth));
            _maxHealth = nextMaximum;
            _currentHealth = Mathf.Clamp(current, 0, _maxHealth);
            _currentShield = Mathf.Max(0, shield);
            // 분모 변경에 따른 압축/확장을 HP 피해로 연출하지 않습니다.
            snap |= previousDenominator != HealthBarDenominator;
            _previewHealth = null;
            RefreshHealth(snap);
            _hasHealth = true;
        }

        /// <summary>Core 예상 피해 구간만 Alpha로 표시한다. HUD는 피해량을 계산하지 않는다.</summary>
        public void SetHealthPreview(int? projectedHealth)
        {
            bool restoreActual = _previewHealth.HasValue && !projectedHealth.HasValue;
            if (!_previewHealth.HasValue && projectedHealth.HasValue)
            {
                _healthPreviewElapsed = 0f;
                _healthPreviewTextElapsed = 0f;
            }
            _previewHealth = projectedHealth;
            RefreshHealth(restoreActual);
        }

        private void RefreshHealth(bool snap = false)
        {
            int shown = _currentHealth;
            if (maxHealthText != null) maxHealthText.SetText("{0}", _maxHealth);
            if (healthFill != null)
                healthFill.color = _isObstacle ? obstacleHealthColor :
                    _owner == Owner.Enemy ? enemyHealthColor : playerHealthColor;
            float amount = HealthBarAmount(shown);
            if (_previewHealth.HasValue)
            {
                ApplyHealthPreview();
                return;
            }
            RestoreHealthPreviewColor();
            if (healthText != null) healthText.SetText("{0}", shown);
            if (snap) _healthAnimation.Snap(amount);
            else _healthAnimation.SetTarget(amount, fillSecondsPerPercent, subFillSecondsPerPercent);
            ApplyHealthFill();
        }

        private void FixedUpdate()
        {
            if (!_previewHealth.HasValue && _healthAnimation.FixedTick(Time.fixedDeltaTime))
                ApplyHealthFill();
        }

        private void ApplyHealthFill()
        {
            if (healthFill != null) healthFill.fillAmount = _healthAnimation.FillAmount;
            if (healthSubFill != null) healthSubFill.fillAmount = _healthAnimation.SubFillAmount;
            ApplyShieldFill();
            CopyHealthToInfoPanel();
        }

        private float HealthBarAmount(double value)
        {
            double denominator = HealthBarDenominator;
            return denominator > 0d ? (float)(value / denominator) : 0f;
        }

        private void ApplyShieldFill()
        {
            // Awake/바인딩 전 Clear는 허용합니다. 필수 참조는 표시 연결 전에 검증합니다.
            if (shieldFill == null) return;
            shieldFill.fillAmount = HealthBarAmount((double)_currentHealth + _currentShield);
            shieldFill.enabled = _currentShield > 0;
        }

        private void CopyHealthToInfoPanel()
        {
            if (healthFill != null && healthSubFill != null && shieldFill != null)
                infoPanel?.CopyHealth(healthFill, healthSubFill, shieldFill);
        }

        private void ApplyHealthPreview()
        {
            float duration = Mathf.Max(0.05f, healthPreviewPulseDuration);
            int projectedHealth = Mathf.Clamp(_previewHealth.Value, 0, _currentHealth);
            // 텍스트는 Alpha 왕복과 독립된 간격으로 예상/현재 HP를 교대한다.
            float textInterval = Mathf.Max(0.05f, healthPreviewTextInterval);
            int shown = _healthPreviewTextElapsed < textInterval ? projectedHealth : _currentHealth;
            if (healthText != null)
            {
                if (!_healthPreviewTextColorCaptured)
                {
                    _healthPreviewTextColor = healthText.color;
                    _healthPreviewTextColorCaptured = true;
                }
                healthText.SetText("{0}", shown);
                // 숫자 전환과 같은 주기로 예상 HP는 흐려지고 현재 HP는 다시 선명해진다.
                float textAlpha = Mathf.Lerp(Mathf.Clamp01(healthPreviewMinAlpha), 1f,
                    (1f + Mathf.Cos(_healthPreviewTextElapsed / textInterval * Mathf.PI)) * 0.5f);
                Color textColor = _healthPreviewTextColor;
                textColor.a *= textAlpha;
                healthText.color = textColor;
            }
            // 예상 보호막 잔량은 공개되지 않았으므로 실제 분모와 실제 보호막 구간을 유지합니다.
            float currentAmount = HealthBarAmount(_currentHealth);
            float projectedAmount = HealthBarAmount(projectedHealth);
            // 앞의 Fill이 남는 HP를 덮고, 뒤의 SubFill에서 피해 예상 구간만 드러난다.
            if (healthFill != null) healthFill.fillAmount = projectedAmount;
            if (healthSubFill == null) return;
            if (!_healthPreviewColorCaptured)
            {
                _healthPreviewSubFillColor = healthSubFill.color;
                _healthPreviewColorCaptured = true;
            }
            float alpha = Mathf.Lerp(Mathf.Clamp01(healthPreviewMinAlpha), 1f,
                (1f + Mathf.Cos(_healthPreviewElapsed / duration * Mathf.PI * 2f)) * 0.5f);
            Color color = healthFill != null ? healthFill.color : _healthPreviewSubFillColor;
            if (_currentShield > 0)
            {
                // 누적 Shield Fill이 예상 HP 손실 구간에 비치지 않도록 불투명도를 유지하고 명도를 맥동합니다.
                color.r *= alpha;
                color.g *= alpha;
                color.b *= alpha;
            }
            else color.a *= alpha;
            healthSubFill.color = color;
            healthSubFill.fillAmount = currentAmount;
            ApplyShieldFill();
            CopyHealthToInfoPanel();
        }

        private void RestoreHealthPreviewColor()
        {
            if (_healthPreviewTextColorCaptured)
            {
                if (healthText != null) healthText.color = _healthPreviewTextColor;
                _healthPreviewTextColorCaptured = false;
            }
            if (!_healthPreviewColorCaptured) return;
            if (healthSubFill != null) healthSubFill.color = _healthPreviewSubFillColor;
            _healthPreviewColorCaptured = false;
        }

        /// <summary>유닛 진영을 명시적으로 전달한다. 체력바 색에 반영한다.</summary>
        public void SetOwner(Owner unitOwner)
        {
            if (unitOwner != Owner.Player && unitOwner != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(unitOwner));
            _owner = unitOwner;
            RefreshHealth();
        }

        private void LateUpdate()
        {
            AnimateHealthPreview(Time.unscaledDeltaTime);
            RefreshStatusLayout();
        }

        /// <summary>예상 피해 구간의 Alpha만 갱신한다. 반복된 예상값 전달은 주기를 초기화하지 않는다.</summary>
        internal void AnimateHealthPreview(float unscaledDeltaTime)
        {
            if (!_previewHealth.HasValue) return;
            float duration = Mathf.Max(0.05f, healthPreviewPulseDuration);
            _healthPreviewElapsed = Mathf.Repeat(_healthPreviewElapsed + Mathf.Max(0f, unscaledDeltaTime), duration);
            float textInterval = Mathf.Max(0.05f, healthPreviewTextInterval);
            _healthPreviewTextElapsed = Mathf.Repeat(_healthPreviewTextElapsed + Mathf.Max(0f, unscaledDeltaTime), textInterval * 2f);
            ApplyHealthPreview();
        }

        /// <summary>프리팹에 배치된 고정 아이템 한 칸입니다. 버프/디버프 목록과 레이아웃에는 넣지 않습니다.</summary>
        internal void SetItem(UnitHUDStatus? item)
        {
            if (item.HasValue)
            {
                ValidateReferences();
                if (_itemSlot == null)
                {
                    Image icon = RequireStatusIcon(itemIcon);
                    Button button = itemIcon.GetComponent<Button>();
                    if (button == null) button = itemIcon.gameObject.AddComponent<Button>();
                    if (button.targetGraphic == null) button.targetGraphic = itemIcon;
                    icon.raycastTarget = false;
                    _itemFallbackIcon = icon.sprite;
                    _itemSlot = new StatusSlot
                    {
                        Root = itemIcon, Icon = icon, Button = button, Transition = button.transition,
                        Input = UIInputBinding.Attach(button, this, ItemSlot, hasHover: true),
                        Secondary = UISecondaryClickBinding.Attach(itemIcon.gameObject, this, ItemSlot)
                    };
                }
            }
            bool sameKey = _itemStatus.HasValue && item.HasValue && _itemStatus.Value.Key == item.Value.Key;
            if (!sameKey)
            {
                _itemSlot?.Input.OnHoverExit();
                if (_hoveredStatus == ItemSlot) _hoveredStatus = -1;
                if (_pinnedStatus == ItemSlot) _pinnedStatus = -1;
            }
            _itemStatus = item;
            if (item.HasValue)
            {
                UnitHUDStatus status = item.Value;
                _itemSlot.Icon.sprite = status.Icon != null ? status.Icon : _itemFallbackIcon;
                bool canDescribe = CanDescribe(ItemSlot);
                if (!canDescribe) _itemSlot.Input.OnHoverExit();
                _itemSlot.Button.transition = canDescribe ? _itemSlot.Transition : Selectable.Transition.None;
                _itemSlot.Button.interactable = canDescribe;
                _itemSlot.Root.raycastTarget = canDescribe || status.HasSecondaryAction;
            }
            else if (_itemSlot != null)
            {
                _itemSlot.Icon.sprite = null;
                _itemSlot.Root.raycastTarget = false;
                _itemSlot.Button.interactable = false;
            }
            if (itemIcon != null) itemIcon.gameObject.SetActive(item.HasValue);
            RefreshDescription();
        }

        /// <summary>View가 준비한 버프/디버프 아이콘과 설명. 순서 변경 시에도 Key로 선택을 유지한다.</summary>
        public void SetStatuses(IReadOnlyList<UnitHUDStatus> statuses)
        {
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            // 구성 오류는 기존 상태/입력 수명을 바꾸기 전에 거절합니다.
            if (statuses.Count > 0) ReadStatusLayout(statuses.Count);
            string pinnedKey = IsStatusValid(_pinnedStatus) ? StatusAt(_pinnedStatus).Key : null;
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
            Image iconTemplate = statusRoot != null && statusIconPrefab != null && statuses.Count > 0
                ? RequireStatusIcon(statusIconPrefab) : null;
            if (!sameKeys)
            {
                for (int i = 0; i < _statusSlots.Count; i++) _statusSlots[i].Input.OnHoverExit();
                if (_hoveredStatus != ItemSlot) _hoveredStatus = -1;
            }
            _statuses.Clear();
            _pinnedStatus = pinnedKey != null && _itemStatus.HasValue && _itemStatus.Value.Key == pinnedKey ? ItemSlot : -1;
            for (int i = 0; i < statuses.Count; i++)
            {
                _statuses.Add(statuses[i]);
                if (statuses[i].Key == pinnedKey) _pinnedStatus = i;
            }
            if (statusRoot != null && statusIconPrefab != null)
            {
                while (_statusSlots.Count < statuses.Count)
                {
                    Image root = Instantiate(statusIconPrefab, statusRoot, false);
                    Image icon = RequireStatusIcon(root);
                    Button button = root.GetComponent<Button>();
                    if (button == null) button = root.gameObject.AddComponent<Button>();
                    if (button.targetGraphic == null) button.targetGraphic = root;
                    root.raycastTarget = false;
                    icon.raycastTarget = false;
                    RectTransform rect = root.rectTransform;
                    Vector2 size = rect.rect.size;
                    rect.anchorMin = rect.anchorMax = Vector2.zero;
                    rect.pivot = Vector2.zero;
                    rect.sizeDelta = size;
                    _statusSlots.Add(new StatusSlot
                    {
                        Button = button, Root = root, Icon = icon, Transition = button.transition,
                        Input = UIInputBinding.Attach(button, this, _statusSlots.Count, hasHover: true),
                        Secondary = UISecondaryClickBinding.Attach(root.gameObject, this, _statusSlots.Count)
                    });
                }
                for (int i = 0; i < _statusSlots.Count; i++)
                {
                    StatusSlot slot = _statusSlots[i];
                    if (i < statuses.Count)
                    {
                        slot.Icon.sprite = statuses[i].Icon != null ? statuses[i].Icon : iconTemplate.sprite;
                        // Description 미구현/참조 누락이면 아이콘 표시만 한다.
                        bool canDescribe = CanDescribe(i);
                        if (!canDescribe) slot.Input.OnHoverExit();
                        slot.Button.transition = canDescribe ? slot.Transition : Selectable.Transition.None;
                        slot.Button.interactable = canDescribe;
                        // 설명이 없어도 우클릭 동작(아이템 해제 등)이 있으면 입력을 받는다. 왼쪽 클릭 고정은 설명 가능할 때만이다(가방 설계 P6).
                        slot.Root.raycastTarget = canDescribe || statuses[i].HasSecondaryAction;
                    }
                    slot.Button.gameObject.SetActive(i < statuses.Count);
                }
            }
            RefreshStatusLayout(force: true);
            RefreshDescription();
        }

        /// <summary>게임 상태를 다시 읽지 않고 실제 가로 폭이 바뀔 때만 기존 슬롯을 재배치합니다.</summary>
        private void RefreshStatusLayout(bool force = false)
        {
            RectTransform area = statusRoot as RectTransform;
            if (_statuses.Count == 0)
            {
                if (area != null && area.rect.height != 0f)
                    area.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
                _hasStatusLayout = false;
                return;
            }

            if (!force && _hasStatusLayout && area != null && _statusLayout.Width == area.rect.width) return;
            UnitHUDStatusLayout layout = ReadStatusLayout(_statuses.Count);
            if (!force && _hasStatusLayout && _statusLayout.Equals(layout)) return;
            area.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, layout.Height);
            for (int i = 0; i < layout.Count; i++)
            {
                layout.PositionOf(i, out float x, out float y);
                _statusSlots[i].Root.rectTransform.anchoredPosition3D = new Vector3(x, y, 0f);
            }
            _statusLayout = layout;
            _hasStatusLayout = true;
        }

        /// <summary>표현에 필요한 크기만 읽습니다. 아이콘의 분류/효과/입력은 해석하지 않습니다.</summary>
        private UnitHUDStatusLayout ReadStatusLayout(int count)
        {
            RectTransform area = statusRoot as RectTransform;
            if (area == null || statusIconPrefab == null)
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': statusRoot(RectTransform)/statusIconPrefab 연결이 필요합니다.");
            if (area.pivot != Vector2.zero || area.anchorMin.y != area.anchorMax.y)
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': statusRoot의 Pivot은 (0, 0), 세로 Anchor는 고정이어야 합니다.");
            if (area.GetComponent<LayoutGroup>() != null || area.GetComponent<ContentSizeFitter>() != null ||
                statusIconPrefab.GetComponent<ContentSizeFitter>() != null)
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': statusRoot의 LayoutGroup/ContentSizeFitter와 statusIconPrefab의 ContentSizeFitter는 코드 배치와 함께 사용할 수 없습니다.");
            RectTransform template = statusIconPrefab.rectTransform;
            Vector3 scale = template.localScale;
            if (!(scale.x > 0f) || !(scale.y > 0f) || template.localRotation != Quaternion.identity)
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': statusIconPrefab은 양수 X/Y 배율과 회전 (0, 0, 0)이 필요합니다.");
            try
            {
                return new UnitHUDStatusLayout(area.rect.width, template.rect.width * scale.x,
                    template.rect.height * scale.y, statusIconSpacing, statusIconRowSpacing, count);
            }
            catch (ArgumentException error)
            {
                throw new InvalidOperationException($"[UnitHUD] UIId.UnitHUDLayer, '{name}': statusRoot 폭/statusIconPrefab 크기/statusIconSpacing/statusIconRowSpacing이 유효하지 않습니다. {error.Message}", error);
            }
        }

        private static Image RequireStatusIcon(Image root)
        {
            Transform iconTransform = root.transform.Find("Icon");
            Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (icon == null)
                throw new InvalidOperationException($"[UnitHUD] statusIconPrefab '{root.name}'의 자식 Icon에 Image가 필요합니다.");
            return icon;
        }

        void IUIInputReceiver.Click(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIUnitHUDClickSample = _profileUIUnitHUDClick.Auto();
#endif

            // 버프는 호버 중에만 표시하고, 장착 아이템만 기존 클릭 고정을 사용합니다.
            if (slot != ItemSlot) return;
            if (!isActiveAndEnabled || !CanDescribe(slot)) return;
            _pinnedStatus = _pinnedStatus == slot ? -1 : slot;
            // 같은 아이콘을 다시 클릭하면 hover 중이어도 닫는다.
            if (_pinnedStatus < 0) _hoveredStatus = -1;
            RefreshDescription();
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIUnitHUDHoverEnterSample = _profileUIUnitHUDHoverEnter.Auto();
#endif

            if (!isActiveAndEnabled || !CanDescribe(slot)) return;
            _hoveredStatus = slot;
            RefreshDescription();
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIUnitHUDHoverExitSample = _profileUIUnitHUDHoverExit.Auto();
#endif

            if (_hoveredStatus == slot) _hoveredStatus = -1;
            RefreshDescription();
        }

        /// <summary>
        /// 상태 아이콘 우클릭을 받을 쪽을 정합니다(가방 설계 P6). 우클릭 동작이 있는 상태(<see cref="UnitHUDStatus.HasSecondaryAction"/>)만
        /// (이 HUD의 개체 ID, 상태 Key)를 넘깁니다. HUD는 그 동작이 무엇인지 해석하지 않습니다. null이면 우클릭을 무시합니다.
        /// </summary>
        public void SetStatusSecondaryHandler(Action<EntityId, string> handler) => _statusSecondary = handler;

        void IUISecondaryClickReceiver.SecondaryClick(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIUnitHUDSecondaryClickSample = _profileUIUnitHUDSecondaryClick.Auto();
#endif

            if (!isActiveAndEnabled || _statusSecondary == null || _entityId.IsEmpty ||
                !IsStatusValid(slot) || !StatusAt(slot).HasSecondaryAction)
                return;
            _statusSecondary(_entityId, StatusAt(slot).Key);
        }

        private bool IsStatusValid(int slot) => slot == ItemSlot ? _itemStatus.HasValue : slot >= 0 && slot < _statuses.Count;
        private UnitHUDStatus StatusAt(int slot) => slot == ItemSlot ? _itemStatus.Value : _statuses[slot];
        private bool CanDescribe(int slot) => IsStatusValid(slot) &&
            (slot == ItemSlot
                ? descriptionPanel != null && descriptionText != null && !string.IsNullOrWhiteSpace(StatusAt(slot).Description)
                : buffPanel != null && buffTitleText != null && buffDescriptionText != null &&
                    (!string.IsNullOrWhiteSpace(StatusAt(slot).Title) || !string.IsNullOrWhiteSpace(StatusAt(slot).Description)));

        private void RefreshDescription()
        {
            int slot = IsStatusValid(_pinnedStatus) ? _pinnedStatus : _hoveredStatus;
            bool show = isActiveAndEnabled && slot == ItemSlot && CanDescribe(slot);
            if (descriptionText != null)
                descriptionText.SetText(show ? StatusAt(slot).Description : string.Empty);
            if (descriptionPanel != null) descriptionPanel.SetActive(show);
            bool showBuff = isActiveAndEnabled && _hoveredStatus >= 0 && CanDescribe(_hoveredStatus);
            if (buffTitleText != null)
                buffTitleText.SetText(showBuff ? StatusAt(_hoveredStatus).Title : string.Empty);
            if (buffDescriptionText != null)
                buffDescriptionText.SetText(showBuff ? StatusAt(_hoveredStatus).Description : string.Empty);
            if (buffPanel != null)
            {
                if (showBuff) buffPanel.transform.SetAsLastSibling();
                buffPanel.SetActive(showBuff);
            }
        }

        private void ClearDescription()
        {
            _hoveredStatus = _pinnedStatus = -1;
            RefreshDescription();
        }

        /// <summary>화면에서 숨길 때 hover만 정리합니다. 핀 선택·수치·진행 중인 연출은 보존합니다.</summary>
        internal void ClearStatusHover()
        {
            _itemSlot?.Input.OnHoverExit();
            for (int i = 0; i < _statusSlots.Count; i++) _statusSlots[i].Input.OnHoverExit();
            _hoveredStatus = -1;
            RefreshDescription();
        }

        /// <summary>Hover 해제, 확정/취소/모드 변경 뒤 예상 체력 표시를 지운다.</summary>
        public void ClearPreview()
        {
            bool restoreActual = _previewHealth.HasValue;
            _previewHealth = null;
            RefreshHealth(restoreActual);
        }

        /// <summary>다른 유닛에 재사용하거나 풀 반환 전에 호출한다.</summary>
        public void Clear()
        {
            if (damageText != null) damageText.Clear();
            SetItem(null);
            // 바인딩 전 정리와 반복 풀 반환도 허용한다.
            _entityId = EntityId.Empty;
            _isRider = false;
            _owner = Owner.Player;
            _isObstacle = false;
            SetUnitExtrasVisible(false);
            for (int i = 0; i < _statusSlots.Count; i++)
            {
                _statusSlots[i].Input.OnHoverExit();
                _statusSlots[i].Button.gameObject.SetActive(false);
            }
            _statuses.Clear();
            RefreshStatusLayout(force: true);
            ClearDescription();
            _hasHealth = false;
            SetHealth(0, 0);
        }

        protected override void OnUIHidden()
        {
            _itemSlot?.Input.OnHoverExit();
            for (int i = 0; i < _statusSlots.Count; i++) _statusSlots[i].Input.OnHoverExit();
            ClearDescription();
            ClearPreview();
            _healthAnimation.Complete();
            ApplyHealthFill();
            _hasHealth = false;
        }
    }

    /// <summary>Core 상태와 View DB를 조합해 호출자가 만든 표시 데이터. HUD는 효과 규칙을 해석하지 않는다.</summary>
    public readonly struct UnitHUDStatus
    {
        public string Key { get; }
        public Sprite Icon { get; }
        public string Description { get; }
        public string Title { get; }

        /// <summary>우클릭 동작이 있는지입니다(가방 설계 P6). 참이면 설명이 없어도 아이콘이 입력을 받고 우클릭을 HUD 처리기로 넘깁니다.</summary>
        public bool HasSecondaryAction { get; }

        public UnitHUDStatus(string key, Sprite icon, string description, bool hasSecondaryAction = false, string title = null)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Status Key가 필요합니다.", nameof(key));
            Key = key;
            Icon = icon;
            Description = description ?? string.Empty;
            Title = title ?? string.Empty;
            HasSecondaryAction = hasSecondaryAction;
        }
    }
}
