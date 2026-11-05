using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>진입 문맥과 선택 의도만 전달하는 불변 회복 입력이다.</summary>
    public sealed class RecoveryCommandData
    {
        public RecoveryCommandKind Kind { get; }
        public RecoveryEntryContext Context { get; }
        public RecoveryOptionId? OptionId { get; }

        private RecoveryCommandData(RecoveryCommandKind kind, RecoveryEntryContext context,
            RecoveryOptionId? optionId)
        {
            // 구조체 기본값은 생성자를 우회하므로 모든 진입 필드를 다시 검증한다.
            Context = new RecoveryEntryContext(context.EntryId, context.ChapterKey,
                context.NodeId, context.StageKey);
            Kind = kind;
            OptionId = optionId;
        }

        /// <summary>유효한 옵션 식별자를 가진 선택 입력을 만든다.</summary>
        public static RecoveryCommandData Select(RecoveryEntryContext context, RecoveryOptionId optionId)
        {
            if (optionId.IsEmpty)
            {
                throw new ArgumentException("회복 옵션 식별자는 비어 있을 수 없습니다.", nameof(optionId));
            }

            return new RecoveryCommandData(RecoveryCommandKind.Select, context, optionId);
        }

        /// <summary>옵션 선택 여부와 무관한 완료 입력을 만든다.</summary>
        public static RecoveryCommandData Complete(RecoveryEntryContext context)
        {
            return new RecoveryCommandData(RecoveryCommandKind.Complete, context, null);
        }
    }
}
