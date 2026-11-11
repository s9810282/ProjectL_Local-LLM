using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item
{
    /// <summary>스테이지 하나(또는 증강)의 화면 글을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2, 공통 레이아웃 설계 SO5).</summary>
    [CreateAssetMenu(fileName = "NewStageOfferTextData", menuName = "ProjectL/Database/View/StageOfferTextData")]
    internal sealed class StageOfferTextDataSO : DatabaseItem
    {
        [SerializeField] private Input stageOfferTextData = new();

        public override IData GetData() => stageOfferTextData.ToData();

        [Serializable]
        private sealed class LineInput
        {
            [SerializeField] internal StageOfferLineKind kind;
            [Tooltip("UnitDetails: {0} 레벨, {1} 보유 수 / ItemDetails: {0} 설명 / RecoveryProjected: {0} 예상 회복량 / RecoveryMaxHealthProjected: {0} 예상 최대 체력 증가량 / "
                + "TraitGrowthHeader: {0} 유닛 이름, {1} 레벨, {2} 종류 낱말 / TraitAcquireLabel·TraitEnhanceLabel: 낱말 / "
                + "TrainingDetails: {0} 효과·수치 변화 글, {1} 특성 기본 설명 / TrainingRank: {0} 지금, {1} 다음 / TrainingEmpty·TrainingUnitNoTrait: 글 / "
                + "TrainingEffectEnabled·TrainingEffectDisabled: 적용·미적용 상태 낱말(인자 없음) / "
                + "TrainingUnitLevel: {0} 레벨 / TrainingUnitTraits: {0} 특성 이름들 / TrainingTraitPrompt: {0} 유닛 이름")]
            [SerializeField, TextArea(1, 4)] internal string format;
        }

        [Serializable]
        private sealed class ErrorInput
        {
            [SerializeField] internal StageOfferErrorKind kind;
            [SerializeField, TextArea(1, 4)] internal string text;
        }

        [Serializable]
        private sealed class Input
        {
            [Tooltip("이 글을 쓰는 곳입니다. Stage = 팝업 화면이 있는 스테이지, TraitGrowth = 증강(항목 하나), Info = 화면 없이 건물 호버의 이름·설명만(전투 등).")]
            [SerializeField] private StageOfferTextTarget target = StageOfferTextTarget.Stage;
            [Tooltip("이 글을 쓰는 스테이지 키 목록입니다. 같은 글이면 SO 하나를 함께 씁니다. 증강은 비웁니다. 등록되지 않은 스테이지를 열면 예외입니다. Info에서 비우면 아래 종류·역할의 공통 줄이 됩니다.")]
            [SerializeField] private List<int> stageKeys = new();
            [Tooltip("Info이고 Stage Keys가 빌 때만 씁니다. 이 종류·역할의 스테이지 중 전용 줄이 없는 것에 이 글을 씁니다.")]
            [SerializeField] private StageType stageType = StageType.Combat;
            [Tooltip("Info이고 Stage Keys가 빌 때만 씁니다.")]
            [SerializeField] private StageMapRole mapRole = StageMapRole.Regular;
            [Tooltip("EventNameText 글입니다. 건물 호버의 제목이기도 합니다.")]
            [SerializeField] private string eventName;
            [Tooltip("EventDescText 글입니다. 건물 호버의 설명이기도 합니다.")]
            [SerializeField, TextArea(1, 4)] private string eventDescription;
            [Tooltip("보유 골드 꼴입니다. {0}에 골드가 들어갑니다. 비우면 골드 칸이 꺼집니다.")]
            [SerializeField] private string goldFormat;
            [Tooltip("카드 가격 꼴입니다. {0}에 가격이 들어갑니다. 가격을 보여 주는 화면만 씁니다.")]
            [SerializeField] private string priceFormat;
            [Tooltip("고를 수 있는 카드의 버튼 글입니다.")]
            [SerializeField] private string selectButtonText;
            [Tooltip("이미 고른 카드의 버튼 글입니다. 쓰지 않는 화면은 비웁니다.")]
            [SerializeField] private string completedButtonText;
            [Tooltip("고를 수 없는 카드의 버튼 글입니다. 쓰지 않는 화면은 비웁니다.")]
            [SerializeField] private string unavailableButtonText;
            [Tooltip("나가기 버튼 글입니다. 나가기가 없는 증강은 비웁니다.")]
            [SerializeField] private string exitButtonText;
            [Tooltip("훈련소 특성 단계에서 유닛 목록으로 돌아가는 버튼 글입니다. 쓰지 않는 화면은 비웁니다.")]
            [SerializeField] private string backButtonText;
            [Tooltip("카드 설명·머리줄 꼴 목록입니다. 이 화면이 쓰는 종류만 넣습니다. 없는 종류를 쓰면 그때 예외입니다.")]
            [SerializeField] private List<LineInput> lines = new();
            [Tooltip("사유별 글이 없을 때 ErrorText에 넣는 글입니다.")]
            [SerializeField, TextArea(1, 4)] private string defaultErrorText;
            [Tooltip("사유별 ErrorText 글 목록입니다.")]
            [SerializeField] private List<ErrorInput> errors = new();

            internal StageOfferTextData ToData() => new(
                target,
                stageKeys != null ? stageKeys.ToArray() : Array.Empty<int>(),
                stageType,
                mapRole,
                eventName,
                eventDescription,
                goldFormat,
                priceFormat,
                selectButtonText,
                completedButtonText,
                unavailableButtonText,
                exitButtonText,
                backButtonText,
                ToLines(),
                defaultErrorText,
                ToErrors());

            private KeyValuePair<StageOfferLineKind, string>[] ToLines()
            {
                int count = lines?.Count ?? 0;
                var result = new KeyValuePair<StageOfferLineKind, string>[count];
                for (int i = 0; i < count; i++)
                    result[i] = new KeyValuePair<StageOfferLineKind, string>(lines[i].kind, lines[i].format);
                return result;
            }

            private KeyValuePair<StageOfferErrorKind, string>[] ToErrors()
            {
                int count = errors?.Count ?? 0;
                var result = new KeyValuePair<StageOfferErrorKind, string>[count];
                for (int i = 0; i < count; i++)
                    result[i] = new KeyValuePair<StageOfferErrorKind, string>(errors[i].kind, errors[i].text);
                return result;
            }
        }
    }
}
