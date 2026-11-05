using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 공격 묶음이 참조하는 투사체 부품 입력입니다(범용 연출 구조 설계 G4, 결정 Q-G4 A). 공격 표시 종류는 이 부품의 타입에서 나옵니다
    /// (직선 = <see cref="CombatStraightProjectileSO"/>, 곡사 = <see cref="CombatArcProjectileSO"/>, 부품 없음 = 근접). 종류를 따로 저장하지 않습니다(P7).
    /// </summary>
    /// <remarks>값 조건의 원본은 공격 값 객체 생성자입니다. 이 SO는 칸을 내주고, 완급 곡선만 여기서 값으로 바꿉니다(곡선은 Unity 형식이라).</remarks>
    internal abstract class CombatProjectileSO : ScriptableObject
    {
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("같은 경로를 가는 빠르기의 완급입니다. 가로 = 시간 비율(0~1), 세로 = 경로 진행률(0~1). (0,0)에서 시작해 (1,1)에서 끝나고 줄어들면 안 됩니다. "
            + "경로 모양과 비행 시간(직선 = 거리 ÷ 속도, 곡사 = 비행 시간)은 그대로입니다. 기본은 등속입니다. "
            + "끝(시간 1)보다 먼저 1에 닿으면 남은 시간 동안 목표 위에 멈춰 있고, 곡사는 비행 시간이 끝날 때 맞습니다.")]
        [SerializeField] private AnimationCurve projectileEasing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("소리")]
        [Tooltip("착탄 소리의 프리셋 번호입니다(효과음 착탄 슬롯의 변종). -1이면 소리가 없습니다. 맞는 순간(곡사 = 착지, 직선 = 대상 몸 접촉) 뒤 아래 지연에 냅니다.")]
        [SerializeField, Min(-1)] private int impactSoundPreset = -1;
        [Tooltip("맞는 순간 뒤 착탄 소리까지의 초입니다. 길게 주면 그만큼 다음 행동이 기다립니다.")]
        [SerializeField, Min(0f)] private float impactSoundDelay;
        [Tooltip("비행 소리의 프리셋 번호입니다(효과음 비행 슬롯의 변종). -1이면 소리가 없습니다. 발사 순간 뒤 아래 지연에 한 번 냅니다.")]
        [SerializeField, Min(-1)] private int flightSoundPreset = -1;
        [Tooltip("발사 순간 뒤 비행 소리까지의 초입니다. 비행 시간보다 길게 주면 그만큼 다음 행동이 기다립니다.")]
        [SerializeField, Min(0f)] private float flightSoundDelay;

        /// <summary>이 투사체의 공격 표시 종류입니다(직선·곡사).</summary>
        internal abstract CombatAttackPresentationKind Kind { get; }

        internal GameObject Prefab => projectilePrefab;

        // 투사체 소리입니다(투사체·피격 소리 설계 PS1·PS3). 무엇을 낼지(프리셋)는 투사체가 주인이고, 언제는 기준 시점(발사 = Cue, 착탄 = Hit) + 지연입니다.
        // 값 조건의 원본은 공격 값 객체 생성자입니다. 재생은 큐 붙이기가 하고, 효과음 슬롯은 소리 표(CombatEventSounds)에서 연결 대기입니다.
        internal int ImpactSoundPreset => impactSoundPreset;
        internal float ImpactSoundDelay => impactSoundDelay;
        internal int FlightSoundPreset => flightSoundPreset;
        internal float FlightSoundDelay => flightSoundDelay;

        /// <summary>
        /// 투사체 완급 곡선입니다(투사체 완급 설계 EZ1). 규칙의 원본은 값 쪽 한 곳이고(<see cref="CombatProjectileEasing.TryFrom"/>, 설계 Q-EZ3 A),
        /// 잘못된 곡선은 부트(DB 생성)에서 멈춥니다.
        /// </summary>
        internal CombatProjectileEasing ReadEasing()
        {
            if (!CombatProjectileEasing.TryFrom(projectileEasing, out CombatProjectileEasing easing, out string error))
                throw new InvalidOperationException($"[{GetType().Name}] Invalid {nameof(projectileEasing)}: {error}. Asset: {name}");
            return easing;
        }
    }
}
