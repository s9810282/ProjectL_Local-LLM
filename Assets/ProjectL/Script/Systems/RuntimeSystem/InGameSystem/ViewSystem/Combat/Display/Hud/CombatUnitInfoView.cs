using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>
    /// 등록된 UnitInfo 내부의 초상·전투 수치·행동 가능 상태·특성·상태 아이콘을 표시합니다.
    /// 특성 칸에는 아이콘만 그리고, 이름·설명은 특성 칸 호버 때 공용 Hover 패널에 띄웁니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatUnitInfoView : UIWidgetBase
    {
        private const int AbilitySlotCount = 3;

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text structureInfoText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private GameObject levelRoot;
        [SerializeField] private GameObject categoryRoot;
        [SerializeField] private Image categoryIcon;
        [SerializeField] private TMP_Text categoryText;
        [Tooltip("프리팹에 배치한 특성 세 개의 Icon을 연결합니다. 이름·설명 호버는 아이콘을 감싼 특성 칸의 Button이 받습니다. 비활성 슬롯도 내용을 갱신하며 활성 상태는 유지합니다.")]
        [SerializeField] private AbilityText[] abilitySlots;
        [SerializeField] private Image unitIcon;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthSubFill;
        [Tooltip("HP/SubFill 뒤에서 현재 HP와 보호막의 합계까지 채우는 보호막입니다.")]
        [SerializeField] private Image shieldFill;
        [Tooltip("Fill이 HP 1%를 줄이는 시간(초). 0이면 즉시 반영합니다.")]
        [SerializeField, Min(0f)] private float fillSecondsPerPercent = 0.003f;
        [Tooltip("Fill 감소 완료 후 SubFill이 HP 1%를 줄이는 시간(초). 0이면 즉시 반영합니다.")]
        [SerializeField, Min(0f)] private float subFillSecondsPerPercent = 0.01f;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text currentHealthText;
        [SerializeField] private TMP_Text attackPowerText;
        [SerializeField] private TMP_Text defenseText;
        [SerializeField] private Image moveAvailability;
        [SerializeField] private Image attackAvailability;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color unavailableColor = new(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private GridLayoutGroup statusLayout;
        [SerializeField] private Image statusIconTemplate;
        [SerializeField] private RectTransform statusTooltipPanel;
        [SerializeField] private TMP_Text statusTitleText;
        [SerializeField] private TMP_Text statusDescriptionText;
        [SerializeField] private Vector2 statusTooltipOffset = new(8f, 8f);

        private readonly List<Image> _statusIcons = new();
        private readonly List<UIInputBinding> _statusBindings = new();
        private readonly List<UnitHUDStatus> _statusContent = new();
        private StatusInputReceiver _statusReceiver;
        private int _hoveredStatus = -1;
        private RectTransform _statusLayoutRect;
        private float _statusWidth;
        private int _statusCount;

        // 특성 설명 호버입니다. 입력 연결·배열은 준비 때 한 번만 만들고 호버 중에는 새로 할당하지 않습니다.
        private readonly UIInputBinding[] _abilityBindings = new UIInputBinding[AbilitySlotCount];
        private IUITextPresentation _hoverText;
        private Canvas _canvas;
        private int _hoveredAbility = -1;
        private bool _abilityHoverShown;
        private uint _abilityHoverVersion;
        private string _abilityHoverName;
        private string _abilityHoverDescription;

        private bool _prepared;
        private bool _hasContent;
        private bool _warnedAbilityOverflow;
        private readonly HealthFillAnimation _healthAnimation = new();
        private bool _hasHealth;
        private int _currentHealth, _maxHealth, _currentShield;
        private double HealthBarDenominator => Math.Max(_maxHealth, (double)_currentHealth + _currentShield);

        internal void ValidateReferences()
        {
            if (levelRoot == null || categoryRoot == null || !IsChild(levelRoot.transform) || !IsChild(categoryRoot.transform) ||
                levelText == null || categoryIcon == null || categoryText == null ||
                !levelText.transform.IsChildOf(levelRoot.transform) ||
                !categoryIcon.transform.IsChildOf(categoryRoot.transform) || !categoryText.transform.IsChildOf(categoryRoot.transform))
                throw ConfigurationError("levelRoot·categoryRoot와 그 내부의 레벨·병과 참조가 필요합니다.");
            if (currentHealthText == null || healthText == currentHealthText)
                throw ConfigurationError("healthText(MaxHP)와 구분된 currentHealthText(CurrHP) 참조가 필요합니다.");
            if (unitIcon == null || healthFill == null || healthSubFill == null || shieldFill == null || healthText == null || attackPowerText == null ||
                defenseText == null || moveAvailability == null || attackAvailability == null || statusLayout == null || statusIconTemplate == null)
                throw ConfigurationError("초상·healthFill·healthSubFill·shieldFill·HP 텍스트·공격력·방어력·행동 가능 표시·상태 아이콘 참조가 필요합니다.");
            if (healthFill == healthSubFill || healthFill.sprite == null || healthSubFill.sprite == null ||
                healthFill.type != Image.Type.Filled || healthSubFill.type != Image.Type.Filled ||
                healthFill.fillMethod != Image.FillMethod.Horizontal || healthSubFill.fillMethod != Image.FillMethod.Horizontal ||
                healthFill.fillOrigin != 0 || healthSubFill.fillOrigin != 0 ||
                healthFill.transform.parent != healthSubFill.transform.parent ||
                healthSubFill.transform.GetSiblingIndex() >= healthFill.transform.GetSiblingIndex())
                throw ConfigurationError("healthFill·healthSubFill은 Sprite가 연결된 왼쪽 시작 가로 Filled Image이며, 같은 부모에서 SubFill을 Fill 뒤에 그려야 합니다.");
            if (shieldFill == healthFill || shieldFill == healthSubFill || shieldFill.sprite == null ||
                shieldFill.type != Image.Type.Filled || shieldFill.fillMethod != Image.FillMethod.Horizontal || shieldFill.fillOrigin != 0 ||
                shieldFill.transform.parent != healthFill.transform.parent ||
                shieldFill.transform.GetSiblingIndex() >= healthSubFill.transform.GetSiblingIndex())
                throw ConfigurationError("shieldFill은 Sprite가 연결된 왼쪽 시작 가로 Filled Image이며 같은 부모의 Shield → SubFill → Fill 순서가 필요합니다.");
            if (!IsChild(unitIcon.transform) || !IsChild(healthFill.transform) || !IsChild(healthSubFill.transform) || !IsChild(shieldFill.transform) || !IsChild(healthText.transform) ||
                !IsChild(currentHealthText.transform) ||
                !IsChild(attackPowerText.transform) || !IsChild(defenseText.transform) || !IsChild(moveAvailability.transform) ||
                !IsChild(attackAvailability.transform) || !IsChild(statusLayout.transform) || statusIconTemplate.transform.parent != statusLayout.transform)
                throw ConfigurationError("기본 정보는 UnitInfo 내부, statusIconTemplate은 statusLayout의 직계 자식이어야 합니다.");
            if (!statusLayout.enabled || statusLayout.constraint != GridLayoutGroup.Constraint.Flexible ||
                statusLayout.startCorner != GridLayoutGroup.Corner.UpperLeft || statusLayout.startAxis != GridLayoutGroup.Axis.Horizontal ||
                statusLayout.cellSize.x <= 0f || statusLayout.cellSize.y <= 0f || statusLayout.spacing.x < 0f || statusLayout.spacing.y < 0f)
                throw ConfigurationError("상태 아이콘은 양수 크기·음수가 아닌 간격의 좌상단 가로 Flexible Grid여야 합니다.");
            if (statusTooltipPanel == null || statusTitleText == null || statusDescriptionText == null)
                throw ConfigurationError("statusTooltipPanel·statusTitleText·statusDescriptionText 참조가 필요합니다.");
            if (!IsChild(statusTooltipPanel) || statusTooltipPanel.IsChildOf(statusLayout.transform) ||
                statusTitleText.transform == statusTooltipPanel || statusDescriptionText.transform == statusTooltipPanel ||
                !statusTitleText.transform.IsChildOf(statusTooltipPanel) || !statusDescriptionText.transform.IsChildOf(statusTooltipPanel))
                throw ConfigurationError("상태 설명 패널은 Grid 바깥의 UnitInfo 자식, 제목·설명은 패널 내부여야 합니다.");
            CanvasGroup statusGroup = statusTooltipPanel.GetComponent<CanvasGroup>();
            if (statusGroup == null || statusGroup.blocksRaycasts)
                throw ConfigurationError("statusTooltipPanel에 blocksRaycasts=false인 CanvasGroup이 필요합니다.");
            if (statusIconTemplate.GetComponent<Button>() == null)
                throw ConfigurationError("statusIconTemplate에 호버 입력용 Button이 필요합니다.");
            if (!(transform is RectTransform))
                throw ConfigurationError("화면에 RectTransform이 필요합니다.");
            if ((nameText != null && !IsChild(nameText.transform)) || !IsChild(levelText.transform) || !IsChild(categoryIcon.transform) ||
                !IsChild(categoryText.transform))
                throw ConfigurationError("이름·레벨·카테고리는 화면 내부에 있어야 합니다.");
            if (abilitySlots == null || abilitySlots.Length != AbilitySlotCount)
                throw ConfigurationError($"abilitySlots는 {AbilitySlotCount}개여야 합니다.");
            for (int i = 0; i < abilitySlots.Length; i++)
            {
                if (abilitySlots[i] == null) throw ConfigurationError($"abilitySlots[{i}] 참조가 필요합니다.");
                abilitySlots[i].Validate(this, i);
                for (int previous = 0; previous < i; previous++)
                    if (abilitySlots[i].SharesReferencesWith(abilitySlots[previous]))
                        throw ConfigurationError($"abilitySlots[{i}]의 Icon·호버 Button 참조가 다른 슬롯과 중복됩니다.");
            }
        }

        internal void Prepare()
        {
            if (_prepared) return;
            if (structureInfoText == null || !structureInfoText.transform.IsChildOf(transform))
                throw ConfigurationError("같은 정보창 내부의 structureInfoText 참조가 필요합니다.");
            ValidateReferences();
            // 등록 전 숨김 상태에서도 준비하므로 비활성 부모까지 찾습니다. 화면 좌표 변환에만 씁니다.
            _canvas = GetComponentInParent<Canvas>(true);
            if (_canvas == null) throw ConfigurationError("특성 설명 위치를 계산할 상위 Canvas가 필요합니다.");
            _statusLayoutRect = (RectTransform)statusLayout.transform;
            _statusReceiver = new StatusInputReceiver(this);
            AddStatusIcon(statusIconTemplate);
            var abilityReceiver = new AbilityInputReceiver(this);
            for (int i = 0; i < abilitySlots.Length; i++)
                _abilityBindings[i] = UIInputBinding.Attach(abilitySlots[i].HoverButton, abilityReceiver, i, hasHover: true);
            _prepared = true;
            Clear();
        }

        /// <summary>특성 설명을 띄울 공용 Hover 패널 서비스입니다. 연결 전의 호버는 정상 대기라 아무것도 띄우지 않습니다.</summary>
        internal void BindHoverText(IUITextPresentation text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            // 다른 서비스로 바뀌면 이전 서비스에 띄운 패널을 먼저 닫습니다.
            if (!ReferenceEquals(_hoverText, text)) ResetAbilityHover();
            _hoverText = text;
        }

        private void AddStatusIcon(Image icon)
        {
            int index = _statusIcons.Count;
            _statusIcons.Add(icon);
            icon.raycastTarget = true;
            _statusBindings.Add(UIInputBinding.Attach(icon.GetComponent<Button>(), _statusReceiver, index, hasHover: true));
        }

        internal void Show(string unitName, int? level, Sprite category, string categoryName, UnitCardTraitContent[] abilities,
            bool showCharacterDetails = true)
        {
            if (abilities == null) throw new ArgumentNullException(nameof(abilities));
            // 임시(2026-11-03): 칸을 늘리기 전까지 앞 세 개만 보이고 멈추지 않습니다. 잘린 사실은 한 번만 알립니다.
            if (abilities.Length > AbilitySlotCount && !_warnedAbilityOverflow)
            {
                _warnedAbilityOverflow = true;
                Debug.LogWarning($"[CombatUnitInfoView] '{name}': 특성 {abilities.Length}개 중 앞 {AbilitySlotCount}개만 표시합니다. 고정 슬롯은 {AbilitySlotCount}개입니다.", this);
            }
            Prepare();
            SetCharacterDetailsVisible(showCharacterDetails);
            _hasContent = true;
            if (nameText != null) nameText.text = unitName;
            levelText.text = level?.ToString() ?? "-";
            categoryIcon.sprite = category;
            categoryIcon.enabled = category != null;
            categoryText.text = categoryName;
            for (int i = 0; i < abilitySlots.Length; i++)
                abilitySlots[i].Render(i < abilities.Length ? abilities[i] : default,
                    showCharacterDetails && i < abilities.Length);
            RefreshAbilityHover();
        }

        /// <summary>공통 내용과 선택적인 캐릭터 그룹을 한 곳에서 적용합니다.</summary>
        internal void ShowInspection(CombatInspectionInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            var character = info.Character;
            bool hasCharacter = character != null;
            Show(info.Name, character?.Level, character?.Category, character?.CategoryName ?? string.Empty,
                character?.Traits ?? Array.Empty<UnitCardTraitContent>(), showCharacterDetails: hasCharacter);
            ShowCombat(info.Icon, info.Health, info.MaxHealth, character?.Attack ?? info.StructureAttackPower ?? 0, character?.Defense ?? 0,
                character?.Statuses ?? Array.Empty<UnitHUDStatus>(), info.Shield);
            attackPowerText.gameObject.SetActive(true);
            moveAvailability.gameObject.SetActive(hasCharacter);
            attackAvailability.gameObject.SetActive(hasCharacter);
            structureInfoText.gameObject.SetActive(!hasCharacter);
            if (!hasCharacter)
            {
                ResetHover();
                string owner = info.Owner == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner.Enemy ? "적" : "아군";
                structureInfoText.text = owner + "\n" + info.Description;
            }
        }

        private void SetCharacterDetailsVisible(bool visible)
        {
            levelRoot.SetActive(visible);
            categoryRoot.SetActive(visible);
            defenseText.gameObject.SetActive(visible);
        }

        internal void Clear()
        {
            // 등록 전 Hide에서도 호버 정리는 허용합니다.
            ResetHover();
            if (!_prepared) return;
            if (structureInfoText != null) structureInfoText.gameObject.SetActive(false);
            _hasContent = false;
            if (nameText != null) nameText.text = string.Empty;
            levelText.text = "-";
            categoryIcon.sprite = null;
            categoryIcon.enabled = false;
            categoryText.text = string.Empty;
            foreach (AbilityText slot in abilitySlots) slot.Render(default, false);
            unitIcon.sprite = null;
            unitIcon.enabled = false;
            _hasHealth = false;
            _currentHealth = _maxHealth = _currentShield = 0;
            _healthAnimation.Snap(0f);
            ApplyHealthFill();
            healthText.text = string.Empty;
            currentHealthText.text = string.Empty;
            attackPowerText.text = string.Empty;
            defenseText.text = string.Empty;
            SetActionAvailability(false, false);
            _statusCount = 0;
            _statusContent.Clear();
            foreach (Image icon in _statusIcons) icon.gameObject.SetActive(false);
            statusLayout.gameObject.SetActive(false);
            ResizeStatusLayout();
        }

        /// <summary>같은 표시 시점의 공개값을 그대로 그립니다. 능력치·효과 규칙은 판정하지 않습니다.</summary>
        internal void ShowCombat(Sprite portrait, int health, int maxHealth, int attackPower, int defense,
            IReadOnlyList<UnitHUDStatus> statuses, int shield = 0)
        {
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            Prepare();
            unitIcon.sprite = portrait;
            unitIcon.enabled = portrait != null;
            SetHealth(health, maxHealth, shield);
            healthText.text = $"/ {maxHealth}";
            currentHealthText.text = health.ToString();
            attackPowerText.text = $"공격력 {attackPower}";
            defenseText.text = $"방어력 {defense}";
            // 목록은 호출자가 재사용하므로 표시 시점의 값을 보관합니다. 다른 효과로 교체되면 호버를 해제합니다.
            if (_hoveredStatus >= 0 && (_hoveredStatus >= statuses.Count ||
                _statusContent[_hoveredStatus].Key != statuses[_hoveredStatus].Key)) ResetStatusHover();
            _statusContent.Clear();
            for (int i = 0; i < statuses.Count; i++) _statusContent.Add(statuses[i]);
            while (_statusIcons.Count < statuses.Count)
                AddStatusIcon(Instantiate(statusIconTemplate, statusLayout.transform, false));
            _statusCount = statuses.Count;
            for (int i = 0; i < _statusIcons.Count; i++)
            {
                Image icon = _statusIcons[i];
                icon.sprite = i < statuses.Count ? statuses[i].Icon : null;
                icon.raycastTarget = true;
                icon.gameObject.SetActive(i < statuses.Count);
            }
            statusLayout.gameObject.SetActive(statuses.Count > 0);
            ResizeStatusLayout();
            if (_hoveredStatus >= 0) ShowStatusTooltip();
        }

        private void SetHealth(int health, int maxHealth, int shield)
        {
            int nextMaximum = Mathf.Max(0, maxHealth);
            bool snap = !_hasHealth || _maxHealth != nextMaximum || !isActiveAndEnabled;
            double previousDenominator = HealthBarDenominator;
            _maxHealth = nextMaximum;
            _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
            _currentShield = Mathf.Max(0, shield);
            double denominator = HealthBarDenominator;
            float amount = denominator > 0d ? (float)(_currentHealth / denominator) : 0f;
            // HUD와 같이 분모 변경에 따른 압축·확장을 HP 피해로 연출하지 않습니다.
            if (snap || previousDenominator != denominator)
                _healthAnimation.Snap(amount);
            else
                _healthAnimation.SetTarget(amount, fillSecondsPerPercent, subFillSecondsPerPercent);
            _hasHealth = true;
            ApplyHealthFill();
        }

        private void FixedUpdate()
        {
            if (_healthAnimation.FixedTick(Time.fixedDeltaTime)) ApplyHealthFill();
        }

        private void ApplyHealthFill()
        {
            healthFill.fillAmount = _healthAnimation.FillAmount;
            healthSubFill.fillAmount = _healthAnimation.SubFillAmount;
            double denominator = HealthBarDenominator;
            shieldFill.fillAmount = denominator > 0d
                ? (float)(((double)_currentHealth + _currentShield) / denominator) : 0f;
            shieldFill.enabled = _currentShield > 0;
        }

        internal void SetActionAvailability(bool canMove, bool canAttack)
        {
            moveAvailability.color = canMove ? availableColor : unavailableColor;
            attackAvailability.color = canAttack ? availableColor : unavailableColor;
        }

        internal void ResetHover()
        {
            ResetStatusHover();
            ResetAbilityHover();
        }

        protected override void OnUIHidden()
        {
            ResetHover();
            // 등록 전 Hide는 허용하며, 숨긴 화면의 이전 피해 연출은 다시 재생하지 않습니다.
            if (!_prepared) return;
            _healthAnimation.Complete();
            ApplyHealthFill();
            _hasHealth = false;
        }

        private void LateUpdate()
        {
            // 해상도가 바뀔 때만 다시 계산합니다. 포인터 입력에서는 슬롯 생성·레이아웃 갱신을 하지 않습니다.
            if (_prepared && _statusWidth != _statusLayoutRect.rect.width) ResizeStatusLayout();
            if (_prepared && _hoveredStatus >= 0) PositionStatusTooltip();
            // 공용 Hover 패널은 필드 설명 호버와 같이 씁니다. 같은 프레임에 그쪽이 먼저 열었던 패널을 닫으면
            // 특성 칸 위에 있는 동안 한 번 다시 엽니다. 띄운 동안에만 숨김 번호 하나를 읽습니다.
            if (_abilityHoverShown && _hoverText.HoverHideVersion != _abilityHoverVersion) ShowAbilityHover();
        }

        private void HoverAbilityEnter(int slot)
        {
            if (_hoverText == null || !isActiveAndEnabled || !_hasContent || slot < 0 || slot >= abilitySlots.Length) return;
            _hoveredAbility = slot;
            ShowAbilityHover();
        }

        private void ShowAbilityHover()
        {
            AbilityText ability = abilitySlots[_hoveredAbility];
            // 빈 칸·숨긴 칸은 이름·설명이 모두 null입니다. 표시 중인 특성은 설명이 비어도 이름은 띄웁니다.
            string abilityName = ability.Name;
            string description = ability.Description;
            if (string.IsNullOrEmpty(abilityName) && string.IsNullOrEmpty(description))
            {
                HideAbilityHover();
                return;
            }
            // 제목 = 특성 이름, 본문 = 설명. 정보를 그릴 때 만든 문자열을 그대로 넘기며 새 문자열을 만들지 않습니다.
            _hoverText.ShowHover(abilityName ?? string.Empty, description ?? string.Empty);
            _abilityHoverShown = true;
            _abilityHoverVersion = _hoverText.HoverHideVersion;
            _abilityHoverName = abilityName;
            _abilityHoverDescription = description;
            _hoverText.MoveHover(AbilityHoverPoint(ability.HoverArea));
        }

        /// <summary>정보 갱신 때 호버 중인 칸의 글이 바뀌었을 때만 다시 띄웁니다. 같은 글이면 패널을 건드리지 않습니다.</summary>
        private void RefreshAbilityHover()
        {
            if (_hoveredAbility < 0) return;
            AbilityText ability = abilitySlots[_hoveredAbility];
            if (_abilityHoverShown &&
                string.Equals(ability.Name, _abilityHoverName, StringComparison.Ordinal) &&
                string.Equals(ability.Description, _abilityHoverDescription, StringComparison.Ordinal)) return;
            ShowAbilityHover();
        }

        // 특성 칸 오른쪽 위 모서리의 화면 좌표입니다(상태 설명창과 같은 기준). 화면 밖 보정은 Hover 패널이 맡습니다.
        private Vector2 AbilityHoverPoint(RectTransform area)
        {
            Canvas root = _canvas.rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            Rect rect = area.rect;
            return RectTransformUtility.WorldToScreenPoint(camera, area.TransformPoint(new Vector3(rect.xMax, rect.yMax, 0f)));
        }

        private void HideAbilityHover()
        {
            if (!_abilityHoverShown) return;
            _abilityHoverShown = false;
            _abilityHoverName = null;
            _abilityHoverDescription = null;
            // 밖에서 이미 닫혔으면 지금 열린 패널은 다른 사용처의 것일 수 있어 닫지 않습니다(스테이지 맵 노드 호버와 같은 규칙).
            if (_hoverText.HoverHideVersion == _abilityHoverVersion) _hoverText.UnHover();
        }

        private void ResetAbilityHover()
        {
            _hoveredAbility = -1;
            HideAbilityHover();
            for (int i = 0; i < _abilityBindings.Length; i++)
                if (_abilityBindings[i] != null) _abilityBindings[i].OnHoverExit();
        }

        private void HoverStatusEnter(int slot)
        {
            if (!isActiveAndEnabled || !_hasContent || slot < 0 || slot >= _statusContent.Count ||
                !_statusIcons[slot].gameObject.activeInHierarchy) return;
            _hoveredStatus = slot;
            ShowStatusTooltip();
        }

        private void ShowStatusTooltip()
        {
            UnitHUDStatus content = _statusContent[_hoveredStatus];
            statusTitleText.text = content.Title;
            statusDescriptionText.text = content.Description;
            PositionStatusTooltip();
            statusTooltipPanel.SetAsLastSibling();
            statusTooltipPanel.gameObject.SetActive(true);
        }

        private void PositionStatusTooltip()
        {
            RectTransform icon = _statusIcons[_hoveredStatus].rectTransform;
            // 아이콘 오른쪽 위에 띄웁니다. 기존 Canvas의 좌표와 배율을 그대로 따릅니다.
            statusTooltipPanel.position = icon.TransformPoint(new Vector3(icon.rect.xMax, icon.rect.yMax, 0f)) +
                transform.TransformVector(new Vector3(statusTooltipOffset.x, statusTooltipOffset.y, 0f));
        }

        private void ResetStatusHover()
        {
            _hoveredStatus = -1;
            if (statusTooltipPanel != null) statusTooltipPanel.gameObject.SetActive(false);
            foreach (UIInputBinding binding in _statusBindings) binding.OnHoverExit();
        }

        /// <summary>
        /// 프리팹의 특성 아이콘만 갱신하고, 호버로 띄울 이름·설명은 기억만 합니다. 배치·오브젝트 활성 상태는 변경하지 않습니다.
        /// </summary>
        [Serializable]
        private sealed class AbilityText
        {
            [SerializeField] private Image icon;
            [NonSerialized] private Button _hoverButton;
            [NonSerialized] private RectTransform _hoverArea;
            [NonSerialized] private string _name;
            [NonSerialized] private string _description;

            /// <summary>이름·설명 호버를 받는 특성 칸의 Button입니다. 아이콘을 감싼 칸에 이미 있는 Button을 씁니다.</summary>
            internal Button HoverButton => _hoverButton;
            internal RectTransform HoverArea => _hoverArea;
            /// <summary>표시 중인 특성의 이름입니다. 빈 칸·숨긴 칸은 null입니다.</summary>
            internal string Name => _name;
            /// <summary>표시 중인 특성의 설명입니다. 빈 칸·숨긴 칸은 null입니다.</summary>
            internal string Description => _description;

            internal void Validate(CombatUnitInfoView owner, int index)
            {
                if (icon == null || !owner.IsChild(icon.transform))
                    throw owner.ConfigurationError($"abilitySlots[{index}].icon이 UnitInfo 내부에 연결돼야 합니다.");
                // 등록 때만 찾습니다. 비활성 칸(Ability2)도 찾도록 비활성 부모를 포함합니다.
                _hoverButton = icon.GetComponentInParent<Button>(true);
                _hoverArea = _hoverButton != null ? _hoverButton.transform as RectTransform : null;
                if (_hoverArea == null || !owner.IsChild(_hoverArea))
                    throw owner.ConfigurationError($"abilitySlots[{index}].icon 자신이나 부모에 설명 호버용 Button이 UnitInfo 내부에 필요합니다.");
            }

            internal bool SharesReferencesWith(AbilityText other) =>
                icon == other.icon || _hoverButton == other._hoverButton;

            internal void Render(in UnitCardTraitContent content, bool visible)
            {
                icon.sprite = visible ? content.Icon : null;
                icon.enabled = visible && content.Icon != null;
                _name = visible ? content.Name : null;
                _description = visible ? content.Description : null;
            }
        }

        /// <summary>특성 칸의 호버 입력을 받습니다.</summary>
        private sealed class AbilityInputReceiver : IUIInputReceiver
        {
            private readonly CombatUnitInfoView _owner;

            internal AbilityInputReceiver(CombatUnitInfoView owner) => _owner = owner;
            public void Click(int slot) { }
            public void HoverEnter(int slot) => _owner.HoverAbilityEnter(slot);
            public void HoverExit(int slot)
            {
                if (_owner._hoveredAbility != slot) return;
                _owner._hoveredAbility = -1;
                _owner.HideAbilityHover();
            }
        }

        /// <summary>상태 아이콘의 호버 입력을 받습니다.</summary>
        private sealed class StatusInputReceiver : IUIInputReceiver
        {
            private readonly CombatUnitInfoView _owner;

            internal StatusInputReceiver(CombatUnitInfoView owner) => _owner = owner;
            public void Click(int slot) { }
            public void HoverEnter(int slot) => _owner.HoverStatusEnter(slot);
            public void HoverExit(int slot)
            {
                if (_owner._hoveredStatus == slot) _owner.ResetStatusHover();
            }
        }

        // 상태 아이콘 영역만 늘립니다. UnitInfo와 특성 설명창의 배치는 프리팹이 정합니다.
        private void ResizeStatusLayout()
        {
            _statusWidth = _statusLayoutRect.rect.width;
            int statusColumns = Mathf.Max(1, Mathf.FloorToInt((_statusWidth - statusLayout.padding.horizontal +
                statusLayout.spacing.x + 0.001f) / (statusLayout.cellSize.x + statusLayout.spacing.x)));
            int statusRows = Mathf.Max(1, Mathf.CeilToInt((float)_statusCount / statusColumns));
            float statusHeight = statusLayout.padding.vertical + statusRows * statusLayout.cellSize.y +
                (statusRows - 1) * statusLayout.spacing.y;
            _statusLayoutRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, statusHeight);
        }

        private bool IsChild(Transform target) => target != transform && target.IsChildOf(transform);

        private InvalidOperationException ConfigurationError(string detail) =>
            new($"[CombatUnitInfoView] UIId.{UIId.CombatUnitInfo}, '{name}': {detail}");
    }
}
