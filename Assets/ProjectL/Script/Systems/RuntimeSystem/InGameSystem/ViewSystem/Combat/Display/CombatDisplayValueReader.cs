using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 그릴 값을 Buffer에서 한 번에 읽습니다. 상태·캐릭터·재화·재고·현재 진행 조회를 갖고,
    /// 표시 값(<see cref="CombatDisplayValues"/>)·이 전투의 스테이지 키·아이템 재고 수를 답합니다. 저장하지 않고 부를 때마다 읽습니다.
    /// </summary>
    /// <remarks>
    /// 조정자(CombatViewSystem)는 서비스를 받을 때 이것을 한 번 만들고, 첫 그리기·결과 그리기·배경 세우기·가방 드래그 재고가 이것을 읽습니다.
    /// 조정자는 재화·재고·현재 진행 조회를 직접 갖지 않으며 이 읽기 경로는 Core를 호출하지 않습니다.
    /// </remarks>
    internal sealed class CombatDisplayValueReader
    {
        private readonly ICombatStateBufferReadSink _state;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICurrencyBufferReadSink _currency;
        private readonly ICurrentProgressBufferReadSink _current;
        // 재고는 가방 표시와 가방 드래그가 쓴다(가방 설계 P1·P4).
        private readonly IItemInventoryReadSink _items;

        internal CombatDisplayValueReader(ICombatStateBufferReadSink state, ICharacterBufferReadSink characters,
            ICurrencyBufferReadSink currency, ICurrentProgressBufferReadSink current,
            IItemInventoryReadSink items)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        /// <summary>그릴 값을 Buffer에서 한 번에 읽습니다. 전투가 있을 때만 부릅니다(전투가 없으면 상태가 없어 예외).</summary>
        internal CombatDisplayValues Read()
        {
            // 오브젝트·지형 외형 회전도 배경(씬 로드 때 세움)과 같은 스테이지 키로 고른다.
            int stageKey = CurrentStageKey;
            // 골드는 기존 Currency 읽기 싱크에서 받아 표시하며 View에 별도 잔액을 두지 않는다.
            // 재고도 같은 시점에 한 번 읽는다. 전투 안 재고 변화는 모두 명령 결과라 설치·결과 후처리 그리기로 충분하다(가방 설계 P1).
            return new CombatDisplayValues(_state.Current, _characters.Characters, _currency.Amount,
                stageKey,
                _characters.Owned, _items.Current);
        }

        /// <summary>
        /// 이 전투의 스테이지 키입니다. 진입 때 Core가 게시한 현재 선택에서 읽고(InGame DB를 읽지 않음, P6), 선택이 없으면 0입니다.
        /// 씬 로드 전 전투 준비가 게시하므로 씬 로드(<see cref="SceneNodeOrder.CombatBackdrop"/>)의 배경 세우기와 표시 값이 같은 키를 씁니다(배경 설계 F1).
        /// </summary>
        internal int CurrentStageKey => _current.HasSelection ? _current.CurrentStage.StageKey : 0;

        /// <summary>
        /// 이 전투의 보드 크기(가로·세로 칸 수)입니다. 전투가 없으면 false입니다.
        /// 전투 준비가 씬 전환보다 먼저 상태를 설치하므로 씬 로드(<see cref="SceneNodeOrder.CombatBackdrop"/>)의 배경 바닥 타일 맞춤이 읽습니다(타일 맞춤 계획 P9).
        /// </summary>
        internal bool TryReadBoardSize(out Vector2Int size)
        {
            if (!_state.HasBattle)
            {
                size = default;
                return false;
            }
            CombatStateData state = _state.Current;
            size = new Vector2Int(state.BoardWidth, state.BoardHeight);
            return true;
        }

        /// <summary>가방 드래그가 읽는 지금 재고 수입니다(가방 설계 P4). 키가 0 이하면 0입니다.</summary>
        internal int ItemStock(int itemKey) => itemKey > 0 ? _items.Current.GetCount(itemKey) : 0;
    }
}
