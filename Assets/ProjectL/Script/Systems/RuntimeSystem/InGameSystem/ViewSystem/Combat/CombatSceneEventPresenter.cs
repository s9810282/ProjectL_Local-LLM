using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>사건 스냅샷의 표시 사실과 이동 보간만 소유합니다. 결과 확인과 풀 반환은 호출자가 담당합니다.</summary>
    internal sealed class CombatSceneEventPresenter : ICombatMovementPlaybackPresenter,
        ICombatResultPresenter, ICombatPresentationControl
    {
        private readonly FieldUnitPresenter _units;
        private readonly float _stepSeconds;
        private readonly List<CharacterCombatFacts> _shownCharacters = new();
        private readonly List<StructureCombatFacts> _shownStructures = new();
        private FieldGridMapper _mapper;
        private CombatCharacterEvent _moving;
        private float _elapsed;
        // Unity 표시 중 재진입한 취소/해제가 이전 사건의 완료 콜백을 되살리지 않도록 한다.
        private int _presentationVersion;

        internal CombatSceneEventPresenter(FieldUnitPresenter units, float stepSeconds)
        {
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
            _stepSeconds = stepSeconds;
        }

        public bool IsMovementComplete => _moving == null;

        /// <summary>Field.Build가 성공한 뒤 현재 Mapper만 연결합니다. 생성 시에는 연결하지 않습니다.</summary>
        internal void Bind(FieldGridMapper mapper)
        {
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            Clear();
            _mapper = mapper;
        }

        internal void Refresh(IReadOnlyList<CharacterData> characters, IReadOnlyList<StructureCombatFacts> structures)
        {
            RequireBound();
            _shownCharacters.Clear();
            for (int i = 0; i < characters.Count; i++)
                _shownCharacters.Add(characters[i].ToFacts());
            _shownStructures.Clear();
            for (int i = 0; i < structures.Count; i++)
                _shownStructures.Add(structures[i]);
            // 같은 사실 목록을 바로 그려 중간 Select/ToArray 복사를 만들지 않는다.
            RenderEvents();
        }

        internal void Clear()
        {
            CancelMovement();
            _mapper = null;
            _shownCharacters.Clear();
            _shownStructures.Clear();
        }

        public void BeginStep(CombatCharacterEvent step)
        {
            RequireBound();
            if (step == null) throw new ArgumentNullException(nameof(step));
            ApplyCharacter(step.Expected);
            _moving = step;
            _elapsed = 0f;
            RenderEvents();
        }

        public void ShowReturn(CombatCharacterEvent returned)
        {
            RequireBound();
            if (returned == null) throw new ArgumentNullException(nameof(returned));
            ApplyCharacter(returned.Final);
            RenderEvents();
        }

        public void TickPresentation(float deltaTime)
        {
            if (_moving == null) return;
            RequireBound();
            _elapsed += Math.Max(0f, deltaTime);
            _units.Interpolate(_moving.Expected, _moving.Final, _mapper, Mathf.Clamp01(_elapsed / _stepSeconds));
            if (_elapsed < _stepSeconds) return;
            ApplyCharacter(_moving.Final);
            _moving = null;
            RenderEvents();
        }

        public void CancelMovement()
        {
            _presentationVersion++;
            _moving = null;
            _elapsed = 0f;
        }

        public void CancelPresentation() => CancelMovement();

        public bool CanPresent(CombatEvent e) => e is CombatCharacterEvent || e is CombatStructureEvent ||
            e is CombatSoulEvent || e is CombatAttackEvent || e is CombatEffectsEvent || e is CombatArtifactEvent ||
            e is CombatTurnEvent || e is CombatGimmickEvent || e is CombatOutcomeEvent ||
            e is CombatMovementSourceEvent || e is CombatThornConsumedEvent ||
            e is CombatObstacleReactionEvent || e is CombatHeldEnvironmentEvent ||
            e is CombatObjectActivationEvent || e is CombatObjectCreatedEvent || e is CombatTornadoEvent;

        public void BeginEvent(CombatEvent e, Action completed)
        {
            RequireBound();
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            int version = _presentationVersion;
            // 환경 메타데이터도 같은 사건 순서로 완료한다. 전용 시각 효과는 별도 표시 작업이다.
            // 비이동 연출은 즉시 반영하며 중간 체력·사망 시점은 최신 Buffer가 아닌 사건 스냅샷을 따른다.
            if (e is CombatCharacterEvent character) ApplyCharacter(character.Final);
            if (e is CombatSoulEvent soul && soul.Characters != null)
                foreach (var change in soul.Characters.Characters) ApplyCharacter(change.Final);
            if (e is CombatStructureEvent structure)
            {
                _shownStructures.RemoveAll(x => x.EntityId == structure.Final.EntityId);
                _shownStructures.Add(structure.Final);
            }
            RenderEvents();
            if (version == _presentationVersion) completed();
        }

        private void ApplyCharacter(CharacterCombatFacts facts)
        {
            _shownCharacters.RemoveAll(character => character.EntityId == facts.EntityId);
            _shownCharacters.Add(facts);
        }

        private void RenderEvents() => _units.RenderFacts(_shownCharacters, _shownStructures, _mapper);

        private void RequireBound()
        {
            if (_mapper == null)
                throw new InvalidOperationException("[CombatSceneEventPresenter] 필드 구성 성공 후 Mapper 연결이 필요합니다.");
        }
    }
}
