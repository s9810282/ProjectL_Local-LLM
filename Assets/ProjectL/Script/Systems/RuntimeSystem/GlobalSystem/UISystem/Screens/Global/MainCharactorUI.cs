using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems
{
    public class MainCharactorUI : GlobalUIBase
    {
        [Serializable]
        private struct CategoryVisual
        {
            public CategoryKey Category;
            public Sprite Portrait;
            public Sprite Synergy;
        }

        [SerializeField] private Image portrait;
        [SerializeField] private Image synergy;
        [SerializeField] private Slider hp;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private CategoryVisual[] categoryVisuals = Array.Empty<CategoryVisual>();

        private ICharacterBufferReadSink _characters;
        private ICurrencyBufferReadSink _currency;
        // int 최대값 두 개와 " / "를 담는다. TMP는 전달된 문자를 자체 버퍼로 복사한다.
        private readonly char[] _numberText = new char[23];
        private bool _hasGold;
        private int _lastGold;
        private bool _hasHealth;
        private int _lastHealth;
        private int _lastMaxHealth;
        private bool _lastHasMaxHealth;
        private bool _hasCategory;
        private CategoryKey _lastCategory;

        public override UIId ID => UIId.MainCharactor;

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffer = resolver.GetService(BufferSystemKey.Buffer);
            _characters = buffer.CharacterReadSink;
            _currency = buffer.CurrencyReadSink;
            Refresh();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            Refresh();
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
        }

        public override void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            RefreshCharacter();
            RefreshGold();
        }

        // 유지 UI는 씬이 바뀌어도 같은 Buffer를 읽으며 표시값 비교로 불필요한 갱신을 피한다.
        private void LateUpdate() => Refresh();

        private void RefreshGold()
        {
            int amount = _currency?.Amount ?? 0;
            if (_hasGold && _lastGold == amount) return;
            if (goldText != null)
                goldText.SetCharArray(_numberText, 0, WriteNumber(amount, 0));
            _lastGold = amount;
            _hasGold = true;
        }

        private void RefreshCharacter()
        {
            CategoryKey category = CategoryKey.None;
            int currentHealth = 0;
            int maxHealth = 0;
            CharacterData main = null;
            if (_characters != null)
            {
                var characters = _characters.Characters;
                for (int i = 0; i < characters.Count; i++)
                {
                    CharacterData character = characters[i];
                    if (character.OwnershipRole != CharacterOwnershipRole.Main) continue;
                    main = character;
                    break;
                }

                if (main != null)
                {
                    category = main.Stats.Category.Key;
                    currentHealth = main.Health.CurrentHealth;
                    maxHealth = main.Health.MaxHealth;
                }
                else if (_characters.TryGetMainState(out var state))
                {
                    currentHealth = state.CurrentHealth;
                    // 지속 상태에는 영혼 보정 후 최대 HP·Category가 없다. UI에서 계산하지 않는다.
                }
            }

            if (!_hasCategory || _lastCategory != category)
            {
                Sprite portraitSprite = null;
                Sprite synergySprite = null;
                foreach (var visual in categoryVisuals)
                {
                    if (category == CategoryKey.None || visual.Category != category) continue;
                    portraitSprite = visual.Portrait;
                    synergySprite = visual.Synergy;
                    break;
                }
                SetImage(portrait, portraitSprite);
                SetImage(synergy, synergySprite);
                _lastCategory = category;
                _hasCategory = true;
            }

            bool hasMaxHealth = main != null;
            if (_hasHealth && _lastHealth == currentHealth && _lastMaxHealth == maxHealth &&
                _lastHasMaxHealth == hasMaxHealth) return;
            if (hp != null)
            {
                hp.interactable = false;
                hp.wholeNumbers = true;
                hp.minValue = 0;
                hp.maxValue = hasMaxHealth ? Mathf.Max(1, maxHealth) : 1;
                hp.SetValueWithoutNotify(hasMaxHealth ? currentHealth : 0);
            }
            if (hpText != null)
            {
                int length = WriteNumber(currentHealth, 0);
                _numberText[length++] = ' ';
                _numberText[length++] = '/';
                _numberText[length++] = ' ';
                if (hasMaxHealth) length = WriteNumber(maxHealth, length);
                else _numberText[length++] = '-';
                hpText.SetCharArray(_numberText, 0, length);
            }
            _lastHealth = currentHealth;
            _lastMaxHealth = maxHealth;
            _lastHasMaxHealth = hasMaxHealth;
            _hasHealth = true;
        }

        // Buffer의 HP와 Gold는 음수가 아니다. float 변환 없이 int 전체 범위를 표시한다.
        private int WriteNumber(int value, int start)
        {
            int digits = 1;
            for (int remaining = value; remaining >= 10; remaining /= 10) digits++;
            int end = start + digits;
            for (int index = end - 1; index >= start; index--)
            {
                _numberText[index] = (char)('0' + value % 10);
                value /= 10;
            }
            return end;
        }

        private static void SetImage(Image target, Sprite sprite)
        {
            if (target == null) return;
            target.sprite = sprite;
            target.enabled = sprite != null;
        }
    }
}
