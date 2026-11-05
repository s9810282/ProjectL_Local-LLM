using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item
{
    /// <summary>
    /// 특성 발동 연출 줄 하나입니다(특성 발동 연출 설계 TA2, 계획 P4). 특성 외형 표의 줄(<see cref = "TraitVisualData"/>)이 발동 종류마다 하나씩 갖습니다.
    /// 열쇠는 발동 종류이고, 특성 키는 그 표 줄이 이미 갖습니다(Q-TA2 A).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 큐(<see cref = "Cues"/>)는 발동의 이펙트·흔들림입니다. 시점은 선택한 Cue Timing 기준의 Start만, 이펙트 자리는 발동 유닛 몸·대상 몸·칸입니다(큐 칸 <see cref = "CombatCueEvent.TraitActivation"/>).
    /// 큐만 있는 줄은 사건을 막지 않고 냅니다(Q-TA4 A). 소리는 특성 소리 갈래를 소리 표에 둘 때 정합니다(계획 P5).
    /// </para>
    /// <para>
    /// 추가 공격 시작 발동(Core 사실에 추가 공격 결과가 있음)은 줄이 있든 없든 발동 유닛이 공격 연출로 칩니다(특성 시스템 View 결정 Q-TS1 A).
    /// 이 줄의 공격 표시(<see cref = "AttackPresentation"/>)는 그때의 덮어쓰기이고, 비우면 그 유닛의 일반 공격 표시를 씁니다(Q-TA3 B, 계획 P8).
    /// 어느 발동 종류가 추가 공격을 내는지는 Core가 정하므로 이 줄은 종류로 막지 않습니다(결정 Q-TS2 A). 공격을 내지 않는 발동 줄의 덮어쓰기는 쓰이지 않을 뿐입니다.
    /// </para>
    /// <para>
    /// 유닛 행동(<see cref = "UnitActions"/>)은 서로 다른 유닛에게 동시에 시키는 행동 목록입니다. Core 위치는 바꾸지 않습니다.
    /// 언제 시작해서 얼마나 머무는지는 발동 사실이 온 자리가 정합니다 — 사건 목록 안 발동은 그 사건 순간에, 공격 결과 안 가로채기(대신 맞기)는 공격 동작보다 먼저입니다.
    /// 추가 공격을 내는 발동은 공격이 그 유닛 자세를 쓰므로 행동을 건너뜁니다(경고 한 번, 설계 P9).
    /// </para>
    /// </remarks>
    [Serializable]
    internal sealed class TraitActivationVisual
    {
        [Tooltip("이 줄이 받을 발동 종류입니다. 한 특성 안에서 같은 종류를 두 줄로 둘 수 없습니다.")]
        [SerializeField]
        private TraitActivationKind kind;
        [Tooltip("이 줄이 받을 값의 방향입니다. Any = 가리지 않음. Up/Down은 결속 방어력(DefenseChanged) 줄에서만 고를 수 있고, "
            + "그 발동 뒤 상태 변화로 오를 때·내릴 때를 가립니다. 방향 줄이 없거나 방향을 모르면 Any 줄을 씁니다. 같은 종류·방향을 두 줄로 둘 수 없습니다.")]
        [SerializeField]
        private CombatActivationDirection direction;
        [Tooltip("ActivationStart는 발동/희생 나가기 시작, StateApplied는 대응 상태의 HUD 갱신 직후입니다. Anchor Start는 선택된 시점을 기준으로 합니다.")]
        [SerializeField]
        private CombatActivationCueTiming cueTiming;
        [SerializeField]
        private CombatBodyTextInput bodyText = new();
        internal CombatActivationCueTiming CueTiming => cueTiming;
        internal CombatBodyTextData BodyText => bodyText?.ToData();

        [Tooltip("선택한 Cue Timing의 이펙트·흔들림입니다. Anchor는 Start만 됩니다. 이펙트 자리는 ActorBody(발동 유닛)·SubjectBody(대상마다)·EventCell(칸마다)입니다(StateApplied는 몸만). " + "지연을 주어도 다음 사건은 기다리지 않습니다.")]
        [SerializeField]
        private CombatVisualCueInput cues = new();
        [Tooltip("이 발동이 추가 공격(난사·2연발·연쇄 폭발·행운의 탄환 등)을 낼 때의 공격 동작·투사체입니다. 비우면 그 유닛의 일반 공격 표시를 씁니다. " + "추가 공격을 내지 않는 발동 줄에서는 쓰이지 않습니다. 이펙트 큐가 이펙트 지점·발사 지점 자리를 쓰면 DB 생성이 멈춥니다(어느 유닛이 쓸지 모름). " + "공격 클립이 있는 유닛은 그 계열(근접/원거리)과 같을 때만 쓰고, 다르면 경고 뒤 그 유닛의 공격 표시를 씁니다.")]
        [SerializeField]
        private CombatAttackPresentationSO attackPresentation;
        [Tooltip("이 발동이 유닛에게 동시에 시키는 화면 동작 목록입니다. 같은 활성 Actor 역할은 한 번만 지정합니다. None 항목은 행동이 없습니다. " + "언제 시작해서 얼마나 머무는지는 발동이 온 자리가 정합니다: 사건 순간 발동은 그 순간에 시작해 머무는 시간만큼, " + "대신 맞기(가로채기)는 공격보다 먼저 나가 맞고 그 유닛의 피격·사망 연출이 끝나면 돌아옵니다. Core 위치는 바뀌지 않습니다.")]
        [SerializeField]
        private List<CombatUnitActionInput> unitActions = new();
        [SerializeField]
        private CombatPositionSwapVisualInput positionSwap = new();

        /// <summary>위치 교환 전용 시간 연출입니다. 비어 있거나 꺼져 있으면 기존 유닛 행동을 사용합니다.</summary>
        internal CombatPositionSwapVisualData PositionSwap => positionSwap?.ToData();

        /// <summary>발동 종류입니다.</summary>
        public TraitActivationKind Kind => kind;

        /// <summary>이 줄이 받는 값의 방향입니다(이펙트 끝 규칙 설계 EL7). 기본값 Any라 이 칸이 없던 에셋은 지금과 같습니다.</summary>
        internal CombatActivationDirection Direction => direction;

        /// <summary>방향을 고를 수 있는 발동 종류인지입니다(계획 TP-E18). 규칙의 원본은 이곳 하나입니다.</summary>
        internal static bool AllowsDirection(TraitActivationKind kind) => kind == TraitActivationKind.DefenseChanged;

        /// <summary>발동 순간의 큐입니다. 읽을 때마다 직렬화 칸에서 새로 만듭니다(쓰는 쪽이 보관).</summary>
        /// <exception cref = "ArgumentException">큐 값이 맞지 않을 때입니다.</exception>
        internal CombatEventCues Cues => (cues ?? throw new InvalidOperationException($"[TraitActivationVisual] 큐 칸이 없습니다. Kind: {kind}")).ToCues();
        /// <summary>추가 공격을 낼 때의 공격 표시 덮어쓰기입니다. 비어 있으면 null이고, 그 유닛의 일반 공격 표시를 씁니다.</summary>
        internal CombatAttackPresentationData AttackPresentation => attackPresentation != null ? attackPresentation.CreateData() : null;

        /// <summary>유닛 행동 값 목록입니다. None 항목은 제외하고, 읽을 때마다 값 사본을 만듭니다.</summary>
        /// <exception cref = "ArgumentOutOfRangeException">행동 값이 범위 밖일 때입니다.</exception>
        internal IReadOnlyList<CombatUnitActionData> UnitActions
        {
            get
            {
                if (unitActions == null)
                    throw new InvalidOperationException($"[TraitActivationVisual] 행동 목록이 없습니다. Kind: {kind}");
                var values = new List<CombatUnitActionData>(unitActions.Count);
                var actors = new HashSet<CombatUnitActionActor>();
                for (int i = 0; i < unitActions.Count; i++)
                {
                    CombatUnitActionInput input = unitActions[i] ?? throw new InvalidOperationException($"[TraitActivationVisual] 행동 항목 {i}이 비었습니다. Kind: {kind}");
                    CombatUnitActionData action = input.ToData();
                    if (action == null)
                        continue;
                    if (!actors.Add(action.Actor))
                        throw new InvalidOperationException($"[TraitActivationVisual] 같은 행동 유닛 역할을 두 번 지정했습니다: {action.Actor}. Kind: {kind}, 항목: {i}");
                    values.Add(action);
                }

                return values.AsReadOnly();
            }
        }

        /// <summary>DB 생성 때 줄 하나를 검사합니다. 같은 종류 겹침은 표 줄(<see cref = "TraitVisualData"/>)이 봅니다.</summary>
        /// <param name = "owner">이 줄을 가진 특성 키입니다(문구용).</param>
        /// <exception cref = "InvalidOperationException">
        /// 발동 종류가 비었거나 정의되지 않았거나, 유닛 행동 값이 범위 밖이거나, 공격 표시의 이펙트 큐가 행위자 지점 자리를 쓸 때입니다.
        /// </exception>
        /// <exception cref = "ArgumentException">큐가 큐 칸 규칙에 맞지 않을 때입니다.</exception>
        internal void Validate(TraitKey owner)
        {
            if (kind == TraitActivationKind.None || !System.Enum.IsDefined(typeof(TraitActivationKind), kind))
                throw new InvalidOperationException($"[TraitActivationVisual] 발동 종류가 비었거나 정의되지 않았습니다. Trait: {owner}, Kind: {kind}");
            string context = $"[TraitActivationVisual] Trait: {owner}, Kind: {kind}, Direction: {direction}";
            if (!System.Enum.IsDefined(typeof(CombatActivationDirection), direction))
                throw new InvalidOperationException($"{context}: 방향이 정의되지 않았습니다.");
            if (direction != CombatActivationDirection.Any && !AllowsDirection(kind))
                throw new InvalidOperationException($"{context}: 방향(Up/Down)은 결속 방어력(DefenseChanged) 줄에서만 고를 수 있습니다.");
            CombatPresentationCueRules.Require(Cues, CombatCueEvent.TraitActivation, context);
            if (!System.Enum.IsDefined(typeof(CombatActivationCueTiming), cueTiming))
                throw new InvalidOperationException($"{context}: Cue Timing이 정의되지 않았습니다.");
            CombatBodyTextData text = BodyText;
            CombatTraitFeedbackProfile profile = CombatTraitFeedbackProfiles.Get(owner, kind);
            if (text != null && !profile.Allows(text.Count))
                throw new InvalidOperationException($"{context}: 이동 칸 수는 가속 AttackGranted에서만 표시합니다.");
            if (cueTiming == CombatActivationCueTiming.StateApplied)
            {
                if (!profile.SupportsStateApplied || UnitActions.Count != 0 || attackPresentation != null)
                    throw new InvalidOperationException($"{context}: 이 줄의 StateApplied 근거가 없거나 행동/추가 공격을 함께 지정했습니다.");
                foreach (CombatEffectCue effect in Cues.Effects)
                    if (effect.Point != CombatCuePoint.ActorBody && effect.Point != CombatCuePoint.SubjectBody)
                        throw new InvalidOperationException($"{context}: StateApplied 효과는 ActorBody/SubjectBody만 지원합니다.");
            }

            // 행동 값은 만들 때 스스로 검사합니다(값 생성자 하나, 유닛 행동 설계 AA1). 어느 특성 줄인지 알 수 있게 감쌉니다.
            IReadOnlyList<CombatUnitActionData> actions;
            try
            {
                actions = UnitActions;
            }
            catch (ArgumentOutOfRangeException error)
            {
                throw new InvalidOperationException($"{context}: 유닛 행동 값이 맞지 않습니다. {error.Message}", error);
            }
            CombatPositionSwapVisualData swap = ValidatePositionSwap(owner, context, actions);
            // 행동 끝 큐의 끝은 그 줄의 유닛 행동 또는 위치 교환 전용 대기가 소유합니다(이펙트 끝 규칙 계획 TP-E2, PS1).
            // 대신 맞기(DamageRedirected)는 공격 조립 안에서 행동하고 행동 끝을 세는 곳이 없어 결과 끝까지 남으므로 막습니다(묶음 끝 검토 M1).
            if (Cues.EndsWithAction)
            {
                if (actions.Count == 0 && swap == null)
                    throw new InvalidOperationException($"{context}: 끝 UnitAction(행동 끝) 큐는 유닛 행동이나 위치 교환 시간 연출이 있는 줄에서만 고를 수 있습니다.");
                if (kind == TraitActivationKind.DamageRedirected)
                    throw new InvalidOperationException($"{context}: 대신 맞기(DamageRedirected) 줄은 공격 조립 안에서 행동하므로 끝 UnitAction(행동 끝) 큐를 쓸 수 없습니다. Natural·Seconds를 고르세요.");
            }

            if (attackPresentation == null)
                return;
            // 공격 표시 값은 만들 때 스스로 검사합니다.
            CombatAttackPresentationData presentation = attackPresentation.CreateData();
            if (CombatPresentationCueRules.UsesAdditionalEvents(presentation.Cues))
                throw new InvalidOperationException($"{context}: 공격 표시 덮어쓰기는 Cue2·Cue3를 쓸 수 없습니다. 어느 유닛의 공격 클립을 쓸지 모르기 때문입니다.");
            // 행위자 지점 자리는 유닛마다 다른 조건(공격 클립·프리팹 지점)인데, 이 줄은 그 특성을 가진 어느 유닛이 쓸지 모릅니다. 그래서 여기서 막습니다
            // (묶음 끝 검토 M2). 유닛 자신의 공격 표시는 외형 표 검사가 그 유닛 기준으로 봅니다.
            if (CombatPresentationCueRules.UsesActorPoint(presentation.Cues))
                throw new InvalidOperationException($"{context}: 공격 표시 덮어쓰기의 이펙트 큐는 행위자 지점 자리(ActorEffectPoint·ActorMuzzle)를 쓸 수 없습니다. 어느 유닛이 쓸지 모르기 때문입니다. " + "ActorBody 등 다른 자리를 고르거나 덮어쓰기를 비우세요(비우면 그 유닛의 공격 표시를 씁니다).");
        }

        private CombatPositionSwapVisualData ValidatePositionSwap(TraitKey owner, string context, IReadOnlyList<CombatUnitActionData> actions)
        {
            CombatPositionSwapVisualData swap;
            try
            {
                swap = PositionSwap;
            }
            catch (ArgumentOutOfRangeException error)
            {
                throw new InvalidOperationException($"{context}: 위치 교환 대기시간이 맞지 않습니다. {error.Message}", error);
            }
            if (swap == null)
                return null;
            if (owner != TraitKey.PositionSwap || kind != TraitActivationKind.MovementChanged ||
                cueTiming != CombatActivationCueTiming.ActivationStart || actions.Count != 0 || attackPresentation != null)
                throw new InvalidOperationException($"{context}: 위치 교환 시간 연출은 PositionSwap/MovementChanged의 ActivationStart에서 유닛 행동·추가 공격 없이 사용합니다.");
            foreach (CombatEffectCue effect in Cues.Effects)
                if (effect.Delay != 0f || effect.End != CombatEffectEnd.UnitAction)
                    throw new InvalidOperationException($"{context}: 위치 교환 이펙트는 지연 0·끝 UnitAction으로 두고 교환 대기시간 하나를 사용합니다.");
            return swap;
        }
    }
}
