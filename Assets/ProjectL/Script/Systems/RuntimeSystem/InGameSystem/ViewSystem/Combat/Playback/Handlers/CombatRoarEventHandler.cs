using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 포효 사건의 표시입니다(Presenter 분리 설계 S3, 보스 활성화 설계 D6b). 강조 → 대기 → 보스 커짐 → 커진 채 공격 모션 → 작아짐 → 강조 끔.
    /// 공격 모션의 타격 순간이 히트이며, 그때 포효 사건 완료를 알려 피해 반응·넉백이 작아지기와 겹쳐 시작합니다(히트 타이밍 설계 §6 Q1 재결정).
    /// 순서는 포효 조립(<see cref="Compose"/>) 한 곳이 원본이고, 조각 실행 재생기가 진행합니다(조각·조립 설계 BP7).
    /// 보스 표시가 없으면 크기를 빌리지 못해 시작하지 않고, 틀이 강조를 보인 채 행동 간격만큼 머문 뒤 완료합니다.
    /// </summary>
    internal sealed class CombatRoarEventHandler : CombatEventHandler
    {
        private readonly CombatPieceRunPlayer _pieces;
        private readonly CombatAbilityMotionSettings _abilityMotion;
        private readonly CombatAttackPlayback _attacks;
        private readonly Func<FieldGridMapper> _mapper;
        private readonly CombatPlaybackHighlight _highlight;
        private readonly CombatShownFacts _facts;
        private readonly CombatEventAudio _sound;
        private readonly CombatCameraWindow _camera;
        private readonly CombatCameraMotionSettings _cameraMotion;
        // 포효 결과의 가로채기를 경고했는지입니다. 연결 동안 한 번만 남깁니다.
        private bool _interceptionWarned;

        /// <param name="camera">맵 카메라 창구입니다(필수). 줌·흔들림을 낼 수 없으면(맵 카메라·수신기 없음) 포효에 넣지 않습니다.</param>
        /// <param name="cameraMotion">카메라 연출 값입니다(필수). "연출 없음"은 <see cref="CombatCameraMotionSettings.None"/>입니다.</param>
        internal CombatRoarEventHandler(CombatPieceRunPlayer pieces, CombatAbilityMotionSettings abilityMotion, CombatAttackPlayback attacks,
            Func<FieldGridMapper> mapper, CombatPlaybackHighlight highlight, CombatShownFacts facts, CombatEventAudio sound,
            CombatCameraWindow camera, CombatCameraMotionSettings cameraMotion)
        {
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            _abilityMotion = abilityMotion ?? throw new ArgumentNullException(nameof(abilityMotion));
            _attacks = attacks ?? throw new ArgumentNullException(nameof(attacks));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _highlight = highlight ?? throw new ArgumentNullException(nameof(highlight));
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _cameraMotion = cameraMotion ?? throw new ArgumentNullException(nameof(cameraMotion));
        }

        /// <summary>
        /// 반응 공격의 영향 칸, 대상이 없으면 Core가 준 포효 범위 칸(<see cref="CombatRoarEvent.AreaCells"/>), 그것도 없으면 보스 칸입니다.
        /// </summary>
        internal override void CollectHighlight(CombatEvent e, List<CellCoord> cells)
        {
            var roar = (CombatRoarEvent)e;
            if (roar.Attack.HasValue)
                foreach (CellCoord cell in roar.Attack.Value.AffectedCells.Span)
                    cells.Add(cell);
            if (cells.Count == 0)
                cells.AddRange(roar.AreaCells);
            if (cells.Count == 0)
                CombatPlaybackHighlight.AddOccupiedCells(cells, _facts.FindCharacter(roar.BossId));
        }

        /// <summary>
        /// 포효 조립을 실행합니다. 공격 모션은 보스가 지금 보는 방향으로 재생합니다(전체 지정은 결과 방향이 없음). 외형에 공격 모션이 없으면 커졌다가 바로 작아집니다.
        /// 보스 표시가 없으면 false입니다. 시작 실패 정리(출처 비우기·크기 되돌림)는 실행이 맡습니다.
        /// 포효 결과에 가로채기(대신 맞기)가 있으면 경고를 한 번 남기고 수호자 행동은 넣지 않습니다(유닛 행동 설계 P9 — 지금 Core는 포효에서 희생을 부르지 않음).
        /// </summary>
        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version)
        {
            var roar = (CombatRoarEvent)e;
            if (!_interceptionWarned && roar.Attack.HasValue && CombatAttackShape.HasInterception(roar.Attack.Value.TraitActivations.Span))
            {
                _interceptionWarned = true;
                UnityEngine.Debug.LogWarning($"[CombatRoarEventHandler] 포효 결과에 대신 맞기(가로채기)가 있습니다. 포효에는 수호자 행동을 넣지 않고 제자리에서 맞습니다: {roar.BossId}");
            }
            CombatComposition composition = Compose(roar, _abilityMotion,
                // 공격 사건 없는 정면 공격입니다. 박치기 = 포효 출처의 대상 모두 해제 → 타격 신호(범용 연출 구조 계획 P8, 입구 하나).
                (end, hit) => _attacks.TryBeginMotion(roar.BossId, null, _mapper(), () => _sound.Play(roar),
                    CombatHitMoment.ReleaseThenSignal(() => _attacks.ReleaseAll(roar, version), hit), end, version),
                _highlight, _camera, _cameraMotion);
            return _pieces.TryBegin(composition, completed, version);
        }

        /// <summary>
        /// 포효 조립입니다(조각·조립 설계 BP7, 카메라 연출 조각 설계 K6). 행동 실행, 출처 완료(포효, 보스, 첫 해제), 자원은 보스 크기(와 줌을 넣으면 카메라 줌)입니다.
        /// 커짐(대기 뒤) → 정면 공격(외부 진행, 박치기 = 대상 모두 해제) → 작아짐 → 강조 끄기. 전체 종료 때 크기·줌을 되돌리고 출처를 닫습니다.
        /// 카메라가 있으면 커짐과 함께 줌인, 박치기가 맞는 순간(<see cref="CombatPieceSignal.Hit"/>) 흔들림, 작아짐과 함께 줌 복귀를 넣습니다.
        /// 줌은 맥박 절반이라 크기와 같은 프레임에 끝나고, 카메라 항목은 기다리지 않습니다(완료는 Gate가 정함).
        /// </summary>
        /// <param name="tryBeginFacing">정면 공격 입구입니다. 인자는 (모션 완료 콜백, 타격 순간 콜백)이고, 시작하면 true, 시작하지 않았으면 false입니다.</param>
        /// <param name="camera">맵 카메라 창구입니다. 없거나 줌·흔들림을 낼 수 없으면(빌린 줌이 있음·수신기 없음) 그 항목을 넣지 않습니다(결정 Q-K6 A·Q-K3 B).</param>
        /// <param name="cameraMotion">카메라 연출 값입니다. 없으면 카메라 항목을 넣지 않습니다.</param>
        internal static CombatComposition Compose(CombatRoarEvent roar, CombatAbilityMotionSettings settings, Func<Action, Action, bool> tryBeginFacing,
            CombatPlaybackHighlight highlight, CombatCameraWindow camera = null, CombatCameraMotionSettings cameraMotion = null)
        {
            if (roar == null) throw new ArgumentNullException(nameof(roar));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            bool zoom = camera != null && cameraMotion != null && cameraMotion.HasRoarZoom && camera.CanZoom;
            bool shake = camera != null && cameraMotion != null && cameraMotion.HasRoarShake && camera.CanShake;
            var composition = CombatComposition.ForAction(
                    CombatPieceCompletion.FromSource(roar, roar.BossId, CombatHitCompletion.FirstRelease))
                .Lease(roar.BossId, CombatPieceChannel.Scale);
            if (zoom)
                composition.LeaseCameraZoom();
            float half = settings.RoarPulseSeconds * .5f;
            int grow = composition.Add(new CombatScalePiece(roar.BossId, 1f, settings.RoarPeakScale, half),
                CombatPieceStart.AtStart(settings.RoarDelaySeconds));
            if (zoom)
                composition.Add(new CombatCameraZoomPiece(cameraMotion.RoarZoomRatio, half), CombatPieceStart.AtStart(settings.RoarDelaySeconds));
            int facing = composition.Add(new CombatFacingAttackPiece(tryBeginFacing), CombatPieceStart.After(grow));
            if (shake)
                composition.Add(new CombatCameraShakePiece(camera, cameraMotion.RoarShake), CombatPieceStart.After(facing, CombatPieceSignal.Hit));
            int shrink = composition.Add(new CombatScalePiece(roar.BossId, settings.RoarPeakScale, 1f, half), CombatPieceStart.After(facing));
            // 줌 복귀는 작아짐 뒤 자리에 둡니다. 같은 실행의 조각을 목록 순서로 진행하므로 작아짐과 같은 프레임에 끝납니다.
            if (zoom)
                composition.Add(new CombatCameraZoomPiece(1f, half), CombatPieceStart.After(facing));
            composition.Add(new CombatHighlightClearPiece(highlight), CombatPieceStart.After(shrink));
            return composition;
        }
    }
}
