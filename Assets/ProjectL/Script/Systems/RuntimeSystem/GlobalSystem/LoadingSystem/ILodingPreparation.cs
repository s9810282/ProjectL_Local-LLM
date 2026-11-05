using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem
{
    public enum LodingPreparationState
    {
        Pending,
        Complete,
        Cancelled,
        Failed
    }

    /// <summary>
    /// AfterLoad 뒤, Reveal 전에 진행하는 씬 연결당 한 번의 준비입니다.
    /// Complete는 임시 대여와 구독까지 정리한 성공만 뜻합니다. Cancel은 반복 호출할 수 있습니다.
    /// </summary>
    public interface ILodingPreparation
    {
        LodingPreparationState State { get; }
        void Begin(bool isCovered);
        void Tick(float deltaTime);
        void Cancel();
    }

    /// <summary>등록 때 실행하지 않습니다. 씬 진입마다 새 작업을 받아 등록 순서로 진행합니다.</summary>
    public sealed class LodingPreparationNode
    {
        private readonly Func<ILodingPreparation> _create;

        public LodingPreparationNode(Func<ILodingPreparation> create)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
        }

        internal ILodingPreparation Create() => _create()
            ?? throw new InvalidOperationException("로딩 준비 작업 생성 결과가 null입니다.");
    }
}
