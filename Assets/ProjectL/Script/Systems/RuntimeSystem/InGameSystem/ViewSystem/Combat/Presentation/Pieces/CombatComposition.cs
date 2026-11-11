using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 실행이 빌리는 채널입니다(조각·조립 설계 BP4). 유닛 채널은 유닛 표시 창구에서 유닛마다, 장면 채널은 장면에 하나인 창구에서 빌립니다.
    /// 유닛 채널은 100 아래, 장면 채널은 100번대에 둡니다(대여 분리 계획 §2.4). 새 유닛 채널은 다음 빈 번호를 씁니다.
    /// </summary>
    internal enum CombatPieceChannel
    {
        /// <summary>크기 배율(<see cref="ICombatUnitScale"/>)입니다.</summary>
        Scale = 1,

        /// <summary>위치·회전 자세(<see cref="ICombatUnitPose"/>)입니다.</summary>
        Pose = 2,

        /// <summary>피격 플래시 외형 층(<see cref="ICombatUnitFlash"/>)입니다(설계 BP9).</summary>
        Flash = 3,

        /// <summary>사건 클립(<see cref="ICombatUnitClip"/>)입니다(클립 설계 AC3). 모델 자식만 움직여 자세·크기 채널과 함께 빌릴 수 있습니다.</summary>
        Clip = 4,

        /// <summary>
        /// 특성 발동 유닛 행동이 옮긴 서는 자리(<see cref="ICombatUnitStand"/>, 유닛 행동 설계 AA2)입니다. 자세 채널의 아래 층이라 같은 유닛의 피격 반응이
        /// 이 위에서 자세를 빌릴 수 있습니다. 걸음 배치 중인 유닛은 빌릴 수 없습니다.
        /// </summary>
        Stand = 5,

        /// <summary>장면 채널: 맵 카메라 줌(<see cref="ICombatCameraZoom"/>, 카메라 연출 조각 설계 K3)입니다. 유닛이 없고 <see cref="CombatComposition.LeaseCameraZoom"/>으로만 적습니다.</summary>
        CameraZoom = 101,
    }

    /// <summary>실행이 무엇을 막는지입니다(조각·조립 설계 BP4).</summary>
    internal enum CombatPieceScope
    {
        /// <summary>행동 실행: 진행 중 모든 사건을 막고, 출처가 겹침을 허용할 때만 엽니다. 포효·소환·클립/코드 모션 공격·낙석 충돌·하붑·위치 교환·진입 등장 등이 씁니다.</summary>
        Action = 1,

        /// <summary>유닛 실행(피격·사망): 주체 유닛의 사건, 주체 없는 사건, 행동 사건을 막습니다.</summary>
        Unit = 2,

        /// <summary>
        /// 곁들임 실행(사실 사건의 순간 큐, 이펙트 창구 확장 설계 EC6·계획 P7): 아무 사건도 막지 않고 채널을 빌리지 않으며, 사건 완료를 알리지 않습니다.
        /// 다만 재생 끝 판정에는 들어가므로 남은 지연 항목만큼 기차 사건과 결과 끝이 기다립니다(설계 §7 F4). 여럿이 함께 진행할 수 있습니다.
        /// </summary>
        Accent = 3,
    }

    /// <summary>
    /// 조립의 사건 완료 방식입니다(조각·조립 설계 BP4). 일반 완료는 기다림 항목이 모두 끝날 때 한 번 알리고,
    /// 출처 완료는 출처 수명(<see cref="ICombatSourceLifetime"/>)을 열어 Gate 정책으로 알립니다.
    /// </summary>
    internal readonly struct CombatPieceCompletion
    {
        private CombatPieceCompletion(CombatEvent source, EntityId actor, CombatHitCompletion policy, bool waitsForAllItems = false)
        {
            Source = source;
            Actor = actor;
            Policy = policy;
            WaitsForAllItems = waitsForAllItems;
        }

        /// <summary>출처 완료인지입니다.</summary>
        internal bool IsSource => Source != null;

        /// <summary>
        /// 일반 완료를 기다림 항목이 아니라 모든 항목이 끝날 때(전체 종료) 알리는지입니다(<see cref="AllItems"/>). 일반 완료(<see cref="General"/>)는 거짓입니다.
        /// </summary>
        internal bool WaitsForAllItems { get; }

        /// <summary>출처 사건입니다. 일반 완료면 null입니다.</summary>
        internal CombatEvent Source { get; }

        /// <summary>출처의 행위자(Gate 규칙 1의 "공격자 자신")입니다.</summary>
        internal EntityId Actor { get; }

        /// <summary>출처 사건 완료 정책입니다.</summary>
        internal CombatHitCompletion Policy { get; }

        /// <summary>일반 완료입니다. 기다림 항목이 없으면 실행 시작 즉시 알립니다.</summary>
        internal static CombatPieceCompletion General => default;

        /// <summary>
        /// 모든 항목 완료입니다. 기다림 여부와 상관없이 모든 항목이 끝나 대여를 되돌린 뒤 한 번 알립니다. 사건 완료 뒤까지 실행이 대여를 쥐면 안 되는 조립이 씁니다 —
        /// 터질 유닛의 깜빡임은 다음 사건(자폭 발동)이 그 몸 대여를 거두므로, 지연 큐가 남아 있어도 사건이 그보다 먼저 끝나면 안 됩니다(자폭 깜빡임 묶음 끝 검토 F1).
        /// </summary>
        internal static CombatPieceCompletion AllItems => new(null, EntityId.Empty, default, waitsForAllItems: true);

        /// <summary>출처 완료입니다. 실행이 시작 때 출처 수명을 열고, 전체 종료 때 닫고, 취소 때 비웁니다.</summary>
        internal static CombatPieceCompletion FromSource(CombatEvent source, EntityId actor, CombatHitCompletion policy)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!CombatEnumValues.IsDefined<CombatHitCompletion>((int)policy))
                throw new ArgumentOutOfRangeException(nameof(policy));
            return new CombatPieceCompletion(source, actor, policy);
        }
    }

    /// <summary>
    /// 사건 하나의 조립입니다(조각·조립 설계 BP3). 항목(조각·시점·기다림) 목록과 실행 범위, 완료 방식, 실행이 빌릴 채널을 가집니다.
    /// 조립은 처리기 코드가 사건마다 만들고, 조각 실행 재생기(<see cref="CombatPieceRunPlayer.TryBegin"/>)에 한 번만 넘깁니다.
    /// </summary>
    internal sealed class CombatComposition
    {
        /// <summary>항목 하나입니다.</summary>
        internal readonly struct Item
        {
            internal Item(ICombatPresentationPiece piece, CombatPieceStart start, bool waits)
            {
                Piece = piece;
                Start = start;
                Waits = waits;
            }

            internal ICombatPresentationPiece Piece { get; }

            internal CombatPieceStart Start { get; }

            /// <summary>일반 완료가 이 항목의 끝을 기다리는지입니다.</summary>
            internal bool Waits { get; }
        }

        /// <summary>실행이 빌릴 채널 하나입니다.</summary>
        internal readonly struct LeaseRequest
        {
            internal LeaseRequest(EntityId unit, CombatPieceChannel channel)
            {
                Unit = unit;
                Channel = channel;
            }

            internal EntityId Unit { get; }

            internal CombatPieceChannel Channel { get; }
        }

        private readonly List<Item> _items = new();
        private readonly List<LeaseRequest> _leases = new();
        private bool _used;

        private CombatComposition(CombatPieceScope scope, EntityId subject, bool isDeath, CombatPieceCompletion completion, bool pausesOtherPlayers = true)
        {
            Scope = scope;
            Subject = subject;
            IsDeath = isDeath;
            Completion = completion;
            PausesOtherPlayers = pausesOtherPlayers;
        }

        internal CombatPieceScope Scope { get; }

        /// <summary>행동 실행이 진행하는 동안 뒤 재생기(걸음·반응)의 Tick을 멈추는지입니다. 다른 행동 실행과 달리 필드 행동(<see cref="ForFieldAction"/>)은 멈추지 않습니다.</summary>
        internal bool PausesOtherPlayers { get; }

        /// <summary>유닛 실행의 주체입니다. 행동 실행이면 비어 있습니다.</summary>
        internal EntityId Subject { get; }

        /// <summary>유닛 실행이 사망 실행인지입니다. 동시 사망 프로필에서 사망 실행끼리만 겹칩니다(설계 B14 O14).</summary>
        internal bool IsDeath { get; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>전체 사건을 갖지 않은 유닛 조립의 종류입니다. 기존 사건 형식 표로만 분류합니다.</summary>
        internal Type ProfilingEventType { get; set; }
        /// <summary>조립의 원본 사건입니다. 계측 외의 실행·완료 판정에는 쓰지 않습니다.</summary>
        internal CombatEvent ProfilingEvent { get; set; }
#endif
        internal CombatPieceCompletion Completion { get; }

        internal IReadOnlyList<Item> Items => _items;

        /// <summary>실행이 빌릴 채널 목록입니다. 유닛 채널과 장면 채널이 적은 순서대로 함께 들어 있습니다(빌리는 순서는 대여 묶음이 정함).</summary>
        internal IReadOnlyList<LeaseRequest> Leases => _leases;

        /// <summary>실행이 장면 채널인 카메라 줌을 빌리는지입니다(카메라 연출 조각 설계 K3). 채널 목록에서 파생합니다.</summary>
        internal bool LeasesCameraZoom
        {
            get
            {
                for (int i = 0; i < _leases.Count; i++)
                    if (_leases[i].Channel == CombatPieceChannel.CameraZoom)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// 유닛이 없는 장면 채널인지입니다. 장면 채널을 가르는 곳은 이 함수 하나이고, 규칙은 "100번대"입니다(대여 분리 계획 §2.4).
        /// 새 장면 채널은 100번대 값만 주면 이 판정에 들어갑니다.
        /// </summary>
        internal static bool IsSceneChannel(CombatPieceChannel channel) => (int)channel >= SceneChannelStart;

        // 장면 채널 번호의 시작입니다. 유닛 채널은 그 아래 번호를 씁니다.
        private const int SceneChannelStart = 100;

        /// <summary>행동 실행의 조립을 시작합니다(쓰는 곳은 <see cref="CombatPieceScope.Action"/> 설명).</summary>
        internal static CombatComposition ForAction(CombatPieceCompletion completion) =>
            new(CombatPieceScope.Action, EntityId.Empty, false, completion);

        /// <summary>
        /// 유닛 연출을 기다리지 않고 함께 진행하는 필드 행동(움직이는 토네이도)의 조립을 시작합니다. 행동 실행이라 다음 사건은 막지만,
        /// 진행 중인 걸음·반응은 멈추지 않습니다(토네이도 View 설계 TN10).
        /// </summary>
        internal static CombatComposition ForFieldAction() =>
            new(CombatPieceScope.Action, EntityId.Empty, false, CombatPieceCompletion.General, pausesOtherPlayers: false);

        /// <summary>유닛 실행(피격·사망)의 조립을 시작합니다. 주체가 있어야 합니다.</summary>
        internal static CombatComposition ForUnit(EntityId subject, bool isDeath, CombatPieceCompletion completion)
        {
            if (subject.IsEmpty)
                throw new ArgumentException("유닛 실행에는 주체가 있어야 합니다.", nameof(subject));
            return new CombatComposition(CombatPieceScope.Unit, subject, isDeath, completion);
        }

        /// <summary>곁들임 실행(사실 사건의 순간 큐)의 조립을 시작합니다. 일반 완료이고, 실행은 완료 통지 없이 시작합니다(계획 P7).</summary>
        internal static CombatComposition ForAccent() =>
            new(CombatPieceScope.Accent, EntityId.Empty, false, CombatPieceCompletion.General);

        /// <summary>
        /// 항목이 하나도 없는지입니다. 붙일 큐가 모두 걸러지면(흔들림 수신기 없음·세기 0·자리 못 찾음) 빈 조립이 되고, 빈 조립은 실행할 수 없어
        /// 내는 곳이 이 값을 보고 시작하지 않습니다(이펙트 창구 확장 계획 P17, 설계 §7 F1).
        /// </summary>
        internal bool IsEmpty => _items.Count == 0;

        /// <summary>
        /// 이 유닛 실행이 주체의 밀림 걸음(넉백 등 밀린 이동)을 막지 않는지입니다(넉백 피격 연출, 임시 — 2026-11-11 사용자 "피격과 동시에 밀림").
        /// 밀림과 함께 가는 피격(자세를 빌리지 않아 걸음 배치와 겹칠 수 있음)만 켭니다. 피격과 밀림 사이에 끼는 그 주체의 특성 발동·상태 사건도 막지 않고,
        /// 그 주체의 다른 캐릭터 사건(피격·사망 등)과 행동·출처를 여는 사건은 이 실행이 끝날 때까지 기다립니다(판정은 조각 실행 재생기 <c>PassesPushAlongsideHit</c>).
        /// </summary>
        internal bool AllowsSubjectPush { get; private set; }

        /// <summary>이 유닛 실행이 주체의 밀림 걸음을 막지 않게 합니다(<see cref="AllowsSubjectPush"/>). 자세 채널을 빌리는 실행은 거절합니다.</summary>
        internal CombatComposition AllowSubjectPush()
        {
            RequireOpen();
            if (Scope != CombatPieceScope.Unit)
                throw new InvalidOperationException("[CombatComposition] 밀림 걸음과 겹치는 것은 유닛 실행만 됩니다.");
            for (int i = 0; i < _leases.Count; i++)
            {
                if (_leases[i].Channel == CombatPieceChannel.Pose)
                    throw new InvalidOperationException("[CombatComposition] 자세를 빌리는 실행은 밀림 걸음과 겹칠 수 없습니다(걸음 배치와 함께 못 씀).");
            }
            AllowsSubjectPush = true;
            return this;
        }

        /// <summary>실행이 시작할 때 빌릴 유닛 채널을 적습니다. 같은 유닛·채널은 한 번만 적고, 장면 채널은 거절합니다(전용 함수로).</summary>
        internal CombatComposition Lease(EntityId unit, CombatPieceChannel channel)
        {
            RequireOpen();
            RequireLeasable();
            if (unit.IsEmpty)
                throw new ArgumentException("빌릴 채널의 유닛이 비어 있습니다.", nameof(unit));
            if (!CombatEnumValues.IsDefined<CombatPieceChannel>((int)channel))
                throw new ArgumentOutOfRangeException(nameof(channel));
            if (IsSceneChannel(channel))
                throw new ArgumentException($"[CombatComposition] 장면 채널은 전용 함수로 빌립니다: {channel}", nameof(channel));
            if (AllowsSubjectPush && channel == CombatPieceChannel.Pose)
                throw new InvalidOperationException("[CombatComposition] 밀림 걸음과 겹치는 실행은 자세를 빌릴 수 없습니다.");
            for (int i = 0; i < _leases.Count; i++)
            {
                if (_leases[i].Unit == unit && _leases[i].Channel == channel)
                    throw new InvalidOperationException($"[CombatComposition] 같은 채널을 두 번 빌릴 수 없습니다: {unit} {channel}");
            }
            _leases.Add(new LeaseRequest(unit, channel));
            return this;
        }

        /// <summary>
        /// 실행이 시작할 때 카메라 줌(장면 채널)을 빌리게 적습니다(카메라 연출 조각 설계 K3). 유닛 없는 장면 채널이라 전용 함수로 적고, 채널 목록에는 장면 채널 요청(유닛 비어 있음)으로 들어갑니다.
        /// 못 빌리면 실행이 시작하지 않으므로, 처리기는 <see cref="CombatCameraWindow.CanZoom"/>을 보고 줌 항목과 함께 넣거나 뺍니다(결정 Q-K6 A).
        /// </summary>
        internal CombatComposition LeaseCameraZoom()
        {
            RequireOpen();
            RequireLeasable();
            if (LeasesCameraZoom)
                throw new InvalidOperationException("[CombatComposition] 카메라 줌은 한 번만 빌릴 수 있습니다.");
            _leases.Add(new LeaseRequest(EntityId.Empty, CombatPieceChannel.CameraZoom));
            return this;
        }

        /// <summary>
        /// 항목을 더하고 그 번호를 돌려줍니다. <see cref="CombatPieceStartKind.After"/>는 앞 항목만 가리킬 수 있습니다(순환 없음).
        /// 기다림은 일반 완료에만 씁니다. 출처 완료는 Gate가 정합니다.
        /// </summary>
        internal int Add(ICombatPresentationPiece piece, CombatPieceStart start, bool waits = false)
        {
            RequireOpen();
            if (piece == null) throw new ArgumentNullException(nameof(piece));
            if (!CombatEnumValues.IsDefined<CombatPieceStartKind>((int)start.Kind))
                throw new ArgumentException("항목의 시점이 없습니다. CombatPieceStart.AtStart·After로 만듭니다.", nameof(start));
            if (start.Kind == CombatPieceStartKind.After && start.Item >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(start), $"앞 항목만 가리킬 수 있습니다: {start.Item}(항목 {_items.Count}개)");
            if (waits && Completion.IsSource)
                throw new InvalidOperationException("[CombatComposition] 출처 완료 조립의 항목은 기다리지 않습니다. 완료는 Gate 정책이 정합니다.");
            for (int i = 0; i < _items.Count; i++)
            {
                if (ReferenceEquals(_items[i].Piece, piece))
                    throw new InvalidOperationException("[CombatComposition] 같은 조각을 두 항목에 넣을 수 없습니다.");
            }
            _items.Add(new Item(piece, start, waits));
            return _items.Count - 1;
        }

        /// <summary>실행이 시작할 때 한 번 부릅니다. 조각은 상태를 가지므로 같은 조립을 두 번 실행하지 않습니다.</summary>
        internal void MarkUsed()
        {
            if (_used)
                throw new InvalidOperationException("[CombatComposition] 조립은 한 번만 실행합니다.");
            if (_items.Count == 0)
                throw new InvalidOperationException("[CombatComposition] 항목이 없는 조립은 실행할 수 없습니다.");
            _used = true;
        }

        private void RequireOpen()
        {
            if (_used)
                throw new InvalidOperationException("[CombatComposition] 실행을 시작한 조립은 바꿀 수 없습니다.");
        }

        // 곁들임 실행은 채널을 빌리지 않습니다(이펙트·흔들림만이라 필요 없음, 계획 P7). 빌리면 다른 실행과 자원을 다퉈 막지 않는다는 약속이 깨집니다.
        private void RequireLeasable()
        {
            if (Scope == CombatPieceScope.Accent)
                throw new InvalidOperationException("[CombatComposition] 곁들임 실행은 채널을 빌리지 않습니다.");
        }
    }
}
