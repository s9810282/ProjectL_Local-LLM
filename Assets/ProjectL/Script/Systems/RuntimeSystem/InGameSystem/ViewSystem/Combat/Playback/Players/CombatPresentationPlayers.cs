using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 사건 Presenter의 재생기 실행 목록입니다(연출 조립 설계 B12). 외형 전환 → 몸 위 글자(있을 때)를 먼저 진행하고, 기존 겹침 순서(공격 독점 → 포효 히트 전 조각 행동 실행만 → 걸음·대기,
    /// 설계 B14 O1·O3·O5·O7) 자리마다 이름 있는 인자로 받아 고정합니다(보완 2). 만든 뒤에는 바뀌지 않습니다(지침 §11).
    /// 조각 실행 집합 자리(<c>pieces</c>)는 옛 능력 자리의 규칙을 이어받았습니다(조각·조립 설계 BP5, B-2). 옛 반응 자리는 피격·사망 반응이
    /// 조각 실행 안의 반응 조각으로 옮겨 걷었습니다(피격 구조 정리 설계 HC1, 2026-11-17).
    /// </summary>
    internal sealed class CombatPresentationPlayers
    {
        private readonly ICombatPresentationPlayer[] _players;

        private CombatPresentationPlayers(ICombatPresentationPlayer[] players) => _players = players;

        internal static CombatPresentationPlayers Create(ICombatPresentationPlayer attack, ICombatPresentationPlayer pieces,
            ICombatPresentationPlayer step, ICombatPresentationPlayer appearance, ICombatPresentationPlayer bodyText = null) =>
            new(new[]
            {
                // 외형은 유닛의 독점 연출과 함께 진행하므로 중단 가능한 재생기보다 먼저 Tick합니다.
                appearance ?? throw new ArgumentNullException(nameof(appearance)),
                bodyText,
                attack ?? throw new ArgumentNullException(nameof(attack)),
                pieces ?? throw new ArgumentNullException(nameof(pieces)),
                step ?? throw new ArgumentNullException(nameof(step)),
            }.Where(player => player != null).ToArray());

        internal void BeginResult(CombatResult result)
        {
            for (int i = 0; i < _players.Length; i++)
                _players[i].BeginResult(result);
        }

        /// <summary>목록 순서로 보고 첫 막는 이유를 돌려줍니다(설계 B11). 모두 막지 않으면 <see cref="CombatStartBlock.None"/>입니다.</summary>
        internal CombatStartBlock FindBlock(in CombatStartQuery query)
        {
            for (int i = 0; i < _players.Length; i++)
            {
                CombatStartBlock block = _players[i].FindBlock(query);
                if (block != CombatStartBlock.None)
                    return block;
            }
            return CombatStartBlock.None;
        }

        /// <summary>모든 재생기의 프레임 시작 사실을 먼저 기록한 뒤, 순서대로 진행하다 false를 답한 재생기에서 멈춥니다.</summary>
        internal void Tick(in CombatPlaybackFrame frame)
        {
            for (int i = 0; i < _players.Length; i++)
                _players[i].BeginFrame(frame);
            for (int i = 0; i < _players.Length; i++)
                if (!_players[i].Tick(frame))
                    return;
        }

        internal bool IsComplete
        {
            get
            {
                for (int i = 0; i < _players.Length; i++)
                    if (!_players[i].IsComplete)
                        return false;
                return true;
            }
        }

        internal bool BlocksFinalFacts
        {
            get
            {
                for (int i = 0; i < _players.Length; i++)
                    if (_players[i].BlocksFinalFacts)
                        return true;
                return false;
            }
        }

        /// <summary>순서대로 모두 취소합니다. 실패는 <paramref name="errors"/>에 모으고 나머지를 계속 정리합니다(결정 Q-R7 A).</summary>
        internal void CancelAll(ref List<Exception> errors)
        {
            for (int i = 0; i < _players.Length; i++)
                CleanupErrors.Try(_players[i].Cancel, ref errors);
        }

        /// <summary>순서대로 모두 해제합니다. 실패는 <paramref name="errors"/>에 모으고 나머지를 계속 해제합니다.</summary>
        internal void ReleaseAll(ref List<Exception> errors)
        {
            for (int i = 0; i < _players.Length; i++)
                CleanupErrors.Try(_players[i].Release, ref errors);
        }
    }
}
