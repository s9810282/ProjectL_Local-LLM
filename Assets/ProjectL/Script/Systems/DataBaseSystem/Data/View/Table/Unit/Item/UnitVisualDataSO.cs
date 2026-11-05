using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item
{
    /// <summary>유닛·구조물의 프리팹, 표시 이름, 아이콘입니다. 키는 EntityKey이고 View는 이 값을 Buffer에서 받습니다.</summary>
    [Serializable]
    internal sealed class UnitVisualData : IData
    {
        [SerializeField] private EntityKey key;
        [SerializeField] private GameObject prefab;
        [Tooltip("비워 두면 프리팹 이름을 표시합니다.")]
        [SerializeField] private string displayName;
        [Tooltip("호버에 표시할 설명입니다. 장애물도 이 Visual SO에서 작성합니다.")]
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [Tooltip("타임라인 UI에 쓰는 아이콘입니다. 비워도 됩니다.")]
        [SerializeField] private Sprite timelineIcon;
        [Tooltip("유닛 정보 UI에 쓰는 아이콘입니다. 비워도 됩니다.")]
        [SerializeField] private Sprite infoIcon;
        [Tooltip("비워 두면 현재의 즉시 공격 표시를 유지합니다. 같은 연출 설정을 여러 유닛이 공유할 수 있습니다.")]
        [SerializeField] private CombatAttackPresentationSO attackPresentation;
        [Tooltip("피격·사망의 공유 표현 설정입니다. 비워 두면 기존 즉시 사실 표시를 유지합니다.")]
        [SerializeField] private CombatUnitReactionPresentationSO reactionPresentation;
        [Tooltip("공격·피격 애니메이션 클립 묶음입니다. 비워 두면 코드 모션만 씁니다. 여러 유닛이 공유할 수 있습니다.")]
        [SerializeField] private CombatUnitAnimationSO animationPresentation;
        [Tooltip("아군일 때 선택·이동·취소·사망 효과음 프리셋 번호입니다. -1이면 소리를 내지 않습니다. 적은 이 값 대신 전투 연출 설정의 적 공용 프리셋을 씁니다(View 3라운드 W9-2). 장애물(화약통·보급품) 외형이면 피해·파괴 효과음 프리셋이고, -1이면 프리셋 없이 냅니다(소리 창구 2차 Q-ES3).")]
        [SerializeField, Min(-1)] private int unitSoundPreset = -1;
        [Tooltip("공격(포효 포함) 효과음 프리셋 번호입니다. -1이면 소리를 내지 않습니다(View 3라운드 W9-2).")]
        [SerializeField, Min(-1)] private int attackSoundPreset = -1;
        [Tooltip("장애물(화약통·보급품) 외형에서만 씁니다. 다른 외형에 넣으면 DB 생성이 멈춥니다. 장애물이 피해를 받은 순간의 이펙트·흔들림이고, "
            + "시점은 Start(사건 순간)만, 이펙트 자리는 사건 칸(EventCell)만 됩니다. 지연을 주어도 다음 사건은 기다리지 않지만, 기차 사건과 결과 끝은 그만큼 기다립니다(이펙트 창구 확장 설계 EC5).")]
        [SerializeField] private CombatVisualCueInput obstacleHitCues = new();
        [Tooltip("일반 적 Structure 피격 이펙트입니다. Start·지연 0·SubjectBody만 허용합니다.")]
        [SerializeField] private CombatVisualCueInput structureHitCues = new();
        [Tooltip("장애물(화약통·보급품) 외형에서만 씁니다. 다른 외형에 넣으면 DB 생성이 멈춥니다. 장애물이 파괴된 순간의 이펙트·흔들림이고, "
            + "시점은 Start(사건 순간)만, 이펙트 자리는 사건 칸(EventCell)만 됩니다. 지연을 주어도 다음 사건은 기다리지 않지만, 기차 사건과 결과 끝은 그만큼 기다립니다(이펙트 창구 확장 설계 EC5).")]
        [SerializeField] private CombatVisualCueInput obstacleDestroyedCues = new();

        public EntityKey Key => key;
        public string Description => description ?? string.Empty;
        public GameObject Prefab => prefab;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) && prefab != null
            ? prefab.name
            : displayName;
        /// <summary>비어 있으면 null입니다.</summary>
        public Sprite TimelineIcon => timelineIcon;
        /// <summary>비어 있으면 null입니다.</summary>
        public Sprite InfoIcon => infoIcon;
        /// <summary>명시적으로 연결된 공격 표시 설정의 불변 사본입니다. 비어 있으면 null입니다.</summary>
        public CombatAttackPresentationData AttackPresentation => attackPresentation != null
            ? attackPresentation.CreateData()
            : null;
        /// <summary>공격 종류와 독립적인 피격·사망 설정입니다. 비어 있으면 null입니다.</summary>
        public CombatUnitReactionPresentationData ReactionPresentation => reactionPresentation != null
            ? reactionPresentation.CreateData()
            : null;
        /// <summary>공격·피격 클립 묶음의 불변 값입니다(클립 설계 AC1). 비어 있으면 null이고 그 유닛은 코드 모션만 씁니다.</summary>
        public CombatUnitAnimationData AnimationPresentation => animationPresentation != null
            ? animationPresentation.CreateData()
            : null;
        /// <summary>아군일 때의 유닛 효과음 프리셋입니다. -1이면 소리 없음입니다. 장애물 외형이면 피해·파괴 프리셋이고, -1이면 프리셋 없이 냅니다(소리 창구 2차 Q-ES3).</summary>
        public int UnitSoundPreset => unitSoundPreset;
        /// <summary>공격 효과음 프리셋입니다. -1이면 소리 없음입니다.</summary>
        public int AttackSoundPreset => attackSoundPreset;
        /// <summary>장애물 피해 순간 큐입니다. 읽을 때마다 새 값을 만들고, 보관은 쓰는 쪽이 합니다(이펙트 창구 확장 계획 P6).</summary>
        internal CombatEventCues ObstacleHitCues => ReadCues(obstacleHitCues, nameof(obstacleHitCues));
        internal CombatEventCues StructureHitCues => (structureHitCues ?? new CombatVisualCueInput()).ToCues();
        /// <summary>장애물 파괴 순간 큐입니다. 읽을 때마다 새 값을 만들고, 보관은 쓰는 쪽이 합니다(이펙트 창구 확장 계획 P6).</summary>
        internal CombatEventCues ObstacleDestroyedCues => ReadCues(obstacleDestroyedCues, nameof(obstacleDestroyedCues));

        internal void Validate()
        {
            if (key == EntityKey.None || !System.Enum.IsDefined(typeof(EntityKey), key))
                throw new InvalidOperationException($"[UnitVisualData] Entity key is invalid. Key: {key}");
            if (prefab == null)
                throw new InvalidOperationException($"[UnitVisualData] Prefab is required. Key: {key}");
            CombatAttackPresentationData attack = attackPresentation != null ? attackPresentation.CreateData() : null;
            CombatUnitReactionPresentationData reaction = reactionPresentation != null ? reactionPresentation.CreateData() : null;
            // 전투 클립 규칙(이벤트 컴포넌트·Idle 컨트롤러·클립 묶음)은 규칙 클래스 한 곳이 원본입니다(클립 설계 AC1·AC4).
            CombatUnitAnimationRules.ValidateUnit(prefab, animationPresentation, attack, reaction, $"Key: {key}");
            CombatPresentationSettingsData.ValidateSoundPreset(unitSoundPreset, $"{nameof(unitSoundPreset)} (Key: {key})");
            CombatPresentationSettingsData.ValidateSoundPreset(attackSoundPreset, $"{nameof(attackSoundPreset)} (Key: {key})");
            CombatEventCues obstacleHit = ObstacleHitCues;
            CombatEventCues structureHit = StructureHitCues;
            CombatPresentationCueRules.Require(structureHit, CombatCueEvent.StructureHit, $"[UnitVisualData] Key: {key}, structureHitCues");
            if (ObstacleContentProfile.IsNativeObstacleKey(key) && !structureHit.IsEmpty)
                throw new InvalidOperationException($"[UnitVisualData] 장애물에는 structureHitCues를 넣을 수 없습니다. Key: {key}");
            CombatEventCues obstacleDestroyed = ObstacleDestroyedCues;
            CombatPresentationCueRules.Require(obstacleHit, CombatCueEvent.ObstacleHit, $"[UnitVisualData] Key: {key}");
            CombatPresentationCueRules.Require(obstacleDestroyed, CombatCueEvent.ObstacleDestroyed, $"[UnitVisualData] Key: {key}");
            // 장애물 큐는 장애물 외형에서만 받습니다. 장애물인지는 Core 규칙 하나를 그대로 씁니다(계획 P18).
            if (!ObstacleContentProfile.IsNativeObstacleKey(key) && (!obstacleHit.IsEmpty || !obstacleDestroyed.IsEmpty))
                throw new InvalidOperationException(
                    $"[UnitVisualData] 장애물 큐({nameof(obstacleHitCues)}·{nameof(obstacleDestroyedCues)})는 장애물(화약통·보급품) 외형에서만 씁니다. Key: {key}");
        }

        private CombatEventCues ReadCues(CombatVisualCueInput input, string name) =>
            (input ?? throw new InvalidOperationException($"[UnitVisualData] {name} 칸이 없습니다. Key: {key}")).ToCues();
    }

    [CreateAssetMenu(fileName = "NewUnitVisualData", menuName = "ProjectL/Database/View/UnitVisualData")]
    internal sealed class UnitVisualDataSO : DatabaseItem
    {
        [SerializeField] private UnitVisualData unitVisualData = new();

        public override IData GetData() => unitVisualData;
    }
}
