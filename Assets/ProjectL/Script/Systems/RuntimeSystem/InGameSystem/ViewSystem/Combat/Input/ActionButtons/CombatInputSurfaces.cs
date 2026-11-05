using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 입력 표면(행동 버튼·BattleChanger·가방) 맞춤입니다(View 2라운드 W6-2). 세 표면이 같은 막힘 판단(<see cref="CombatInputPolicy.IsInputBlocked"/>) 하나를 씁니다.
    /// 연결 수명 객체입니다(계획 검증 M5): 연결 상태(<c>CombatViewConnection</c>)가 표시와 함께 만들고, 씬 루트는 구독할 때 <see cref="BindRoot"/>로 받습니다.
    /// </summary>
    /// <remarks>
    /// 씬 없이 조립하는 시험은 루트가 없어 BattleChanger·가방 갈래를 건너뜁니다. 두 UI의 서명은 GlobalSystem 쪽이라 그대로 두고 값만 넘깁니다(1라운드 P2).
    /// 배치 확인창 맞춤은 배치 진입(연결 입력 묶음이 가짐)의 일이라 조정자가 이것보다 먼저 부릅니다.
    /// </remarks>
    internal sealed class CombatInputSurfaces
    {
        private readonly ICombatActionDisplay _actions;
        private CombatSceneRoot _root;

        internal CombatInputSurfaces(ICombatActionDisplay actions)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        /// <summary>
        /// 묶인 씬 루트입니다. 연결 상태(<c>CombatViewConnection</c>)는 루트를 따로 두지 않고 구독을 풀 때 이것을 씁니다(View 3라운드 W11-4).
        /// 묶기 전·푼 뒤에는 null입니다.
        /// </summary>
        internal CombatSceneRoot Root => _root;

        /// <summary>씬 루트의 BattleChanger에 입력 입구를 연결하고 그 루트의 표면을 맞추기 시작합니다. 루트가 없으면 아무것도 하지 않습니다.</summary>
        internal void BindRoot(CombatSceneRoot root, IStageInputReceiver input)
        {
            if (root == null)
                return;
            _root = root;
            // 실제 씬은 Root.ValidateReferences로 필수 연결을 검사한다. 가짜 Display 테스트의 루트에는 UI가 없다.
            root.BattleChanger?.BindInput(input);
        }

        /// <summary>BattleChanger 입력 연결을 끊고 가방을 숨긴 뒤 루트를 놓습니다. 루트가 없으면 아무것도 하지 않습니다.</summary>
        internal void UnbindRoot()
        {
            if (_root == null)
                return;
            _root.BattleChanger?.UnbindInput();
            _root.Pack?.Refresh(default, true);
            _root.UnitInfo?.SetActionAvailability(null);
            _root = null;
        }

        /// <summary>
        /// 세 표면을 지금 사실로 맞춥니다(행동 버튼 설계 B3·B6). 판단은 Policy와 공격 진입 판단의 결과를 읽기만 하고, 패널은 값이 바뀔 때만 씁니다.
        /// </summary>
        /// <param name="facts">턴 종료 판단까지 읽은 사실입니다(선행 미처리 명령 포함, Router의 EndTurn과 같은 사실).</param>
        internal void Sync(in CombatInputFacts facts, bool canEnterAttack, bool isAttackActive, bool canUseReturn = false,
            CombatActionAvailability availability = null)
        {
            bool blocked = CombatInputPolicy.IsInputBlocked(facts);
            _root?.BattleChanger?.Refresh(facts, blocked);
            _root?.Pack?.Refresh(facts, blocked);
            _root?.UnitInfo?.SetActionAvailability(blocked ? null : availability,
                CombatInputPolicy.AllowsTutorialCommand(facts, CombatViewCommandKind.Move),
                CombatInputPolicy.AllowsTutorialCommand(facts, CombatViewCommandKind.Attack));
            _actions.SetActionsAvailable(CombatActionButtons.Read(facts, canEnterAttack, isAttackActive, canUseReturn));
        }

        /// <summary>행동 버튼을 모두 숨기고 BattleChanger·가방도 막힌 상태로 둡니다. 정지·입력 닫힘이 부릅니다.</summary>
        internal void Hide()
        {
            _actions.SetActionsAvailable(default);
            _root?.BattleChanger?.Refresh(default, true);
            _root?.Pack?.Refresh(default, true);
            _root?.UnitInfo?.SetActionAvailability(null);
        }
    }
}
