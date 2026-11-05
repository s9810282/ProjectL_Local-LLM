using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// StageSelect 범례의 표시 수명만 기존 UISystem에 연결합니다.
    /// 서비스 연결·데이터·열기·닫기는 <see cref="UIBase"/> 기본 그대로입니다(검사 후속 정리 R-f UL9).
    /// </summary>
    public sealed class StageLegendUI : UIBase
    {
        public StageLegendUI()
        {
            Policy = UIOpenPolicy.Parallel;
        }

        public override UIId ID => UIId.StageSelect;
    }
}
