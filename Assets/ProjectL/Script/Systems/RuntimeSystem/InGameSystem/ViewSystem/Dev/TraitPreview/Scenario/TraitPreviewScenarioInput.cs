#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    public enum PreviewEntryKind
    {
        Packet,
        TimeWait,
        ManualWait
    }

    public enum PreviewBlockKind
    {
        Movement,
        Attack,
        State,
        Activation,
        ChildAttack,
        Interception,
        Health,
        Return,
        FieldPlacement
    }

    public enum PreviewStateKind
    {
        Acceleration,
        // v1 직렬화 번호를 보존합니다. 새 입력은 Effect를 사용합니다.
        AttackGrant,
        Effect,
        Cohesion,
        Regeneration
    }

    public enum PreviewStateOperation
    {
        Set,
        Remove
    }

    public enum PreviewEventRelation
    {
        Recorded,
        AttackHit
    }

    /// <summary>원본 연출 값 대신 배치와 사건 블록을 저장하는 시연 입력입니다.</summary>
    [Serializable]
    public sealed class TraitPreviewScenarioInput
    {
        public int version = 3;
        public int stageViewKey = 10000;
        public bool repeat;
        public bool pauseBetweenPackets;
        public float observationSeconds = 1f;
        public float repeatInterval = 0.5f;
        public List<PreviewScenarioUnit> units = new();
        public List<PreviewScenarioEntry> entries = new();
        public TraitPreviewScenarioInput Copy() => JsonUtility.FromJson<TraitPreviewScenarioInput>(JsonUtility.ToJson(this));
    }

    [Serializable]
    public sealed class PreviewScenarioUnit
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "유닛";
        public TraitActivationPreviewUnit unit = new();
        public UnitActionUse actionUse = UnitActionUse.Available;
        public int distanceMoved;
        public bool ownsPositionSwap;
        public bool ownsReconSquad;
        public int reconIncreasePercent = 10;
        public int reconRadius = 1;
        public bool ownsAcceleration;
        public bool ownsCleanSweep;
        public int acceleration;
        // v1 가져오기에서만 소비하는 입력입니다.
        public List<PreviewGrantInput> grants = new();
        public List<PreviewEffectSourceInput> effects = new();
        public int cohesionDefenseBonus;
        public bool ownsRegeneration;
        public int healAmount = 10;
        public int healEveryOwnSideTurnEnds = 2;
        public int completedEligibleTurnEnds;
    }

    [Serializable]
    public sealed class PreviewScenarioEntry
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "재생 묶음";
        public PreviewEntryKind kind;
        public float seconds = 1f;
        public List<PreviewScenarioBlock> blocks = new();
    }

    /// <summary>활성 종류의 구체 payload만 사용합니다. 관리 참조 직렬화는 사용하지 않습니다.</summary>
    [Serializable]
    public sealed class PreviewScenarioBlock
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "블록";
        public PreviewBlockKind kind;
        public PreviewEventRelation relation;
        public PreviewMovementInput movement = new();
        public PreviewAttackInput attack = new();
        public PreviewStateInput state = new();
        public PreviewTraitInput trait = new();
        public PreviewHealthInput health = new();
        public PreviewReturnInput returned = new();
        public PreviewFieldPlacementInput placement = new();
    }

    [Serializable]
    public sealed class PreviewMovementInput
    {
        public string unitId;
        public List<Vector2Int> path = new();
        public Rotation4 direction;
    }

    [Serializable]
    public sealed class PreviewAttackInput
    {
        public string attackerId;
        public Rotation4 direction;
        public Vector2Int targetCell = new(3, 3);
        public List<PreviewHitInput> hits = new();
    }

    [Serializable]
    public sealed class PreviewHitInput
    {
        public string targetId;
        public int finalHealth = 90;
    }

    [Serializable]
    public sealed class PreviewTraitInput
    {
        public UnityEngine.Object asset;
        public TraitActivationKind kind = TraitActivationKind.AttackGranted;
        public string sourceId;
        public List<string> targetIds = new();
        public List<Vector2Int> cells = new();
        public int magnitude = 5;
    }

    [Serializable]
    public sealed class PreviewStateInput
    {
        public string unitId;
        public PreviewStateKind kind;
        public PreviewStateOperation operation;
        public int acceleration;
        // v1 입력 이전 전용입니다. 현재 재생기는 이 필드를 읽지 않습니다.
        public PreviewGrantInput grant = new();
        public PreviewEffectSourceInput effect = new();
        public int cohesionDefenseBonus;
        public int healAmount = 10;
        public int healEveryOwnSideTurnEnds = 2;
        public int completedEligibleTurnEnds;
    }

    [Serializable]
    public sealed class PreviewEffectSourceInput
    {
        public string id = Guid.NewGuid().ToString("N");
        public WeaknessExposureLifetimeKind lifetime = WeaknessExposureLifetimeKind.AffectedSideTurnEnds;
        public bool hasMovement;
        public long battleInstanceId = 1;
        public long controllerTurnSequence = 1;
        public int moveCommandNumber = 1;
        public StatusEffectKey key = StatusEffectKey.AttackIncrease;
        public EffectOwnerKind ownerKind = EffectOwnerKind.Trait;
        public string sourceId;
        public int ownerInstanceId;
        public TraitKey trait = TraitKey.CleanSweep;
        public int definitionKey;
        public int amount = 5;
        public int percent = 20;
        public int remainingTurns = 1;
        public bool eligible = true;
        public long lastResetControllerTurnCount;
    }

    [Serializable]
    public sealed class PreviewHealthInput
    {
        public string unitId;
        public int finalHealth = 100;
    }

    [Serializable]
    public sealed class PreviewReturnInput
    {
        public string unitId;
        public CharacterRuntimeArea area = CharacterRuntimeArea.Hand;
        public int reserveSlot;
    }

    [Serializable]
    public sealed class PreviewFieldPlacementInput
    {
        public CombatEventKind eventKind = CombatEventKind.Moved;
        public TraitKey movementTrait;
        public int moveCommandNumber = 1;
        public List<PreviewFieldPlacementEntry> entries = new();
    }

    [Serializable]
    public sealed class PreviewFieldPlacementEntry
    {
        public string unitId;
        public Vector2Int cell;
        public Rotation4 rotation;
        public CombatMovementAgency agency = CombatMovementAgency.Voluntary;
        public UnitActionUse actionUse = UnitActionUse.Available;
        public int distanceMoved;
    }

    [Serializable]
    public sealed class PreviewGrantInput
    {
        public string id = Guid.NewGuid().ToString("N");
        public string sourceId;
        public TraitKey trait = TraitKey.CleanSweep;
        public int amount = 5;
        public int remainingTurns = 1;
    }
}
#endif
