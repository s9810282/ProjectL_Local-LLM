using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>소리 단서의 종류입니다(조각·조립 설계 BP8).</summary>
    internal enum CombatSoundCueKind
    {
        /// <summary>내지 않습니다.</summary>
        None = 0,

        /// <summary>효과음 칸을 프리셋과 함께 냅니다.</summary>
        Effect = 1,

        /// <summary>효과음 칸을 프리셋을 바꾸지 않고 냅니다(칸의 지금 프리셋, 장애물·추락·기차 등).</summary>
        EffectWithoutPreset = 2,

        /// <summary>배경음 프리셋을 바꿉니다.</summary>
        BackgroundPreset = 3,

        /// <summary>UnitAttack 플레이어의 이벤트 슬롯을 냅니다.</summary>
        UnitAttack = 4,
    }

    /// <summary>전투 배경음의 국면입니다(구조 원칙 평가 R2 (a)). 열거 값은 프리셋 번호가 아닙니다. 번호는 <see cref="CombatSoundPresets.Bgm"/>로만 얻습니다.</summary>
    internal enum CombatBgmPhase
    {
        /// <summary>낮(배치) 국면입니다.</summary>
        Day = 1,

        /// <summary>밤(전투) 국면입니다.</summary>
        Night = 2,

        /// <summary>승패 표시를 확인한 뒤입니다. 맵으로 돌아가는 배경음입니다.</summary>
        AfterCompletion = 3,
    }

    /// <summary>"무엇을 내나"의 답 하나입니다. 언제 내는지는 시계를 가진 쪽이 정합니다(조각·조립 설계 BP8).</summary>
    internal readonly struct CombatSoundCue
    {
        private CombatSoundCue(CombatSoundCueKind kind, SfxEvent slot, int preset, bool playStageChanger = false, UnitEvent? unitSlot = null, int attackSlotIndex = -1)
        {
            Kind = kind;
            Slot = slot;
            UnitSlot = unitSlot;
            Preset = preset;
            PlayStageChanger = playStageChanger;
            AttackSlotIndex = attackSlotIndex;
        }

        internal CombatSoundCueKind Kind { get; }

        /// <summary>SFX 플레이어의 칸입니다. 유닛·배경음 단서에서는 쓰지 않습니다.</summary>
        internal SfxEvent Slot { get; }

        /// <summary>Unit 플레이어의 칸입니다. 값이 없으면 SFX 플레이어의 칸을 씁니다.</summary>
        internal UnitEvent? UnitSlot { get; }

        /// <summary>프리셋 번호입니다. 프리셋 없는 효과음이면 -1입니다.</summary>
        internal int Preset { get; }

        /// <summary>공격 소리 단서에서 사용하는 UnitAttack 이벤트 슬롯 번호입니다.</summary>
        internal int AttackSlotIndex { get; }

        /// <summary>배경음 프리셋 변경과 함께 전투 진입음을 냅니다.</summary>
        internal bool PlayStageChanger { get; }

        internal static CombatSoundCue None => default;

        /// <summary>프리셋이 있는 효과음입니다. 프리셋이 없으면(음수) 내지 않습니다.</summary>
        internal static CombatSoundCue Effect(SfxEvent slot, int preset) =>
            CombatSoundPresets.IsSound(preset) ? new CombatSoundCue(CombatSoundCueKind.Effect, slot, preset) : None;

        internal static CombatSoundCue EffectWithoutPreset(SfxEvent slot) => new(CombatSoundCueKind.EffectWithoutPreset, slot, -1);

        /// <summary>Unit 플레이어의 효과음입니다. 프리셋이 없으면 내지 않습니다.</summary>
        internal static CombatSoundCue Effect(UnitEvent slot, int preset) =>
            CombatSoundPresets.IsSound(preset) ? new CombatSoundCue(CombatSoundCueKind.Effect, default, preset, unitSlot: slot) : None;

        internal static CombatSoundCue EffectWithoutPreset(UnitEvent slot) =>
            new(CombatSoundCueKind.EffectWithoutPreset, default, -1, unitSlot: slot);

        /// <summary>
        /// Unit 플레이어의 효과음을 프리셋이 있으면 그 프리셋으로, 없으면(-1) 칸의 지금 프리셋을 바꾸지 않고 냅니다(소리 프리셋 원본 설계 SP3).
        /// -1의 뜻이 "소리 없음"인 <see cref="Effect(UnitEvent, int)"/>와 다른 곳은 이 함수를 부르는 곳(장애물·추락)입니다.
        /// </summary>
        internal static CombatSoundCue EffectKeepingPreset(UnitEvent slot, int preset) =>
            CombatSoundPresets.IsSound(preset) ? Effect(slot, preset) : EffectWithoutPreset(slot);

        internal static CombatSoundCue Background(int preset, bool playStageChanger = false) =>
            new(CombatSoundCueKind.BackgroundPreset, default, preset, playStageChanger);

        internal static CombatSoundCue Attack(int slotIndex) =>
            CombatSoundPresets.IsSound(slotIndex) ? new CombatSoundCue(CombatSoundCueKind.UnitAttack, default, -1, attackSlotIndex: slotIndex) : None;

        public override string ToString() => Kind switch
        {
            CombatSoundCueKind.Effect => $"{UnitSlot?.ToString() ?? Slot.ToString()}({Preset})",
            CombatSoundCueKind.EffectWithoutPreset => UnitSlot?.ToString() ?? Slot.ToString(),
            CombatSoundCueKind.BackgroundPreset => $"BGM({Preset})",
            CombatSoundCueKind.UnitAttack => $"UnitAttack({AttackSlotIndex})",
            _ => "None",
        };
    }

    /// <summary>
    /// 조립 하나가 낼 유닛 소리 줄입니다(유닛 소리 계획 TP-6~TP-8). 칸(공격·피격·사망)과 그 유닛의 줄 목록, 사망이면 "아직 반영 전" 조건을 볼 사망 사건입니다.
    /// 무엇을 낼지는 칸과 줄 번호로 소리 표(<see cref="CombatEventSounds.UnitSound"/>)가 정하고, 언제 낼지는 줄의 시점·지연을 조립이 씁니다.
    /// 줄이 하나도 없으면 만들지 않습니다(받는 쪽은 null을 "유닛 소리 없음"으로 봄).
    /// </summary>
    internal sealed class CombatUnitSoundSet
    {
        private CombatUnitSoundSet(CombatUnitSoundSlot slot, IReadOnlyList<CombatUnitSoundCue> rows, CombatCharacterEvent death)
        {
            Slot = slot;
            Rows = rows;
            Death = death;
        }

        internal CombatUnitSoundSlot Slot { get; }

        /// <summary>줄 목록입니다. 하나 이상입니다.</summary>
        internal IReadOnlyList<CombatUnitSoundCue> Rows { get; }

        /// <summary>
        /// 소리를 낼 때 "아직 사망 반영 전"인지 볼 사망 사건입니다. 다른 칸이거나 판단을 이미 끝낸 사망 줄(<see cref="Decided"/>)은 null입니다.
        /// </summary>
        internal CombatCharacterEvent Death { get; }

        /// <summary>
        /// 사망 판단을 지금 끝낸 같은 줄입니다(낼 때 다시 보지 않음, 묶음 끝 검토 M1). 사망 조립은 처리기가 "반영 전"을 확인한 뒤 만들고 시작 바로 뒤에 사망을 반영하므로,
        /// 시작 순간이 아닌 줄(End·지연)까지 내려면 판단을 조립 때 끝내야 합니다. 판단할 것이 없으면 이 객체 그대로입니다.
        /// </summary>
        internal CombatUnitSoundSet Decided() => Death == null ? this : new CombatUnitSoundSet(Slot, Rows, null);

        /// <summary>그 유닛 소리 줄 묶음의 한 칸입니다. 그 칸에 줄이 없으면 null입니다.</summary>
        /// <param name="death">사망 칸이면 사망 사건입니다(소리 조각이 시작할 때 "아직 반영 전"인지 봄).</param>
        internal static CombatUnitSoundSet Of(CombatUnitSoundSlot slot, CombatUnitSoundsData sounds, CombatCharacterEvent death = null)
        {
            if (sounds == null) throw new ArgumentNullException(nameof(sounds));
            IReadOnlyList<CombatUnitSoundCue> rows = sounds.Of(slot);
            return rows.Count == 0 ? null : new CombatUnitSoundSet(slot, rows, slot == CombatUnitSoundSlot.Death ? death : null);
        }

        /// <summary>
        /// 그 공격의 표시 종류·클립에 맞춘 공격 줄입니다(유닛 소리 계획 TP-4). 스킬·특성 발동 줄이 공격 표시를 덮어쓰면 시전자 줄이 그 종류에 맞지 않을 수 있어
        /// 공격 시작이 늘 부릅니다. 바뀐 줄이 없으면 이 객체 그대로입니다(할당 없음). 규칙은 <see cref="CombatPresentationCueRules.AttackSoundsFor"/> 하나입니다.
        /// </summary>
        internal CombatUnitSoundSet ForAttack(CombatCueEvent kind, CombatClipMotion motion)
        {
            if (Slot != CombatUnitSoundSlot.Attack)
                throw new InvalidOperationException($"[CombatUnitSoundSet] 공격 칸이 아닌 줄을 공격에 맞추려 했습니다: {Slot}");
            IReadOnlyList<CombatUnitSoundCue> fitted = CombatPresentationCueRules.AttackSoundsFor(Rows, kind, motion);
            return ReferenceEquals(fitted, Rows) ? this : new CombatUnitSoundSet(Slot, fitted, null);
        }
    }

    /// <summary>
    /// 결과 재생 사건의 "무엇을 내나" 표입니다(조각·조립 설계 BP8, 결정 Q-B4 A). 사건과 보인 사실로 소리 단서를 답합니다.
    /// 분기는 옛 사건 소리에 소리 창구 2차(오브젝트·추락·약한 지반 붕괴·아이템·장애물 프리셋)를 더한 것이고, 사실 사건의 순간은
    /// 사건 형식 표의 순간 칸을 읽습니다(이펙트 창구 확장 계획 P16). 칸에 넣을 프리셋 번호는 모두
    /// View DB에서 고르는 <see cref="CombatSoundPresets"/>가 답합니다(View 3라운드 W9-2). 이 표에는 프리셋 숫자를 두지 않습니다(소리 프리셋 원본 설계 SP1~SP3).
    /// </summary>
    internal static class CombatEventSounds
    {
        /// <summary>
        /// 유닛 소리가 아닌 사건 하나의 소리 단서입니다. 공격·스킬·포효·사망은 유닛 외형의 소리 줄이라 <see cref="UnitSoundsOf"/>가 답하고 여기서는 없음입니다(유닛 소리 설계 US2).
        /// </summary>
        /// <param name="presets">칸에 넣을 프리셋 번호를 답합니다.</param>
        /// <param name="findCharacter">프리셋을 고를 유닛을 지금 보인 사실에서 찾습니다.</param>
        internal static CombatSoundCue For(CombatEvent e, CombatSoundPresets presets, Func<EntityId, CharacterCombatFacts> findCharacter)
        {
            if (presets == null) throw new ArgumentNullException(nameof(presets));
            if (findCharacter == null) throw new ArgumentNullException(nameof(findCharacter));
            switch (e)
            {
                // 신규 표식은 Core의 실제 적용 발동을 읽습니다. 상태 만료·소모 사건에는 소리를 내지 않습니다.
                case CombatEffectsEvent effects when HasEnemyMarkApplication(effects, findCharacter):
                    return CombatSoundCue.EffectWithoutPreset(SfxEvent.Mark);
                // 옛 기록의 표식 적용도 같은 슬롯으로 냅니다. Final이 없으면 소모입니다.
                case CombatHuntersMarkEvent mark when mark.Final != null && findCharacter(mark.TargetId)?.Owner == Owner.Enemy:
                    return CombatSoundCue.EffectWithoutPreset(SfxEvent.Mark);
                case CombatFallEvent fall:
                    return findCharacter(fall.EntityId) != null
                        ? CombatSoundCue.EffectKeepingPreset(UnitEvent.Fall, presets.Fall())
                        : CombatSoundCue.None;
                case CombatMovementSourceEvent source when CombatTerrainChanges.TryGetWeakGroundEntry(source, out _, out var ground) && !ground.PendingCollapse:
                    return CombatSoundCue.Effect(SfxEvent.WeakTerrian, presets.WeakGroundEntry(ground.RemainingEntries));
                case CombatTurnEvent turn when turn.Kind == CombatEventKind.CombatEntered:
                    return CombatSoundCue.Background(presets.Bgm(CombatBgmPhase.Night), playStageChanger: true);
                // 기차인지는 사건 형식 표의 경로가 정한다(View 3라운드 W12-4).
                case CombatGimmickEvent gimmick when CombatEventTypeTable.IsRailway(gimmick):
                    return CombatSoundCue.EffectWithoutPreset(SfxEvent.TrainCall);
                // 사실 사건의 순간(장애물 반응·오브젝트·약한 지반 붕괴·아이템)은 사건 형식 표의 순간 칸이 고릅니다(이펙트 창구 확장 계획 P16).
                // 어느 사건·조건이 순간인지는 그 칸 한 곳이고, 여기서는 순간 종류마다 무엇을 낼지만 정합니다.
                default:
                    return CombatEventTypeTable.TryGetMoment(e, out CombatEventMoment moment) ? Moment(moment, presets) : CombatSoundCue.None;
            }
        }

        /// <summary>
        /// 사건 하나가 낼 유닛 소리 줄입니다(유닛 소리 계획 TP-7). 어느 사건이 어느 칸인지는 여기 한 곳입니다:
        /// 성공한 공격·스킬 공격·포효 = 행위자(사건 형식 표 <see cref="CombatEventTypeTable.ActorOf"/>)의 공격 줄, 사망 캐릭터 사건 = 그 캐릭터의 사망 줄입니다.
        /// 그 밖의 사건은 null입니다. 특성 발동이 출처인 추가 공격도 null이라 지금처럼 공격 소리가 없습니다(계획 검토 R4). 줄이 없어도 null입니다.
        /// </summary>
        internal static CombatUnitSoundSet UnitSoundsOf(CombatEvent e, CombatSoundPresets presets, Func<EntityId, CharacterCombatFacts> findCharacter)
        {
            if (presets == null) throw new ArgumentNullException(nameof(presets));
            if (findCharacter == null) throw new ArgumentNullException(nameof(findCharacter));
            switch (e)
            {
                case CombatAttackEvent attack when !attack.Attack.Succeeded:
                    return null;
                case CombatAttackEvent:
                case CombatPlayerSkillAttackEvent:
                case CombatRoarEvent:
                    CharacterCombatFacts actor = findCharacter(CombatEventTypeTable.ActorOf(e));
                    return actor == null ? null : CombatUnitSoundSet.Of(CombatUnitSoundSlot.Attack, presets.UnitSounds(actor.CharacterKey));
                case CombatCharacterEvent character when character.Kind == CombatEventKind.Died:
                    return CombatUnitSoundSet.Of(CombatUnitSoundSlot.Death, presets.UnitSounds(character.Final.CharacterKey), character);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 유닛 소리 줄 하나의 단서입니다(유닛 소리 계획 TP-6). 칸 → 효과음 칸 대응은 이 함수 하나입니다.
        /// 공격 = UnitAttack 플레이어의 그 이벤트 칸, 피격 = 피격 효과음 칸(연결 대기), 사망 = Unit 플레이어 <see cref="UnitEvent.UnitDead"/>입니다.
        /// </summary>
        internal static CombatSoundCue UnitSound(CombatUnitSoundSlot slot, int sound) => slot switch
        {
            CombatUnitSoundSlot.Attack => CombatSoundCue.Attack(sound),
            CombatUnitSoundSlot.Hit => UnitHit(sound),
            CombatUnitSoundSlot.Death => CombatSoundCue.Effect(UnitEvent.UnitDead, sound),
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "모르는 유닛 소리 칸입니다."),
        };

        /// <summary>
        /// 사망 소리를 낼 수 있는 사망 사건인지입니다. 보인 사실에 아직 사망이 반영되지 않았을 때만 참입니다(이미 사망으로 보인 유닛은 소리를 내지 않음).
        /// 소리 조각이 시작하는 순간에 봅니다(옛 사건 소리 표의 사망 조건 그대로).
        /// </summary>
        internal static bool IsUnshownDeath(CombatCharacterEvent death, Func<EntityId, CharacterCombatFacts> findCharacter)
        {
            if (death == null) throw new ArgumentNullException(nameof(death));
            if (findCharacter == null) throw new ArgumentNullException(nameof(findCharacter));
            return death.Kind == CombatEventKind.Died && !death.Expected.Health.IsDead && findCharacter(death.Final.EntityId)?.Health.IsDead != true;
        }

        // 순간 종류 → 소리입니다. 모두 사건 순간에 내고 지연은 없습니다(소리 창구 2차 결정 Q-ES7′). 옛 사건 갈래와 결과가 같습니다.
        // 추락(Fall) 순간은 앞 갈래(CombatFallEvent)가 먼저 받아 여기 오지 않습니다. 다른 사건이 추락 순간을 내게 되면 이 표에 줄을 더해야 합니다(소리 프리셋 원본 설계 Q-SP4 A).
        private static CombatSoundCue Moment(in CombatEventMoment moment, CombatSoundPresets presets) => moment.Kind switch
        {
            // 오브젝트는 발동(지뢰)·습득(깃발·보안관 배지)이 같은 칸이고, 다른 소리는 외형 번호가 가릅니다(소리 창구 2차 결정 Q-ES2″).
            CombatMomentKind.ObjectActivated => Slotted(ObjectActivatedSlot, presets.Object(moment.ObjectKind)),
            // 생성도 같은 외형 번호를 씁니다. 지금 Core는 이 사건을 내지 않습니다(소리 창구 2차 계획 §0.2).
            CombatMomentKind.ObjectCreated => Slotted(ObjectCreatedSlot, presets.Object(moment.ObjectKind)),
            // 장애물 피해·파괴는 그 장애물 외형의 번호를 넣고, 번호가 없으면(-1) 프리셋 없이 냅니다(소리 창구 2차 결정 Q-ES3, 설계 ES5).
            CombatMomentKind.ObstacleHit => CombatSoundCue.EffectKeepingPreset(UnitEvent.ObsDamaged, presets.Obstacle(moment.ObstacleKind)),
            CombatMomentKind.ObstacleDestroyed => CombatSoundCue.EffectKeepingPreset(UnitEvent.ObsBroken, presets.Obstacle(moment.ObstacleKind)),
            // 실제 붕괴는 남은 진입 횟수가 0인 순간입니다.
            CombatMomentKind.WeakGroundCollapse => CombatSoundCue.Effect(SfxEvent.WeakTerrian, presets.WeakGroundCollapse()),
            // 효과가 겹쳐 수치 없이 소모만 된 "중복 효과"도 같은 소리를 냅니다(소리 창구 2차 검토 L1 결정 A).
            CombatMomentKind.ItemActivated => Slotted(ItemActivatedSlot, presets.Item(moment.ItemKey)),
            _ => throw new ArgumentOutOfRangeException(nameof(moment), moment.Kind, "모르는 사건 순간 종류입니다."),
        };

        private static bool HasEnemyMarkApplication(CombatEffectsEvent effects, Func<EntityId, CharacterCombatFacts> findCharacter)
        {
            for (int i = 0; i < effects.Traits.Count; i++)
            {
                TraitActivationSnapshot activation = effects.Traits[i];
                if (activation.TraitKey != TraitKey.HuntersMark || activation.TraitActivationKind != TraitActivationKind.StatusApplied)
                    continue;
                for (int target = 0; target < activation.TargetIds.Count; target++)
                    if (findCharacter(activation.TargetIds[target])?.Owner == Owner.Enemy)
                        return true;
            }
            return false;
        }

        // 비행·피격 소리의 효과음 칸입니다(투사체·피격 소리 설계 PS2, 계획 P9). AudioSystem에 그 슬롯이 아직 없어 연결 대기(null)이고,
        // 그동안은 프리셋이 있어도 소리를 내지 않습니다(예외도 없음). 슬롯이 생기면 이 두 줄을 해당 값으로 채웁니다.
        // SfxEvent에 없는 값을 숫자로 짐작해 쓰지 않습니다(Boot 플레이어 슬롯 순서와 어긋날 수 있음). 요청: handoffs/2026-10-27-combat-projectile-hit-sound-audio-request.md
        private static readonly SfxEvent? ProjectileFlightSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.ProjectileFlight
        private static readonly SfxEvent? UnitHitSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.UnitHit

        // 아직 연결 대기인 효과음 칸입니다. 슬롯이 확정되면 해당 값으로 채웁니다.
        private static readonly SfxEvent? ObjectActivatedSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.ObjectActivated(발동·습득 공용)
        private static readonly SfxEvent? ObjectCreatedSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.ObjectCreated
        private static readonly SfxEvent? TornadoWarningSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.TornadoWarning
        private static readonly SfxEvent? ItemActivatedSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.ItemActivated

        /// <summary>투사체 착탄 소리 단서입니다. Unit 플레이어의 BulletFall 슬롯에 투사체 부품의 프리셋을 넘깁니다. 프리셋이 없으면 내지 않습니다.</summary>
        internal static CombatSoundCue ProjectileImpact(int preset) => CombatSoundCue.Effect(UnitEvent.BulletFall, preset);

        /// <summary>투사체 비행 소리 단서입니다. 프리셋은 투사체 부품이 갖습니다. 슬롯이 연결 대기거나 프리셋이 없으면 내지 않습니다.</summary>
        internal static CombatSoundCue ProjectileFlight(int preset) => Slotted(ProjectileFlightSlot, preset);

        /// <summary>피격 소리 단서입니다. 프리셋은 맞은 유닛 외형의 피격 소리 줄이 갖습니다(유닛 소리 설계 US1). 슬롯이 연결 대기면 내지 않습니다.</summary>
        internal static CombatSoundCue UnitHit(int preset) => Slotted(UnitHitSlot, preset);

        /// <summary>토네이도에 휩쓸려 들어 올려지기 시작하는 소리입니다.</summary>
        internal static CombatSoundCue TornadoLift() => TornadoMove();

        /// <summary>토네이도 유닛 연출의 기존 종료 시점에 내는 소리입니다.</summary>
        internal static CombatSoundCue TornadoLand() => CombatSoundCue.EffectWithoutPreset(SfxEvent.Tornado_Ended);

        /// <summary>토네이도가 이동을 시작하는 소리 단서입니다. 원본이 보이는지는 소리 표가 몰라 토네이도 처리기가 묻고 냅니다(토네이도 View 설계 TN8).</summary>
        internal static CombatSoundCue TornadoMove() => CombatSoundCue.EffectWithoutPreset(SfxEvent.Tornado);

        /// <summary>토네이도가 다음 방향을 예고하는 소리 단서입니다. 프리셋은 토네이도 연출 설정이 갖습니다.</summary>
        internal static CombatSoundCue TornadoWarning(int preset) => Slotted(TornadoWarningSlot, preset);

        /// <summary>터널 폭발 Cue가 시작될 때의 소리입니다.</summary>
        internal static CombatSoundCue RockfallWarning() => CombatSoundCue.EffectWithoutPreset(SfxEvent.MineExplosion);

        /// <summary>돌 하나의 기존 착지 Cue 시점에 내는 소리입니다.</summary>
        internal static CombatSoundCue RockfallImpact() => CombatSoundCue.EffectWithoutPreset(SfxEvent.FallStone);

        /// <summary>구름이 다른 칸으로 옮겨질 때 한 번 내는 소리입니다.</summary>
        internal static CombatSoundCue HaboobRelocation() => CombatSoundCue.EffectWithoutPreset(SfxEvent.Haboob);

        private static CombatSoundCue Slotted(SfxEvent? slot, int preset) =>
            slot.HasValue ? CombatSoundCue.Effect(slot.Value, preset) : CombatSoundCue.None;

        /// <summary>걸음 소리 단서입니다. 걸음 레인이 걸음 시작에 냅니다.</summary>
        internal static CombatSoundCue Movement(CharacterCombatFacts unit, CombatSoundPresets presets)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (presets == null) throw new ArgumentNullException(nameof(presets));
            return Unit(UnitEvent.UnitMovement, unit, presets);
        }

        private static CombatSoundCue Unit(UnitEvent slot, CharacterCombatFacts unit, CombatSoundPresets presets) =>
            CombatSoundCue.Effect(slot, presets.Unit(unit.CharacterKey, unit.Owner));
    }
}
