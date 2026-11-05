using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>양수 요청 번호와 원본 명령에 대응하는 회복 결과를 함께 보관한다.</summary>
    public sealed class RecoveryProcessedResult
    {
        public int Number { get; }
        public RecoveryCommandData Command { get; }
        public RecoveryOperationResult Result { get; }

        public RecoveryProcessedResult(int number, RecoveryCommandData command, RecoveryOperationResult result)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (result.Kind == RecoveryResultKind.StateMismatch)
            {
                if (command.Kind == RecoveryCommandKind.Complete && result.FailureReason == RecoveryFailureReason.UnknownOption)
                {
                    throw new ArgumentException("옵션 없는 완료 요청에는 알 수 없는 옵션 결과를 연결할 수 없습니다.", nameof(result));
                }
            }
            else
            {
                if (result.StateAfter.Context != command.Context)
                {
                    throw new ArgumentException("회복 결과의 진입 문맥이 원본 요청과 일치해야 합니다.", nameof(result));
                }
                if (command.Kind == RecoveryCommandKind.Complete)
                {
                    if (result.Kind != RecoveryResultKind.Accepted || result.Progression == null)
                    {
                        throw new ArgumentException("완료 요청에는 완료 수락 결과가 필요합니다.", nameof(result));
                    }
                }
                else
                {
                    ValidateSelection(command, result);
                }
            }

            Number = number;
            Command = command;
            Result = result;
        }

        private static void ValidateSelection(RecoveryCommandData command, RecoveryOperationResult result)
        {
            RecoveryOptionState requestedOption = null;
            foreach (RecoveryOptionState option in result.StateAfter.Options)
            {
                if (option.Option.Id == command.OptionId.Value)
                {
                    requestedOption = option;
                    break;
                }
            }
            if (requestedOption == null)
            {
                throw new ArgumentException("선택 결과에는 요청한 옵션이 설치되어 있어야 합니다.", nameof(result));
            }

            if (result.Kind == RecoveryResultKind.Accepted)
            {
                if (result.Progression != null || result.StateAfter.Receipt.AppliedOptionId != command.OptionId)
                {
                    throw new ArgumentException("선택 수락 영수증은 요청한 옵션과 일치하고 진행 결과가 없어야 합니다.", nameof(result));
                }
            }
            else if (result.FailureReason == RecoveryFailureReason.AlreadyResolved)
            {
                if (result.StateAfter.Receipt.AppliedOptionId == command.OptionId)
                {
                    throw new ArgumentException("같은 옵션 재요청은 거래 거절이 아닌 기존 선택 수락으로 표현해야 합니다.", nameof(result));
                }
            }
            else if (requestedOption.UnavailableReason != result.FailureReason)
            {
                throw new ArgumentException("거래 거절 사유는 요청한 옵션의 선택 불가 사유와 일치해야 합니다.", nameof(result));
            }
        }
    }
}
