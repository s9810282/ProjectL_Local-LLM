using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>
    /// 연출 없이 사실만 반영하는 사건의 표시입니다(Presenter 분리 설계 S3). 표에 이 처리기로 등록한 형식 가운데
    /// 영혼(캐릭터 사실)·오브젝트 발동·지형 종류 바뀜(약한 지반 붕괴)·특성 흔적·아이템·추락·표식·걸린 상태 바뀜만 반영할 것이 있고(구조물 사실은 <see cref="CombatStructureEventHandler"/>가 반영),
    /// 나머지는 틀의 일반 경로(소리·순간 큐·다시 그리기)만 탑니다.
    /// </summary>
    internal sealed class CombatFactEventHandler : CombatEventHandler
    {
        private readonly CombatShownFacts _facts;
        private readonly Action<long> _hideObject;
        private readonly Action<StaticTerrainFact, StaticTerrainFact> _showTerrainChange;
        private readonly Action<TraitTrailFact> _showTrail;

        /// <param name="hideObject">발동한 오브젝트 원본 표시를 숨깁니다(이동 접촉 설계 M6). 없으면 건너뜁니다.</param>
        /// <param name="showTerrainChange">지형의 전후 사실로 진입 하강이나 종류 변경을 표시합니다.</param>
        /// <param name="showTrail">특성 흔적 원본(땅 다지기·먼지 연막)을 놓거나 바꾸거나 지웁니다(특성 시스템 View 설계 TS7). 없으면 건너뜁니다.</param>
        internal CombatFactEventHandler(CombatShownFacts facts, Action<long> hideObject,
            Action<StaticTerrainFact, StaticTerrainFact> showTerrainChange = null, Action<TraitTrailFact> showTrail = null)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _hideObject = hideObject;
            _showTerrainChange = showTerrainChange;
            _showTrail = showTrail;
        }

        internal override void ApplyFacts(CombatEvent e)
        {
            switch (e)
            {
                // ID 39의 옛 기록만 읽습니다. 신규 약점 노출은 전체 TraitState 사건이며 여기서 변환하지 않습니다.
                case CombatHuntersMarkEvent mark:
                    CharacterCombatFacts target = _facts.FindCharacter(mark.TargetId);
                    if (target == null)
                        throw new InvalidOperationException("표식 대상의 표시 사실이 없습니다.");
                    CharacterDebuffState debuffs = target.Debuffs;
                    _facts.ApplyCharacter(target.With(debuffs: new CharacterDebuffState(
                        intimidation: debuffs.Intimidation, defenseReduction: debuffs.DefenseReduction, huntersMark: mark.Final,
                        disarm: debuffs.Disarm, ankleBreaker: debuffs.AnkleBreaker, armorBreak: debuffs.ArmorBreak)));
                    break;
                // 걸린 상태가 바뀐 순간 그 유닛 사실을 사건 값으로 바꿉니다(특성 시스템 View 설계 TS1). 결과 끝 Buffer 사실을 미리 쓰지 않습니다.
                // 사건 시점 공격력·방어력(FinalStats)은 읽는 화면이 없어 보관하지 않습니다(결정 Q-TS8 A).
                case CombatTraitStateEvent state:
                    _facts.ApplyTraitState(state);
                    break;
                case CombatSoulEvent soul when soul.Characters != null:
                    foreach (var change in soul.Characters.Characters) _facts.ApplyCharacter(change.Final);
                    break;
                // 발동한 오브젝트는 사건 시점에 사라집니다(Q-M2 A). 걸음 접촉이면 닿는 순간이고, 접촉 몸은 이미 보간 시작(걸음 전 회전 뒤) 때 정했습니다.
                case CombatObjectActivationEvent activation:
                    _hideObject?.Invoke(activation.Source.Id);
                    break;
                // 약한 지반 진입은 높이를, 붕괴는 외형을 사건 시점에 반영합니다.
                // 결과 끝 갱신이 최종 사실로 다시 맞춥니다. 뒤따르는 추락 사건·사망은 이미 있는 추락 연출이 받습니다. 갈래 판정은 한 곳입니다(이펙트 창구 확장 계획 P11).
                case CombatMovementSourceEvent change when CombatTerrainChanges.TryGetKindChange(change, out var before, out var after) ||
                    CombatTerrainChanges.TryGetWeakGroundDescent(change, out before, out after):
                    _showTerrainChange?.Invoke(before, after);
                    break;
                // 특성 흔적은 생성(이전 값 없음)·남은 턴 바뀜·꺼짐 모두 사건 시점에 그 원본 하나를 맞춥니다(특성 시스템 View 설계 TS7, 인계 §6).
                // 이전 값이 없을 수 있어 결과 값만 봅니다. 결과 끝 갱신이 최종 사실로 다시 맞춥니다.
                case CombatMovementSourceEvent trailChange when trailChange.Final is TraitTrailFact trail:
                    _showTrail?.Invoke(trail);
                    break;
                // 장착·발동·버림 순간에 소지자의 장착 칸을 사건 값(새 장착 키)으로 바꿉니다(가방 설계 P7, 결정 ③).
                // View는 칸을 계산하지 않고 옮기기만 하며, 대상 쪽 남는 효과는 그리지 않습니다(결정 ⑦). 결과 끝 갱신이 최종 사실로 맞춥니다.
                case CombatItemEvent item:
                    ShowEquippedItem(item);
                    break;
                // 낭떠러지 추락은 사실만 남깁니다(임시). 뒤따르는 사망 사건이 이 사실을 보고 일반 사망 대신 추락 연출을 고릅니다.
                case CombatFallEvent fall:
                    _facts.RetainFall(fall.EntityId);
                    break;
            }
        }

        private void ShowEquippedItem(CombatItemEvent item)
        {
            CharacterCombatFacts holder = _facts.FindCharacter(item.HolderId);
            if (holder == null || holder.Items.EquippedItemKey == item.NewItemKey)
                return;
            _facts.ApplyCharacter(ItemCharacterChanges.WithItems(holder,
                new CharacterItemState(item.NewItemKey, holder.Items.Effects)));
        }
    }
}
