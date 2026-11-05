namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>요청 Buffer가 번호를 붙여 보관한 명령 하나입니다. 보관된 뒤에는 바뀌지 않습니다. 내부 Store에서만 씁니다.</summary>
    internal readonly struct TutorialRequest
    {
        public int Number { get; }
        public TutorialCommandData Data { get; }

        internal TutorialRequest(int number, TutorialCommandData data)
        {
            Number = number;
            Data = data;
        }
    }
}
