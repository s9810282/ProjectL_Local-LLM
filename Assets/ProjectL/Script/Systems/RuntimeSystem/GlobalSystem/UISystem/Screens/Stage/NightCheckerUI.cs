using System;
using System.Globalization;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.ServiceKeys.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>이번 밤의 완료된 아군 턴 수와 현재 진영/준비 단계를 공개 Buffer에서 읽어 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class NightCheckerUI : UIBase
    {
        [FormerlySerializedAs("currDelay")]
        [SerializeField] private TMP_Text progressText;
        [Tooltip("NightClock/Fill Image. 이번 밤에 완료한 아군 턴 수 / 전체 밤 턴 수를 표시한다.")]
        [SerializeField] private Image progressFill;

        private ICombatStateBufferReadSink _combat;
        private long _shownDelay = -1;
        private StagePhase? _shownStagePhase;
        private CombatControllerPhase? _shownControllerPhase;

        public NightCheckerUI()
        {
            Policy = UIOpenPolicy.Parallel;
        }

        public override UIId ID => UIId.NightChecker;

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ValidateReferences();
            _combat = resolver.GetService(BufferSystemKey.Buffer).CombatStateReadSink;
            _shownDelay = -1;
            _shownStagePhase = null;
            _shownControllerPhase = null;
            Refresh();
        }

        public override void GetUIData(IServiceModule query) { }

        public override void Open()
        {
            ValidateReferences();
            gameObject.SetActive(true);
            Refresh();
        }

        public override void Hide() => gameObject.SetActive(false);

        private void Update() => Refresh();

        private void Refresh()
        {
            var battle = _combat != null && _combat.HasBattle ? _combat.Current : null;
            var turn = battle?.CommandState.Turn;
            StagePhase? stage = turn?.StagePhase;
            CombatControllerPhase? controller = turn?.ControllerPhase;
            // 낮밤 순환이 없어져(밤 삭제 설계 2026-11-10) 전투 중 완료한 플레이어 턴 수만 보입니다. 분모·진행 막대는 담당자가 다시 정합니다.
            long delay = turn != null && turn.StagePhase == StagePhase.Combat ? turn.CompletedPlayerTurnCount : 0;
            if (_shownDelay != delay ||
                _shownStagePhase != stage || _shownControllerPhase != controller)
            {
                progressText.text = DescribePhase(stage, controller) + " · " + delay.ToString(CultureInfo.InvariantCulture);
                progressFill.fillAmount = 0f;
                _shownDelay = delay;
                _shownStagePhase = stage;
                _shownControllerPhase = controller;
            }
        }

        private static string DescribePhase(StagePhase? stage, CombatControllerPhase? controller)
        {
            if (!stage.HasValue) return "전투 대기";
            if (stage == StagePhase.Deployment) return "배치";
            switch (controller)
            {
                case CombatControllerPhase.StartingPlayer: return "아군 준비";
                case CombatControllerPhase.Player: return "아군 턴";
                case CombatControllerPhase.StartingEnemy: return "적 준비";
                case CombatControllerPhase.Enemy: return "적 응답";
                default: return "전투 준비";
            }
        }

        private void ValidateReferences()
        {
            if (progressText == null)
                throw new InvalidOperationException("[NightCheckerUI] UIId.NightChecker의 progressText TMP Text 참조가 필요합니다.");
            if (progressFill == null)
                throw new InvalidOperationException($"[NightCheckerUI] UIId.{ID} '{name}': progressFill에 NightClock/Fill의 Image 참조가 필요합니다.");
            if (progressFill.type != Image.Type.Filled)
                throw new InvalidOperationException($"[NightCheckerUI] UIId.{ID} '{name}': progressFill '{progressFill.name}'의 Image Type은 Filled여야 합니다.");
        }
    }
}
