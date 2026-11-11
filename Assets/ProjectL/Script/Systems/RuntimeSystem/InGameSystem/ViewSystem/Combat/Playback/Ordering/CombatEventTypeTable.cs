using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>결과 재생에서 사건 하나가 보이는 길입니다(연출 조립 설계 §9 I-3).</summary>
    internal enum CombatEventRoute
    {
        /// <summary>표시 계약이 없어 경고 후 사실 반영 없이 건너뜁니다(리팩토링 설계 D1). 최종 상태는 결과 끝 표시 갱신이 맞춥니다.</summary>
        Skip,

        /// <summary>이동 한 걸음입니다. 유닛별 걸음 레인이 보입니다.</summary>
        Step,

        /// <summary>필드 안에서의 체크포인트 복귀입니다. 표시 회전을 지우고 복귀 사실을 바로 보입니다.</summary>
        Return,

        /// <summary>기차 발동입니다. 기차 전용 재생(접촉 순 사망)이 보입니다(동시 처리 설계 Q-C3 A).</summary>
        Railway,

        /// <summary>사건 틀입니다. 사건 Presenter가 형식별 처리기로 보입니다(Presenter 분리 설계 S3).</summary>
        Event,
    }

    /// <summary>사건 틀 형식을 맡는 처리기 칸입니다(형식 표의 열, View 2라운드 W7-1). 처리기 표가 이 칸으로 처리기를 고릅니다.</summary>
    internal enum CombatEventHandlerSlot
    {
        /// <summary>연출 없이 사실만 반영합니다(사실 처리기).</summary>
        Facts = 0,
        Structure,
        Attack,
        Roar,
        Summon,
        Character,
        Turn,
        /// <summary>끌어오기입니다. 대상의 실제 경로를 그 유닛의 이동 레인에서 보입니다.</summary>
        Pull,
        /// <summary>특성 발동입니다. 특성 외형 표의 발동 연출 줄로 보입니다(특성 발동 연출 설계 TA1).</summary>
        Trait,
        /// <summary>움직이는 토네이도입니다. 몸 이동과 방향 화살표를 보입니다(토네이도 View 설계 TN1).</summary>
        Tornado,
        /// <summary>낙석입니다. 터널 폭발·경고 칸·돌 낙하를 보입니다(낙석 View 설계 RF1).</summary>
        Rockfall,
        /// <summary>하붑입니다. 구름 칸마다 연기를 맞춥니다(하붑 View 계획 Hb.5).</summary>
        Haboob,
        /// <summary>적 본부 증원의 생성 시점 사실을 보입니다.</summary>
        Reinforcement,
    }

    /// <summary>적 연출 간격의 종류입니다(형식 표의 열). 간격 값과 상태 기계는 <c>CombatEnemyPresentationPacing</c>이 맡습니다.</summary>
    internal enum CombatEventPacing
    {
        /// <summary>간격이 없습니다(실패한 공격 포함).</summary>
        None = 0,

        /// <summary>성공한 공격입니다. 행위자의 행동 하나이고, 뒤따르는 이동은 자발적 이동이 아닙니다.</summary>
        Attack,

        /// <summary>공격과 같은 행동 경계(포효)입니다. 뒤따르는 넉백 이동은 자발적 이동이 아닙니다(보스 활성화 설계 D6·D8).</summary>
        AttackLike,

        /// <summary>행위자의 행동 하나(소환)입니다(보스 활성화 설계 D7).</summary>
        Action,

        /// <summary>턴 사건입니다. 턴 안의 갈래(턴 끝·턴 시작·전투 진입·밤 끝)는 사건 종류(Core 값)로 가릅니다.</summary>
        Turn,
    }

    /// <summary>
    /// "이 사건 형식은 무엇인가"의 원본입니다(View 2라운드 W7-1, 옛 사건 경로 표를 넓힘). 한 줄 = 사건 형식 하나의 사실 지식이고,
    /// 경로·처리기 칸·행동 시작·출처를 여는 사건·행위자·주체·피해 근원·공격 출처·이동 사건·간격 종류·머묾·사건 순간을 모두 이 표가 답합니다.
    /// 사건 형식과 사건 값만 보고 답하므로 표시 상태와 무관합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 열쇠는 사건 형식입니다. 모든 사건 형식은 <c>sealed</c>라 형식 같음 비교가 <c>is</c> 검사와 같습니다(표를 만들 때 확인).
    /// 값에 따라 갈리는 것(캐릭터 사건의 걸음·복귀 경로, 기믹의 기차 경로, 턴 사건 종류별 전환, 공격 성공 여부)은 그 줄의 함수가 답합니다.
    /// </para>
    /// <para>
    /// 사건을 묻는 질문은 null 사건과 표에 없는 형식에 예외 없이 "없음"으로 답합니다(경로는 건너뜀). 형식으로 묻는 <see cref="HandlerSlotOf"/>만
    /// 표에 없는 형식에 예외입니다(처리기 표는 표의 형식만 돕니다). 사실만 반영하는 새 형식은 표에 한 줄을 더하고,
    /// 연출이 있는 새 형식은 처리기 칸을 하나 더해 처리기 표(<c>CombatEventHandlerTable</c>)에 처리기를 맞춥니다.
    /// </para>
    /// <para>
    /// 표에 없는 사건 종류 지식: 근원 여럿을 열쇠 하나로 합치는 규칙은 <see cref="CombatEventSubjects"/>, 간격 상태 기계는
    /// <c>CombatEnemyPresentationPacing</c>, 후속 반응을 모으는 상태는 <c>CombatHitFollowUps</c>가 갖습니다.
    /// </para>
    /// </remarks>
    internal static class CombatEventTypeTable
    {
        // 표 순서는 사건 틀 형식 목록(EventTypes)의 순서입니다.
        private static readonly TypeRow[] RowArray =
        {
            // 연출이 있는 형식입니다. 처리기 표가 형식별 처리기를 맞춥니다.
            Row<CombatAttackEvent>(CombatEventHandlerSlot.Attack, isActionStart: true,
                actor: e => e.Attack.AttackerId,
                attackSource: e => CombatAttackSource.Of(e.Attack.AttackerId, e.Attack),
                pacing: e => e.Attack.Succeeded ? CombatEventPacing.Attack : CombatEventPacing.None),
            Row<CombatRoarEvent>(CombatEventHandlerSlot.Roar, isActionStart: true,
                actor: e => e.BossId,
                // 대상 없는 포효는 출처가 아닙니다.
                attackSource: e => e.Attack.HasValue ? CombatAttackSource.Of(e.BossId, e.Attack.Value) : (CombatAttackSource?)null,
                pacing: _ => CombatEventPacing.AttackLike, holdsWhenShownOnly: true),
            Row<CombatSummonEvent>(CombatEventHandlerSlot.Summon, isActionStart: true,
                actor: e => e.Record.SummonerId,
                pacing: _ => CombatEventPacing.Action, holdsWhenShownOnly: true),
            Row<CombatReinforcementEvent>(CombatEventHandlerSlot.Reinforcement,
                subject: e => e.Created.EntityId),
            Row<CombatCharacterEvent>(CombatEventHandlerSlot.Character,
                route: CharacterRoute,
                subject: e => e.Final?.EntityId ?? e.Expected?.EntityId ?? EntityId.Empty,
                origins: e => e.DamageOrigins,
                isMovement: e => e.Kind == CombatEventKind.Moved || e.Kind == CombatEventKind.Returned
                    || e.Kind == CombatEventKind.Relocated),
            Row<CombatTurnEvent>(CombatEventHandlerSlot.Turn, isActionStart: true,
                actor: e => e.ActorId,
                pacing: _ => CombatEventPacing.Turn),
            // 끌어오기는 Core의 실제 경로만 대상의 이동 레인에서 보입니다(Core/View 인계 2026-10-26). 피해 근원이 없어 동시 묶음에 들지 않고
            // 뒤따르는 지형·피격·체력 사건의 장벽이 됩니다. 같은 걸음의 일반 Moved 사건은 Core가 내지 않습니다.
            Row<CombatPullEvent>(CombatEventHandlerSlot.Pull,
                subject: e => e.Movement.TargetId),
            // 특성 발동은 특성 처리기가 발동 연출 줄로 보입니다(특성 발동 연출 설계 TA1). 주체·근원 규칙은 특성 시스템 View 계획 TP-4·TP-3입니다.
            // 발동 모두에 결과 근원이 있으면 같은 순간의 피격 묶음에 들고(설계 TS3), 하나라도 비면 지금처럼 묶음 밖입니다.
            // 직군 발동(Categories)은 보이지 않으므로 주체·근원에 넣지 않습니다.
            // 추가 공격 시작 발동(추가 공격 결과가 있음)은 공격 연출을 다시 열어 출처를 여는 사건이고, 그 공격의 출처 사실도 줍니다(결정 Q-TD1 A,
            // 특성 시스템 View 계획 TP-1). 새 행동 시작은 아닙니다(설계 TA6).
            Row<CombatEffectsEvent>(CombatEventHandlerSlot.Trait,
                subject: TraitSubjectOf,
                origins: TraitOriginsOf,
                attackSource: ChildAttackSourceOf,
                opensAttackSource: HasChildAttackStart),
            // 연출 없이 사실만 반영하는 형식입니다(사실 처리기). 낙하도 사실만 반영한다(리팩토링 설계 D2).
            Row<CombatStructureEvent>(CombatEventHandlerSlot.Structure,
                subject: e => e.Final?.EntityId ?? e.Expected?.EntityId ?? EntityId.Empty,
                origins: e => e.DamageOrigins),
            Row<CombatSoulEvent>(CombatEventHandlerSlot.Facts),
            Row<CombatArtifactEvent>(CombatEventHandlerSlot.Facts),
            Row<CombatGimmickEvent>(CombatEventHandlerSlot.Facts,
                route: e => e.Activation is RailwayGimmickActivationSnapshot ? CombatEventRoute.Railway : CombatEventRoute.Event),
            Row<CombatOutcomeEvent>(CombatEventHandlerSlot.Facts),
            // 순간 칸(이펙트 창구 확장 설계 EC7, 계획 P15): 소리 표와 큐가 함께 읽습니다. 어느 갈래가 순간인지는 이 표에만 둡니다.
            // 정적 지형 바뀜은 약한 지반 붕괴만 순간입니다. 진입 기록·다른 종류 바뀜에는 소리·큐가 없습니다(갈래 판정 CombatTerrainChanges).
            Row<CombatMovementSourceEvent>(CombatEventHandlerSlot.Facts,
                moment: e => CombatTerrainChanges.IsWeakGroundCollapse(e, out CellCoord cell)
                    ? CombatEventMoment.AtCell(CombatMomentKind.WeakGroundCollapse, cell)
                    : (CombatEventMoment?)null),
            Row<CombatThornConsumedEvent>(CombatEventHandlerSlot.Facts),
            // 장애물 칸은 피해 근원의 물리 칸(= 장애물 앵커)입니다. 비면 칸 없는 순간이라 소리만 납니다.
            Row<CombatObstacleReactionEvent>(CombatEventHandlerSlot.Facts,
                subject: e => e.ObstacleId,
                origins: e => e.Origin != null ? new[] { e.Origin } : null,
                moment: e => CombatEventMoment.ForObstacle(e.Reaction, e.ContentKind, e.Origin?.PhysicalCell)),
            Row<CombatHeldEnvironmentEvent>(CombatEventHandlerSlot.Facts,
                subject: e => e.CharacterId,
                origins: e => e.Origin != null ? new[] { e.Origin } : null),
            // 오브젝트는 발동(지뢰)·습득(깃발·보안관 배지)이 같은 사건이고 같은 칸입니다(소리 창구 2차 결정 Q-ES2″).
            Row<CombatObjectActivationEvent>(CombatEventHandlerSlot.Facts,
                moment: e => CombatEventMoment.ForObject(CombatMomentKind.ObjectActivated, EnvironmentVisualKinds.ObjectKind(e.Source), e.Source.Cell)),
            Row<CombatObjectCreatedEvent>(CombatEventHandlerSlot.Facts,
                moment: e => CombatEventMoment.ForObject(CombatMomentKind.ObjectCreated, EnvironmentVisualKinds.ObjectKind(e.Source), e.Source.Cell)),
            // 움직이는 토네이도 사건입니다. 주체·근원이 없어 묶음 밖 장벽이고 Core 순서대로 재생합니다(인계 §1.2·§4.4).
            // 날아간 유닛이 공중에 있어도 폭풍은 계속 갑니다(토네이도 View 설계 TN10).
            Row<CombatTornadoGimmickEvent>(CombatEventHandlerSlot.Tornado, overlapsUnitRuns: true),
            Row<CombatTornadoCenterEvent>(CombatEventHandlerSlot.Tornado, overlapsUnitRuns: true),
            Row<CombatTornadoEntryEvent>(CombatEventHandlerSlot.Tornado, overlapsUnitRuns: true),
            Row<CombatTornadoMemberEvent>(CombatEventHandlerSlot.Tornado, overlapsUnitRuns: true),
            // 낙석 사건입니다. 주체·근원이 없어 묶음 밖 장벽이라, 공중의 유닛이 착지한 뒤 시작하고 돌이 체력·상태 사건보다 먼저 보입니다(낙석 View 설계 §3.4).
            Row<CombatRockfallEvent>(CombatEventHandlerSlot.Rockfall),
            // 하붑 사건입니다. 주체·근원이 없어 묶음 밖 장벽이라 공중의 토네이도 유닛이 착지한 뒤 구름이 옮겨집니다. 토네이도 → 철도 → 하붑 → 낙석의 Core 순서를 지킵니다(하붑 View 계획 HP-7).
            Row<CombatHaboobEvent>(CombatEventHandlerSlot.Haboob),
            // 떨어지기 시작하는 순간입니다. 유닛과 구조물(장애물) 모두 이 사건이 납니다(소리 창구 2차 검토 L2).
            Row<CombatFallEvent>(CombatEventHandlerSlot.Facts,
                moment: e => CombatEventMoment.AtCell(CombatMomentKind.Fall, e.Cell)),
            // 아이템 장착·발동·버림은 소지자의 장착 칸만 사건 순간에 바꿉니다(가방 설계 P7). 주체는 소지자입니다.
            // 순간은 쓰인(소모된) 때뿐입니다. 판정은 Core 공개 속성 Activated 하나이고 중복 효과도 같습니다(소리 창구 2차 검토 L1).
            Row<CombatItemEvent>(CombatEventHandlerSlot.Facts,
                subject: e => e.HolderId,
                moment: e => e.Activated ? CombatEventMoment.ForItem(e.ItemKey, e.HolderId) : (CombatEventMoment?)null),
            // 사냥꾼의 표식은 대상의 표식 디버프 사실만 바꿉니다(Final이 null이면 소모). 주체는 표식 대상입니다.
            Row<CombatHuntersMarkEvent>(CombatEventHandlerSlot.Facts,
                subject: e => e.TargetId),
            // 걸린 상태(디버프·공격력 부여·보호막 등)가 바뀐 순간입니다(특성 시스템 View 설계 TS1). 사실 처리기가 그 유닛 사실을 사건 값으로 바꿉니다.
            // 근원은 피해·처치 길에서만 있고(보호막 흡수 → 상태 → 체력이 같은 순간), 턴 끝 만료·정리 길에서는 비어 묶음 밖입니다.
            Row<CombatTraitStateEvent>(CombatEventHandlerSlot.Facts,
                subject: e => e.Final.EntityId,
                origins: e => e.Origin != null ? new[] { e.Origin } : null),
        };

        private static readonly Type[] EventTypeArray = TypesOf(RowArray);

        private static readonly Dictionary<Type, TypeRow> Rows = Index(RowArray);

        /// <summary>사건 틀로 보이는 형식입니다(표 순서).</summary>
        internal static IReadOnlyList<Type> EventTypes => EventTypeArray;

        /// <summary>그 형식이 사건 틀로 보이는 형식(표에 있는 형식)인지입니다.</summary>
        internal static bool IsEventType(Type type) => type != null && Rows.ContainsKey(type);

        /// <summary>
        /// 사건이 보이는 길입니다. 표에 없거나 null이면 건너뜀, 그 줄의 경로 함수가 있으면 그 답, 없으면 사건 틀입니다.
        /// 순차 재생이 재생 첫머리에 모든 사건에 부르므로 사건 값만 봅니다.
        /// </summary>
        internal static CombatEventRoute RouteOf(CombatEvent e)
        {
            TypeRow row = RowOf(e);
            if (row == null)
                return CombatEventRoute.Skip;
            return row.Route != null ? row.Route(e) : CombatEventRoute.Event;
        }

        /// <summary>기차(레일웨이) 사건인지입니다. 재생 조정과 소리 표가 함께 씁니다(View 3라운드 W12-4).</summary>
        internal static bool IsRailway(CombatEvent e) => RouteOf(e) == CombatEventRoute.Railway;

        /// <summary>
        /// "새 행동 시작"인 사건(공격·포효·소환·턴)인지입니다(히트 타이밍 설계 H9a). 피해·사망·넉백 같은 앞 행동의 결과 사건은 아닙니다.
        /// 공격 대기·후속 반응 끊김·걸음 구간·걸음 접촉 Gate·주체 막기·재생 강조 끄기·이동 경로 끝·동시 묶음이 이 분류 하나를 같이 씁니다.
        /// </summary>
        internal static bool IsActionStart(CombatEvent e) => RowOf(e)?.IsActionStart ?? false;

        /// <summary>행동 간격을 고를 행위자입니다(공격자·보스·소환자·턴 행위자). 없으면 <see cref="EntityId.Empty"/>입니다.</summary>
        internal static EntityId ActorOf(CombatEvent e)
        {
            TypeRow row = RowOf(e);
            return row?.Actor != null ? row.Actor(e) : EntityId.Empty;
        }

        /// <summary>
        /// 그 사건이 표시 자원을 쓰는 유닛(주체)입니다(동시 처리 설계 C1). 캐릭터 사건 = 그 캐릭터, 구조물 사건 = 그 구조물,
        /// 보유 효과 = 그 캐릭터, 장애물 반응 = 그 장애물, 아이템 사건 = 소지자, 특성 발동 = 대상 하나 또는 발동 유닛(특성 시스템 View 계획 TP-4),
        /// 걸린 상태 바뀜 = 그 유닛입니다. 고른 값이 비었으면 false입니다.
        /// </summary>
        internal static bool TryGetSubject(CombatEvent e, out EntityId subject)
        {
            TypeRow row = RowOf(e);
            subject = row?.Subject != null ? row.Subject(e) : EntityId.Empty;
            return !subject.IsEmpty;
        }

        /// <summary>
        /// 피해 근원을 가질 수 있는 사건(피해·사망 캐릭터 사건, 구조물 사건, 보유 효과, 장애물 반응, 특성 발동, 걸린 상태 바뀜)의 근원 목록입니다.
        /// 목록이 null이거나 비면 false입니다(동시 묶음의 회복 사건 붙이기가 이 규칙에 기댑니다).
        /// </summary>
        internal static bool TryGetOrigins(CombatEvent e, out IReadOnlyList<CombatDamageOrigin> origins)
        {
            TypeRow row = RowOf(e);
            origins = row?.Origins != null ? row.Origins(e) : null;
            return origins != null && origins.Count > 0;
        }

        /// <summary>
        /// 후속 반응의 출처가 될 수 있는 사건이면 그 출처 사실을 줍니다(히트 타이밍 설계 H2, 특성 시스템 View 계획 TP-1).
        /// 공격 사건, 반응 공격 결과가 있는 포효, 추가 공격 시작(특성 발동 사건, <see cref="IsChildAttackStart"/>)입니다.
        /// </summary>
        internal static bool TryGetAttackSource(CombatEvent e, out CombatAttackSource source)
        {
            CombatAttackSource? value = RowOf(e)?.AttackSource?.Invoke(e);
            source = value ?? default;
            return value.HasValue;
        }

        /// <summary>
        /// 처리기가 공격 연출(과 그 출처 Gate)을 새로 열 수 있는 사건인지입니다(특성 발동 연출 결정 Q-TD1 A). 지금은 추가 공격 시작 발동 사건입니다(특성 시스템 View 계획 Tb.3).
        /// 출처와 행동 실행은 하나씩이라 공격 Gate가 이 사건을 이전 출처의 전체 종료까지 기다리게 하고, 주체 막기도 행동 사건처럼 모든 유닛 연출을 기다리게 합니다
        /// (행동 실행이 히트 전인 동안 걸음·반응이 멈추므로, 묶음 끝 검토 M1). 새 행동 시작(<see cref="IsActionStart"/>)과 달리
        /// 강조 끄기·동시 묶음·이동 경로·간격은 바꾸지 않습니다. View 연출 줄이 없어도 Core 사실만 보고 답합니다.
        /// </summary>
        internal static bool OpensAttackSource(CombatEvent e)
        {
            TypeRow row = RowOf(e);
            return row?.OpensAttackSource != null && row.OpensAttackSource(e);
        }

        /// <summary>
        /// 특성 발동 하나가 추가 공격 시작(처치 연쇄·2연발·연쇄 폭발·행운의 탄환)인지입니다. 판정은 Core 사실의 추가 공격 결과(<c>ChildAttack</c>)가 있는지
        /// 하나이고, 발동 종류 이름이나 대상 수는 보지 않습니다(특성 시스템 View 설계 TS4, 결정 Q-TS2 A). 이 사건이 출처를 여는지(<see cref="OpensAttackSource"/>)·
        /// 출처 사실(<see cref="TryGetAttackSource"/>)·특성 처리기의 공격 시작이 이 한 곳을 부릅니다(어긋나면 출처를 열지 않은 사건이 공격을 쳐 Gate가 멈춤).
        /// View 연출 줄은 보지 않습니다(줄이 없어도 공격을 그림, 결정 Q-TS1 A).
        /// </summary>
        internal static bool IsChildAttackStart(TraitActivationSnapshot trait) => trait?.ChildAttack != null;

        /// <summary>
        /// 이동 표시 사건(<c>Moved</c>·<c>Returned</c>·<c>Relocated</c>)인지입니다. 이동 후속 대응·공격 Gate 규칙 1·걸음 구간 해석·걸음 접촉 Gate·같은 순간 묶음이 같이 씁니다.
        /// </summary>
        internal static bool IsMovement(CombatEvent e)
        {
            TypeRow row = RowOf(e);
            return row?.IsMovement != null && row.IsMovement(e);
        }

        /// <summary>적 연출 간격의 종류입니다. 없으면 <see cref="CombatEventPacing.None"/>입니다.</summary>
        internal static CombatEventPacing PacingOf(CombatEvent e)
        {
            TypeRow row = RowOf(e);
            return row?.Pacing != null ? row.Pacing(e) : CombatEventPacing.None;
        }

        /// <summary>
        /// 동작(등장·커지기·공격 연출)을 시작하지 못해 사실만 보였을 때 완료 전까지 머무는 사건(포효·소환)인지입니다(보스 활성화 설계 D6·D7·D6b).
        /// </summary>
        internal static bool HoldsWhenShownOnly(CombatEvent e) => RowOf(e)?.HoldsWhenShownOnly ?? false;

        /// <summary>
        /// 주체가 없어도 진행 중인 유닛 연출을 기다리지 않는 사건인지입니다(읽는 곳은 주체 막기 규칙 <c>CombatSubjectBlocking</c>). 지금은 움직이는 토네이도 사건뿐입니다(토네이도 View 설계 TN10).
        /// </summary>
        internal static bool OverlapsUnitRuns(CombatEvent e) => RowOf(e)?.OverlapsUnitRuns ?? false;

        /// <summary>
        /// 사건 순간에 소리·연출 큐가 붙는 사건이면 그 순간 값을 줍니다(이펙트 창구 확장 설계 EC7, 계획 P15). 소리 표(사건 소리)와
        /// 순간 큐 내는 곳이 이 질문 하나를 읽습니다. 순간이 아닌 사건·갈래면 false입니다.
        /// </summary>
        internal static bool TryGetMoment(CombatEvent e, out CombatEventMoment moment)
        {
            CombatEventMoment? value = RowOf(e)?.Moment?.Invoke(e);
            moment = value ?? default;
            return value.HasValue;
        }

        /// <summary>사건 틀 형식을 맡는 처리기 칸입니다. 표에 없는 형식은 예외입니다(처리기 표는 표의 형식만 돕니다).</summary>
        internal static CombatEventHandlerSlot HandlerSlotOf(Type eventType)
        {
            if (eventType == null)
                throw new ArgumentNullException(nameof(eventType));
            if (!Rows.TryGetValue(eventType, out TypeRow row))
                throw new ArgumentException($"[CombatEventTypeTable] 형식 표에 없는 사건 형식입니다: {eventType}", nameof(eventType));
            return row.HandlerSlot;
        }

        // null을 표를 찾기 전에 거릅니다(모든 질문이 null에 "없음"으로 답하는 규칙).
        private static TypeRow RowOf(CombatEvent e) => e != null && Rows.TryGetValue(e.GetType(), out TypeRow row) ? row : null;

        private static CombatEventRoute CharacterRoute(CombatCharacterEvent e)
        {
            if (e.Kind == CombatEventKind.Moved)
                return CombatEventRoute.Step;
            if (e.Kind == CombatEventKind.Returned && e.Expected.Area == CharacterRuntimeArea.Field &&
                e.Final.Area == CharacterRuntimeArea.Field)
                return CombatEventRoute.Return;
            return CombatEventRoute.Event;
        }

        // 특성 발동 사건의 주체입니다(특성 시스템 View 계획 TP-4). 추가 공격 시작이면 발동 유닛(쏘는 쪽)입니다. 발동이 모두 같은 대상 하나면 그 대상이고
        // (피해 변화·상태 적용은 맞는 쪽에 붙어 그 대상의 체력 사건과 같은 주체로 차례로 이어짐), 그 밖에 발동 유닛이 모두 같으면 그 유닛입니다.
        // 아니거나 특성 발동이 없으면 비어 있습니다(모든 유닛 연출을 봄).
        private static EntityId TraitSubjectOf(CombatEffectsEvent e)
        {
            EntityId target = EntityId.Empty;
            EntityId source = EntityId.Empty;
            bool oneTarget = true;
            bool oneSource = true;
            for (int i = 0; i < e.Traits.Count; i++)
            {
                TraitActivationSnapshot trait = e.Traits[i];
                if (IsChildAttackStart(trait))
                    return trait.SourceUnitId;
                oneTarget &= trait.TargetId.HasValue && (target.IsEmpty || target == trait.TargetId.Value);
                if (oneTarget)
                    target = trait.TargetId.Value;
                oneSource &= source.IsEmpty || source == trait.SourceUnitId;
                if (oneSource)
                    source = trait.SourceUnitId;
            }
            if (e.Traits.Count == 0)
                return EntityId.Empty;
            return oneTarget ? target : oneSource ? source : EntityId.Empty;
        }

        // 특성 발동 사건의 피해 근원입니다(특성 시스템 View 계획 TP-3). 발동 모두에 결과 근원이 있을 때만 그 목록이고, 하나라도 비면 null(묶음 밖)입니다.
        // 추가 공격 시작은 출처를 여는 사건이라 공격 사건처럼 묶음 밖이어야 하므로 근원을 주지 않습니다.
        private static IReadOnlyList<CombatDamageOrigin> TraitOriginsOf(CombatEffectsEvent e)
        {
            if (e.Traits.Count == 0)
                return null;
            var origins = new CombatDamageOrigin[e.Traits.Count];
            for (int i = 0; i < e.Traits.Count; i++)
            {
                TraitActivationSnapshot trait = e.Traits[i];
                if (IsChildAttackStart(trait) || trait.ResultOrigin == null)
                    return null;
                origins[i] = trait.ResultOrigin;
            }
            return origins;
        }

        // 추가 공격 시작 발동이 있는지입니다(결정 Q-TD1 A). 발동 하나의 판정은 IsChildAttackStart 하나입니다.
        private static bool HasChildAttackStart(CombatEffectsEvent e) => ChildAttackStartOf(e) != null;

        // 추가 공격 시작의 출처 사실입니다(특성 시스템 View 계획 TP-1). 공격자 = 발동 유닛, 공격 그룹 = 결과 근원의 그룹(Core가 늘 채움, WithChildAttack 검사).
        private static CombatAttackSource? ChildAttackSourceOf(CombatEffectsEvent e)
        {
            TraitActivationSnapshot child = ChildAttackStartOf(e);
            if (child == null)
                return null;
            if (child.ResultOrigin == null)
                throw new InvalidOperationException($"[CombatEventTypeTable] 추가 공격 시작에 결과 근원이 없습니다(Core 계약). 특성: {child.TraitKey}");
            return new CombatAttackSource(child.SourceUnitId, child.ResultOrigin.AttackGroup, null);
        }

        // 사건의 추가 공격 시작 발동입니다. 없으면 null입니다. Core는 추가 공격 시작을 발동 하나짜리 사건으로 냅니다(AttackResultFactory.AttachChild).
        // 계약 검사(둘 이상이면 오류)는 그 발동을 쓰는 특성 처리기가 합니다(계획 TP-6).
        private static TraitActivationSnapshot ChildAttackStartOf(CombatEffectsEvent e)
        {
            for (int i = 0; i < e.Traits.Count; i++)
            {
                if (IsChildAttackStart(e.Traits[i]))
                    return e.Traits[i];
            }
            return null;
        }

        // 줄 하나를 만듭니다. 비운 칸(null·false)은 "없음"입니다. 함수는 형식이 맞는 사건에만 불립니다(표를 형식으로 찾음).
        private static TypeRow Row<T>(CombatEventHandlerSlot handlerSlot,
            bool isActionStart = false,
            Func<T, CombatEventRoute> route = null,
            Func<T, EntityId> actor = null,
            Func<T, EntityId> subject = null,
            Func<T, IReadOnlyList<CombatDamageOrigin>> origins = null,
            Func<T, CombatAttackSource?> attackSource = null,
            Func<T, bool> isMovement = null,
            Func<T, CombatEventPacing> pacing = null,
            bool holdsWhenShownOnly = false,
            Func<T, CombatEventMoment?> moment = null,
            Func<T, bool> opensAttackSource = null,
            bool overlapsUnitRuns = false)
            where T : CombatEvent
        {
            return new TypeRow(typeof(T), handlerSlot, isActionStart, holdsWhenShownOnly, overlapsUnitRuns,
                Wrap(route), Wrap(actor), Wrap(subject), Wrap(origins), Wrap(attackSource), Wrap(isMovement), Wrap(pacing),
                Wrap(moment), Wrap(opensAttackSource));
        }

        // 형식별 함수를 사건 함수로 감쌉니다. 표를 만들 때 한 번만 부릅니다.
        private static Func<CombatEvent, TResult> Wrap<T, TResult>(Func<T, TResult> read) where T : CombatEvent
        {
            if (read == null)
                return null;
            return e => read((T)e);
        }

        private static Type[] TypesOf(TypeRow[] rows)
        {
            var types = new Type[rows.Length];
            for (int i = 0; i < rows.Length; i++)
                types[i] = rows[i].EventType;
            return types;
        }

        private static Dictionary<Type, TypeRow> Index(TypeRow[] rows)
        {
            var index = new Dictionary<Type, TypeRow>(rows.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                Type type = rows[i].EventType;
                if (!type.IsSealed)
                    throw new InvalidOperationException($"[CombatEventTypeTable] 형식 표에는 sealed 사건 형식만 넣습니다: {type}");
                if (index.ContainsKey(type))
                    throw new InvalidOperationException($"[CombatEventTypeTable] 같은 사건 형식이 두 줄입니다: {type}");
                index.Add(type, rows[i]);
            }
            return index;
        }

        // 표의 줄 하나입니다. 칸은 만들 때 한 번 정하고 바꾸지 않습니다. 함수 칸이 null이면 그 질문의 답은 "없음"입니다.
        private sealed class TypeRow
        {
            internal TypeRow(Type eventType, CombatEventHandlerSlot handlerSlot, bool isActionStart, bool holdsWhenShownOnly, bool overlapsUnitRuns,
                Func<CombatEvent, CombatEventRoute> route,
                Func<CombatEvent, EntityId> actor,
                Func<CombatEvent, EntityId> subject,
                Func<CombatEvent, IReadOnlyList<CombatDamageOrigin>> origins,
                Func<CombatEvent, CombatAttackSource?> attackSource,
                Func<CombatEvent, bool> isMovement,
                Func<CombatEvent, CombatEventPacing> pacing,
                Func<CombatEvent, CombatEventMoment?> moment,
                Func<CombatEvent, bool> opensAttackSource)
            {
                EventType = eventType;
                HandlerSlot = handlerSlot;
                IsActionStart = isActionStart;
                HoldsWhenShownOnly = holdsWhenShownOnly;
                OverlapsUnitRuns = overlapsUnitRuns;
                Route = route;
                Actor = actor;
                Subject = subject;
                Origins = origins;
                AttackSource = attackSource;
                IsMovement = isMovement;
                Pacing = pacing;
                Moment = moment;
                OpensAttackSource = opensAttackSource;
            }

            internal Type EventType { get; }
            internal CombatEventHandlerSlot HandlerSlot { get; }
            internal bool IsActionStart { get; }
            internal bool HoldsWhenShownOnly { get; }
            internal bool OverlapsUnitRuns { get; }
            internal Func<CombatEvent, CombatEventRoute> Route { get; }
            internal Func<CombatEvent, EntityId> Actor { get; }
            internal Func<CombatEvent, EntityId> Subject { get; }
            internal Func<CombatEvent, IReadOnlyList<CombatDamageOrigin>> Origins { get; }
            internal Func<CombatEvent, CombatAttackSource?> AttackSource { get; }
            internal Func<CombatEvent, bool> IsMovement { get; }
            internal Func<CombatEvent, CombatEventPacing> Pacing { get; }
            internal Func<CombatEvent, CombatEventMoment?> Moment { get; }
            internal Func<CombatEvent, bool> OpensAttackSource { get; }
        }
    }
}
