using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIUnitInfoAbilityHoverEnter = new("Combat.Action.UI.UnitInfo.AbilityHoverEnter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIUnitInfoAbilityHoverExit = new("Combat.Action.UI.UnitInfo.AbilityHoverExit");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIUnitInfoStatusHoverEnter = new("Combat.Action.UI.UnitInfo.StatusHoverEnter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIUnitInfoStatusHoverExit = new("Combat.Action.UI.UnitInfo.StatusHoverExit");
#endif

        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text structureInfoText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private GameObject levelRoot;
        [SerializeField] private GameObject categoryRoot;
        [SerializeField] private Image categoryIcon;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private ScrollRect abilityScroll;
        [SerializeField] private AbilitySection unitAbilities;
        [SerializeField] private AbilitySection categoryAbilities;
        [SerializeField] private AbilitySection growthAbilities;
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

        private readonly List<UnitAbilitySlot> _abilitySlots = new();
        private readonly List<UIInputBinding> _abilityBindings = new();
        private AbilityInputReceiver _abilityReceiver;
        private IUITextPresentation _hoverText;
        private Canvas _canvas;
        private int _hoveredAbility = -1;
        private bool _abilityHoverShown;
        private uint _abilityHoverVersion;
        private string _abilityHoverName;
        private string _abilityHoverDescription;
        private Vector2 _abilityHoverPoint;

        private bool _prepared;
        private bool _hasContent;
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
            if (abilityScroll == null || !IsChild(abilityScroll.transform) || abilityScroll.content == null ||
                !abilityScroll.content.IsChildOf(abilityScroll.transform) || abilityScroll.content == abilityScroll.transform ||
                unitAbilities == null || categoryAbilities == null || growthAbilities == null)
                throw ConfigurationError("UnitInfo 내부의 abilityScroll·content와 유닛·병종·추가 특성 영역 참조가 필요합니다.");
            if (!abilityScroll.enabled || !abilityScroll.horizontal || abilityScroll.vertical || abilityScroll.viewport == null ||
                !abilityScroll.content.IsChildOf(abilityScroll.viewport) ||
                !abilityScroll.viewport.TryGetComponent(out RectMask2D mask) || !mask.enabled ||
                !abilityScroll.content.TryGetComponent(out HorizontalLayoutGroup layout) || !layout.enabled ||
                !abilityScroll.content.TryGetComponent(out ContentSizeFitter fitter) || !fitter.enabled ||
                fitter.horizontalFit != ContentSizeFitter.FitMode.PreferredSize)
                throw ConfigurationError("abilityScroll에는 가로 스크롤·뷰포트 마스크·가로 자동 배치가 필요합니다.");
            unitAbilities.Validate(this, nameof(unitAbilities), requireLevel: true);
            categoryAbilities.Validate(this, nameof(categoryAbilities), allowIndependentPlacement: true);
            growthAbilities.Validate(this, nameof(growthAbilities), requireLevel: true);
            if (unitAbilities.Root == categoryAbilities.Root || unitAbilities.Root == growthAbilities.Root ||
                categoryAbilities.Root == growthAbilities.Root)
                throw ConfigurationError("유닛·병종·추가 특성은 서로 다른 영역에 연결해야 합니다.");
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
            _abilityReceiver = new AbilityInputReceiver(this);
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

        private void AddAbilitySlot(UnitAbilitySlot slot)
        {
            _abilityBindings.Add(UIInputBinding.Attach(slot.Button, _abilityReceiver, _abilitySlots.Count, hasHover: true));
            _abilitySlots.Add(slot);
        }

        private void RenderAbilities(UnitCardTraitContent[] contents)
        {
            unitAbilities.Render(this, contents, TraitOrigin.Unit);
            categoryAbilities.Render(this, contents, TraitOrigin.Category);
            growthAbilities.Render(this, contents, TraitOrigin.Growth);
            RefreshAbilityHover();
        }

        internal void Show(string unitName, int? level, Sprite category, string categoryName, UnitCardTraitContent[] abilities,
            bool showCharacterDetails = true)
        {
            if (abilities == null) throw new ArgumentNullException(nameof(abilities));
            Prepare();
            SetCharacterDetailsVisible(showCharacterDetails);
            _hasContent = true;
            if (nameText != null) nameText.text = unitName;
            levelText.text = level?.ToString() ?? "-";
            categoryIcon.sprite = category;
            categoryIcon.enabled = category != null;
            categoryText.text = categoryName;
            RenderAbilities(showCharacterDetails ? abilities : Array.Empty<UnitCardTraitContent>());
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
            RenderAbilities(Array.Empty<UnitCardTraitContent>());
            abilityScroll.StopMovement();
            abilityScroll.content.anchoredPosition = Vector2.zero;
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
            if (_abilityHoverShown)
            {
                if (_hoverText.HoverHideVersion != _abilityHoverVersion) ShowAbilityHover();
                else MoveAbilityHover();
            }
        }

        private void HoverAbilityEnter(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIUnitInfoAbilityHoverEnterSample = _profileUIUnitInfoAbilityHoverEnter.Auto();
#endif

            if (_hoverText == null || !isActiveAndEnabled || !_hasContent || slot < 0 || slot >= _abilitySlots.Count ||
                !_abilitySlots[slot].Button.gameObject.activeInHierarchy) return;
            _hoveredAbility = slot;
            ShowAbilityHover();
        }

        private void ShowAbilityHover()
        {
            UnitCardTraitContent ability = _abilitySlots[_hoveredAbility].Content;
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
            _abilityHoverPoint = AbilityHoverPoint((RectTransform)_abilitySlots[_hoveredAbility].Button.transform);
            _hoverText.MoveHover(_abilityHoverPoint);
        }

        /// <summary>정보 갱신 때 호버 중인 칸의 글이 바뀌었을 때만 다시 띄웁니다. 같은 글이면 패널을 건드리지 않습니다.</summary>
        private void RefreshAbilityHover()
        {
            if (_hoveredAbility < 0) return;
            UnitAbilitySlot slot = _abilitySlots[_hoveredAbility];
            if (!slot.Button.gameObject.activeInHierarchy)
            {
                ResetAbilityHover();
                return;
            }
            UnitCardTraitContent ability = slot.Content;
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

        private void MoveAbilityHover()
        {
            Vector2 point = AbilityHoverPoint((RectTransform)_abilitySlots[_hoveredAbility].Button.transform);
            if (point == _abilityHoverPoint) return;
            _abilityHoverPoint = point;
            _hoverText.MoveHover(point);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIUnitInfoAbilityHoverExitSample = _profileUIUnitInfoAbilityHoverExit.Auto();
#endif

            _hoveredAbility = -1;
            HideAbilityHover();
            foreach (UIInputBinding binding in _abilityBindings) binding.OnHoverExit();
        }

        private void HoverStatusEnter(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIUnitInfoStatusHoverEnterSample = _profileUIUnitInfoStatusHoverEnter.Auto();
#endif

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIUnitInfoStatusHoverExitSample = _profileUIUnitInfoStatusHoverExit.Auto();
#endif

            _hoveredStatus = -1;
            if (statusTooltipPanel != null) statusTooltipPanel.gameObject.SetActive(false);
            foreach (UIInputBinding binding in _statusBindings) binding.OnHoverExit();
        }

        /// <summary>한 출처의 아이콘과 입력을 재사용합니다. 입력 번호는 각 슬롯이 생성될 때 고정합니다.</summary>
        [Serializable]
        private sealed class AbilitySection
        {
            [SerializeField] private RectTransform root;
            [SerializeField] private UnitAbilitySlot template;
            private readonly List<UnitAbilitySlot> _slots = new();

            internal RectTransform Root => root;

            internal void Validate(CombatUnitInfoView owner, string field, bool allowIndependentPlacement = false,
                bool requireLevel = false)
            {
                if (root == null || !owner.IsChild(root))
                    throw owner.ConfigurationError($"{field}.root는 UnitInfo 내부에 연결해야 합니다.");
                if (template == null)
                    throw owner.ConfigurationError($"{field}.template 참조가 필요합니다.");

                Transform scroll = owner.abilityScroll.transform;
                bool inScrollList = root.parent == owner.abilityScroll.content;
                // 병종 고유 특성은 Core에서 최대 한 개를 제공하므로 스크롤과 분리해서 배치할 수 있습니다.
                bool independent = allowIndependentPlacement && !root.IsChildOf(scroll) && !scroll.IsChildOf(root);
                if (!inScrollList && !independent)
                    throw owner.ConfigurationError(allowIndependentPlacement
                        ? $"{field}.root는 스크롤 콘텐츠의 직계 자식이거나 스크롤과 분리된 UnitInfo 내부 영역이어야 합니다."
                        : $"{field}.root는 스크롤 콘텐츠의 직계 자식이어야 합니다.");

                template.Validate(root, field + ".template", requireLevel: requireLevel);
                Transform button = template.Button.transform;
                if (!(button is RectTransform))
                    throw owner.ConfigurationError($"{field}.template 버튼에는 RectTransform이 필요합니다.");
                if (inScrollList && (button.parent.parent != root ||
                    !button.parent.TryGetComponent(out HorizontalLayoutGroup layout) || !layout.enabled))
                    throw owner.ConfigurationError($"{field}.template 버튼은 영역 내부의 가로 목록에 연결해야 합니다.");
            }

            internal void Render(CombatUnitInfoView owner, UnitCardTraitContent[] contents, TraitOrigin origin)
            {
                if (_slots.Count == 0)
                {
                    owner.AddAbilitySlot(template);
                    _slots.Add(template);
                }
                int count = 0;
                foreach (UnitCardTraitContent content in contents)
                {
                    if (content.Origin != origin) continue;
                    if (count == _slots.Count)
                    {
                        UnitAbilitySlot slot = template.Clone(template.Button.transform.parent);
                        owner.AddAbilitySlot(slot);
                        _slots.Add(slot);
                    }
                    _slots[count++].Render(content, true);
                }
                for (int i = count; i < _slots.Count; i++) _slots[i].Render(default, false);
                root.gameObject.SetActive(count > 0);
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
