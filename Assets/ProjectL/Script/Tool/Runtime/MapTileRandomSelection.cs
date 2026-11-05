using System;

namespace ProjectL.Tool.Runtime
{
    /// <summary>
    /// 표시용 시드와 타일 좌표로 외형 후보를 고릅니다. 전투 난수와 전역 난수 상태를 사용하지 않습니다.
    /// 런타임과 에디터 미리보기가 같은 선택 규칙을 사용합니다.
    /// </summary>
    public static class MapTileRandomSelection
    {
        /// <summary>등록한 후보 중 하나의 인덱스입니다. 후보가 하나면 항상 0입니다.</summary>
        public static int PrefabIndex(int seed, int column, int row, int kind, int candidateCount)
        {
            if (candidateCount < 1)
                throw new ArgumentOutOfRangeException(nameof(candidateCount));
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0)
                throw new ArgumentOutOfRangeException(nameof(row));

            return candidateCount == 1 ? 0 : Choose(CellSeed(seed, column, row, kind, 0), candidateCount);
        }

        /// <summary>Random 지정 타일의 실제 90도 단위 회전입니다. 직사각 타일은 0 또는 2만 고릅니다.</summary>
        public static int RotationQuarterTurns(int seed, int column, int row, int kind, bool square)
        {
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0)
                throw new ArgumentOutOfRangeException(nameof(row));

            uint state = CellSeed(seed, column, row, kind, 1);
            return square ? Choose(state, 4) : Choose(state, 2) * 2;
        }

        // 반복 표시에서도 할당 없이 계산합니다. 32비트 값을 섞고 남는 구간을 거절해 후보 수로 나눈 편향을 없앱니다.
        private static int Choose(uint state, int count)
        {
            unchecked
            {
                uint bound = (uint)count;
                uint threshold = (0u - bound) % bound;
                while (true)
                {
                    state += 0x9e3779b9u;
                    uint sample = (state ^ (state >> 16)) * 0x21f0aaadu;
                    sample = (sample ^ (sample >> 15)) * 0x735a2d97u;
                    sample ^= sample >> 15;
                    if (sample >= threshold) return (int)(sample % bound);
                }
            }
        }

        // 좌표·종류·추첨 용도를 섞어 순회 순서와 다른 칸의 후보 개수에 영향을 받지 않게 합니다.
        private static uint CellSeed(int seed, int column, int row, int kind, int purpose)
        {
            unchecked
            {
                uint hash = 2166136261;
                hash = (hash ^ (uint)seed) * 16777619;
                hash = (hash ^ (uint)column) * 16777619;
                hash = (hash ^ (uint)row) * 16777619;
                hash = (hash ^ (uint)kind) * 16777619;
                hash = (hash ^ (uint)purpose) * 16777619;
                return hash;
            }
        }
    }
}
