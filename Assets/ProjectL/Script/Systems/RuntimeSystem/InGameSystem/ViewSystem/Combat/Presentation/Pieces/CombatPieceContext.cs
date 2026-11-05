using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 조각이 실행에게서 받는 맥락입니다(조각·조립 설계 BP1·BP4). 실행이 항목마다 하나씩 만듭니다.
    /// 조각은 이 맥락으로만 대여·세대·Mapper·외부 끝 입구를 얻고, 실행이나 다른 조각을 직접 알지 않습니다.
    /// </summary>
    internal sealed class CombatPieceContext
    {
        private readonly CombatPieceRun _run;
        private readonly int _item;
        private bool _endCallbackIssued;
        private int _issuedSignals;

        internal CombatPieceContext(CombatPieceRun run, int item)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _item = item;
        }

        /// <summary>실행을 시작한 재생 세대입니다.</summary>
        internal int Version => _run.Version;

        /// <summary>지금 연결된 Mapper입니다. 연결 전이면 null입니다.</summary>
        internal FieldGridMapper Mapper => _run.Mapper;

        /// <summary>실행이 빌린 그 유닛의 크기 대여입니다. 조립이 적지 않은 채널이면 예외입니다.</summary>
        internal ICombatUnitScale Scale(EntityId unit) => _run.Leases.Scale(unit);

        /// <summary>실행이 빌린 그 유닛의 자세 대여입니다. 조립이 적지 않은 채널이면 예외입니다.</summary>
        internal ICombatUnitPose Pose(EntityId unit) => _run.Leases.Pose(unit);

        /// <summary>실행이 빌린 그 유닛의 피격 플래시 대여입니다. 조립이 적지 않은 채널이면 예외입니다.</summary>
        internal ICombatUnitFlash Flash(EntityId unit) => _run.Leases.Flash(unit);

        /// <summary>실행이 빌린 그 유닛의 사건 클립 대여입니다. 조립이 적지 않은 채널이면 예외입니다(클립 설계 AC3).</summary>
        internal ICombatUnitClip Clip(EntityId unit) => _run.Leases.Clip(unit);

        /// <summary>실행이 빌린 그 유닛의 서는 자리 대여입니다. 조립이 적지 않은 채널이면 예외입니다(유닛 행동 설계 AA2).</summary>
        internal ICombatUnitStand Stand(EntityId unit) => _run.Leases.Stand(unit);

        /// <summary>실행이 빌린 카메라 줌(장면 채널)입니다. 조립이 적지 않았으면 예외입니다(카메라 연출 조각 설계 K3).</summary>
        internal ICombatCameraZoom CameraZoom() => _run.Leases.CameraZoom();

        /// <summary>
        /// 외부 진행 조각의 끝 입구입니다(설계 BP1). 진행 쪽 완료 콜백으로 넘기면, 실행이 세대를 확인하고 그 자리에서 이 항목의 끝 신호를 냅니다.
        /// 시작 안에서 불러도 됩니다(바로 끝남). 이 입구는 항목 하나에 한 번만 받고, 두 번째로 받으면 예외입니다.
        /// 받은 콜백을 두 번 부르거나 늦게 부르면(지난 세대·끝난 실행) 실행이 무시합니다.
        /// </summary>
        internal Action EndCallback()
        {
            if (_endCallbackIssued)
                throw new InvalidOperationException("[CombatPieceContext] 끝 입구는 항목 하나에 한 번만 받습니다.");
            _endCallbackIssued = true;
            CombatPieceRun run = _run;
            int item = _item;
            int version = _run.Version;
            return () => run.EndExternal(item, version);
        }

        /// <summary>
        /// 끝 말고 다른 신호(예: <see cref="CombatPieceSignal.Hit"/>)를 내는 입구입니다(조각·조립 설계 BP2 보충, 카메라 연출 조각 설계 K4).
        /// 받은 콜백을 부르면 실행이 세대를 확인하고 그 신호를 기다리는 뒤 항목을 시작합니다. 시작 안에서 불러도 됩니다(시작이 끝난 뒤 시작의 남은 시간으로 처리).
        /// 항목·신호마다 한 번만 받고, 두 번째로 받으면 예외입니다. 받은 콜백을 두 번 부르거나 늦게 부르면(지난 세대·끝난 항목) 실행이 무시합니다.
        /// 끝 신호는 실행이 내므로 이 입구로 받지 않습니다(외부 진행 조각은 <see cref="EndCallback"/>).
        /// </summary>
        internal Action SignalCallback(CombatPieceSignal signal)
        {
            if (signal == CombatPieceSignal.End)
                throw new ArgumentException("끝 신호는 실행이 냅니다. 외부 진행 조각은 EndCallback을 씁니다.", nameof(signal));
            if (!CombatEnumValues.IsDefined<CombatPieceSignal>((int)signal))
                throw new ArgumentOutOfRangeException(nameof(signal));
            int bit = 1 << (int)signal;
            if ((_issuedSignals & bit) != 0)
                throw new InvalidOperationException($"[CombatPieceContext] 신호 입구는 항목·신호마다 한 번만 받습니다: {signal}");
            _issuedSignals |= bit;
            CombatPieceRun run = _run;
            int item = _item;
            int version = _run.Version;
            return () => run.Signal(item, signal, version);
        }
    }
}
