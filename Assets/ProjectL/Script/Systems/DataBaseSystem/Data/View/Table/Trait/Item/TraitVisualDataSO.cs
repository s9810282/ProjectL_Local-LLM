using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item
{
    /// <summary>
    /// 유닛 특성의 표시 이름, 아이콘, 설명과 발동 연출, 흔적 지형 외형입니다. 키는 InGame과 같은 TraitKey입니다.
    /// 발동 연출은 발동 종류마다 한 줄이고(<see cref="TraitActivationVisual"/>, 특성 발동 연출 설계 TA2), 줄이 없는 발동은 화면에 아무것도 내지 않습니다.
    /// 상시 이펙트 목록(<see cref="AlwaysEffects"/>)은 조건이 맞는 동안 늘 보이는 이펙트입니다(이펙트 끝 규칙 설계 EL3).
    /// </summary>
    [Serializable]
    internal sealed class TraitVisualData : IData
    {
        [SerializeField] private TraitKey key;
        [SerializeField] private string displayName;
        [Tooltip("특성 아이콘입니다. 비워도 됩니다.")]
        [SerializeField] private Sprite icon;
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [Tooltip("가시밭의 방어력 감소량이 양수일 때 덧붙일 문장입니다. {방어력감소율}을 사용할 수 있습니다. 다른 특성은 비워 둡니다.")]
        [TextArea(1, 3)]
        [SerializeField] private string defenseReductionDescription;
        [Tooltip("훈련 카드에 나오는 등급 수치 글입니다. {0} 지금, {1} 오르는 양, {2} 다음. 값이 둘인 특성은 {3}·{4}·{5}가 둘째 값입니다. "
            + "훈련되는 특성에 비우면 그 카드를 그릴 때 멈춥니다.")]
        [SerializeField] private string rankValueFormat;
        [Tooltip("이 특성이 발동할 때의 연출입니다. 발동 종류마다 한 줄이고, 줄이 없는 발동은 화면에 아무것도 내지 않습니다(지금과 같음).")]
        [SerializeField] private List<TraitActivationVisual> activations = new();
        [Tooltip("조건이 맞는 동안 몸에 늘 붙어 보이는 이펙트 목록입니다(희생·굳건한 의지 등). 항목마다 조건 하나와 이펙트 하나입니다. "
            + "끝은 조건이 정합니다(조건이 켜지면 내고 꺼지면 거둠). 조건이 꺼질 때까지 남아야 하므로 반복(Loop)·링 버퍼 파티클이 맞습니다.")]
        [SerializeField] private List<TraitAlwaysEffectEntry> alwaysEffects = new();
        [Tooltip("이 특성이 이동 경로에 남기는 흔적 지형(땅 다지기·먼지 연막)의 외형입니다. 흔적을 남기지 않는 특성은 비워 둡니다. "
            + "흔적을 남기는 특성인데 비우면 그 흔적은 그리지 않습니다(경고 한 번). 칸 하나에 흔적 하나씩, 칸 표면(타일·지형 윗면) 가운데에 놓습니다.")]
        [SerializeField] private GameObject trailPrefab;
        [Tooltip("흔적 외형을 칸 표면 가운데에서 옮길 거리입니다(필드 기준, 월드 단위). 바닥과 겹쳐 깜박이면 Y를 조금 올립니다.")]
        [SerializeField] private Vector3 trailLocalOffset;
        [Tooltip("먼지 연막 칸에 호버했을 때의 설명입니다. 비우면 설명 없이 범위만 표시합니다. "
            + "{사거리감소량}·{피해감소율}은 연막의 현재 수치로 바뀝니다. 특성 아이콘의 Description과 따로 작성합니다.")]
        [TextArea(2, 6)]
        [SerializeField] private string trailDescription;
        // 상시 이펙트 값입니다. DB 생성 검사(Validate)가 만들고, 같은 연결 동안 같은 객체를 돌려줍니다(유지 표시 부품이 값 객체로 "같은 이펙트"를 가림).
        [NonSerialized] private IReadOnlyList<TraitAlwaysEffect> _always;

        public TraitKey Key => key;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? key.ToString() : displayName;
        /// <summary>비어 있으면 null입니다.</summary>
        public Sprite Icon => icon;
        public string Description => description ?? string.Empty;
        internal string DefenseReductionDescription => defenseReductionDescription ?? string.Empty;
        /// <summary>등급 수치 글의 꼴입니다. 훈련되지 않는 특성은 비어 있습니다.</summary>
        internal string RankValueFormat => rankValueFormat;
        /// <summary>먼지 연막 칸 호버의 전용 설명입니다. 빈 값이면 설명 패널을 열지 않습니다.</summary>
        public string TrailDescription => trailDescription ?? string.Empty;

        /// <summary>그 발동 종류의 방향을 가리지 않는(Any) 연출 줄입니다. 없으면 false입니다(그 발동은 화면에 아무것도 내지 않음).</summary>
        internal bool TryGetActivation(TraitActivationKind kind, out TraitActivationVisual activation) =>
            TryGetActivation(kind, CombatActivationDirection.Any, out activation);

        /// <summary>
        /// 그 발동 종류의 줄 하나입니다(Any 줄이 먼저, 없으면 첫 방향 줄). 줄 목록을 보여 주는 도구(특성 연출 확인 도구)가 씁니다.
        /// 재생은 방향까지 보고 고르므로 이 함수를 쓰지 않습니다.
        /// </summary>
        internal bool TryGetFirstActivation(TraitActivationKind kind, out TraitActivationVisual activation)
        {
            if (TryGetActivation(kind, out activation))
                return true;
            if (activations != null)
                for (int i = 0; i < activations.Count; i++)
                    if (activations[i] != null && activations[i].Kind == kind)
                    {
                        activation = activations[i];
                        return true;
                    }
            activation = null;
            return false;
        }

        /// <summary>
        /// 그 발동 종류·방향과 꼭 맞는 연출 줄입니다(이펙트 끝 규칙 설계 EL7). 없으면 false입니다. 방향 줄이 없을 때 Any 줄로 가는 것은 쓰는 쪽(View 발동 줄 보관)이 정합니다.
        /// </summary>
        internal bool TryGetActivation(TraitActivationKind kind, CombatActivationDirection direction, out TraitActivationVisual activation)
        {
            if (activations != null)
            {
                for (int i = 0; i < activations.Count; i++)
                {
                    if (activations[i] != null && activations[i].Kind == kind && activations[i].Direction == direction)
                    {
                        activation = activations[i];
                        return true;
                    }
                }
            }
            activation = null;
            return false;
        }

        /// <summary>
        /// 조건이 맞는 동안 늘 보이는 이펙트 목록입니다(이펙트 끝 규칙 설계 EL3). 항목 번호가 유지 이펙트 열쇠의 일부입니다. 비어 있으면 빈 목록입니다.
        /// </summary>
        internal IReadOnlyList<TraitAlwaysEffect> AlwaysEffects => _always ??= BuildAlways();

        /// <summary>발동 연출 줄 목록입니다(빈 칸 없음, DB 생성 때 검사). 줄을 모두 훑는 곳(몸 글자 프리팹 모으기)이 씁니다.</summary>
        internal IReadOnlyList<TraitActivationVisual> Activations =>
            activations != null ? activations.AsReadOnly() : (IReadOnlyList<TraitActivationVisual>)Array.Empty<TraitActivationVisual>();

        /// <summary>
        /// 이 특성이 남기는 흔적 지형의 외형입니다(특성 시스템 View 설계 TS7, 결정 Q-TS5 A). 프리팹이 없으면 false입니다(그 흔적은 그리지 않음).
        /// 어느 특성이 흔적을 남기는지는 Core가 정하고, 이 칸은 외형만 줍니다.
        /// </summary>
        internal bool TryGetTrail(out GameObject prefab, out Vector3 localOffset)
        {
            prefab = trailPrefab;
            localOffset = trailLocalOffset;
            return trailPrefab != null;
        }

        internal void Validate()
        {
            if (key == TraitKey.None || !System.Enum.IsDefined(typeof(TraitKey), key))
                throw new InvalidOperationException($"[TraitVisualData] Trait key is invalid. Key: {key}");
            if (!IsFinite(trailLocalOffset))
                throw new InvalidOperationException($"[TraitVisualData] 흔적 위치 보정이 유한한 수가 아닙니다. Key: {key}, Offset: {trailLocalOffset}");
            ValidateRankValueFormat();
            // 연결마다 새로 만들어 인스펙터에서 고친 값을 다음 DB 생성부터 씁니다.
            _always = BuildAlways();
            if (activations == null)
                return;
            var kinds = new HashSet<(TraitActivationKind, CombatActivationDirection)>();
            for (int i = 0; i < activations.Count; i++)
            {
                TraitActivationVisual activation = activations[i]
                    ?? throw new InvalidOperationException($"[TraitVisualData] 발동 연출 줄 {i}이 비었습니다. Key: {key}");
                activation.Validate(key);
                if (!kinds.Add((activation.Kind, activation.Direction)))
                    throw new InvalidOperationException($"[TraitVisualData] 발동 연출 줄이 같은 발동 종류·방향을 두 번 씁니다. Key: {key}, Kind: {activation.Kind}, Direction: {activation.Direction}");
            }
        }

        private void ValidateRankValueFormat()
        {
            if (string.IsNullOrWhiteSpace(rankValueFormat))
                return;
            try
            {
                string.Format(rankValueFormat, new object[6]);
            }
            catch (FormatException error)
            {
                throw new InvalidOperationException($"[TraitVisualData] 등급 수치 글의 꼴이 맞지 않습니다. Key: {key}", error);
            }
        }

        private IReadOnlyList<TraitAlwaysEffect> BuildAlways()
        {
            if (alwaysEffects == null || alwaysEffects.Count == 0)
                return Array.Empty<TraitAlwaysEffect>();
            var values = new TraitAlwaysEffect[alwaysEffects.Count];
            for (int i = 0; i < alwaysEffects.Count; i++)
            {
                string context = $"[TraitVisualData] Key: {key}, 상시 이펙트 {i}";
                TraitAlwaysEffectEntry entry = alwaysEffects[i] ?? throw new InvalidOperationException($"{context}: 항목이 비었습니다.");
                try
                {
                    values[i] = entry.ToData(context);
                }
                catch (ArgumentOutOfRangeException error)
                {
                    throw new InvalidOperationException($"{context}: {error.Message}", error);
                }
            }
            return Array.AsReadOnly(values);
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    [CreateAssetMenu(fileName = "NewTraitVisualData", menuName = "ProjectL/Database/View/TraitVisualData")]
    internal sealed class TraitVisualDataSO : DatabaseItem
    {
        [SerializeField] private TraitVisualData traitVisualData = new();

        public override IData GetData() => traitVisualData;
    }
}
