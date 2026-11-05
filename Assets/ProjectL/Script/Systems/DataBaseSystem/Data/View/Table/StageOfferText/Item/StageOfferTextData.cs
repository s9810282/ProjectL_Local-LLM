using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item
{
    /// <summary>
    /// 스테이지 하나(또는 증강)의 화면 글입니다. 화면 글의 원본은 SO이고 코드는 값만 넘깁니다(공통 레이아웃 설계 SO5).
    /// 이름·설명은 건물 호버의 글이기도 합니다. <see cref="StageOfferTextTarget.Info"/> 줄은 이름·설명만 씁니다(스테이지 맵 설계 §5.7 3판).
    /// </summary>
    internal sealed class StageOfferTextData : IData
    {
        private readonly int[] _stageKeys;
        private readonly KeyValuePair<StageOfferLineKind, string>[] _lines;
        private readonly KeyValuePair<StageOfferErrorKind, string>[] _errors;
        private readonly string _goldFormat;
        private readonly string _priceFormat;
        private readonly string _completedButtonText;
        private readonly string _unavailableButtonText;
        private readonly string _exitButtonText;
        private readonly string _backButtonText;
        private readonly string _defaultErrorText;

        internal StageOfferTextData(
            StageOfferTextTarget target,
            int[] stageKeys,
            StageType stageType,
            StageMapRole mapRole,
            string eventName,
            string eventDescription,
            string goldFormat,
            string priceFormat,
            string selectButtonText,
            string completedButtonText,
            string unavailableButtonText,
            string exitButtonText,
            string backButtonText,
            KeyValuePair<StageOfferLineKind, string>[] lines,
            string defaultErrorText,
            KeyValuePair<StageOfferErrorKind, string>[] errors)
        {
            Target = target;
            _stageKeys = stageKeys ?? throw new ArgumentNullException(nameof(stageKeys));
            StageType = stageType;
            MapRole = mapRole;
            EventName = eventName;
            EventDescription = eventDescription;
            _goldFormat = goldFormat;
            _priceFormat = priceFormat;
            SelectButtonText = selectButtonText;
            _completedButtonText = completedButtonText;
            _unavailableButtonText = unavailableButtonText;
            _exitButtonText = exitButtonText;
            _backButtonText = backButtonText;
            _lines = lines ?? throw new ArgumentNullException(nameof(lines));
            _defaultErrorText = defaultErrorText;
            _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        internal StageOfferTextTarget Target { get; }
        internal IReadOnlyList<int> StageKeys => _stageKeys;
        /// <summary>키 목록이 빈 <see cref="StageOfferTextTarget.Info"/> 줄이면 종류·역할 공통 줄입니다.</summary>
        internal bool IsCommonInfo => Target == StageOfferTextTarget.Info && _stageKeys.Length == 0;
        /// <summary>공통 줄의 종류입니다. 공통 줄이 아니면 쓰지 않습니다.</summary>
        internal StageType StageType { get; }
        /// <summary>공통 줄의 역할입니다. 공통 줄이 아니면 쓰지 않습니다.</summary>
        internal StageMapRole MapRole { get; }
        internal string EventName { get; }
        internal string EventDescription { get; }
        internal string SelectButtonText { get; }
        internal string CompletedButtonText => Required(_completedButtonText, "completedButtonText");
        internal string UnavailableButtonText => Required(_unavailableButtonText, "unavailableButtonText");
        internal string ExitButtonText => Required(_exitButtonText, "exitButtonText");
        internal string BackButtonText => Required(_backButtonText, "backButtonText");

        /// <summary>보유 골드 글입니다. 꼴이 비어 있으면 골드를 쓰지 않는 화면이라 빈 글입니다(R2).</summary>
        internal string FormatGold(int amount) =>
            string.IsNullOrWhiteSpace(_goldFormat) ? string.Empty : string.Format(_goldFormat, amount);

        internal string FormatPrice(int cost) => string.Format(Required(_priceFormat, "priceFormat"), cost);

        internal string FormatLine(StageOfferLineKind kind, params object[] values)
        {
            foreach (KeyValuePair<StageOfferLineKind, string> line in _lines)
            {
                if (line.Key == kind)
                    return string.Format(line.Value, values);
            }

            throw new InvalidOperationException($"[StageOfferTextData] '{EventName}'에 글 종류 {kind}가 없습니다.");
        }

        /// <summary>사유별 글이 없으면 기본 글입니다.</summary>
        internal string ErrorOf(StageOfferErrorKind kind)
        {
            foreach (KeyValuePair<StageOfferErrorKind, string> error in _errors)
            {
                if (error.Key == kind)
                    return error.Value;
            }

            return _defaultErrorText;
        }

        /// <summary>글 종류마다 꼴에 들어가는 값 수입니다. 칸 차례의 원본입니다.</summary>
        internal static int ValueCountOf(StageOfferLineKind kind)
        {
            switch (kind)
            {
                case StageOfferLineKind.UnitDetails: // {0} 레벨, {1} 보유 수
                case StageOfferLineKind.EnhancementDetails: // {0} 캐릭터 이름, {1} 설명
                case StageOfferLineKind.TrainingRank: // {0} 지금 등급, {1} 다음 등급
                case StageOfferLineKind.TrainingDetails: // {0} 등급 수치 글, {1} 특성 설명
                    return 2;
                case StageOfferLineKind.ItemDetails: // {0} 설명
                case StageOfferLineKind.RecoveryProjected: // {0} 예상 회복량
                case StageOfferLineKind.RecoveryMaxHealthProjected: // {0} 예상 최대 체력 증가량
                case StageOfferLineKind.TrainingUnitLevel: // {0} 레벨
                case StageOfferLineKind.TrainingUnitTraits: // {0} 훈련할 수 있는 특성 이름들
                case StageOfferLineKind.TrainingTraitPrompt: // {0} 유닛 이름
                    return 1;
                case StageOfferLineKind.TraitGrowthHeader: // {0} 유닛 이름, {1} 레벨, {2} 종류 낱말
                    return 3;
                case StageOfferLineKind.TraitAcquireLabel:
                case StageOfferLineKind.TraitEnhanceLabel:
                case StageOfferLineKind.TrainingEmpty:
                case StageOfferLineKind.TrainingUnitNoTrait:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "[StageOfferTextData] 글 종류가 맞지 않습니다.");
            }
        }

        internal void Validate()
        {
            RequireText(EventName, "eventName");
            RequireText(EventDescription, "eventDescription");
            // Info 줄은 화면이 없어 버튼·안내 글을 보지 않습니다.
            if (Target != StageOfferTextTarget.Info)
            {
                RequireText(SelectButtonText, "selectButtonText");
                RequireText(_defaultErrorText, "defaultErrorText");
            }
            if (Target == StageOfferTextTarget.Stage) RequireText(_exitButtonText, "exitButtonText");
            ValidateTarget();
            ValidateFormat(_goldFormat, "goldFormat", 1);
            ValidateFormat(_priceFormat, "priceFormat", 1);
            ValidateLines();
            ValidateErrors();
        }

        private void ValidateTarget()
        {
            if (Target == StageOfferTextTarget.Stage && _stageKeys.Length == 0)
                throw Invalid("스테이지 글에 stageKeys가 없습니다");
            if (Target == StageOfferTextTarget.TraitGrowth && _stageKeys.Length > 0)
                throw Invalid("증강 글은 stageKeys를 비워야 합니다");
            if (Target != StageOfferTextTarget.Stage && Target != StageOfferTextTarget.TraitGrowth &&
                Target != StageOfferTextTarget.Info)
                throw Invalid($"target 값이 맞지 않습니다: {Target}");
            if (IsCommonInfo && (!System.Enum.IsDefined(typeof(StageType), StageType) ||
                                 !System.Enum.IsDefined(typeof(StageMapRole), MapRole)))
                throw Invalid($"공통 줄의 종류·역할이 맞지 않습니다: ({StageType}, {MapRole})");
        }

        private void ValidateLines()
        {
            var seen = new HashSet<StageOfferLineKind>();
            foreach (KeyValuePair<StageOfferLineKind, string> line in _lines)
            {
                if (!seen.Add(line.Key))
                    throw Invalid($"글 종류가 겹칩니다: {line.Key}");

                RequireText(line.Value, $"lines[{line.Key}]");
                ValidateFormat(line.Value, $"lines[{line.Key}]", ValueCountOf(line.Key));
            }
        }

        private void ValidateErrors()
        {
            var seen = new HashSet<StageOfferErrorKind>();
            foreach (KeyValuePair<StageOfferErrorKind, string> error in _errors)
            {
                if (error.Key == StageOfferErrorKind.None || !seen.Add(error.Key))
                    throw Invalid($"안내 사유가 비었거나 겹칩니다: {error.Key}");

                RequireText(error.Value, $"errors[{error.Key}]");
            }
        }

        // 비어 있는 꼴은 여기서 보지 않습니다. 쓰는 때 Required나 빈 글 규칙이 정합니다.
        private void ValidateFormat(string format, string field, int valueCount)
        {
            if (string.IsNullOrWhiteSpace(format))
                return;

            try
            {
                string.Format(format, new object[valueCount]);
            }
            catch (FormatException error)
            {
                throw new InvalidOperationException($"[StageOfferTextData] '{EventName}'의 {field} 꼴이 맞지 않습니다. 값 {valueCount}개까지 쓸 수 있습니다: {format}", error);
            }
        }

        private void RequireText(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw Invalid($"{field}가 비어 있습니다");
        }

        private string Required(string value, string field)
        {
            RequireText(value, field);
            return value;
        }

        private InvalidOperationException Invalid(string message) =>
            new($"[StageOfferTextData] '{EventName}': {message}.");
    }
}
