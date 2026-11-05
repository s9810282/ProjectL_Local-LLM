using System;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 이펙트 하나를 내는 시간 없는 조각입니다(이펙트 설계 EF2). 사건 소리·흔들림 조각과 같은 모양으로, 시작에서 내고 바로 끝납니다.
    /// 내는 일은 창구(<see cref="CombatEffectWindow"/>)가 하고, 자리는 만드는 함수가 정합니다(피격 이펙트 설계 HE4):
    /// 공격 클립 이펙트는 <see cref="AtClipPoint"/>(시작 때 그 유닛 클립 대여의 이펙트 지점), 정해진 월드 자리는 <see cref="At"/>입니다.
    /// 따라갈지·언제 끝나나는 큐의 놓기 값(<see cref="CombatEffectPlacement.ForCue"/>)입니다(이펙트 끝 규칙 설계 EL1·EL5).
    /// </summary>
    /// <remarks>
    /// 이펙트가 끝날 때까지 실행을 붙잡지 않습니다. 조각이 취소돼도 낸 이펙트는 그대로 갑니다(끝은 큐의 끝 규칙).
    /// 남은 이펙트의 회수는 결과 재생 취소·해제 때 창구가 합니다(결정 Q-E4 A).
    /// </remarks>
    internal sealed class CombatEffectPiece : ICombatPresentationPiece
    {
        private readonly CombatEffectWindow _effects;
        private readonly EffectType _effect;
        private readonly Func<CombatPieceContext, Pose> _at;
        // 놓기 값(따라갈 유닛·끝)입니다(설계 EL5).
        private readonly CombatEffectPlacement _placement;
        // 행동 끝 큐의 발동 묶음입니다(계획 TP-E3). 없으면 결과 끝에 창구가 거둡니다(계획 TP-E4).
        private readonly CombatEffectActionGroup _group;

        private CombatEffectPiece(CombatEffectWindow effects, EffectType effect, Func<CombatPieceContext, Pose> at, CombatEffectPlacement placement,
            CombatEffectActionGroup group = null)
        {
            _group = group;
            if (effect == EffectType.Empty)
                throw new ArgumentOutOfRangeException(nameof(effect), effect, "낼 이펙트가 없습니다.");
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _effect = effect;
            _at = at;
            _placement = placement;
        }

        /// <summary>
        /// 시작 때 그 유닛 클립 대여의 이펙트 지점(위치·회전)에 냅니다. 조립이 그 유닛의 클립 채널을 적어야 합니다.
        /// 지점은 시작 순간에만 읽을 수 있어 큐의 위치·회전 보정도 이 조각이 그때 적용합니다(이펙트 창구 확장 계획 P3).
        /// </summary>
        /// <param name="unit">클립 채널을 빌린 유닛입니다.</param>
        /// <param name="cue">낼 이펙트 큐입니다. 이펙트가 비어 있으면 안 됩니다(없는 칸은 조립이 항목을 넣지 않음).</param>
        internal static CombatEffectPiece AtClipPoint(EntityId unit, CombatEffectWindow effects, CombatEffectCue cue)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("이펙트를 낼 유닛이 비어 있습니다.", nameof(unit));
            if (cue == null) throw new ArgumentNullException(nameof(cue));
            return new CombatEffectPiece(effects, cue.Effect, context =>
            {
                if (!context.Clip(unit).TryGetEffectPoint(cue.ActorPoint, out Vector3 position, out Quaternion rotation))
                    throw new InvalidOperationException(
                        $"[CombatEffectPiece] 공격 이펙트 지점 {cue.ActorPoint}가 없습니다(외형 표 검사가 막아야 함): {unit}");
                return CombatCuePoints.Apply(new Pose(position, rotation), cue, effects.AdditionalWorldOffsetOf(unit, cue.RiderYOffset));
            }, CombatEffectPlacement.ForCue(cue, EntityId.Empty));
        }

        /// <summary>
        /// 클립 채널을 빌린 유닛의 발사 지점에서 내는 이펙트입니다(범용 연출 구조 설계 G3 자리 <c>ActorMuzzle</c>). 회전은 발사 지점이 위치만 주므로
        /// 같은 실행이 빌린 그 유닛의 자세 방향입니다(클립 공격 조립은 자세·클립을 함께 빌림, 계획 Gd.5에서 정함). 보정은 시작 순간에 이 조각이 적용합니다.
        /// </summary>
        internal static CombatEffectPiece AtClipMuzzle(EntityId unit, CombatEffectWindow effects, CombatEffectCue cue)
        {
            if (unit.IsEmpty)
                throw new ArgumentException("이펙트를 낼 유닛이 비어 있습니다.", nameof(unit));
            if (cue == null) throw new ArgumentNullException(nameof(cue));
            return new CombatEffectPiece(effects, cue.Effect, context =>
            {
                if (!context.Clip(unit).TryGetMuzzle(cue.ActorPoint, out Vector3 position))
                    throw new InvalidOperationException(
                        $"[CombatEffectPiece] 공격 총구 지점 {cue.ActorPoint}가 없습니다(외형 표 검사가 막아야 함): {unit}");
                return CombatCuePoints.Apply(new Pose(position, context.Pose(unit).Rotation), cue, effects.AdditionalWorldOffsetOf(unit, cue.RiderYOffset));
            }, CombatEffectPlacement.ForCue(cue, EntityId.Empty));
        }

        /// <summary>
        /// 정해진 월드 자리에 냅니다. 채널을 빌리지 않습니다. 보정은 부르는 쪽(<see cref="CombatCuePoints"/>)이 이미 적용한 자리입니다.
        /// 몸 자리면 <paramref name="unit"/>에 몸의 유닛을 넘기고, 따라갈지는 큐가 정합니다(<see cref="CombatEffectCue.FollowsUnit"/>, 이펙트 따라가기 설계 FE1).
        /// </summary>
        /// <param name="unit">몸 자리의 유닛입니다. 칸 자리는 비웁니다.</param>
        /// <param name="group">행동 끝 큐면 그 발동의 행동 묶음입니다(특성 발동 큐만). 없으면 결과 끝에 거둡니다.</param>
        internal static CombatEffectPiece At(CombatEffectWindow effects, CombatEffectCue cue, Pose at, EntityId unit, CombatEffectActionGroup group = null)
        {
            if (cue == null) throw new ArgumentNullException(nameof(cue));
            return new CombatEffectPiece(effects, cue.Effect, _ => at, CombatEffectPlacement.ForCue(cue, unit), group);
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            IsEnded = true;
            // 지연 때문에 행동이 끝난 뒤에 온 행동 끝 큐는 내지 않습니다(계획 TP-E27).
            if (_placement.ActionBound && _group != null && _group.IsEnded)
                return;
            CombatEffectHandle handle = _effects.Play(_effect, _at(context), _placement);
            if (_placement.ActionBound)
                _group?.Hold(handle);
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}
