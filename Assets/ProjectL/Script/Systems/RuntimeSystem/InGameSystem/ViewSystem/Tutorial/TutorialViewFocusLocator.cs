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
        private readonly List<Rect> _targetRects = new List<Rect>();
        private readonly TutorialFocusTarget[] _singleTarget = new TutorialFocusTarget[1];

        internal TutorialViewFocusLocator(CombatSceneRoot root, ICharacterBufferReadSink characters,
            ICombatStateBufferReadSink combat)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        /// <summary><paramref name="screenRects"/>를 비우고 대상마다 찾은 화면 영역을 넣습니다.</summary>
        internal void Locate(IReadOnlyList<TutorialFocusTarget> targets, List<Rect> screenRects)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));
            if (screenRects == null)
                throw new ArgumentNullException(nameof(screenRects));

            screenRects.Clear();
            for (int i = 0; i < targets.Count; i++)
            {
                switch (targets[i])
                {
                    case TutorialFocusTarget.Hand:
                        AddHand(screenRects);
                        break;
                    case TutorialFocusTarget.MainCharacterCard:
                        AddMainCharacterCard(screenRects);
                        break;
                    case TutorialFocusTarget.EnterCombatButton:
                        AddVisible(screenRects, _root.BattleChanger != null ? _root.BattleChanger.transform : null);
                        break;
                    case TutorialFocusTarget.AttackButton:
                        AddVisible(screenRects, _root.ActionPanel != null ? _root.ActionPanel.AttackButtonRect : null);
                        break;
                    case TutorialFocusTarget.MainCharacterHealth:
                        AddMainCharacterHud(screenRects, healthOnly: true);
                        break;
                    case TutorialFocusTarget.ActionCost:
                        AddActorHud(screenRects);
                        break;
                    case TutorialFocusTarget.MainCharacterUnit:
                        AddUnit(screenRects, FindMainCharacter(CharacterRuntimeArea.Field));
                        break;
                    case TutorialFocusTarget.CurrentActorUnit:
                        AddUnit(screenRects, ActiveActor());
                        break;
                    case TutorialFocusTarget.EnemyNexus:
                        AddUnit(screenRects, FindEnemyNexus());
                        break;
                    case TutorialFocusTarget.EnemyUnits:
                        AddUnit(screenRects, FindAnyEnemyUnit());
                        break;
                    default:
                        throw new InvalidOperationException($"[TutorialViewFocusLocator] 알 수 없는 보일 대상입니다: {targets[i]}");
                }
            }
        }

        internal bool TryLocateTarget(TutorialFocusTarget target, out Rect rect)
        {
            _singleTarget[0] = target;
            Locate(_singleTarget, _targetRects);
            rect = _targetRects.Count > 0 ? _targetRects[0] : default;
            return _targetRects.Count > 0;
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

        private void AddHand(List<Rect> rects) => AddCards(rects, false);
        private void AddMainCharacterCard(List<Rect> rects) => AddCards(rects, true);

        // 유지 덱의 실제 카드만 사용합니다. 메인은 보유 ID로 식별하며 일시 부재 시 대체하지 않습니다.
        private void AddCards(List<Rect> rects, bool mainOnly)
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
            DeckCardList[] lists = UnityEngine.Object.FindObjectsByType<DeckCardList>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (DeckCardList list in lists)
            {
                foreach (UnitCardUI card in list.GetComponentsInChildren<UnitCardUI>(false))
                {
                    if (mainOnly && card.OwnedCharacterId != main)
                        continue;
                    if (!TryScreenRect(card.transform as RectTransform, out Rect cardRect) || IsFadedOut(card.transform))
                        continue;
                    union = found ? Union(union, cardRect) : cardRect;
                    found = true;
                }
            }
            if (found)
                rects.Add(union);
        }
        // 강조 구멍은 HP 하위 영역만, 설명 박스가 피할 장애물은 기존 HUD 전체를 사용합니다.
        private void AddMainCharacterHud(List<Rect> rects, bool healthOnly)
        {
            MainCharactorUI[] portraits = UnityEngine.Object.FindObjectsByType<MainCharactorUI>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (portraits.Length > 0)
                AddVisible(rects, healthOnly ? portraits[0].HealthBarTransform : portraits[0].transform);
        }

        private void AddActorHud(List<Rect> rects)
        {
            FieldUnitView view = FindUnitView(ActiveActor());
            if (view != null && view.Hud != null)
                AddVisible(rects, view.Hud.transform);
        }

        private void AddUnit(List<Rect> rects, EntityId entityId)
        {
            FieldUnitView view = FindUnitView(entityId);
            if (view == null)
                return;

            Camera camera = _root.WorldCamera != null ? _root.WorldCamera : Camera.main;
            if (camera == null)
                return;

            bool found = false;
            Bounds bounds = default;
            foreach (Renderer renderer in view.GetComponentsInChildren<Renderer>(false))
            {
                if (!renderer.enabled
                    || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer || renderer is SpriteRenderer))
                    continue;
                if (found)
                    bounds.Encapsulate(renderer.bounds);
                else
                    bounds = renderer.bounds;
                found = true;
            }

            if (!found)
                return;

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
                rects.Add(Rect.MinMaxRect(xMin, yMin, xMax, yMax));
        }

        // 그 아래에서 실제로 보이는 그래픽을 모두 합친 영역이다. 켜져 있어도 투명하거나 CanvasGroup 알파가 0이면 뺀다.
        private void AddVisible(List<Rect> rects, Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy || IsFadedOut(target))
                return;

            bool found = false;
            Rect union = default;
            foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(false))
            {
                if (!graphic.enabled || graphic.color.a <= 0f || IsFadedOut(graphic.transform))
                    continue;
                if (!TryScreenRect(graphic.rectTransform, out Rect rect))
                    continue;
                union = found ? Union(union, rect) : rect;
                found = true;
            }

            if (found)
                rects.Add(union);
        }

        private static bool IsFadedOut(Transform target)
        {
            foreach (CanvasGroup group in target.GetComponentsInParent<CanvasGroup>())
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

        private static FieldUnitView FindUnitView(EntityId entityId)
        {
            if (entityId == EntityId.Empty)
                return null;

            FieldUnitView[] views = UnityEngine.Object.FindObjectsByType<FieldUnitView>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (FieldUnitView view in views)
            {
                if (view.EntityId == entityId)
                    return view;
            }

            return null;
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
