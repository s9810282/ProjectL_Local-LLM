using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 그릴 값을 읽고 보유 능력치가 포함된 덱 스냅샷을 로스터 버전별로 재사용합니다.
    /// </summary>
    /// <remarks>
    /// 조정자(CombatViewSystem)는 서비스를 받을 때 이것을 한 번 만들고, 첫 그리기·결과 그리기·배경 세우기·가방 드래그 재고가 이것을 읽습니다.
    /// 조정자는 재화·재고·현재 진행 조회를 직접 갖지 않습니다.
    /// </remarks>
    internal sealed class CombatDisplayValueReader
    {
        private readonly ICombatStateBufferReadSink _state;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICurrencyBufferReadSink _currency;
        private readonly ICurrentProgressBufferReadSink _current;
        // 재고는 가방 표시와 가방 드래그가 쓴다(가방 설계 P1·P4).
        private readonly IItemInventoryReadSink _items;
        private readonly IRunDeckService _deck;
        private DeckEditorSnapshot _deckSnapshot;

        internal CombatDisplayValueReader(ICombatStateBufferReadSink state, ICharacterBufferReadSink characters,
            ICurrencyBufferReadSink currency, ICurrentProgressBufferReadSink current,
            IItemInventoryReadSink items, IRunDeckService deck)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
        }

        /// <summary>현재 전투의 Buffer 값과 대응하는 보유 능력치를 읽습니다.</summary>
        internal CombatDisplayValues Read()
        {
            // 오브젝트·지형 외형 회전도 배경(씬 로드 때 세움)과 같은 스테이지 키로 고른다.
            int stageKey = CurrentStageKey;
            // 골드는 기존 Currency 읽기 싱크에서 받아 표시하며 View에 별도 잔액을 두지 않는다.
            // 재고도 같은 시점에 한 번 읽는다. 전투 안 재고 변화는 모두 명령 결과라 설치·결과 후처리 그리기로 충분하다(가방 설계 P1).
            CombatStateData state = _state.Current;
            return new CombatDisplayValues(state, _characters.Characters, _currency.Amount,
                stageKey,
                _characters.Owned, _items.Current, ReadOwnedStats(state.Membership));
        }

        private IReadOnlyList<OwnedCharacterStatsSnapshot> ReadOwnedStats(RunDeckSnapshot membership)
        {
            // 확정 덱이 없는 개발 전투에서는 보유 등급 스냅샷도 사용하지 않습니다.
            if (membership == null) return Array.Empty<OwnedCharacterStatsSnapshot>();
            long revision = _characters.RosterRevision;
            if (_deckSnapshot != null && _deckSnapshot.RunId == membership.RunId && _deckSnapshot.RosterRevision == revision)
                return _deckSnapshot.Stats;

            // 덱 조회가 원본 갱신을 기다리거나 다른 Run을 읽은 경우 다음 표시에서 다시 조회합니다.
            DeckEditorSnapshot snapshot = _deck.Read().Snapshot;
            _deckSnapshot = snapshot != null && snapshot.RunId == membership.RunId && snapshot.RosterRevision == revision
                ? snapshot
                : null;
            return _deckSnapshot?.Stats ?? Array.Empty<OwnedCharacterStatsSnapshot>();
        }

        internal void ClearOwnedStats() => _deckSnapshot = null;

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
