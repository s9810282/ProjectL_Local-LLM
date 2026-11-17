using System;
using System.Collections.Generic;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 클립 중간 이벤트 번호의 표입니다(평가 C2-04, 작은 묶음 설계 SB2). 번호마다 한 줄 = (번호, 애니메이션 이름표 함수 이름, 큐 시점)입니다.
    /// 첫 줄은 기준 이벤트(<see cref="CombatClipEventId.Event1"/>, 필수)이고, 나머지 줄은 선택 이벤트입니다. 표 순서가 번호 순서입니다.
    /// DB 읽기(<see cref="CombatClipEvents.ReadTimings"/>)·시각 검사(<see cref="CombatClipTimings.FindTimeError"/>)·큐 규칙(<see cref="CombatPresentationCueRules"/>)과
    /// 전투 재생의 큐 연결이 이 표 하나를 읽습니다. 번호 → 실행 신호(View 형식)는 층 규칙상 View 쪽 표 <c>CombatClipEventSignals</c>가 맡습니다.
    /// </summary>
    /// <remarks>
    /// <para>새 번호를 더할 때 고칠 곳은 값 넷(<see cref="CombatClipEventId"/>·<see cref="CombatCueAnchor"/>·실행 신호 값·<see cref="CombatAnimationEvents"/>의 이름표 메서드)과
    /// 이 표·View 표의 줄 하나씩입니다. 재생·조각·커서·DB 읽기·검사는 고치지 않습니다.</para>
    /// <para>정적 생성자가 표를 검사합니다. 줄이 빠지거나 겹치면 이 형식을 처음 쓸 때(부트의 DB 생성) 예외이고, 원인 문구는 형식 초기화 예외의 안쪽 예외에 있습니다(설계 §5).
    /// 반복은 인덱스 루프라 할당하지 않습니다.</para>
    /// </remarks>
    internal static class CombatClipEventIds
    {
        private readonly struct Row
        {
            internal Row(CombatClipEventId id, string functionName, CombatCueAnchor anchor)
            {
                Id = id;
                FunctionName = functionName;
                Anchor = anchor;
            }

            internal CombatClipEventId Id { get; }
            internal string FunctionName { get; }
            internal CombatCueAnchor Anchor { get; }
        }

        private static readonly Row[] Rows =
        {
            new Row(CombatClipEventId.Event1, nameof(CombatAnimationEvents.ClipEvent), CombatCueAnchor.Event1),
            new Row(CombatClipEventId.Event2, nameof(CombatAnimationEvents.ClipEvent2), CombatCueAnchor.Event2),
            new Row(CombatClipEventId.Event3, nameof(CombatAnimationEvents.ClipEvent3), CombatCueAnchor.Event3),
        };

        // 명시적 정적 생성자입니다. 표를 처음 쓸 때 한 번 검사합니다(설계 P4, 계획 TP-7).
        static CombatClipEventIds()
        {
            string error = FindTableError();
            if (error != null)
                throw new InvalidOperationException($"[CombatClipEventIds] 클립 이벤트 번호 표가 잘못되었습니다: {error}");
            var ids = new CombatClipEventId[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
                ids[i] = Rows[i].Id;
            All = Array.AsReadOnly(ids);
        }

        /// <summary>표의 번호입니다. 표 순서(= 번호 순)이고 첫 칸이 기준 이벤트입니다.</summary>
        internal static IReadOnlyList<CombatClipEventId> All { get; }

        /// <summary>표의 줄 수입니다.</summary>
        internal static int Count => Rows.Length;

        /// <summary>기준 이벤트(필수, 표의 첫 줄)입니다. 시각 값의 <see cref="CombatClipTimings.EventTime"/>이 이 번호의 시각입니다.</summary>
        internal static CombatClipEventId Primary => Rows[0].Id;

        /// <summary>표에 줄이 있는 번호인지입니다.</summary>
        internal static bool IsDefined(CombatClipEventId id) => IndexOf(id) >= 0;

        /// <summary>표에서 그 번호의 칸입니다. 없으면 -1입니다.</summary>
        internal static int IndexOf(CombatClipEventId id)
        {
            for (int i = 0; i < Rows.Length; i++)
                if (Rows[i].Id == id) return i;
            return -1;
        }

        /// <summary>그 번호의 이름표 함수 이름입니다(<see cref="CombatAnimationEvents"/>의 메서드 이름).</summary>
        /// <exception cref="ArgumentOutOfRangeException">표에 없는 번호일 때입니다.</exception>
        internal static string FunctionNameOf(CombatClipEventId id) => Rows[RequireIndex(id)].FunctionName;

        /// <summary>그 번호의 큐 시점입니다. 런타임 연결과 DB 존재 검사가 함께 씁니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">표에 없는 번호일 때입니다.</exception>
        internal static CombatCueAnchor AnchorOf(CombatClipEventId id) => Rows[RequireIndex(id)].Anchor;

        /// <summary>이름표 함수 이름으로 번호를 찾습니다. 시작·끝 이름표와 모르는 이름은 false입니다.</summary>
        internal static bool TryFindByFunction(string functionName, out CombatClipEventId id)
        {
            for (int i = 0; i < Rows.Length; i++)
            {
                if (Rows[i].FunctionName != functionName) continue;
                id = Rows[i].Id;
                return true;
            }
            id = default;
            return false;
        }

        /// <summary>그 큐 시점이 클립 이벤트 시점(Event1~3)인지입니다.</summary>
        internal static bool IsEventAnchor(CombatCueAnchor anchor)
        {
            for (int i = 0; i < Rows.Length; i++)
                if (Rows[i].Anchor == anchor) return true;
            return false;
        }

        private static int RequireIndex(CombatClipEventId id)
        {
            int index = IndexOf(id);
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "정의되지 않은 클립 이벤트입니다.");
            return index;
        }

        // 표 규칙입니다: 줄이 있고, 첫 줄이 기준 이벤트이며, 번호가 늘어나는 순서로 겹치지 않고, 모든 번호 값에 줄이 있고,
        // 함수 이름은 비지 않고 시작·끝 이름표가 아니며 겹치지 않고, 큐 시점은 정의된 값이며 시작·접촉·끝이 아니고 겹치지 않습니다.
        private static string FindTableError()
        {
            if (Rows.Length == 0)
                return "줄이 없습니다";
            if (Rows[0].Id != CombatClipEventId.Event1)
                return $"첫 줄은 기준 이벤트({CombatClipEventId.Event1})여야 합니다: {Rows[0].Id}";
            for (int i = 0; i < Rows.Length; i++)
            {
                Row row = Rows[i];
                if (!System.Enum.IsDefined(typeof(CombatClipEventId), row.Id))
                    return $"{i}번 줄의 번호가 정의되지 않은 값입니다: {(int)row.Id}";
                if (i > 0 && row.Id <= Rows[i - 1].Id)
                    return $"{i}번 줄의 번호({row.Id})가 앞 줄({Rows[i - 1].Id})보다 커야 합니다";
                if (string.IsNullOrEmpty(row.FunctionName))
                    return $"{row.Id}의 함수 이름이 비어 있습니다";
                if (row.FunctionName == nameof(CombatAnimationEvents.ClipStart) || row.FunctionName == nameof(CombatAnimationEvents.ClipEnd))
                    return $"{row.Id}의 함수 이름이 시작·끝 이름표입니다: {row.FunctionName}";
                if (!System.Enum.IsDefined(typeof(CombatCueAnchor), row.Anchor))
                    return $"{row.Id}의 큐 시점이 정의되지 않은 값입니다: {(int)row.Anchor}";
                if (row.Anchor is CombatCueAnchor.Start or CombatCueAnchor.Contact or CombatCueAnchor.End)
                    return $"{row.Id}의 큐 시점은 클립 이벤트 시점이어야 합니다(Start·Contact·End가 아님): {row.Anchor}";
                for (int j = 0; j < i; j++)
                {
                    if (Rows[j].FunctionName == row.FunctionName)
                        return $"{Rows[j].Id}와 {row.Id}의 함수 이름이 같습니다: {row.FunctionName}";
                    if (Rows[j].Anchor == row.Anchor)
                        return $"{Rows[j].Id}와 {row.Id}의 큐 시점이 같습니다: {row.Anchor}";
                }
            }
            foreach (CombatClipEventId value in System.Enum.GetValues(typeof(CombatClipEventId)))
            {
                bool found = false;
                for (int i = 0; i < Rows.Length && !found; i++)
                    found = Rows[i].Id == value;
                if (!found)
                    return $"번호 {value}의 줄이 없습니다";
            }
            return null;
        }
    }
}
