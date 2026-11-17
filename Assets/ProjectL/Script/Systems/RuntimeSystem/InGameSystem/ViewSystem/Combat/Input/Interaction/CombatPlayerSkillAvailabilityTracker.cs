#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatInputProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.CombatInputProfiler;
#endif
using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.PlayerSkill.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 행동 패널 스킬 버튼의 배경 가용성 조회입니다(전투 스킬 View 설계 SK4·P3). 행동 가능 여부 조회(<see cref="CombatActionAvailabilityTracker"/>)와 같은 꼴로,
    /// 전투·턴·조회 판·결과 번호·행위자(스킬 상태의 메인)가 같은 답만 보관하고, 슬롯이 한가할 때만 묻습니다(<see cref="CombatPreviewSlotRule"/>).
    /// </summary>
    /// <remarks>
    /// 쓸 수 있는지·남은 횟수·이번 턴 사용은 Core 답만 씁니다(View가 스킬 상태로 계산하지 않음). 전경 입력(스킬 모드 포함) 중에는 대기만 내려놓고 받은 답은 남깁니다.
    /// 스킬 상태가 없으면(메인에 스킬이 묶이지 않음) 묻지 않고 버튼은 숨깁니다.
    /// </remarks>
    internal sealed class CombatPlayerSkillAvailabilityTracker
    {
        private readonly ICombatViewBufferReadSink _input;
        private readonly ICombatViewBufferWriteSink _write;
        private readonly ICombatStateBufferReadSink _state;
        private readonly IPlayerSkillVisualTableQuery _visuals;
        private bool _stopped;
        private bool _hasKey;
        private long _battleInstance;
        private long _turnSequence;
        private long _queryRevision;
        private int _resultSequence;
        private EntityId _actor;
        private int _requestNumber;
        private PlayerSkillAvailability _answer;

        internal CombatPlayerSkillAvailabilityTracker(ICombatViewBufferReadSink input, ICombatViewBufferWriteSink write,
            ICombatStateBufferReadSink state, IPlayerSkillVisualTableQuery visuals)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
        }

        /// <summary>
        /// 열쇠·슬롯을 맞추고 자기 답을 소비한 뒤, 전경이 없는 안정된 입력에서만 배경 조회를 게시합니다.
        /// </summary>
        internal void Sync(in CombatInputFacts facts, bool hasForeground, int answeredPreviewNumber)
        {
            if (_stopped)
                return;
            EntityId actor = CombatPlayerSkillInteraction.ActorOf(_state);
            if (facts.QueryRevision <= 0 || actor.IsEmpty || !CombatInputPolicy.CanQuerySkillAvailability(facts))
            {
                Clear();
                return;
            }
            if (!Matches(facts, actor))
            {
                Clear();
                _hasKey = true;
                _battleInstance = facts.BattleInstanceId;
                _turnSequence = facts.ControllerTurnSequence;
                _queryRevision = facts.QueryRevision;
                _resultSequence = facts.LatestResultSequence;
                _actor = actor;
            }
            if (_requestNumber != 0 && !OwnsSlot)
                DropPending();
            Consume(facts.PreviewAnswer);
            if (hasForeground)
            {
                DropPending();
                return;
            }
            if (!CombatPreviewSlotRule.ShouldRequestBackground(_answer != null, _requestNumber != 0,
                _input.HasPreview, _input.HasPreview ? _input.Preview.Number : 0, answeredPreviewNumber))
                return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _requestNumber = CombatInputProfiler.SetPreview(_write, CombatViewPreviewData.ForPlayerSkillAvailability(_actor));
#else
            _requestNumber = _write.SetPreview(CombatViewPreviewData.ForPlayerSkillAvailability(_actor));
#endif
        }

        /// <summary>
        /// 버튼 값입니다. 스킬 상태가 없으면 숨김입니다. 답 전이거나 열쇠가 맞지 않거나 진입할 수 없으면(고른 유닛이 메인이 아님 포함 — 계획 §6.6) 보이되 누를 수 없습니다
        /// (스킬 모드 중이면 끄기로 누를 수 있음). 아이콘·이름·설명은 스킬 외형 표의 (스킬, 강화) 줄입니다.
        /// </summary>
        /// <param name="isActive">스킬 모드 중인지입니다.</param>
        /// <param name="canEnter">키와 같은 진입 판단입니다(<c>CombatInteraction.CanEnterSkill</c>).</param>
        internal CombatSkillButton ReadButton(in CombatInputFacts facts, bool isActive, bool canEnter)
        {
            PlayerSkillBattleState skill = _state.HasBattle ? _state.Current.CommandState.PlayerSkill : null;
            if (skill == null || _stopped)
                return default;
            PlayerSkillVisualLook look = _visuals.Get(skill.Definition.Key, skill.UpgradeKey);
            PlayerSkillAvailability answer = Read(facts, skill.ActorId);
            bool canUse = isActive || (canEnter && answer != null && answer.CanUse);
            int remaining = answer?.RemainingUses ?? skill.RemainingUses;
            int limit = answer?.StageUseLimit ?? skill.Definition.StageUseLimit;
            return new CombatSkillButton(true, canUse, isActive, remaining, limit, look.Icon, look.DisplayName, look.Description);
        }

        /// <summary>새 결과·입력 닫힘의 정리입니다. 자기 요청 번호만 비웁니다.</summary>
        internal void Clear()
        {
            _hasKey = false;
            _answer = null;
            DropPending();
        }

        /// <summary>연결 입력 정지 뒤에는 다시 조회하지 않습니다.</summary>
        internal void Stop()
        {
            _stopped = true;
            Clear();
        }

        private PlayerSkillAvailability Read(in CombatInputFacts facts, EntityId actor) =>
            _answer != null && Matches(facts, actor) && CombatInputPolicy.CanQuerySkillAvailability(facts) ? _answer : null;

        private bool Matches(in CombatInputFacts facts, EntityId actor) =>
            _hasKey && _battleInstance == facts.BattleInstanceId && _turnSequence == facts.ControllerTurnSequence
                && _queryRevision > 0 && _queryRevision == facts.QueryRevision && _resultSequence == facts.LatestResultSequence && _actor == actor;

        private bool OwnsSlot => _input.HasPreview && _input.Preview.Number == _requestNumber
            && _input.Preview.Data.Kind == CombatViewPreviewKind.SkillAvailability && _input.Preview.Data.EntityId == _actor;

        private void Consume(CombatPreviewResult preview)
        {
            PlayerSkillAvailability answer = preview?.SkillAvailability;
            if (_requestNumber == 0 || !OwnsSlot || preview == null || preview.RequestNumber != _requestNumber || answer == null)
                return;
            if (!CombatPlayerSkillInteraction.IsSource(answer.Source, _actor, _battleInstance, _turnSequence, _queryRevision))
                return;
            int number = _requestNumber;
            _requestNumber = 0;
            _answer = answer;
            _write.ClearPreview(number);
        }

        private void DropPending()
        {
            if (_requestNumber == 0)
                return;
            int number = _requestNumber;
            _requestNumber = 0;
            _write.ClearPreview(number);
        }
    }
}
