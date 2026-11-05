using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput
{
    /// <summary>
    /// 설명 구간 동안 빌리는 Layer입니다(튜토리얼 설계 §15.4.3). 튜토리얼 map만 담고 공통(ESC)·배치·전투 map은 담지 않아,
    /// 설명 중에는 G키·드래그·턴 종료·공격·ESC가 들어오지 않습니다.
    /// </summary>
    internal static class TutorialInputLayers
    {
        internal static readonly InputLayer Tutorial = new(TutorialInputNames.Map, TutorialInputNames.Map);
    }
}
