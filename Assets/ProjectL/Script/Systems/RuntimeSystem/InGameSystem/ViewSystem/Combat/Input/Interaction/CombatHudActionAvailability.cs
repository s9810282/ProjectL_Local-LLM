#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatInputProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.CombatInputProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>유닛 HUD용 배경 조회입니다. 턴 처음과 표시 대상의 사실이 바뀔 때만 Core에 묻습니다.</summary>
    internal sealed class CombatHudActionAvailability
    {
        private readonly ICombatViewBufferReadSink _read;
        private readonly ICombatViewBufferWriteSink _write;
        private readonly Action<EntityId, CombatActionAvailability> _show;
        private readonly HashSet<EntityId> _dirty = new();
        private Dictionary<EntityId, CharacterCombatFacts> _observed = new();
        private Dictionary<EntityId, CharacterCombatFacts> _next = new();
        private long _battle;
        private long _turn;
        private long _revision;
        private bool _hasNonSingleCellUnit;
        private int _request;
        private EntityId _requestedActor;

        internal CombatHudActionAvailability(ICombatViewBufferReadSink read, ICombatViewBufferWriteSink write,
            Action<EntityId, CombatActionAvailability> show)
        {
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _show = show ?? throw new ArgumentNullException(nameof(show));
        }

        internal void Sync(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, bool hasForeground,
            int answeredNumber, CombatPreviewResult latest, CombatActionAvailability selected)
        {
            if (CombatInputPolicy.IsInputClosed(facts) || CombatInputPolicy.IsTerminal(facts))
            {
                Clear();
                return;
            }
            if (facts.IsPresenting)
                return;
            if (!facts.HasBattle || facts.StagePhase != StagePhase.Combat || facts.Phase != CombatFlowPhase.PlayerInput)
            {
                Clear();
                return;
            }
            if (facts.QueryRevision <= 0 || !CombatInputPolicy.CanQueryActionAvailability(facts))
                return;

            if (_battle != facts.BattleInstanceId || _turn != facts.ControllerTurnSequence)
            {
                Clear();
                _battle = facts.BattleInstanceId;
                _turn = facts.ControllerTurnSequence;
            }
            if (_revision != facts.QueryRevision)
            {
                ClearRequest();
                ObserveCharacters(characters);
                _revision = facts.QueryRevision;
            }

            // 슬롯이 전경 요청으로 바뀌었어도 이미 게시된 자기 응답은 번호와 원본이 같으면 쓸 수 있습니다.
            if (_request != 0)
            {
                CombatActionAvailability answer = latest?.ActionAvailability;
                if (latest?.RequestNumber == _request && answer != null && answer.ActorId == _requestedActor &&
                    answer.BattleInstanceId == _battle && answer.ControllerTurnSequence == _turn && answer.SourceRevision == _revision)
                {
                    Apply(answer);
                    ClearRequest();
                }
                else if (!_read.HasPreview || _read.Preview.Number != _request || answeredNumber >= _request)
                {
                    ClearRequest();
                }
            }

            if (selected != null)
                Apply(selected);

            if (hasForeground || _dirty.Count == 0 ||
                !CombatPreviewSlotRule.ShouldRequestBackground(false, _request != 0,
                    _read.HasPreview, _read.HasPreview ? _read.Preview.Number : 0, answeredNumber))
                return;

            foreach (EntityId actor in _dirty)
            {
                // 선택 유닛은 UnitInfo와 같은 추적기의 답으로 갱신합니다.
                if (actor == facts.SelectedActorId) continue;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _request = CombatInputProfiler.SetPreview(_write, CombatViewPreviewData.ActionAvailability(actor));
#else
                _request = _write.SetPreview(CombatViewPreviewData.ActionAvailability(actor));
#endif
                _requestedActor = actor;
                return;
            }
        }

        private void ObserveCharacters(IReadOnlyList<CharacterData> characters)
        {
            bool hasNonSingleCellUnit = false;
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterCombatFacts character = characters[i].ToFacts();
                if (character.Area == CharacterRuntimeArea.Field && character.OccupiedCells.Count != 1)
                    hasNonSingleCellUnit = true;
                if (CombatActionAvailabilityTracker.IsEligibleActor(character))
                    _next.Add(character.EntityId, character);
            }

            // 필드 점유 형태는 다른 유닛의 공격 가용성에도 영향을 줄 수 있습니다. 결과는 Core에서 다시 받습니다.
            bool fieldShapeChanged = hasNonSingleCellUnit != _hasNonSingleCellUnit;
            _hasNonSingleCellUnit = hasNonSingleCellUnit;
            foreach (var pair in _next)
            {
                if ((fieldShapeChanged || !_observed.TryGetValue(pair.Key, out var before) || !before.SameValues(pair.Value))
                    && _dirty.Add(pair.Key))
                    _show(pair.Key, null);
            }
            foreach (EntityId actor in _observed.Keys)
            {
                if (_next.ContainsKey(actor)) continue;
                _dirty.Remove(actor);
                _show(actor, null);
            }
            (_observed, _next) = (_next, _observed);
            _next.Clear();
        }

        private void Apply(CombatActionAvailability availability)
        {
            if (_dirty.Remove(availability.ActorId))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var profilePreview = CombatInputProfiler.Apply(CombatViewPreviewKind.ActionAvailability).Auto();
#endif
                _show(availability.ActorId, availability);
            }
        }

        private void ClearRequest()
        {
            if (_request != 0)
                _write.ClearPreview(_request);
            _request = 0;
            _requestedActor = EntityId.Empty;
        }

        internal void Clear()
        {
            ClearRequest();
            foreach (EntityId actor in _observed.Keys)
                _show(actor, null);
            _observed.Clear();
            _dirty.Clear();
            _battle = 0;
            _turn = 0;
            _revision = 0;
            _hasNonSingleCellUnit = false;
        }
    }
}
