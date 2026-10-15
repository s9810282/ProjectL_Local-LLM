using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.Core;
using ProjectL.Framework.SystemBase;
using ProjectL.Framework.Utility;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View
{
    #region 서비스

    public interface IDelayService : IServiceModule
    {
        float CurrentTime { get; }
        public void AddDelayEvent(DelayDataHandler Data);
    }

    #endregion

    #region 시스템 모듈

    internal class DelaySystem : 
        IDelayService, 
        ISystemTick, 
        ISystemShutdown,
        IExportSystemServices
    {
        private class DelayDataComparer : IComparer<DelayData>
        {
            public int Compare(DelayData x, DelayData y)
            {
                if (x.DelayTime < y.DelayTime)
                    return -1;
                else if (x.DelayTime > y.DelayTime)
                    return 1;
                else
                    return 0;
            }
        }

        private class DelayPoolSetup : IPoolLifecycle<DelayData>
        {
            public void OnGet(DelayData item) { }

            public void OnBeforeReturn(DelayData item)
            {
                item.Clear();
            }

            public void OnReturn(DelayData item) { }
        }

        private float CurrentTickTime = 0;
        public float CurrentTime => CurrentTickTime;
        private CustomPool<DelayData> DelayDataPool = new CustomPool<DelayData>(new PoolContext<DelayData>
            (
            () => { return new DelayData(); },
            10,
            null,
            new DelayPoolSetup()
            ));

        private JobQueue<DelayData> DelayQueue = new JobQueue<DelayData>(new DelayDataComparer());
        private List<DelayData> TickList = new List<DelayData>();


        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IDelayService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public void Tick(float deltatime)
        {
            CurrentTickTime += deltatime;

            DelayTick();
            DelayEnd();
        }

        public void Shutdown()
        {

        }

        #region SystemFunction

        public void AddDelayEvent(DelayDataHandler Data)
        {
            DelayData delayData = DelayDataPool.Get();
            delayData.Set(Data, CurrentTickTime + Data.DelayTime);

            DelayQueue.Enqueue(delayData);
            if(delayData.TickAction != null)
            {
                TickList.Add(delayData);
            }
        }

        public void DelayTick()
        {
            if(TickList.Count != 0) 
            {
                int Count = TickList.Count;

                for (int i = 0; i < Count; i++) 
                {
                    TickList[i].TickAction?.Invoke();
                }
            }
        }

        public void DelayEnd()
        {
            if (DelayQueue.Count != 0)
            {
                while (DelayQueue.Count != 0 && DelayQueue.Peek().DelayTime <= CurrentTickTime)
                {
                    DelayData delayData = DelayQueue.Dequeue();

                    Action endAction = delayData.EndAction;

                    TickList.Remove(delayData);
                    DelayDataPool.Return(delayData);

                    endAction?.Invoke();
                }
            }
        }

        #endregion
    }

    #endregion
}
