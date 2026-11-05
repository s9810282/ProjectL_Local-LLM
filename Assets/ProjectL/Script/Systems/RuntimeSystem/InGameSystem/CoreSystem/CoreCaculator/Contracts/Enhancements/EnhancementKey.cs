
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public enum EnhancementKey
    {
        // ChangeRider 외의 옛 상품 키는 직렬화 호환성만 보존하며 다시 상품으로 등록하지 않는다.
        None = 0,
        LuckyBullet = 1,
        ChangeRider = 2,
        DoubleShot = 3,
        DoorBreaching = 4,
        ContemptForWeak = 5,
        ChainExplosion = 6,
        PoisonThorn = 7,
        PrecisionShot = 8 // 저작된 강화가 쓰지 않는 값(정밀 사격 강화 삭제). 이 번호는 재사용하지 않는다.
    }
}
