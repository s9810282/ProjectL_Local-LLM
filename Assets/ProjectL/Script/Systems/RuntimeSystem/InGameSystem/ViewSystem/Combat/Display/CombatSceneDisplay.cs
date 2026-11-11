using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// Combat 씬 연결 한 번의 표시 수명(설치·결과 끝 그리기·정리·해제)과 그 순서입니다(씬 표시 나누기 설계 DS7, 평가 C2-03).
    /// 각 단계의 본문은 하위 표시가 맡습니다: 필드(<see cref="CombatFieldDisplay"/>), 선택·목표(<see cref="CombatSelectionDisplay"/>),
    /// HUD 표면(<see cref="CombatHudSurfaces"/>), 행동 버튼(<see cref="CombatActionButtonsDisplay"/>), 결과 화면(<see cref="CombatResultScreenDisplay"/>).
    /// </summary>
    /// <remarks>
    /// 씬 연결(Attach) 하나에 하나 만들어지고 Release 뒤 다시 쓰지 않습니다. Buffer를 읽지 않고 받은 값만 씁니다.
    /// 입력 피드백 계약은 실제 주인이 구현하고 이 표시는 묶음(<see cref="Feedback"/>)을 넘기기만 합니다(설계 DS2).
    /// 사건 재생은 <see cref="CombatSceneEventPlayback"/>(재생 계약으로 그대로 넘김, <see cref="EventPlayback"/>)이 맡습니다(구조 리팩토링 설계 D5).
    /// "이 연결의 표시가 해제됨"의 원본은 공유 수명(<see cref="CombatDisplayLifetime"/>) 하나이고 해제 첫 줄에서 세웁니다(설계 DS1).
    /// 이 표시는 그 뒤 Install·Refresh·Clear·완료 표시·준비 생성을 예외로 막습니다. 공유 수명을 받는 객체(고스트·공격·선택·HUD 표면·행동 버튼)는
    /// 입력 표시 의도·새 연결을 무시하고, 숨기기·정리·연결 해제(콜백 모두 null)는 막지 않습니다.
    /// 포인터 매퍼·이동 초안·<c>FieldView</c>는 자기 해제 상태로 거르고(집기는 false), 공격 모드 대상은 값만 저장합니다(설계 P4·P7·P10).
    /// 연결 해제가 입력을 먼저 멈추므로 해제 뒤 입력 피드백 호출은 운영에서 생기지 않습니다. 사건 시작·Tick·취소의 같은 규칙은 재생 조정이 지킵니다(A-4b).
    /// 배경은 씬 수명으로 남고 공용 카메라는 Session이 표시 해제 때 기준 자세·렌즈·Priority를 복원합니다.
    /// 정리 목록은 하위 표시의 묶음을 <see cref="Join"/>으로 펼쳐 한 겹으로 실행합니다(설계 §3.8, 계획 TP-11).
    /// </remarks>
    internal sealed class CombatSceneDisplay : ICombatBattleDisplay
    {
        private readonly CombatDisplayLifetime _lifetime;
        private readonly CombatDisplaySnapshot _snapshot;
        private readonly CombatInputFeedback _feedback;
        private readonly CombatFieldDisplay _field;
        private readonly CombatSelectionDisplay _selection;
        private readonly CombatHudSurfaces _hud;
        private readonly CombatActionButtonsDisplay _actions;
        private readonly CombatResultScreenDisplay _completion;
        // 국면 배경음 정책입니다(평가 C2-03 1단계). 결과 화면이 같은 객체를 받아 승리·완료 뒤 배경음을 냅니다.
        private readonly CombatBgmPolicy _bgm;
        private readonly FieldUnitPresenter _units;
        private readonly PlacementGhost _ghost;
        private readonly UnitHUDLayer _unitHudLayer;
        private readonly CombatMovementPlanningPresenter _movementPlanning;
        private readonly CombatSceneEventPresenter _events;
        // 사건 재생 조정(구조 리팩토링 설계 D5). 사건 Presenter·레일 표시의 필드 연결 수명은 이 표시가 정한다.
        private readonly CombatSceneEventPlayback _eventPlayback;
        // 화면 좌표 → 칸·격자 변환과 UI 가림 Raycast(구조 개선 VR4). Install 때 연결, Clear 때 해제, Release 때 최종 해제.
        private readonly CombatPointerMapper _pointer;
        // 공용 시점의 조작·연출 창구와 복원 수명을 한 번 소유합니다.
        private readonly CombatCameraSession _cameraSession;
        private readonly CombatEntryPresentation _entryPresentation;
        private CombatUnitPreparation _unitPreparation;
        // 정리 묶음(D8): Mapper를 붙잡고 스스로 진행할 수 있는 표시다. Install 직전·실패와 Clear가 이 목록 하나를 함께 쓴다.
        private readonly Action[] _fieldBound;

        /// <summary>Factory가 조립한 같은 협력 객체를 보관하고 공통 정리 목록을 정의합니다.</summary>
        /// <remarks>
        /// SceneRoot 하나에 살아 있는 Display는 하나입니다. 생성·검증·Configure는 CombatSceneDisplayFactory가 담당합니다.
        /// 공유 수명·스냅숏·배경음 정책은 Factory가 하나씩 만들어 이 표시와 하위 객체에 같은 것을 넘깁니다(계획 TP-12).
        /// 이동 초안 표시는 정리 목록이 <c>Clear</c>·<c>Release</c>를 부르므로 묶음과 따로 받습니다(설계 DS7).
        /// </remarks>
        /// <param name="feedback">입력 피드백 계약 묶음입니다(설계 DS2). 입력 Factory가 계약 이름으로 꺼냅니다.</param>
        /// <param name="cameraSession">공용 카메라 연결 한 번의 수명입니다.</param>
        internal CombatSceneDisplay(
            CombatDisplayLifetime lifetime,
            CombatDisplaySnapshot snapshot,
            CombatInputFeedback feedback,
            CombatFieldDisplay field,
            CombatSelectionDisplay selection,
            CombatHudSurfaces hud,
            CombatActionButtonsDisplay actions,
            CombatResultScreenDisplay completion,
            CombatBgmPolicy bgm,
            FieldUnitPresenter units,
            PlacementGhost ghost,
            UnitHUDLayer unitHudLayer,
            CombatMovementPlanningPresenter movementPlanning,
            CombatSceneEventPresenter events,
            CombatSceneEventPlayback eventPlayback,
            CombatPointerMapper pointer,
            CombatCameraSession cameraSession,
            CombatEntryPresentation entryPresentation = null)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _field = field ?? throw new ArgumentNullException(nameof(field));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _completion = completion ?? throw new ArgumentNullException(nameof(completion));
            _bgm = bgm ?? throw new ArgumentNullException(nameof(bgm));
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
            _ghost = ghost != null ? ghost : throw new ArgumentNullException(nameof(ghost));
            _unitHudLayer = unitHudLayer != null ? unitHudLayer : throw new ArgumentNullException(nameof(unitHudLayer));
            _movementPlanning = movementPlanning ?? throw new ArgumentNullException(nameof(movementPlanning));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _eventPlayback = eventPlayback ?? throw new ArgumentNullException(nameof(eventPlayback));
            _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
            _cameraSession = cameraSession ?? throw new ArgumentNullException(nameof(cameraSession));
            _entryPresentation = entryPresentation;
            _fieldBound = Join(
                new Action[] { CancelEntryPresentation, _eventPlayback.CancelPendingEvents, _movementPlanning.Clear, _events.Clear, _pointer.Unbind },
                _field.BoundClears);
        }

        /// <summary>입력 피드백 계약 묶음입니다(설계 DS2·P1). 조정자는 쓰지 않고 입력 Factory로 넘기기만 합니다.</summary>
        public CombatInputFeedback Feedback => _feedback;

        /// <summary>행동 버튼 표시입니다(설계 DS4·P1). 조정자의 버튼 연결과 입력 표면의 가용성 맞춤이 이것만 씁니다.</summary>
        public ICombatActionDisplay Actions => _actions;

        /// <summary>정보창 선택 계약입니다(설계 DS3·P1). 묶음 계약 밖이라 조정자가 구체 형식에서 받습니다.</summary>
        internal ICombatUnitInspection Inspection => _selection;

        /// <summary>카메라 조작 시점입니다(카메라 조작 설계 CC1). 카메라 입력이 이것만 움직입니다.</summary>
        public ICombatCameraControl CameraControl => _cameraSession.Control;

        internal ICombatEntryPresentation EntryPresentation => _entryPresentation;

        /// <summary>
        /// 순차 연출이 쓰는 재생 계약입니다. 위임하지 않고 재생 조정을 그대로 넘깁니다(연출 조립 설계 §9 I-4, 결정 Q-R16 A).
        /// 해제 뒤 사건 시작 거절(D9)은 재생 조정이 합니다.
        /// </summary>
        public ICombatEventPlayback EventPlayback => _eventPlayback;

        #region 표시

        /// <summary>이 표시 연결에 귀속된 준비를 조립합니다. 진행은 AfterLoad 뒤 LoadingSystem이 소유합니다.</summary>
        internal CombatUnitPreparation CreateUnitPreparation(CombatUnitPreparationInput input, Func<bool> isCurrent)
        {
            ThrowIfReleased();
            if (_unitPreparation != null)
                throw new InvalidOperationException("[CombatSceneDisplay] 같은 연결의 선택 유닛 준비가 이미 있습니다.");
            _unitPreparation = new CombatUnitPreparation(_units, input, isCurrent);
            return _unitPreparation;
        }

        private void CancelUnitPreparation() => _unitPreparation?.Cancel();

        public void Install(in CombatDisplayValues values)
        {
            ThrowIfReleased();
            ClearSelection();
            _hud.ClearTurn();
            // 설치 중의 UI/필드 콜백보다 먼저 목록 시점을 고정합니다. RefreshCore에서 한 번 적용합니다.
            IReadOnlyList<CharacterData> characters = CombatDisplaySnapshot.Copy(values.Characters);
            // Build 전 실패라도 이전 Mapper로 사건 표시·좌표 변환이 재개되지 않도록 먼저 연결을 끊는다.
            // Object·Terrain·흔적은 Mapper를 붙잡고 스스로 움직이지 않으므로 비우지 않는다. 아래 Refresh가 새 Mapper로 다시 그리고 View를 재사용한다(D8).
            RunAll(_fieldBound);
            try
            {
                _completion.Hide();
                _hud.Present();
                CombatStateData state = values.State;
                _field.Build(state);
                _events.Bind(_field.Mapper);
                _pointer.Bind(_field.Mapper);
                _field.ApplyStaticHighlights(state);
                _entryPresentation?.PrepareDisplay();
                RefreshCore(values, characters);
                // 첫 그리기 뒤, 아직 안 만든 것만 미리 만듭니다(GC 끊김 설계 GH6). 결과 끝 Refresh에서는 하지 않습니다.
                // 등장 연출 준비보다 먼저 해, 미리 만들기가 실패해도 준비된 등장 연출이 남지 않게 합니다.
                Prewarm(characters, state.NexusReinforcements);
                _entryPresentation?.Prepare(characters, state.Structures);
            }
            catch (Exception error)
            {
                ClearSelection();
                // 연결됐던 대상까지 끊고, 일부만 그려진 Object·Terrain·기믹(흔적·토네이도·낙석·하붑)도 비운다. 다음 Install에서 다시 연결한다.
                try
                {
                    RunAll(Join(_fieldBound, new Action[] { _hud.ClearTurn, _hud.ClearUnitInfo }, _field.ObjectClears, _field.GimmickClears));
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
        }

        private void CancelEntryPresentation() => _entryPresentation?.Cancel();

        public void Refresh(in CombatDisplayValues values)
        {
            ThrowIfReleased();
            RefreshCore(values, CombatDisplaySnapshot.Copy(values.Characters));
        }

        private void RefreshCore(in CombatDisplayValues values, IReadOnlyList<CharacterData> characters)
        {
            if (!_eventPlayback.IsPresentationComplete) throw new InvalidOperationException("남은 기차·유닛 연출을 마친 뒤 최종 사실을 표시해야 합니다.");
            CombatStateData state = values.State;
            _snapshot.Record(characters, state.CommandState.Turn, state.CommandState.Phase);
            _selection.Synchronize(state.CommandState.Turn.StagePhase, values.Owned, state.TraitSettings);
            // 칸 표면 높이(고지대)는 그 위에 서는 유닛보다 먼저 정합니다(고지대 View 설계 F10·HG-2).
            _field.SyncSurface(state);
            // 설치 첫 그리기와 결과 끝 그리기의 국면 배경음은 배경음 정책이 냅니다(평가 C2-03). 결과 재생 중 국면이 바뀌는 사건의 배경음은 사건 소리(CombatEventAudio)가 냅니다.
            // 국면 → 프리셋 번호는 소리 프리셋 한 곳입니다(구조 원칙 평가 R2 (a), View 3라운드 W9-2).
            _bgm.ApplyPhase(state.CommandState.InCombat);
            // 목표 대상은 사실 그리기(_events.Refresh) 전에 정해, 그 그리기가 부르는 강조 맞춤이 이번 목표를 쓰게 한다(평가 C1-14).
            _selection.SetObjective(state.CommandState.Objective);
            _events.Refresh(_snapshot.Characters, state.Structures);
            _selection.AfterFacts();
            _field.RenderObjects(state, values.StageKey);
            _hud.RenderStatus(values.RunGold, state.CommandState.Turn, state.CommandState.Phase);
            // 진입 배치 표시는 같은 턴 표시 UI·덱 카드를 다루므로 HUD 그리기 둘 사이에 둡니다(설계 §3.8).
            _entryPresentation?.RenderDeployment(state.CommandState.Turn.StagePhase, state.CommandState.Phase);
            _hud.RenderPanels(values.ItemInventory, state.CommandState.Turn.StagePhase);
            // 정보창의 UI 콜백에서 씬 연결이 해제될 수 있습니다.
            if (_lifetime.IsReleased)
                return;
            _selection.RenderHighlights();
        }

        /// <summary>
        /// 전투 중 처음 나올 때 만들던 유닛 표시·HUD·고스트·투사체·몸 위 글자를 설치 때 만듭니다(GC 끊김 설계 GH6, 계획 TP-6).
        /// 씬 전환 로딩 안이라 끊김이 보이지 않습니다. 무엇을 몇 개 만들지는 <see cref="CombatPrewarmPlan"/>이 정합니다(설계 DS6).
        /// 실패하면 설치 실패 정리 길로 갑니다(GC 계획 TP-7).
        /// </summary>
        private void Prewarm(IReadOnlyList<CharacterData> characters, NexusReinforcementSpec reinforcements)
        {
            CombatPrewarmPlan plan = CombatPrewarmPlan.From(characters, reinforcements);
            _units.Prewarm(plan.Counts);
            for (int i = 0; i < plan.PlayerKeys.Count; i++)
                _ghost.Prewarm(plan.PlayerKeys[i]);
            _events.Prewarm(plan.Keys);
        }

        // 선택(정보창·HUD 선택·목표)과 완료 표시 목록을 비웁니다. 선택 바뀜 알림은 내지 않습니다.
        private void ClearSelection()
        {
            _selection.Clear();
            _snapshot.Clear();
        }

        public void Clear()
        {
            ThrowIfReleased();
            RunAll(Join(
                new Action[] { CancelUnitPreparation, ClearSelection },
                _fieldBound,
                new Action[]
                {
                    _hud.ClearUnitInfo, _units.HudFeedback.ClearAttackHealthPreview, _ghost.Hide, _units.Release,
                    _completion.Hide, _hud.ClearHud, _hud.ClearTurn, _actions.Hide
                },
                _field.ObjectClears,
                _field.GimmickClears,
                new Action[] { _field.ReleaseField }));
        }

        public void Release()
        {
            if (_lifetime.IsReleased)
                return;

            _lifetime.MarkReleased();
            // 해제 뒤 사건 시작 거절(D9)은 재생 조정이 맡습니다. 정리 목록보다 먼저 알립니다.
            _eventPlayback.MarkReleased();
            // 소유 자원 해제(D8)는 이 목록 한 곳에서만 정의한다. 기존 표시는 풀·참조를 정리하고 View 소유 레일 인스턴스는 제거한다.
            // 셀 색은 되돌리지 않는다.
            RunAll(Join(
                new Action[]
                {
                    CancelUnitPreparation, ClearSelection, CancelEntryPresentation, _selection.ReleaseInspection, _hud.ClearUnitInfo,
                    _eventPlayback.CancelPendingEvents, _movementPlanning.Release, _units.HudFeedback.ClearAttackHealthPreview,
                    _pointer.Release, _events.Release
                },
                _field.BoundReleases,
                new Action[]
                {
                    _completion.Hide, _hud.ClearTurn, _actions.Unbind, _actions.Hide,
                    _ghost.Release, _units.Release, _units.ReleaseHudLayer, _units.ReleasePools, ReleaseUnitHudLayer
                },
                _field.ObjectReleases,
                _field.GimmickReleases,
                new Action[] { _cameraSession.Release, _field.ReleaseField }));
        }

        public void ShowCompletion(BattleCompletionChanges completion, Func<bool> confirm)
        {
            ThrowIfReleased();
            // 확인을 받아들였을 때만 완료 뒤 배경음으로 바꾼다. 국면 배경음(Refresh)·승리 효과음과 같은 배경음 정책이 낸다(View 2라운드 W6-5, 평가 C2-03).
            _completion.Show(completion, confirm);
            _events.HideBodyText();
        }

        // HUD 아이콘 우클릭 해제는 유닛 표시가 HUD에 연결한다(가방 설계 P6). 풀기(null)는 해제 뒤에도 받는다.
        public void BindItemUnequip(Action<EntityId> unequip)
        {
            if (!_lifetime.IsReleased || unequip == null) _units.BindItemUnequip(unequip);
        }

        public void SetUnitActionAvailability(EntityId entityId, CombatActionAvailability availability)
        {
            if (!_lifetime.IsReleased) _units.SetHudActionAvailability(entityId, availability);
        }

        #endregion

        private void ReleaseUnitHudLayer()
        {
            if (_unitHudLayer == null) return;
            _unitHudLayer.Release();
            if (Application.isPlaying) UnityEngine.Object.Destroy(_unitHudLayer.gameObject);
            else UnityEngine.Object.DestroyImmediate(_unitHudLayer.gameObject);
        }

        private void ThrowIfReleased()
        {
            if (_lifetime.IsReleased)
                throw new InvalidOperationException("[CombatSceneDisplay] 씬 연결이 해제된 표시입니다.");
        }

        /// <summary>정리 묶음들을 차례대로 펼친 목록을 만듭니다. 묶음을 중첩 실행하지 않아 실패가 한 겹으로 모입니다(계획 TP-11).</summary>
        private static Action[] Join(params Action[][] groups)
        {
            int length = 0;
            for (int i = 0; i < groups.Length; i++)
                length += groups[i].Length;
            var all = new Action[length];
            int offset = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i].CopyTo(all, offset);
                offset += groups[i].Length;
            }
            return all;
        }

        /// <summary>하나가 실패해도 나머지를 실행하고 실패를 모아 던집니다(공용 규칙 <see cref="CleanupErrors"/>).</summary>
        private static void RunAll(params Action[] actions) =>
            CleanupErrors.RunAll("[CombatSceneDisplay] 표시 정리 중 실패했습니다.", actions);
    }
}
