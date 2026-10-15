using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.Core
{
    public struct DelayDataHandler
    {
        public Action EndAction;
        public Action TickAction;
        public float DelayTime;
        public DelayDataHandler(Action endAction,
                                Action tickAction,
                                float delayTime)
        {
            EndAction = endAction;
            TickAction = tickAction;
            DelayTime = delayTime;
        }
    }

    public class DelayData
    {
        public Action EndAction;
        public Action TickAction;
        public float DelayTime;

        public void Set(DelayDataHandler data, float delayTime)
        {
            EndAction = data.EndAction;
            TickAction = data.TickAction;
            DelayTime = delayTime;
        }

        public void Clear()
        {
            EndAction = null;
            TickAction = null;
            DelayTime = 0f;
        }
    }
}