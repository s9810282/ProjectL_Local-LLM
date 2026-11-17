using System;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>
    /// Combat 씬 연결 한 번의 입력 객체(연결 입력 묶음: 대기 관문·입력 기능 묶음·Router·덱/아이템 입구·환경 호버·거절 안내·배치 진입·턴 종료·행동 가용성·카메라 조작·이동 되돌리기)와
    /// 입력 기능(배치 Drag·공격 조준·이동 경로)·커서 스냅을 생성하고 의존성을 연결합니다(구조 개선 SV5, View 2라운드 W6-1).
    /// </summary>
    /// <remarks>
    /// 조정자(CombatViewSystem)는 이 Factory가 돌려주는 연결 입력 묶음과 서비스 수명 사실 읽기를 받고,
    /// 개별 입력 객체의 생성 인자를 알지 않습니다(View 2라운드 W6-1). 묶음 안의 대기 관문(<see cref="CombatCommandGate"/>)·Router·
    /// 덱/아이템 입구·환경 호버·거절 안내도 여기서 만듭니다. 사실 공급자·수신 조건·막힌 입력 알림·재고 읽기는 조정자가 고른 원본이라
    /// 재료 묶음(<see cref="CombatSceneInputSources"/>)으로 받습니다(View 3라운드 W12-1).
    /// 기능 내부 부품(경로 입력 안의 이동 상호작용·범위 요청, 배치·공격 안의 호버 추적)은 각 기능이 소유하므로 여기서 만들지 않습니다.
    /// </remarks>
    internal static class CombatSceneInputFactory
    {
        /// <summary>명시적인 읽기 의존성만 연결해 입력 사실 읽기를 만듭니다.</summary>
        internal static CombatInputFactsReader CreateFactsReader(ICombatStateBufferReadSink state, ICombatViewBufferReadSink inputRead,
            CombatInputConnection inputConnection, CombatResultPlayback playback, CombatViewProgress progress,
            CombatBlockedInputReport blockedInput, Func<CombatViewConnection> readConnection,
            Func<bool> isInputReady, Func<bool> hasPendingCommand, Func<bool> isModalOpen) =>
            new CombatInputFactsReader(state, inputRead, inputConnection, playback, progress, blockedInput,
                readConnection, isInputReady, hasPendingCommand, isModalOpen);

        /// <summary>
        /// 환경 설명·기믹 호버 범위에 필요한 표·집기·표시 서비스만 연결합니다. 연결 입력 묶음(<see cref="CreateSceneInput"/>)이 부릅니다.
        /// 기믹·본부 범위 계약은 표시의 입력 피드백 묶음이 내놓으므로 형식 검사로 꺼내지 않습니다(씬 표시 나누기 설계 DS2, 평가 C2-15 일부).
        /// </summary>
        private static CombatEnvironmentHover CreateEnvironmentHover(CombatInputFeedback feedback,
            CombatDisplayTables tables, IUITextPresentation text)
        {
            if (feedback == null) throw new ArgumentNullException(nameof(feedback));
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            return new CombatEnvironmentHover(feedback.Picker, feedback.Occlusion,
                new CombatEnvironmentText(tables.Objects, tables.Terrains, tables.Units, tables.Rockfall, tables.Haboob, tables.Traits,
                    CombatEnvironmentDescriptionFactory.Create(tables)), text, feedback.GimmickRange, new CombatNexusRangeHover(feedback.NexusRange));
        }

        /// <summary>
        /// 씬 연결 한 번의 입력 객체를 만들어 연결 입력 묶음으로 돌려줍니다(View 2라운드 W6-1). 첫 그리기 성공 뒤에 부릅니다.
        /// 생성 순서: 대기 관문 → 선택·HUD 행동 가용성 조회 → 포효 예고 조회 → 입력 기능 묶음·거절 안내 → 이동 되돌리기 → Router → 덱 배치 입구 → 아이템 입구 → 환경 호버
        /// → 배치 진입·턴 종료(Router·거절 안내를 씀, 3라운드 W11-2) → 카메라 조작.
        /// HUD 아이템 아이콘 해제 연결은 연결 상태가 돌려받은 뒤 마지막에 겁니다(생성이 실패하면 풀 수 없는 연결을 남기지 않음, 계획 검증 L11).
        /// </summary>
        /// <param name="display">이 연결의 표시입니다(연결 수명).</param>
        /// <param name="sources">확인창을 포함한 필수 서비스 수명 재료입니다.</param>
        internal static CombatSceneInput CreateSceneInput(ICombatBattleDisplay display, CombatSceneInputSources sources)
        {
            if (display == null)
                throw new ArgumentNullException(nameof(display));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            // 입력 기능이 쓰는 표시 의도 계약은 표시의 묶음에서 이름으로 꺼낸다(씬 표시 나누기 설계 DS2).
            CombatInputFeedback feedback = display.Feedback;
            // 대기 관문은 조정자가 Resolve·HasPending으로 쓰는 상태 원본이다. 묶음이 갖고 조정자는 묶음에서 읽는다.
            var gate = new CombatCommandGate(sources.InputWrite);
            var actionAvailability = new CombatActionAvailabilityTracker(sources.InputRead, sources.InputWrite);
            var hudActionAvailability = new CombatHudActionAvailability(sources.InputRead, sources.InputWrite, display.SetUnitActionAvailability);
            // 포효 예고 조회는 HUD 조회와 같은 배경 조회 꼴이고, 바닥 범위는 표시 묶음의 계약으로 그립니다(포효 예고 계획 TP-11·TP-16).
            var roarForecast = new CombatRoarForecastTracker(sources.InputRead, sources.InputWrite, feedback.RoarForecast);
            // 스킬 버튼의 배경 가용성 조회는 행동 가능 여부 조회와 같은 꼴이고, 아이콘·이름은 스킬 외형 표에서 읽습니다(전투 스킬 View 설계 SK4).
            var skillAvailability = new CombatPlayerSkillAvailabilityTracker(sources.InputRead, sources.InputWrite, sources.State, sources.Tables.PlayerSkills);
            (CombatInteraction interaction, CombatActionNotice notices) = CreateSceneInteraction(
                feedback, sources.State, sources.Characters, sources.InputRead, sources.InputWrite, gate, sources.ReadPathFacts,
                new CombatInputSounds(sources.Audio, sources.Tables.SoundPresets), sources.ItemStock, sources.Text, actionAvailability,
                sources.ReportBlockedInput, sources.Tables.PlayerSkills);
            // Router는 조정자의 사실 공급자를 받는다. UI 위의 Select를 공격으로 쓰지 않는다(행동 버튼 설계 B5).
            var movementUndo = new CombatMovementUndoInput(actionAvailability, gate, sources.ReadInputFacts, () => interaction.IsInputOwnerActive);
            var router = new CombatInputRouter(interaction, gate, sources.ReadInputFacts, CreateCursorSnap(feedback), feedback.Occlusion,
                sources.ReportBlockedInput, movementUndo);
            var deckPlacement = new CombatDeckPlacementInput(interaction, sources.Characters, sources.ReadInputFacts,
                sources.CanReceiveUiInput, sources.Faults);
            // 가방 아이템 입력은 손패 카드와 같은 수신 조건을 쓴다(가방 설계 P4).
            var itemInput = new CombatItemInput(interaction, sources.Characters, sources.ReadInputFacts, sources.CanReceiveUiInput,
                sources.Faults);
            CombatEnvironmentHover environmentHover = CreateEnvironmentHover(feedback, sources.Tables, sources.Text);
            // 서비스 바인딩에서 확인한 계약을 넘기며 여기서 표시 형식을 발견하지 않습니다.
            var deployment = new CombatDeploymentEntry(sources.State, sources.Characters, sources.Confirmation,
                sources.Faults, sources.IsInputReady, sources.ReadInputFacts, router, notices, sources.AfterConfirmedSubmit);
            // 턴 종료 확인창은 배치 확인창과 같은 공용 확인창·같은 콜백을 쓴다.
            var endTurn = new CombatEndTurnEntry(sources.Characters, sources.Confirmation, sources.Faults, sources.IsInputReady,
                sources.ReadInputFacts, router, sources.AfterConfirmedSubmit);
            // 카메라 조작 입력은 이 연결 표시의 시점만 움직이고, 가장자리 이동은 Router와 같은 UI 가림 판정을 씁니다(카메라 조작 설계 CC1·Q-CC5 A).
            var camera = new CombatCameraInput(display.CameraControl, feedback.Occlusion,
                () => new UnityEngine.Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height));
            return new CombatSceneInput(gate, interaction, router, deckPlacement, itemInput, environmentHover, notices, deployment,
                endTurn, actionAvailability, hudActionAvailability, camera, movementUndo, roarForecast, skillAvailability);
        }

        /// <summary>
        /// 씬 연결 한 번에 쓸 입력 기능 묶음을 조립합니다. 첫 그리기 성공 뒤에 부릅니다(경로 입력이 현재 전투를 기억).
        /// 연결 입력 묶음(<see cref="CreateSceneInput"/>)이 부릅니다.
        /// </summary>
        /// <param name="feedback">입력 기능이 쓰는 좁은 계약(집기·입력 중 표시·좌표 변환·이동 초안 표시)의 묶음입니다(씬 표시 나누기 설계 DS2).</param>
        /// <param name="readPathFacts">경로 입력이 판단할 때마다 새로 읽는 사실 공급자입니다(선행 미처리 명령 포함).</param>
        /// <param name="sounds">선택·취소 효과음 창구입니다(표시 서비스의 오디오 + 표시 표의 프리셋, View 3라운드 W9-2).</param>
        /// <param name="itemStock">가방 아이템 드래그가 읽는 아이템 키별 재고 수입니다(가방 설계 P4). 표시 값 읽기가 Buffer에서 읽습니다.</param>
        /// <param name="actionAvailability">씬 입력이 소유하는 가용성 조회입니다. 필수입니다(평가 C5-02).</param>
        /// <param name="reportBlockedInput">실제 금지 이동 시도가 끝났을 때 기존 튜토리얼 안내를 요청합니다.</param>
        /// <param name="playerSkills">플레이어 액티브 스킬 외형 표입니다. 스킬 모드가 공격 연출(선 높이)을 찾습니다(전투 스킬 View 설계 SK5).</param>
        private static (CombatInteraction Interaction, CombatActionNotice Notices) CreateSceneInteraction(
            CombatInputFeedback feedback,
            ICombatStateBufferReadSink state,
            ICharacterBufferReadSink characters,
            ICombatViewBufferReadSink inputRead,
            ICombatViewBufferWriteSink inputWrite,
            CombatCommandGate gate,
            Func<CombatInputFacts> readPathFacts,
            CombatInputSounds sounds,
            Func<int, int> itemStock,
            IUITextPresentation text,
            CombatActionAvailabilityTracker actionAvailability, Action reportBlockedInput, IPlayerSkillVisualTableQuery playerSkills)
        {
            if (feedback == null)
                throw new ArgumentNullException(nameof(feedback));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (inputRead == null)
                throw new ArgumentNullException(nameof(inputRead));
            if (inputWrite == null)
                throw new ArgumentNullException(nameof(inputWrite));
            if (readPathFacts == null)
                throw new ArgumentNullException(nameof(readPathFacts));
            if (itemStock == null)
                throw new ArgumentNullException(nameof(itemStock));
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (actionAvailability == null)
                throw new ArgumentNullException(nameof(actionAvailability));
            if (reportBlockedInput == null)
                throw new ArgumentNullException(nameof(reportBlockedInput));
            if (playerSkills == null)
                throw new ArgumentNullException(nameof(playerSkills));
            // 경로 입력을 만들기 전에 확인한다(경로 생성자가 이동 상호작용을 연결한다).
            if (gate == null)
                throw new ArgumentNullException(nameof(gate));
            if (sounds == null)
                throw new ArgumentNullException(nameof(sounds));

            // 텍스트 서비스는 필수다(View 2라운드 W6-7).
            var notices = new CombatActionNotice(text);
            var path = new CombatPathInput(state, characters, inputRead, inputWrite, feedback.Mapping, feedback.Movement, readPathFacts, notices,
                actionAvailability, reportBlockedInput);
            var item = new CombatItemDragInteraction(feedback.Picker, feedback.HoverCell, feedback.ItemDrag, gate, itemStock);
            // 유닛 호버 공격 범위는 씬 조립에서만 만든다(호버 공격 범위 설계 §4.1). 칸 집기·UI 가림·호버 범위는 묶음의 계약이다.
            // 이동 모드 행위자는 경로 입력이 알려 준다(설계 §9, 그 유닛은 호버하지 않음).
            var rangeHover = new CombatUnitRangeHover(inputWrite, inputRead, state, feedback.Picker, feedback.Occlusion, feedback.UnitRange, path, gate);
            // 배치·공격 조준의 효과음·궤적·거절 안내·가용성 조회는 모두 필수다(평가 C5-02). 고스트·공격 범위와 호버 칸은 따로 받는다(결정 Q-DS2 A).
            var placement = new PlacementDragInteraction(inputWrite, feedback.Picker, feedback.Placement, feedback.HoverCell, gate, sounds);
            // 넉백 미리보기·공격 모드 대상 표시 의도도 묶음이 내놓는다(넉백 미리보기 설계 KB1, 결정 Q-KB4 A / 이펙트 끝 규칙 계획 TP-E16).
            var attack = new AttackTargetingInteraction(inputWrite, inputRead, feedback.Picker, feedback.AttackRange, feedback.HoverCell, gate,
                feedback.Knockback, feedback.AttackModeTargets, sounds,
                rejections: notices, trajectory: feedback.Trajectory, actionAvailability: actionAvailability);
            // 플레이어 액티브 스킬 모드는 스킬 외형 표(공격 연출·선 높이)와 거절 안내를 받는다(전투 스킬 View 설계 SK1·SK2).
            var skill = new CombatPlayerSkillInteraction(inputWrite, inputRead, state, feedback.Picker, feedback.PlayerSkill, feedback.HoverCell,
                gate, notices, playerSkills, sounds);
            return (new CombatInteraction(placement, attack, path, new CombatAttackDragGesture(feedback.Mapping), item, rangeHover, skill), notices);
        }

        /// <summary>배치 Drag·경로 지정·공격 모드 중 칸이 바뀔 때만 OS 커서를 칸 중심으로 옮기는 스냅을 만듭니다(커서 그리드 스냅 설계). 연결 입력 묶음을 만드는 <see cref="CreateSceneInput"/>이 부릅니다.</summary>
        private static CombatCursorSnap CreateCursorSnap(CombatInputFeedback feedback)
        {
            if (feedback == null)
                throw new ArgumentNullException(nameof(feedback));

            return new CombatCursorSnap(feedback.Mapping, feedback.Cursor);
        }
    }
}
