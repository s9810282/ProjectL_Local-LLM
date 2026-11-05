using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    public sealed class ItemGrantResult
    {
        public bool Accepted { get; }
        public string Reason { get; }
        public ItemInventoryChange Change { get; }

        public ItemGrantResult(bool accepted, string reason, ItemInventoryChange change)
        {
            if (reason == null) throw new ArgumentNullException(nameof(reason));
            if (accepted != (change != null) || (accepted ? reason.Length != 0 : string.IsNullOrWhiteSpace(reason)))
                throw new ArgumentException("성공에는 변경 제안만, 거절에는 사유만 있어야 합니다.");
            Accepted = accepted;
            Reason = reason;
            Change = change;
        }
    }
}
