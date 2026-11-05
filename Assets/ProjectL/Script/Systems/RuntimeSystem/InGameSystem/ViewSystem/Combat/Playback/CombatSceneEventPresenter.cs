using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback
{
    /// <summary>
    /// 사건 스냅샷을 표시 단위로 나눠 줍니다. 공통 수명(재생 세대·Mapper·취소·해제), 공개 진입점, 사건 하나 안의 순서 틀을 소유합니다.
    /// 재생기(공격·조각 실행·걸음·대기·반응)의 시작 조건·프레임 진행·완료·정리는 실행 목록(<see cref = "CombatPresentationPlayers"/>)이 맡고
    /// (연출 조립 설계 B11·B12), 형식별 표시 지식은 처리기 표(<see cref = "CombatEventHandlerTable"/>), 보인 사실은 <see cref = "CombatShownFacts"/>,
    /// 재생 강조는 <see cref = "CombatPlaybackHighlight"/>, 소리는 <see cref = "CombatEventAudio"/>, 이동 표시는 <see cref = "CombatMovementPresentation"/>이 맡습니다
    /// (Presenter 분리 설계 S1~S6). 공격 연출은 <see cref = "CombatAttackPlayback"/>, 걸음·회전은 유닛별 레인(<see cref = "CombatUnitMotionSet"/>)이 진행합니다
    /// (구조 리팩토링 설계 D10, 동시 처리 설계 C5).
    /// 결과 확인과 풀 반환은 호출자가 담당합니다. 재생 계약(<see cref = "ICombatEventPlayback"/>)과 같은 이름의 멤버를 갖지만 구현하지 않습니다(구조 개선 SV3).
    /// </summary>
    internal sealed class CombatSceneEventPresenter
    {
        private static readonly Unity.Profiling.ProfilerMarker PresentEventMarker = new("Combat.Result.PresentEvent");
        // 유닛 표시 창구입니다(연출 조립 설계 B13). 재생 쪽의 자세·크기·걸음 배치·표시 회전·사망 유지는 모두 여기서 빌립니다.
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly CombatEnemyPresentationPacing _pacing;
        // 아래 부품 필드는 처리기·이동 표시 조립에 쓰고, 시험이 리플렉션으로 읽습니다(_attacks·_reaction·_motion, 분리 계획 §1).
        // 수명(시작 조건·Tick·완료·정리)은 재생기 실행 목록(_players)만 다룹니다.
        private readonly CombatUnitReactionSet _reaction = new();
        private readonly CombatAttackPlayback _attacks;
        // 유닛별 걸음 레인입니다(동시 처리 설계 C5). 같은 순간에 밀린 여러 유닛이 함께 걸을 수 있습니다.
        private readonly CombatUnitMotionSet _motion;
        // 재생기 실행 목록입니다(연출 조립 설계 B12). 순서가 곧 겹침 규칙이라 Create의 자리로 고정합니다.
        private readonly CombatPresentationPlayers _players;
        // 간격·머묾 대기 칸의 역할 계약입니다(설계 B12 보완 1). 걸음·대기 재생기가 구현합니다.
        private readonly ICombatWaitSlot _waitSlot;
        // 재생 강조 층에 둔 칸입니다(Presenter 분리 설계 S4). 어떤 칸을 둘지는 포효·소환 처리기가 고릅니다.
        private readonly CombatPlaybackHighlight _highlight;
        // 지금 보인 사실의 원본입니다(Presenter 분리 설계 S1). 걸음·공격 재생·소리도 여기서 읽습니다.
        private readonly CombatShownFacts _facts;
        private CombatStructureDamage _structureDamage;
        internal void BindStructureDamage(CombatStructureDamage damage) =>
            _structureDamage = damage ?? throw new ArgumentNullException(nameof(damage));
        internal ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Selection.ICombatInspectionFactsReader InspectionFacts => _facts;
        internal void BindInspectionRendered(Action rendered) => _facts.BindRendered(rendered);
        // 사건 효과음·배경음 호출입니다(Presenter 분리 설계 S5). 값과 호출 시점은 그대로이고 위치만 옮겼습니다.
        private readonly CombatEventAudio _sound;
        // 표시하는 사건 형식과 그 처리기입니다(Presenter 분리 설계 S3). 사건 하나 안의 순서는 PresentEvent 틀 하나가 정합니다.
        private readonly CombatEventHandlerTable _handlers;
        // 이동 표시입니다(Presenter 분리 설계 S6). 공개 진입점과 시작 조건은 여기 남고, 확인 뒤 넘깁니다.
        private readonly CombatMovementPresentation _movement;
        private readonly CombatPositionSwapPresentation _positionSwap;
        // 연출 이펙트 창구입니다(이펙트 설계 EF6, 필수). 재생 취소 때 회수·해제 때 닫는 일은 이 Presenter가 합니다(결정 Q-E4 A).
        private readonly CombatEffectWindow _effects;
        // 사실 사건의 순간 큐를 내는 곳입니다(이펙트 창구 확장 설계 EC7). 사건 소리 바로 뒤에 부르고, 큐 값은 연결 동안 보관합니다.
        private readonly CombatMomentCuePlayback _momentCues;
        private readonly CombatTraitFeedbackPlayback _feedback;
        // 전투 설치 때 미리 만들 몸 위 글자 프리팹을 고를 때만 씁니다(GC 끊김 설계 GH6). 글자 창구는 없을 수 있습니다.
        private readonly CombatCueSources _cueSources;
        private readonly CombatBodyTextWindow _bodyText;
        // 공격 모드 대상 유지 이펙트의 출처입니다. 공격 조준이 씬 표시를 거쳐 대상을 넘깁니다(이펙트 끝 규칙 계획 TP-E16).
        private readonly CombatAttackModeEffectSource _attackModeTargets;
        private FieldGridMapper _mapper;
        // 순차 재생기가 붙을 때 받는 후속 정리 질문입니다(유닛 행동 계획 TP-13). 공격 반응 행동(대신 맞기)의 돌아오기가 읽습니다(TS-f4에서 연결).
        private ICombatSourceSettlement _settlement;
        // 재생 세대입니다. 이 Presenter와 하위 재생 부품(공격·걸음) 범위에서는 이 값 하나만 원본이며 부품은 읽기만 합니다(D10).
        // 사건 재생 조정(CombatSceneEventPlayback)의 실패 정리 버전과는 범위가 다른 별도 카운터입니다.
        // Unity 표시 중 재진입한 취소/해제가 이전 사건의 완료 콜백을 되살리지 않도록 한다.
        private int _presentationVersion;
        /// <summary>
        /// 모든 의존을 필수로 받습니다(View 3라운드 W9-3, 결정 Q-V27 A). 운영은 표시 Factory가 설정 표·필드 표시로 묶음을 만들어 넘기고,
        /// 시험 조립은 시험 도우미(<c>CombatViewSystemTests.EventValues</c>·<c>FieldPorts</c>)로 만든 묶음과 가짜 서비스를 넘깁니다.
        /// </summary>
        /// <param name = "values">연출 값 묶음입니다. 값 검사는 묶음 생성자가 합니다.</param>
        /// <param name = "audio">효과음·배경음 서비스입니다.</param>
        /// <param name = "sounds">사건 소리의 프리셋 번호를 답합니다(표시 표의 것, View 3라운드 W9-2).</param>
        /// <param name = "cueSources">사실 사건의 순간 큐 값을 답합니다(표시 표의 것, 이펙트 창구 확장 계획 P9).</param>
        /// <param name = "camera">맵 카메라 창구입니다. 표시 Factory가 맵 프랍과 함께 넘깁니다(카메라 연출 조각 설계 K1). 조각 실행이 줌을 빌립니다.</param>
        /// <param name = "effects">연출 이펙트 창구입니다(이펙트 설계 EF6). 클립 공격·투사체 접촉·피격 이펙트가 함께 씁니다.</param>
        /// <param name = "projectileRoot">투사체를 둘 부모입니다(필드 칸 루트).</param>
        /// <param name = "ports">필드 표시 창구 묶음입니다(가시·오브젝트·토네이도·지형 바뀜·재생 강조).</param>
        internal CombatSceneEventPresenter(FieldUnitPresenter units, CombatEventPresentationValues values, IAudioSystemService audio, CombatSoundPresets sounds, CombatCueSources cueSources, CombatCameraWindow camera, CombatEffectWindow effects, Transform projectileRoot, CombatFieldEventPorts ports, CombatBodyTextWindow bodyText = null, CombatTraitStateEvidenceRegistry feedbackEvidence = null)
        {
            if (units == null)
                throw new ArgumentNullException(nameof(units));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            if (projectileRoot == null)
                throw new ArgumentNullException(nameof(projectileRoot));
            if (ports == null)
                throw new ArgumentNullException(nameof(ports));
            _presentation = units.Presentation;
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            _cueSources = cueSources ?? throw new ArgumentNullException(nameof(cueSources));
            _bodyText = bodyText;
            _pacing = new CombatEnemyPresentationPacing(values.EnemyActionGapSeconds, values.EnemyTurnGapSeconds);
            _highlight = new CombatPlaybackHighlight(ports.PresentHighlight);
            Func<int> generation = () => _presentationVersion;
            Func<FieldGridMapper> mapper = () => _mapper;
            // 오래 보이는 이펙트(상태 유지·특성 상시)는 보인 사실을 그릴 때마다 맞춥니다(이펙트 끝 규칙 설계 EL4). 출처마다 클래스 하나입니다(계획 TP-E23).
            if (cueSources == null) throw new ArgumentNullException(nameof(cueSources));
            _attackModeTargets = new CombatAttackModeEffectSource(cueSources.Traits);
            var persistent = new CombatPersistentEffects(effects, new ICombatPersistentEffectSource[]
            {
                new CombatStatusEffectSource(cueSources.UnitStatuses),
                new CombatOwnedTraitEffectSource(cueSources.Traits),
                _attackModeTargets,
            });
            _facts = new CombatShownFacts(units, mapper, persistent);
            effects.BindBodyCategories(id => _facts.FindCharacter(id)?.Stats.Category.Key);
            _sound = new CombatEventAudio(audio, sounds, _facts.FindCharacter);
            // 투사체 접촉 이펙트도 같은 창구로 냅니다. 회수는 이 Presenter의 취소·해제가 합니다(투사체 접촉 이펙트 설계 IE5).
            _attacks = new CombatAttackPlayback(_presentation, projectileRoot, generation, _facts.TryGetBody, effects);
            // 걸음 접촉 원본의 몸은 가시·오브젝트 표시가 좁은 조회로 줍니다(이동 접촉 설계 M2).
            // 이동자 몸은 사실 그리기와 같은 배치 규칙을 쓰므로 유닛 표시가 줍니다(이동 접촉 설계 M2).
            // 걸음 모양 표는 값 묶음에서 한 번 만듭니다. 걸음 시간·높이·접촉 시점은 레인이 이 표에서 고른 모양에 묻습니다(걸음 모양 설계 ST2).
            _motion = new CombatUnitMotionSet(_presentation, units.BodyOf, CombatStepStyles.From(values), values.MovementTurnSeconds, generation, _facts.ShowCharacter, _sound.PlayMovement, ports.ThornBody, ports.ObjectBody, values.LeapMotion);
            // 재생기를 이름으로 아는 곳은 이 조립 하나입니다(설계 B12). 그 밖의 멤버는 실행 목록과 역할 계약만 씁니다.
            var attackPlayer = new CombatAttackPlayer(_attacks);
            var stepPlayer = new CombatStepPlayer(_motion, PresentEvent, generation, mapper);
            var reactionPlayer = new CombatReactionPlayer(_reaction);
            // 조각 실행 집합입니다(조각·조립 설계 BP5). 포효·소환 처리기가 조립을 넘깁니다(설계 BP7).
            var piecePlayer = new CombatPieceRunPlayer(_presentation, camera, attackPlayer, _attacks, generation, mapper);
            // 사실 사건의 순간 큐는 같은 조각 실행 집합에 곁들임 실행으로 넘깁니다(이펙트 창구 확장 설계 EC6).
            _momentCues = new CombatMomentCuePlayback(cueSources, piecePlayer, _facts, effects, camera, mapper, generation);
            _players = CombatPresentationPlayers.Create(attack: attackPlayer, pieces: piecePlayer, step: stepPlayer, reaction: reactionPlayer, bodyText: bodyText, appearance: new CombatFieldAppearancePlayer(ports.TickAppearance, ports.AppearancePlaying, ports.AppearanceBlocksStart, ports.CancelAppearance));
            _waitSlot = stepPlayer;
            // 조립은 이 생성자 안에서 합니다(설계 S8, 결정 Q-S3 A). 처리기는 원본(보인 사실·세대·Mapper)을 참조로 쓰고 사본을 갖지 않습니다.
            // 발동한 오브젝트 원본 표시는 사건 시점에 숨기고(이동 접촉 설계 M6), 결과 끝 Refresh가 최종 사실로 다시 맞춥니다.
            // 무너진 약한 지반은 사건 시점에 칸을 낭떠러지로 바꿉니다(약한 지반 설계 WG6). 배치는 이 Presenter의 Mapper를 씁니다.
            Action<MovementTileFact> showTerrainChange = final => ports.ShowTerrainChange(final, _mapper);
            // 특성 흔적(땅 다지기·먼지 연막)도 사건 시점에 놓거나 지웁니다(특성 시스템 View 설계 TS7). 배치는 이 Presenter의 Mapper를 씁니다.
            Action<TraitTrailFact> showTrail = trail => ports.ShowTrail(trail, _mapper);
            // 특성 발동 연출 줄 보관 하나를 특성 처리기와 대신 맞기 앞 항목이 같이 씁니다(묶음 끝 검토 M2).
            var activationRows = new CombatActivationRows(cueSources);
            _feedback = new CombatTraitFeedbackPlayback(activationRows, _momentCues, piecePlayer, bodyText, _facts, mapper, generation, feedbackEvidence, persistent);
            _positionSwap = new CombatPositionSwapPresentation(activationRows, piecePlayer, _facts, _feedback, generation);
            _movement = new CombatMovementPresentation(_motion, _pacing, _highlight, _waitSlot, _facts, _presentation, mapper, _positionSwap);
            // 대신 맞는 수호자의 행동은 그 줄로 찾고, 뛰기 높이는 걸음 모양 설정, 돌아오기는 순차 재생기의 후속 정리 질문(붙은 뒤 읽음)을 씁니다(유닛 행동 계획 TP-13·TP-21·TP-22).
            var guards = new CombatGuardLead(activationRows, _facts, _presentation, effects, camera, mapper, values.StepLook.HopHeight, () => _settlement, _feedback);
            // 공격 모양 하나의 연출 시작은 공격 처리기와 특성 처리기(추가 공격)가 함께 씁니다(특성 발동 연출 계획 Td.2).
            var attackStart = new CombatAttackStart(_attacks, mapper, _sound, piecePlayer, _presentation, effects, camera, _facts, guards);
            _handlers = CombatEventHandlerTable.Create(attack: new CombatAttackEventHandler(attackStart), roar: new CombatRoarEventHandler(piecePlayer, values.AbilityMotion, _attacks, mapper, _highlight, _facts, _sound, camera, values.CameraMotion), summon: new CombatSummonEventHandler(_facts, piecePlayer, values.AbilityMotion, mapper, _highlight), character: new CombatCharacterEventHandler(units.HudFeedback, _presentation, _reaction, reactionPlayer.CancelUnavailable, mapper, _facts, _sound, piecePlayer, generation, camera, effects, values.FallMotion, ports.Tornado), turn: new CombatTurnEventHandler(), facts: new CombatFactEventHandler(_facts, ports.ConsumeThorn, ports.HideObject, showTerrainChange, showTrail), pull: new CombatPullEventHandler(_motion, _facts, mapper), // 특성 발동 큐는 순간 큐와 같은 곁들임 실행 길로 냅니다(특성 발동 연출 설계 TA3). 큐 값은 순간 큐 내는 곳이 연결 동안 보관합니다.
            // 추가 공격 시작 발동은 줄이 있든 없든 공격 시작으로 다시 칩니다(설계 TA6, 특성 시스템 View 결정 Q-TS1 A).
            // 줄의 유닛 행동은 같은 조각 실행 집합에 유닛 실행으로 엽니다(유닛 행동 설계 AA7, 뛰기 높이는 걸음 모양 설정 — 계획 TP-21).
            // 줄 고르기(방향 포함)·큐·몸 글자는 발동 피드백이 맡습니다(이펙트 끝 규칙 계획 TP-E28, Ee.2).
            trait: new CombatTraitEventHandler(attackStart, _facts, piecePlayer, values.StepLook.HopHeight, _feedback, _positionSwap),
            structure: new CombatStructureEventHandler(_facts.StandingBodyOf, _momentCues.PlayStructureHit,
                (world, amount) => (_structureDamage ?? throw new InvalidOperationException("Structure 숫자 창구가 연결되지 않았습니다.")).Show(world, amount),
                _facts.ApplyStructure),
                tornado: new CombatTornadoEventHandler(ports.Tornado, piecePlayer, mapper, _sound),
                rockfall: new CombatRockfallEventHandler(ports.Rockfall, _momentCues, piecePlayer, effects, camera, _facts, mapper, _sound),
                haboob: new CombatHaboobEventHandler(ports.Haboob, piecePlayer, mapper, _sound));
        }

        public void BeginPresentation(CombatResult result)
        {
            RequireBound();
            RequireStart(CombatStartQuery.Result());
            _facts.ForgetPullArrivals();
            _facts.ForgetFalls();
            var placements = CombatPlacementBatchPlan.Build(result);
            _positionSwap.Prepare(placements);
            _movement.PreparePlacements(placements);
            _feedback.Begin(result);
            _players.BeginResult(result);
            _pacing.BeginResult(result);
        }

        public bool IsPresentationComplete => _players.IsComplete;
        // 최초 외형 생성은 결과보다 먼저 시작될 수 있습니다. 결과 수락과 사건/최종 사실 완료는 같은 재생기의 서로 다른 질의입니다.
        internal bool CanBeginResult => _players.FindBlock(CombatStartQuery.Result()) == CombatStartBlock.None;

        public bool CanBeginPresentationEvent(CombatEvent item) => _players.FindBlock(CombatStartQuery.Start(item)) == CombatStartBlock.None;
        /// <summary>
        /// 순차 재생기의 후속 정리 질문을 받아 둡니다(유닛 행동 계획 TP-13). 재생기가 붙을 때 재생 조정을 거쳐 옵니다. 대신 맞은 수호자의 돌아오기가 읽습니다.
        /// </summary>
        internal void BindSettlement(ICombatSourceSettlement settlement) => _settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
        /// <summary>Field.Build가 성공한 뒤 현재 Mapper만 연결합니다. 생성 시에는 연결하지 않습니다.</summary>
        internal void Bind(FieldGridMapper mapper)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            Clear();
            _mapper = mapper;
        }

        /// <summary>
        /// 전투 설치 때 한 번, 처음 쓸 때 만들던 투사체와 몸 위 글자를 미리 만듭니다(GC 끊김 설계 GH6, 계획 TP-6).
        /// 투사체는 이 키들의 공격 연출 값마다 한 발, 글자는 특성 발동 줄이 쓰는 프리팹마다 글자 창구가 정한 수입니다.
        /// 특성 추가 공격의 공격 표시 덮어쓰기 투사체는 넣지 않습니다(설계 GH6 "넣지 않는 것"). 실패하면 그대로 던집니다(계획 TP-7).
        /// </summary>
        /// <param name="characterKeys">미리 만들 캐릭터 키입니다. 어떤 캐릭터를 셀지는 부르는 쪽(설치)이 정합니다.</param>
        internal void Prewarm(IReadOnlyCollection<EntityKey> characterKeys)
        {
            if (characterKeys == null)
                throw new ArgumentNullException(nameof(characterKeys));
            foreach (EntityKey key in characterKeys)
            {
                var attack = _presentation.AttackSettingsOf(key);
                if (attack != null)
                    _attacks.Prewarm(attack);
            }
            _bodyText?.Prewarm(_cueSources.BodyTextPrefabs(_bodyText.DefaultPrefab));
        }

        internal void Refresh(IReadOnlyList<CharacterData> characters, IReadOnlyList<StructureCombatFacts> structures) => Refresh((characters ?? throw new ArgumentNullException(nameof(characters))).Select(c => c.ToFacts()).ToArray(), structures);
        /// <summary>Buffer를 바꾸지 않고 표시용 사실 목록을 동기화합니다.</summary>
        internal void Refresh(IReadOnlyList<CharacterCombatFacts> characters, IReadOnlyList<StructureCombatFacts> structures)
        {
            RequireBound();
            if (_players.BlocksFinalFacts)
                throw new InvalidOperationException("[CombatSceneEventPresenter] 공격 완료 전에 최종 사실을 표시할 수 없습니다.");
            // 재생 강조는 결과 하나 안에서만 보입니다. 최종 사실을 그릴 때 지웁니다.
            _highlight.Clear();
            _facts.Replace(characters, structures);
            // 유지 이펙트도 이 그리기에서 최종 사실로 맞춥니다(이펙트 끝 규칙 설계 EL4 — 옛 결과 끝 다시 내기를 대신함).
            _facts.Render();
            // 행동 끝 큐로 낸 이펙트 중 남은 것(완료가 오지 않은 행동·대신 맞기 길)을 거둡니다(계획 TP-E4).
            _effects.RecallActionBound();
        }

        /// <summary>진입 준비/취소의 상시 이펙트 보류만 바꿉니다. 일회성 큐는 억제하지 않습니다.</summary>
        internal void SetEntryEffectsDeferred(bool deferred) => _facts.SetEntryEffectsDeferred(deferred);

        /// <summary>진입 유닛 등장 완료 뒤 상시 이펙트만 표시합니다.</summary>
        internal void RevealEntryEffects()
        {
            RequireBound();
            _facts.RevealEntryEffects();
        }

        internal void Clear()
        {
            try
            {
                CancelMovement();
            }
            finally
            {
                _mapper = null;
                _attackModeTargets.Clear();
                _facts.Clear();
            }
        }

        /// <summary>
        /// 공격 모드 대상(즉시 처치 대상)을 바꾸고 유지 이펙트를 맞춥니다(마무리 사격 FS-b, 계획 Ed.3). 무엇을 낼지는 특성 외형 상시 목록이 정합니다.
        /// 연결 전(Mapper 없음)에는 값만 바꾸고, 다음 그리기에서 맞춥니다.
        /// </summary>
        internal void ShowAttackModeTargets(EntityId actor, ReadOnlyMemory<EntityId> targets)
        {
            if (_attackModeTargets.Show(actor, targets) && _mapper != null)
                _facts.SyncPersistent();
        }

        /// <summary>공격 모드 대상을 지우고 유지 이펙트를 맞춥니다. 여러 번 불려도 안전합니다.</summary>
        internal void ClearAttackModeTargets()
        {
            if (_attackModeTargets.Clear() && _mapper != null)
                _facts.SyncPersistent();
        }

        internal void Release()
        {
            _facts.BindRendered(null);
            List<Exception> errors = null;
            CleanupErrors.Try(() => _structureDamage?.Release(), ref errors);
            CleanupErrors.Try(Clear, ref errors);
            _players.ReleaseAll(ref errors);
            CleanupErrors.Try(_effects.Release, ref errors);
            CleanupErrors.ThrowIfAny(errors, CleanupFailureMessage);
        }

        /// <summary>걸음 하나를 그 유닛의 레인에서 시작합니다. <paramref name = "completed"/>는 걸음 사건 완료 통지입니다(동시 처리 설계 C6).</summary>
        public void BeginStep(CombatCharacterEvent step, Action completed)
        {
            RequireBound();
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            // 넉백 같은 이동 후속은 히트 뒤 남은 공격·포효 연출과 겹쳐 시작할 수 있습니다(설계 §6 Q2 재결정).
            RequireIdleFor(step);
            _movement.BeginStep(step, completed);
        }

        public void BeginMovement(CombatCharacterEvent firstStep)
        {
            RequireBound();
            _movement.BeginMovement(firstStep);
        }

        public void BeginMovementEnd(CombatCharacterEvent lastStep, Action completed)
        {
            RequireBound();
            if (lastStep == null)
                throw new ArgumentNullException(nameof(lastStep));
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            // 접촉으로 사건 완료가 일찍 온 마지막 걸음은 아직 보간 중이라 남은 연출 대기를 보지 않습니다(이동 접촉 설계 M3).
            bool interpolating = _motion.IsInterpolating(lastStep);
            if (!interpolating)
                RequireIdleFor(lastStep);
            _movement.EndMovement(lastStep, interpolating, completed);
        }

        public void ShowReturn(CombatCharacterEvent returned)
        {
            RequireBound();
            if (returned == null)
                throw new ArgumentNullException(nameof(returned));
            RequireIdleFor(returned);
            _movement.ShowReturn(returned);
        }

        /// <summary>
        /// 한 프레임 진행합니다. 순서(공격 → 조각 실행 → 걸음·대기 → 반응)와 멈춤 규칙은 실행 목록과 각 재생기가 정합니다(연출 조립 설계 B12·B14).
        /// </summary>
        public void TickPresentation(float deltaTime) => _players.Tick(new CombatPlaybackFrame(deltaTime, _mapper != null));
        public void CancelMovement()
        {
            _presentationVersion++;
            _movement.CancelPlacements();
            _feedback.Clear();
            _facts.ForgetPullArrivals();
            _facts.ForgetFalls();
            // 한 묶음으로 정리합니다(결정 Q-R7 A). 한 정리가 실패해도 나머지를 계속하고 끝에 모아 던집니다.
            List<Exception> errors = null;
            CleanupErrors.Try(_positionSwap.Clear, ref errors);
            CleanupErrors.Try(() => _structureDamage?.CancelAll(), ref errors);
            _players.CancelAll(ref errors);
            CleanupErrors.Try(_pacing.Clear, ref errors);
            CleanupErrors.Try(() => _presentation.ClearAllFacing(_facts.Characters, _mapper), ref errors);
            CleanupErrors.Try(_highlight.Clear, ref errors);
            // 남은 연출 이펙트를 회수합니다(결정 Q-E4 A). 정상 완료에는 불리지 않으므로 끝난 공격의 이펙트는 자연히 끝납니다.
            CleanupErrors.Try(_effects.StopAll, ref errors);
            CleanupErrors.ThrowIfAny(errors, CleanupFailureMessage);
        }

        public void CancelPresentation() => CancelMovement();
        /// <summary>일반 사건 표시를 우회하는 전용 재생 경로에서 사건 소리만 냅니다.</summary>
        internal void PlayEventSound(CombatEvent e) => _sound.Play(e);

        public void BeginEvent(CombatEvent e, Action completed)
        {
            RequireBound();
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            // 스케줄러의 질의(CanBeginPresentationEvent)와 같은 판단을 한 번만 확인합니다(연출 조립 설계 §9 I-1, 결정 Q-R14 A).
            CombatStartBlock block = _players.FindBlock(CombatStartQuery.Start(e));
            if (block != CombatStartBlock.None)
                throw new InvalidOperationException($"[CombatSceneEventPresenter] 접촉 또는 남은 공격 표시를 먼저 기다려야 합니다. 막는 이유: {block}");
            CombatEventHandler handler = _handlers.For(e);
            // 행위자는 사건 형식 표가 원본입니다(View 2라운드 W7-1). 간격 계산은 그 ID와 표시 중인 사실의 Owner를 받습니다(구조 원칙 평가 R3).
            EntityId actor = CombatEventTypeTable.ActorOf(e);
            float gap = _pacing.BeforeEvent(e, actor, FindCharacter(actor)?.Owner);
            if (gap == 0f)
            {
                PresentEvent(e, completed);
                return;
            }

            _waitSlot.StartGap(gap);
            bool shownBeforeGap = handler.ShowsBeforeGap(e);
            _waitSlot.HoldAfterGap(e, completed, shownBeforeGap);
            if (shownBeforeGap)
                PresentEvent(e, null);
        }

        /// <summary>다른 대상의 사망만 겹쳐 재생합니다. 일반 사건의 직렬 대기는 그대로 유지합니다.</summary>
        internal void BeginConcurrentDeath(CombatCharacterEvent death, Action completed)
        {
            RequireBound();
            if (death == null)
                throw new ArgumentNullException(nameof(death));
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            if (death.Kind != CombatEventKind.Died || death.Expected.Health.IsDead || !death.Final.Health.IsDead || death.Expected.EntityId != death.Final.EntityId || FindCharacter(death.Final.EntityId)?.Health.IsDead == true)
                throw new InvalidOperationException("동시 사망은 아직 표시하지 않은 생존 → 사망 사건만 받습니다.");
            RequireStart(CombatStartQuery.ConcurrentDeaths());
            PresentEvent(death, completed);
        }

        /// <summary>
        /// 사건 하나 안의 순서를 정하는 틀입니다(Presenter 분리 계획 §5.1, 설계 §1.4 보존). 형식별 지식은 처리기가 단계마다 답합니다.
        /// 강조 → 사실 전 시작(공격·포효·사망·피격) → 소리 → 순간 큐 → 사실 반영 → 다시 그리기 → 사실 뒤 시작(소환 등장) → 머묾 또는 완료.
        /// </summary>
        private void PresentEvent(CombatEvent e, Action completed)
        {
            using var sample = PresentEventMarker.Auto();
            _motion.BeforeEvent(e);
            e = _facts.ForPresentation(e);
            int version = _presentationVersion;
            CombatEventHandler handler = _handlers.For(e);
            // 새 행동이 시작되면 앞 행동의 재생 강조를 지우고, 포효·소환이면 그 칸을 새로 보인다(보스 활성화 설계 D6·D7).
            if (CombatEventTypeTable.IsActionStart(e))
                _highlight.Clear();
            _highlight.Show(e, handler);
            // 사실 반영 전에 연출을 시작했으면 사건 완료는 그 연출이 알린다. 포효 피해·넉백은 히트부터 작아지기와 겹쳐 이어진다(히트 타이밍 §6 Q1).
            bool begunBeforeFacts = handler.TryBeginBeforeFacts(e, completed, version);
            // 위치 교환 승객의 미뤄 둔 배치를 위치 이동 행동이 가져가지 않았으면 여기서 바로 반영합니다(안전장치, 이펙트 끝 규칙 계획 TP-E7 ③).
            // 부르는 곳은 이곳 하나입니다. 교환 쌍 뒤 처음 표시하는 사건이 그 발동이라, 줄·행동이 없을 때 승객이 발동 순간 새 자리에 섭니다.
            _facts.ReleaseUnclaimedPlacements();
            if (begunBeforeFacts)
            {
                if (version == _presentationVersion) _motion.ResumeLeaps(_mapper);
                return;
            }
            _sound.Play(e);
            if (version != _presentationVersion)
                return;
            // 사실 사건의 순간 큐(이펙트·흔들림, 화약통·지뢰 폭발 포함)는 사건 소리와 같은 순간에 시작하고 사건을 막지 않습니다
            // (이펙트 창구 확장 설계 EC6·EC7·EC9). 주체 몸 자리는 사실 반영 전 보인 사실로 구합니다.
            _momentCues.Play(e);
            if (version != _presentationVersion)
                return;
            // 행동 대기가 끝난 뒤 사건을 반영하며 중간 체력·사망 시점은 최신 Buffer가 아닌 사건 스냅샷을 따른다.
            handler.ApplyFacts(e);
            if (version != _presentationVersion) return;
            _motion.ResumeLeaps(_mapper);
            _facts.Render();
            if (version == _presentationVersion)
                _feedback.AfterFacts(e);
            if (version != _presentationVersion)
                return;
            // 사실 반영 뒤에 시작하는 연출(소환 등장)은 끝날 때 완료를 알린다.
            if (handler.TryBeginAfterFacts(e, completed, version))
                return;
            // 동작을 시작하지 못한 포효·소환(유닛 표시 없음 등)은 강조를 보인 채 행동 간격만큼 머문 뒤 완료한다.
            float hold = completed != null ? _pacing.HoldAfter(e) : 0f;
            if (hold > 0f)
            {
                _waitSlot.HoldShown(e, completed, hold);
                return;
            }

            completed?.Invoke();
        }

        /// <summary>표시 정리 실패 문구입니다. 사건 Presenter와 그 처리기가 함께 씁니다.</summary>
        internal const string CleanupFailureMessage = "[CombatSceneEventPresenter] 표시 정리에 실패했습니다.";
        private const string BusyMessage = "[CombatSceneEventPresenter] 이전 사건 연출이 아직 끝나지 않았습니다.";
        /// <summary>
        /// 시작 단정입니다(연출 조립 설계 B11). 막는 이유가 있으면 지금 문구에 그 이유를 붙여 던집니다(결정 Q-R8 A). 사건 시작은 자기 문구로 같은 판단을 씁니다.
        /// </summary>
        private void RequireStart(in CombatStartQuery query)
        {
            CombatStartBlock block = _players.FindBlock(query);
            if (block != CombatStartBlock.None)
                throw new InvalidOperationException($"{BusyMessage} 막는 이유: {block}");
        }

        /// <summary>
        /// 이동·복귀 표시를 시작하기 전 조건입니다. 스케줄러의 질의(<see cref = "CanBeginPresentationEvent"/>)와 같은 시작 판단 하나를 단정합니다(연출 조립 설계 §9 I-1).
        /// 남은 공격·포효 연출과의 겹침은 공격 Gate가 허락한 사건만입니다(넉백 등 이동 후속, 설계 §6 Q2).
        /// 같은 유닛의 남은 걸음 연출 중 이동은 그 레인의 걸음 Gate 규칙 1이 막고, 다른 유닛의 걸음과는 겹칠 수 있습니다(이동 접촉 설계 M4, 동시 처리 설계 C5).
        /// </summary>
        private void RequireIdleFor(CombatCharacterEvent movement)
        {
            RequireStart(CombatStartQuery.Start(movement));
        }

        private CharacterCombatFacts FindCharacter(EntityId id) => _facts.FindCharacter(id);
        private void RequireBound()
        {
            if (_mapper == null)
                throw new InvalidOperationException("[CombatSceneEventPresenter] 필드 구성 성공 후 Mapper 연결이 필요합니다.");
        }
    }
}
