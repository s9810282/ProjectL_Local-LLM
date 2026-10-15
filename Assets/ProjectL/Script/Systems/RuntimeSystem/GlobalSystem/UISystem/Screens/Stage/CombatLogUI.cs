using System.Collections.Generic;
using System.Globalization;
using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.ServiceKeys;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems
{
    [DisallowMultipleComponent]
    public sealed class CombatLogUI : UIBase, IUIInputReceiver
    {
        private const float LatestDamageTextOffsetX = 0f;

        [Tooltip("Content/Log가 없을 때 사용할 외부 Log 프리팹입니다.")]
        [SerializeField] private RectTransform _logPrefab;
        [Tooltip("사망 표시 이미지입니다. 비워두면 Log/Dead의 기존 이미지를 사용합니다.")]
        [SerializeField] private Sprite _deathIcon;
        [SerializeField] private Sprite _attackIcon;

        [System.Serializable]
        private struct UnitIcon
        {
            public EntityKey Key;
            public Sprite Icon;
        }

        [SerializeField] private UnitIcon[] _unitIcons = System.Array.Empty<UnitIcon>();
        private readonly Dictionary<EntityId, EntityKey> _unitKeys = new Dictionary<EntityId, EntityKey>();
        [Tooltip("드롭다운을 열고 닫는 버튼입니다.")]
        [SerializeField] private Button toggleButton;
        [Tooltip("최신 로그 한 줄을 표시할 고정 영역입니다. Scroll View 바깥에 연결합니다.")]
        [SerializeField] private RectTransform latestLogRoot;
        [Tooltip("이전 로그만 표시하는 별도 자식 Scroll View입니다.")]
        [SerializeField] private ScrollRect scrollView;
        private ICombatResultHistoryReadSink _history;
        private ICombatStateBufferReadSink _combat;
        private ICharacterBufferReadSink _characters;
        private long _battleId = -1;
        private int _readCount;
        private bool _expanded;
        private RectTransform _scrollTransform;
        public override UIId ID => UIId.CombatLog;
        public bool IsExpanded => _expanded;
        private readonly List<GameObject> _entries = new List<GameObject>();
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private RectTransform _template;
        private RectTransform _latestRow;
        private float _rowHeight;
        private bool _initialized;
        private bool _scrollPending;

        public int LogCount => _entries.Count;

        private void Awake()
        {
            HideEmptyLogs();
            UIInputBinding.Attach(toggleButton, this, 0);
            Initialize();
            CloseDropDown();
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot == 0) ToggleDropDown();
        }
        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }
        // UI를 활성화할 때마다 이전 기록은 기본적으로 숨긴다.
        private void OnEnable()
        {
            HideEmptyLogs();
            CloseDropDown();
        }

        private void HideEmptyLogs()
        {
            if (_entries.Count == 0 && latestLogRoot != null && latestLogRoot != transform &&
                latestLogRoot.IsChildOf(transform))
                latestLogRoot.gameObject.SetActive(false);

            // 외부 프리팹 대신 씬 안의 Log를 템플릿으로 연결한 경우에도 원본은 표시하지 않는다.
            if (_logPrefab != null && _logPrefab != transform && _logPrefab.IsChildOf(transform))
                _logPrefab.gameObject.SetActive(false);
            var content = scrollView != null ? scrollView.content : null;
            if (content == null && scrollView != null)
                content = scrollView.transform.Find("Viewport/Content") as RectTransform;
            // 초기화 후에는 이름이 같은 과거 로그가 아닌 저장된 템플릿만 숨긴다.
            Transform template = _initialized ? _template : content != null ? content.Find("Log") : null;
            if (template != null && template.IsChildOf(transform)) template.gameObject.SetActive(false);
        }
        private void OnDisable() => CloseDropDown();

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _history = buffers.CombatResultHistoryReadSink;
            _combat = buffers.CombatStateReadSink;
            _characters = buffers.CharacterReadSink;
            _battleId = -1;
            RefreshLogs();
        }

        public override void GetUIData(IServiceModule query) { }
        public override void Open()
        {
            gameObject.SetActive(true);
            RefreshLogs();
        }

        public override void Hide()
        {
            CloseDropDown();
            gameObject.SetActive(false);
        }

        public void ToggleDropDown() => SetExpanded(!_expanded);
        public void OpenDropDown() => SetExpanded(true);
        public void CloseDropDown() => SetExpanded(false);

        private void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            // 로그 템플릿 설정과 무관하게 버튼으로 이전 기록 영역을 여닫을 수 있어야 한다.
            var view = _scrollRect != null ? _scrollRect : scrollView;
            if (view == null || view.gameObject == gameObject || !view.transform.IsChildOf(transform)) return;
            view.gameObject.SetActive(expanded);
            if (view.verticalScrollbar != null)
                view.verticalScrollbar.gameObject.SetActive(expanded);
            _scrollPending = true;
        }

        private void Update() => RefreshLogs();

        public void RefreshLogs()
        {
            if (_combat == null || _history == null || !Initialize()) return;
            if (_battleId != _combat.BattleInstanceId)
            {
                _battleId = _combat.BattleInstanceId;
                _unitKeys.Clear();
                ClearLogs();
                _readCount = 0;
                CloseDropDown();
            }
            var results = _history.Results;
            if (_readCount == results.Count) return;
            if (_characters != null)
                foreach (var character in _characters.Characters)
                    _unitKeys[character.EntityId] = character.CharacterKey;
            if (_combat.Current != null)
                foreach (var structure in _combat.Current.CommandState.Structures)
                    _unitKeys[structure.EntityId] = structure.Definition.Key;

            while (_readCount < results.Count)
            {
                CombatResult result = results[_readCount++];
                if (!result.Accepted) continue;
                // 사망/반환으로 현재 버퍼에서 빠진 개체도 사건의 불변 사실에서 식별한다.
                foreach (CombatEvent entry in result.Events)
                {
                    if (entry is CombatCharacterEvent character)
                        _unitKeys[character.Final.EntityId] = character.Final.CharacterKey;
                    else if (entry is CombatStructureEvent structure)
                        _unitKeys[structure.Final.EntityId] = structure.Final.Definition.Key;
                }
                bool hasAttackEvent = false;
                foreach (CombatEvent entry in result.Events)
                {
                    if (entry is not CombatAttackEvent attack) continue;
                    AppendAttack(attack.Attack);
                    hasAttackEvent = true;
                }
                if (!hasAttackEvent && result.Attack.HasValue) AppendAttack(result.Attack.Value);
            }
        }

        private void AppendAttack(AttackResult result)
        {
            if (!result.Succeeded) return;
            _unitKeys.TryGetValue(result.AttackerId, out EntityKey attackerKey);
            foreach (AttackHitResult hit in result.Hits.Span)
            {
                _unitKeys.TryGetValue(hit.TargetId, out EntityKey targetKey);
                AddLog(FindIcon(attackerKey), _attackIcon, FindIcon(targetKey), hit.DamageDealt,
                    hit.TargetKilled, attackerKey == EntityKey.None ? result.AttackerId.ToString() : attackerKey.ToString(),
                    targetKey == EntityKey.None ? hit.TargetId.ToString() : targetKey.ToString());
            }
        }

        private Sprite FindIcon(EntityKey key)
        {
            foreach (var entry in _unitIcons)
                if (entry.Key == key) return entry.Icon;
            return null;
        }

        /// <summary>Adds one attack result. Attacker and target are portrait sprites.</summary>
        public void AddLog(Sprite attacker, Sprite attackImage, Sprite target, int damage, bool isDead)
            => AddLog(attacker, attackImage, target, damage, isDead, null, null);

        private void AddLog(Sprite attacker, Sprite attackImage, Sprite target, int damage, bool isDead,
            string attackerName, string targetName)
        {
            if (!Initialize()) return;

            // 지금까지의 최신 행을 이전 기록으로 이동하고 새 행을 고정 영역에 표시한다.
            if (_latestRow != null)
            {
                FindLogComponent<TMP_Text>(_latestRow, "DMG/HEAL").rectTransform.anchoredPosition -=
                    Vector2.right * LatestDamageTextOffsetX;
                _latestRow.SetParent(_content, false);
                _latestRow.name = "Log";
                _latestRow.SetAsFirstSibling();
            }
            RectTransform row = Instantiate(_template, latestLogRoot, false);
            row.name = "LatestLog";
            row.anchoredPosition = new Vector2(0f, -50f);
            _latestRow = row;
            Image attackerImage = FindLogComponent<Image>(row, "Origin");
            Image actionImage = FindLogComponent<Image>(row, "Action");
            Image targetImage = FindLogComponent<Image>(row, "Target");
            TMP_Text damageText = FindLogComponent<TMP_Text>(row, "DMG/HEAL");
            damageText.rectTransform.anchoredPosition += Vector2.right * LatestDamageTextOffsetX;
            Image deadImage = FindLogComponent<Image>(row, "Dead");

            SetImage(attackerImage, attacker);
            SetImage(actionImage, attackImage);
            SetImage(targetImage, target);
            damageText.text = damage.ToString(CultureInfo.InvariantCulture);
            damageText.raycastTarget = false;
            if (_deathIcon != null) deadImage.sprite = _deathIcon;
            deadImage.gameObject.SetActive(true);
            deadImage.enabled = isDead;
            deadImage.preserveAspect = true;
            deadImage.raycastTarget = false;
            if (attacker == null) AddFallbackLabel(attackerImage, damageText, attackerName);
            if (target == null) AddFallbackLabel(targetImage, damageText, targetName);
            if (attackImage == null) AddFallbackLabel(actionImage, damageText, ">");
            if (isDead && deadImage.sprite == null)
            {
                deadImage.enabled = false;
                AddFallbackLabel(deadImage, damageText, "X");
            }
            // The prefab has no layout element; retain its designed row height.
            LayoutElement element = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = false;
            element.minHeight = _rowHeight;
            element.preferredHeight = _rowHeight;
            element.flexibleHeight = 0f;
            row.SetAsFirstSibling();
            row.gameObject.SetActive(true);
            latestLogRoot.gameObject.SetActive(true);
            _entries.Add(row.gameObject);
            SetExpanded(_expanded);
            _scrollPending = true;
        }

        public void ClearLogs()
        {
            foreach (GameObject entry in _entries)
            {
                if (entry == null) continue;
                // Remove from layout immediately, even though Destroy is deferred.
                entry.SetActive(false);
                if (Application.isPlaying) Destroy(entry);
                else DestroyImmediate(entry);
            }
            _entries.Clear();
            _latestRow = null;
            if (latestLogRoot != null && latestLogRoot != transform)
                latestLogRoot.gameObject.SetActive(false);
            // 공개 ClearLogs는 현재 이력까지 소비한 상태로 만든다. 새 전투에서는 호출자가 0으로 되돌린다.
            _readCount = _history?.Results.Count ?? 0;
            _scrollPending = true;
        }

        private static void AddFallbackLabel(Image image, TMP_Text style, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.layer = image.gameObject.layer;
            labelObject.transform.SetParent(image.transform, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = style.font;
            label.fontSize = style.fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = Mathf.Max(12f, style.fontSize);
            label.alignment = TextAlignmentOptions.Center;
            label.richText = false;
            label.raycastTarget = false;
            label.text = text;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
        }

        private bool Initialize()
        {
            if (_initialized) return true;
            _scrollRect = scrollView;
            if (_scrollRect == null || latestLogRoot == null || latestLogRoot == transform || _scrollRect.gameObject == gameObject ||
                !_scrollRect.transform.IsChildOf(transform) || !latestLogRoot.IsChildOf(transform) ||
                latestLogRoot == _scrollRect.transform || latestLogRoot.IsChildOf(_scrollRect.transform))
            {
                Debug.LogError("CombatLog requires a separate child Scroll View and a Latest Log Root outside it.", this);
                return false;
            }
            _scrollTransform = (RectTransform)_scrollRect.transform;
            _content = _scrollRect.content;
            if (_content == null) _content = _scrollTransform.Find("Viewport/Content") as RectTransform;
            if (_content == null)
            {
                Debug.LogError("CombatLog requires ScrollRect Content or a Viewport/Content child.", this);
                return false;
            }

            RectTransform embeddedTemplate = _content.Find("Log") as RectTransform;
            _template = embeddedTemplate != null ? embeddedTemplate : _logPrefab;
            if (_template == null || FindLogComponent<Image>(_template, "Origin") == null ||
                FindLogComponent<Image>(_template, "Action") == null || FindLogComponent<Image>(_template, "Target") == null ||
                FindLogComponent<TMP_Text>(_template, "DMG/HEAL") == null || FindLogComponent<Image>(_template, "Dead") == null)
            {
                Debug.LogError("CombatLog requires Log with Origin, Action, Target, Dead Images and DMG/HEAL TMP text.", this);
                return false;
            }

            _rowHeight = Mathf.Max(1f, _template.rect.height);
            if (_scrollRect.viewport == null) _scrollRect.viewport = _scrollTransform.Find("Viewport") as RectTransform;
            if (_scrollRect.viewport == null || _scrollRect.viewport == _scrollTransform)
            {
                Debug.LogError("CombatLog requires a separate Viewport RectTransform.", this);
                return false;
            }
            var viewport = _scrollRect.viewport;
            if (viewport.GetComponent<RectMask2D>() == null && viewport.GetComponent<Mask>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
            if (embeddedTemplate != null) embeddedTemplate.gameObject.SetActive(false);
            latestLogRoot.gameObject.SetActive(false);
            _scrollRect.content = _content;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = Vector2.one;
            _content.pivot = new Vector2(0.5f, 1f);
            _content.sizeDelta = new Vector2(0f, _content.sizeDelta.y);
            _content.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = _content.GetComponent<VerticalLayoutGroup>() ??
                _content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = _content.GetComponent<ContentSizeFitter>() ??
                _content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _initialized = true;
            SetExpanded(_expanded);
            _scrollPending = true;
            return true;
        }

        private static T FindLogComponent<T>(Transform root, string name) where T : Component
        {
            // DMG/HEAL은 경로가 아닌 실제 자식 이름이다. Transform.Find로 해석하지 않는다.
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name != name) continue;
                var component = child.GetComponent<T>();
                if (component != null) return component;
            }
            return null;
        }

        private static void SetImage(Image image, Sprite sprite)
        {
            image.gameObject.SetActive(true);
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void LateUpdate()
        {
            if (!_initialized || !_scrollPending || !_expanded) return;
            _scrollPending = false;
            // Batch additions in the same frame, then scroll using the updated content height.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scrollRect.StopMovement();
            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
