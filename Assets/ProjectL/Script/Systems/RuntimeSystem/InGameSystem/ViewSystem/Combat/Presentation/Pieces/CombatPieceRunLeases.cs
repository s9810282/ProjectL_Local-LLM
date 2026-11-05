using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 조각 실행 하나가 빌린 대여 묶음입니다(조각·조립 설계 BP4, 카메라 원칙 평가 C6·C7). 조립이 적은 채널을 실행 시작 때 한 번에 빌리고
    /// (하나라도 못 빌리면 모두 되돌림), 조각 맥락에 꺼내 주며, 실행이 끝나거나 멈출 때 되돌립니다.
    /// 시간 규칙·신호·완료는 실행(<see cref="CombatPieceRun"/>)이 맡고, 이 묶음은 대여만 압니다.
    /// </summary>
    /// <remarks>
    /// 빌린 것은 유닛 채널·장면 채널 모두 한 모양(유닛, 채널, 대여, 되돌리기)으로 기록합니다. 채널 하나는 <see cref="TryBorrowOne"/>의 한 칸
    /// (창구에서 빌리고 그 자리에서 대여와 되돌리기를 기록)과 꺼내기 한 줄이라, 새 유닛 채널은 열거 값 + 한 칸 + 꺼내기 한 줄입니다(대여 분리 계획 §2.1).
    /// 새 장면 채널은 100번대 열거 값(<see cref="CombatComposition.IsSceneChannel"/>이 가름)과 조립의 전용 적기 함수가 더 필요합니다.
    /// 빌리는 순서는 유닛 채널을 조립에 적은 순서대로 먼저, 장면 채널은 그 뒤입니다. 되돌리는 순서는 빌린 순서 그대로입니다(계획 §2.5 R1·R2).
    /// </remarks>
    internal sealed class CombatPieceRunLeases
    {
        // 빌린 것 하나입니다. 장면 채널은 유닛이 비어 있습니다.
        private readonly struct Held
        {
            internal Held(EntityId unit, CombatPieceChannel channel, object lease, Action restore)
            {
                Unit = unit;
                Channel = channel;
                Lease = lease;
                Restore = restore;
            }

            internal EntityId Unit { get; }

            internal CombatPieceChannel Channel { get; }

            internal object Lease { get; }

            internal Action Restore { get; }
        }

        private readonly List<Held> _held = new();
        private readonly CombatUnitPresentationWindow _window;
        private readonly CombatCameraWindow _camera;

        /// <param name="window">유닛 채널을 빌리는 유닛 표시 창구입니다.</param>
        /// <param name="camera">장면 채널(카메라 줌)을 빌리는 카메라 창구입니다(카메라 연출 조각 설계 K3).</param>
        internal CombatPieceRunLeases(CombatUnitPresentationWindow window, CombatCameraWindow camera)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        /// <summary>
        /// 조립이 적은 채널을 빌립니다. 하나라도 못 빌리면 앞서 빌린 것을 되돌리고 false입니다. 빌리다 예외가 나면 되돌린 뒤 다시 던집니다
        /// (되돌리기에도 오류가 있으면 원래 예외를 맨 앞에 둔 <see cref="AggregateException"/>).
        /// </summary>
        /// <param name="cancelled">대여가 밖에서 취소될 때(표시 꺼짐·재대여·맵 카메라 빠짐) 창구가 부르는 콜백입니다.</param>
        internal bool TryBorrow(CombatComposition composition, Action cancelled)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            IReadOnlyList<CombatComposition.LeaseRequest> requests = composition.Leases;
            // 첫 바퀴는 유닛 채널, 둘째 바퀴는 장면 채널입니다(R1). 조립에 장면 채널을 먼저 적어도 순서가 같습니다.
            for (int pass = 0; pass < 2; pass++)
            {
                bool scene = pass == 1;
                for (int i = 0; i < requests.Count; i++)
                {
                    CombatComposition.LeaseRequest request = requests[i];
                    if (CombatComposition.IsSceneChannel(request.Channel) != scene)
                        continue;
                    bool borrowed;
                    try
                    {
                        borrowed = TryBorrowOne(request, cancelled);
                    }
                    catch (Exception error)
                    {
                        List<Exception> cleanup = null;
                        Restore(ref cleanup);
                        if (cleanup == null)
                            throw;
                        cleanup.Insert(0, error);
                        throw new AggregateException(cleanup);
                    }
                    if (!borrowed)
                    {
                        List<Exception> cleanup = null;
                        Restore(ref cleanup);
                        CleanupErrors.ThrowIfAny(cleanup, CombatSceneEventPresenter.CleanupFailureMessage);
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>빌린 그 유닛의 크기 대여입니다.</summary>
        internal ICombatUnitScale Scale(EntityId unit) =>
            (ICombatUnitScale)Find(unit, CombatPieceChannel.Scale)
            ?? throw new InvalidOperationException($"[CombatPieceRun] 조립이 빌리지 않은 크기 채널입니다: {unit}");

        /// <summary>빌린 그 유닛의 자세 대여입니다.</summary>
        internal ICombatUnitPose Pose(EntityId unit) =>
            (ICombatUnitPose)Find(unit, CombatPieceChannel.Pose)
            ?? throw new InvalidOperationException($"[CombatPieceRun] 조립이 빌리지 않은 자세 채널입니다: {unit}");

        /// <summary>빌린 그 유닛의 피격 플래시 대여입니다.</summary>
        internal ICombatUnitFlash Flash(EntityId unit) =>
            (ICombatUnitFlash)Find(unit, CombatPieceChannel.Flash)
            ?? throw new InvalidOperationException($"[CombatPieceRun] 조립이 빌리지 않은 플래시 채널입니다: {unit}");

        /// <summary>빌린 그 유닛의 사건 클립 대여입니다(클립 설계 AC3).</summary>
        internal ICombatUnitClip Clip(EntityId unit) =>
            (ICombatUnitClip)Find(unit, CombatPieceChannel.Clip)
            ?? throw new InvalidOperationException($"[CombatPieceRun] 조립이 빌리지 않은 클립 채널입니다: {unit}");

        /// <summary>빌린 그 유닛의 서는 자리 대여입니다(유닛 행동 설계 AA2).</summary>
        internal ICombatUnitStand Stand(EntityId unit) =>
            (ICombatUnitStand)Find(unit, CombatPieceChannel.Stand)
            ?? throw new InvalidOperationException($"[CombatPieceRun] 조립이 빌리지 않은 서는 자리 채널입니다: {unit}");

        /// <summary>빌린 카메라 줌(장면 채널)입니다(카메라 연출 조각 설계 K3).</summary>
        internal ICombatCameraZoom CameraZoom() =>
            (ICombatCameraZoom)Find(EntityId.Empty, CombatPieceChannel.CameraZoom)
            ?? throw new InvalidOperationException("[CombatPieceRun] 조립이 빌리지 않은 카메라 줌 채널입니다.");

        /// <summary>빌린 것을 빌린 순서대로 되돌리고 비웁니다(유닛 채널 → 장면 채널). 여러 번 불려도 두 번째부터는 아무것도 하지 않습니다.</summary>
        internal void Restore(ref List<Exception> errors)
        {
            for (int i = 0; i < _held.Count; i++)
                CleanupErrors.Try(_held[i].Restore, ref errors);
            _held.Clear();
        }

        /// <summary>
        /// 채널 하나를 빌립니다. 채널마다 한 칸이며, 창구에서 빌린 자리에서 대여와 되돌리기를 함께 기록합니다.
        /// 새 채널은 여기 한 칸과 꺼내기 한 줄을 더합니다. 모르는 채널은 다른 채널로 조용히 빌리지 않게 예외입니다.
        /// </summary>
        private bool TryBorrowOne(CombatComposition.LeaseRequest request, Action cancelled)
        {
            switch (request.Channel)
            {
                case CombatPieceChannel.Scale:
                    if (!_window.TryBeginScale(request.Unit, cancelled, out ICombatUnitScale scale)) return false;
                    Hold(request, scale, scale.RestoreScale);
                    return true;
                case CombatPieceChannel.Pose:
                    if (!_window.TryBeginPose(request.Unit, cancelled, out ICombatUnitPose pose)) return false;
                    Hold(request, pose, pose.RestorePose);
                    return true;
                case CombatPieceChannel.Flash:
                    if (!_window.TryBeginFlash(request.Unit, cancelled, out ICombatUnitFlash flash)) return false;
                    Hold(request, flash, flash.RestoreFlash);
                    return true;
                case CombatPieceChannel.Clip:
                    if (!_window.TryBeginClip(request.Unit, cancelled, out ICombatUnitClip clip)) return false;
                    Hold(request, clip, clip.RestoreClip);
                    return true;
                case CombatPieceChannel.Stand:
                    if (!_window.TryBeginStand(request.Unit, cancelled, out ICombatUnitStand stand)) return false;
                    Hold(request, stand, stand.RestoreStand);
                    return true;
                case CombatPieceChannel.CameraZoom:
                    if (!_camera.TryBeginZoom(cancelled, out ICombatCameraZoom zoom)) return false;
                    Hold(request, zoom, zoom.RestoreZoom);
                    return true;
                default:
                    throw new InvalidOperationException($"[CombatPieceRun] 대여 방법을 모르는 채널입니다: {request.Channel}");
            }
        }

        private void Hold(CombatComposition.LeaseRequest request, object lease, Action restore) =>
            _held.Add(new Held(request.Unit, request.Channel, lease, restore));

        private object Find(EntityId unit, CombatPieceChannel channel)
        {
            for (int i = 0; i < _held.Count; i++)
                if (_held[i].Channel == channel && _held[i].Unit == unit)
                    return _held[i].Lease;
            return null;
        }
    }
}
