using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 클립 이벤트 번호 → 실행 신호 표입니다(평가 C2-04, 작은 묶음 설계 SB2). 클립 조각이 신호 입구를 받을 때와 원거리 클립 조립이 발사·큐 시점을 신호에 이을 때 씁니다.
    /// 번호의 이름표·큐 시점·순서는 DB 표(<see cref="CombatClipEventIds"/>)가 원본이고, 이 표는 View 형식(<see cref="CombatPieceSignal"/>)만 맡습니다
    /// (층 규칙: DB 입력 계층은 View 형식을 쓰지 않음).
    /// </summary>
    /// <remarks>
    /// 정적 생성자가 DB 표의 번호마다 줄이 하나씩 있는지 봅니다. 공격 시작(<c>CombatAttackStart</c>) 생성자가 <see cref="Validate"/>로 한 번 건드려,
    /// 줄이 빠졌으면 첫 클립 공격이 아니라 전투 연결 때 드러납니다(설계 P10, 계획 TP-5). 반복은 인덱스 루프라 할당하지 않습니다.
    /// </remarks>
    internal static class CombatClipEventSignals
    {
        private static readonly (CombatClipEventId Id, CombatPieceSignal Signal)[] Rows =
        {
            (CombatClipEventId.Event1, CombatPieceSignal.ClipEvent),
            (CombatClipEventId.Event2, CombatPieceSignal.ClipEvent2),
            (CombatClipEventId.Event3, CombatPieceSignal.ClipEvent3),
        };

        // 명시적 정적 생성자입니다. 표를 처음 쓸 때 한 번 검사합니다.
        static CombatClipEventSignals()
        {
            string error = FindTableError();
            if (error != null)
                throw new InvalidOperationException($"[CombatClipEventSignals] 클립 이벤트 신호 표가 잘못되었습니다: {error}");
        }

        /// <summary>표 검사(정적 생성자)를 돌리는 입구입니다. 하는 일은 없습니다.</summary>
        internal static void Validate()
        {
        }

        /// <summary>그 번호의 실행 신호입니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">표에 없는 번호일 때입니다(정적 생성자가 DB 표의 번호를 모두 확인하므로 정의된 번호에서는 나지 않음).</exception>
        internal static CombatPieceSignal SignalOf(CombatClipEventId id)
        {
            for (int i = 0; i < Rows.Length; i++)
                if (Rows[i].Id == id) return Rows[i].Signal;
            throw new ArgumentOutOfRangeException(nameof(id), id, "정의되지 않은 클립 이벤트입니다.");
        }

        // 규칙: DB 표의 번호마다 줄이 하나, DB 표에 없는 번호의 줄 없음, 신호는 겹치지 않음.
        private static string FindTableError()
        {
            IReadOnlyList<CombatClipEventId> ids = CombatClipEventIds.All;
            for (int i = 0; i < ids.Count; i++)
            {
                int found = 0;
                for (int r = 0; r < Rows.Length; r++)
                    if (Rows[r].Id == ids[i]) found++;
                if (found != 1)
                    return $"번호 {ids[i]}의 줄이 {found}개입니다(하나여야 함)";
            }
            for (int r = 0; r < Rows.Length; r++)
            {
                if (!CombatClipEventIds.IsDefined(Rows[r].Id))
                    return $"번호 표에 없는 번호의 줄이 있습니다: {Rows[r].Id}";
                for (int s = 0; s < r; s++)
                    if (Rows[s].Signal == Rows[r].Signal)
                        return $"{Rows[s].Id}와 {Rows[r].Id}의 신호가 같습니다: {Rows[r].Signal}";
            }
            return null;
        }
    }
}
