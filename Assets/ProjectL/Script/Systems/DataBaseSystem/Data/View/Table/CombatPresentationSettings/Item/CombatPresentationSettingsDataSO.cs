using System;
using System.Collections.Generic;
using Cinemachine;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item
{
    /// <summary>
    /// 전투 연출의 전역 시간·크기 값입니다(연출 조립 설계 §9 I-5, 결정 Q-R17 A). 유닛별 연출 값은 UnitVisual, 전역 값은 이 설정 하나가 원본입니다.
    /// 수 조건의 원본은 수 규칙(<see cref="CombatPresentationValueRules"/>)이고, 이 설정의 조건 함수(Validate…)는 거기에 문구를 붙입니다. 하위 설정 묶음 셋(보드 등장·선로 낙하·Nexus 낙하)은 수 규칙을 바로 부르고,
    /// 보드 등장의 시간 칸 둘만 이 설정의 조건 함수(<see cref="ValidateSeconds"/>)를 씁니다(카메라 흔들림 시간·모양은 예외로 카메라 효과가 원본, 카메라 연출 조각 설계 K5 결정 A). 표가 만들 때 <see cref = "Validate"/>로 거절하고, 전투 표시 조립과 재생 부품도 같은 조건 함수를 씁니다.
    /// </summary>
    [Serializable]
    internal sealed class CombatPresentationSettingsData : IData
    {
        internal const float DefaultLeapSeconds = .5f;
        internal const float DefaultLeapHeight = 1f;

        [Header("전투 진입 보드 등장")]
        [SerializeField] private CombatBoardRevealSettings boardReveal = new();
        internal CombatBoardRevealSettings BoardReveal => boardReveal;

        [Header("전투 진입 선로 순차 낙하")]
        [SerializeField] private CombatRailwayDropSettings railwayDrop = new();
        internal CombatRailwayDropSettings RailwayDrop => railwayDrop;

        [Header("전투 진입 적 Nexus 낙하")]
        [SerializeField] private CombatNexusDropSettings nexusDrop = new();
        internal CombatNexusDropSettings NexusDrop => nexusDrop;

        [Header("전투 진입 적 행별 등장")]
        [Tooltip("그리드 Y가 큰 행부터 적 소환을 시작하는 행 사이 간격입니다(초). 같은 행은 동시에 시작하며, 빈 행은 건너뜁니다. 0이면 전부 동시에 등장합니다.")]
        [SerializeField, Min(0f)]
        private float enemyEntryRowGapSeconds = .2f;
        internal float EnemyEntryRowGapSeconds => enemyEntryRowGapSeconds;

        [Header("특성 몸체 텍스트")]
        [SerializeField]
        private GameObject bodyTextPrefab;
        [SerializeField, Min(0)]
        private float bodyTextStackGap = 28f;
        internal GameObject BodyTextPrefab => bodyTextPrefab;
        internal float BodyTextStackGap => bodyTextStackGap;

        [Header("유닛 이동 연출")]
        [Tooltip("이동 한 걸음의 보간 시간입니다(초). 유한한 양수여야 합니다.")]
        [SerializeField, Min(.01f)]
        private float movementStepSeconds = .2f;
        [Tooltip("아군·적의 이동 전 회전과 최종 도착 후 기존 방향 복귀 시간입니다(초).")]
        [SerializeField, Min(0f)]
        private float movementTurnSeconds = .1f;
        [Tooltip("높이가 다른 칸 사이 걸음(고지대 오르내림·넉백)이 칸 경계에서 높은 칸보다 더 올라가는 높이입니다(필드 로컬 단위). 0이면 블록 모서리에 딱 닿습니다. 같은 높이 걸음에는 쓰지 않습니다.")]
        [SerializeField, Min(0f)]
        private float stepClimbHop = .1f;
        [Tooltip("자발 이동(아군·적의 경로 걸음)의 걸음 모양입니다(걸음 모양 설계 ST4). 넉백·끌어오기 같은 밀림은 늘 걷습니다. " + "Walk면 위 걷기 값으로 걷고, Hop이면 아래 뛰기 값으로 칸마다 한 번씩 뜁니다.")]
        [SerializeField]
        private CombatStepLook voluntaryStepLook = CombatStepLook.Hop;
        [Tooltip("뛰기의 꼭대기가 두 칸 중 높은 칸 표면보다 더 올라가는 높이입니다(필드 로컬 단위). 걸음 모양이 Walk면 쓰지 않습니다.")]
        [SerializeField, Min(0f)]
        private float hopHeight = .3f;
        [Tooltip("뛰기 한 번(한 칸)의 시간입니다(초). 유한한 양수여야 합니다. 걸음 모양이 Walk면 쓰지 않습니다.")]
        [SerializeField, Min(.01f)]
        private float hopSeconds = .25f;
        [Tooltip("작렬처럼 날려 보내는 밀림의 한 칸 시간입니다(초, 작렬 View 설계 CV5). 유한한 양수여야 합니다. 일반 넉백·끌어오기는 위 걷기 값을 씁니다.")]
        [SerializeField, Min(.01f)]
        private float flungStepSeconds = .08f;
        [Header("뛰어넘기 구간 연출")]
        [Tooltip("뛰어넘기로 중간 칸을 통과해 다음 착지 칸까지 한 번 도약하는 전체 시간입니다(초). 이동 전 회전과 중간 사건 대기는 제외합니다.")]
        [SerializeField, Min(.01f)]
        private float leapSeconds = DefaultLeapSeconds;
        [Tooltip("도약 구간에서 가장 높은 칸 표면보다 더 올라가는 높이입니다(필드 로컬 단위). 일반 칸당 Hop과 별개입니다.")]
        [SerializeField, Min(0f)]
        private float leapHeight = DefaultLeapHeight;
        [Header("적 행동 연출")]
        [Tooltip("적 행동 사이 간격입니다(초).")]
        [SerializeField, Min(0f)]
        private float enemyActionGapSeconds = .2f;
        [Tooltip("적 턴 사이 간격입니다(초).")]
        [SerializeField, Min(0f)]
        private float enemyTurnGapSeconds = .4f;
        [Header("보스 능력 연출 (보스 활성화 설계 D6b)")]
        [Tooltip("소환 칸을 강조한 뒤 유닛이 바닥 아래에서 올라오기 시작할 때까지의 시간입니다(초).")]
        [SerializeField, Min(0f)]
        private float summonRiseDelaySeconds = .3f;
        [Tooltip("소환 유닛이 바닥 아래에서 제자리까지 올라오는 시간입니다(초). 끝나면 칸 강조를 끕니다.")]
        [SerializeField, Min(0f)]
        private float summonRiseSeconds = 1f;
        [Tooltip("소환 유닛이 올라오기 시작하는 깊이(월드 단위)입니다.")]
        [SerializeField, Min(0f)]
        private float summonRiseDepth = 1.5f;
        [Tooltip("올라오는 동안 Y축으로 도는 바퀴 수입니다.")]
        [SerializeField, Min(0f)]
        private float summonRiseTurns = 1f;
        [Tooltip("포효 칸을 강조한 뒤 보스가 커지기 시작할 때까지의 시간입니다(초).")]
        [SerializeField, Min(0f)]
        private float roarDelaySeconds = .2f;
        [Tooltip("포효 때 커지기와 작아지기에 걸리는 전체 시간입니다(초, 각각 절반). 공격 모션은 커진 채로 그 사이에 재생합니다.")]
        [SerializeField, Min(0f)]
        private float roarPulseSeconds = 1f;
        [Tooltip("포효 때 가장 커지는 크기입니다. 현재 크기에 곱합니다(보스 프리팹 크기 2 기준 1.25 = 2.5, 1.5 = 3).")]
        [SerializeField, Min(.01f)]
        private float roarPeakScale = 1.25f;
        [Header("낭떠러지 추락 연출 (임시)")]
        [Tooltip("낭떠러지에 떨어진 유닛이 구멍 아래로 떨어지는 전체 시간입니다(초). 끝나면 모델을 내려놓습니다.")]
        [SerializeField, Min(0f)]
        private float fallSeconds = .6f;
        [Tooltip("추락하는 동안 떨어지는 깊이(월드 단위)입니다. 점점 빨라집니다(약한 지반 붕괴 때 타일 가라앉기와 같은 곡선).")]
        [SerializeField, Min(0f)]
        private float fallDepth = 3f;
        [Tooltip("추락이 끝날 때의 크기입니다. 시작 크기에 곱하며 0보다 크고 1 이하여야 합니다.")]
        [SerializeField, Range(.01f, 1f)]
        private float fallEndScale = .05f;
        [Tooltip("전체 추락 시간 가운데 작아지기 시작하는 지점입니다(0 = 처음부터, 0.5 = 절반부터). 그 뒤 끝까지 끝 크기로 작아집니다. 0 이상 1 미만입니다.")]
        [SerializeField, Range(0f, .99f)]
        private float fallShrinkStart = .5f;
        [Header("약한 지반 붕괴 연출 (타일 교체 설계 TR8)")]
        [Tooltip("약한 지반이 무너질 때 그 칸 외형(바뀐 바닥 타일)이 떨리는 시간입니다(초). 이 동안 다음 사건(그 칸 유닛 추락)은 기다립니다. 0이면 떨지 않고 바로 가라앉습니다.")]
        [SerializeField, Min(0f)]
        private float tileCollapseShakeSeconds = .35f;
        [Tooltip("떨림의 세기입니다. 필드 가로 두 축으로 흔들리는 거리(월드 단위)입니다. 0이면 떨림 시간 동안 제자리에 있습니다.")]
        [SerializeField, Min(0f)]
        private float tileCollapseShakeStrength = .04f;
        [Tooltip("떨림의 빠르기입니다(초당 왕복 수). 0이면 흔들리지 않습니다.")]
        [SerializeField, Min(0f)]
        private float tileCollapseShakeFrequency = 25f;
        [Tooltip("떨림 뒤 외형이 아래로 가라앉는 시간입니다(초). 그 칸 유닛의 추락과 함께 진행하며, 끝나면 외형을 끕니다. 결과 끝은 이것까지 기다립니다.")]
        [SerializeField, Min(0f)]
        private float tileCollapseSinkSeconds = .5f;
        [Tooltip("외형이 가라앉는 깊이(월드 단위)입니다. 점점 빨라집니다(추락 연출의 가라앉기와 같은 곡선).")]
        [SerializeField, Min(0f)]
        private float tileCollapseSinkDepth = 1.5f;
        [Tooltip("약한 지반을 밟을 때 붕괴 연출의 떨림 시간·강도와 하강 시간에 곱합니다. 0이면 즉시 내려갑니다.")]
        [SerializeField, Min(0f)]
        private float weakGroundEntryScale = .5f;
        [Header("카메라 연출 (카메라 연출 조각 설계 K5)")]
        [Tooltip("포효 때 맵 카메라 줌 배율입니다. 기준 렌즈값에 곱하며 작을수록 확대입니다. 1이면 줌하지 않습니다. 커짐·작아짐과 같은 시간(맥박 절반)에 들어가고 돌아옵니다.")]
        [SerializeField, Min(.01f)]
        private float roarZoomRatio = .85f;
        [Tooltip("포효 박치기가 맞는 순간의 카메라 흔들림 세기입니다(아래 방향 속도 크기). 0이면 흔들지 않습니다. 맵 카메라에 흔들림 수신기(Listener)가 있어야 보입니다.")]
        [SerializeField, Min(0f)]
        private float roarShakeStrength = .4f;
        [Tooltip("포효 흔들림 시간입니다(초). 양수여야 하며, 값 검사는 카메라 효과가 보낼 때 합니다(설계 K5 결정 A).")]
        [SerializeField]
        private float roarShakeSeconds = .25f;
        [Tooltip("포효 흔들림 모양입니다. Recoil·Bump·Explosion·Rumble만 됩니다(Custom 제외). 값 검사는 카메라 효과가 보낼 때 합니다.")]
        [SerializeField]
        private CinemachineImpulseDefinition.ImpulseShapes roarShakeShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        // 피격·사망 흔들림 값(옛 hitShake*·deathShake*)은 공유 반응 묶음의 피격·사망 칸 큐로 옮겼다(범용 연출 구조 설계 G7, 계획 Gd.8).
        [Tooltip("기차 앞면이 필드(첫 레일 칸)에 들어오는 순간의 카메라 흔들림 세기입니다. 0이면 흔들지 않습니다. 사망보다 약하게 두면 치인 유닛의 사망 흔들림도 함께 나갑니다.")]
        [SerializeField, Min(0f)]
        private float trainShakeStrength = .2f;
        [Tooltip("기차 흔들림 시간입니다(초). 양수여야 하며, 값 검사는 카메라 효과가 보낼 때 합니다(설계 K5 결정 A).")]
        [SerializeField]
        private float trainShakeSeconds = 1.5f;
        [Tooltip("기차 흔들림 모양입니다. Recoil·Bump·Explosion·Rumble만 됩니다. 값 검사는 카메라 효과가 보낼 때 합니다.")]
        [SerializeField]
        private CinemachineImpulseDefinition.ImpulseShapes trainShakeShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
        [Tooltip("기차가 필드를 지나는 동안의 맵 카메라 줌 배율입니다. 기준 렌즈값에 곱하며 작을수록 확대입니다. 1이면 줌하지 않습니다.")]
        [SerializeField, Min(.01f)]
        private float trainZoomRatio = .92f;
        [Tooltip("기차 앞면이 필드에 들어올 때 줌으로 옮기는 시간입니다(초).")]
        [SerializeField, Min(0f)]
        private float trainZoomInSeconds = .3f;
        [Tooltip("기차 앞면이 필드를 빠져나갈 때 기준 크기로 돌아오는 시간입니다(초).")]
        [SerializeField, Min(0f)]
        private float trainZoomOutSeconds = .4f;
        [Header("전투 소리 프리셋 (View 3라운드 W9-2)")]
        [Tooltip("적 유닛의 선택·이동 효과음 프리셋입니다. 적은 외형과 상관없이 이 값 하나를 씁니다. -1이면 소리를 내지 않습니다. 적 사망 소리는 유닛 외형의 사망 소리 줄입니다(유닛 소리 설계 US3).")]
        [SerializeField, Min(-1)]
        private int enemyUnitSoundPreset = 5;
        [Tooltip("밤(전투) 국면 배경음 프리셋입니다.")]
        [SerializeField, Min(0)]
        private int nightBgmPreset = 3;
        [Tooltip("낮(배치) 국면 배경음 프리셋입니다.")]
        [SerializeField, Min(0)]
        private int dayBgmPreset = 2;
        [Tooltip("승패 표시를 확인한 뒤(맵으로 돌아가기 전) 배경음 프리셋입니다.")]
        [SerializeField, Min(0)]
        private int afterCompletionBgmPreset = 1;
        [Tooltip("약한 지반에 들어갈 때의 효과음 프리셋입니다. 첫 원소는 남은 진입 횟수 1, 둘째는 2, … 남은 횟수가 목록보다 많으면 마지막 원소를 씁니다. " + "-1은 그 횟수에 소리를 내지 않고, 빈 목록은 진입 소리를 내지 않습니다. 무너지는 진입은 붕괴 칸이 냅니다(소리 프리셋 원본 설계 SP2).")]
        [SerializeField, Min(-1)]
        private List<int> weakGroundEntrySoundPresets = new() { 1, 2 };
        [Tooltip("약한 지반이 무너져 낭떠러지가 될 때의 효과음 프리셋입니다. -1이면 소리를 내지 않습니다(소리 창구 2차 설계 Q-ES5′).")]
        [SerializeField, Min(-1)]
        private int weakGroundCollapseSoundPreset = 0;
        [Header("전투 소리 프리셋 — -1이면 프리셋 없이 냄 (소리 프리셋 원본 설계 SP3)")]
        [Tooltip("유닛이 낭떠러지로 떨어지기 시작할 때의 효과음 프리셋입니다. -1이면 프리셋을 지정하지 않고 냅니다(Boot Fall 칸에는 지금 프리셋이 없어 기본 소리). " + "0 이상을 쓰려면 Boot Fall 칸에 그 프리셋을 먼저 더해야 하며, 없으면 재생 때 오류입니다. 유닛의 이어지는 사망 소리는 따로 납니다.")]
        [SerializeField, Min(-1)]
        private int fallSoundPreset = -1;
        [Header("전투 연출 큐 (이펙트 창구 확장 설계 EC5)")]
        [Tooltip("유닛·구조물(장애물)이 낭떠러지로 떨어지기 시작하는 순간의 이펙트·흔들림입니다. 시점은 Start(사건 순간)만, 이펙트 자리는 떨어진 칸(EventCell)만 됩니다. " + "지연을 주어도 다음 사건은 기다리지 않지만, 기차 사건과 결과 끝은 그만큼 기다립니다. 소리는 유닛만 위 추락 프리셋이 냅니다.")]
        [SerializeField]
        private CombatVisualCueInput fallCues = new();
        [Tooltip("약한 지반이 무너져 낭떠러지가 되는 순간의 이펙트·흔들림입니다. 시점은 Start(사건 순간)만, 이펙트 자리는 무너진 칸(EventCell)만 됩니다. " + "지연을 주어도 다음 사건은 기다리지 않지만, 기차 사건과 결과 끝은 그만큼 기다립니다. 소리는 위 붕괴 프리셋이 냅니다.")]
        [SerializeField]
        private CombatVisualCueInput weakGroundCollapseCues = new();
        public float MovementStepSeconds => movementStepSeconds;
        public float MovementTurnSeconds => movementTurnSeconds;
        public float StepClimbHop => stepClimbHop;
        /// <summary>자발 이동의 걸음 모양입니다.</summary>
        public CombatStepLook VoluntaryStepLook => voluntaryStepLook;
        /// <summary>뛰기 꼭대기가 높은 칸 표면보다 더 올라가는 높이입니다(필드 로컬 단위).</summary>
        public float HopHeight => hopHeight;
        /// <summary>뛰기 한 번의 시간입니다(초).</summary>
        public float HopSeconds => hopSeconds;
        /// <summary>날려 가는 밀림(작렬)의 한 칸 시간입니다(초).</summary>
        public float FlungStepSeconds => flungStepSeconds;
        /// <summary>뛰어넘기 구간 전체의 보간 시간입니다(회전·중간 사건 대기 제외).</summary>
        public float LeapSeconds => leapSeconds;
        /// <summary>뛰어넘기 호의 꼭대기가 구간의 가장 높은 표면보다 더 올라가는 높이입니다.</summary>
        public float LeapHeight => leapHeight;
        public float EnemyActionGapSeconds => enemyActionGapSeconds;
        public float EnemyTurnGapSeconds => enemyTurnGapSeconds;
        public float SummonRiseDelaySeconds => summonRiseDelaySeconds;
        public float SummonRiseSeconds => summonRiseSeconds;
        public float SummonRiseDepth => summonRiseDepth;
        public float SummonRiseTurns => summonRiseTurns;
        public float RoarDelaySeconds => roarDelaySeconds;
        public float RoarPulseSeconds => roarPulseSeconds;
        public float RoarPeakScale => roarPeakScale;
        public float FallSeconds => fallSeconds;
        public float FallDepth => fallDepth;
        public float FallEndScale => fallEndScale;
        /// <summary>전체 추락 시간 가운데 작아지기 시작하는 지점입니다(0 이상 1 미만).</summary>
        public float FallShrinkStart => fallShrinkStart;
        /// <summary>바닥 교체 외형이 사건 시점에 빠질 때 떨리는 시간입니다(초, 타일 교체 설계 TR8).</summary>
        public float TileCollapseShakeSeconds => tileCollapseShakeSeconds;
        /// <summary>떨림 세기입니다(월드 단위).</summary>
        public float TileCollapseShakeStrength => tileCollapseShakeStrength;
        /// <summary>떨림 빠르기입니다(초당 왕복 수).</summary>
        public float TileCollapseShakeFrequency => tileCollapseShakeFrequency;
        /// <summary>떨림 뒤 가라앉는 시간입니다(초).</summary>
        public float TileCollapseSinkSeconds => tileCollapseSinkSeconds;
        /// <summary>가라앉는 깊이입니다(월드 단위).</summary>
        public float TileCollapseSinkDepth => tileCollapseSinkDepth;

        public float WeakGroundEntryScale => weakGroundEntryScale;
        public float RoarZoomRatio => roarZoomRatio;
        public float RoarShakeStrength => roarShakeStrength;
        public float RoarShakeSeconds => roarShakeSeconds;
        public CinemachineImpulseDefinition.ImpulseShapes RoarShakeShape => roarShakeShape;
        public float TrainShakeStrength => trainShakeStrength;
        public float TrainShakeSeconds => trainShakeSeconds;
        public CinemachineImpulseDefinition.ImpulseShapes TrainShakeShape => trainShakeShape;
        public float TrainZoomRatio => trainZoomRatio;
        public float TrainZoomInSeconds => trainZoomInSeconds;
        public float TrainZoomOutSeconds => trainZoomOutSeconds;
        public int EnemyUnitSoundPreset => enemyUnitSoundPreset;
        public int NightBgmPreset => nightBgmPreset;
        public int DayBgmPreset => dayBgmPreset;
        public int AfterCompletionBgmPreset => afterCompletionBgmPreset;
        /// <summary>낭떠러지 추락 효과음 프리셋입니다(유닛). -1이면 프리셋을 지정하지 않고 냅니다(장애물 소리와 같은 규칙, 소리 프리셋 원본 설계 SP3).</summary>
        public int FallSoundPreset => fallSoundPreset;
        /// <summary>약한 지반 붕괴 효과음 프리셋입니다. -1이면 소리 없음입니다.</summary>
        public int WeakGroundCollapseSoundPreset => weakGroundCollapseSoundPreset;
        /// <summary>
        /// 약한 지반 진입 효과음 프리셋 목록의 복사본입니다(원소 i = 남은 진입 횟수 i + 1). 부를 때마다 새 배열을 만들고,
        /// 프리셋 답(전투 소리 프리셋)이 만들어질 때 한 번 부릅니다(View 서비스가 시작할 때, 재생 시작 때 한 번). 그래서 재생 중에 이 목록을 바꾸면 다음 재생부터 반영됩니다.
        /// 직렬화 목록은 밖에 내주지 않습니다(소리 프리셋 원본 계획 TP-2).
        /// </summary>
        internal int[] CopyWeakGroundEntrySoundPresets() => weakGroundEntrySoundPresets.ToArray();
        /// <summary>낭떠러지 추락 순간 큐입니다. 읽을 때마다 새 값을 만들고, 보관은 쓰는 쪽이 합니다(이펙트 창구 확장 계획 P6).</summary>
        internal CombatEventCues FallCues => ReadCues(fallCues, nameof(fallCues));
        /// <summary>약한 지반 붕괴 순간 큐입니다. 읽을 때마다 새 값을 만들고, 보관은 쓰는 쪽이 합니다(이펙트 창구 확장 계획 P6).</summary>
        internal CombatEventCues WeakGroundCollapseCues => ReadCues(weakGroundCollapseCues, nameof(weakGroundCollapseCues));

        /// <summary>모든 값을 조건 함수로 검사합니다. 표가 만들 때와 전투 표시 조립 맨 앞에서 부릅니다.</summary>
        internal void Validate()
        {
            if (boardReveal == null) throw new InvalidOperationException("[CombatPresentationSettingsData] 보드 등장 설정이 필요합니다.");
            boardReveal.Validate();
            if (railwayDrop == null) throw new InvalidOperationException("[CombatPresentationSettingsData] 선로 낙하 설정이 필요합니다.");
            railwayDrop.Validate();
            if (nexusDrop == null) throw new InvalidOperationException("[CombatPresentationSettingsData] Nexus 낙하 설정이 필요합니다.");
            nexusDrop.Validate();
            ValidateSeconds(enemyEntryRowGapSeconds, nameof(enemyEntryRowGapSeconds));
            CombatPresentationValueRules.RequireNonNegative(bodyTextStackGap, nameof(bodyTextStackGap));
            // 순서는 옛 씬 검사·능력 동작 생성자와 같습니다. 여러 값이 함께 잘못됐을 때 먼저 알리는 값이 같습니다.
            ValidateStepSeconds(movementStepSeconds, nameof(movementStepSeconds));
            ValidateSeconds(enemyActionGapSeconds, nameof(enemyActionGapSeconds));
            ValidateSeconds(enemyTurnGapSeconds, nameof(enemyTurnGapSeconds));
            ValidateSeconds(movementTurnSeconds, nameof(movementTurnSeconds));
            ValidateSeconds(summonRiseDelaySeconds, nameof(summonRiseDelaySeconds));
            ValidateSeconds(summonRiseSeconds, nameof(summonRiseSeconds));
            ValidateSeconds(roarDelaySeconds, nameof(roarDelaySeconds));
            ValidateSeconds(roarPulseSeconds, nameof(roarPulseSeconds));
            ValidateRiseDepth(summonRiseDepth, nameof(summonRiseDepth));
            ValidateRiseTurns(summonRiseTurns, nameof(summonRiseTurns));
            ValidatePeakScale(roarPeakScale, nameof(roarPeakScale));
            // 카메라 값은 우리 개념(배율·세기)만 검사합니다. 흔들림 시간·모양의 조건 원본은 카메라 효과입니다(카메라 연출 조각 설계 K5 결정 A).
            ValidateZoomRatio(roarZoomRatio, nameof(roarZoomRatio));
            ValidateShakeStrength(roarShakeStrength, nameof(roarShakeStrength));
            ValidateShakeStrength(trainShakeStrength, nameof(trainShakeStrength));
            ValidateZoomRatio(trainZoomRatio, nameof(trainZoomRatio));
            ValidateSeconds(trainZoomInSeconds, nameof(trainZoomInSeconds));
            ValidateSeconds(trainZoomOutSeconds, nameof(trainZoomOutSeconds));
            ValidateStepClimbHop(stepClimbHop, nameof(stepClimbHop));
            ValidateSoundPreset(enemyUnitSoundPreset, nameof(enemyUnitSoundPreset));
            ValidateBgmPreset(nightBgmPreset, nameof(nightBgmPreset));
            ValidateBgmPreset(dayBgmPreset, nameof(dayBgmPreset));
            ValidateBgmPreset(afterCompletionBgmPreset, nameof(afterCompletionBgmPreset));
            // 추락 값은 맨 뒤에 둡니다. 앞 값들이 함께 잘못됐을 때 먼저 알리는 값이 그대로입니다.
            ValidateSeconds(fallSeconds, nameof(fallSeconds));
            ValidateFallDepth(fallDepth, nameof(fallDepth));
            ValidateFallEndScale(fallEndScale, nameof(fallEndScale));
            // 2차 소리 프리셋도 맨 뒤에 둡니다(같은 까닭).
            ValidateSoundPreset(fallSoundPreset, nameof(fallSoundPreset));
            ValidateSoundPreset(weakGroundCollapseSoundPreset, nameof(weakGroundCollapseSoundPreset));
            // 진입 목록은 그 뒤에 둡니다(같은 까닭, 소리 프리셋 원본 계획 TP-3).
            if (weakGroundEntrySoundPresets == null) throw new InvalidOperationException("[CombatPresentationSettingsData] 약한 지반 진입 소리 프리셋 목록이 필요합니다.");
            for (int i = 0; i < weakGroundEntrySoundPresets.Count; i++)
                ValidateSoundPreset(weakGroundEntrySoundPresets[i], $"{nameof(weakGroundEntrySoundPresets)}[{i}]");
            // 걸음 모양 값도 맨 뒤에 둡니다(같은 까닭). 뛰기 값은 모양이 Walk여도 검사합니다(나중에 Hop으로 바꿀 때 남은 잘못된 값으로 멈추지 않게, 걸음 모양 설계 ST4).
            ValidateStepLook(voluntaryStepLook, nameof(voluntaryStepLook));
            ValidateHopHeight(hopHeight, nameof(hopHeight));
            ValidateStepSeconds(hopSeconds, nameof(hopSeconds));
            ValidateStepSeconds(leapSeconds, nameof(leapSeconds));
            ValidateHopHeight(leapHeight, nameof(leapHeight));
            // 사실 사건 큐도 맨 뒤에 둡니다(같은 까닭). 칸 규칙의 원본은 큐 규칙입니다(이펙트 창구 확장 설계 EC3).
            CombatPresentationCueRules.Require(FallCues, CombatCueEvent.Fall, $"[CombatPresentationSettingsData] {nameof(fallCues)}");
            CombatPresentationCueRules.Require(WeakGroundCollapseCues, CombatCueEvent.WeakGroundCollapse, $"[CombatPresentationSettingsData] {nameof(weakGroundCollapseCues)}");
            // 약한 지반 붕괴 연출 값도 맨 뒤에 둡니다(같은 까닭, 타일 교체 설계 TR8).
            ValidateSeconds(tileCollapseShakeSeconds, nameof(tileCollapseShakeSeconds));
            ValidateTileCollapseAmount(tileCollapseShakeStrength, nameof(tileCollapseShakeStrength));
            ValidateTileCollapseAmount(tileCollapseShakeFrequency, nameof(tileCollapseShakeFrequency));
            ValidateSeconds(tileCollapseSinkSeconds, nameof(tileCollapseSinkSeconds));
            ValidateTileCollapseAmount(tileCollapseSinkDepth, nameof(tileCollapseSinkDepth));
            if (!CombatPresentationValueRules.IsNonNegative(weakGroundEntryScale))
                throw new ArgumentOutOfRangeException(nameof(weakGroundEntryScale), "약한 지반의 진입 배율은 유한한 0 이상이어야 합니다.");
            // 추락 작아지기 시작 지점도 맨 뒤에 둡니다(같은 까닭).
            ValidateFallShrinkStart(fallShrinkStart, nameof(fallShrinkStart));
            // 날려 감 걸음 시간도 맨 뒤에 둡니다(같은 까닭, 작렬 View 설계 CV5).
            ValidateStepSeconds(flungStepSeconds, nameof(flungStepSeconds));
        }

        private static CombatEventCues ReadCues(CombatVisualCueInput input, string name) => (input ?? throw new InvalidOperationException($"[CombatPresentationSettingsData] {name} 칸이 없습니다.")).ToCues();
        /// <summary>
        /// 효과음 프리셋 번호 조건입니다(-1 = 프리셋 없음, 그 밖은 0 이상. -1을 받은 칸이 소리를 안 내는지, 프리셋 없이 내는지는 그 칸의 툴팁이 적습니다). 유닛 외형 표의 유닛·공격 프리셋(View 3라운드 W9-2)과 오브젝트·아이템 외형의 소리 프리셋(소리 창구 2차)도 이 조건을 씁니다.
        /// 규칙의 원본은 수 규칙 <see cref = "CombatPresentationValueRules.RequireSoundPreset"/> 하나이고, 이 함수는 그것을 부릅니다(투사체·피격 소리 묶음 끝 검토 중간 2).
        /// </summary>
        internal static void ValidateSoundPreset(int preset, string name) => CombatPresentationValueRules.RequireSoundPreset(preset, name, allowNone: true);
        /// <summary>배경음 프리셋 번호 조건입니다(0 이상). 국면 배경음은 늘 냅니다.</summary>
        internal static void ValidateBgmPreset(int preset, string name)
        {
            if (preset < 0)
                throw new ArgumentOutOfRangeException(name, preset, "배경음 프리셋은 0 이상이어야 합니다.");
        }

        /// <summary>높이가 다른 칸 사이 걸음의 추가 뜀 높이 조건입니다(유한한 0 이상, 고지대 View 계획 P11). 사건 Presenter 생성자도 이 조건을 씁니다.</summary>
        internal static void ValidateStepClimbHop(float hop, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(hop))
                throw new ArgumentOutOfRangeException(name, "걸음 뜀 높이는 유한한 0 이상의 값이어야 합니다.");
        }

        /// <summary>
        /// 이동 한 걸음의 연출 시간 조건입니다(유한한 양수). 사건 Presenter 생성자도 이 조건을 씁니다.
        /// 뛰기 한 번의 시간(<c>hopSeconds</c>)도 같은 조건입니다(걸음 모양 계획 P3).
        /// </summary>
        internal static void ValidateStepSeconds(float seconds, string name)
        {
            if (!CombatPresentationValueRules.IsPositive(seconds))
                throw new ArgumentOutOfRangeException(name, "이동 걸음 연출 시간은 유한한 양수 초 단위 값이어야 합니다.");
        }

        /// <summary>뛰기 꼭대기 높이 조건입니다(유한한 0 이상, 걸음 모양 설계 ST4). 걸음 모양 값 객체도 이 조건을 씁니다.</summary>
        internal static void ValidateHopHeight(float height, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(height))
                throw new ArgumentOutOfRangeException(name, "뛰기 높이는 유한한 0 이상의 값이어야 합니다.");
        }

        /// <summary>걸음 모양 조건입니다(열거에 있는 값, 걸음 모양 설계 ST4). 걸음 모양 값 객체도 이 조건을 씁니다.</summary>
        internal static void ValidateStepLook(CombatStepLook look, string name)
        {
            if (!System.Enum.IsDefined(typeof(CombatStepLook), look))
                throw new ArgumentOutOfRangeException(name, look, "걸음 모양이 열거 CombatStepLook에 없는 값입니다.");
        }

        /// <summary>연출 간격·시간 조건입니다(유한한 0 이상). 적 행동 간격과 능력 동작 시간이 이 조건을 씁니다.</summary>
        internal static void ValidateSeconds(float seconds, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(seconds))
                throw new ArgumentOutOfRangeException(name, "연출 간격은 유한한 0 이상의 초 단위 값이어야 합니다.");
        }

        /// <summary>소환 등장 깊이 조건입니다(유한한 0 이상).</summary>
        internal static void ValidateRiseDepth(float depth, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(depth))
                throw new ArgumentOutOfRangeException(name, "소환 등장 깊이는 유한한 0 이상의 값이어야 합니다.");
        }

        /// <summary>소환 등장 회전 바퀴 수 조건입니다(유한한 0 이상).</summary>
        internal static void ValidateRiseTurns(float turns, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(turns))
                throw new ArgumentOutOfRangeException(name, "소환 등장 회전 바퀴 수는 유한한 0 이상의 값이어야 합니다.");
        }

        /// <summary>포효 커지기 배율 조건입니다(유한한 양수).</summary>
        internal static void ValidatePeakScale(float scale, string name)
        {
            if (!CombatPresentationValueRules.IsPositive(scale))
                throw new ArgumentOutOfRangeException(name, "포효 커지기 배율은 유한한 양수여야 합니다.");
        }

        /// <summary>낭떠러지 추락 깊이 조건입니다(유한한 0 이상).</summary>
        internal static void ValidateFallDepth(float depth, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(depth))
                throw new ArgumentOutOfRangeException(name, "추락 깊이는 유한한 0 이상의 값이어야 합니다.");
        }

        /// <summary>낭떠러지 추락의 작아지기 시작 지점 조건입니다(유한한 0 이상 1 미만). 1이면 작아지는 시간이 없어 끝에 크기가 튑니다.</summary>
        internal static void ValidateFallShrinkStart(float start, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(start) || start >= 1f)
                throw new ArgumentOutOfRangeException(name, "추락 작아지기 시작 지점은 0 이상 1 미만이어야 합니다.");
        }

        /// <summary>낭떠러지 추락 끝 크기 조건입니다(0보다 크고 1 이하). 크기 대여가 0 배율을 받지 않아 0은 안 됩니다.</summary>
        internal static void ValidateFallEndScale(float scale, string name)
        {
            if (!CombatPresentationValueRules.IsPositive(scale) || scale > 1f)
                throw new ArgumentOutOfRangeException(name, "추락 끝 크기는 0보다 크고 1 이하여야 합니다.");
        }

        /// <summary>약한 지반 붕괴 연출의 떨림 세기·빠르기·가라앉기 깊이 조건입니다(유한한 0 이상, 타일 교체 설계 TR8). 값 객체도 이 조건을 씁니다.</summary>
        internal static void ValidateTileCollapseAmount(float value, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(value))
                throw new ArgumentOutOfRangeException(name, "약한 지반 붕괴 연출의 떨림 세기·빠르기·가라앉기 깊이는 유한한 0 이상의 값이어야 합니다.");
        }

        /// <summary>카메라 줌 배율 조건입니다(유한한 양수). 맵 카메라 기준 렌즈값에 곱하고, 작을수록 확대입니다(카메라 연출 조각 설계 K1).</summary>
        internal static void ValidateZoomRatio(float ratio, string name)
        {
            if (!CombatPresentationValueRules.IsPositive(ratio))
                throw new ArgumentOutOfRangeException(name, "카메라 줌 배율은 유한한 양수여야 합니다.");
        }

        /// <summary>카메라 흔들림 세기 조건입니다(유한한 0 이상). 세기는 아래 방향 속도의 크기로 쓰는 우리 값이라 여기에 둡니다.</summary>
        internal static void ValidateShakeStrength(float strength, string name)
        {
            if (!CombatPresentationValueRules.IsNonNegative(strength))
                throw new ArgumentOutOfRangeException(name, "카메라 흔들림 세기는 유한한 0 이상의 값이어야 합니다.");
        }
    }

    [CreateAssetMenu(fileName = "NewCombatPresentationSettingsData", menuName = "ProjectL/Database/View/CombatPresentationSettingsData")]
    internal sealed class CombatPresentationSettingsDataSO : DatabaseItem
    {
        [SerializeField]
        private CombatPresentationSettingsData combatPresentationSettingsData = new();
        public override IData GetData() => combatPresentationSettingsData;
    }
}
