using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>
    /// 유닛 카드 한 장의 표시 전용 컴포넌트입니다(손패 카드화 설계 C1). 받은 값만 그리고, Buffer·DB를 읽지 않습니다.
    /// 이름 결정 순서(카드 Visuals SO → 호출자가 준 이름 → 키)와 능력치 문구 형식은 이 클래스 한 곳에만 둡니다.
    /// </summary>
    /// <remarks>
    /// 덱 정보 카드는 <see cref="UnitCardUI"/>가 조회한 값을, 전투 손패는 HandPresenter가 넘긴 값을 이 컴포넌트에 전달합니다.
    /// UIBase가 아니므로 UISystem에 등록되지 않고 여러 장을 동시에 둘 수 있습니다. 같은 값이면 텍스트·이미지를 다시 쓰지 않습니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class UnitCardView : MonoBehaviour, IUIInputReceiver
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private UnitCardVisualsSO visuals;
        [SerializeField] private Button synergyButton;
        [SerializeField] private Button functionButton;
        [Tooltip("UnitCardView 아래의 별도 자식 패널을 연결합니다.")]
        [SerializeField] private GameObject synergyPanel;
        [SerializeField] private Image synergyIcon;
        [SerializeField] private TMP_Text synergyDescription;
        [SerializeField] private GameObject functionPanel;
        [SerializeField] private Image functionIcon;
        [SerializeField] private TMP_Text functionName;
        [SerializeField] private TMP_Text functionDescription;

        private enum Panel
        {
            None,
            Synergy,
            Function
        }

        private Panel _visible;
        private bool _pinned;
        private bool _hasContent;
        private bool _panelsEnabled = true;

        /// <summary>패널을 열기 직전에 호출됩니다. 조회하는 쪽이 최신 값으로 다시 그릴 기회입니다.</summary>
        public event Action PanelOpening;

        /// <summary>표시 중인 캐릭터가 있는지입니다. 없으면 패널을 열지 않습니다.</summary>
        public bool HasContent => _hasContent;

        /// <summary>캐릭터 키로 이름·시너지를 고를 때 쓰는 카드 Visuals SO입니다. 없으면 null입니다.</summary>
        public UnitCardVisualsSO Visuals => visuals;

        private void Awake()
        {
            UIInputBinding.Attach(synergyButton, this, (int)Panel.Synergy);
            UIInputBinding.Attach(functionButton, this, (int)Panel.Function);
            ClosePanels();
        }

        private void OnDisable() => ClosePanels();

        void IUIInputReceiver.Click(int slot) => TogglePanel((Panel)slot);

        void IUIInputReceiver.HoverEnter(int slot) => HoverEnter((Panel)slot);

        void IUIInputReceiver.HoverExit(int slot) => HoverExit((Panel)slot);

        /// <summary>
        /// 캐릭터의 이름·초상화·레벨을 표시합니다. 이름은 카드 Visuals SO, <paramref name="fallbackName"/>, 키 순서로 고릅니다.
        /// 레벨이 없으면 '-'입니다.
        /// </summary>
        public void ShowCharacter(EntityKey key, string fallbackName, Sprite portraitSprite, int? level)
        {
            _hasContent = true;
            string displayName = visuals != null ? visuals.GetVisual(key)?.DisplayName : null;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = fallbackName;

            SetText(levelText, FormatLevel(level));
            SetText(nameText, string.IsNullOrWhiteSpace(displayName) ? key.ToString() : displayName);
            SetImage(portrait, portraitSprite);
        }

        /// <summary>전투 개체의 현재 능력치를 표시합니다. HP는 현재/최대, ATK는 보너스를 포함한 값입니다.</summary>
        public void ShowDetails(CharacterData character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            var stats = character.Stats;
            ShowDetails(character.Health.CurrentHealth, character.Health.MaxHealth, stats.PhysicalDefense,
                stats.EffectiveAttackPower, stats.AttackCost, stats.MaxActionCost, stats.Priority);
        }

        public void ShowDetails(int health, int maxHealth, int defense, int attack, int attackCost, int maxCost, int priority)
        {
            SetText(detailsText, $"HP : {health} / {maxHealth}\nDEF : {defense}\nATK : {attack}\n" +
                $"ATK COST : {attackCost}\nMAX COST : {maxCost}\nPRIORITY : {priority}");
        }

        public void ClearDetails()
        {
            SetText(detailsText, string.Empty);
        }

        /// <summary>시너지(카드 Visuals SO)와 기능(호출자가 고른 특성 내용) 패널 내용을 채웁니다.</summary>
        public void ShowPanelContent(EntityKey key, Sprite functionIconSprite, string functionNames, string functionDescriptions)
        {
            UnitCardVisualsSO.CharacterVisual visual = visuals != null ? visuals.GetVisual(key) : null;
            SetImage(synergyIcon, visual?.SynergyIcon);
            SetText(synergyDescription, visual?.SynergyDescription ?? string.Empty);
            SetImage(functionIcon, functionIconSprite);
            SetText(functionName, functionNames ?? string.Empty);
            SetText(functionDescription, functionDescriptions ?? string.Empty);
        }

        /// <summary>레벨만 바꿉니다. 표시 중인 캐릭터가 없으면 '-'입니다.</summary>
        public void SetLevel(int? level)
        {
            SetText(levelText, _hasContent ? FormatLevel(level) : "-");
        }

        /// <summary>표시 내용을 비우고 패널을 닫습니다.</summary>
        public void Clear()
        {
            _hasContent = false;
            SetText(levelText, "-");
            SetText(nameText, string.Empty);
            SetText(detailsText, string.Empty);
            SetImage(portrait, null);
            ShowPanelContent(EntityKey.None, null, string.Empty, string.Empty);
            ClosePanels();
        }

        /// <summary>Synergy/Function 패널 버튼을 쓸지 정합니다. 끄면 버튼을 숨기고 열린 패널을 닫습니다(손패 카드, 설계 U5).</summary>
        public void SetPanelsEnabled(bool enabled)
        {
            _panelsEnabled = enabled;
            SetButtonVisible(synergyButton, enabled);
            SetButtonVisible(functionButton, enabled);
            if (!enabled)
                ClosePanels();
        }

        public void HoverSynergyEnter() => HoverEnter(Panel.Synergy);

        public void HoverSynergyExit() => HoverExit(Panel.Synergy);

        public void HoverFunctionEnter() => HoverEnter(Panel.Function);

        public void HoverFunctionExit() => HoverExit(Panel.Function);

        public void ToggleSynergyPanel() => TogglePanel(Panel.Synergy);

        public void ToggleFunctionPanel() => TogglePanel(Panel.Function);

        public void ClosePanels()
        {
            _visible = Panel.None;
            _pinned = false;
            if (IsChildPanel(synergyPanel))
                synergyPanel.SetActive(false);
            if (IsChildPanel(functionPanel))
                functionPanel.SetActive(false);
        }

        private void HoverEnter(Panel panel)
        {
            // 고정 패널 자신의 Hover는 고정을 유지한다. 다른 Index는 패널을 전환한다.
            if (_visible == panel && _pinned)
                return;

            ShowPanel(panel, false);
        }

        private void HoverExit(Panel panel)
        {
            if (_visible == panel && !_pinned)
                ClosePanels();
        }

        private void TogglePanel(Panel panel)
        {
            if (_visible == panel && _pinned)
                ClosePanels();
            else
                ShowPanel(panel, true);
        }

        private void ShowPanel(Panel panel, bool pinned)
        {
            if (!_panelsEnabled || !isActiveAndEnabled)
                return;

            PanelOpening?.Invoke();
            if (!_hasContent)
                return;

            GameObject target = panel == Panel.Synergy ? synergyPanel : functionPanel;
            if (!IsChildPanel(target))
                return;

            ClosePanels();
            _visible = panel;
            _pinned = pinned;
            target.SetActive(true);
        }

        private bool IsChildPanel(GameObject panel) =>
            panel != null && panel != gameObject && panel.transform.IsChildOf(transform);

        private static string FormatLevel(int? level) => level?.ToString() ?? "-";

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null && button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && target.text != value)
                target.text = value;
        }

        private static void SetImage(Image target, Sprite sprite)
        {
            if (target == null)
                return;

            if (target.sprite != sprite)
                target.sprite = sprite;
            bool visible = sprite != null;
            if (target.enabled != visible)
                target.enabled = visible;
        }
    }
}
