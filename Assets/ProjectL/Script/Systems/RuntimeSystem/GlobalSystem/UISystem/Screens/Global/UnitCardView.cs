using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>카드 본체의 외형과 상태만 표시합니다. 상세 선택·개폐는 공용 패널이 소유합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class UnitCardView : UIWidgetBase
    {
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text attackText;
        [SerializeField] private TMP_Text defenseText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private UnitCardVisualsSO visuals;
        [SerializeField] private Button infoButton;
        [Tooltip("사망 표시가 필요한 카드에만 연결하는 내부 위젯입니다.")]
        [SerializeField] private GameObject deadPanel;
        [Tooltip("배치 완료 표시가 필요한 카드에만 연결하는 내부 위젯입니다.")]
        [SerializeField] private GameObject checkPanel;
        private UIInputBinding _infoBinding;
        private bool _hasContent;
        private EntityKey _shownKey;

        public bool HasContent => _hasContent;
        public UnitCardVisualsSO Visuals => visuals;

        private void Awake()
        {
            SetDead(false);
            SetDeployed(false);
        }

        internal void BindInfoInput(IUIInputReceiver receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            if (infoButton == null || infoButton.transform == transform || !infoButton.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[UnitCardView] UIId.{OwnerUI?.ID ?? UIId.None}, '{name}': 카드 자식 infoButton이 필요합니다.");
            _infoBinding = UIInputBinding.Attach(infoButton, receiver, 0);
        }

        /// <summary>상세 입력을 제공하지 않는 손패·로비 위젯에서는 정보 버튼을 숨깁니다.</summary>
        public void SetInfoInputEnabled(bool enabled)
        {
            if (!enabled) _infoBinding?.OnHoverExit();
            if (infoButton != null) infoButton.gameObject.SetActive(enabled);
        }

        internal string ResolveDisplayName(EntityKey key, string fallbackName)
        {
            string displayName = visuals != null ? visuals.GetVisual(key)?.DisplayName : null;
            if (string.IsNullOrWhiteSpace(displayName)) displayName = fallbackName;
            return string.IsNullOrWhiteSpace(displayName) ? key.ToString() : displayName;
        }

        public void ShowCharacter(EntityKey key, string fallbackName, Sprite portraitSprite, int? level)
        {
            if (!_hasContent || _shownKey != key)
            {
                SetDead(false);
                SetDeployed(false);
            }
            _shownKey = key;
            _hasContent = true;
            SetText(nameText, ResolveDisplayName(key, fallbackName));
            SetImage(portrait, portraitSprite);
        }

        public void ShowDetails(CharacterData character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            ShowDetails(character.Health.CurrentHealth, character.Health.MaxHealth, character.EffectivePhysicalDefense,
                character.EffectiveAttackPower);
            SetDead(character.Health.IsDead);
        }

        public void ShowDetails(int health, int maxHealth, int defense, int attack)
        {
            SetText(attackText, attack.ToString());
            SetText(defenseText, defense.ToString());
            SetText(healthText, health.ToString());
        }

        public void ShowDeckDetails(int? level, int health, int maxHealth, int defense, int attack) =>
            ShowDetails(health, maxHealth, defense, attack);

        public void ClearDetails()
        {
            SetDead(false);
            SetDeployed(false);
            SetText(attackText, string.Empty);
            SetText(defenseText, string.Empty);
            SetText(healthText, string.Empty);
        }

        public void Clear()
        {
            _hasContent = false;
            _infoBinding?.OnHoverExit();
            SetText(nameText, string.Empty);
            ClearDetails();
            SetImage(portrait, null);
        }

        public void SetDead(bool dead) => SetStatePanel(deadPanel, dead, nameof(deadPanel));
        public void SetDeployed(bool deployed) => SetStatePanel(checkPanel, deployed, nameof(checkPanel));

        private void SetStatePanel(GameObject panel, bool visible, string field)
        {
            // 표시를 제공하지 않는 구성에서는 생략할 수 있습니다.
            if (panel == null) return;
            if (panel == gameObject || !panel.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[UnitCardView] '{name}': {field}은 카드의 별도 자식이어야 합니다.");
            if (panel.activeSelf != visible) panel.SetActive(visible);
        }

        protected override void OnUIHidden()
        {
            _infoBinding?.OnHoverExit();
            SetDead(false);
            SetDeployed(false);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && target.text != value) target.text = value;
        }

        private static void SetImage(Image target, Sprite sprite)
        {
            if (target == null) return;
            if (target.sprite != sprite) target.sprite = sprite;
            bool visible = sprite != null;
            if (target.enabled != visible) target.enabled = visible;
        }
    }
}
