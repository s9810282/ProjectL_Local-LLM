using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item
{
    /// <summary>
    /// 전투 필드의 색 묶음입니다(필드 색 DB 설계 FC1). 칸 강조 층 색·채우기 불투명도·공격 궤적 색·넉백/끌어오기 화살표 색의 원본이고,
    /// <see cref="StageFieldViewData.Colors"/> 한 칸으로 들어갑니다. 필드는 이 묶음을 들고 있다가 그릴 때 읽습니다(FC2).
    /// </summary>
    /// <remarks>
    /// 기본값은 이 묶음을 옮기기 전 <c>Combat.unity</c>의 <c>FieldView</c> 씬 값입니다(계획 TP-1). 칸이 빠진 에셋도 같은 화면이 됩니다.
    /// 겹칠 때 우선순위는 호버 &gt; 층 표(<c>FieldHighlightLayers</c>의 Rows) 뒤쪽 층 &gt; … &gt; 앞쪽 층 &gt; 정적 구역이고, 그 표 한 곳이 원본입니다.
    /// 필드 순서는 인스펙터 묶음만 정하고 우선순위와는 상관없습니다.
    /// 에디터에서 값을 고치면 전투 중인 필드에 바로 반영됩니다(FC3). 플레이 중에 고친 값도 에셋에 남습니다(FC4).
    /// </remarks>
    [Serializable]
    internal sealed class FieldColorsData
    {
        [Header("정적 구역 색 (가장 아래 층)")]
        [Tooltip("아군 배치 구역 칸입니다(스테이지의 아군 배치 칸).\n쓰이는 곳: 손패·덱 카드를 끌어 유닛을 배치하는 동안.\n그리기: GridOutline = 배치 Drag 중에만 테두리·채우기 / CellPrefab = 늘 칸 색(가장 아래 층).")]
        [SerializeField] private Color playerZoneColor = new(0.55f, 0.75f, 1f, 1f);
        [Tooltip("레일(기차 선로) 칸입니다.\n쓰이는 곳: 레일이 있는 스테이지에서 늘.\n그리기: GridOutline = 늘 테두리(채우기 없음) / CellPrefab = 늘 칸 색(가장 아래 층).")]
        [SerializeField] private Color railColor = new(0.85f, 0.75f, 0.45f, 1f);

        [Header("호버 칸 색 (모든 층 위)")]
        [Tooltip("포인터가 올라간 칸, Core 답을 받기 전입니다.\n쓰이는 곳: 유닛 배치 Drag, 가방 아이템 장착 Drag, 이동 경로를 끄는 동안의 끝 칸. 공격 조준은 아래 공격 호버 색을 씁니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색, 모든 층 위.")]
        [SerializeField] private Color hoverColor = new(1f, 1f, 0.6f, 1f);
        [Tooltip("포인터가 올라간 칸, Core 답이 가능일 때입니다(배치 가능·장착 가능·그 칸까지 이동 가능).\n쓰이는 곳: 공용 호버 색(hoverColor)과 같은 셋.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색, 모든 층 위.")]
        [SerializeField] private Color hoverValidColor = new(0.35f, 1f, 0.45f, 1f);
        [Tooltip("포인터가 올라간 칸, Core 답이 불가일 때입니다(배치·장착·이동 불가).\n쓰이는 곳: 공용 호버 색(hoverColor)과 같은 셋.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색, 모든 층 위.")]
        [SerializeField] private Color hoverInvalidColor = new(1f, 0.3f, 0.3f, 1f);
        [Tooltip("공격 모드에서 포인터가 올라간 칸, Core 답을 받기 전입니다.\n쓰이는 곳: 공격 조준 호버. GridOutline에서는 공격이 닿는 칸(스플래시 포함)의 채우기도 이 상태 색을 따릅니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색, 모든 층 위.")]
        [SerializeField] private Color attackHoverColor = new(1f, 1f, 0.6f, 1f);
        [Tooltip("공격 모드에서 포인터가 올라간 칸, 공격 가능일 때입니다.\n쓰이는 곳: 공격 조준 호버, GridOutline의 공격이 닿는 칸 채우기.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색, 모든 층 위.")]
        [SerializeField] private Color attackHoverValidColor = new(0.35f, 1f, 0.45f, 1f);
        [Tooltip("공격 모드에서 포인터가 올라간 칸, 공격 불가일 때입니다.\n쓰이는 곳: ① 공격 조준 호버 ② GridOutline의 공격이 닿는 칸 채우기 ③ 공격 조준 중 전달·파급을 막는 벽 칸(막힌 벽, 테두리만).\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color attackHoverInvalidColor = new(1f, 0.3f, 0.3f, 1f);

        [Header("입력·미리보기 층 색 (위 칸이 이김)")]
        [Tooltip("공격이 닿는 칸과 결과 재생 중 잠깐 짚는 칸입니다.\n쓰이는 곳: ① 공격 조준 호버의 공격이 닿는 칸(스플래시 포함) 테두리 ② 결과 재생 강조 — 포효 범위·소환 칸·증원 칸(테두리만).\n그리기: GridOutline = 테두리(공격이 닿는 칸 채우기는 공격 호버 상태 색) / CellPrefab = 칸 색.")]
        [SerializeField] private Color affectedColor = new(1f, 0.55f, 0.15f, 1f);
        [Tooltip("끌어서 정한 이동 예정 경로입니다. 갈 수 있는지는 끝 칸의 호버 색으로 보입니다.\n쓰이는 곳: ① 이동 Drag 경로(GridOutline은 경로 띠·끝 화살촉, Movement Path 재질 색을 덮어씀) ② 끌어오기 공격 조준 때 대상이 끌려올 도착 칸(테두리만).\n그리기: CellPrefab = 경로·도착 칸의 칸 색. 끌어오기 화살표 색(pullArrowColor)의 기본값과 같습니다.")]
        [SerializeField] private Color movementDraftColor = new(0.05f, 1f, 0.75f, 1f);
        [Tooltip("진영을 가리지 않는 위험의 피해 칸입니다.\n쓰이는 곳: 지뢰·화약통에 호버했을 때 그 피해 칸. 유닛 호버 범위와 함께 나오지 않습니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color hazardRangeColor = new(1f, 0.15686275f, 0f, 1f);
        [Tooltip("적 유닛의 공격 범위입니다.\n쓰이는 곳: 적 유닛에 호버했을 때. 그 적의 이동 칸(enemyMoveRangeColor) 위에 그려집니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색. GridOutline은 이동 칸이 보이면 이동 칸 밖에서도 같은 안쪽 크기입니다.")]
        [SerializeField] private Color enemyAttackRangeColor = new(1f, 0.15662651f, 0f, 1f);
        [Tooltip("공격 범위 응답의 칸입니다.\n쓰이는 곳: ① 공격 모드의 기본 공격 범위 ② 아군 유닛에 호버했을 때 그 유닛의 공격 범위(아군 호버 이동 칸 위). 끌어오기 확장 범위는 이동 범위 색입니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color attackRangeColor = new(1f, 0.15686275f, 0f, 1f);
        [Tooltip("적 유닛의 이동 칸입니다.\n쓰이는 곳: 적 유닛에 호버했을 때. 그 위에 적 공격 범위가 그려집니다. 이동 모드 이동 범위(moveRangeColor)와 겹칠 수 있어 다른 색을 권장합니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color enemyMoveRangeColor = new(0.45f, 0.85f, 1f, 1f);
        [Tooltip("아군 유닛의 이동 칸입니다(호버용).\n쓰이는 곳: 아군 유닛에 호버했을 때(이동 모드에서 고른 유닛은 빠짐). 그 위에 아군 공격 범위가 그려집니다. 이동 모드 이동 범위(moveRangeColor)와 겹칠 수 있어 다른 색을 권장합니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color allyHoverMoveRangeColor = new(0.45f, 0.85f, 1f, 1f);
        [Tooltip("이동 범위 응답의 칸입니다.\n쓰이는 곳: ① 이동 모드인 동안 고른 아군의 이동 범위 ② 이동 Drag 중 ③ 끌어오기 공격의 확장 범위(공격 모드에서 기본 공격 범위 밖, 테두리만).\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color moveRangeColor = new(0.10849059f, 1f, 0.20388347f, 1f);

        [Header("상태 층 색 (행동 중이 아니어도 보임)")]
        [Tooltip("고른 아군 유닛이 서 있는 칸입니다.\n쓰이는 곳: 유닛을 클릭해 고른 동안(행동 중이 아니어도 보임). GridOutline에서는 이동·공격 범위 테두리가 이 칸 쪽 변을 엽니다.\n그리기: GridOutline = 테두리만 / CellPrefab = 칸 색.")]
        [SerializeField] private Color activeActorColor = new(1f, 0.95f, 0.3f, 1f);
        [Tooltip("전투 목표(격파 목표 보스)가 서 있는 칸입니다.\n쓰이는 곳: 격파 목표가 있는 전투에서 늘. 결과 재생 중 넉백·이동을 따라갑니다.\n그리기: GridOutline = 테두리만 / CellPrefab = 칸 색(고른 아군 칸 아래, 정적 구역 위).")]
        [SerializeField] private Color objectiveTargetColor = new(1f, 0.25f, 0.75f, 1f);
        [Tooltip("적 본부의 증원 공급 범위입니다. 기하 범위일 뿐 실제로 생성 가능한 칸을 뜻하지 않습니다.\n쓰이는 곳: 증원 중인 적 본부의 칸에 호버했을 때.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color nexusReinforcementRangeColor = new(0.6f, 0.35f, 0.9f, 1f);

        [Header("기믹·먼지 연막 호버 범위 색")]
        [Tooltip("기믹이 덮는 영역입니다.\n쓰이는 곳: ① 토네이도·하붑·Rider 먼지 연막에 호버했을 때 지금 영역 ② 토네이도를 클릭했을 때 도착 영역. 유닛 범위·이동 경로·공격 조준 아래에 그려집니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color gimmickHoverRangeColor = new(0.15f, 0.85f, 0.75f, 1f);

        [Header("포효 예고 색 (포효 예고 설계 RF4)")]
        [Tooltip("곧 포효할 보스의 포효 범위입니다.\n쓰이는 곳: 포효 카운터가 1 이하로 남은 보스가 있을 때, 전투 국면의 플레이어 턴에(배치 국면·적 턴·결과 재생 중 제외, 행동 중이 아니어도 보임). 그 범위로 밀릴 유닛의 화살표·도착 칸은 넉백 색입니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색. 공격·이동 범위 아래입니다.")]
        [SerializeField] private Color roarForecastColor = new(0.6f, 0.34901962f, 0.90196085f, 1f);

        [Header("낙석 경고 색 (낙석 View 계획 RP-16)")]
        [Tooltip("낙석이 떨어질 칸입니다.\n쓰이는 곳: 낙석 경고가 있는 동안 늘(범위를 보는 동안에도 남음). 칸 위 느낌표는 이미지 색 그대로라, 이 값을 바꾸면 느낌표 이미지(Sprites/rockfall_warning.png)도 함께 맞춥니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color rockfallWarningColor = new(0.918f, 0.278f, 0.271f, 1f);

        [Header("플레이어 스킬 색 (전투 스킬 View 설계 SK2)")]
        [Tooltip("스킬 모드에서 다음 점으로 고를 수 있는 칸입니다(Core가 준 후보, 마탄은 고른 점에 따라 바뀜).\n쓰이는 곳: R·스킬 버튼으로 스킬 모드에 들어가 점을 고르는 동안.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색. 공격 범위와 같은 자리에 그려지고, 테두리는 시전자 칸에서 열리지 않습니다(시전자가 선택 유닛과 다를 수 있어서).")]
        [SerializeField] private Color skillCandidateColor = new(0.35f, 0.75f, 1f, 1f);
        [Tooltip("스킬 모드에서 이미 고른 점입니다.\n쓰이는 곳: 마탄처럼 점을 고르는 스킬에서 점을 찍은 뒤.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색. 영향 칸 위에 보입니다.")]
        [SerializeField] private Color skillPointColor = new(1f, 0.85f, 0.2f, 1f);

        [Header("GridOutline 채우기")]
        [Tooltip("GridOutline 모드의 칸 채우기 불투명도입니다.\n쓰이는 곳: 모든 채우기(배치 구역·호버·채우는 층 전부). 채우기 색은 각 색의 RGB 그대로이고, 알파는 그 색의 알파 × 이 값입니다.\n그리기: CellPrefab 모드는 쓰지 않습니다.")]
        [SerializeField, Range(0f, 1f)] private float fillAlpha = 0.183f;

        [Header("공격 궤적 색 (공격 궤적 설계 TJ8)")]
        [Tooltip("공격 궤적 점선(공격 가능)입니다.\n쓰이는 곳: 공격 모드에서 칸에 호버했을 때 공격자에서 목표까지. 직선은 Core가 고른 대상마다 한 줄(부채꼴 산탄은 대상이 선 자리까지, 대상이 없으면 가장 먼 칸까지), 곡사는 곡선 한 줄, 근접·전체 조준은 선 없음.\n그리기: 칸 강조와 따로 궤적 선에만(Attack Trajectory 재질 색을 덮어씀).")]
        [SerializeField] private Color trajectoryColor = new(1f, 0.95f, 0.75f, 1f);
        [Tooltip("벽에 막힌 공격 궤적 점선입니다.\n쓰이는 곳: 공격 모드 호버에서 Core가 벽 막힘을 알릴 때, 막힌 자리까지. 막힌 자리의 X 표지는 프리팹 자기 색입니다.\n그리기: 칸 강조와 따로 궤적 선에만.")]
        [SerializeField] private Color trajectoryBlockedColor = new(1f, 0.3f, 0.3f, 1f);

        [Header("넉백·끌어오기 미리보기 색 (넉백 미리보기 설계 KB6, 궤적 대상별 선 설계 PL3)")]
        [Tooltip("넉백으로 밀릴 대상이 도착할 칸입니다.\n쓰이는 곳: ① 공격 모드 호버의 넉백 미리보기 ② 포효 예고의 넉백 미리보기. 끌어오기 도착 칸은 이동 경로 색(movementDraftColor)입니다.\n그리기: GridOutline = 테두리·채우기 / CellPrefab = 칸 색.")]
        [SerializeField] private Color knockbackDestinationColor = new(1f, 0.8509804f, 0.2f, 1f);
        [Tooltip("넉백 화살표 — 끝까지 밀리거나 도중에 끊기는 대상입니다(막혀서가 아님).\n쓰이는 곳: 공격 모드 호버·포효 예고의 넉백 미리보기, 대상 앞 가장자리.\n그리기: 칸 강조와 따로 화살표에만(Knockback Preview 재질 색을 덮어씀).")]
        [SerializeField] private Color knockbackColor = new(1f, 0.85f, 0.2f, 1f);
        [Tooltip("넉백 화살표 — 막혀서 덜 가거나 못 가는 대상입니다(부딪힘 피해 없음).\n쓰이는 곳: 공격 모드 호버·포효 예고의 넉백 미리보기.\n그리기: 칸 강조와 따로 화살표에만.")]
        [SerializeField] private Color knockbackBlockedColor = new(1f, 0.3f, 0.3f, 1f);
        [Tooltip("넉백 화살표 — 넉백 면역이라 움직이지 않는 대상입니다.\n쓰이는 곳: 공격 모드 호버·포효 예고의 넉백 미리보기.\n그리기: 칸 강조와 따로 화살표에만.")]
        [SerializeField] private Color knockbackImmuneColor = new(0.65f, 0.65f, 0.65f, 1f);
        [Tooltip("넉백 화살표 — 막혀 멈춘 곳에서 부딪힘 피해를 받는 대상(작렬)입니다. 부딪힘 피해가 없는 막힘은 knockbackBlockedColor입니다.\n쓰이는 곳: 공격 모드 호버·포효 예고의 넉백 미리보기.\n그리기: 칸 강조와 따로 화살표에만.")]
        [SerializeField] private Color knockbackCollisionColor = new(1f, 0.55f, 0.1f, 1f);
        [Tooltip("끌어오기 화살표 — 끌어오기 공격으로 끌려오는 대상입니다(멈춘 이유로 나누지 않음).\n쓰이는 곳: 끌어오기 공격(카우보이 등)의 공격 모드 호버, 대상 앞 가장자리. 공격 점선은 없고 도착 칸은 이동 경로 색입니다.\n그리기: 칸 강조와 따로 화살표에만. 모양은 넉백 화살표와 같은 Knockback Preview 값이고, 기본값은 이동 경로 색(movementDraftColor)과 같습니다.")]
        [SerializeField] private Color pullArrowColor = new(0.05f, 1f, 0.75f, 1f);

        public Color PlayerZoneColor => playerZoneColor;
        public Color RailColor => railColor;
        public Color HoverColor => hoverColor;
        public Color HoverValidColor => hoverValidColor;
        public Color HoverInvalidColor => hoverInvalidColor;
        public Color AttackHoverColor => attackHoverColor;
        public Color AttackHoverValidColor => attackHoverValidColor;
        public Color AttackHoverInvalidColor => attackHoverInvalidColor;
        public Color AffectedColor => affectedColor;
        public Color MovementDraftColor => movementDraftColor;
        public Color HazardRangeColor => hazardRangeColor;
        public Color EnemyAttackRangeColor => enemyAttackRangeColor;
        public Color AttackRangeColor => attackRangeColor;
        public Color EnemyMoveRangeColor => enemyMoveRangeColor;
        public Color AllyHoverMoveRangeColor => allyHoverMoveRangeColor;
        public Color MoveRangeColor => moveRangeColor;
        public Color ActiveActorColor => activeActorColor;
        public Color ObjectiveTargetColor => objectiveTargetColor;
        public Color NexusReinforcementRangeColor => nexusReinforcementRangeColor;
        public Color GimmickHoverRangeColor => gimmickHoverRangeColor;
        public Color RoarForecastColor => roarForecastColor;
        public Color RockfallWarningColor => rockfallWarningColor;
        public Color SkillCandidateColor => skillCandidateColor;
        public Color SkillPointColor => skillPointColor;
        /// <summary>칸 채우기 불투명도입니다. 모든 채우기가 함께 쓰고, 강조 색 알파에 곱합니다.</summary>
        public float FillAlpha => fillAlpha;
        public Color TrajectoryColor => trajectoryColor;
        public Color TrajectoryBlockedColor => trajectoryBlockedColor;
        public Color KnockbackDestinationColor => knockbackDestinationColor;
        public Color KnockbackColor => knockbackColor;
        public Color KnockbackBlockedColor => knockbackBlockedColor;
        public Color KnockbackImmuneColor => knockbackImmuneColor;
        public Color KnockbackCollisionColor => knockbackCollisionColor;
        public Color PullArrowColor => pullArrowColor;

        /// <summary>색 성분이 유한하고 알파가 [0, 1]인지, 채우기 불투명도가 유한한 [0, 1]인지 검사합니다. 부트 DB 생성에서 실패합니다.</summary>
        internal void Validate()
        {
            RequireColor(playerZoneColor, nameof(playerZoneColor));
            RequireColor(railColor, nameof(railColor));
            RequireColor(hoverColor, nameof(hoverColor));
            RequireColor(hoverValidColor, nameof(hoverValidColor));
            RequireColor(hoverInvalidColor, nameof(hoverInvalidColor));
            RequireColor(attackHoverColor, nameof(attackHoverColor));
            RequireColor(attackHoverValidColor, nameof(attackHoverValidColor));
            RequireColor(attackHoverInvalidColor, nameof(attackHoverInvalidColor));
            RequireColor(affectedColor, nameof(affectedColor));
            RequireColor(movementDraftColor, nameof(movementDraftColor));
            RequireColor(hazardRangeColor, nameof(hazardRangeColor));
            RequireColor(enemyAttackRangeColor, nameof(enemyAttackRangeColor));
            RequireColor(attackRangeColor, nameof(attackRangeColor));
            RequireColor(enemyMoveRangeColor, nameof(enemyMoveRangeColor));
            RequireColor(allyHoverMoveRangeColor, nameof(allyHoverMoveRangeColor));
            RequireColor(moveRangeColor, nameof(moveRangeColor));
            RequireColor(activeActorColor, nameof(activeActorColor));
            RequireColor(objectiveTargetColor, nameof(objectiveTargetColor));
            RequireColor(nexusReinforcementRangeColor, nameof(nexusReinforcementRangeColor));
            RequireColor(gimmickHoverRangeColor, nameof(gimmickHoverRangeColor));
            RequireColor(roarForecastColor, nameof(roarForecastColor));
            RequireColor(rockfallWarningColor, nameof(rockfallWarningColor));
            RequireColor(skillCandidateColor, nameof(skillCandidateColor));
            RequireColor(skillPointColor, nameof(skillPointColor));
            RequireColor(trajectoryColor, nameof(trajectoryColor));
            RequireColor(trajectoryBlockedColor, nameof(trajectoryBlockedColor));
            RequireColor(knockbackDestinationColor, nameof(knockbackDestinationColor));
            RequireColor(knockbackColor, nameof(knockbackColor));
            RequireColor(knockbackBlockedColor, nameof(knockbackBlockedColor));
            RequireColor(knockbackImmuneColor, nameof(knockbackImmuneColor));
            RequireColor(knockbackCollisionColor, nameof(knockbackCollisionColor));
            RequireColor(pullArrowColor, nameof(pullArrowColor));

            if (!float.IsFinite(fillAlpha) || fillAlpha < 0f || fillAlpha > 1f)
                throw new InvalidOperationException($"[FieldColorsData] 채우기 불투명도(fillAlpha)는 유한한 [0, 1]이어야 합니다: {fillAlpha}");
        }

        private static void RequireColor(Color color, string name)
        {
            if (!float.IsFinite(color.r) || !float.IsFinite(color.g) || !float.IsFinite(color.b) ||
                !float.IsFinite(color.a) || color.a < 0f || color.a > 1f)
                throw new InvalidOperationException($"[FieldColorsData] {name} 색은 성분이 유한하고 알파가 [0, 1]이어야 합니다: {color}");
        }
    }
}
