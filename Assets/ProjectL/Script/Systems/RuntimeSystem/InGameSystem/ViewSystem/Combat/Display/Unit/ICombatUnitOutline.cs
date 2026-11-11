using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>현재 개체·모델 수명 동안만 유효한 외곽선 대여입니다.</summary>
    internal interface ICombatUnitOutline : IDisposable
    {
        bool IsAvailable { get; }
    }
}
