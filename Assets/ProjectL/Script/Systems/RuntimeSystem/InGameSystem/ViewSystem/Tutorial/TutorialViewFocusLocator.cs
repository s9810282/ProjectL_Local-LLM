using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 보일 대상(<see cref="TutorialFocusTarget"/>)의 화면 영역을 구합니다(구현 계획 §8 정한 것 1, 2026-10-20 사용자).
    /// </summary>
    /// <remarks>
    /// 기존 표시 참조를 읽습니다. 공격 버튼은 그 버튼의 하위 그래픽만 사용합니다.
    /// 행동 코스트는 현재 행동 유닛 HUD 전체, 상단 체력은 HP 아이콘·수치 영역만 사용합니다.
    /// UI 대상은 그 아래에 실제로 보이는 그래픽을 합친 영역이라 루트 크기 밖의 자식(초상화 옆 체력 막대 등)도 들어갑니다.
    /// 지금 화면에 없는 대상(꺼진 버튼, 없는 유닛 등)은 구멍을 만들지 않습니다. 설정 오류가 아니라 그 순간의 화면 사실이기 때문입니다.
    /// 영역은 화면 좌표(픽셀, 왼쪽 아래 원점)입니다.
    /// </remarks>
    internal sealed class TutorialViewFocusLocator
    {
        private readonly CombatSceneRoot _root;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly List<Rect> _targetRects = new();
        private readonly Dictionary<TutorialFocusTarget, Rect?> _anchors = new();
        private readonly Dictionary<EntityId, Rect?> _modelRects = new();
        private readonly TutorialModelBoundsCache _modelBounds = new();
        private readonly Func<float> _readModelOutlineWidth;
        private float _modelOutlineWidth;
        // 유닛 표시는 전투 표시 표(개체 ID → 표시의 원본)에서 찾습니다. 씬을 검색하지 않습니다.
        private readonly FieldUnitPresenter _units;
        // 유지 UI 대상은 연결마다 한 번 찾아 둡니다. 파괴됐거나 없을 때만 다시 찾고, 켜짐 여부는 쓸 때마다 봅니다.
        private DeckCardList[] _deckLists;
        private MainCharactorUI[] _portraits;
        // 매 갱신의 컴포넌트 수집은 아래 목록을 다시 씁니다(Tick 할당 없음). 서로 중첩해 쓰지 않습니다.
        private readonly List<UnitCardUI> _cards = new();
        private readonly List<Graphic> _graphics = new();
        private readonly List<CanvasGroup> _canvasGroups = new();

        internal TutorialViewFocusLocator(CombatSceneRoot root, ICharacterBufferReadSink characters,
            ICombatStateBufferReadSink combat, Func<float> readModelOutlineWidth)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _readModelOutlineWidth = readModelOutlineWidth ?? throw new ArgumentNullException(nameof(readModelOutlineWidth));
            if (_root.BattleChanger == null)
                throw new InvalidOperationException("[TutorialViewFocusLocator] UIId.BattleChanger의 CombatSceneRoot.battleChanger 참조가 없습니다.");
            if (_root.ActionPanel == null)
                throw new InvalidOperationException("[TutorialViewFocusLocator] UIId.CombatActionPanel의 CombatSceneRoot.actionPanel 참조가 없습니다.");
            _units = _root.FieldUnits != null ? _root.FieldUnits
                : throw new InvalidOperationException("[TutorialViewFocusLocator] UIId.Tutorial에 필요한 CombatSceneRoot.fieldUnits 참조가 없습니다.");
            // 비활성 여부와 구성 누락을 구분합니다. 실제 영역 조회는 표시 때 합니다.
            _ = _root.ActionPanel.AttackButtonRect;
            _ = _root.ActionPanel.EndTurnButtonRect;
            _ = _root.BattleChanger.ButtonRect;
        }

        /// <summary>대상을 한 번 해석해 덮개·UI 점선·모델·앵커가 같은 결과를 쓰게 합니다.</summary>
        internal void Locate(TutorialStepData step, TutorialFocusFrame output)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (output == null) throw new ArgumentNullException(nameof(output));
            // 스타일은 UI 한 곳이 소유합니다. 직렬화로 객체가 교체돼도 이번 조회의 폭은 현재 값을 한 번만 읽습니다.
            _modelOutlineWidth = _readModelOutlineWidth();
            output.Clear();
            _anchors.Clear();
            _modelRects.Clear();
            foreach (TutorialFocusTarget target in step.FocusTargets)
            {
                if (!_anchors.ContainsKey(target))
                    _anchors.Add(target, LocateTarget(target, output, true));
            }
            TutorialFocusTarget anchor = step.ExplanationAnchor;
            output.Anchor = anchor == TutorialFocusTarget.None ? null
                : _anchors.TryGetValue(anchor, out Rect? rect) ? rect : LocateTarget(anchor, output, false);
        }

        internal void LocatePractice(TutorialStepData step, StagePhase stagePhase, TutorialFocusFrame output)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (output == null) throw new ArgumentNullException(nameof(output));
            output.Clear();
            IReadOnlyList<TutorialFocusTarget> targets = stagePhase switch
            {
                StagePhase.Deployment => step.PracticeDeploymentTargets,
                StagePhase.Combat => step.PracticeCombatTargets,
                _ => throw new InvalidOperationException($"[TutorialViewFocusLocator] 실습 표시 국면이 잘못됐습니다: {stagePhase}")
            };
            foreach (TutorialFocusTarget target in targets)
                LocateTarget(target, output, true, practice: true);
        }

        private Rect? LocateTarget(TutorialFocusTarget target, TutorialFocusFrame output, bool highlight, bool practice = false)
        {
            _targetRects.Clear();
            TutorialFocusFrame uiOutput = highlight ? output : null;
            switch (target)
            {
                case TutorialFocusTarget.None: return null;
                // 전체 손패 소개는 기존 흰 영역만 사용하고, 조작 실습에서만 카드별 노란 점선을 표시합니다.
                case TutorialFocusTarget.Hand: AddCards(_targetRects, false, practice ? uiOutput : null); break;
                case TutorialFocusTarget.MainCharacterCard: AddCards(_targetRects, true, uiOutput); break;
                case TutorialFocusTarget.EnterCombatButton:
                    AddVisible(_targetRects, _root.BattleChanger.transform);
                    if (_targetRects.Count > 0) uiOutput?.AddUiTarget(_root.BattleChanger.ButtonRect);
                    break;
                case TutorialFocusTarget.AttackButton: AddVisible(_targetRects, _root.ActionPanel.AttackButtonRect, uiOutput); break;
                case TutorialFocusTarget.EndTurnButton: AddVisible(_targetRects, _root.ActionPanel.EndTurnButtonRect, uiOutput); break;
                case TutorialFocusTarget.MainCharacterHealth: AddMainCharacterHud(_targetRects, healthOnly: true); break;
                case TutorialFocusTarget.ActionCost: AddActorHud(_targetRects, uiOutput); break;
                case TutorialFocusTarget.MainCharacterUnit: return AddUnit(output, FindMainCharacter(CharacterRuntimeArea.Field), highlight);
                case TutorialFocusTarget.CurrentActorUnit: return AddUnit(output, ActiveActor(), highlight);
                case TutorialFocusTarget.EnemyNexus: return AddUnit(output, FindEnemyNexus(), highlight);
                case TutorialFocusTarget.EnemyUnits: return AddUnit(output, FindAnyEnemyUnit(), highlight);
                case TutorialFocusTarget.PlayerUnits:
                    Rect? first = null;
                    foreach (CharacterData character in _characters.Characters)
                    {
                        if (character.Owner != Owner.Player || character.Area != CharacterRuntimeArea.Field || character.Health.IsDead)
                            continue;
                        Rect? area = AddUnit(output, character.EntityId, highlight);
                        first ??= area;
                    }
                    return first;
                default:
                    throw new InvalidOperationException($"[TutorialViewFocusLocator] 알 수 없는 보일 대상입니다: {target}");
            }
            if (highlight)
                foreach (Rect rect in _targetRects) output.AddUi(rect);
            return _targetRects.Count > 0 ? _targetRects[0] : null;
        }

        internal void LocateObstacles(List<Rect> screenRects)
        {
            if (screenRects == null)
                throw new ArgumentNullException(nameof(screenRects));
            screenRects.Clear();
            AddMainCharacterHud(screenRects, healthOnly: false);
            AddVisible(screenRects, _root.BattleChanger != null ? _root.BattleChanger.transform : null);
            AddVisible(screenRects, _root.ActionPanel != null ? _root.ActionPanel.transform : null);
        }

        // 유지 덱의 실제 카드만 사용합니다. 메인은 보유 ID로 식별하며 일시 부재 시 대체하지 않습니다.
        private void AddCards(List<Rect> rects, bool mainOnly, TutorialFocusFrame output)
        {
            CharacterInstanceId main = CharacterInstanceId.Empty;
            if (mainOnly)
            {
                foreach (OwnedCharacterRecord owned in _characters.Owned)
                {
                    if (owned.Role == CharacterOwnershipRole.Main)
                    {
                        main = owned.Id;
                        break;
                    }
                }
                if (main.IsEmpty)
                    return;
            }
            bool found = false;
            Rect union = default;
            foreach (DeckCardList list in FindCached(ref _deckLists))
            {
                if (!list.gameObject.activeInHierarchy)
                    continue;
                list.GetComponentsInChildren(false, _cards);
                foreach (UnitCardUI card in _cards)
                {
                    if (mainOnly && card.OwnedCharacterId != main)
                        continue;
                    if (!TryScreenRect(card.transform as RectTransform, out Rect cardRect) || IsFadedOut(card.transform))
                        continue;
                    if (output != null)
                    {
                        // 루트는 배치용 여백을 포함합니다. 기존 버튼의 표시 대상이 실제 카드 배경입니다.
                        Button button = card.GetComponent<Button>();
                        Graphic body = button != null ? button.targetGraphic : null;
                        if (body == null || !body.transform.IsChildOf(card.transform))
                            throw new InvalidOperationException($"[TutorialViewFocusLocator] UIId.UnitCard '{card.name}'의 Button.targetGraphic에 카드 배경이 필요합니다.");
                        if (body.enabled && body.color.a > 0f && body.gameObject.activeInHierarchy && !IsFadedOut(body.transform))
                            output.AddUiTarget(body.rectTransform);
                    }
                    union = found ? Union(union, cardRect) : cardRect;
                    found = true;
                }
            }
            if (found)
                rects.Add(union);
        }
        // 강조 구멍은 HP 하위 영역만, 설명 박스가 피할 장애물은 기존 HUD 전체를 사용합니다.
        private void AddMainCharacterHud(List<Rect> rects, bool healthOnly, TutorialFocusFrame output = null)
        {
            foreach (MainCharactorUI portrait in FindCached(ref _portraits))
            {
                if (!portrait.gameObject.activeInHierarchy)
                    continue;
                AddVisible(rects, healthOnly ? portrait.HealthBarTransform : portrait.transform, output);
                return;
            }
        }

        private void AddActorHud(List<Rect> rects, TutorialFocusFrame output)
        {
            ICombatUnitHighlightTarget view = FindUnitView(ActiveActor());
            if (view != null && view.HudTransform != null)
                AddVisible(rects, view.HudTransform, output);
        }

        private Rect? AddUnit(TutorialFocusFrame output, EntityId entityId, bool highlight)
        {
            ICombatUnitHighlightTarget view = FindUnitView(entityId);
            if (view == null || !TryReadModelOwner(entityId, out Owner owner)) return null;
            if (owner != Owner.Player && owner != Owner.Enemy)
                throw new InvalidOperationException($"[TutorialViewFocusLocator] UIId.Tutorial의 모델 ({entityId})은 강조를 지원하지 않는 진영입니다: {owner}");
            if (!_modelRects.TryGetValue(entityId, out Rect? rect))
            {
                rect = ModelRect(view);
                _modelRects.Add(entityId, rect);
            }
            if (highlight && rect.HasValue) output.AddModel(view, rect.Value, owner);
            return rect;
        }

        // 모델 이름이나 FocusTarget 대신 현재 공개 전투 기록의 소유 진영을 전달합니다.
        private bool TryReadModelOwner(EntityId entityId, out Owner owner)
        {
            if (_characters.TryGet(entityId, out CharacterData character))
            {
                owner = character.Owner;
                return true;
            }
            if (_combat.HasBattle && _combat.Current != null)
            {
                foreach (StructureCombatFacts structure in _combat.Current.Structures)
                {
                    if (structure.EntityId != entityId) continue;
                    owner = structure.Owner;
                    return true;
                }
            }
            // 표시 해제와 기록 정리 사이에 대상이 사라지는 것은 정상적인 화면 부재입니다.
            owner = Owner.None;
            return false;
        }

        internal void ClearModelBounds() => _modelBounds.Clear();

        private Rect? ModelRect(ICombatUnitHighlightTarget view)
        {
            Camera camera = _root.WorldCamera != null ? _root.WorldCamera : Camera.main;
            if (camera == null || !_modelBounds.TryGet(view, out Bounds bounds)) return null;
            // 셰이더는 정규화된 월드 법선 방향으로 폭만큼 확장하므로 월드 AABB도 각 방향으로 같은 폭을 더합니다.
            bounds.Expand(_modelOutlineWidth * 2f);
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                Vector3 screen = camera.WorldToScreenPoint(corner);
                if (screen.z < 0f)
                    continue;
                xMin = Mathf.Min(xMin, screen.x);
                yMin = Mathf.Min(yMin, screen.y);
                xMax = Mathf.Max(xMax, screen.x);
                yMax = Mathf.Max(yMax, screen.y);
            }

            if (xMax > xMin && yMax > yMin)
                return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return null;
        }

        // 그 아래에서 실제로 보이는 그래픽을 모두 합친 영역이다. 켜져 있어도 투명하거나 CanvasGroup 알파가 0이면 뺀다.
        private void AddVisible(List<Rect> rects, Transform target, TutorialFocusFrame output = null)
        {
            if (target == null || !target.gameObject.activeInHierarchy || IsFadedOut(target))
                return;

            bool found = false;
            Rect union = default;
            target.GetComponentsInChildren(false, _graphics);
            foreach (Graphic graphic in _graphics)
            {
                if (!graphic.enabled || graphic.color.a <= 0f || IsFadedOut(graphic.transform))
                    continue;
                if (!TryScreenRect(graphic.rectTransform, out Rect rect))
                    continue;
                union = found ? Union(union, rect) : rect;
                found = true;
            }

            if (found)
            {
                rects.Add(union);
                output?.AddUiTarget(target as RectTransform);
            }
        }

        private bool IsFadedOut(Transform target)
        {
            target.GetComponentsInParent(false, _canvasGroups);
            foreach (CanvasGroup group in _canvasGroups)
            {
                if (group.alpha <= 0f)
                    return true;
            }

            return false;
        }

        // 지금 전투 UI는 모두 화면 덮개(Overlay) Canvas라 모서리가 곧 화면 좌표다. 카메라 Canvas면 그 카메라로 바꾼다.
        private bool TryScreenRect(RectTransform target, out Rect rect)
        {
            rect = default;
            if (target == null || !target.gameObject.activeInHierarchy)
                return false;

            Canvas canvas = target.GetComponentInParent<Canvas>();
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            Camera camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            target.GetWorldCorners(_corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, _corners[0]);
            Vector2 max = min;
            // 비용 배경처럼 회전한 그래픽도 잘리지 않도록 네 모서리를 모두 포함합니다.
            for (int i = 1; i < _corners.Length; i++)
            {
                Vector2 corner = RectTransformUtility.WorldToScreenPoint(camera, _corners[i]);
                min = Vector2.Min(min, corner);
                max = Vector2.Max(max, corner);
            }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return rect.width > 0f && rect.height > 0f;
        }

        // 꺼진 표시는 화면에 없는 대상으로 봅니다(옛 활성 객체 검색과 같은 결과).
        private ICombatUnitHighlightTarget FindUnitView(EntityId entityId)
        {
            if (entityId == EntityId.Empty)
                return null;

            return _units.TryGetHighlightTarget(entityId, out ICombatUnitHighlightTarget view) && view.IsActive ? view : null;
        }

        // 씬 검색은 연결마다 한 번입니다(JMH 지침 §25). 아직 없거나 그사이 파괴된 것이 있을 때만 다시 찾습니다.
        // 꺼진 객체도 담아 두고 켜짐은 쓰는 쪽이 매번 봅니다. 그래서 숨겨 둔 UI가 다시 켜져도 검색하지 않습니다.
        private static T[] FindCached<T>(ref T[] cache) where T : Component
        {
            bool valid = cache != null && cache.Length > 0;
            for (int i = 0; valid && i < cache.Length; i++)
                valid = cache[i] != null;
            if (!valid)
                cache = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return cache;
        }

        private EntityId ActiveActor()
        {
            return _combat.HasBattle && _combat.Current != null
                ? FindMainCharacter(CharacterRuntimeArea.Field)
                : EntityId.Empty;
        }

        private EntityId FindMainCharacter(CharacterRuntimeArea area)
        {
            foreach (CharacterData character in _characters.Characters)
            {
                if (character.Owner == Owner.Player && character.OwnershipRole == CharacterOwnershipRole.Main
                    && character.Area == area && !character.Health.IsDead)
                    return character.EntityId;
            }

            return EntityId.Empty;
        }

        // 슬라이드 20은 아무 적 하나를 보인다(2026-10-20 사용자). 목록의 첫 생존 적이다.
        private EntityId FindAnyEnemyUnit()
        {
            foreach (CharacterData character in _characters.Characters)
            {
                if (character.Owner == Owner.Enemy && character.Area == CharacterRuntimeArea.Field
                    && !character.Health.IsDead)
                    return character.EntityId;
            }

            return EntityId.Empty;
        }

        private EntityId FindEnemyNexus()
        {
            if (!_combat.HasBattle || _combat.Current == null)
                return EntityId.Empty;

            foreach (StructureCombatFacts structure in _combat.Current.Structures)
            {
                if (structure.Owner == Owner.Enemy && structure.Definition.Key == EntityKey.Structure_Headquarters
                    && !structure.Health.IsDead)
                    return structure.EntityId;
            }

            return EntityId.Empty;
        }

        private static Rect Union(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
                Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }
    }
}
