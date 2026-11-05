using ProjectL.Systems.ServiceKeys.Runtime;
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

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems", sourceAssembly: "ProjectL.Systems", sourceClassName: "MainCharactorUI")]
    public sealed class MainCharactorUI : GlobalUIBase
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
        [Tooltip("HP 아이콘과 체력 수치를 포함하는 자식 영역입니다. 얼굴·골드는 포함하지 않습니다.")]
        [SerializeField] private GameObject healthBarRoot;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text goldText;
        [Tooltip("아이콘과 금액을 포함하는 Gold 자식 영역입니다.")]
        [SerializeField] private GameObject goldRoot;
        [SerializeField] private CategoryVisual[] categoryVisuals = Array.Empty<CategoryVisual>();

        private ICharacterBufferReadSink _characters;
        private OwnedCharacterStatsReader _ownedStats;
        private IUIRuntimeFaultHandler _faults;
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
        private Sprite _defaultPortrait;

        public override UIId ID => UIId.MainCharactor;

        /// <summary>설명 강조에 사용하는 기존 HP 아이콘·수치 영역입니다. 얼굴·골드는 포함하지 않습니다.</summary>
        internal Transform HealthBarTransform => RequireHealthBarRoot().transform;

        // 프리팹의 초상화를 기본값으로 보관하고 카테고리별 이미지가 없을 때 사용한다.
        private void Awake() => _defaultPortrait = portrait != null ? portrait.sprite : null;

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _faults = service as IUIRuntimeFaultHandler;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffer = resolver.GetService(BufferSystemKey.Buffer);
            _characters = buffer.CharacterReadSink;
            _currency = buffer.CurrencyReadSink;
            _ownedStats = new OwnedCharacterStatsReader(_characters, buffer.CombatStateReadSink,
                buffer.CharacterStatsReadSink, buffer.RunDeckReadSink, _faults, buffer.CharacterStatsRequestSink);
            _hasHealth = false;
            RefreshGold();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            // InitData와 PostBindService의 순서는 고정되지 않는다. 표시 조회는 Open/갱신에서 한다.
        }

        public override void Hide()
        {
            _ownedStats?.Clear();
            gameObject.SetActive(false);
        }

        /// <summary>메인 UI를 유지하면서 HP 영역만 전환합니다.</summary>
        public void SetHealthBarVisible(bool visible) => RequireHealthBarRoot().SetActive(visible);

        private GameObject RequireHealthBarRoot()
        {
            if (healthBarRoot == null || healthBarRoot == gameObject || !healthBarRoot.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[MainCharactorUI] '{name}'의 {nameof(healthBarRoot)}는 얼굴·골드와 구분된 자식 HP 영역이어야 합니다.");
            return healthBarRoot;
        }

        /// <summary>기존 Gold 위젯을 씬별 재화 표시 정책에 따라 전환합니다.</summary>
        public void SetGoldVisible(bool visible)
        {
            if (goldRoot == null || goldRoot == gameObject || !goldRoot.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[MainCharactorUI] '{name}' ({ID})의 goldRoot는 Gold 자식 영역이어야 합니다.");
            goldRoot.SetActive(visible);
        }

        public override void Open()
        {
            _ownedStats?.Clear();
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

        private void OnDisable()
        {
            _ownedStats?.Clear();
            _hasHealth = false;
        }

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
            _ownedStats?.Refresh();
            if (_ownedStats?.IsFaulted == true) return;
            var ownedMain = _ownedStats?.Main;
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
                else if (ownedMain != null)
                {
                    category = ownedMain.Stats.CategoryKey;
                    currentHealth = ownedMain.Stats.CurrentHealth;
                    maxHealth = ownedMain.Stats.MaxHealth;
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
                SetImage(portrait, portraitSprite != null ? portraitSprite : _defaultPortrait);
                SetImage(synergy, synergySprite);
                _lastCategory = category;
                _hasCategory = true;
            }

            bool hasMaxHealth = main != null || ownedMain != null;
            if (_hasHealth && _lastHealth == currentHealth && _lastMaxHealth == maxHealth &&
                _lastHasMaxHealth == hasMaxHealth) return;
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
