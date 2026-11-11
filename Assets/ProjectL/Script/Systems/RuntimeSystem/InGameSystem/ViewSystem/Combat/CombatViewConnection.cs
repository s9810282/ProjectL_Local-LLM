using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 씬 연결 한 번의 상태입니다(View 3라운드 W11-4). 표시, 진입 연출, 선택 유닛 준비, 입력 표면, 연결 입력 묶음, 정보창 창구,
    /// 선택 읽기, 덱 호버, 씬 루트 구독, 첫 그리기 성공, 연결한 전투 번호를 한 객체에 둡니다. 연결이 없으면 조정자는 이 객체를 갖지 않습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 조정자가 표시를 인수하는 순간(<c>TakeDisplay</c>의 마지막 문장, 소유권 이전) 만들고, <c>Detach</c>가 정리 순서대로 비운 뒤 버립니다(계획 TP-23).
    /// 정리 순서(재생 끊기·입력 해제·연출 끊기·표시 반환)는 조정자가 부르고, 이 객체는 자기 몫(루트 구독·입력 정지)만 합니다(설계 P24).
    /// </para>
    /// <para>
    /// 씬 루트는 입력 표면 하나가 갖습니다. 구독을 풀 때도 표면의 루트를 씁니다(2라운드가 남긴 "씬 루트 두 필드" 해소).
    /// 조정자 안에서 쓰고, 조정자가 나눈 입력 분배·결과 소비는 필드로 갖지 않고 매 호출 인자로 받습니다(설계 P25, 조정자 나누기 설계 CS1).
    /// </para>
    /// </remarks>
    internal sealed class CombatViewConnection
    {
        private Action _focusLost;
        private Action _disabled;

        /// <param name="display">인수한 표시입니다.</param>
        /// <param name="battleInstance">연결한 전투 번호입니다. 다른 전투로 바뀌었는지 판단에 씁니다.</param>
        internal CombatViewConnection(ICombatBattleDisplay display, long battleInstance,
            ICombatEntryPresentation entryPresentation = null, ILodingPreparation preparation = null)
        {
            Display = display ?? throw new ArgumentNullException(nameof(display));
            BattleInstance = battleInstance;
            EntryPresentation = entryPresentation;
            Preparation = preparation;
            // 입력 표면은 표시와 함께 만들고 함께 놓는다(View 2라운드 W6-2). 생성은 예외가 없어 소유권 이전이 마지막 실패 지점으로 남는다.
            Surfaces = new CombatInputSurfaces(display.Actions);
        }

        /// <summary>인수한 표시입니다.</summary>
        internal ICombatBattleDisplay Display { get; }

        /// <summary>표시가 소유하는 진입 연출입니다. 운영 표시는 늘 넘깁니다(없으면 null).</summary>
        internal ICombatEntryPresentation EntryPresentation { get; }
        internal ILodingPreparation Preparation { get; }
        // 취소/실패가 입력 허용으로 바뀌지 않습니다. 해당 연결은 오류 경계와 Detach가 정리합니다.
        internal bool IsPreparationPending => Preparation != null && Preparation.State != LodingPreparationState.Complete;

        /// <summary>행동 버튼·BattleChanger·가방 맞춤입니다. 표시와 함께 만들고 함께 놓습니다.</summary>
        internal CombatInputSurfaces Surfaces { get; }

        /// <summary>연결한 전투 번호입니다.</summary>
        internal long BattleInstance { get; }

        /// <summary>
        /// 첫 그리기(Install)와 입력 객체 생성이 성공했는지입니다. 입력 허용 판단(facts)의 "최초 표시 뒤"가 이 값이고(개선 설계 D13),
        /// 거짓이면 Tick이 미리보기·결과를 소비하지 않습니다(구조 개선 SV1·SV4).
        /// </summary>
        internal bool Shown { get; private set; }

        /// <summary>선택 누름 입구입니다. 첫 표시 전에 명시적으로 연결합니다.</summary>
        internal ICombatUnitInspection Inspection { get; private set; }

        /// <summary>표시 소유 선택의 읽기 참조입니다. 연결은 별도 선택 원본을 만들지 않습니다.</summary>
        internal ICombatSelectionReader Selection { get; private set; }

        /// <summary>손패 카드 호버 입구입니다. 표시를 인수할 때 만들어 첫 그리기 전에도 있습니다(View 2라운드 TP-6). 씬 루트가 없으면 null입니다.</summary>
        internal CombatDeckHoverInput DeckHover { get; private set; }

        /// <summary>연결 입력 묶음입니다. 첫 그리기 성공 뒤 생기고, 입력 정지 뒤에도 남습니다(분리 때 함께 버림).</summary>
        internal CombatSceneInput Input { get; private set; }

        /// <summary>
        /// 표시를 인수한 뒤의 연결 재료입니다. 첫 그리기 전에 둡니다.
        /// 생성자에서 받지 않는 것은 덱 호버 입구를 인수 뒤에 만들기 위해서입니다. 생성자는 예외가 없어야 소유권 이전(생성 대입)이 마지막 실패 지점으로 남습니다(계획 TP-23).
        /// </summary>
        internal void BeginUse(ICombatUnitInspection inspection, ICombatSelectionReader selection, CombatDeckHoverInput deckHover)
        {
            Inspection = inspection;
            Selection = selection;
            DeckHover = deckHover;
        }

        /// <summary>
        /// 연결 입력 묶음을 받고 유닛 HUD 아이템 아이콘 우클릭 해제를 겁니다(가방 설계 P6). 묶음 생성이 모두 끝난 뒤 부릅니다.
        /// 생성이 실패하면 이것을 부르지 않으므로 풀 수 없는 연결을 남기지 않습니다(2라운드 계획 검증 L11).
        /// </summary>
        internal void AttachInput(CombatSceneInput input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Display.BindItemUnequip(input.ItemInput.RequestUnequip);
        }

        /// <summary>첫 그리기와 입력 객체 생성이 성공했습니다.</summary>
        internal void MarkShown() => Shown = true;

        /// <summary>
        /// 씬 루트에 연결합니다: 입력 표면의 BattleChanger 입력 연결, 포커스 상실·비활성화 구독. 첫 그리기가 끝나고 표시가 아직 연결돼 있을 때 부릅니다.
        /// </summary>
        internal void BindRoot(CombatSceneRoot root, IStageInputReceiver input, Action focusLost, Action disabled)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            _focusLost = focusLost ?? throw new ArgumentNullException(nameof(focusLost));
            _disabled = disabled ?? throw new ArgumentNullException(nameof(disabled));
            Surfaces.BindRoot(root, input);
            root.FocusLost += _focusLost;
            root.Disabled += _disabled;
        }

        /// <summary>
        /// 씬 루트 연결을 풉니다: 입력 표면(BattleChanger 입력·가방) → 구독 해제 순서입니다. 루트가 없으면 아무것도 하지 않습니다.
        /// 분리의 맨 앞(확인창 닫기 다음)에서 부릅니다(구조 개선 SV1, 지침 §20·21).
        /// 루트는 표면이 갖고(<see cref="CombatInputSurfaces.Root"/>), 표면은 루트가 있을 때만 묶으므로 구독한 루트와 같습니다.
        /// </summary>
        internal void UnbindRoot()
        {
            CombatSceneRoot root = Surfaces.Root;
            try
            {
                Surfaces.UnbindRoot();
            }
            finally
            {
                // 표면 풀기가 실패해도 루트 구독은 풉니다(평가 C9-01). 정상일 때 순서는 같습니다.
                if (root != null)
                {
                    root.FocusLost -= _focusLost;
                    root.Disabled -= _disabled;
                    _focusLost = null;
                    _disabled = null;
                }
            }
        }

        /// <summary>
        /// Drag·모드·로컬 대기를 끝내고 고스트·원본 숨김·미리보기를 정리합니다(옛 조정자 <c>StopInteraction</c> 순서).
        /// 덱 호버 → HUD 아이템 해제 연결 → 연결 입력 묶음 정지입니다. 실패는 모으고 나머지를 이어 갑니다. 명령 Buffer는 보존합니다.
        /// </summary>
        internal void StopInput(ref List<Exception> errors)
        {
            if (DeckHover != null) CleanupErrors.Try(DeckHover.Release, ref errors);
            DeckHover = null;
            // 묶음 정지가 아이템 입구를 비우므로, 이미 정지했으면(오류 정지 뒤 분리) 해제 연결을 다시 풀지 않는다.
            if (Input?.ItemInput != null) CleanupErrors.Try(UnbindItemUnequip, ref errors);
            Input?.Stop(ref errors);
        }

        /// <summary>HUD 아이템 아이콘 우클릭 해제 입구를 풉니다. 정리 하나(<see cref="Action"/>)로 넘기려고 메서드로 둡니다.</summary>
        private void UnbindItemUnequip() => Display.BindItemUnequip(null);
    }
}
