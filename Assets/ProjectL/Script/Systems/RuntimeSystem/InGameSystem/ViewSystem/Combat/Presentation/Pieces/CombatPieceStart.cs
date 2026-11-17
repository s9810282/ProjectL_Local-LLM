using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>항목이 언제 시작하는지의 종류입니다(조각·조립 설계 BP2). 0은 쓰지 않아 기본값 시점을 조립이 거절합니다.</summary>
    internal enum CombatPieceStartKind
    {
        /// <summary>실행 시작 뒤 지연만큼 지나면 시작합니다.</summary>
        AtStart = 1,

        /// <summary>앞 항목의 신호 뒤 지연만큼 지나면 시작합니다.</summary>
        After = 2,
    }

    /// <summary>
    /// 조각이 내는 신호입니다(조각·조립 설계 BP1·BP2 보충). 뜻의 원본은 클립 설계 AC6 신호 표입니다. 다른 신호는 쓰는 조립이 생길 때 더하고,
    /// 번호는 들어온 순서대로 붙입니다(번호 값에 기대는 코드는 없음).
    /// </summary>
    /// <remarks>
    /// <see cref="End"/>는 실행이 냅니다. 그 밖의 신호는 조각이 맥락의 신호 입구(<see cref="CombatPieceContext.SignalCallback"/>)로 냅니다.
    /// 끝 말고 다른 신호를 내지 못한 채 조각이 끝나면, 그 신호를 기다리던 항목은 시작하지 않고 끝난 것으로 봅니다.
    /// </remarks>
    internal enum CombatPieceSignal
    {
        /// <summary>조각의 연출이 끝났습니다.</summary>
        End = 1,

        /// <summary>
        /// 대상이 맞는 순간입니다(Gate 해제와 같은 순간, 카메라 연출 조각 설계 K4). 정면 공격 조각(<see cref="CombatFacingAttackPiece"/>), 코드 모션 조각(<see cref="CombatCodeMotionPiece"/>, 코드 모션 공격),
        /// 해제 조각(<see cref="CombatReleasePiece"/>, 근접 클립 공격), 접촉 집계(<see cref="CombatVolleyReleasePiece"/>, 원거리 클립 공격)가 냅니다.
        /// 내는 조각은 <b>대상을 해제한 뒤</b> 냅니다(원거리는 첫 해제 뒤, 규칙 <see cref="CombatContactRelease"/>). 해제가 연 피격 사건과
        /// 이 신호로 시작한 항목(카메라 흔들림 등)이 같은 순간에 보이게 하는 규칙이고, 이 문서가 원본입니다(클립 계획 §0.6 CK4).
        /// 클립의 이벤트 타이밍(<see cref="ClipEvent"/>)과는 다른 사실입니다(클립 설계 §8 E1). 피격 선딜레이가 0보다 크면 이벤트 타이밍보다 늦습니다.
        /// </summary>
        Hit = 2,

        /// <summary>
        /// 클립의 시작 이벤트(<see cref="DataBaseSystem.Data.View.CombatPresentation.CombatAnimationEvents.ClipStart"/>)입니다. 클립 조각(<see cref="CombatClipPiece"/>)이 냅니다.
        /// </summary>
        ClipStart = 3,

        /// <summary>
        /// 클립의 이벤트 타이밍(연출 기준 시점, <see cref="DataBaseSystem.Data.View.CombatPresentation.CombatAnimationEvents.ClipEvent"/>)입니다. 클립 조각이 냅니다.
        /// 선딜레이는 이 신호 뒤의 시점 지연입니다(<c>After(클립 항목, ClipEvent, 선딜레이)</c>). 대상이 맞는 순간(<see cref="Hit"/>)과는 다른 사실입니다.
        /// </summary>
        ClipEvent = 4,

        /// <summary>
        /// 코드 모션의 기준 순간입니다(범용 연출 구조 설계 G3·G5, 계획 P7). 근접은 타격 순간, 직선·곡사는 발사 순간이고, 옛 코드 모션이 공격 소리를 내던 순간과 같습니다.
        /// 코드 모션 조각(<see cref="CombatCodeMotionPiece"/>)이 냅니다. 클립의 이벤트 타이밍(<see cref="ClipEvent"/>)과는 다른 사실이라 값을 나눕니다.
        /// 근접은 이 신호를 낸 뒤 같은 순간에 대상을 해제하므로(<see cref="Hit"/>), 이 신호로 시작한 항목이 해제보다 먼저 보입니다(옛 "소리 → 해제" 순서).
        /// </summary>
        MotionCue = 5,

        /// <summary>클립의 선택 두 번째 중간 이벤트입니다. 번호가 없는 클립은 내지 않습니다.</summary>
        ClipEvent2 = 6,

        /// <summary>클립의 선택 세 번째 중간 이벤트입니다. 번호가 없는 클립은 내지 않습니다.</summary>
        ClipEvent3 = 7,

        /// <summary>한 탄의 물리 접촉입니다. 피해 후속 해제 뒤에 나는 Hit과 구분합니다.</summary>
        Contact = 8,
    }

    /// <summary>
    /// 항목 하나의 시점입니다(조각·조립 설계 BP2). <see cref="AtStart"/> 또는 <see cref="After"/>로만 만듭니다.
    /// 앞 항목만 가리킬 수 있어 순환이 없고, 번호는 조립(<see cref="CombatComposition.Add"/>)이 더할 때 검사합니다.
    /// </summary>
    internal readonly struct CombatPieceStart
    {
        private CombatPieceStart(CombatPieceStartKind kind, int item, CombatPieceSignal signal, float delay)
        {
            Kind = kind;
            Item = item;
            Signal = signal;
            Delay = delay;
        }

        internal CombatPieceStartKind Kind { get; }

        /// <summary><see cref="CombatPieceStartKind.After"/>가 가리키는 앞 항목 번호입니다. 실행 시작이면 -1입니다.</summary>
        internal int Item { get; }

        /// <summary><see cref="CombatPieceStartKind.After"/>가 기다리는 신호입니다.</summary>
        internal CombatPieceSignal Signal { get; }

        /// <summary>시작 조건이 맞은 뒤 기다리는 초입니다. 유한한 0 이상입니다.</summary>
        internal float Delay { get; }

        /// <summary>실행 시작 뒤 <paramref name="delay"/>초에 시작합니다.</summary>
        internal static CombatPieceStart AtStart(float delay = 0f) =>
            new(CombatPieceStartKind.AtStart, -1, CombatPieceSignal.End, ValidDelay(delay));

        /// <summary>앞 항목 <paramref name="item"/>의 <paramref name="signal"/> 뒤 <paramref name="delay"/>초에 시작합니다.</summary>
        internal static CombatPieceStart After(int item, CombatPieceSignal signal = CombatPieceSignal.End, float delay = 0f)
        {
            if (item < 0)
                throw new ArgumentOutOfRangeException(nameof(item), "앞 항목 번호는 0 이상이어야 합니다.");
            if (!CombatEnumValues.IsDefined<CombatPieceSignal>((int)signal))
                throw new ArgumentOutOfRangeException(nameof(signal));
            return new CombatPieceStart(CombatPieceStartKind.After, item, signal, ValidDelay(delay));
        }

        private static float ValidDelay(float delay)
        {
            if (delay < 0f || float.IsNaN(delay) || float.IsInfinity(delay))
                throw new ArgumentOutOfRangeException(nameof(delay), "시점 지연은 유한한 0 이상의 값이어야 합니다.");
            return delay;
        }
    }
}
