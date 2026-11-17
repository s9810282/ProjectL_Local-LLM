#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatInputProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.CombatInputProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 유닛 HUD용 배경 조회입니다. 판(조회 원본 버전)이 바뀌면 Core에 자격 아군 모두의 답을 한 번 묻습니다(새 SSOT 설계 SN2).
    /// 누구를 물을지·답이 무엇에 기대는지는 Core가 정합니다.
    /// </summary>
    /// <remarks>
    /// 다시 물을 때는 답이 올 때까지 이전 아이콘을 둡니다(Q-SN4 A). 답이 오면 받은 유닛마다 보이고, 받은 목록에 없는 유닛은 숨깁니다(계획 TP-16).
    /// 선택 유닛은 UnitInfo와 같은 선택 추적기의 답도 함께 보입니다(같은 판의 같은 Core 함수 답, 계획 TP-9). 마지막으로 보인 답과 같은 객체면
    /// 다시 그리지 않아 매 Tick HUD 쓰기가 없습니다(계획 TP-17).
    /// </remarks>
    internal sealed class CombatHudActionAvailability
    {
        private readonly ICombatViewBufferReadSink _read;
        private readonly ICombatViewBufferWriteSink _write;
        private readonly Action<EntityId, CombatActionAvailability> _show;
        // 유닛마다 마지막으로 보인 답입니다. 숨긴 유닛은 들지 않습니다.
        private readonly Dictionary<EntityId, CombatActionAvailability> _shown = new();
        // 묶음 답을 적용할 때 받은 유닛·숨길 유닛을 모으는 재사용 목록입니다.
        private readonly HashSet<EntityId> _seen = new();
        private readonly List<EntityId> _gone = new();
        private long _battle;
        private long _turn;
        private long _revision;
        // 묶음 답을 받은 판입니다. 지금 판과 같으면 다시 묻지 않습니다.
        private long _answeredRevision;
        private int _request;

        internal CombatHudActionAvailability(ICombatViewBufferReadSink read, ICombatViewBufferWriteSink write,
            Action<EntityId, CombatActionAvailability> show)
        {
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _show = show ?? throw new ArgumentNullException(nameof(show));
        }

        /// <param name="hasForeground">전경 입력 소유자가 활성인지입니다. 활성이면 새로 묻지 않습니다.</param>
        /// <param name="answeredNumber">Core가 마지막으로 답한 요청 번호입니다.</param>
        /// <param name="latest">마지막으로 게시된 미리보기 응답입니다(번호 무관). 없으면 null입니다.</param>
        /// <param name="selected">선택 추적기의 지금 답입니다. 없으면 null입니다.</param>
        internal void Sync(in CombatInputFacts facts, bool hasForeground,
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
            // 판이 바뀌면 요청만 버리고 아이콘은 답이 올 때까지 둡니다(Q-SN4 A).
            if (_revision != facts.QueryRevision)
            {
                ClearRequest();
                _revision = facts.QueryRevision;
            }

            // 슬롯이 전경 요청으로 바뀌었어도 이미 게시된 자기 응답은 번호와 원본이 같으면 쓸 수 있습니다.
            if (_request != 0)
            {
                IReadOnlyList<CombatActionAvailability> answers = latest?.ActionAvailabilities;
                if (latest?.RequestNumber == _request && IsCurrent(answers))
                {
                    ShowAnswers(answers);
                    _answeredRevision = _revision;
                    ClearRequest();
                }
                else if (!_read.HasPreview || _read.Preview.Number != _request || answeredNumber >= _request)
                {
                    ClearRequest();
                }
            }

            if (selected != null)
                Show(selected.ActorId, selected);

            bool hasPreview = _read.HasPreview;
            if (hasForeground || !CombatPreviewSlotRule.ShouldRequestBackground(_answeredRevision == _revision, _request != 0,
                    hasPreview, hasPreview ? _read.Preview.Number : 0, answeredNumber))
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _request = CombatInputProfiler.SetPreview(_write, CombatViewPreviewData.ActionAvailabilities());
#else
            _request = _write.SetPreview(CombatViewPreviewData.ActionAvailabilities());
#endif
        }

        /// <summary>요청과 보인 아이콘을 모두 지웁니다(입력 닫힘·종료·국면 아님·전투나 턴이 바뀜).</summary>
        internal void Clear()
        {
            ClearRequest();
            foreach (EntityId actor in _shown.Keys)
                _show(actor, null);
            _shown.Clear();
            _battle = 0;
            _turn = 0;
            _revision = 0;
            _answeredRevision = 0;
        }

        // 묶음 답의 항목이 모두 지금 전투·턴·판에서 계산됐는지입니다.
        private bool IsCurrent(IReadOnlyList<CombatActionAvailability> answers)
        {
            if (answers == null)
                return false;
            for (int i = 0; i < answers.Count; i++)
            {
                CombatActionAvailability answer = answers[i];
                if (answer.BattleInstanceId != _battle || answer.ControllerTurnSequence != _turn || answer.SourceRevision != _revision)
                    return false;
            }
            return true;
        }

        // 받은 유닛마다 보이고, 받은 목록에 없는 유닛(자격을 잃은 유닛)은 숨깁니다(계획 TP-16).
        // 적용 계측 표지는 묶음 답 하나에 한 번입니다. 선택 답 그리기는 재지 않습니다(그 답의 적용은 선택 추적기가 잽니다, SN-d 검토 L-D1).
        private void ShowAnswers(IReadOnlyList<CombatActionAvailability> answers)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePreview = CombatInputProfiler.Apply(CombatViewPreviewKind.ActionAvailabilities).Auto();
#endif
            _seen.Clear();
            for (int i = 0; i < answers.Count; i++)
            {
                _seen.Add(answers[i].ActorId);
                Show(answers[i].ActorId, answers[i]);
            }
            _gone.Clear();
            foreach (EntityId actor in _shown.Keys)
            {
                if (!_seen.Contains(actor))
                    _gone.Add(actor);
            }
            for (int i = 0; i < _gone.Count; i++)
                Show(_gone[i], null);
        }

        // 아이콘을 맞추는 한 곳입니다. 마지막으로 보인 답과 같은 객체면 그리지 않습니다(계획 TP-17).
        private void Show(EntityId actor, CombatActionAvailability answer)
        {
            if (answer == null)
            {
                if (_shown.Remove(actor))
                    _show(actor, null);
                return;
            }
            if (_shown.TryGetValue(actor, out CombatActionAvailability shown) && ReferenceEquals(shown, answer))
                return;
            _shown[actor] = answer;
            _show(actor, answer);
        }

        private void ClearRequest()
        {
            if (_request != 0)
                _write.ClearPreview(_request);
            _request = 0;
        }
    }
}
