using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 시작 조건을 어느 자리에서 묻는지입니다(연출 조립 설계 B11). 같은 규칙을 자리마다 다르게 적용하므로,
    /// 겹침 허용·사망 겹침은 호출자가 불리언으로 조합하지 않고 이 값에서 정합니다.
    /// </summary>
    internal enum CombatStartProfile
    {
        // 0은 쓰지 않습니다. 기본값 질문(default)이 겹침 허용으로 조용히 통과하지 않고 겹침 없는 단정처럼 막게 합니다.
        /// <summary>
        /// 사건·걸음·복귀를 시작해도 되는지입니다. 스케줄러의 질의와 시작 직전 단정이 같은 판단을 씁니다(연출 조립 설계 §9 I-1, 결정 Q-R14 A).
        /// 사건 완료를 알린 출처의 남은 연출과 겹침을 허용합니다.
        /// </summary>
        Start = 1,

        /// <summary>결과 하나를 받기 전의 단정입니다. 사건 재생은 쉬어야 하며 결과 없이 시작한 외형 생성은 계속 진행할 수 있습니다.</summary>
        Result,

        /// <summary>기차 동시 사망을 겹쳐 시작하기 전의 단정입니다. 반응은 사망끼리만 겹칠 수 있습니다(동시 처리 설계 C7).</summary>
        ConcurrentDeaths,
    }

    /// <summary>시작을 막는 첫 이유입니다(연출 조립 설계 B11). 예외 문구와 진단에 씁니다.</summary>
    internal enum CombatStartBlock
    {
        None,

        /// <summary>공격·조각 실행·반응 중 유닛·무기 표시가 사라졌습니다. 다음 Tick이 그 원인으로 실패를 알립니다.</summary>
        Unavailable,

        /// <summary>공격 출처 Gate가 아직 이 사건을 허락하지 않습니다(히트 타이밍 설계 H3).</summary>
        AttackGate,

        /// <summary>히트 전 공격이 진행 중입니다.</summary>
        AttackBusy,

        /// <summary>조각 행동 실행(조립된 포효·소환)이 진행 중이고 겹침이 허용되지 않습니다(조각·조립 설계 BP5).</summary>
        PieceBusy,

        /// <summary>사건 주체(또는 모든 유닛)의 조각 유닛 실행(조립된 피격·사망)이 진행 중입니다(조각·조립 설계 BP5).</summary>
        PieceSubject,

        /// <summary>사건 주체(또는 모든 유닛)의 반응이 진행 중입니다(동시 처리 설계 C5).</summary>
        Reaction,

        /// <summary>사건 주체(또는 모든 유닛)의 걸음 레인이 진행 중입니다(동시 처리 설계 C5).</summary>
        StepLane,

        /// <summary>걸음 Gate가 아직 이 사건을 허락하지 않습니다(이동 접촉 설계 M4).</summary>
        StepGate,

        /// <summary>간격 뒤 처리할 대기 사건이 있습니다.</summary>
        Waiting,
        /// <summary>다음 사건을 막는 필드 외형 연출(토네이도 생성·축소, 바닥 교체 무너짐의 떨림)이 진행 중입니다(타일 교체 설계 TR8).</summary>
        FieldAppearance,
    }

    /// <summary>
    /// 시작 조건 질문 하나입니다(연출 조립 설계 B11). 프로필과 시작할 사건을 묶습니다.
    /// </summary>
    internal readonly struct CombatStartQuery
    {
        private CombatStartQuery(CombatStartProfile profile, CombatEvent item)
        {
            Profile = profile;
            Item = item;
        }

        internal CombatStartProfile Profile { get; }

        /// <summary>시작할 사건입니다. 결과 시작·동시 사망 프로필에서는 없습니다.</summary>
        internal CombatEvent Item { get; }

        /// <summary>사건 완료를 알린 출처(공격·포효·걸음)의 남은 연출과 겹쳐도 되는지입니다.</summary>
        internal bool AllowsOverlap => Profile == CombatStartProfile.Start;

        /// <summary>반응을 사망끼리만 겹치는지입니다.</summary>
        internal bool AllowsDeaths => Profile == CombatStartProfile.ConcurrentDeaths;

        internal static CombatStartQuery Start(CombatEvent item) =>
            new(CombatStartProfile.Start, item ?? throw new ArgumentNullException(nameof(item)));

        internal static CombatStartQuery Result() => new(CombatStartProfile.Result, null);

        internal static CombatStartQuery ConcurrentDeaths() => new(CombatStartProfile.ConcurrentDeaths, null);
    }
}
