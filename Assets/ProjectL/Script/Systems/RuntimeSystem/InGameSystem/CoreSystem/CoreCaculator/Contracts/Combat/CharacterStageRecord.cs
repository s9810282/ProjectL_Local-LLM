using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 보유 아군 하나가 이번 전투(스테이지)에서 남긴 행동 기록이다. 획득 경험치와 같이 전투 동안만 살고
    /// 전투가 끝나면 사실과 함께 버려진다. 원본은 전투 사실이며, 승리 영수증 행은 정산 때의 사본이다.
    /// 적·소환물은 늘 <see cref="Empty"/>다(유닛별 스테이지 기록 설계 D1·D2).
    /// </summary>
    public sealed class CharacterStageRecord
    {
        /// <summary>아직 아무 기록도 없는 값이다. 전투를 시작하는 모든 사실의 기록이다.</summary>
        public static CharacterStageRecord Empty { get; } = new CharacterStageRecord(0, 0, 0, 0);

        /// <summary>피해 적용 경계를 지난 피해마다 실제로 줄어든 HP의 합이다. 넘친 피해·추락은 들어가지 않는다.</summary>
        public int DamageTaken { get; }
        /// <summary>이 아군에게 귀속된, 적 캐릭터가 실제로 잃은 HP의 합이다. 귀속은 처치와 같은 규칙이다.</summary>
        public int DamageDealt { get; }
        /// <summary>이 아군이 행동자인 명령의 자발 경로 칸 수 합이다. 강제 이동·배치·손패 복귀는 들어가지 않는다.</summary>
        public int DistanceMoved { get; }
        /// <summary>이 아군에게 귀속된 보상 자격 처치 수다. Run 처치 수와 같은 정의다.</summary>
        public int Kills { get; }

        /// <summary>네 값이 모두 0인지다.</summary>
        public bool IsEmpty => DamageTaken == 0 && DamageDealt == 0 && DistanceMoved == 0 && Kills == 0;

        private CharacterStageRecord(int damageTaken, int damageDealt, int distanceMoved, int kills)
        {
            DamageTaken = damageTaken;
            DamageDealt = damageDealt;
            DistanceMoved = distanceMoved;
            Kills = kills;
        }

        /// <summary>
        /// 지정한 값만큼 더한 기록이다. 값은 0 이상이고 합이 넘치면 예외가 난다. 모두 0이면 자신을 돌려준다.
        /// </summary>
        public CharacterStageRecord Add(int damageTaken = 0, int damageDealt = 0, int distanceMoved = 0, int kills = 0)
        {
            if (damageTaken < 0) throw new ArgumentOutOfRangeException(nameof(damageTaken));
            if (damageDealt < 0) throw new ArgumentOutOfRangeException(nameof(damageDealt));
            if (distanceMoved < 0) throw new ArgumentOutOfRangeException(nameof(distanceMoved));
            if (kills < 0) throw new ArgumentOutOfRangeException(nameof(kills));
            if (damageTaken == 0 && damageDealt == 0 && distanceMoved == 0 && kills == 0)
                return this;

            return new CharacterStageRecord(checked(DamageTaken + damageTaken), checked(DamageDealt + damageDealt),
                checked(DistanceMoved + distanceMoved), checked(Kills + kills));
        }

        /// <summary>취소한 이동 거리만 차감한다. 게시 권한은 명령 원본 검증에서 확인한다.</summary>
        public CharacterStageRecord UndoMovement(int distance)
        {
            if (distance < 0 || distance > DistanceMoved) throw new ArgumentOutOfRangeException(nameof(distance));
            return distance == 0 ? this : new CharacterStageRecord(DamageTaken, DamageDealt, DistanceMoved - distance, Kills);
        }

        /// <summary>네 값 각각이 <paramref name="other"/>보다 작지 않은지다. Buffer가 기록이 줄지 않음을 확인할 때 쓴다.</summary>
        public bool IsAtLeast(CharacterStageRecord other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            return DamageTaken >= other.DamageTaken && DamageDealt >= other.DamageDealt &&
                   DistanceMoved >= other.DistanceMoved && Kills >= other.Kills;
        }

        /// <summary>네 값이 모두 같은지 비교한다.</summary>
        public bool SameValues(CharacterStageRecord other) =>
            other != null && DamageTaken == other.DamageTaken && DamageDealt == other.DamageDealt &&
            DistanceMoved == other.DistanceMoved && Kills == other.Kills;
    }
}
