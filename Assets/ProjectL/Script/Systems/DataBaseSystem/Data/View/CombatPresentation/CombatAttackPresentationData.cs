using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 공유 SO(공격 묶음과 그 투사체·코드 모션 부품)에서 복사한 불변 공격 표시 설정입니다. 재생 상태나 원본 SO를 보관하지 않습니다.
    /// 값은 평평하게 둡니다(범용 연출 구조 계획 P1). 그 종류에 없는 칸(예: 근접의 투사체 값)은 0이고(소리 프리셋은 -1 = 없음) 읽는 곳이 없습니다.
    /// </summary>
    internal sealed class CombatAttackPresentationData
    {
        /// <param name="kind">공격 표시 종류입니다. 입력 SO가 투사체 부품의 타입에서 정합니다(부품 없음 = 근접, 범용 연출 구조 설계 G4).</param>
        /// <param name="hasCodeMotion">코드 모션 부품이 있는지입니다. false면 모션 값을 검사하지 않고, 코드 모션 길·정면 공격이 시작하지 않습니다(계획 P1).</param>
        /// <param name="impactSoundPreset">투사체 착탄 소리 프리셋입니다(-1 = 소리 없음, 투사체·피격 소리 설계 PS1). 근접은 -1입니다.</param>
        /// <param name="impactSoundDelay">맞는 순간 뒤 착탄 소리까지의 초입니다(0 이상).</param>
        /// <param name="flightSoundPreset">투사체 비행 소리 프리셋입니다(-1 = 소리 없음). 근접은 -1입니다.</param>
        /// <param name="flightSoundDelay">발사 순간 뒤 비행 소리까지의 초입니다(0 이상).</param>
        /// <exception cref="ArgumentException">
        /// 값이 규칙에 맞지 않을 때입니다(<see cref="ArgumentNullException"/>·<see cref="ArgumentOutOfRangeException"/> 포함). 값 조건의 원본은 이 생성자이고,
        /// 수 판정은 <see cref="CombatPresentationValueRules"/>를 따릅니다. 입력 SO가 에셋·부품 이름을 붙여 다시 던집니다.
        /// </exception>
        internal CombatAttackPresentationData(
            CombatAttackPresentationKind kind,
            GameObject projectilePrefab,
            CombatProjectileEasing projectileEasing,
            float projectileSpeed,
            float projectileHeight,
            float flightDuration,
            float arcHeightInCells,
            Vector3 spinDegreesPerSecond,
            bool hasCodeMotion,
            float recoilDistance,
            float recoilAngle,
            float shootDuration,
            float fireTime,
            float prepareDuration,
            float returnDuration,
            float bucketPitch,
            float meleeDuration,
            float meleeWindupAngle,
            float meleeImpactAngle,
            float meleePullback,
            float meleeLunge,
            CombatEventCues cues,
            int impactSoundPreset = -1,
            float impactSoundDelay = 0f,
            int flightSoundPreset = -1,
            float flightSoundDelay = 0f,
            CombatArcRotationMode arcRotationMode = CombatArcRotationMode.Spin)
        {
            if (!System.Enum.IsDefined(typeof(CombatAttackPresentationKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "정의된 공격 표시 종류여야 합니다.");
            if (cues == null) throw new ArgumentNullException(nameof(cues));
            // 큐 규칙의 원본은 한 곳입니다(범용 연출 구조 설계 G3). 근접은 투사체가 없어 접촉(Contact) 큐를 받지 않습니다(옛 "근접 접촉 이펙트는 Empty만").
            CombatPresentationCueRules.Require(cues, CombatPresentationCueRules.AttackEventOf(kind));
            // 효과음 프리셋 조건은 수 규칙 원본 하나입니다(-1 = 소리 없음도 받음).
            CombatPresentationValueRules.RequireSoundPreset(impactSoundPreset, nameof(impactSoundPreset), allowNone: true);
            CombatPresentationValueRules.RequireNonNegative(impactSoundDelay, nameof(impactSoundDelay));
            CombatPresentationValueRules.RequireSoundPreset(flightSoundPreset, nameof(flightSoundPreset), allowNone: true);
            CombatPresentationValueRules.RequireNonNegative(flightSoundDelay, nameof(flightSoundDelay));

            if (kind == CombatAttackPresentationKind.Melee)
            {
                if (hasCodeMotion)
                {
                    CombatPresentationValueRules.RequirePositive(meleeDuration, nameof(meleeDuration));
                    CombatPresentationValueRules.RequireNonNegative(meleeWindupAngle, nameof(meleeWindupAngle));
                    CombatPresentationValueRules.RequireNonNegative(meleeImpactAngle, nameof(meleeImpactAngle));
                    CombatPresentationValueRules.RequireNonNegative(meleePullback, nameof(meleePullback));
                    CombatPresentationValueRules.RequireNonNegative(meleeLunge, nameof(meleeLunge));
                }
            }
            else
            {
                if (projectilePrefab == null)
                    throw new ArgumentNullException(nameof(projectilePrefab), "직선·곡사 공격은 투사체 프리팹이 있어야 합니다.");
                if (kind == CombatAttackPresentationKind.Mortar)
                {
                    if (!System.Enum.IsDefined(typeof(CombatArcRotationMode), arcRotationMode))
                        throw new ArgumentOutOfRangeException(nameof(arcRotationMode), arcRotationMode, "정의된 곡사 회전 방식이어야 합니다.");
                    CombatPresentationValueRules.RequirePositive(flightDuration, nameof(flightDuration));
                    CombatPresentationValueRules.RequireNonNegative(arcHeightInCells, nameof(arcHeightInCells));
                    CombatPresentationValueRules.RequireFinite(spinDegreesPerSecond.x, nameof(spinDegreesPerSecond));
                    CombatPresentationValueRules.RequireFinite(spinDegreesPerSecond.y, nameof(spinDegreesPerSecond));
                    CombatPresentationValueRules.RequireFinite(spinDegreesPerSecond.z, nameof(spinDegreesPerSecond));
                }
                else
                {
                    CombatPresentationValueRules.RequirePositive(projectileSpeed, nameof(projectileSpeed));
                    CombatPresentationValueRules.RequireNonNegative(projectileHeight, nameof(projectileHeight));
                }

                if (hasCodeMotion)
                {
                    CombatPresentationValueRules.RequireNonNegative(recoilDistance, nameof(recoilDistance));
                    CombatPresentationValueRules.RequireNonNegative(recoilAngle, nameof(recoilAngle));
                    CombatPresentationValueRules.RequirePositive(shootDuration, nameof(shootDuration));
                    CombatPresentationValueRules.RequireFinite(fireTime, nameof(fireTime));
                    if (fireTime < 0f || fireTime > 1f)
                        throw new ArgumentOutOfRangeException(nameof(fireTime), fireTime, "발사 시점 비율은 0~1이어야 합니다.");
                    if (kind == CombatAttackPresentationKind.Mortar)
                    {
                        CombatPresentationValueRules.RequireNonNegative(prepareDuration, nameof(prepareDuration));
                        CombatPresentationValueRules.RequireNonNegative(returnDuration, nameof(returnDuration));
                        CombatPresentationValueRules.RequireFinite(bucketPitch, nameof(bucketPitch));
                    }
                }
            }

            Kind = kind;
            ProjectilePrefab = projectilePrefab;
            ProjectileEasing = projectileEasing;
            ProjectileSpeed = projectileSpeed;
            ProjectileHeight = projectileHeight;
            FlightDuration = flightDuration;
            ArcHeightInCells = arcHeightInCells;
            SpinDegreesPerSecond = spinDegreesPerSecond;
            ArcRotationMode = arcRotationMode;
            HasCodeMotion = hasCodeMotion;
            RecoilDistance = recoilDistance;
            RecoilAngle = recoilAngle;
            ShootDuration = shootDuration;
            FireTime = fireTime;
            PrepareDuration = prepareDuration;
            ReturnDuration = returnDuration;
            BucketPitch = bucketPitch;
            MeleeDuration = meleeDuration;
            MeleeWindupAngle = meleeWindupAngle;
            MeleeImpactAngle = meleeImpactAngle;
            MeleePullback = meleePullback;
            MeleeLunge = meleeLunge;
            Cues = cues;
            ImpactSoundPreset = impactSoundPreset;
            ImpactSoundDelay = impactSoundDelay;
            FlightSoundPreset = flightSoundPreset;
            FlightSoundDelay = flightSoundDelay;
        }

        internal CombatAttackPresentationKind Kind { get; }
        internal GameObject ProjectilePrefab { get; }

        /// <summary>
        /// 투사체가 같은 경로를 가는 빠르기의 완급입니다(투사체 완급 설계 EZ1). 직선·곡사, 클립 길·코드 모션 길 모두 발사체 풀이 이것으로 진행률을 정합니다.
        /// 근접은 투사체가 없어 언제나 등속입니다.
        /// </summary>
        internal CombatProjectileEasing ProjectileEasing { get; }

        internal float ProjectileSpeed { get; }
        internal float ProjectileHeight { get; }
        internal float FlightDuration { get; }
        internal float ArcHeightInCells { get; }
        internal Vector3 SpinDegreesPerSecond { get; }
        internal CombatArcRotationMode ArcRotationMode { get; }

        /// <summary>코드 모션 부품이 있는지입니다. false면 아래 모션 값은 0이고, 코드 모션 길·정면 공격이 시작하지 않습니다(계획 P1).</summary>
        internal bool HasCodeMotion { get; }

        internal float RecoilDistance { get; }
        internal float RecoilAngle { get; }
        internal float ShootDuration { get; }
        /// <summary>준비를 제외한 발사 동작 안의 발사 시점 비율입니다.</summary>
        internal float FireTime { get; }
        internal float PrepareDuration { get; }
        internal float ReturnDuration { get; }
        internal float BucketPitch { get; }
        internal float MeleeDuration { get; }
        internal float MeleeWindupAngle { get; }
        internal float MeleeImpactAngle { get; }
        internal float MeleePullback { get; }
        internal float MeleeLunge { get; }

        /// <summary>
        /// 공격 칸의 연출 큐입니다(범용 연출 구조 설계 G2). 클립·코드 모션 두 길의 조립이 같은 큐를 붙이고, 접촉(Contact) 시점 이펙트는 공격 재생의 접촉 입구가 냅니다
        /// (곡사 = 착탄, 직선 = 주 대상 몸 접촉, 계획 P5). 공격 소리는 이 큐가 아니라 유닛 외형의 공격 소리 줄입니다(유닛 소리 설계 US2).
        /// </summary>
        internal CombatEventCues Cues { get; }

        /// <summary>
        /// 투사체 착탄 소리 프리셋입니다(-1 = 없음, 투사체·피격 소리 설계 PS1). 맞는 순간(Hit) + <see cref="ImpactSoundDelay"/>에 냅니다.
        /// 투사체 공격에서는 접촉(Contact)과 맞음(Hit)이 같은 순간이라(범용 연출 구조 계획 P5) 시점은 Hit 하나이고, 조절은 지연으로 합니다.
        /// </summary>
        internal int ImpactSoundPreset { get; }

        /// <summary>맞는 순간 뒤 착탄 소리까지의 초입니다.</summary>
        internal float ImpactSoundDelay { get; }

        /// <summary>투사체 비행 소리 프리셋입니다(-1 = 없음). 발사 순간(Event1) + <see cref="FlightSoundDelay"/>에 한 번 냅니다(결정 Q-PS1).</summary>
        internal int FlightSoundPreset { get; }

        /// <summary>발사 순간 뒤 비행 소리까지의 초입니다.</summary>
        internal float FlightSoundDelay { get; }
    }
}
