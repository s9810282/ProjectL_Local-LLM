using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// 셀 좌표를 나타내는 구조체입니다. X와 Y 좌표를 가지며, 셀의 위치를 나타냅니다.
    /// </summary>
    public struct CellCoord : IEquatable<CellCoord>
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public CellCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static CellCoord Zero => new CellCoord(0, 0);
        public static CellCoord Right => new CellCoord(1, 0);
        public static CellCoord Left => new CellCoord(-1, 0);
        public static CellCoord Up => new CellCoord(0, 1);
        public static CellCoord Down => new CellCoord(0, -1);

        public static CellCoord operator +(CellCoord left, CellCoord right)
        {
            return new CellCoord(left.X + right.X, left.Y + right.Y);
        }

        public static CellCoord operator -(CellCoord left, CellCoord right)
        {
            return new CellCoord(left.X - right.X, left.Y - right.Y);
        }

        public static bool operator ==(CellCoord left, CellCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CellCoord left, CellCoord right)
        {
            return !left.Equals(right);
        }

        public bool Equals(CellCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is CellCoord other && Equals(other);
        }

        public override int GetHashCode() // Hash나 딕셔너리 같은거에 쓸 int형으로 바꾸는 것 (빠른 검색 담당)
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"X : {X}, Y : {Y}";
        }
    }
}