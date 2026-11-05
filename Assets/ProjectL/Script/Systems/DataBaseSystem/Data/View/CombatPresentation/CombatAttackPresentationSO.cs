using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 유닛들이 함께 참조하는 공격 표시 묶음입니다. 투사체 부품과 코드 모션 부품을 참조하고, 종류는 투사체 부품의 타입에서 나옵니다
    /// (범용 연출 구조 설계 G4, 결정 Q-G4 A — 부품 없음 = 근접, 직선 투사체 = 직선, 곡사 투사체 = 곡사). 규칙이나 런타임 상태는 포함하지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatAttackPresentation", menuName = "ProjectL/Database/View/Combat Attack Presentation")]
    internal sealed class CombatAttackPresentationSO : ScriptableObject
    {
        [Header("전달")]
        [Tooltip("투사체 부품입니다. 직선(Straight)·곡사(Arc) 투사체 SO를 넣습니다. 비우면 근접(투사체 없음)입니다.")]
        [SerializeField] private CombatProjectileSO projectile;

        [Header("코드 모션")]
        [Tooltip("공격 클립이 없는 유닛의 공격과 보스 포효의 정면 공격이 쓰는 몸 동작입니다. 근접 = Melee, 직선 = Shoot, 곡사 = Mortar 부품이어야 합니다. "
            + "비우면 코드 모션 공격·정면 공격이 시작하지 않습니다(그 유닛은 공격 클립으로만 공격합니다).")]
        [SerializeField] private CombatCodeMotionSO codeMotion;

        [Header("큐")]
        [Tooltip("공격 칸의 연출 큐입니다(이펙트·흔들림·소리 시점). 착탄·몸 접촉 이펙트는 이펙트 큐의 시점·자리를 Contact로 둡니다(원거리만, 지연 0). "
            + "Cue2·Cue3는 해당 이벤트가 있는 원거리 공격 클립만 씁니다. "
            + "이펙트 지점(ActorEffectPoint)·발사 지점(ActorMuzzle)은 공격 클립의 Actor Point 번호(기본 0)를 고릅니다. 이벤트 번호와 별개입니다.")]
        [SerializeField] private CombatAttackCueInput cues = new();

        // 옛 칸(kind·투사체 값·발사 반동·곡사·근접 값)은 부품 SO로 옮겼다(범용 연출 구조 계획 Gb.3·Gb.8). 에셋 YAML의 옛 키는 Unity가 다시 저장할 때 지운다.

        /// <summary>
        /// 매번 새 불변 값을 만듭니다. 종류는 투사체 부품에서 정하고, 코드 모션 부품이 그 종류와 맞는지 여기서 봅니다.
        /// 수 값 조건의 원본은 값 객체 생성자이고, 여기서는 실패에 에셋·부품 이름을 붙입니다(범용 연출 구조 설계 G8).
        /// </summary>
        /// <exception cref="InvalidOperationException">부품이 맞지 않거나 값이 규칙에 맞지 않을 때입니다(부트의 DB 생성이 멈춥니다).</exception>
        internal CombatAttackPresentationData CreateData()
        {
            CombatAttackPresentationKind kind = projectile != null ? projectile.Kind : CombatAttackPresentationKind.Melee;
            if (codeMotion != null && codeMotion.Kind != kind)
                throw Invalid(nameof(codeMotion), $"코드 모션 부품({codeMotion.GetType().Name})이 전달 종류({kind})와 맞지 않습니다", kind);

            // 부품 타입마다 가진 칸이 다르므로 값은 여기 한 곳에서 꺼냅니다. 그 종류에 없는 칸은 0입니다(값 객체가 읽지 않음).
            var straight = projectile as CombatStraightProjectileSO;
            var arc = projectile as CombatArcProjectileSO;
            var melee = codeMotion as CombatMeleeMotionSO;
            var shoot = codeMotion as CombatShootMotionSO;
            var mortar = codeMotion as CombatMortarMotionSO;
            try
            {
                return new CombatAttackPresentationData(
                    kind,
                    projectile != null ? projectile.Prefab : null,
                    projectile != null ? projectile.ReadEasing() : CombatProjectileEasing.Linear,
                    straight != null ? straight.Speed : 0f,
                    straight != null ? straight.Height : 0f,
                    arc != null ? arc.FlightDuration : 0f,
                    arc != null ? arc.ArcHeightInCells : 0f,
                    arc != null ? arc.SpinDegreesPerSecond : Vector3.zero,
                    hasCodeMotion: codeMotion != null,
                    recoilDistance: shoot != null ? shoot.RecoilDistance : 0f,
                    recoilAngle: shoot != null ? shoot.RecoilAngle : 0f,
                    shootDuration: shoot != null ? shoot.ShootDuration : 0f,
                    fireTime: shoot != null ? shoot.FireTime : 0f,
                    prepareDuration: mortar != null ? mortar.PrepareDuration : 0f,
                    returnDuration: mortar != null ? mortar.ReturnDuration : 0f,
                    bucketPitch: mortar != null ? mortar.BucketPitch : 0f,
                    meleeDuration: melee != null ? melee.Duration : 0f,
                    meleeWindupAngle: melee != null ? melee.WindupAngle : 0f,
                    meleeImpactAngle: melee != null ? melee.ImpactAngle : 0f,
                    meleePullback: melee != null ? melee.Pullback : 0f,
                    meleeLunge: melee != null ? melee.Lunge : 0f,
                    cues: (cues ?? throw new ArgumentException("공격 큐 칸이 없습니다.", nameof(cues))).ToCues(),
                    // 투사체 소리는 투사체 부품이 주인입니다(투사체·피격 소리 설계 PS1). 근접(부품 없음)은 -1 = 소리 없음입니다.
                    impactSoundPreset: projectile != null ? projectile.ImpactSoundPreset : -1,
                    impactSoundDelay: projectile != null ? projectile.ImpactSoundDelay : 0f,
                    flightSoundPreset: projectile != null ? projectile.FlightSoundPreset : -1,
                    flightSoundDelay: projectile != null ? projectile.FlightSoundDelay : 0f,
                    arcRotationMode: arc != null ? arc.RotationMode : CombatArcRotationMode.Spin);
            }
            catch (ArgumentException e)
            {
                throw Invalid(e.ParamName, e.Message, kind, e);
            }
        }

        private InvalidOperationException Invalid(string field, string reason, CombatAttackPresentationKind kind, Exception inner = null)
        {
            return new InvalidOperationException(
                $"[CombatAttackPresentationSO] Invalid {field}: {reason}. Kind: {kind}, Asset: {name}, "
                + $"Projectile: {(projectile != null ? projectile.name : "없음")}, CodeMotion: {(codeMotion != null ? codeMotion.name : "없음")}",
                inner);
        }
    }
}
