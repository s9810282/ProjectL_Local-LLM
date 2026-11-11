using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>선택한 아군의 남은 일반 행동을 유닛 위 프리팹으로 표시합니다.</summary>
    internal sealed class CombatUnitActionCursorPresenter
    {
        private readonly Func<EntityId, Transform> _anchorOf;
        private readonly GameObject _prefab;
        private readonly Vector3 _offset;
        private readonly Color _movedColor;
        private readonly Dictionary<Transform, CursorEntry> _cursors = new();
        private readonly HashSet<Transform> _shown = new();

        internal CombatUnitActionCursorPresenter(Func<EntityId, Transform> anchorOf, GameObject prefab, Vector3 offset,
            Color movedColor)
        {
            _anchorOf = anchorOf ?? throw new ArgumentNullException(nameof(anchorOf));
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _offset = offset;
            _movedColor = movedColor;
        }

        internal void Refresh(IReadOnlyList<CharacterData> characters, CombatTurnFacts turn, CombatFlowPhase phase,
            EntityId selectedActorId)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (turn == null) throw new ArgumentNullException(nameof(turn));
            _shown.Clear();
            if (phase == CombatFlowPhase.PlayerInput && turn.StagePhase == StagePhase.Combat &&
                turn.ControllerPhase == CombatControllerPhase.Player)
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    CharacterData character = characters[i];
                    if (character.Owner != Owner.Player || character.Area != CharacterRuntimeArea.Field ||
                        character.Health.IsDead ||
                        !(UnitActionUseRules.CanStartMove(character.ActionUse) || UnitActionUseRules.CanStartAttack(character.ActionUse)))
                        continue;

                    Transform anchor = _anchorOf(character.EntityId);
                    if (anchor == null) continue;

                    if (!_cursors.TryGetValue(anchor, out CursorEntry cursor))
                    {
                        cursor = Create(anchor);
                        _cursors.Add(anchor, cursor);
                    }
                    cursor.SetMoved(character.ActionUse == UnitActionUse.Moved);
                    bool selected = character.EntityId == selectedActorId;
                    if (selected) _shown.Add(anchor);
                    cursor.SetVisible(selected);
                }
            }

            foreach (KeyValuePair<Transform, CursorEntry> pair in _cursors)
                if (!_shown.Contains(pair.Key)) pair.Value.SetVisible(false);
        }

        internal void Clear()
        {
            _shown.Clear();
            foreach (CursorEntry cursor in _cursors.Values) cursor.SetVisible(false);
        }

        internal void Hide(Transform anchor)
        {
            if (anchor != null && _cursors.TryGetValue(anchor, out CursorEntry cursor))
                cursor.SetVisible(false);
        }

        internal void Forget(Transform anchor)
        {
            if (anchor == null || !_cursors.TryGetValue(anchor, out CursorEntry cursor)) return;
            _cursors.Remove(anchor);
            _shown.Remove(anchor);
            DestroyCursor(cursor.Instance);
        }

        internal void Release()
        {
            Clear();
            foreach (CursorEntry cursor in _cursors.Values)
                DestroyCursor(cursor.Instance);
            _cursors.Clear();
        }

        private static void DestroyCursor(GameObject cursor)
        {
            if (cursor == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(cursor);
            else UnityEngine.Object.DestroyImmediate(cursor);
        }

        private CursorEntry Create(Transform anchor)
        {
            GameObject cursor = UnityEngine.Object.Instantiate(_prefab);
            try
            {
                // 머리 위 커서는 유닛 몸체가 아니므로 튜토리얼 모델 경계에서 제외합니다.
                if (cursor.GetComponent<UnitModelBoundsExclusion>() == null)
                    cursor.AddComponent<UnitModelBoundsExclusion>();
                // 유닛별 모델 배율은 상속하지 않고, 프리팹 월드 크기를 보존한 채 이동·회전만 따라갑니다.
                cursor.transform.rotation = anchor.rotation * _prefab.transform.localRotation;
                cursor.transform.SetParent(anchor, true);
                // 프리팹 루트의 원점이 임의 위치여도 보이는 형상의 중심을 머리 위에 맞춥니다.
                Renderer[] renderers = cursor.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    throw new InvalidOperationException("[CombatUnitActionCursorPresenter] Unit Cursor 프리팹에 Renderer가 필요합니다.");
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                cursor.transform.position += anchor.position + _offset - bounds.center;
                // 월드 커서는 포인터 대상이 아닙니다. 프리팹의 Collider가 유닛 클릭을 가리지 않게 합니다.
                Collider[] colliders = cursor.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
                return new CursorEntry(cursor, renderers, _movedColor);
            }
            catch
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(cursor);
                else UnityEngine.Object.DestroyImmediate(cursor);
                throw;
            }
        }

        private sealed class CursorEntry
        {
            private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
            private static readonly int ColorId = Shader.PropertyToID("_Color");
            private readonly Renderer[] _renderers;
            private readonly MaterialPropertyBlock[] _originalBlocks;
            private readonly MaterialPropertyBlock[] _movedBlocks;
            private bool _isMoved;

            internal GameObject Instance { get; }

            internal CursorEntry(GameObject instance, Renderer[] renderers, Color movedColor)
            {
                Instance = instance;
                _renderers = renderers;
                _originalBlocks = new MaterialPropertyBlock[renderers.Length];
                _movedBlocks = new MaterialPropertyBlock[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    Material material = renderer.sharedMaterial;
                    int property = material != null && material.HasProperty(BaseColorId) ? BaseColorId
                        : material != null && material.HasProperty(ColorId) ? ColorId : 0;
                    if (property == 0)
                        throw new InvalidOperationException($"[CombatUnitActionCursorPresenter] 커서 Renderer의 색 속성이 없습니다: {renderer.name}.");
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original);
                    _originalBlocks[i] = original;
                    var moved = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(moved);
                    moved.SetColor(property, movedColor);
                    _movedBlocks[i] = moved;
                }
            }

            internal void SetMoved(bool moved)
            {
                if (_isMoved == moved) return;
                for (int i = 0; i < _renderers.Length; i++)
                {
                    MaterialPropertyBlock block = moved ? _movedBlocks[i] : _originalBlocks[i];
                    _renderers[i].SetPropertyBlock(block.isEmpty ? null : block);
                }
                _isMoved = moved;
            }

            internal void SetVisible(bool visible)
            {
                if (Instance != null && Instance.activeSelf != visible) Instance.SetActive(visible);
            }
        }
    }
}
