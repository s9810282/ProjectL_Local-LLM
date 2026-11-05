using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 걸음 레인 하나(유닛 하나)의 접촉 해제 원본입니다(이동 접촉 설계 M3·M4, 동시 처리 설계 C5). 활성 걸음, 묶음별 해제,
    /// 걸음 사건 완료(순차 진행 허용)를 소유하고 다음 사건이 이 레인의 남은 걸음 연출(보간·미룬 방향 복귀)과 겹쳐도 되는지 답합니다.
    /// 공격 출처의 <see cref="CombatHitGate"/>와 따로입니다. 결과의 걸음 해석(<see cref="CombatStepContacts"/>)은 레인들이 공유합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 세 시점을 구분합니다. 걸음 사건 완료 = 첫 접촉 묶음 해제(묶음이 없으면 보간 끝), 묶음 해제 = 그 묶음 사건을 시작해도 됨,
    /// 걸음 끝 = 보간과 미룬 방향 복귀가 모두 끝남(<see cref="EndStep"/>). 보간 시계는 <see cref="CombatUnitMotionPlayback"/>이 원본이며 여기 복제하지 않습니다.
    /// </para>
    /// <para>
    /// 겹침 규칙(활성 걸음이 있을 때): 1) 새 행동·이동자 자신의 사건(이동 포함)은 걸음 끝까지 기다립니다. 자발적 걸음이면 다른 유닛의 이동도 걸음 끝까지 기다립니다
    /// (자발적 걸음은 같은 순간 묶음에 들지 않아 함께 걸을 이동이 없음, C-4 검토·사용자 결정 "자발적 걸음은 HEAD대로"). 2) 이 걸음의 접촉 묶음 사건은 그 묶음이 해제된 뒤 시작합니다.
    /// 3) 그 밖의 사건은 이 레인이 막지 않습니다. 다른 유닛 사건의 순서(같은 순간 묶음이면 동시, 아니면 앞 사건 완료 뒤)는 순차 재생이 정합니다(동시 처리 설계 C2·C3).
    /// 묶음은 Core 순서대로 해제합니다(뒤 묶음의 접촉 시각이 앞 묶음보다 일러도 앞 묶음 시각까지 기다림).
    /// </para>
    /// <para>완료는 조회입니다(<see cref="IsEventComplete"/>). 걸음 끝·취소(<see cref="EndStep"/>)에서 비우며, 비운 뒤의 진행은 활성 걸음이 없어 무시됩니다. 결과 해석은 레인 집합이 결과마다 다시 만듭니다.</para>
    /// </remarks>
    internal sealed class CombatStepContactGate
    {
        private static readonly IReadOnlyList<CombatStepContacts.Group> NoGroups = Array.Empty<CombatStepContacts.Group>();

        private readonly CombatStepContacts _contacts;
        private readonly List<float> _releaseTimes = new();
        private CombatCharacterEvent _moved;
        private CombatStepContacts.Step _step;
        private EntityId _mover;
        private bool _voluntary;
        private bool _armed;
        private int _released;
        private bool _interpolated;

        /// <param name="contacts">결과마다 한 번 해석한 걸음 구간·접촉 묶음입니다. 걸음 레인들이 공유합니다.</param>
        internal CombatStepContactGate(CombatStepContacts contacts)
        {
            _contacts = contacts ?? throw new ArgumentNullException(nameof(contacts));
        }

        /// <summary>보간 또는 미룬 방향 복귀가 남은 걸음이 있는지입니다.</summary>
        internal bool IsActive => _moved != null;

        /// <summary>활성 걸음의 사건 완료(첫 묶음 해제, 묶음이 없으면 보간 끝)를 지났는지입니다. 활성 걸음이 없으면 true입니다.</summary>
        internal bool IsEventComplete => _moved == null || _released > 0 || _interpolated;

        /// <summary>걸음 사건 완료 뒤 남은 걸음 연출과 겹칠 수 있는지입니다.</summary>
        internal bool AllowsOverlap => _moved != null && IsEventComplete;

        /// <summary>활성 걸음의 접촉 묶음입니다(Core 순서). 없으면 빈 목록입니다.</summary>
        internal IReadOnlyList<CombatStepContacts.Group> Groups => _step?.Groups ?? NoGroups;

        /// <summary>걸음 하나를 활성으로 합니다. 이전 걸음이 끝나기 전에는 시작할 수 없습니다.</summary>
        /// <param name="voluntary">자발적 이동(경로)의 걸음인지입니다. 참이면 규칙 1이 다른 유닛의 이동도 걸음 끝까지 막습니다.</param>
        internal void BeginStep(CombatCharacterEvent moved, bool voluntary)
        {
            if (moved == null) throw new ArgumentNullException(nameof(moved));
            if (moved.Kind != CombatEventKind.Moved) throw new ArgumentException("걸음 사건이 아닙니다.", nameof(moved));
            if (_moved != null)
                throw new InvalidOperationException("[CombatStepContactGate] 이전 걸음이 끝나기 전에 새 걸음을 시작할 수 없습니다.");
            _moved = moved;
            _mover = moved.Expected.EntityId;
            _voluntary = voluntary;
            _step = _contacts.TryGetStep(moved, out CombatStepContacts.Step step) ? step : null;
            _armed = false;
            _released = 0;
            _interpolated = false;
            _releaseTimes.Clear();
        }

        /// <summary>
        /// 보간이 시작될 때 묶음별 접촉 진행률(0~1, <see cref="Groups"/>와 같은 순서)을 받습니다. 걸음당 한 번입니다.
        /// 해제 시각은 Core 순서의 누적 최대값입니다.
        /// </summary>
        internal void Arm(IReadOnlyList<float> contactProgress)
        {
            if (contactProgress == null) throw new ArgumentNullException(nameof(contactProgress));
            if (_moved == null || _armed) return;
            if (contactProgress.Count != Groups.Count)
                throw new ArgumentException("접촉 진행률은 묶음마다 하나여야 합니다.", nameof(contactProgress));
            float latest = 0f;
            for (int i = 0; i < contactProgress.Count; i++)
            {
                float time = contactProgress[i];
                time = float.IsNaN(time) ? 1f : Math.Min(1f, Math.Max(0f, time));
                latest = Math.Max(latest, time);
                _releaseTimes.Add(latest);
            }
            _armed = true;
        }

        /// <summary>보간 진행률까지 닿은 묶음을 해제합니다. 보간이 시작되지 않았으면(Arm 전) 아무것도 하지 않습니다.</summary>
        internal void Advance(float progress)
        {
            if (_moved == null || !_armed) return;
            while (_released < _releaseTimes.Count && _releaseTimes[_released] <= progress)
                _released++;
        }

        /// <summary>보간이 끝났습니다. 남은 묶음을 모두 해제하고 걸음 사건을 완료합니다. 걸음 끝은 <see cref="EndStep"/>입니다.</summary>
        internal void CompleteInterpolation()
        {
            if (_moved == null) return;
            if (!_armed) Arm(new float[Groups.Count]);
            _released = _releaseTimes.Count;
            _interpolated = true;
        }

        /// <summary>남은 걸음 연출(보간·미룬 방향 복귀)이 끝났습니다. 활성 걸음을 비웁니다.</summary>
        internal void EndStep()
        {
            _moved = null;
            _step = null;
            _mover = EntityId.Empty;
            _voluntary = false;
            _armed = false;
            _released = 0;
            _interpolated = false;
            _releaseTimes.Clear();
        }

        /// <summary>그 사건을 지금 시작해도 되는지입니다(겹침 규칙 1~3). 활성 걸음이 없으면 true입니다.</summary>
        internal bool CanBegin(CombatEvent item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (_moved == null) return true;
            if (CombatEventTypeTable.IsActionStart(item) || IsMoverOwn(item) || (_voluntary && CombatEventTypeTable.IsMovement(item)))
                return false;
            if (_step != null && _contacts.TryGetGroup(item, out CombatStepContacts.Group group) && ReferenceEquals(group.Step, _step))
                return group.Index < _released;
            return true;
        }

        // 규칙 1의 이동자 자신의 사건: 이동자의 캐릭터 사건(다음 걸음·반응 자세 대여가 보간과 충돌)과
        // 이동자 사실을 바꾸는 영혼 수집(표시 사실을 도착 칸으로 바꿔 보간 중 순간 이동시킴, 설계 M4).
        private bool IsMoverOwn(CombatEvent item)
        {
            switch (item)
            {
                case CombatCharacterEvent character:
                    return character.Final?.EntityId == _mover || character.Expected?.EntityId == _mover;
                case CombatSoulEvent soul when soul.Characters?.Characters != null:
                    foreach (CharacterStateChange change in soul.Characters.Characters)
                        if (change.Final?.EntityId == _mover || change.Expected?.EntityId == _mover) return true;
                    return false;
                default:
                    return false;
            }
        }
    }
}
