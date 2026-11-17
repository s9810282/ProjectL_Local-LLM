using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 칸 하나에 겹칠 수 있는 강조 층입니다. 비트로 함께 표시합니다(17개째부터 uint).
    /// 값을 더하면 층 표(<see cref="FieldHighlightLayers"/>)에도 한 줄을 더합니다. 빠뜨리면 표를 만들 때 예외입니다.
    /// </summary>
    [Flags]
    internal enum FieldHighlightLayer : uint
    {
        None = 0,
        /// <summary>Core의 현재 행위자가 점유한 칸입니다.</summary>
        ActiveActor = 1 << 0,
        /// <summary>이동 범위 응답의 칸입니다.</summary>
        MoveRange = 1 << 1,
        /// <summary>호버한 공격 조준의 영향 칸입니다.</summary>
        Affected = 1 << 2,
        /// <summary>드래그로 선택한 이동 예정 경로입니다. Core 승인 여부와는 별개입니다.</summary>
        MovementDraft = 1 << 3,
        /// <summary>현재 조준의 전달 또는 파급을 막는 벽이다.</summary>
        BlockedWall = 1 << 4,
        /// <summary>공격 범위 응답의 칸입니다. 이동 범위와 색을 따로 정할 수 있도록 층을 나눴습니다.</summary>
        AttackRange = 1 << 5,
        /// <summary>전투 목표(지정 보스)가 점유한 칸입니다. 대상은 Buffer의 전투 목표가 정합니다(보스 활성화 설계 D3).</summary>
        ObjectiveTarget = 1 << 6,
        /// <summary>
        /// 결과 재생이 잠깐 보이는 칸입니다(포효 영향 칸·소환 칸, 보스 활성화 설계 D6·D7). 재생 쪽만 쓰며,
        /// 입력 조준의 <see cref="Affected"/>와 칸을 나눠 서로 지우지 않습니다. 순서·색·채움은 층 표가 원본입니다.
        /// </summary>
        PresentationHighlight = 1 << 7,
        /// <summary>
        /// 호버한 적 유닛의 공격 범위 칸입니다(호버 공격 범위 설계 Q-UR3 B). 공격 모드 범위는 <see cref="AttackRange"/>, 아군 호버는 <see cref="AllyHoverRange"/>이고,
        /// 적과 색을 나누려고 층을 따로 둡니다. 아군 호버 범위와 함께 보이는 일은 없습니다. 순서·색·채움은 층 표(<see cref="FieldHighlightLayers"/>)가 원본입니다.
        /// </summary>
        EnemyAttackRange = 1 << 8,
        /// <summary>
        /// 호버한 지뢰·화약통의 피해 칸입니다(호버 공격 범위 설계 §8 HZ5·Q-H2 A). 진영을 가리지 않는 위험이라 아군·적 범위와 색을 나눕니다.
        /// 유닛 호버 범위 둘과 함께 보이는 일은 없습니다. 순서·색·채움은 층 표가 원본입니다.
        /// </summary>
        HazardRange = 1 << 9,
        /// <summary>
        /// 호버한 아군 유닛의 공격 범위 칸입니다(View 2라운드 W5-6). 공격 모드의 <see cref="AttackRange"/>와 층을 나눠,
        /// 호버를 지워도 공격 모드 범위가 지워지지 않습니다. 둘이 함께 보이지 않는 것은 입력 기능 묶음이 정합니다
        /// (공격 모드에 들어갈 때 호버를 내려놓고, 다른 입력 소유자가 활성인 동안 호버를 보이지 않음). 순서·색·채움은 층 표가 원본입니다.
        /// </summary>
        AllyHoverRange = 1 << 10,
        /// <summary>Core가 제공한 전체 선택 범위 중 기본 공격 범위 바깥 칸입니다(끌어오기 확장 범위).</summary>
        PullRange = 1 << 11,
        /// <summary>끌어오기 미리보기의 실제 도착 칸입니다.</summary>
        PullDestination = 1 << 12,
        /// <summary>
        /// 공격 미리보기의 넉백 도착 칸입니다(넉백 미리보기 설계 KB1). 끌어오기 도착 칸과 소유자·지우는 시점이 달라 층을 나눕니다.
        /// </summary>
        KnockbackDestination = 1 << 13,
        /// <summary>
        /// 호버한 아군 유닛의 이동 칸입니다(호버 이동 범위 설계 EM2·P4). 이동 모드의 <see cref="MoveRange"/>와 소유자·색이 달라 층을 나눕니다.
        /// 공격 범위 층들보다 아래라 겹친 칸에서는 공격 범위가 안쪽에 그려집니다. 이 층이 보이면 그 유닛의 공격 범위(<see cref="AllyHoverRange"/>)는
        /// 이동 칸 밖에서도 같은 안쪽 크기입니다(층 표의 받침 층). 순서·색·채움은 층 표가 원본입니다.
        /// </summary>
        AllyHoverMoveRange = 1 << 14,
        /// <summary>
        /// 호버한 적 유닛의 이동 칸입니다(호버 이동 범위 설계 EM2). 아군 호버 이동 칸과 색을 나눕니다. 순서·색·채움은 층 표가 원본입니다.
        /// </summary>
        EnemyMoveRange = 1 << 15,
        /// <summary>
        /// 낙석이 떨어질 칸입니다(낙석 View 계획 RP-16). 전투 상태를 보이는 층이라 입력이 없는 평소 화면에도 테두리·채우기가 나오고 행동 중으로 치지 않습니다.
        /// </summary>
        RockfallWarning = 1 << 16,
        /// <summary>호버한 토네이도·하붑·Rider 먼지 연막의 현재 영역입니다. 유닛 범위·공격 조준과 별도로 지우며 행동 중으로 치지 않습니다.</summary>
        GimmickHoverRange = 1 << 17,
        /// <summary>적 본부의 증원 가능 기하 범위입니다. 현재 생성 가능한 칸의 판정이 아닙니다.</summary>
        NexusReinforcementRange = 1 << 18,
        /// <summary>
        /// 포효가 임박한 보스의 포효 범위 칸입니다(포효 예고 설계 RF4). 전투 상태를 보이는 층이라 평소 화면에도 나오고 행동 중으로 치지 않습니다.
        /// 공격·이동 범위 층들보다 아래라 겹친 칸에서는 그 범위가 안쪽에 그려집니다. 순서·색·채움은 층 표가 원본입니다.
        /// </summary>
        RoarForecast = 1 << 19,
        /// <summary>
        /// 스킬 모드에서 다음 점으로 고를 수 있는 칸입니다(Core 선택 답의 후보, 전투 스킬 View 설계 SK2). 행동 중 층입니다.
        /// 원점 열기는 하지 않습니다(시전자 = 메인이 선택 유닛과 다를 수 있어 선택 유닛 칸 기준으로 열면 엉뚱한 변이 열림, 계획 TP-1 정정).
        /// </summary>
        SkillCandidate = 1 << 20,
        /// <summary>스킬 모드에서 이미 고른 점입니다(전투 스킬 View 설계 SK2). 영향 칸 위에 보입니다.</summary>
        SkillPoint = 1 << 21
    }

    /// <summary>칸에 실제로 칠할 색의 종류입니다.</summary>
    internal enum FieldHighlightColor
    {
        /// <summary>강조 없음(셀 기본 색).</summary>
        None = 0,
        Static = 1,
        ActiveActor = 2,
        MoveRange = 3,
        Affected = 4,
        HoverNeutral = 5,
        HoverValid = 6,
        HoverInvalid = 7,
        MovementDraft = 8,
        AttackRange = 9,
        ObjectiveTarget = 10,
        EnemyAttackRange = 11,
        AttackHoverNeutral = 12,
        AttackHoverValid = 13,
        AttackHoverInvalid = 14,
        HazardRange = 15,
        /// <summary>넉백 도착 칸입니다. 이동 초안·끌어오기 도착 칸과 색을 나눠 따로 정합니다.</summary>
        KnockbackDestination = 16,
        /// <summary>호버한 아군 유닛의 이동 칸입니다. 이동 모드 이동 범위와 겹쳐도 구분되게 색을 따로 정합니다.</summary>
        AllyHoverMoveRange = 17,
        /// <summary>호버한 적 유닛의 이동 칸입니다.</summary>
        EnemyMoveRange = 18,
        /// <summary>낙석이 떨어질 칸입니다. 느낌표 이미지 색에 맞춘 View DB 값입니다(필드 색 묶음, 필드 색 DB 설계 FC1).</summary>
        RockfallWarning = 19,
        /// <summary>호버한 토네이도·하붑·Rider 먼지 연막의 현재 영역 색입니다.</summary>
        GimmickHoverRange = 20,
        NexusReinforcementRange = 21,
        /// <summary>포효가 임박한 보스의 포효 범위 색입니다.</summary>
        RoarForecast = 22,
        /// <summary>스킬 모드의 다음 점 후보 칸 색입니다.</summary>
        SkillCandidate = 23,
        /// <summary>스킬 모드에서 고른 점 색입니다.</summary>
        SkillPoint = 24
    }

    /// <summary>
    /// 칸 강조 층 표입니다(View 3라운드 W10). 층 하나의 속성(그리는 순서·색·채움·평소에도 그림·원점 열기·받침 층)을 표 한 줄에 둡니다.
    /// 우선순위는 호버 > 표의 뒤쪽 층 > … > 표의 앞쪽 층 > 정적(구역·레일)이고, 높은 층이 지워지면 다음 층 색으로 돌아갑니다(설계 §21.1).
    /// 층을 더하면 열거와 이 표 한 줄을 고칩니다(새 색이면 팔레트까지). 상태를 갖지 않는 순수 계산입니다.
    /// </summary>
    internal static class FieldHighlightLayers
    {
        // 그리는 순서(낮은 것부터)대로 층마다 한 줄입니다. 호버·정적은 층이 아니라 표에 없습니다.
        // "평소에도 그림"은 행동 중이 아니어도 GridOutline이 그리는 층(전투 상태·결과 재생을 보이는 층)이고, 나머지는 행동 중 층입니다.
        // 이 칸 하나가 "평소에도 그림"과 "칸이 있으면 행동 중이 아님"을 함께 정합니다. 둘이 갈리는 층이 생기면 칸을 나눕니다.
        // "받침 층"이 있는 채우는 층은 받침 층이 이번에 한 칸이라도 칠해져 있으면, 받침 층이 없는 칸에서도 받침 층 위 깊이에 놓입니다
        // (GridOutline 겹친 강조, 2단계 설계 6판 RC18). 호버한 유닛의 공격 범위가 이동 칸 위·밖 어디서나 같은 안쪽 크기가 됩니다.
        // "원점 열기"는 GridOutline 테두리·채우기가 범위 원점 쪽 변을 여는지와 원점이 어디인지입니다(평가 C1-21). Origin은 선택 유닛 칸
        // (ActiveActor 층), HoverOrigin은 호버한 유닛 칸(유닛 호버 범위)이고, None은 닫습니다. 열린 변 계산은 FieldAttackOutlineOpening이 합니다.
        private static readonly LayerRow[] Rows =
        {
            //           층                                        색                                        채움   평소에도 그림  원점 열기                  받침 층
            new LayerRow(FieldHighlightLayer.NexusReinforcementRange, FieldHighlightColor.NexusReinforcementRange, true,  true, SourceOpening.None),
            new LayerRow(FieldHighlightLayer.ObjectiveTarget,       FieldHighlightColor.ObjectiveTarget,      false, true,  SourceOpening.None),
            new LayerRow(FieldHighlightLayer.GimmickHoverRange,     FieldHighlightColor.GimmickHoverRange,    true,  true,  SourceOpening.None),
            // 포효 예고는 공격 조준·이동 범위 아래라 겹친 칸에서 조준이 안쪽에 그려집니다(포효 예고 설계 RF4, 계획 TP-16).
            new LayerRow(FieldHighlightLayer.RoarForecast,          FieldHighlightColor.RoarForecast,         true,  true,  SourceOpening.None),
            new LayerRow(FieldHighlightLayer.ActiveActor,          FieldHighlightColor.ActiveActor,          false, true,  SourceOpening.Origin),
            new LayerRow(FieldHighlightLayer.MoveRange,             FieldHighlightColor.MoveRange,            true,  false, SourceOpening.Origin),
            // 끌어오기 확장 범위는 이동 범위 색입니다(기본 범위와 칸 집합만 나눔, 거리·공격 가능 여부는 Core 응답).
            // 채움은 천명님 Phase D 판(GridOutline 채우기 목록에 없음)과 같게 두었습니다(병합, 채울지는 후속 결정).
            new LayerRow(FieldHighlightLayer.PullRange,             FieldHighlightColor.MoveRange,            false, false, SourceOpening.Origin),
            // 호버 이동 칸은 모든 공격 범위 층 아래입니다. 겹친 칸에서 공격 범위가 안쪽에 그려집니다(호버 이동 범위 설계 EM2).
            new LayerRow(FieldHighlightLayer.AllyHoverMoveRange,    FieldHighlightColor.AllyHoverMoveRange,   true,  false, SourceOpening.HoverOrigin),
            new LayerRow(FieldHighlightLayer.EnemyMoveRange,        FieldHighlightColor.EnemyMoveRange,       true,  false, SourceOpening.HoverOrigin),
            new LayerRow(FieldHighlightLayer.AttackRange,          FieldHighlightColor.AttackRange,          true,  false, SourceOpening.Origin),
            // 스킬 후보 칸은 공격 범위와 같은 자리입니다. 둘은 전경이 배타라 함께 보이지 않습니다(전투 스킬 View 계획 TP-1).
            new LayerRow(FieldHighlightLayer.SkillCandidate,        FieldHighlightColor.SkillCandidate,       true,  false, SourceOpening.None),
            // 아군 호버 범위는 공격 범위와 같은 색입니다(층만 나눔, View 2라운드 W5-6).
            new LayerRow(FieldHighlightLayer.AllyHoverRange,        FieldHighlightColor.AttackRange,          true,  false, SourceOpening.HoverOrigin, FieldHighlightLayer.AllyHoverMoveRange),
            new LayerRow(FieldHighlightLayer.EnemyAttackRange,      FieldHighlightColor.EnemyAttackRange,     true,  false, SourceOpening.HoverOrigin, FieldHighlightLayer.EnemyMoveRange),
            new LayerRow(FieldHighlightLayer.HazardRange,           FieldHighlightColor.HazardRange,          true,  false, SourceOpening.None),
            // 낙석 경고는 범위를 보는 동안에도 남고, 겹친 칸에서는 범위 위 안쪽에 그려집니다(낙석 View 계획 RP-16·RP-17).
            new LayerRow(FieldHighlightLayer.RockfallWarning,       FieldHighlightColor.RockfallWarning,      true,  true,  SourceOpening.None),
            new LayerRow(FieldHighlightLayer.MovementDraft,         FieldHighlightColor.MovementDraft,        false, false, SourceOpening.None),
            // 재생 강조는 영향 칸과 같은 색입니다.
            new LayerRow(FieldHighlightLayer.PresentationHighlight, FieldHighlightColor.Affected,             false, true,  SourceOpening.None),
            new LayerRow(FieldHighlightLayer.Affected,              FieldHighlightColor.Affected,             true,  false, SourceOpening.Origin),
            // 고른 점은 영향 칸 위에 보입니다(마지막 점 호버 예보에서 영향 칸과 겹침, 전투 스킬 View 계획 TP-1).
            new LayerRow(FieldHighlightLayer.SkillPoint,            FieldHighlightColor.SkillPoint,           true,  false, SourceOpening.None),
            // 끌어오기 도착 칸은 이동 초안 색입니다(테두리만).
            new LayerRow(FieldHighlightLayer.PullDestination,       FieldHighlightColor.MovementDraft,        false, false, SourceOpening.None),
            // 넉백 도착 칸은 전용 색으로 칸을 채웁니다(2026-10-27 사용자 요청, 넉백 미리보기 계획 §7.5).
            new LayerRow(FieldHighlightLayer.KnockbackDestination,  FieldHighlightColor.KnockbackDestination, true,  false, SourceOpening.None),
            // 차단 벽은 공격 조준에서만 생기므로 공격 호버 불가 색입니다.
            new LayerRow(FieldHighlightLayer.BlockedWall,           FieldHighlightColor.AttackHoverInvalid,   false, false, SourceOpening.None)
        };

        static FieldHighlightLayers()
        {
            var order = new FieldHighlightLayer[Rows.Length];
            FieldHighlightLayer seen = FieldHighlightLayer.None;
            FieldHighlightLayer idle = FieldHighlightLayer.None;
            FieldHighlightLayer idleFill = FieldHighlightLayer.None;
            FieldHighlightLayer opensAtSource = FieldHighlightLayer.None;
            FieldHighlightLayer hoverRange = FieldHighlightLayer.None;
            for (int i = 0; i < Rows.Length; i++)
            {
                FieldHighlightLayer layer = Rows[i].Layer;
                if (!IsSingle(layer))
                    throw new InvalidOperationException($"[FieldHighlightLayers] 층 표의 {i}번째 줄이 단일 강조 층이 아닙니다: {layer}");
                if ((seen & layer) != 0)
                    throw new InvalidOperationException($"[FieldHighlightLayers] 층 표에 같은 층이 두 번 있습니다: {layer}");
                // 받침 층은 표에서 앞(아래)에 있는 단일 층이어야 깊이를 셀 때 그 위에 놓입니다.
                FieldHighlightLayer stacksOn = Rows[i].StacksOn;
                if (stacksOn != FieldHighlightLayer.None && (!IsSingle(stacksOn) || (seen & stacksOn) == 0))
                    throw new InvalidOperationException($"[FieldHighlightLayers] {layer}의 받침 층 {stacksOn}이 표에서 그 앞에 있는 단일 층이 아닙니다.");
                seen |= layer;
                order[i] = layer;
                if (Rows[i].ShownWhenIdle)
                    idle |= layer;
                if (Rows[i].ShownWhenIdle && Rows[i].Fills)
                    idleFill |= layer;
                if (Rows[i].Opening != SourceOpening.None)
                    opensAtSource |= layer;
                if (Rows[i].Opening == SourceOpening.HoverOrigin)
                    hoverRange |= layer;
            }

            // 열거의 단일 비트 값이 모두 표에 있어야 합니다(빠뜨리면 그 층이 조용히 그려지지 않음, 설계 P20).
            foreach (FieldHighlightLayer layer in (FieldHighlightLayer[])System.Enum.GetValues(typeof(FieldHighlightLayer)))
            {
                if (layer != FieldHighlightLayer.None && (seen & layer) == 0)
                    throw new InvalidOperationException($"[FieldHighlightLayers] 층 표에 없는 강조 층이 있습니다: {layer}");
            }

            // 받침 층은 채우기 깊이 규칙이라 두 층 모두 채우는 층이어야 합니다.
            for (int i = 0; i < Rows.Length; i++)
            {
                FieldHighlightLayer stacksOn = Rows[i].StacksOn;
                if (stacksOn != FieldHighlightLayer.None && (!Rows[i].Fills || !FillsCell(stacksOn)))
                    throw new InvalidOperationException($"[FieldHighlightLayers] {Rows[i].Layer}와 받침 층 {stacksOn}은 모두 채우는 층이어야 합니다.");
            }

            DrawOrder = order;
            IdleLayers = idle;
            IdleFillLayers = idleFill;
            ActingLayers = seen & ~idle;
            OpensAtSourceLayers = opensAtSource;
            HoverRangeLayers = hoverRange;
        }

        /// <summary>
        /// 호버·정적을 뺀 층의 우선순위입니다(낮은 것부터). 셀 색 선택과 아웃라인을 쌓는 높이가 이 순서 하나를 따릅니다. 층 표에서 만듭니다.
        /// </summary>
        internal static IReadOnlyList<FieldHighlightLayer> DrawOrder { get; }

        /// <summary>행동 중이 아니어도 GridOutline이 그리는 층의 합입니다. 층 표의 "평소에도 그림"에서 만듭니다.</summary>
        internal static FieldHighlightLayer IdleLayers { get; }

        /// <summary>행동 중이 아니어도 칸을 채우는 층의 합입니다. 층 표의 "채움"과 "평소에도 그림"이 모두 참인 층입니다(낙석 View 계획 RP-17).</summary>
        internal static FieldHighlightLayer IdleFillLayers { get; }

        /// <summary>칸이 있으면 "행동 중"이 되는 층의 합입니다(<see cref="IdleLayers"/>가 아닌 모든 층). 층 표에서 만듭니다.</summary>
        internal static FieldHighlightLayer ActingLayers { get; }

        /// <summary>GridOutline 테두리·채우기가 범위 원점 쪽 변을 여는 층의 합입니다(원점이 선택 유닛·호버 유닛). 층 표의 "원점 열기"에서 만듭니다.</summary>
        internal static FieldHighlightLayer OpensAtSourceLayers { get; }

        /// <summary>원점이 호버한 유닛 칸인 층(유닛 호버 범위)의 합입니다. 층 표의 "원점 열기"에서 만듭니다.</summary>
        internal static FieldHighlightLayer HoverRangeLayers { get; }

        internal static FieldHighlightColor Resolve(
            bool isHovered,
            CombatHoverState hoverState,
            FieldHighlightLayer layers,
            bool hasStaticColor,
            CombatHoverKind hoverKind = CombatHoverKind.Default)
        {
            if (isHovered)
                return HoverColor(hoverState, hoverKind);

            for (int i = Rows.Length - 1; i >= 0; i--)
            {
                if ((layers & Rows[i].Layer) != 0)
                    return Rows[i].Color;
            }

            return hasStaticColor ? FieldHighlightColor.Static : FieldHighlightColor.None;
        }

        /// <summary>호버 칸의 색 종류입니다. 공격 조준 호버는 다른 호버와 색을 나눕니다(가능 여부 셋 모두).</summary>
        internal static FieldHighlightColor HoverColor(CombatHoverState hoverState, CombatHoverKind hoverKind)
        {
            bool attack = hoverKind == CombatHoverKind.Attack;
            switch (hoverState)
            {
                case CombatHoverState.Valid:
                    return attack ? FieldHighlightColor.AttackHoverValid : FieldHighlightColor.HoverValid;
                case CombatHoverState.Invalid:
                    return attack ? FieldHighlightColor.AttackHoverInvalid : FieldHighlightColor.HoverInvalid;
                default:
                    return attack ? FieldHighlightColor.AttackHoverNeutral : FieldHighlightColor.HoverNeutral;
            }
        }

        /// <summary>층 하나의 색 종류입니다(층 표의 색). 단일 강조 층이 아니면 예외입니다.</summary>
        internal static FieldHighlightColor ColorOf(FieldHighlightLayer layer) => RowOf(layer).Color;

        /// <summary>
        /// GridOutline 표시가 칸을 반투명하게 채우는 층인지입니다(층 표의 채움). 단일 강조 층이 아니면(None·여러 층) 거짓입니다.
        /// 채우기 우선순위는 <see cref="DrawOrder"/>를 그대로 따르고, 호버·배치 구역은 아웃라인 쪽 특례입니다.
        /// </summary>
        internal static bool FillsCell(FieldHighlightLayer layer) => TryRowOf(layer, out LayerRow row) && row.Fills;

        /// <summary>
        /// 채우는 층의 색 종류입니다. 범위는 층 색(<see cref="ColorOf"/>)입니다. 공격 영향(공격 호버 대상)은 공격 조준에서만 생기므로
        /// 공격 호버 상태 색(가능·불가·판단 전)을 씁니다.
        /// </summary>
        internal static FieldHighlightColor FillColorOf(FieldHighlightLayer layer, CombatHoverState hoverState) =>
            layer == FieldHighlightLayer.Affected ? HoverColor(hoverState, CombatHoverKind.Attack) : ColorOf(layer);

        /// <summary>
        /// 채우는 층의 받침 층입니다(층 표). 받침 층이 이번에 한 칸이라도 칠해져 있으면 이 층 칸은 받침 층이 없는 칸에서도 받침 층 위 깊이에 놓입니다
        /// (2단계 설계 6판 RC18). 없거나 단일 강조 층이 아니면 None입니다.
        /// </summary>
        internal static FieldHighlightLayer StacksOn(FieldHighlightLayer layer) =>
            TryRowOf(layer, out LayerRow row) ? row.StacksOn : FieldHighlightLayer.None;

        /// <summary>범위 원점 쪽 변을 여는 층인지입니다(층 표의 원점 열기). 단일 강조 층이 아니면(None·여러 층) 거짓입니다.</summary>
        internal static bool OpensAtSource(FieldHighlightLayer layer) => IsSingle(layer) && (OpensAtSourceLayers & layer) != 0;

        /// <summary>원점이 호버한 유닛 칸인 층(유닛 호버 범위)인지입니다(층 표의 원점 열기). 단일 강조 층이 아니면 거짓입니다.</summary>
        internal static bool IsHoverRange(FieldHighlightLayer layer) => IsSingle(layer) && (HoverRangeLayers & layer) != 0;

        private static LayerRow RowOf(FieldHighlightLayer layer) =>
            TryRowOf(layer, out LayerRow row) ? row : throw new ArgumentOutOfRangeException(nameof(layer), layer, "단일 강조 층이 아닙니다.");

        private static bool TryRowOf(FieldHighlightLayer layer, out LayerRow row)
        {
            for (int i = 0; i < Rows.Length; i++)
            {
                if (Rows[i].Layer == layer)
                {
                    row = Rows[i];
                    return true;
                }
            }
            row = default;
            return false;
        }

        private static bool IsSingle(FieldHighlightLayer layer) => layer != FieldHighlightLayer.None && (layer & (layer - 1)) == 0;

        /// <summary>층 표의 "원점 열기" 칸입니다. 범위 원점 쪽 변을 여는지와 원점이 어디인지입니다(평가 C1-21).</summary>
        private enum SourceOpening
        {
            /// <summary>원점 쪽을 열지 않고 닫힌 테두리로 그립니다.</summary>
            None = 0,
            /// <summary>원점은 선택 유닛 칸(<see cref="FieldHighlightLayer.ActiveActor"/> 층)입니다.</summary>
            Origin = 1,
            /// <summary>원점은 호버한 유닛 칸(호버 범위 원점 칸)입니다. 유닛 호버 범위 층입니다.</summary>
            HoverOrigin = 2
        }

        /// <summary>층 표의 한 줄입니다. 만든 뒤 바꾸지 않습니다.</summary>
        private readonly struct LayerRow
        {
            // 원점 열기는 기본값 없이 줄마다 적습니다. 새 층을 더할 때 빠뜨리면 컴파일에서 걸립니다(평가 C1-21, 결정 Q-N1 A).
            internal LayerRow(FieldHighlightLayer layer, FieldHighlightColor color, bool fills, bool shownWhenIdle,
                SourceOpening opening, FieldHighlightLayer stacksOn = FieldHighlightLayer.None)
            {
                Layer = layer;
                Color = color;
                Fills = fills;
                ShownWhenIdle = shownWhenIdle;
                Opening = opening;
                StacksOn = stacksOn;
            }

            internal FieldHighlightLayer Layer { get; }
            internal FieldHighlightColor Color { get; }
            internal bool Fills { get; }
            internal bool ShownWhenIdle { get; }
            internal SourceOpening Opening { get; }
            // 받침 층입니다. 없으면 None입니다.
            internal FieldHighlightLayer StacksOn { get; }
        }
    }
}
