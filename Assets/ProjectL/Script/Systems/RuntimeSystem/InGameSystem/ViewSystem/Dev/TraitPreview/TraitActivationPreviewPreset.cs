#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview
{
    public enum TraitActivationPreviewContext
    {
        Moment,
        ChildAttack,
        Interception
    }

    /// <summary>연출 확인용 입력입니다. 규칙이나 연출 에셋 값의 사본을 저장하지 않습니다.</summary>
    [Serializable]
    public sealed class TraitActivationPreviewInput
    {
        public UnityEngine.Object traitAsset;
        public TraitActivationKind kind = TraitActivationKind.AttackGranted;
        public TraitActivationPreviewContext context;
        public int stageViewKey = 10000;
        public int sourceIndex;
        public int attackerIndex = 2;
        public List<int> targetIndices = new() { 1 };
        public List<Vector2Int> cells = new();
        public Vector2Int targetCell = new(3, 3);
        public Rotation4 direction = Rotation4.R0;
        public int magnitude = 5;
        public int damage = 10;
        public bool kill;
        public bool repeat;
        public float observationSeconds = 1f;
        public float repeatInterval = 0.5f;
        public List<TraitActivationPreviewUnit> units = new()
        {
            new() { key = EntityKey.Unit_Akimbo, cell = new Vector2Int(2, 3) },
            new() { key = EntityKey.Unit_Demon, enemy = true, cell = new Vector2Int(3, 3) },
            new() { key = EntityKey.Unit_Gravekeeper, enemy = true, cell = new Vector2Int(5, 3) }
        };

        public TraitActivationPreviewInput Copy() => JsonUtility.FromJson<TraitActivationPreviewInput>(JsonUtility.ToJson(this));
    }

    /// <summary>화면에 놓을 유닛의 합성 사실 입력입니다. 값은 Core 계산에서 얻은 것이 아닙니다.</summary>
    [Serializable]
    public sealed class TraitActivationPreviewUnit
    {
        public EntityKey key = EntityKey.Unit_Akimbo;
        public bool enemy;
        public Vector2Int cell;
        public Rotation4 direction;
        public int health = 100;
        public int maxHealth = 100;
        public int attack = 20;
        public int defense;
    }

    /// <summary>팀에서 공유하는 입력 프리셋입니다. 원본 Activation 값은 원본 SO에 남습니다.</summary>
    [CreateAssetMenu(menuName = "ProjectL/Editor/Trait Activation Preview Preset")]
    public sealed class TraitActivationPreviewPreset : ScriptableObject
    {
        public TraitActivationPreviewInput input = new();
    }
}
#endif
