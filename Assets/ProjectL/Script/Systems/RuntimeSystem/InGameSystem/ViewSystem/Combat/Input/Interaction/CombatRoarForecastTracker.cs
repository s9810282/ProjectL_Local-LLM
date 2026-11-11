using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 포효 예고의 배경 조회입니다(포효 예고 설계 RF1~RF5, 계획 TP-11~TP-17). 포효가 임박한 보스(<see cref="IsImminent"/>)마다 Core에
    /// "지금 포효하면"을 한 번 묻고, 받은 범위 칸을 바닥에, 밀릴 대상이 있으면 그 넉백을 밀치기 미리보기와 같은 모양으로 그립니다.
    /// 사실이 바뀌면(결과 확인 뒤) 지우고 다시 묻습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 언제 보일지(카운터 기준)는 이 객체의 <see cref="IsImminent"/> 하나가 정하고, 범위 칸은 Core 응답(<see cref="RoarForecastResult.AreaCells"/>)만 씁니다.
    /// 무장 해제 등 포효할 수 없는 상태는 View가 다시 판단하지 않습니다(Core 실패 답이면 그 보스는 범위 없음).
    /// </para>
    /// <para>
    /// 사실 정체성은 (전투 번호, 조회 원본 버전)입니다. 결과 재생 중·입력 닫힘·종료·밤 전투 아님이면 지웁니다. 새로 묻는 것은 플레이어 입력의
    /// 안정 상태(<see cref="CombatInputPolicy.CanQueryRoarForecast"/>)에서만이라 적 턴에는 보이지 않습니다. 전경 입력(공격 모드·이동·배치·아이템 드래그)
    /// 중에는 새로 묻지 않고, 이미 그린 범위는 남기되 넉백 화살표는 숨깁니다(공격 조준이 호버마다 자기 넉백을 지우는 사이에 끼지 않게, 검토 M1).
    /// 슬롯은 배경 요청 규칙(<see cref="CombatPreviewSlotRule"/>)을 따릅니다.
    /// 보스·범위 목록은 사실이 바뀔 때만 다시 모으므로 매 Tick 할당이 없습니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatRoarForecastTracker
    {
        // 포효 카운터가 이만큼 남았을 때부터 예고합니다(사용자 요청 "1 남았을 때", 설계 Q-RF2 A = 이하).
        private const int ImminentRemaining = 1;

        private readonly ICombatViewBufferReadSink _read;
        private readonly ICombatViewBufferWriteSink _write;
        private readonly ICombatRoarForecastFeedback _display;
        // 지금 사실로 예고할 보스입니다(EntityId 순). 사실이 바뀔 때만 다시 모읍니다.
        private readonly List<EntityId> _bosses = new();
        // 답을 받은 보스의 Core 답(불변)입니다. 실패 답도 "답 받음"으로 둡니다(계획 TP-20).
        private readonly Dictionary<EntityId, RoarForecastResult> _answers = new();
        // 그릴 칸·공격 미리보기 작업 목록입니다(재사용). 보스가 여럿이면 범위를 합치고(겹친 칸은 집합으로 한 번만) 미리보기를 모읍니다.
        private readonly List<CellCoord> _cells = new();
        private readonly HashSet<CellCoord> _cellSet = new();
        private readonly List<AttackPreview> _attacks = new();
        private long _battle;
        private long _revision;
        private int _request;
        private EntityId _requestedBoss;
        private bool _shown;
        // 전경 입력(공격 모드·이동·배치·아이템 드래그) 중이라 포효 넉백을 숨기는지입니다(검토 M1). 바닥 범위는 숨기지 않습니다.
        private bool _hideKnockback;

        internal CombatRoarForecastTracker(ICombatViewBufferReadSink read, ICombatViewBufferWriteSink write,
            ICombatRoarForecastFeedback display)
        {
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _display = display ?? throw new ArgumentNullException(nameof(display));
        }

        /// <summary>
        /// 포효가 임박했는지입니다(보이는 기준의 유일한 식, 설계 RF1·계획 TP-12). 살아 있는 필드 유닛이고 포효 카운터의 남은 수
        /// (임계 − 카운터)가 <see cref="ImminentRemaining"/> 이하입니다. 카운터·임계는 Core 상태 값을 그대로 읽습니다.
        /// </summary>
        /// <remarks>
        /// 증가량(피격 한 번·턴 끝마다 더하는 양)은 보지 않습니다. 지금 데이터(Stage1Boss)는 증가량이 모두 1이라 "1 남음" = "다음 한 번이면 포효"입니다.
        /// 증가량이 2 이상인 포효 보스가 생기면 이 기준을 다시 정합니다(검토 L1, 사용자 기준 "1 남았을 때" 유지).
        /// </remarks>
        private static bool IsImminent(CharacterCombatFacts character)
        {
            if (character.Health.IsDead || character.Area != CharacterRuntimeArea.Field)
                return false;
            ReadOnlySpan<TraitStatusSnapshot> traits = character.Stats.TraitStatuses.Span;
            for (int i = 0; i < traits.Length; i++)
            {
                if (traits[i] is RoarTraitStatusSnapshot roar)
                    return roar.HitThreshold - roar.HitCount <= ImminentRemaining;
            }
            return false;
        }

        /// <summary>
        /// 매 Tick 맞춥니다: 지울 사실이면 지우고, 사실이 바뀌었으면 보스를 다시 모으고, 자기 응답이면 그리고, 답을 못 받은 보스가 있으면 묻습니다.
        /// </summary>
        /// <param name="hasForeground">전경 입력 소유자가 활성인지입니다. 활성이면 새로 묻지 않고(TP-15) 포효 넉백을 숨깁니다(검토 M1).</param>
        /// <param name="answeredNumber">Core가 마지막으로 답한 요청 번호입니다.</param>
        /// <param name="latest">마지막으로 게시된 미리보기 응답입니다(번호 무관). 없으면 null입니다.</param>
        internal void Sync(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, bool hasForeground,
            int answeredNumber, CombatPreviewResult latest)
        {
            if (!CanKeep(facts))
            {
                Clear();
                return;
            }
            if (_battle != facts.BattleInstanceId || _revision != facts.QueryRevision)
            {
                Clear();
                _battle = facts.BattleInstanceId;
                _revision = facts.QueryRevision;
                CollectBosses(characters);
            }
            // 공격 조준은 호버 칸을 옮길 때마다 자기 넉백을 지웁니다. 그 사이에 포효 넉백이 끼어 보이지 않게 전경 입력 중에는 넉백만 숨기고,
            // 전경 입력이 끝나면 다시 넘깁니다(검토 M1). 그리는 중일 때만 다시 넘깁니다.
            if (_hideKnockback != hasForeground)
            {
                _hideKnockback = hasForeground;
                if (_shown)
                    Redraw();
            }
            if (_request != 0)
                ConsumeOrForget(latest, answeredNumber);

            if (_answers.Count == _bosses.Count || hasForeground || !CombatInputPolicy.CanQueryRoarForecast(facts))
                return;
            bool hasPreview = _read.HasPreview;
            if (!CombatPreviewSlotRule.ShouldRequestBackground(false, _request != 0,
                    hasPreview, hasPreview ? _read.Preview.Number : 0, answeredNumber))
                return;

            for (int i = 0; i < _bosses.Count; i++)
            {
                if (_answers.ContainsKey(_bosses[i]))
                    continue;
                _request = _write.SetPreview(CombatViewPreviewData.RoarForecast(_bosses[i]));
                _requestedBoss = _bosses[i];
                return;
            }
        }

        /// <summary>요청·그린 범위·모은 보스를 모두 지웁니다(표현 정리·정지·지울 사실). 지울 것이 없으면 바로 돌아갑니다.</summary>
        internal void Clear()
        {
            if (_request == 0 && !_shown && _battle == 0)
                return;
            ClearRequest();
            _bosses.Clear();
            _answers.Clear();
            _cells.Clear();
            _cellSet.Clear();
            _attacks.Clear();
            _battle = 0;
            _revision = 0;
            if (!_shown)
                return;
            _shown = false;
            _display.ClearRoarForecast();
        }

        // 예고를 남겨 둘 수 있는 사실인지입니다. 아니면 지웁니다(계획 TP-13).
        private static bool CanKeep(in CombatInputFacts facts) =>
            facts.HasBattle && facts.QueryRevision > 0 && facts.StagePhase == StagePhase.Combat && !facts.IsPresenting
                && !CombatInputPolicy.IsInputClosed(facts) && !CombatInputPolicy.IsTerminal(facts);

        private void CollectBosses(IReadOnlyList<CharacterData> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterCombatFacts character = characters[i].ToFacts();
                if (IsImminent(character))
                    _bosses.Add(character.EntityId);
            }
            // EntityId 순입니다(Core 대기 판정과 같은 순서). 기본 비교자를 써 대리자를 만들지 않습니다.
            _bosses.Sort();
        }

        // 자기 응답이면 범위를 기억하고 다시 그립니다. 응답이 아닌데 슬롯을 잃었거나 다른 답이 났으면 기다림을 끝냅니다(다음 Tick에 다시 묻음).
        private void ConsumeOrForget(CombatPreviewResult latest, int answeredNumber)
        {
            // 답이 있으면 응답도 있습니다(답은 응답의 칸).
            RoarForecastResult? answer = latest?.RoarForecast;
            if (answer.HasValue && latest.RequestNumber == _request && answer.Value.BossId == _requestedBoss)
            {
                _answers[_requestedBoss] = answer.Value;
                ClearRequest();
                Redraw();
            }
            else if (!_read.HasPreview || _read.Preview.Number != _request || answeredNumber >= _request)
            {
                ClearRequest();
            }
        }

        // 성공한 답의 범위 칸을 합치고 대상이 있는 답의 공격 미리보기(넉백 기록)를 모아 넘깁니다. 범위가 없으면 지웁니다.
        // 전경 입력 중이면 넉백은 빈 목록으로 넘겨 범위만 남깁니다(검토 M1).
        private void Redraw()
        {
            _cells.Clear();
            _cellSet.Clear();
            _attacks.Clear();
            foreach (RoarForecastResult answer in _answers.Values)
            {
                if (!answer.Succeeded)
                    continue;
                ReadOnlySpan<CellCoord> area = answer.AreaCells.Span;
                for (int i = 0; i < area.Length; i++)
                {
                    if (_cellSet.Add(area[i]))
                        _cells.Add(area[i]);
                }
                if (answer.Attack.HasValue)
                    _attacks.Add(answer.Attack.Value);
            }
            if (_cells.Count > 0)
            {
                _shown = true;
                IReadOnlyList<AttackPreview> attacks = _hideKnockback ? Array.Empty<AttackPreview>() : _attacks;
                _display.ShowRoarForecast(_cells, attacks);
            }
            else if (_shown)
            {
                _shown = false;
                _display.ClearRoarForecast();
            }
        }

        // 자기 요청이 슬롯에 있을 때만 비웁니다(판단은 Buffer의 번호 있는 비우기).
        private void ClearRequest()
        {
            if (_request != 0)
                _write.ClearPreview(_request);
            _request = 0;
            _requestedBoss = EntityId.Empty;
        }
    }
}
