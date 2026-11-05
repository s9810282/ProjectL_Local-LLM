using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 사건 형식 하나의 표시 지식입니다(Presenter 분리 설계 S3, 계획 §5.1). 사건 하나 안의 순서는 사건 Presenter의 틀 하나가 정하고,
    /// 처리기는 연출 단계마다 답만 합니다: 간격 전에 보임 → 강조 칸 → 사실 전 시작 → 사실 반영 → 사실 뒤 시작.
    /// 사건 형식의 사실 지식(행위자·주체·간격 종류 등)은 사건 형식 표(<c>CombatEventTypeTable</c>)가 답합니다(View 2라운드 W7-1).
    /// 기본 구현은 "그 단계에서 할 일 없음"입니다. 처리기는 <see cref="CombatEventHandlerTable"/>에 형식별로 하나씩 등록합니다.
    /// </summary>
    internal abstract class CombatEventHandler
    {
        /// <summary>표에 없는 사건의 처리기입니다. 모든 단계를 건너뛰어 틀의 일반 경로(소리·순간 큐·다시 그리기·머묾·완료)만 탑니다.</summary>
        internal static readonly CombatEventHandler None = new NoneHandler();

        /// <summary>
        /// 간격을 기다리기 전에 사건을 먼저 보이고 간격 뒤 완료만 알릴지입니다. 기본은 간격 뒤에 보입니다.
        /// 진영 종료 사건은 턴 처리기가 답합니다.
        /// </summary>
        internal virtual bool ShowsBeforeGap(CombatEvent e) => false;

        /// <summary>재생 강조에 둘 칸을 <paramref name="cells"/>에 더합니다. 표시 콜백이 있을 때만 부릅니다.</summary>
        internal virtual void CollectHighlight(CombatEvent e, List<CellCoord> cells)
        {
        }

        /// <summary>
        /// 사실 반영 전에 연출을 시작합니다. 시작했으면 true이고, 사건 완료는 그 연출이 알립니다.
        /// false면 틀이 소리·순간 큐·사실 반영으로 이어 갑니다.
        /// </summary>
        internal virtual bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version) => false;

        /// <summary>사건 스냅샷을 표시 사실·가시·오브젝트 표시에 반영합니다. 다시 그리기는 틀이 합니다.</summary>
        internal virtual void ApplyFacts(CombatEvent e)
        {
        }

        /// <summary>사실 반영 뒤에 연출을 시작합니다. 시작했으면 true이고, 사건 완료는 그 연출이 알립니다.</summary>
        internal virtual bool TryBeginAfterFacts(CombatEvent e, Action completed, int version) => false;

        private sealed class NoneHandler : CombatEventHandler
        {
        }
    }
}
