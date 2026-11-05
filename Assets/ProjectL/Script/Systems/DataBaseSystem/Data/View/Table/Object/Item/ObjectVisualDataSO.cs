using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Object.Enum;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Object.Item
{
    /// <summary>Object 종류의 표현 데이터입니다. 규칙 수치와 현재 활성 상태는 포함하지 않습니다.</summary>
    internal sealed class ObjectVisualData : IData
    {
        public ObjectContentKind Kind { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public GameObject Prefab { get; }
        public Vector3 LocalOffset { get; }
        public Vector3 LocalScale { get; }
        /// <summary>발동·습득·생성 효과음 프리셋입니다(소리 창구 2차 설계 Q-ES2″). -1이면 소리 없음입니다.</summary>
        public int ObjectSoundPreset { get; }
        /// <summary>발동·습득 순간의 이펙트·흔들림 큐입니다(이펙트 창구 확장 설계 EC5). 비어 있으면 빈 묶음입니다.</summary>
        internal CombatEventCues ActivationCues { get; }

        /// <exception cref="ArgumentException">큐가 칸 규칙(<see cref="CombatCueEvent.ObjectActivated"/>)에 맞지 않을 때입니다.</exception>
        internal ObjectVisualData(ObjectContentKind kind, string displayName, string description, Sprite icon,
            GameObject prefab, Vector3 localOffset, Vector3 localScale, int objectSoundPreset, CombatEventCues activationCues)
        {
            if (kind == ObjectContentKind.None || !System.Enum.IsDefined(typeof(ObjectContentKind), kind))
            {
                throw new InvalidOperationException("Object 표현에는 지원되는 종류 키가 필요합니다.");
            }

            if (!float.IsFinite(localOffset.x) || !float.IsFinite(localOffset.y) || !float.IsFinite(localOffset.z)
                || !float.IsFinite(localScale.x) || !float.IsFinite(localScale.y) || !float.IsFinite(localScale.z)
                || localScale.x <= 0f || localScale.y <= 0f || localScale.z <= 0f)
            {
                throw new InvalidOperationException("Object 표현 보정은 유한한 값이고 크기는 양수여야 합니다.");
            }

            CombatPresentationSettingsData.ValidateSoundPreset(objectSoundPreset, $"{nameof(objectSoundPreset)} (Kind: {kind})");
            if (activationCues == null) throw new ArgumentNullException(nameof(activationCues));
            CombatPresentationCueRules.Require(activationCues, CombatCueEvent.ObjectActivated, $"[ObjectVisualData] Kind: {kind}");

            Kind = kind;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? kind.ToString() : displayName;
            Description = description ?? string.Empty;
            Icon = icon;
            Prefab = prefab;
            LocalOffset = localOffset;
            LocalScale = localScale;
            ObjectSoundPreset = objectSoundPreset;
            ActivationCues = activationCues;
        }
    }

    /// <summary>ViewGroup에서 설정하는 지뢰·깃발·배지 외형 SO입니다.</summary>
    [CreateAssetMenu(fileName = "NewObjectVisual", menuName = "ProjectL/Database/View/ObjectVisual")]
    internal sealed class ObjectVisualDataSO : DatabaseItem
    {
        [SerializeField] private ObjectContentKind kind = ObjectContentKind.Mine;
        [SerializeField] private string displayName;
        [TextArea(2, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;
        [Tooltip("셀 중심을 기준으로 한 표현 위치 보정입니다. 스테이지 배치 좌표가 아닙니다.")]
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [Tooltip("발동(지뢰)·습득(깃발·보안관 배지)·생성 효과음 프리셋 번호입니다. 유닛 소리처럼 슬롯이 달라도 이 번호 하나를 씁니다. -1이면 소리를 내지 않습니다(소리 창구 2차 설계 Q-ES2″).")]
        [SerializeField, Min(-1)] private int objectSoundPreset = -1;
        [Tooltip("발동(지뢰)·습득(깃발·보안관 배지) 순간의 이펙트·흔들림입니다. 시점은 Start(사건 순간)만, 이펙트 자리는 사건 칸(EventCell)만 됩니다. "
            + "지연을 주어도 다음 사건은 기다리지 않지만, 기차 사건과 결과 끝은 그만큼 기다립니다. 소리는 위 프리셋 번호가 냅니다(이펙트 창구 확장 설계 EC5).")]
        [SerializeField] private CombatVisualCueInput activationCues = new();

        public override IData GetData()
        {
            CombatEventCues cues = (activationCues ?? throw new InvalidOperationException($"[ObjectVisualData] 발동 큐 칸이 없습니다. Kind: {kind}")).ToCues();
            return new ObjectVisualData(kind, displayName, description, icon, prefab, localOffset, localScale, objectSoundPreset, cues);
        }
    }
}
