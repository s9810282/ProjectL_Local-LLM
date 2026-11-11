using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.UnitStatus.Item
{
    /// <summary>걸린 상태 하나의 HUD 외형을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2, 특성 발동 연출 설계 TA9).</summary>
    [CreateAssetMenu(fileName = "NewUnitStatusVisualData", menuName = "ProjectL/Database/View/UnitStatusVisualData")]
    internal sealed class UnitStatusVisualDataSO : DatabaseItem
    {
        [SerializeField] private Input unitStatusVisualData = new();

        public override IData GetData() => unitStatusVisualData.ToData();

        [Serializable]
        private sealed class Input
        {
            [Tooltip("Core 효과 키입니다. 같은 효과는 외형 하나를 등록하며 분류는 Core가 정합니다. 필수 행 누락은 전투 조립 전에 진단합니다.")]
            [SerializeField] private StatusEffectKey effectKey = StatusEffectKey.None;
            [Tooltip("HUD 설명 앞에 붙는 이름입니다. 비우면 종류 이름을 씁니다.")]
            [SerializeField] private string displayName;
            [Tooltip("유닛 HUD 상태 칸의 아이콘입니다. 비우면 HUD 기본 아이콘을 씁니다.")]
            [SerializeField] private Sprite icon;
            [Tooltip("HUD 설명 틀입니다. {0}·{1}…에 그 상태의 값이 차례로 들어갑니다. 칸 수와 차례의 원본은 효과별 표시 변환(CombatEffectFormatters)의 그 종류 줄이고, "
                + "지금 에셋의 틀이 예입니다. 그 종류가 주지 않는 칸 번호를 쓰면 전투 진입 때 멈춥니다. 비우면 표시 이름만 보입니다.")]
            [SerializeField, TextArea(2, 6)] private string descriptionTemplate;
            [Tooltip("이 상태가 유닛에 보이는 동안(HUD 아이콘과 같은 순간) 그 유닛 몸에 붙는 이펙트입니다(이펙트 끝 규칙 설계 EL2). 상태가 꺼지면 거둡니다. "
                + "같은 상태가 출처 여럿으로 걸려도 하나만 보입니다. Effect를 Empty로 두면 유지 이펙트가 없습니다.")]
            [SerializeField] private CombatHeldEffectEntry whileEffect = new();
            [Tooltip("이 발동 종류가 그 유닛에 오면 유지 이펙트를 거두고 다시 냅니다. 예: 보호막의 ShieldReset(턴 끝 리셋마다 다시 재생). 유지 이펙트가 없으면 비웁니다.")]
            [SerializeField] private List<TraitActivationKind> restartOn = new();
            [Tooltip("이 상태가 유닛에 보이는 동안(HUD 아이콘과 같은 순간) 원래 모델 대신 보일 모델 프리팹입니다. 예: 재구성 중 잔해. 비우면 모습 그대로입니다. "
                + "유닛 아래 자식으로 놓여 넉백·피격 자세를 따라가고, 몸 상자·HUD는 원래 유닛 것을 씁니다. 렌더러만 두고 충돌체·FieldUnitView는 넣지 않습니다.")]
            [SerializeField] private GameObject whileAppearance;

            internal UnitStatusVisualData ToData() => new(effectKey, displayName, icon, descriptionTemplate,
                WhileEffect(), restartOn != null ? restartOn.ToArray() : Array.Empty<TraitActivationKind>(), whileAppearance);

            // 유지 이펙트 값은 만들 때 스스로 검사합니다. 어느 상태 외형인지 알 수 있게 감쌉니다(묶음 끝 검토 L9).
            private CombatHeldEffect WhileEffect()
            {
                try
                {
                    return whileEffect?.ToData();
                }
                catch (ArgumentOutOfRangeException error)
                {
                    throw new InvalidOperationException($"[UnitStatusVisualData] 유지 이펙트 값이 맞지 않습니다. Key: {effectKey}. {error.Message}", error);
                }
            }
        }
    }
}
