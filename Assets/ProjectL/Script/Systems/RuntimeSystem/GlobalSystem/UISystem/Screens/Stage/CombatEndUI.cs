using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.ServiceKeys.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>승리·패배·포기 완료를 표시하고 기존 완료 확인 입구를 한 번 호출합니다. 보상을 지급하지 않습니다.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatEndUI : UIBase, IUIInputReceiver
    {
        [SerializeField] private EndingRunSummaryUI ending;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private TMP_Text victoryStage;
        [SerializeField] private TMP_Text victoryGold;
        [SerializeField] private TMP_Text victoryReputation;
        [SerializeField] private TMP_Text result;
        [SerializeField] private TMP_Text defeatStage;
        [SerializeField] private TMP_Text runReputation;
        [SerializeField] private TMP_Text runGoldSpent;
        [SerializeField] private GameObject lastHit;
        [SerializeField] private CombatRewardSlot[] characters;
        [SerializeField] private Button confirmButton;
        [Tooltip("각 레벨 구간을 채우는 시간입니다. 중간 요구 경험치를 재계산하지 않고 비율만 연출합니다.")]
        [SerializeField, Min(0.01f)] private float experienceSegmentSeconds = 0.65f;

        private IUISystemService _ui;
        private IUnitVisualTableQuery _visuals;
        private IStageProgressBufferReadSink _stages;
        private ICurrentProgressBufferReadSink _current;
        private IRunProgressBufferReadSink _run;
        private Action _confirmed;
        private bool _inputBound;
        private bool _shown;

        public CombatEndUI() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.CombatEnd;

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ValidateReferences();
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _stages = buffers.StageProgressReadSink
                ?? throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd의 StageProgress Buffer가 없습니다.");
            _current = buffers.CurrentProgressReadSink
                ?? throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd의 CurrentProgress Buffer가 없습니다.");
            _run = buffers.RunProgressReadSink
                ?? throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd의 RunProgress Buffer가 없습니다.");
            if (!_inputBound)
            {
                UIInputBinding.Attach(confirmButton, this, 0);
                _inputBound = true;
            }
        }

        public override void GetUIData(IServiceModule query) { }

        internal void Configure(IUnitVisualTableQuery visuals)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
        }

        internal void Present(BattleCompletionChanges completion, Action confirmed)
        {
            if (completion == null) throw new ArgumentNullException(nameof(completion));
            if (confirmed == null) throw new ArgumentNullException(nameof(confirmed));
            ValidateReferences();
            if (_ui == null || !_inputBound || _visuals == null || _stages == null || _current == null || _run == null)
                throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd의 UISystem·입력·외형·Buffer 연결이 필요합니다.");
            if (_shown || _confirmed != null || ending.IsOpen)
                throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd에 이미 완료가 표시되어 있습니다.");

            bool victory = completion.Outcome.Outcome == StageOutcome.Victory;
            if (victory && _run.Outcome == RunOutcome.Victory)
            {
                // 엔딩 참조는 위 ValidateReferences가 이미 확인했습니다(View 단계 설계 W1).
                RunSummary summary = _run.LastSummary;
                var progress = completion.Progression;
                if (summary == null || summary.Outcome != RunOutcome.Victory ||
                    !ReferenceEquals(summary.Completion, _run.Completion) ||
                    summary.Completion.ChapterKey != progress.ChapterKey ||
                    summary.Completion.NodeId != progress.NodeId || summary.Completion.StageKey != progress.StageKey)
                    throw new InvalidOperationException("[CombatEndUI] 현재 전투와 일치하는 최종 승리 Run 요약이 필요합니다.");
                ending.Present(summary, confirmed);
                return;
            }
            BattleRewardReceipt reward = completion.Rewards;
            CharacterRewardReceipt[] rows = null;
            Sprite[] icons = null;
            RunSummary runSummary = null;
            string stage;
            // 모든 필수 데이터·자원을 검증한 뒤 표시 상태를 변경합니다.
            if (victory)
            {
                if (reward == null)
                    throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd: 승리 보상 영수증이 없습니다.");
                if (reward.ClearParticipantIds.Count > characters.Length)
                    throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd: 최초 배치 참가자가 4개 슬롯보다 많습니다.");
                rows = new CharacterRewardReceipt[reward.ClearParticipantIds.Count];
                icons = new Sprite[rows.Length];
                for (int i = 0; i < rows.Length; i++)
                {
                    foreach (CharacterRewardReceipt row in reward.Characters)
                        if (row.CharacterId == reward.ClearParticipantIds[i]) { rows[i] = row; break; }
                    if (rows[i] == null)
                        throw new InvalidOperationException($"[CombatEndUI] 참가자 {reward.ClearParticipantIds[i]}의 보상 행이 없습니다.");
                    icons[i] = _visuals.GetUnitVisual(rows[i].CharacterKey).InfoIcon;
                }
                stage = $"{reward.StageDepth}-{reward.StageWidthIndex + 1}";
            }
            else
            {
                stage = ReadStage(completion);
                runSummary = ReadRunSummary(completion);
            }

            ClearWidgets();
            victoryPanel.SetActive(victory);
            defeatPanel.SetActive(!victory);
            if (victory)
            {
                victoryStage.text = stage;
                victoryGold.SetText("+{0}", reward.TotalGoldEarned);
                victoryReputation.SetText("+{0}", reward.ClearFame);
                for (int i = 0; i < rows.Length; i++) characters[i].Show(rows[i], icons[i], experienceSegmentSeconds);
            }
            else
            {
                result.text = completion.Outcome.Reason == StageOutcomeReason.VoluntaryAbandonment ? "전투 포기" : "패배";
                defeatStage.text = stage;
                // 획득 명성·소비 골드는 이 패배로 끝난 Run의 요약 값입니다(Run 요약 설계 §10). 명성 잔액이 아니라 이번 Run 합계입니다.
                runReputation.SetText("+{0}", runSummary.FameEarned);
                runGoldSpent.SetText("{0}", runSummary.GoldSpent);
            }
            _confirmed = confirmed;
            confirmButton.interactable = true;
            confirmButton.gameObject.SetActive(true);
            _ui.OpenUI(ID, true);
        }

        private string ReadStage(BattleCompletionChanges completion)
        {
            foreach (StageProgressData stage in _stages.GetStages(_current.CurrentMapKey))
                if (stage.NodeId == completion.Progression.NodeId.Value && stage.StageKey == completion.Progression.StageKey.Value)
                    return $"{stage.Depth}-{stage.WidthIndex + 1}";
            throw new InvalidOperationException($"[CombatEndUI] 완료 노드 {completion.Progression.NodeId}의 스테이지 좌표가 없습니다.");
        }

        /// <summary>
        /// 패배·포기 완료는 같은 거래에서 Run 종료 영수증과 요약을 남깁니다. 마지막 요약이 이 완료의 영수증인지 확인합니다.
        /// </summary>
        private RunSummary ReadRunSummary(BattleCompletionChanges completion)
        {
            RunSummary summary = _run.LastSummary;
            if (completion.RunCompletion == null || summary == null || !ReferenceEquals(summary.Completion, completion.RunCompletion))
                throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd: 패배 완료의 Run 요약이 없습니다.");
            return summary;
        }

        internal void Dismiss()
        {
            if (ending != null) ending.Dismiss();
            // 최초 Install 전과 반복 정리에서는 아직 열린 화면이 없을 수 있습니다.
            if (!_shown && _confirmed == null) return;
            if (_ui == null) throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd의 UISystem 연결이 없습니다.");
            _ui.CloseUI(ID);
        }

        public override void Open()
        {
            ValidateReferences();
            if (_confirmed == null)
                throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd는 완료 데이터를 준비한 뒤 열어야 합니다.");
            _shown = true;
            gameObject.SetActive(true);
            // 실제 화면에서는 즉시 키보드/게임패드 Submit으로 확인할 수 있게 선택합니다.
            // EventSystem이 없는 격리 표시 시험에서는 선택만 생략합니다.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
        }

        public override void Hide()
        {
            _shown = false;
            _confirmed = null;
            ClearWidgets();
            confirmButton.interactable = false;
            ClearSelection();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _shown = false;
            _confirmed = null;
            ClearSelection();
            // 에디터에서 컴포넌트를 붙인 직후에는 아직 직렬화 참조가 연결되기 전일 수 있습니다.
            if (characters != null)
                for (int i = 0; i < characters.Length; i++)
                    if (characters[i] != null) characters[i].Clear();
        }

        private void ClearSelection()
        {
            EventSystem events = EventSystem.current;
            GameObject selected = events != null ? events.currentSelectedGameObject : null;
            if (selected != null && selected.transform.IsChildOf(transform)) events.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if (!_shown) return;
            float delta = Time.unscaledDeltaTime;
            for (int i = 0; i < characters.Length; i++) characters[i].Tick(delta);
        }

        private void ClearWidgets()
        {
            for (int i = 0; i < characters.Length; i++) characters[i].Clear();
            lastHit.SetActive(false);
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot != 0 || !_shown || !isActiveAndEnabled || !confirmButton.IsInteractable()) return;
            Action confirmed = _confirmed;
            if (confirmed == null) return;
            _confirmed = null;
            confirmButton.interactable = false;
            // 닫기는 실제 활성 변경을 UISystem 수명 안에서 수행합니다. 콜백은 보상 지급 없이 기존 Plan 확인만 합니다.
            _ui.CloseUI(ID);
            confirmed();
        }

        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        internal void ValidateReferences()
        {
            // 최종 승리는 이 결과 화면을 열지 않고 엔딩만 엽니다. 이 화면(평소 꺼진 결과 캔버스) 아래에 두면 엔딩의 부모가 꺼져 있어
            // 열 수 없으므로, 참조와 함께 자리도 확인합니다(View 단계 설계 W1). 다른 칸과 달리 "화면 안" 조건(Require)을 쓰지 않습니다.
            if (ending == null)
                throw new InvalidOperationException($"[CombatEndUI] '{name}' UIId.CombatEnd.{nameof(ending)} 참조가 없습니다.");
            if (ending.transform.IsChildOf(transform))
                throw new InvalidOperationException(
                    "[CombatEndUI] UIId.CombatEnd.ending은 결과 화면이 열리지 않는 최종 승리에서도 열리도록 결과 화면 밖의 항상 켜진 캔버스에 두어야 합니다.");
            // 엔딩이 열릴 때 보는 부모 계층 조건과 같습니다(EndingRunSummaryUI.Open). 엔딩 자신은 평소 꺼져 있는 것이 정상이라 부모 계층만 봅니다.
            Transform endingParent = ending.transform.parent;
            if (endingParent != null && !endingParent.gameObject.activeInHierarchy)
                throw new InvalidOperationException(
                    "[CombatEndUI] UIId.CombatEnd.ending의 부모 캔버스가 꺼져 있어 최종 승리 때 열 수 없습니다. 항상 켜진 캔버스에 두어야 합니다.");
            Require(victoryPanel, nameof(victoryPanel));
            Require(defeatPanel, nameof(defeatPanel));
            Require(victoryStage, nameof(victoryStage));
            Require(victoryGold, nameof(victoryGold));
            Require(victoryReputation, nameof(victoryReputation));
            Require(result, nameof(result));
            Require(defeatStage, nameof(defeatStage));
            Require(runReputation, nameof(runReputation));
            Require(runGoldSpent, nameof(runGoldSpent));
            Require(lastHit, nameof(lastHit));
            Require(confirmButton, nameof(confirmButton));
            if (characters == null || characters.Length != 4)
                throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd.characters는 정확히 4개여야 합니다.");
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] == null)
                    throw new InvalidOperationException($"[CombatEndUI] UIId.CombatEnd.characters[{i}] 참조가 없습니다.");
                if (!characters[i].transform.IsChildOf(victoryPanel.transform))
                    throw new InvalidOperationException($"[CombatEndUI] UIId.CombatEnd.characters[{i}]는 victoryPanel 내부에 있어야 합니다.");
                for (int j = 0; j < i; j++)
                    if (characters[i] == characters[j])
                        throw new InvalidOperationException($"[CombatEndUI] UIId.CombatEnd.characters[{i}]가 [{j}]와 중복됩니다.");
                characters[i].BindOwner(this);
                characters[i].ValidateReferences();
            }
            if (float.IsNaN(experienceSegmentSeconds) || float.IsInfinity(experienceSegmentSeconds) || experienceSegmentSeconds <= 0)
                throw new InvalidOperationException("[CombatEndUI] UIId.CombatEnd.experienceSegmentSeconds는 유한한 양수여야 합니다.");
        }

        private void Require(UnityEngine.Object reference, string field)
        {
            if (reference == null)
                throw new InvalidOperationException($"[CombatEndUI] '{name}' UIId.CombatEnd.{field} 참조가 없습니다.");
            Transform target = reference is GameObject go ? go.transform : ((Component)reference).transform;
            if (!target.IsChildOf(transform))
                throw new InvalidOperationException($"[CombatEndUI] '{name}' UIId.CombatEnd.{field}는 종료 화면 내부에 있어야 합니다.");
        }
    }
}
