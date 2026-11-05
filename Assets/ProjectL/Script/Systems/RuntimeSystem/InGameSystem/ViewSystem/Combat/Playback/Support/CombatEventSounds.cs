using System;
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

        /// <summary>효과음 칸을 프리셋 없이 냅니다(장애물 반응).</summary>
        EffectWithoutPreset = 2,

        /// <summary>배경음 프리셋을 바꿉니다.</summary>
        BackgroundPreset = 3,
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
        private CombatSoundCue(CombatSoundCueKind kind, SfxEvent slot, int preset, bool playStageChanger = false, UnitEvent? unitSlot = null)
        {
            Kind = kind;
            Slot = slot;
            UnitSlot = unitSlot;
            Preset = preset;
            PlayStageChanger = playStageChanger;
        }

        internal CombatSoundCueKind Kind { get; }

        /// <summary>SFX 플레이어의 칸입니다. 유닛·배경음 단서에서는 쓰지 않습니다.</summary>
        internal SfxEvent Slot { get; }

        /// <summary>Unit 플레이어의 칸입니다. 값이 없으면 SFX 플레이어의 칸을 씁니다.</summary>
        internal UnitEvent? UnitSlot { get; }

        /// <summary>프리셋 번호입니다. 프리셋 없는 효과음이면 -1입니다.</summary>
        internal int Preset { get; }

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

        internal static CombatSoundCue Background(int preset, bool playStageChanger = false) =>
            new(CombatSoundCueKind.BackgroundPreset, default, preset, playStageChanger);

        public override string ToString() => Kind switch
        {
            CombatSoundCueKind.Effect => $"{UnitSlot?.ToString() ?? Slot.ToString()}({Preset})",
            CombatSoundCueKind.EffectWithoutPreset => UnitSlot?.ToString() ?? Slot.ToString(),
            CombatSoundCueKind.BackgroundPreset => $"BGM({Preset})",
            _ => "None",
        };
    }

    /// <summary>
    /// 결과 재생 사건의 "무엇을 내나" 표입니다(조각·조립 설계 BP8, 결정 Q-B4 A). 사건과 보인 사실로 소리 단서를 답합니다.
    /// 분기는 옛 사건 소리에 소리 창구 2차(오브젝트·추락·약한 지반 붕괴·아이템·장애물 프리셋)를 더한 것이고, 사실 사건의 순간은
    /// 사건 형식 표의 순간 칸을 읽습니다(이펙트 창구 확장 계획 P16). 칸에 넣을 프리셋 번호는
    /// View DB에서 고르는 <see cref="CombatSoundPresets"/>가 답합니다(View 3라운드 W9-2).
    /// </summary>
    internal static class CombatEventSounds
    {
        /// <summary>
        /// 사건 하나의 소리 단서입니다. 사망은 보인 사실에 아직 사망이 반영되지 않았을 때만 냅니다(반영 전에 물어야 함).
        /// </summary>
        /// <param name="presets">칸에 넣을 프리셋 번호를 답합니다.</param>
        /// <param name="findCharacter">프리셋을 고를 유닛을 지금 보인 사실에서 찾습니다.</param>
        internal static CombatSoundCue For(CombatEvent e, CombatSoundPresets presets, Func<EntityId, CharacterCombatFacts> findCharacter)
        {
            if (presets == null) throw new ArgumentNullException(nameof(presets));
            if (findCharacter == null) throw new ArgumentNullException(nameof(findCharacter));
            switch (e)
            {
                case CombatCharacterEvent character when character.Kind == CombatEventKind.Died &&
                    !character.Expected.Health.IsDead && findCharacter(character.Final.EntityId)?.Health.IsDead != true:
                    return Unit(UnitEvent.UnitDead, character.Final, presets);
                case CombatAttackEvent attack when attack.Attack.Succeeded:
                    CharacterCombatFacts attacker = findCharacter(attack.Attack.AttackerId);
                    return attacker == null ? CombatSoundCue.None
                        : CombatSoundCue.Effect(UnitEvent.UnitAttack, presets.Attack(attacker.CharacterKey));
                // 포효 소리는 포효한 유닛의 공격 프리셋이다(View 3라운드 W9-2, 지금 데이터에서는 보스만 포효해 옛 값과 같음).
                case CombatRoarEvent roar:
                    CharacterCombatFacts roarer = findCharacter(roar.BossId);
                    return roarer == null ? CombatSoundCue.None
                        : CombatSoundCue.Effect(UnitEvent.UnitAttack, presets.Attack(roarer.CharacterKey));
                // 신규 표식은 Core의 실제 적용 발동을 읽습니다. 상태 만료·소모 사건에는 소리를 내지 않습니다.
                case CombatEffectsEvent effects when HasEnemyMarkApplication(effects, findCharacter):
                    return CombatSoundCue.EffectWithoutPreset(SfxEvent.Mark);
                // 옛 기록의 표식 적용도 같은 슬롯으로 냅니다. Final이 없으면 소모입니다.
                case CombatHuntersMarkEvent mark when mark.Final != null && findCharacter(mark.TargetId)?.Owner == Owner.Enemy:
                    return CombatSoundCue.EffectWithoutPreset(SfxEvent.Mark);
                case CombatMovementSourceEvent source when WeakGroundEntryPreset(source) is int entryPreset:
                    return CombatSoundCue.Effect(SfxEvent.WeakTerrian, entryPreset);
                case CombatTurnEvent turn when turn.Kind == CombatEventKind.CombatEntered:
                    return CombatSoundCue.Background(presets.Bgm(CombatBgmPhase.Night), playStageChanger: true);
                case CombatTurnEvent turn when turn.Kind == CombatEventKind.NightEnded:
                    return CombatSoundCue.Background(presets.Bgm(CombatBgmPhase.Day));
                // 기차인지는 사건 형식 표의 경로가 정한다(View 3라운드 W12-4).
                case CombatGimmickEvent gimmick when CombatEventTypeTable.IsRailway(gimmick):
                    return CombatSoundCue.EffectWithoutPreset(SfxEvent.TrainCall);
                // 사실 사건의 순간(장애물 반응·오브젝트·추락·약한 지반 붕괴·아이템)은 사건 형식 표의 순간 칸이 고릅니다(이펙트 창구 확장 계획 P16).
                // 어느 사건·조건이 순간인지는 그 칸 한 곳이고, 여기서는 순간 종류마다 무엇을 낼지만 정합니다.
                default:
                    return CombatEventTypeTable.TryGetMoment(e, out CombatEventMoment moment) ? Moment(moment, presets) : CombatSoundCue.None;
            }
        }

        // 순간 종류 → 소리입니다. 모두 사건 순간에 내고 지연은 없습니다(소리 창구 2차 결정 Q-ES7′). 옛 사건 갈래와 결과가 같습니다.
        private static CombatSoundCue Moment(in CombatEventMoment moment, CombatSoundPresets presets) => moment.Kind switch
        {
            // 오브젝트는 발동(지뢰)·습득(깃발·보안관 배지)이 같은 칸이고, 다른 소리는 외형 번호가 가릅니다(소리 창구 2차 결정 Q-ES2″).
            CombatMomentKind.ObjectActivated => Slotted(ObjectActivatedSlot, presets.Object(moment.ObjectKind)),
            // 생성도 같은 외형 번호를 씁니다. 지금 Core는 이 사건을 내지 않습니다(소리 창구 2차 계획 §0.2).
            CombatMomentKind.ObjectCreated => Slotted(ObjectCreatedSlot, presets.Object(moment.ObjectKind)),
            CombatMomentKind.ObstacleHit => Obstacle(UnitEvent.ObsDamaged, moment.ObstacleKind, presets),
            CombatMomentKind.ObstacleDestroyed => Obstacle(UnitEvent.ObsBroken, moment.ObstacleKind, presets),
            // 유닛과 구조물(장애물) 모두 같은 소리를 냅니다(소리 창구 2차 검토 L2 결정 A). 유닛의 이어지는 사망 소리는 추락 조립이 따로 냅니다.
            CombatMomentKind.Fall => Slotted(FallSlot, presets.Fall()),
            // 붕괴는 별도 사건에서 2번을 냅니다. 진입 소리는 위의 누적 진입 사건에서 고릅니다.
            CombatMomentKind.WeakGroundCollapse => CombatSoundCue.Effect(SfxEvent.WeakTerrian, 2),
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

        // Core가 누적한 실제 진입 횟수만 표현에 사용합니다. 붕괴 예정 진입은 뒤의 붕괴 사건이 2번을 내므로 중복 재생하지 않습니다.
        private static int? WeakGroundEntryPreset(CombatMovementSourceEvent source)
        {
            if (source.Expected is not StaticTerrainFact { Kind: StaticTerrainKind.WeakGround } before ||
                source.Final is not StaticTerrainFact { Kind: StaticTerrainKind.WeakGround } after ||
                after.EntryCount <= before.EntryCount || after.PendingCollapse)
                return null;
            return (int)Math.Min(1L, after.EntryCount - 1);
        }

        // 장애물 피해·파괴는 그 장애물 외형의 번호를 넣습니다(소리 창구 2차 결정 Q-ES3). 번호가 없으면(-1) 소리를 끄지 않고 지금처럼
        // 프리셋 없이 냅니다(설계 ES5, 계획 P6). 유닛의 "-1 = 소리 없음"과 뜻이 다른 곳은 여기 하나입니다.
        private static CombatSoundCue Obstacle(UnitEvent slot, ObstacleContentKind obstacle, CombatSoundPresets presets)
        {
            int preset = presets.Obstacle(obstacle);
            return CombatSoundPresets.IsSound(preset) ? CombatSoundCue.Effect(slot, preset) : CombatSoundCue.EffectWithoutPreset(slot);
        }

        // 비행·피격 소리의 효과음 칸입니다(투사체·피격 소리 설계 PS2, 계획 P9). AudioSystem에 그 슬롯이 아직 없어 연결 대기(null)이고,
        // 그동안은 프리셋이 있어도 소리를 내지 않습니다(예외도 없음). 슬롯이 생기면 이 두 줄을 해당 값으로 채웁니다.
        // SfxEvent에 없는 값을 숫자로 짐작해 쓰지 않습니다(Boot 플레이어 슬롯 순서와 어긋날 수 있음). 요청: handoffs/2026-10-27-combat-projectile-hit-sound-audio-request.md
        private static readonly SfxEvent? ProjectileFlightSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.ProjectileFlight
        private static readonly SfxEvent? UnitHitSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.UnitHit

        // 아직 연결 대기인 효과음 칸입니다. 슬롯이 확정되면 해당 값으로 채웁니다.
        private static readonly SfxEvent? ObjectActivatedSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.ObjectActivated(발동·습득 공용)
        private static readonly SfxEvent? ObjectCreatedSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.ObjectCreated
        private static readonly SfxEvent? FallSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.Fall
        private static readonly SfxEvent? TornadoWarningSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.TornadoWarning
        private static readonly SfxEvent? ItemActivatedSlot = null; // AudioSystem 슬롯 대기 → SfxEvent.ItemActivated

        /// <summary>투사체 착탄 소리 단서입니다. Unit 플레이어의 BulletFall 슬롯에 투사체 부품의 프리셋을 넘깁니다. 프리셋이 없으면 내지 않습니다.</summary>
        internal static CombatSoundCue ProjectileImpact(int preset) => CombatSoundCue.Effect(UnitEvent.BulletFall, preset);

        /// <summary>투사체 비행 소리 단서입니다. 프리셋은 투사체 부품이 갖습니다. 슬롯이 연결 대기거나 프리셋이 없으면 내지 않습니다.</summary>
        internal static CombatSoundCue ProjectileFlight(int preset) => Slotted(ProjectileFlightSlot, preset);

        /// <summary>피격 소리 단서입니다. 프리셋은 반응 묶음의 피격 소리 큐가 갖습니다(결정 Q-PS2). 슬롯이 연결 대기면 내지 않습니다.</summary>
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
            if (unit.Owner == Owner.Enemy) return CombatSoundCue.None;
            return Unit(UnitEvent.UnitMovement, unit, presets);
        }

        private static CombatSoundCue Unit(UnitEvent slot, CharacterCombatFacts unit, CombatSoundPresets presets) =>
            CombatSoundCue.Effect(slot, presets.Unit(unit.CharacterKey, unit.Owner));
    }
}
