using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 전투 입력 기능(배치 Drag·공격 조준·이동 경로·가방 아이템·유닛 호버 공격 범위·플레이어 스킬 모드)을 묶는 조합 객체입니다(구조 개선 VR3·SV4).
    /// 포인터 대상(<see cref="CombatInputRouter.PointerTarget"/>)은 <see cref="ResolvePointerTarget"/> 한 곳이 소유자의 실시간 상태로 정합니다(평가 C4-13).
    /// <see cref="CombatInputRouter"/>가 그 대상과 함께 넘긴 포인터 입력은 그 대상을 받는 소유자에게 그대로 넘깁니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 조정자(CombatViewSystem)는 개별 입력 기능을 모릅니다. 연결 입력 묶음(<see cref="CombatSceneInput"/>)의 입구로 이 객체의 수명 메서드(Tick·결과 전·결과 후·확인 뒤·취소·정지)를 부릅니다(평가 C8-01).
    /// 기능마다 공통 수명 계약 <see cref="CombatInputOwner"/>의 어댑터가 있고, 수명 메서드는 소유자 목록을 생성자에서 정한 순서대로 돕니다(View 2라운드 W5-1).
    /// 순서: 공격 Drag 넘겨받기 → 경로 → 아이템 → 배치 → 공격 → 호버. 경로가 호버보다 먼저라서 한 슬롯의 배경 요청은 범위 → 복귀 견적 → 호버 순서로 답을 받습니다(<see cref="CombatPreviewSlotRule"/>).
    /// 이 객체는 상태·정책을 갖지 않습니다. 기능 상태의 원본은 각 기능 객체입니다.
    /// </para>
    /// <para>
    /// 전경 전환 규칙은 <see cref="BeginForeground"/> 하나입니다. 덱 카드 집기·아이템 집기·공격 모드 진입은 들어갈 수 있을 때만,
    /// 다음 소유자 밖의 표현을 정리하고 배경 소유자를 내려놓은 뒤 다음 소유자를 활성화합니다.
    /// 시작/턴 종료 제출과 대기는 Router와 <see cref="CombatCommandGate"/>가 맡습니다. Gate는 입력 Factory가 연결 입력 묶음(<see cref="CombatSceneInput"/>)과 함께 만들고
    /// 배치·공격 객체에 연결합니다. 이 객체는 Gate를 모릅니다.
    /// </para>
    /// <para>
    /// 경로 입력은 정지(<see cref="Stop"/>) 때 연결을 끊고, 이름 입구용 참조를 놓습니다. 목록의 경로 어댑터는 끊긴 경로를 다시 부르지 않습니다(<see cref="CombatPathInput.IsDetached"/>).
    /// 정지 뒤 경로 위임은 아무것도 하지 않습니다. 기능 여섯은 모두 생성 때 필수입니다(평가 C5-02).
    /// </para>
    /// </remarks>
    internal sealed class CombatInteraction
    {
        internal enum DragKind
        {
            None = 0,
            /// <summary>필드에서 집거나 유지 덱 카드로 고른 아군 유닛을 칸에 놓습니다(배치 단계).</summary>
            Placement = 1,
            /// <summary>가방 아이템을 유닛에 놓아 장착합니다(배치 단계, 가방 설계 P4).</summary>
            Item = 2,
        }

        internal enum TargetMode
        {
            None = 0,
            Attack = 2
        }

        private readonly PlacementDragInteraction _placement;
        private readonly AttackTargetingInteraction _attack;
        // 이동 경로·복귀 입력입니다. 이름 입구(복귀·취소·이동 모드 끝)와 읽기 멤버가 씁니다.
        // 생성 때는 필수지만 정지 때 Detach하고 null로 두므로, 쓰는 곳 셋은 null 갈래를 남깁니다(F2, 평가 C5-02 P1).
        private CombatPathInput _path;
        // 공격 모드에서 시작한 Drag를 경로로 넘기는 소유자입니다(View 2라운드 W5-5).
        private readonly CombatAttackDragHandover _handover;
        // 가방 아이템 드래그입니다.
        private readonly CombatItemDragInteraction _item;
        // 플레이어 액티브 스킬 모드입니다(전투 스킬 View 설계 SK1). 전경 소유자이고 포인터 대상은 Skill입니다.
        private readonly CombatPlayerSkillInteraction _skill;
        // 수명 메서드가 도는 소유자 목록입니다(View 2라운드 W5-1). 순서는 생성자 한 곳이 정합니다.
        private readonly CombatInputOwner[] _owners;
        // 포인터 대상마다 그 대상을 받는 소유자입니다. 대상 하나에 소유자는 하나입니다(P10).
        private readonly Dictionary<CombatInputRouter.PointerTarget, CombatInputOwner> _byTarget;

        /// <summary>
        /// 이미 만든 입력 기능 객체를 묶습니다. 기능 객체 생성과 의존성 연결은 <c>CombatSceneInputFactory</c>가 합니다(구조 개선 SV5).
        /// 받은 기능의 소유자로 목록과 대상 표를 만듭니다. 같은 포인터 대상을 받는 소유자가 둘이면 예외입니다(P10).
        /// </summary>
        /// <param name="path">이동 경로 입력입니다. 필수이고, 정지 뒤에는 놓습니다.</param>
        /// <param name="attackDrag">공격 Drag 몸짓입니다. 경로로 넘기는 소유자를 만들어 목록 맨 앞에 둡니다(View 2라운드 W5-5).</param>
        /// <param name="item">가방 아이템 드래그입니다(가방 설계 P4).</param>
        /// <param name="rangeHover">유닛 호버 공격 범위입니다(호버 공격 범위 설계 UR6). 배경 소유자는 이것 하나입니다.</param>
        /// <param name="skill">플레이어 액티브 스킬 모드입니다(전투 스킬 View 설계 SK1).</param>
        internal CombatInteraction(PlacementDragInteraction placement, AttackTargetingInteraction attack, CombatPathInput path,
            CombatAttackDragGesture attackDrag, CombatItemDragInteraction item, CombatUnitRangeHover rangeHover, CombatPlayerSkillInteraction skill)
        {
            _placement = placement ?? throw new ArgumentNullException(nameof(placement));
            _attack = attack ?? throw new ArgumentNullException(nameof(attack));
            // 운영 조립(CombatSceneInputFactory)은 여섯 기능을 늘 넘깁니다(평가 C5-02). 빠진 인자는 넘겨받기를 만들기 전에 이름으로 알립니다.
            _path = path ?? throw new ArgumentNullException(nameof(path));
            if (attackDrag == null)
                throw new ArgumentNullException(nameof(attackDrag));
            _item = item ?? throw new ArgumentNullException(nameof(item));
            if (rangeHover == null)
                throw new ArgumentNullException(nameof(rangeHover));
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
            _handover = new CombatAttackDragHandover(attackDrag, path, attack);

            // 목록 순서(TP-2): 넘겨받기 → 경로 → 아이템 → 배치 → 공격 → 스킬 → 호버.
            _owners = new[]
            {
                _handover.InputOwner, path.InputOwner, item.InputOwner, placement.InputOwner, attack.InputOwner, skill.InputOwner,
                rangeHover.InputOwner,
            };

            _byTarget = new Dictionary<CombatInputRouter.PointerTarget, CombatInputOwner>();
            for (int i = 0; i < _owners.Length; i++)
            {
                CombatInputOwner owner = _owners[i];
                if (owner.Target == CombatInputRouter.PointerTarget.None)
                    continue;
                if (_byTarget.ContainsKey(owner.Target))
                    throw new InvalidOperationException($"[CombatInteraction] 포인터 대상 {owner.Target}을 받는 입력 소유자가 둘입니다.");
                _byTarget.Add(owner.Target, owner);
            }
        }

        /// <summary>가방 아이템을 끌고 있는지입니다(읽기 전용, 아이템 소유자 값). 포인터 대상 판정이 읽습니다.</summary>
        internal bool IsItemDragActive => _item.IsDragging;

        /// <summary>공격 모드인지입니다. 입력 대상 판정이 읽습니다.</summary>
        internal bool IsAttackActive => _attack.IsActive;

        /// <summary>플레이어 액티브 스킬 모드인지입니다. 입력 대상 판정과 행동 버튼이 읽습니다.</summary>
        internal bool IsSkillActive => _skill.IsActive;

        /// <summary>
        /// 지금 포인터 입력을 받을 대상입니다(평가 C4-13). 판정식(<see cref="CombatInputRouter.ResolvePointerTarget"/>)에 소유자의 실시간 상태
        /// (공격 모드·아이템 드래그·스킬 모드)를 넘기는 유일한 곳입니다. Router와 감시 단계가 부르고, 입력 사실 읽기가 같은 값을
        /// <see cref="CombatInputFacts.PointerTarget"/>에 담습니다. 모드가 있는 소유자를 더하면 여기만 고칩니다.
        /// </summary>
        internal CombatInputRouter.PointerTarget ResolvePointerTarget(InputLayer layer) =>
            CombatInputRouter.ResolvePointerTarget(layer, IsAttackActive, IsItemDragActive, IsSkillActive);

        /// <summary>배치 Drag 상태입니다(읽기 전용, 배치 소유자 값).</summary>
        internal DragKind Drag => _placement.IsDragging ? DragKind.Placement : IsItemDragActive ? DragKind.Item : DragKind.None;

        /// <summary>조준 모드입니다(읽기 전용, 공격 소유자 값).</summary>
        internal TargetMode Mode => _attack.IsActive ? TargetMode.Attack : TargetMode.None;

        /// <summary>공격 범위 응답을 받았는지입니다(읽기 전용, 공격 소유자 값).</summary>
        internal bool HasRangeAnswer => _attack.HasRangeAnswer;

        /// <summary>현재 전투 정체성에 맞는 빈 이동 범위 근거입니다. 소유자인 경로 입력에 위임합니다.</summary>
        internal bool HasCurrentAcceptedEmptyMovementRange(EntityId selectedActor) =>
            _path != null && _path.HasCurrentAcceptedEmptyMovementRange(selectedActor);

        /// <summary>
        /// 전경 소유자 하나가 포인터를 쓰고 있는지입니다: 배치·아이템 Drag 중, 공격 모드, 경로 지정 중. 각 소유자의 상태를 읽기만 합니다.
        /// 커서 스냅(<see cref="IsCursorSnapMode"/>)과 배경 소유자의 내려놓기가 이 한 식을 씁니다.
        /// </summary>
        internal bool IsInputOwnerActive
        {
            get
            {
                for (int i = 0; i < _owners.Length; i++)
                {
                    if (!_owners[i].IsBackground && _owners[i].IsActive)
                        return true;
                }
                return false;
            }
        }

        /// <summary>클릭 후보의 견적도 배경 조회보다 우선합니다.</summary>
        internal bool HasForegroundPreview => IsInputOwnerActive || (_path?.HasClickCandidate ?? false);

        internal bool TryPressDestination(CellCoord? cell, bool occupied) =>
            _path != null && _path.TryPressDestination(cell, occupied);

        internal void ReleaseDestination(CellCoord? cell) => _path?.ReleaseDestination(cell);

        /// <summary>커서를 칸에 스냅할 모드인지입니다(커서 그리드 스냅 설계 C2). 입력 소유자가 포인터를 쓰는 동안입니다.</summary>
        internal bool IsCursorSnapMode => IsInputOwnerActive;

        /// <summary>호버 공격 범위가 보여도 되는 입력 대상인지입니다: 배치·경로 대상이고 다른 입력 소유자가 비활성입니다.</summary>
        private bool IsRangeHoverTarget(CombatInputRouter.PointerTarget target) =>
            (target == CombatInputRouter.PointerTarget.Placement || target == CombatInputRouter.PointerTarget.Path)
            && !HasForegroundPreview;

        internal bool CanSelectDeckUnit(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CharacterInstanceId ownedId) =>
            _placement.CanSelectDeckUnit(facts, characters, ownedId);

        /// <summary>손패 카드를 집습니다. 집을 수 있을 때만 다른 소유자의 표현(아이템 드래그 등)을 먼저 정리합니다(배타, 가방 설계 P4).</summary>
        internal void SelectDeckUnit(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CharacterInstanceId ownedId)
        {
            if (!_placement.CanSelectDeckUnit(facts, characters, ownedId))
                return;
            BeginForeground(_placement.InputOwner);
            _placement.SelectDeckUnit(facts, characters, ownedId);
        }

        internal void CancelDeckSelection() => _placement.CancelDeckSelection();

        internal bool CanSelectItem(in CombatInputFacts facts, int itemKey) =>
            !_placement.IsDragging && _item.CanSelect(facts, itemKey);

        /// <summary>가방 아이템을 집습니다. 집을 수 있을 때만 다른 소유자의 표현(배치 호버·고스트 등)을 먼저 정리합니다.</summary>
        internal void SelectItem(in CombatInputFacts facts, int itemKey)
        {
            if (!CanSelectItem(facts, itemKey))
                return;
            BeginForeground(_item.InputOwner);
            _item.Select(facts, itemKey);
        }

        internal void CancelItemSelection() => _item.Cancel();

        /// <summary>HUD 아이템 아이콘 우클릭 해제입니다(가방 설계 P6). 제출했으면 true입니다.</summary>
        internal bool RequestItemUnequip(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, EntityId unitId) =>
            _item.RequestUnequip(facts, characters, unitId);

        /// <summary>
        /// 배치 Layer에서 마지막으로 받은 포인터 위치입니다(가방 설계 P6). 우클릭 취소에는 좌표가 없어 Router가 UI 위인지 볼 때 읽습니다.
        /// 받은 적이 없으면 false입니다.
        /// </summary>
        internal bool TryGetLastPlacementPointer(out float screenX, out float screenY) =>
            _placement.TryGetLastPointer(out screenX, out screenY);

        #region 대상 지정 입력

        /// <summary>
        /// 새 공격 Drag가 소유한 입력만 먼저 처리합니다. 클릭과 소유권 없는 release는 기존 배분으로 보냅니다.
        /// 넘겨받기 소유자에 위임합니다(View 2라운드 W5-5).
        /// </summary>
        internal bool TryHandleAttackDragPointer(CombatInputRouter.Request request, float screenX, float screenY, bool blockedByUi) =>
            _handover.TryHandlePointer(request, screenX, screenY, blockedByUi);

        internal void PointerMoved(CombatInputRouter.PointerTarget target, in CombatInputFacts facts,
            IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            var input = new CombatPointerInput(facts, characters, screenX, screenY);
            OwnerOf(target)?.PointerMoved(input);
            // 소유자가 이 위치를 먼저 처리한 뒤 배경(호버 범위)을 맞춘다. 다른 소유자가 포인터를 쓰는 동안에는 내려놓기만 한다.
            bool canShow = IsRangeHoverTarget(target);
            for (int i = 0; i < _owners.Length; i++)
            {
                if (_owners[i].IsBackground)
                    _owners[i].ObservePointer(input, canShow);
            }
        }

        internal void SelectPressed(CombatInputRouter.PointerTarget target, in CombatInputFacts facts,
            IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            // 누를 때 할 일이 있는 것은 배치·경로뿐이다(공격 조준 제출은 뗄 때).
            var input = new CombatPointerInput(facts, characters, screenX, screenY);
            OwnerOf(target)?.SelectPressed(input);
            // 누름이 Drag·경로 지정을 시작했으면 배경(호버 범위)을 내려놓는다(그 소유자가 슬롯과 범위 층을 쓴다).
            if (IsInputOwnerActive)
                YieldBackground();
        }

        internal void SelectReleased(CombatInputRouter.PointerTarget target, in CombatInputFacts facts,
            IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            var input = new CombatPointerInput(facts, characters, screenX, screenY);
            OwnerOf(target)?.SelectReleased(input);
        }

        /// <summary>포인터 대상을 받는 소유자입니다. 대상이 None이면 null입니다.</summary>
        private CombatInputOwner OwnerOf(CombatInputRouter.PointerTarget target) =>
            _byTarget.TryGetValue(target, out CombatInputOwner owner) ? owner : null;

        #endregion

        #region 정리·전환

        /// <summary>
        /// 선택이 강제로 끝났습니다(Layer 전환·Unbind 등). 모든 소유자에 목록 순서로 알립니다: 공격 Drag 넘겨받기 무효화 → 경로 입력 취소 → 아이템·배치 Drag 취소 → 유닛 호버 지우기(공격 조준은 할 일 없음). 명령은 기록하지 않습니다.
        /// </summary>
        internal void SelectCanceled()
        {
            for (int i = 0; i < _owners.Length; i++)
                _owners[i].SelectCanceled();
        }

        /// <summary>
        /// 취소 키: Drag 중이면 Drag를, 아니면 공격 모드를 끝낸 뒤 경로를 취소합니다(경로는 취소 뒤 복귀 견적을 다시 요청).
        /// 공격 모드가 먼저 끝나야 견적이 같은 호출에서 다시 요청됩니다. 대기는 지우지 않습니다.
        /// </summary>
        internal void Cancel(IReadOnlyList<CharacterData> characters)
        {
            CancelDragOrAttack(characters);
            _path?.Cancel();
        }

        /// <summary>
        /// 배치 단계 우클릭: Drag 중이 아니면 포인터 아래 필드 일반 유닛을 손패로 되돌립니다. 대상이 있어 제출을 시도했으면 true입니다.
        /// 배치 Layer일 때만 부릅니다(Router의 포인터 대상 판정).
        /// </summary>
        internal bool TryReturnPlacedUnit(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
            _placement.TryReturnUnderPointer(facts, characters);

        /// <summary>
        /// 공격 버튼의 끄기: Drag 중이면 Drag를, 아니면 공격 모드만 끝냅니다. 경로는 건드리지 않습니다(행동 버튼 설계 A2).
        /// </summary>
        internal void CancelDragOrAttack(IReadOnlyList<CharacterData> characters)
        {
            _handover.Invalidate();
            if (IsItemDragActive)
                _item.Cancel();
            else if (_placement.IsDragging)
                _placement.Cancel();
            else
                _attack.Cancel(characters);
        }

        /// <summary>
        /// 이동 모드를 끝냅니다: 턴 종료 제출과 공격 모드 진입 전에 경로 입력을 취소하고 이동 모드 범위도 지웁니다(이동 모드 범위 설계 MR3).
        /// <see cref="CancelInputs"/>는 이동 모드가 이어지면 범위를 남기므로, 범위까지 지우는 쪽은 이것입니다(경로 입력 분리 설계 Q-R4).
        /// </summary>
        internal void LeaveMoveMode()
        {
            _handover.Invalidate();
            _path?.LeaveMoveMode();
        }

        /// <summary>
        /// 지금 공격 모드에 들어갈 수 있는지입니다(읽기 전용). <see cref="AttackModeRequested"/>가 쓰는 판단과 같으며,
        /// 행동 버튼의 누름 가능 여부가 읽습니다(행동 버튼 설계 B3).
        /// </summary>
        internal bool CanEnterAttack(in CombatInputFacts facts) => _attack.CanEnter(facts, out _);

        /// <summary>
        /// 공격 모드 진입: 들어갈 수 있을 때만 다른 소유자의 표현(배치 쪽 표시·요청)과 배경(호버 공격 범위)을 먼저 정리하고, 공격 쪽이 범위를 요청합니다.
        /// 호버 범위와 공격 범위는 함께 보이지 않으며 그 배타는 <see cref="BeginForeground"/>가 정합니다(호버 공격 범위 설계 UR1).
        /// 두 범위는 층이 달라(View 2라운드 W5-6) 순서가 바뀌어도 서로의 칸을 지우지 않습니다.
        /// 들어갈 수 없으면 공격 Drag 넘겨받기 몸짓만 끝내고 나머지는 바꾸지 않습니다. 경로 취소는 호출자가 먼저 합니다(<see cref="LeaveMoveMode"/>).
        /// </summary>
        internal void AttackModeRequested(in CombatInputFacts facts)
        {
            // 들어갈 수 없어도 넘겨받기 몸짓은 끝낸다(옛 묶음과 같이 진입 판단 앞, 계획 검증 F10).
            _handover.Invalidate();
            if (!_attack.CanEnter(facts, out EntityId actor))
            {
                _attack.NotifyEntryRejected(facts);
                return;
            }

            BeginForeground(_attack.InputOwner);
            _attack.Enter(facts, actor);
        }

        /// <summary>지금 스킬 모드에 들어갈 수 있는지입니다(읽기 전용). 진입(<see cref="SkillModeRequested"/>)과 같은 판단입니다.</summary>
        internal bool CanEnterSkill(in CombatInputFacts facts) => _skill.CanEnter(facts, out _);

        /// <summary>
        /// 스킬 모드 진입입니다(전투 스킬 View 설계 SK1). 들어갈 수 있을 때만 다른 소유자의 표현과 배경을 정리하고 스킬 모드가 가용성을 묻습니다.
        /// 공격 모드 중이면 공격 모드가 끝나고 스킬 모드가 됩니다(전경 배타). 경로 취소는 호출자가 먼저 합니다(<see cref="LeaveMoveMode"/>).
        /// </summary>
        internal void SkillModeRequested(in CombatInputFacts facts)
        {
            _handover.Invalidate();
            if (!_skill.CanEnter(facts, out EntityId actor))
            {
                _skill.NotifyEntryRejected(facts);
                return;
            }

            BeginForeground(_skill.InputOwner);
            _skill.Enter(facts, actor);
        }

        /// <summary>스킬 모드를 끝냅니다(R·스킬 버튼의 끄기).</summary>
        internal void CancelSkill(IReadOnlyList<CharacterData> characters) => _skill.Cancel(characters);

        /// <summary>스킬 모드의 우클릭입니다: 마지막 점을 빼고, 점이 없으면 모드를 끝냅니다(설계 SK1).</summary>
        internal void SkillBack(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) => _skill.Back(facts, characters);

        /// <summary>
        /// 전경 소유자 하나가 활성이 되기 전의 규칙 하나입니다(View 2라운드 W5-1·W5-6). 배경 소유자는 내려놓고,
        /// 다음 소유자가 아닌 전경 소유자는 표현을 정리합니다(경로는 정리할 표현이 없음 — 경로 취소는 호출자가 합니다).
        /// 그때 활성이 아닌 소유자는 할 일이 없거나 세대 번호만 올립니다.
        /// </summary>
        private void BeginForeground(CombatInputOwner next)
        {
            for (int i = 0; i < _owners.Length; i++)
            {
                CombatInputOwner owner = _owners[i];
                if (owner.IsBackground)
                    owner.YieldToForeground();
                else if (!ReferenceEquals(owner, next))
                    owner.CancelPresentation();
            }
        }

        /// <summary>배경 소유자만 내려놓습니다. 누름이 전경 소유자를 활성으로 만들었을 때 씁니다.</summary>
        private void YieldBackground()
        {
            for (int i = 0; i < _owners.Length; i++)
            {
                if (_owners[i].IsBackground)
                    _owners[i].YieldToForeground();
            }
        }

        /// <summary>
        /// 입력 분배(<c>CombatSceneInputDriver.TickInputs</c>)가 연결 입력 묶음의 입구(<see cref="CombatSceneInput.TickInteraction"/>)로 매 Tick 부르는 입력 기능의 입구 하나입니다(View 2라운드 W5-3, 평가 C8-01).
        /// 감시 단계(<see cref="Poll(CombatPreviewResult, in CombatInputFacts, IReadOnlyList{CharacterData})"/>) 뒤,
        /// 새 응답이 왔으면(<paramref name="answered"/>) <b>그때</b> <paramref name="readAnswerFacts"/>로 응답 사실을 읽어 응답 단계(<see cref="PreviewAnswered"/>)를 돕니다.
        /// 응답 사실은 감시 단계가 바꾼 슬롯을 반영해야 해서 감시 뒤에 읽습니다.
        /// </summary>
        /// <param name="facts">감시 단계에 쓰는 이번 Tick의 사실입니다.</param>
        /// <param name="latestPreview">마지막 응답(번호 무관)입니다. 없으면 null입니다.</param>
        /// <param name="answered">이번 Tick에 현재 요청의 새 응답이 왔는지입니다.</param>
        /// <param name="readAnswerFacts">응답 단계의 사실을 읽는 공급자입니다(입력 분배가 한 번 만든 대리자).</param>
        internal void Tick(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CombatPreviewResult latestPreview,
            bool answered, Func<CombatInputFacts> readAnswerFacts)
        {
            if (readAnswerFacts == null)
                throw new ArgumentNullException(nameof(readAnswerFacts));

            Poll(latestPreview, facts, characters);
            if (answered)
                PreviewAnswered(readAnswerFacts(), characters);
        }

        /// <summary>
        /// 감시 단계입니다. 전경 소유자에게 마지막 응답을 주어 자기 요청의 수명을 확인·재시도하게 하고(경로의 응답 소비·이동 모드 범위·복귀 견적, 평가 E4),
        /// 그 뒤 배경 소유자(호버 공격 범위)를 맞춥니다(호버 공격 범위 설계 UR6). 응답이 없으면 null입니다.
        /// 배치·공격의 응답 전달(<see cref="PreviewAnswered"/>)과는 규칙이 달라 두 단계로 둡니다(View 2라운드 W5-3).
        /// 밖에서는 <see cref="Tick"/>만 부릅니다(평가 C5-02).
        /// </summary>
        private void Poll(CombatPreviewResult latestPreview, in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            Poll(latestPreview);
            // 전경 감시 뒤의 실시간 상태(공격 모드·아이템 드래그·스킬 모드)로 판단한다. facts의 대상은 감시 전에 읽은 사본이라 쓰지 않는다(평가 C4-13).
            var target = ResolvePointerTarget(facts.CurrentLayer);
            bool canShow = IsRangeHoverTarget(target);
            for (int i = 0; i < _owners.Length; i++)
            {
                if (_owners[i].IsBackground)
                    _owners[i].ObserveTick(facts, characters, canShow);
            }
        }

        /// <summary>
        /// 전경 소유자만 감시합니다. 감시 단계가 맨 먼저 부르는 단계이고, 사실을 받지 않으므로 배경 소유자는 돌지 않습니다.
        /// </summary>
        private void Poll(CombatPreviewResult latestPreview)
        {
            for (int i = 0; i < _owners.Length; i++)
                _owners[i].Poll(latestPreview);
        }

        /// <summary>
        /// Core가 현재 요청에 답했습니다(facts의 <see cref="CombatInputFacts.PreviewAnswer"/>, 설계 §21.1).
        /// 요청 번호가 자기 것인 소유자 하나만 반영하고, 반영한 소유자에서 멈춥니다. 답이 없으면 돌지 않습니다.
        /// </summary>
        internal void PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            if (facts.PreviewAnswer == null)
                return;

            for (int i = 0; i < _owners.Length; i++)
            {
                if (_owners[i].PreviewAnswered(facts, characters))
                    return;
            }
        }

        /// <summary>
        /// 결과 재생 전: 경로 입력을 취소하고 공격 상세 프리뷰만 정리합니다. 공격 모드·범위·명령은 유지합니다.
        /// 호버 공격 범위는 내려놓고(유닛이 움직일 수 있음), 재생이 끝나면 마지막 포인터로 다시 판단합니다.
        /// </summary>
        internal void BeforeResult()
        {
            for (int i = 0; i < _owners.Length; i++)
                _owners[i].BeforeResult();
        }

        /// <summary>
        /// 결과를 그린 직후(후처리 <see cref="AfterResult"/> 전) 명령 결과를 알립니다. 공격 조준과 스킬 모드가 씁니다 —
        /// 그 모드가 낸 명령이 수락됐으면 후처리에서 그 모드를 끝냅니다(공격 2026-10-28 사용자, 스킬 = 전투 스킬 View 설계 P8 — 거절이면 가용성부터 다시).
        /// </summary>
        internal void CommandResolved(CombatResult result)
        {
            _attack.CommandResolved(result);
            _skill.CommandResolved(result);
        }

        /// <summary>
        /// 결과 후처리: 각 소유자가 현재 사실과 맞지 않게 된 Drag·모드를 정리하고,
        /// 유효하게 남은 쪽만 새 번호로 다시 요청합니다(설계 §5.5·§7.1). 결과 확인 전에 부릅니다.
        /// </summary>
        internal void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            for (int i = 0; i < _owners.Length; i++)
                _owners[i].AfterResult(facts, characters);
        }

        /// <summary>
        /// 결과 확인(처리 번호 삭제·확인 번호 기록·재생 해제) 뒤: 경로 입력이 복귀 견적을 맞춥니다.
        /// 경로 편집 허용이 "재생 중 아님"을 요구하므로 확인 전 <see cref="AfterResult"/>와 합치지 않습니다.
        /// </summary>
        internal void AfterAcknowledged()
        {
            for (int i = 0; i < _owners.Length; i++)
                _owners[i].AfterAcknowledged();
        }

        /// <summary>
        /// <b>경로 제외</b> — 배치·공격·아이템의 표현만 정리합니다(Drag·고스트·원본 숨김·호버·모드·범위·자기 미리보기). 경로는 건드리지 않고 대기는 보존합니다.
        /// 시작·턴 종료 제출 성공 뒤와 개발 Layer 전환에서 씁니다. 정리할 것이 없으면 아무것도 하지 않습니다.
        /// </summary>
        internal void CancelPresentation()
        {
            for (int i = 0; i < _owners.Length; i++)
                _owners[i].CancelPresentation();
        }

        /// <summary>
        /// <b>경로 포함</b> — 경로 입력을 취소한 뒤 배치·공격 표현을 정리합니다. 포커스 상실, 포기 요청, 정상 종료의 제출 정지, 입력 Layer 해제·변경에서 씁니다.
        /// 이동 모드 범위는 이동 모드가 같은 사실로 이어지면 남깁니다(범위까지 지우려면 <see cref="LeaveMoveMode"/>).
        /// 호버 공격 범위는 포인터도 잊습니다(새 포인터 입력 전에는 다시 보이지 않음).
        /// </summary>
        internal void CancelInputs()
        {
            for (int i = 0; i < _owners.Length; i++)
                _owners[i].CancelInputs();
        }

        /// <summary>
        /// 오류 정지·씬 연결 해제: 이름 입구용 경로 참조를 먼저 놓고, 소유자마다 정지(경로는 연결 끊기, 나머지는 입력 정리)를 시도합니다.
        /// 하나가 실패해도 나머지를 시도하고 실패를 모아 한 번 던집니다. 명령 Buffer는 바꾸지 않습니다.
        /// 시작/턴 종료 대기는 Gate를 가진 연결 입력 묶음(<see cref="CombatSceneInput.Stop"/>)이 따로 지웁니다.
        /// </summary>
        internal void Stop()
        {
            // 정지 콜백 안에서 이름 입구가 다시 들어와도 놓인 경로에 닿지 않게 목록을 돌기 전에 놓는다(F2).
            _path = null;
            List<Exception> errors = null;
            for (int i = 0; i < _owners.Length; i++)
                CleanupErrors.Try(_owners[i].Stop, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatInteraction] 입력 정지 중 정리에 실패했습니다.");
        }

        #endregion
    }
}
